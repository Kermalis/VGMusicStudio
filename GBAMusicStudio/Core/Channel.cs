using GBAMusicStudio.Util;
using System;
using System.Collections;

namespace GBAMusicStudio.Core
{
    // All channels have very basic interpolation
    abstract class Channel
    {
        public ADSRState State { get; protected set; } = ADSRState.Dead;
        public byte OwnerIdx { get; protected set; } = 0xFF; // 0xFF indicates no owner

        public Note Note;
        protected ADSR adsr;

        protected byte processStep;
        protected uint pos;
        protected float interPos;
        protected float frequency;

        protected byte curVelocity, prevVelocity;

        public abstract ChannelVolume GetVolume();
        public abstract void SetVolume(byte vol, sbyte pan);
        public abstract void SetPitch(int pitch);
        public virtual void Release()
        {
            if (State < ADSRState.Releasing)
                State = ADSRState.Releasing;
        }

        public abstract void Process(float[] buffer);

        // Returns whether the note is active or not
        public virtual bool TickNote()
        {
            if (State < ADSRState.Releasing)
            {
                if (Note.Duration > 0)
                {
                    Note.Duration--;
                    if (Note.Duration == 0)
                    {
                        State = ADSRState.Releasing;
                        return false;
                    }
                    return true;
                }
                else
                    return true;
            }
            else
            {
                return false;
            }
        }
        public void Stop()
        {
            State = ADSRState.Dead;
            OwnerIdx = 0xFF;
            processStep = 0;
        }
    }
    class DirectSoundChannel : Channel
    {
        struct ProcArgs
        {
            public float LeftVol;
            public float RightVol;
            public float LeftVolStep;
            public float RightVolStep;
            public float InterStep;
        }

        WrappedSample sample; GoldenSunPSG gsPSG;

        bool bFixed, bGoldenSun;
        byte curLeftVol, curRightVol, prevLeftVol, prevRightVol;

        public void Init(byte ownerIdx, Note note, ADSR adsr, WrappedSample sample, byte vol, sbyte pan, int pitch, bool bFixed)
        {
            State = ADSRState.Initializing;
            pos = 0; processStep = 0; interPos = 0;
            OwnerIdx = ownerIdx;
            Note = note;
            this.adsr = adsr;
            this.sample = sample;
            this.bFixed = bFixed;
            bGoldenSun = (ROM.Instance.Game.Engine.HasGoldenSunSynths && sample.bLoop && sample.LoopPoint == 0 && sample.Length == 0);
            if (bGoldenSun)
                gsPSG = ROM.Instance.Reader.ReadObject<GoldenSunPSG>(sample.GetOffset());

            SetVolume(vol, pan);
            SetPitch(pitch);
        }

