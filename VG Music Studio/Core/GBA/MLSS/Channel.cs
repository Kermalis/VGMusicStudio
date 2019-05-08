using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal abstract class Channel
    {
        protected readonly Mixer mixer;
        public EnvelopeState State;
        public byte Key;
        public bool Stopped;

        protected ADSR adsr;

        protected byte velocity;
        protected int pos;
        protected float interPos;
        protected float frequency;
        protected byte leftVol, rightVol;

        protected Channel(Mixer mixer)
        {
            this.mixer = mixer;
        }

        public ChannelVolume GetVolume()
        {
            const float max = 0x10000;
            return new ChannelVolume
            {
                LeftVol = leftVol * velocity / max,
                RightVol = rightVol * velocity / max
            };
        }
        public void SetVolume(byte vol, sbyte pan)
        {
            leftVol = (byte)((vol * (-pan + 0x80)) >> 8);
            rightVol = (byte)((vol * (pan + 0x80)) >> 8);
        }
        public abstract void SetPitch(int pitch);

        public abstract void Process(float[] buffer);
    }
    internal class PCMChannel : Channel
    {
        private SampleHeader sampleHeader;
        private int sampleOffset;
        private bool bFixed;

        public PCMChannel(Mixer mixer) : base(mixer) { }
        public void Init(byte key, ADSR adsr, int sampleOffset, bool bFixed)
        {
            velocity = adsr.A;
            State = EnvelopeState.Attack;
            pos = 0; interPos = 0;
            Key = key;
            this.adsr = adsr;
            sampleHeader = mixer.Config.Reader.ReadObject<SampleHeader>(sampleOffset);
            this.sampleOffset = sampleOffset + 0x10;
            this.bFixed = bFixed;
            Stopped = false;
        }

        public override void SetPitch(int pitch)
        {
            if (sampleHeader != null)
            {
                frequency = (sampleHeader.SampleRate >> 10) * (float)Math.Pow(2, ((Key - 60) / 12f) + (pitch / 768f));
            }
        }

        private void StepEnvelope()
        {
            switch (State)
            {
                case EnvelopeState.Attack:
                {
                    int nextVel = velocity + adsr.A;
                    if (nextVel >= 0xFF)
                    {
                        State = EnvelopeState.Decay;
                        velocity = 0xFF;
                    }
                    else
                    {
                        velocity = (byte)nextVel;
                    }
                    break;
                }
                case EnvelopeState.Decay:
                {
                    int nextVel = (velocity * adsr.D) >> 8;
                    if (nextVel <= adsr.S)
                    {
                        State = EnvelopeState.Sustain;
                        velocity = adsr.S;
                    }
                    else
                    {
                        velocity = (byte)nextVel;
                    }
                    break;
                }
                case EnvelopeState.Release:
                {
                    int nextVel = (velocity * adsr.R) >> 8;
                    velocity = (byte)Math.Max(nextVel, 0);
                    break;
                }
            }
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();

            ChannelVolume vol = GetVolume();
            float interStep = (bFixed ? sampleHeader.SampleRate >> 10 : frequency) * mixer.SampleRateReciprocal;
            int bufPos = 0; int samplesPerBuffer = mixer.SamplesPerBuffer;
            do
            {
                float samp = (mixer.Config.ROM[pos + sampleOffset] - 0x80) / (float)0x80;

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
                        Stopped = true;
                        break;
                    }
                }
            } while (--samplesPerBuffer > 0);
        }
    }
    internal class SquareChannel : Channel
    {
        private float[] pat;

        public SquareChannel(Mixer mixer) : base(mixer) { }
        public void Init(byte key, ADSR env, byte vol, sbyte pan, int pitch)
        {
            pat = MP2K.Utils.SquareD50; // TODO
            Key = key;
            adsr = env;
            SetVolume(vol, pan);
            SetPitch(pitch);
            State = EnvelopeState.Attack;
        }

        public override void SetPitch(int pitch)
        {
            frequency = 3520 * (float)Math.Pow(2, ((Key - 69) / 12f) + (pitch / 768f));
        }

        private void StepEnvelope()
        {
            switch (State)
            {
                case EnvelopeState.Attack:
                {
                    int nextVel = velocity + adsr.A;
                    if (nextVel >= 0xF)
                    {
                        State = EnvelopeState.Decay;
                        velocity = 0xF;
                    }
                    else
                    {
                        velocity = (byte)nextVel;
                    }
                    break;
                }
                case EnvelopeState.Decay:
                {
                    int nextVel = (velocity * adsr.D) >> 3;
                    if (nextVel <= adsr.S)
                    {
                        State = EnvelopeState.Sustain;
                        velocity = adsr.S;
                    }
                    else
                    {
                        velocity = (byte)nextVel;
                    }
                    break;
                }
                case EnvelopeState.Release:
                {
                    int nextVel = (velocity * adsr.R) >> 3;
                    velocity = (byte)Math.Max(nextVel, 0);
                    break;
                }
            }
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();

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
}
