using GBAMusicStudio.Util;
using System;
using System.Collections;

namespace GBAMusicStudio.Core
{
    abstract class Channel
    {
        public ADSRState State { get; protected set; } = ADSRState.Dead;
        public byte OwnerIdx { get; protected set; } = 0xFF; // 0xFF indicates no owner

        public Note Note;
        protected ADSR adsr;

        protected byte velocity;
        protected int pos;
        protected float interPos;
        protected float frequency;

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
        }
    }
    class DirectSoundChannel : Channel
    {
        struct ProcArgs
        {
            public float LeftVol;
            public float RightVol;
            public float InterStep;
        }

        WrappedSample sample; GoldenSunPSG gsPSG;

        bool bFixed, bGoldenSun, bCompressed;
        byte leftVol, rightVol;
        sbyte[] decompressedSample;

        public void Init(byte ownerIdx, Note note, ADSR adsr, WrappedSample sample, byte vol, sbyte pan, int pitch, bool bFixed, bool bCompressed)
        {
            State = ADSRState.Initializing;
            pos = 0; interPos = 0;
            OwnerIdx = ownerIdx;
            Note = note;
            this.adsr = adsr;
            this.sample = sample;
            this.bFixed = bFixed;
            this.bCompressed = bCompressed;
            decompressedSample = bCompressed ? Samples.Decompress(sample) : null;
            bGoldenSun = ROM.Instance.Game.Engine.HasGoldenSunSynths && sample.bLoop && sample.LoopPoint == 0 && sample.Length == 0;
            if (bGoldenSun)
                gsPSG = ROM.Instance.Reader.ReadObject<GoldenSunPSG>(sample.GetOffset());

            SetVolume(vol, pan);
            SetPitch(pitch);
        }

        public override ChannelVolume GetVolume()
        {
            const float max = 0x10000; // 0x100 * 0x100
            return new ChannelVolume
            {
                LeftVol = leftVol * velocity / max,
                RightVol = rightVol * velocity / max
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            if (State < ADSRState.Releasing)
            {
                var range = Engine.GetPanpotRange();
                var fix = (Engine.GetMaxVolume() + 1) * range;
                leftVol = (byte)(Note.Velocity * vol * (-pan + range) / fix);
                rightVol = (byte)(Note.Velocity * vol * (pan + range) / fix);
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
                    velocity = adsr.A;
                    State = ADSRState.Rising;
                    break;
                case ADSRState.Rising:
                    int nextVel = velocity + adsr.A;
                    if (nextVel >= 0xFF)
                    {
                        State = ADSRState.Decaying;
                        velocity = 0xFF;
                    }
                    else
                        velocity = (byte)nextVel;
                    break;
                case ADSRState.Decaying:
                    nextVel = (velocity * adsr.D) >> 8;
                    if (nextVel <= adsr.S)
                    {
                        State = ADSRState.Playing;
                        velocity = adsr.S;
                    }
                    else
                        velocity = (byte)nextVel;
                    break;
                case ADSRState.Playing:
                    break;
                case ADSRState.Releasing:
                    nextVel = (velocity * adsr.R) >> 8;
                    if (nextVel <= 0)
                    {
                        State = ADSRState.Dying;
                        velocity = 0;
                    }
                    else
                    {
                        velocity = (byte)nextVel;
                    }
                    break;
                case ADSRState.Dying:
                    Stop();
                    break;
            }
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();

            if (State == ADSRState.Dead) return;

            ChannelVolume vol = GetVolume();
            vol.LeftVol *= SoundMixer.Instance.DSMasterVolume;
            vol.RightVol *= SoundMixer.Instance.DSMasterVolume;

            ProcArgs pargs;
            pargs.LeftVol = vol.LeftVol;
            pargs.RightVol = vol.RightVol;

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
            else if (bCompressed)
            {
                ProcessCompressed(buffer, SoundMixer.Instance.SamplesPerBuffer, pargs);
            }
            else
            {
                ProcessNormal(buffer, SoundMixer.Instance.SamplesPerBuffer, pargs);
            }
        }

        void ProcessNormal(float[] buffer, int samplesPerBuffer, ProcArgs pargs)
        {
            float GetSample(int position)
            {
                position += sample.GetOffset();
                return (sample.bUnsigned ? ROM.Instance.Reader.ReadByte(position) - 0x80 : ROM.Instance.Reader.ReadSByte(position)) / (float)0x80;
            }

            int bufPos = 0;
            do
            {
                float samp = GetSample(pos);

                buffer[bufPos++] += samp * pargs.LeftVol;
                buffer[bufPos++] += samp * pargs.RightVol;

                interPos += pargs.InterStep;
                int posDelta = (int)interPos;
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
        void ProcessCompressed(float[] buffer, int samplesPerBuffer, ProcArgs pargs)
        {
            int bufPos = 0;
            do
            {
                float samp = decompressedSample[pos] / (float)0x80;

                buffer[bufPos++] += samp * pargs.LeftVol;
                buffer[bufPos++] += samp * pargs.RightVol;

                interPos += pargs.InterStep;
                int posDelta = (int)interPos;
                interPos -= posDelta;
                pos += posDelta;
                if (pos >= decompressedSample.Length)
                {
                    Stop();
                    break;
                }
            } while (--samplesPerBuffer > 0);
        }
        void ProcessSquare(float[] buffer, int samplesPerBuffer, ProcArgs pargs)
        {
            pos += gsPSG.CycleSpeed << 24;
            int iThreshold = (gsPSG.MinimumCycle << 24) + pos;
            iThreshold = (iThreshold < 0 ? ~iThreshold : iThreshold) >> 8;
            iThreshold = iThreshold * gsPSG.CycleAmplitude + (gsPSG.InitialCycle << 24);
            float threshold = iThreshold / (float)0x100000000;

            int bufPos = 0;
            do
            {
                float samp = interPos < threshold ? 0.5f : -0.5f;
                samp += 0.5f - threshold;
                buffer[bufPos++] += samp * pargs.LeftVol;
                buffer[bufPos++] += samp * pargs.RightVol;

                interPos += pargs.InterStep;
                if (interPos >= 1) interPos--;
            } while (--samplesPerBuffer > 0);
        }
        void ProcessSaw(float[] buffer, int samplesPerBuffer, ProcArgs pargs)
        {
            const int fix = 0x70;

            int bufPos = 0;
            do
            {
                interPos += pargs.InterStep;
                if (interPos >= 1) interPos--;
                int var1 = (int)(interPos * 0x100) - fix;
                int var2 = (int)(interPos * 0x10000) << 17;
                int var3 = var1 - (var2 >> 27);
                pos = var3 + (pos >> 1);

                float samp = pos / (float)0x100;

                buffer[bufPos++] += samp * pargs.LeftVol;
                buffer[bufPos++] += samp * pargs.RightVol;
            } while (--samplesPerBuffer > 0);
        }
        void ProcessTri(float[] buffer, int samplesPerBuffer, ProcArgs pargs)
        {
            int bufPos = 0;
            do
            {
                interPos += pargs.InterStep;
                if (interPos >= 1) interPos--;
                float samp = interPos < 0.5f ? interPos * 4 - 1 : 3 - (interPos * 4);

                buffer[bufPos++] += samp * pargs.LeftVol;
                buffer[bufPos++] += samp * pargs.RightVol;
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

        byte processStep;
        ADSRState nextState;
        byte peakVelocity, sustainVelocity;
        protected GBPan panpot = GBPan.Center;

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
                    velocity = 0;
                    Stop();
                }
                else if (velocity == 0)
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
                        if (velocity == 0)
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
            const float max = 0x20;
            return new ChannelVolume
            {
                LeftVol = panpot == GBPan.Right ? 0 : velocity / max,
                RightVol = panpot == GBPan.Left ? 0 : velocity / max
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            if (State < ADSRState.Releasing)
            {
                var range = Engine.GetPanpotRange() / 2;
                if (pan < -range)
                    panpot = GBPan.Left;
                else if (pan > range)
                    panpot = GBPan.Right;
                else
                    panpot = GBPan.Center;
                peakVelocity = (byte)((Note.Velocity * vol) >> 10).Clamp(0, 0xF);
                sustainVelocity = (byte)((peakVelocity * adsr.S + 0x10) >> 4).Clamp(0, 0xF);
                if (State == ADSRState.Playing)
                    velocity = sustainVelocity;
            }
        }

        protected void StepEnvelope()
        {
            void dec()
            {
                processStep = 0;
                if (velocity - 1 <= sustainVelocity)
                {
                    velocity = sustainVelocity;
                    nextState = ADSRState.Playing;
                }
                else
                {
                    velocity = (byte)(velocity - 1).Clamp(0, 0xF);
                }
            }
            void sus()
            {
                processStep = 0;
            }
            void rel()
            {
                if (adsr.R == 0)
                {
                    velocity = 0;
                    Stop();
                }
                else
                {
                    processStep = 0;
                    if (velocity - 1 <= 0)
                    {
                        nextState = ADSRState.Dying;
                        velocity = 0;
                    }
                    else
                    {
                        velocity--;
                    }
                }
            }

            switch (State)
            {
                case ADSRState.Initializing:
                    nextState = ADSRState.Rising;
                    processStep = 0;
                    if ((adsr.A | adsr.D) == 0 || (sustainVelocity == 0 && peakVelocity == 0))
                    {
                        State = ADSRState.Playing;
                        velocity = sustainVelocity;
                        return;
                    }
                    else if (adsr.A == 0 && adsr.S < 0xF)
                    {
                        State = ADSRState.Decaying;
                        velocity = (byte)(peakVelocity - 1).Clamp(0, 0xF);
                        if (velocity < sustainVelocity) velocity = sustainVelocity;
                        return;
                    }
                    else if (adsr.A == 0)
                    {
                        State = ADSRState.Playing;
                        velocity = sustainVelocity;
                        return;
                    }
                    else
                    {
                        State = ADSRState.Rising;
                        velocity = 1;
                        return;
                    }
                case ADSRState.Rising:
                    if (++processStep >= adsr.A)
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
                        processStep = 0;
                        if (++velocity >= peakVelocity)
                        {
                            if (adsr.D == 0)
                            {
                                nextState = ADSRState.Playing;
                            }
                            else if (peakVelocity == sustainVelocity)
                            {
                                nextState = ADSRState.Playing;
                                velocity = peakVelocity;
                            }
                            else
                            {
                                velocity = peakVelocity;
                                nextState = ADSRState.Decaying;
                            }
                        }
                    }
                    break;
                case ADSRState.Decaying:
                    if (++processStep >= adsr.D)
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
                    if (++processStep >= 1)
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
                    if (++processStep >= adsr.R)
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
                default: pat = Samples.SquareD12; break;
                case SquarePattern.D12: pat = Samples.SquareD25; break;
                case SquarePattern.D25: pat = Samples.SquareD50; break;
                case SquarePattern.D75: pat = Samples.SquareD75; break;
            }
        }

        public override void SetPitch(int pitch)
        {
            frequency = 3520 * (float)Math.Pow(2, (Note.Key - 69) / 12f + pitch / 768f);
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == ADSRState.Dead)
                return;

            ChannelVolume vol = GetVolume();
            float interStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = SoundMixer.Instance.SamplesPerBuffer;
            do
            {
                float samp = pat[pos];

                buffer[bufPos++] += samp * vol.LeftVol;
                buffer[bufPos++] += samp * vol.RightVol;

                interPos += interStep;
                int posDelta = (int)interPos;
                interPos -= posDelta;
                pos = (pos + posDelta) & 0x7;
            } while (--samplesPerBuffer > 0);
        }
    }
    class WaveChannel : GBChannel
    {
        float[] sample;

        public WaveChannel() : base() { }
        public void Init(byte ownerIdx, Note note, ADSR env, int address)
        {
            Init(ownerIdx, note, env);

            sample = Samples.PCM4ToFloat(address);
        }

        public override void SetPitch(int pitch)
        {
            frequency = 7040 * (float)Math.Pow(2, (Note.Key - 69) / 12f + pitch / 768f);
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == ADSRState.Dead)
                return;

            ChannelVolume vol = GetVolume();
            float interStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = SoundMixer.Instance.SamplesPerBuffer;
            do
            {
                float samp = sample[pos];

                buffer[bufPos++] += samp * vol.LeftVol;
                buffer[bufPos++] += samp * vol.RightVol;

                interPos += interStep;
                int posDelta = (int)interPos;
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
            pat = (pattern == NoisePattern.Fine ? Samples.NoiseFine : Samples.NoiseRough);
        }

        public override void SetPitch(int pitch)
        {
            frequency = (0x1000 * (float)Math.Pow(8, (Note.Key - 60) / 12f + pitch / 768f)).Clamp(8, 0x80000); // Thanks ipatix
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == ADSRState.Dead)
                return;

            ChannelVolume vol = GetVolume();
            float interStep = frequency * SoundMixer.Instance.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = SoundMixer.Instance.SamplesPerBuffer;
            do
            {
                float samp = pat[pos & (pat.Length - 1)] ? 0.5f : -0.5f;

                buffer[bufPos++] += samp * vol.LeftVol;
                buffer[bufPos++] += samp * vol.RightVol;

                interPos += interStep;
                int posDelta = (int)interPos;
                interPos -= posDelta;
                pos = (pos + posDelta) & (pat.Length - 1);
            } while (--samplesPerBuffer > 0);
        }
    }
}