        public override ChannelVolume GetVolume()
        {
            float baseVel = prevVelocity;
            float deltaVel = (curVelocity - baseVel) / Config.Instance.InterFrames;
            float fromVel = baseVel + deltaVel * processStep;
            float toVel = baseVel + deltaVel * (processStep + 1);
            return new ChannelVolume
            {
                FromLeftVol = prevLeftVol * fromVel / 0x10000,
                FromRightVol = prevRightVol * fromVel / 0x10000,
                ToLeftVol = curLeftVol * toVel / 0x10000,
                ToRightVol = curRightVol * toVel / 0x10000
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            if (State < ADSRState.Releasing)
            {
                var range = Engine.GetPanpotRange();
                var fix = ((Engine.GetMaxVolume() + 1) * range);
                curLeftVol = (byte)(Note.Velocity * vol * (-pan + range) / fix);
                curRightVol = (byte)(Note.Velocity * vol * (pan + range) / fix);
            }
        }
        public override void SetPitch(int pitch)
        {
            frequency = sample.Frequency * (float)Math.Pow(2, (Note.Key - 60) / 12f + pitch / 768f);
        }

        void StepEnvelope()
        {
            switch (State)
            {
                case ADSRState.Initializing:
                    prevLeftVol = curLeftVol;
                    prevRightVol = curRightVol;
                    prevVelocity = (byte)(adsr.A == 0xFF ? 0xFF : 0);
                    curVelocity = adsr.A;
                    processStep = 0;
                    State = ADSRState.Rising;
                    break;
                case ADSRState.Rising:
                    if (++processStep >= Config.Instance.InterFrames)
                    {
                        prevVelocity = curVelocity;
                        processStep = 0;
                        int nextVel = curVelocity + adsr.A;
                        if (nextVel >= 0xFF)
                        {
                            State = ADSRState.Decaying;
                            curVelocity = 0xFF;
                        }
                        else
                            curVelocity = (byte)nextVel;
                    }
                    break;
                case ADSRState.Decaying:
                    if (++processStep >= Config.Instance.InterFrames)
                    {
                        prevVelocity = curVelocity;
                        processStep = 0;
                        int nextVel = (curVelocity * adsr.D) >> 8;
                        if (nextVel <= adsr.S)
                        {
                            State = ADSRState.Playing;
                            curVelocity = adsr.S;
                        }
                        else
                            curVelocity = (byte)nextVel;
                    }
                    break;
                case ADSRState.Playing:
                    if (++processStep >= Config.Instance.InterFrames)
                    {
                        prevVelocity = curVelocity;
                        processStep = 0;
                    }
                    break;
                case ADSRState.Releasing:
                    if (++processStep >= Config.Instance.InterFrames)
                    {
                        prevVelocity = curVelocity;
                        processStep = 0;
                        int nextVel = (curVelocity * adsr.R) >> 8;
                        if (nextVel <= 0)
                        {
                            State = ADSRState.Dying;
                            curVelocity = 0;
                        }
                        else
                        {
                            curVelocity = (byte)nextVel;
                        }
                    }
                    break;
                case ADSRState.Dying:
                    if (++processStep >= Config.Instance.InterFrames)
                    {
                        prevVelocity = curVelocity;
                        Stop();
                    }
                    break;
            }
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            prevLeftVol = curLeftVol; prevRightVol = curRightVol;

            if (State == ADSRState.Dead) return;

            ChannelVolume vol = GetVolume();
            vol.FromLeftVol *= SoundMixer.Instance.DSMasterVolume;
            vol.FromRightVol *= SoundMixer.Instance.DSMasterVolume;
            vol.ToLeftVol *= SoundMixer.Instance.DSMasterVolume;
            vol.ToRightVol *= SoundMixer.Instance.DSMasterVolume;

            ProcArgs pargs;
            pargs.LeftVolStep = (vol.ToLeftVol - vol.FromLeftVol) * SoundMixer.Instance.SamplesReciprocal;
            pargs.RightVolStep = (vol.ToRightVol - vol.FromRightVol) * SoundMixer.Instance.SamplesReciprocal;
            pargs.LeftVol = vol.FromLeftVol;
            pargs.RightVol = vol.FromRightVol;

            if (bFixed && !bGoldenSun)
                pargs.InterStep = (ROM.Instance.Game.Engine.Type == EngineType.M4A ? ROM.Instance.Game.Engine.Frequency : sample.Frequency) * SoundMixer.Instance.SampleRateReciprocal;
            else
                pargs.InterStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            if (bGoldenSun) // Most Golden Sun processing is thanks to ipatix
            {
                pargs.InterStep /= 0x40;
                switch (gsPSG.Type)
                {
                    case GSPSGType.Square: ProcessSquare(buffer, SoundMixer.Instance.SamplesPerBuffer, pargs); break;
                    case GSPSGType.Saw: ProcessSaw(buffer, SoundMixer.Instance.SamplesPerBuffer, pargs); break;
                    case GSPSGType.Triangle: ProcessTri(buffer, SoundMixer.Instance.SamplesPerBuffer, pargs); break;
                }
            }
            else
            {
                ProcessNormal(buffer, SoundMixer.Instance.SamplesPerBuffer, pargs);
            }
        }

        void ProcessNormal(float[] buffer, uint samplesPerBuffer, ProcArgs pargs)
        {
            float GetSample(uint position)
            {
                position += sample.GetOffset();
                return (sample.bUnsigned ? ROM.Instance.Reader.ReadByte(position) - 0x80 : ROM.Instance.Reader.ReadSByte(position)) / (float)0x80;
            }

            int bufPos = 0;
            do
            {
                float baseSamp = GetSample(pos);
                float deltaSamp;
                if (pos + 1 >= sample.Length)
                    deltaSamp = sample.bLoop ? GetSample(sample.LoopPoint) - baseSamp : 0;
                else
                    deltaSamp = GetSample(pos + 1) - baseSamp;
                float finalSamp = baseSamp + deltaSamp * interPos;

                buffer[bufPos++] += finalSamp * pargs.LeftVol;
                buffer[bufPos++] += finalSamp * pargs.RightVol;

                pargs.LeftVol += pargs.LeftVolStep;
                pargs.RightVol += pargs.RightVolStep;

                interPos += pargs.InterStep;
                uint posDelta = (uint)interPos;
                interPos -= posDelta;
                pos += posDelta;
                if (pos >= sample.Length)
                {
                    if (sample.bLoop)
                    {
                        pos = sample.LoopPoint;
                    }
                    else
                    {
                        Stop();
                        break;
                    }
                }
            } while (--samplesPerBuffer > 0);
        }
        void ProcessSquare(float[] buffer, uint samplesPerBuffer, ProcArgs pargs)
        {
            float CalcThresh(uint val)
            {
                uint iThreshold = (uint)(gsPSG.MinimumCycle << 24) + val;
                iThreshold = ((int)iThreshold < 0 ? ~iThreshold : iThreshold) >> 8;
                iThreshold = iThreshold * gsPSG.CycleAmplitude + (uint)(gsPSG.InitialCycle << 24);
                return iThreshold / (float)0x100000000;
            }

            uint curPos = pos += (uint)(processStep == 0 ? gsPSG.CycleSpeed << 24 : 0);
            uint nextPos = curPos + (uint)(gsPSG.CycleSpeed << 24);

            float curThresh = CalcThresh(curPos), nextThresh = CalcThresh(nextPos);

            float deltaThresh = nextThresh - curThresh;
            float baseThresh = curThresh + (deltaThresh * (processStep / (float)Config.Instance.InterFrames));
            float threshStep = deltaThresh / Config.Instance.InterFrames * SoundMixer.Instance.SamplesReciprocal;
            float fThreshold = baseThresh;

            int bufPos = 0;
            do
            {
                float baseSamp = interPos < fThreshold ? 0.5f : -0.5f;
                baseSamp += 0.5f - fThreshold;
                fThreshold += threshStep;
                buffer[bufPos++] += baseSamp * pargs.LeftVol;
                buffer[bufPos++] += baseSamp * pargs.RightVol;

                pargs.LeftVol += pargs.LeftVolStep;
                pargs.RightVol += pargs.RightVolStep;

                interPos += pargs.InterStep;
                if (interPos >= 1) interPos--;
            } while (--samplesPerBuffer > 0);
        }
        void ProcessSaw(float[] buffer, uint samplesPerBuffer, ProcArgs pargs)
        {
            const uint fix = 0x70;

            int bufPos = 0;
            do
            {
                interPos += pargs.InterStep;
                if (interPos >= 1) interPos--;
                uint var1 = (uint)(interPos * 0x100) - fix;
                uint var2 = (uint)(interPos * 0x10000) << 17;
                uint var3 = var1 - (var2 >> 27);
                pos = var3 + (uint)((int)pos >> 1);

                float baseSamp = (float)(int)pos / 0x100;

                buffer[bufPos++] += baseSamp * pargs.LeftVol;
                buffer[bufPos++] += baseSamp * pargs.RightVol;

                pargs.LeftVol += pargs.LeftVolStep;
                pargs.RightVol += pargs.RightVolStep;
            } while (--samplesPerBuffer > 0);
        }
        void ProcessTri(float[] buffer, uint samplesPerBuffer, ProcArgs pargs)
        {
            int bufPos = 0;
            do
            {
                interPos += pargs.InterStep;
                if (interPos >= 1) interPos--;
                float baseSamp;
                if (interPos < 0.5f)
                {
                    baseSamp = interPos * 4 - 1;
                }
                else
                {
                    baseSamp = 3 - (interPos * 4);
                }

                buffer[bufPos++] += baseSamp * pargs.LeftVol;
                buffer[bufPos++] += baseSamp * pargs.RightVol;

                pargs.LeftVol += pargs.LeftVolStep;
                pargs.RightVol += pargs.RightVolStep;
            } while (--samplesPerBuffer > 0);
        }
    }
    abstract class GBChannel : Channel
    {
        protected enum GBPan
        {
            Left,
            Center,
            Right
        }

