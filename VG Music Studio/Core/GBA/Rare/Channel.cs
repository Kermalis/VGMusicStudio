using System;

namespace Kermalis.VGMusicStudio.Core.GBA.Rare
{
    internal abstract class Channel
    {
        protected readonly Mixer _mixer;
        public EnvelopeState State;
        public byte Key;
        public bool Stopped;

        protected ADSR _adsr;

        protected byte _velocity;
        protected int _pos;
        protected float _interPos;
        protected float _frequency;
        protected byte _leftVol;
        protected byte _rightVol;

        protected Channel(Mixer mixer)
        {
            _mixer = mixer;
        }

        public ChannelVolume GetVolume()
        {
            const float max = 0x10000;
            return new ChannelVolume
            {
                LeftVol = _leftVol * _velocity / max,
                RightVol = _rightVol * _velocity / max
            };
        }
        public void SetVolume(byte vol, sbyte pan)
        {
            _leftVol = (byte)((vol * (-pan + 0x80)) >> 8);
            _rightVol = (byte)((vol * (pan + 0x80)) >> 8);
        }
        public abstract void SetPitch(int pitch);

        public abstract void Process(float[] buffer);
    }
    internal class SquareChannel : Channel
    {
        private float[] _pat;

        public SquareChannel(Mixer mixer) : base(mixer) { }
        public void Init(byte key, ADSR env, byte vol, sbyte pan, int pitch)
        {
            _pat = MP2K.Utils.SquareD50; // TODO
            Key = key;
            _adsr = env;
            SetVolume(vol, pan);
            SetPitch(pitch);
            State = EnvelopeState.Attack;
        }

        public override void SetPitch(int pitch)
        {
            _frequency = 3520 * (float)Math.Pow(2, ((Key - 69) / 12f) + (pitch / 768f));
        }

        private void StepEnvelope()
        {
            switch (State)
            {
                case EnvelopeState.Attack:
                {
                    int next = _velocity + _adsr.A;
                    if (next >= 0xF)
                    {
                        State = EnvelopeState.Decay;
                        _velocity = 0xF;
                    }
                    else
                    {
                        _velocity = (byte)next;
                    }
                    break;
                }
                case EnvelopeState.Decay:
                {
                    int next = (_velocity * _adsr.D) >> 3;
                    if (next <= _adsr.S)
                    {
                        State = EnvelopeState.Sustain;
                        _velocity = _adsr.S;
                    }
                    else
                    {
                        _velocity = (byte)next;
                    }
                    break;
                }
                case EnvelopeState.Release:
                {
                    int next = (_velocity * _adsr.R) >> 3;
                    if (next < 0)
                    {
                        next = 0;
                    }
                    _velocity = (byte)next;
                    break;
                }
            }
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();

            ChannelVolume vol = GetVolume();
            float interStep = _frequency * _mixer.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
            do
            {
                float samp = _pat[_pos];

                buffer[bufPos++] += samp * vol.LeftVol;
                buffer[bufPos++] += samp * vol.RightVol;

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _pos = (_pos + posDelta) & 0x7;
            } while (--samplesPerBuffer > 0);
        }
    }
}
