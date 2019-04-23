using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    struct ChannelVolume
    {
        public float LeftVol, RightVol;
    }
    struct Note
    {
        public byte Key, OriginalKey;
        public byte Velocity;
        public int Duration; // -1 = forever
    }

    abstract class Channel
    {
        public EnvelopeState State = EnvelopeState.Dead;
        public Track Owner = null;
        protected readonly M4AMixer mixer;

        public Note Note; // Must be a struct & field
        protected ADSR adsr;

        protected byte velocity;
        protected int pos;
        protected float interPos;
        protected float frequency;

        protected Channel(M4AMixer mixer)
        {
            this.mixer = mixer;
        }

        public abstract ChannelVolume GetVolume();
        public abstract void SetVolume(byte vol, sbyte pan);
        public abstract void SetPitch(int pitch);
        public virtual void Release()
        {
            if (State < EnvelopeState.Releasing)
            {
                State = EnvelopeState.Releasing;
            }
        }

        public abstract void Process(float[] buffer);

        // Returns whether the note is active or not
        public virtual bool TickNote()
        {
            if (State < EnvelopeState.Releasing)
            {
                if (Note.Duration > 0)
                {
                    Note.Duration--;
                    if (Note.Duration == 0)
                    {
                        State = EnvelopeState.Releasing;
                        return false;
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        public void Stop()
        {
            State = EnvelopeState.Dead;
            if (Owner != null)
            {
                Owner.Channels.Remove(this);
            }
            Owner = null;
        }
    }
    class DirectSoundChannel : Channel
    {
        SampleHeader sampleHeader; int sampleOffset; GoldenSunPSG gsPSG;

        bool bFixed, bGoldenSun, bCompressed;
        byte leftVol, rightVol;
        sbyte[] decompressedSample;

        public DirectSoundChannel(M4AMixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR adsr, int sampleOffset, byte vol, sbyte pan, int pitch, bool bFixed, bool bCompressed)
        {
            State = EnvelopeState.Initializing;
            pos = 0; interPos = 0;
            if (Owner != null)
            {
                Owner.Channels.Remove(this);
            }
            Owner = owner;
            Owner.Channels.Add(this);
            Note = note;
            this.adsr = adsr;
            sampleHeader = mixer.Reader.ReadObject<SampleHeader>(sampleOffset);
            this.sampleOffset = sampleOffset + 0x10;
            this.bFixed = bFixed;
            this.bCompressed = bCompressed;
            decompressedSample = bCompressed ? Samples.Decompress(mixer.Reader, this.sampleOffset, sampleHeader.Length) : null;
            //bGoldenSun = ROM.Instance.Game.Engine.HasGoldenSunSynths && sample.bLoop && sample.LoopPoint == 0 && sample.Length == 0;
            bGoldenSun = false && sampleHeader.DoesLoop == 0x40000000 && sampleHeader.LoopOffset == 0 && sampleHeader.Length == 0; // TODO
            if (bGoldenSun)
            {
                gsPSG = mixer.Reader.ReadObject<GoldenSunPSG>(this.sampleOffset);
            }
            SetVolume(vol, pan);
            SetPitch(pitch);
        }

        public override ChannelVolume GetVolume()
        {
            const float max = 0x10000;
            return new ChannelVolume
            {
                LeftVol = leftVol * velocity / max * mixer.DSMasterVolume,
                RightVol = rightVol * velocity / max * mixer.DSMasterVolume
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            const int fix = 0x2000;
            if (State < EnvelopeState.Releasing)
            {
                int a = Note.Velocity * vol;
                leftVol = (byte)(a * (-pan + 0x40) / fix);
                rightVol = (byte)(a * (pan + 0x40) / fix);
            }
        }
        public override void SetPitch(int pitch)
        {
            frequency = (sampleHeader.SampleRate >> 10) * (float)Math.Pow(2, (Note.Key - 60) / 12f + pitch / 768f);
        }

        void StepEnvelope()
        {
            switch (State)
            {
                case EnvelopeState.Initializing:
                    {
                        velocity = adsr.A;
                        State = EnvelopeState.Rising;
                        break;
                    }
                case EnvelopeState.Rising:
                    {
                        int nextVel = velocity + adsr.A;
                        if (nextVel >= 0xFF)
                        {
                            State = EnvelopeState.Decaying;
                            velocity = 0xFF;
                        }
                        else
                        {
                            velocity = (byte)nextVel;
                        }
                        break;
                    }
                case EnvelopeState.Decaying:
                    {
                        int nextVel = (velocity * adsr.D) >> 8;
                        if (nextVel <= adsr.S)
                        {
                            State = EnvelopeState.Playing;
                            velocity = adsr.S;
                        }
                        else
                        {
                            velocity = (byte)nextVel;
                        }
                        break;
                    }
                case EnvelopeState.Playing:
                    {
                        break;
                    }
                case EnvelopeState.Releasing:
                    {
                        int nextVel = (velocity * adsr.R) >> 8;
                        if (nextVel <= 0)
                        {
                            State = EnvelopeState.Dying;
                            velocity = 0;
                        }
                        else
                        {
                            velocity = (byte)nextVel;
                        }
                        break;
                    }
                case EnvelopeState.Dying:
                    {
                        Stop();
                        break;
                    }
            }
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

            ChannelVolume vol = GetVolume();
            float interStep;
            if (bFixed && !bGoldenSun)
            {
                interStep = 13379 * mixer.SampleRateReciprocal; // TODO
                //interStep = ROM.Instance.Game.Engine.Frequency * mixer.SampleRateReciprocal;
            }
            else
            {
                interStep = frequency * mixer.SampleRateReciprocal;
            }

            if (bGoldenSun) // Most Golden Sun processing is thanks to ipatix
            {
                interStep /= 0x40;
                switch (gsPSG.Type)
                {
                    case GoldenSunPSGType.Square:
                        {
                            pos += gsPSG.CycleSpeed << 24;
                            int iThreshold = (gsPSG.MinimumCycle << 24) + pos;
                            iThreshold = (iThreshold < 0 ? ~iThreshold : iThreshold) >> 8;
                            iThreshold = iThreshold * gsPSG.CycleAmplitude + (gsPSG.InitialCycle << 24);
                            float threshold = iThreshold / (float)0x100000000;

                            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
                            do
                            {
                                float samp = interPos < threshold ? 0.5f : -0.5f;
                                samp += 0.5f - threshold;
                                buffer[bufPos++] += samp * vol.LeftVol;
                                buffer[bufPos++] += samp * vol.RightVol;

                                interPos += interStep;
                                if (interPos >= 1)
                                {
                                    interPos--;
                                }
                            } while (--samplesPerBuffer > 0);
                            break;
                        }
                    case GoldenSunPSGType.Saw:
                        {
                            const int fix = 0x70;

                            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
                            do
                            {
                                interPos += interStep;
                                if (interPos >= 1)
                                {
                                    interPos--;
                                }
                                int var1 = (int)(interPos * 0x100) - fix;
                                int var2 = (int)(interPos * 0x10000) << 17;
                                int var3 = var1 - (var2 >> 27);
                                pos = var3 + (pos >> 1);

                                float samp = pos / (float)0x100;

                                buffer[bufPos++] += samp * vol.LeftVol;
                                buffer[bufPos++] += samp * vol.RightVol;
                            } while (--samplesPerBuffer > 0);
                            break;
                        }
                    case GoldenSunPSGType.Triangle:
                        {
                            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
                            do
                            {
                                interPos += interStep;
                                if (interPos >= 1)
                                {
                                    interPos--;
                                }
                                float samp = interPos < 0.5f ? interPos * 4 - 1 : 3 - (interPos * 4);

                                buffer[bufPos++] += samp * vol.LeftVol;
                                buffer[bufPos++] += samp * vol.RightVol;
                            } while (--samplesPerBuffer > 0);
                            break;
                        }
                }
            }
            else if (bCompressed)
            {
                int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
                do
                {
                    float samp = decompressedSample[pos] / (float)0x80;

                    buffer[bufPos++] += samp * vol.LeftVol;
                    buffer[bufPos++] += samp * vol.RightVol;

                    interPos += interStep;
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
            else
            {
                int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
                do
                {
                    float samp = mixer.Reader.ReadSByte(pos + sampleOffset) / (float)0x80;

                    buffer[bufPos++] += samp * vol.LeftVol;
                    buffer[bufPos++] += samp * vol.RightVol;

                    interPos += interStep;
                    int posDelta = (int)interPos;
                    interPos -= posDelta;
                    pos += posDelta;
                    if (pos >= sampleHeader.Length)
                    {
                        if (sampleHeader.DoesLoop == 0x40000000)
                        {
                            pos = sampleHeader.LoopOffset;
                        }
                        else
                        {
                            Stop();
                            break;
                        }
                    }
                } while (--samplesPerBuffer > 0);
            }
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
        EnvelopeState nextState;
        byte peakVelocity, sustainVelocity;
        protected GBPan panpot = GBPan.Center;

        public GBChannel(M4AMixer mixer) : base(mixer) { }
        protected void Init(Track owner, Note note, ADSR env)
        {
            State = EnvelopeState.Initializing;
            if (Owner != null)
            {
                Owner.Channels.Remove(this);
            }
            Owner = owner;
            Owner.Channels.Add(this);
            Note = note;
            adsr.A = (byte)(env.A & 0x7);
            adsr.D = (byte)(env.D & 0x7);
            adsr.S = (byte)(env.S & 0xF);
            adsr.R = (byte)(env.R & 0x7);
        }

        public override void Release()
        {
            if (State < EnvelopeState.Releasing)
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
                    nextState = EnvelopeState.Releasing;
                }
            }
        }
        public override bool TickNote()
        {
            if (State < EnvelopeState.Releasing)
            {
                if (Note.Duration > 0)
                {
                    Note.Duration--;
                    if (Note.Duration == 0)
                    {
                        if (velocity == 0)
                        {
                            Stop();
                        }
                        else
                        {
                            State = EnvelopeState.Releasing;
                        }
                        return false;
                    }
                    return true;
                }
                else
                {
                    return true;
                }
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
            if (State < EnvelopeState.Releasing)
            {
                if (pan < -0x20)
                {
                    panpot = GBPan.Left;
                }
                else if (pan > 0x20)
                {
                    panpot = GBPan.Right;
                }
                else
                {
                    panpot = GBPan.Center;
                }
                peakVelocity = (byte)((Note.Velocity * vol) >> 10).Clamp(0, 0xF);
                sustainVelocity = (byte)((peakVelocity * adsr.S + 0xF) >> 4).Clamp(0, 0xF);
                if (State == EnvelopeState.Playing)
                {
                    velocity = sustainVelocity;
                }
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
                    nextState = EnvelopeState.Playing;
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
                        nextState = EnvelopeState.Dying;
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
                case EnvelopeState.Initializing:
                    {
                        nextState = EnvelopeState.Rising;
                        processStep = 0;
                        if ((adsr.A | adsr.D) == 0 || (sustainVelocity == 0 && peakVelocity == 0))
                        {
                            State = EnvelopeState.Playing;
                            velocity = sustainVelocity;
                            return;
                        }
                        else if (adsr.A == 0 && adsr.S < 0xF)
                        {
                            State = EnvelopeState.Decaying;
                            velocity = (byte)(peakVelocity - 1).Clamp(0, 0xF);
                            if (velocity < sustainVelocity)
                            {
                                velocity = sustainVelocity;
                            }
                            return;
                        }
                        else if (adsr.A == 0)
                        {
                            State = EnvelopeState.Playing;
                            velocity = sustainVelocity;
                            return;
                        }
                        else
                        {
                            State = EnvelopeState.Rising;
                            velocity = 1;
                            return;
                        }
                    }
                case EnvelopeState.Rising:
                    {
                        if (++processStep >= adsr.A)
                        {
                            if (nextState == EnvelopeState.Decaying)
                            {
                                State = EnvelopeState.Decaying;
                                dec(); return;
                            }
                            if (nextState == EnvelopeState.Playing)
                            {
                                State = EnvelopeState.Playing;
                                sus(); return;
                            }
                            if (nextState == EnvelopeState.Releasing)
                            {
                                State = EnvelopeState.Releasing;
                                rel(); return;
                            }
                            processStep = 0;
                            if (++velocity >= peakVelocity)
                            {
                                if (adsr.D == 0)
                                {
                                    nextState = EnvelopeState.Playing;
                                }
                                else if (peakVelocity == sustainVelocity)
                                {
                                    nextState = EnvelopeState.Playing;
                                    velocity = peakVelocity;
                                }
                                else
                                {
                                    velocity = peakVelocity;
                                    nextState = EnvelopeState.Decaying;
                                }
                            }
                        }
                        break;
                    }
                case EnvelopeState.Decaying:
                    {
                        if (++processStep >= adsr.D)
                        {
                            if (nextState == EnvelopeState.Playing)
                            {
                                State = EnvelopeState.Playing;
                                sus(); return;
                            }
                            if (nextState == EnvelopeState.Releasing)
                            {
                                State = EnvelopeState.Releasing;
                                rel(); return;
                            }
                            dec();
                        }
                        break;
                    }
                case EnvelopeState.Playing:
                    {
                        if (++processStep >= 1)
                        {
                            if (nextState == EnvelopeState.Releasing)
                            {
                                State = EnvelopeState.Releasing;
                                rel(); return;
                            }
                            sus();
                        }
                        break;
                    }
                case EnvelopeState.Releasing:
                    {
                        if (++processStep >= adsr.R)
                        {
                            if (nextState == EnvelopeState.Dying)
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
    }

    class SquareChannel : GBChannel
    {
        float[] pat;

        public SquareChannel(M4AMixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR env, SquarePattern pattern)
        {
            Init(owner, note, env);
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
            frequency = 3520 * (float)Math.Pow(2, ((Note.Key - 69) / 12f) + (pitch / 768f));
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

            ChannelVolume vol = GetVolume();
            float interStep = frequency * mixer.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
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

        public WaveChannel(M4AMixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR env, int sampleOffset)
        {
            Init(owner, note, env);

            sample = Samples.PCM4ToFloat(mixer.Reader, sampleOffset);
        }

        public override void SetPitch(int pitch)
        {
            frequency = 7040 * (float)Math.Pow(2, ((Note.Key - 69) / 12f) + (pitch / 768f));
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

            ChannelVolume vol = GetVolume();
            float interStep = frequency * mixer.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
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

        public NoiseChannel(M4AMixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR env, NoisePattern pattern)
        {
            Init(owner, note, env);
            pat = pattern == NoisePattern.Fine ? Samples.NoiseFine : Samples.NoiseRough;
        }

        public override void SetPitch(int pitch)
        {
            frequency = (0x1000 * (float)Math.Pow(8, ((Note.Key - 60) / 12f) + (pitch / 768f))).Clamp(8, 0x80000); // Thanks ipatix
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

            ChannelVolume vol = GetVolume();
            float interStep = frequency * mixer.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
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