        ADSRState nextState;
        byte peakVelocity, sustainVelocity;
        protected GBPan curPan = GBPan.Center, prevPan = GBPan.Center;

        protected void Init(byte ownerIdx, Note note, ADSR env)
        {
            State = ADSRState.Initializing;
            OwnerIdx = ownerIdx;
            Note = note;
            adsr.A = (byte)(env.A & 0x7);
            adsr.D = (byte)(env.D & 0x7);
            adsr.S = (byte)(env.S & 0xF);
            adsr.R = (byte)(env.R & 0x7);
        }

        public override void Release()
        {
            if (State < ADSRState.Releasing)
            {
                if (adsr.R == 0)
                {
                    curVelocity = 0;
                    Stop();
                }
                else if (curVelocity == 0 && prevVelocity == 0)
                {
                    Stop();
                }
                else
                {
                    nextState = ADSRState.Releasing;
                }
            }
        }
        public override bool TickNote()
        {
            if (State < ADSRState.Releasing)
            {
                if (Note.Duration > 0)
                {
                    Note.Duration--;
                    if (Note.Duration == 0)
                    {
                        if (curVelocity == 0)
                            Stop();
                        else
                            State = ADSRState.Releasing;
                        return false;
                    }
                    return true;
                }
                else
                    return true;
            }
            else
            {
                return false;
            }
        }

        public override ChannelVolume GetVolume()
        {
            float baseVel = prevVelocity;
            uint step;
            switch (State)
            {
                case ADSRState.Rising:
                    step = adsr.A;
                    break;
                case ADSRState.Decaying:
                    step = adsr.D;
                    break;
                case ADSRState.Releasing:
                case ADSRState.Dying:
                    step = adsr.R;
                    break;
                default:
                    step = 1;
                    break;
            }
            float deltaVel = (curVelocity - baseVel) / (Config.Instance.InterFrames * step);
            float fromVel = baseVel + deltaVel * processStep;
            float toVel = baseVel + deltaVel * (processStep + 1);
            return new ChannelVolume
            {
                FromLeftVol = prevPan == GBPan.Right ? 0 : fromVel / 0x20,
                FromRightVol = prevPan == GBPan.Left ? 0 : fromVel / 0x20,
                ToLeftVol = prevPan == GBPan.Right ? 0 : toVel / 0x20,
                ToRightVol = prevPan == GBPan.Left ? 0 : toVel / 0x20
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            if (State < ADSRState.Releasing)
            {
                var range = Engine.GetPanpotRange() / 2;
                if (pan < -range)
                    curPan = GBPan.Left;
                else if (pan > range)
                    curPan = GBPan.Right;
                else
                    curPan = GBPan.Center;
                peakVelocity = (byte)((Note.Velocity * vol) >> 10).Clamp(0, 0xF);
                sustainVelocity = (byte)((peakVelocity * adsr.S + 0xF) >> 4).Clamp(0, 0xF);
                if (State == ADSRState.Playing)
                    curVelocity = sustainVelocity;
            }
        }

        protected void StepEnvelope()
        {
            void dec()
            {
                prevVelocity = curVelocity;
                processStep = 0;
                if (curVelocity - 1 <= sustainVelocity)
                {
                    curVelocity = sustainVelocity;
                    nextState = ADSRState.Playing;
                }
                else
                {
                    curVelocity = (byte)(curVelocity - 1).Clamp(0, 0xF);
                }
            }
            void sus()
            {
                prevVelocity = curVelocity;
                processStep = 0;
            }
            void rel()
            {
                if (adsr.R == 0)
                {
                    prevVelocity = 0;
                    curVelocity = 0;
                    Stop();
                }
                else
                {
                    prevVelocity = curVelocity;
                    processStep = 0;
                    if (curVelocity - 1 <= 0)
                    {
                        nextState = ADSRState.Dying;
                        curVelocity = 0;
                    }
                    else
                    {
                        curVelocity--;
                    }
                }
            }

            switch (State)
            {
                case ADSRState.Initializing:
                    nextState = ADSRState.Rising;
                    prevPan = curPan;
                    processStep = 0;
                    if ((adsr.A | adsr.D) == 0 || (sustainVelocity == 0 && peakVelocity == 0))
                    {
                        State = ADSRState.Playing;
                        prevVelocity = sustainVelocity;
                        curVelocity = sustainVelocity;
                        return;
                    }
                    else if (adsr.A == 0 && adsr.S < 0xF)
                    {
                        State = ADSRState.Decaying;
                        prevVelocity = peakVelocity;
                        curVelocity = (byte)(peakVelocity - 1).Clamp(0, 0xF);
                        if (curVelocity < sustainVelocity) curVelocity = sustainVelocity;
                        return;
                    }
                    else if (adsr.A == 0)
                    {
                        State = ADSRState.Playing;
                        prevVelocity = sustainVelocity;
                        curVelocity = sustainVelocity;
                        return;
                    }
                    else
                    {
                        State = ADSRState.Rising;
                        prevVelocity = 0;
                        curVelocity = 1;
                        return;
                    }
                case ADSRState.Rising:
                    if (++processStep >= Config.Instance.InterFrames * adsr.A)
                    {
                        if (nextState == ADSRState.Decaying)
                        {
                            State = ADSRState.Decaying;
                            dec(); return;
                        }
                        if (nextState == ADSRState.Playing)
                        {
                            State = ADSRState.Playing;
                            sus(); return;
                        }
                        if (nextState == ADSRState.Releasing)
                        {
                            State = ADSRState.Releasing;
                            rel(); return;
                        }
                        prevVelocity = curVelocity;
                        processStep = 0;
                        if (++curVelocity >= peakVelocity)
                        {
                            if (adsr.D == 0)
                            {
                                nextState = ADSRState.Playing;
                            }
                            else if (peakVelocity == sustainVelocity)
                            {
                                nextState = ADSRState.Playing;
                                curVelocity = peakVelocity;
                            }
                            else
                            {
                                curVelocity = peakVelocity;
                                nextState = ADSRState.Decaying;
                            }
                        }
                    }
                    break;
                case ADSRState.Decaying:
                    if (++processStep >= Config.Instance.InterFrames * adsr.D)
                    {
                        if (nextState == ADSRState.Playing)
                        {
                            State = ADSRState.Playing;
                            sus(); return;
                        }
                        if (nextState == ADSRState.Releasing)
                        {
                            State = ADSRState.Releasing;
                            rel(); return;
                        }
                        dec();
                    }
                    break;
                case ADSRState.Playing:
                    if (++processStep >= Config.Instance.InterFrames)
                    {
                        if (nextState == ADSRState.Releasing)
                        {
                            State = ADSRState.Releasing;
                            rel(); return;
                        }
                        sus();
                    }
                    break;
                case ADSRState.Releasing:
                    if (++processStep >= Config.Instance.InterFrames * adsr.R)
                    {
                        if (nextState == ADSRState.Dying)
                        {
                            Stop();
                            return;
                        }
                        rel();
                    }
                    break;
            }
        }
    }

    class SquareChannel : GBChannel
    {
        float[] pat;

        public SquareChannel() : base() { }
        public void Init(byte ownerIdx, Note note, ADSR env, SquarePattern pattern)
        {
            Init(ownerIdx, note, env);
            switch (pattern)
            {
                default: pat = GBSamples.SquareD12; break;
                case SquarePattern.D12: pat = GBSamples.SquareD25; break;
                case SquarePattern.D25: pat = GBSamples.SquareD50; break;
                case SquarePattern.D75: pat = GBSamples.SquareD75; break;
            }
        }

        public override void SetPitch(int pitch)
        {
            frequency = 3520 * (float)Math.Pow(2, (Note.Key - 69) / 12f + pitch / 768f);
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            prevPan = curPan;
            if (State == ADSRState.Dead)
                return;

            ChannelVolume vol = GetVolume();
            float leftVolStep = (vol.ToLeftVol - vol.FromLeftVol) * SoundMixer.Instance.SamplesReciprocal;
            float rightVolStep = (vol.ToRightVol - vol.FromRightVol) * SoundMixer.Instance.SamplesReciprocal;
            float leftVol = vol.FromLeftVol;
            float rightVol = vol.FromRightVol;
            float interStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            int bufPos = 0; uint samplesPerBuffer = SoundMixer.Instance.SamplesPerBuffer;
            do
            {
                float samp = pat[pos];

                buffer[bufPos++] += samp * leftVol;
                buffer[bufPos++] += samp * rightVol;

                leftVol += leftVolStep;
                rightVol += rightVolStep;

                interPos += interStep;
                uint posDelta = (uint)interPos;
                interPos -= posDelta;
                pos = (pos + posDelta) & 0x7;
            } while (--samplesPerBuffer > 0);
        }
    }
    class WaveChannel : GBChannel
    {
        float[] sample;

        public WaveChannel() : base() { }
        public void Init(byte ownerIdx, Note note, ADSR env, uint address)
        {
            Init(ownerIdx, note, env);

            sample = GBSamples.PCM4ToFloat(address);
        }

        public override void SetPitch(int pitch)
        {
            frequency = 7040 * (float)Math.Pow(2, (Note.Key - 69) / 12f + pitch / 768f);
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            prevPan = curPan;
            if (State == ADSRState.Dead)
                return;

            ChannelVolume vol = GetVolume();
            float leftVolStep = (vol.ToLeftVol - vol.FromLeftVol) * SoundMixer.Instance.SamplesReciprocal;
            float rightVolStep = (vol.ToRightVol - vol.FromRightVol) * SoundMixer.Instance.SamplesReciprocal;
            float leftVol = vol.FromLeftVol;
            float rightVol = vol.FromRightVol;
            float interStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            int bufPos = 0; uint samplesPerBuffer = SoundMixer.Instance.SamplesPerBuffer;
            do
            {
                float samp = sample[pos];

                buffer[bufPos++] += samp * leftVol;
                buffer[bufPos++] += samp * rightVol;

                leftVol += leftVolStep;
                rightVol += rightVolStep;

                interPos += interStep;
                uint posDelta = (uint)interPos;
                interPos -= posDelta;
                pos = (pos + posDelta) & 0x1F;
            } while (--samplesPerBuffer > 0);
        }
    }
    class NoiseChannel : GBChannel
    {
        BitArray pat;

        public void Init(byte ownerIdx, Note note, ADSR env, NoisePattern pattern)
        {
            Init(ownerIdx, note, env);
            pat = (pattern == NoisePattern.Fine ? GBSamples.NoiseFine : GBSamples.NoiseRough);
        }

        public override void SetPitch(int pitch)
        {
            frequency = (0x1000 * (float)Math.Pow(8, (Note.Key - 60) / 12f + pitch / 768f)).Clamp(8, 0x80000); // Thanks ipatix
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            prevPan = curPan;
            if (State == ADSRState.Dead)
                return;

            ChannelVolume vol = GetVolume();
            float leftVolStep = (vol.ToLeftVol - vol.FromLeftVol) * SoundMixer.Instance.SamplesReciprocal;
            float rightVolStep = (vol.ToRightVol - vol.FromRightVol) * SoundMixer.Instance.SamplesReciprocal;
            float leftVol = vol.FromLeftVol;
            float rightVol = vol.FromRightVol;
            float interStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            int bufPos = 0; uint samplesPerBuffer = SoundMixer.Instance.SamplesPerBuffer;
            do
            {
                float samp = pat[(int)pos & (pat.Length - 1)] ? 0.5f : -0.5f;

                buffer[bufPos++] += samp * leftVol;
                buffer[bufPos++] += samp * rightVol;

                leftVol += leftVolStep;
                rightVol += rightVolStep;

                interPos += interStep;
                uint posDelta = (uint)interPos;
                interPos -= posDelta;
                pos = (uint)((pos + posDelta) & (pat.Length - 1));
            } while (--samplesPerBuffer > 0);
        }
    }
}
