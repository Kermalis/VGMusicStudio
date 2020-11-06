using System;
using System.Collections;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal abstract class Channel
    {
        public EnvelopeState State = EnvelopeState.Dead;
        public Track Owner;
        protected readonly Mixer _mixer;

        public Note Note; // Must be a struct & field
        protected ADSR _adsr;

        protected byte _velocity;
        protected int _pos;
        protected float _interPos;
        protected float _frequency;

        protected Channel(Mixer mixer)
        {
            _mixer = mixer;
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
    internal class PCM8Channel : Channel
    {
        private SampleHeader _sampleHeader;
        private int _sampleOffset;
        private GoldenSunPSG _gsPSG;
        private bool _bFixed;
        private bool _bGoldenSun;
        private bool _bCompressed;
        private byte _leftVol;
        private byte _rightVol;
        private sbyte[] _decompressedSample;

        public PCM8Channel(Mixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR adsr, int sampleOffset, byte vol, sbyte pan, int pitch, bool bFixed, bool bCompressed)
        {
            State = EnvelopeState.Initializing;
            _pos = 0; _interPos = 0;
            if (Owner != null)
            {
                Owner.Channels.Remove(this);
            }
            Owner = owner;
            Owner.Channels.Add(this);
            Note = note;
            _adsr = adsr;
            _sampleHeader = _mixer.Config.Reader.ReadObject<SampleHeader>(sampleOffset);
            _sampleOffset = sampleOffset + 0x10;
            _bFixed = bFixed;
            _bCompressed = bCompressed;
            _decompressedSample = bCompressed ? Utils.Decompress(_sampleOffset, _sampleHeader.Length) : null;
            _bGoldenSun = _mixer.Config.HasGoldenSunSynths && _sampleHeader.DoesLoop == 0x40000000 && _sampleHeader.LoopOffset == 0 && _sampleHeader.Length == 0;
            if (_bGoldenSun)
            {
                _gsPSG = _mixer.Config.Reader.ReadObject<GoldenSunPSG>(_sampleOffset);
            }
            SetVolume(vol, pan);
            SetPitch(pitch);
        }

        public override ChannelVolume GetVolume()
        {
            const float max = 0x10000;
            return new ChannelVolume
            {
                LeftVol = _leftVol * _velocity / max * _mixer.PCM8MasterVolume,
                RightVol = _rightVol * _velocity / max * _mixer.PCM8MasterVolume
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            const int fix = 0x2000;
            if (State < EnvelopeState.Releasing)
            {
                int a = Note.Velocity * vol;
                _leftVol = (byte)(a * (-pan + 0x40) / fix);
                _rightVol = (byte)(a * (pan + 0x40) / fix);
            }
        }
        public override void SetPitch(int pitch)
        {
            _frequency = (_sampleHeader.SampleRate >> 10) * (float)Math.Pow(2, ((Note.Key - 60) / 12f) + (pitch / 768f));
        }

        private void StepEnvelope()
        {
            switch (State)
            {
                case EnvelopeState.Initializing:
                {
                    _velocity = _adsr.A;
                    State = EnvelopeState.Rising;
                    break;
                }
                case EnvelopeState.Rising:
                {
                    int nextVel = _velocity + _adsr.A;
                    if (nextVel >= 0xFF)
                    {
                        State = EnvelopeState.Decaying;
                        _velocity = 0xFF;
                    }
                    else
                    {
                        _velocity = (byte)nextVel;
                    }
                    break;
                }
                case EnvelopeState.Decaying:
                {
                    int nextVel = (_velocity * _adsr.D) >> 8;
                    if (nextVel <= _adsr.S)
                    {
                        State = EnvelopeState.Playing;
                        _velocity = _adsr.S;
                    }
                    else
                    {
                        _velocity = (byte)nextVel;
                    }
                    break;
                }
                case EnvelopeState.Playing:
                {
                    break;
                }
                case EnvelopeState.Releasing:
                {
                    int nextVel = (_velocity * _adsr.R) >> 8;
                    if (nextVel <= 0)
                    {
                        State = EnvelopeState.Dying;
                        _velocity = 0;
                    }
                    else
                    {
                        _velocity = (byte)nextVel;
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
            float interStep = _bFixed && !_bGoldenSun ? _mixer.SampleRate * _mixer.SampleRateReciprocal : _frequency * _mixer.SampleRateReciprocal;
            if (_bGoldenSun) // Most Golden Sun processing is thanks to ipatix
            {
                interStep /= 0x40;
                switch (_gsPSG.Type)
                {
                    case GoldenSunPSGType.Square:
                    {
                        _pos += _gsPSG.CycleSpeed << 24;
                        int iThreshold = (_gsPSG.MinimumCycle << 24) + _pos;
                        iThreshold = (iThreshold < 0 ? ~iThreshold : iThreshold) >> 8;
                        iThreshold = (iThreshold * _gsPSG.CycleAmplitude) + (_gsPSG.InitialCycle << 24);
                        float threshold = iThreshold / (float)0x100000000;

                        int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
                        do
                        {
                            float samp = _interPos < threshold ? 0.5f : -0.5f;
                            samp += 0.5f - threshold;
                            buffer[bufPos++] += samp * vol.LeftVol;
                            buffer[bufPos++] += samp * vol.RightVol;

                            _interPos += interStep;
                            if (_interPos >= 1)
                            {
                                _interPos--;
                            }
                        } while (--samplesPerBuffer > 0);
                        break;
                    }
                    case GoldenSunPSGType.Saw:
                    {
                        const int fix = 0x70;

                        int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
                        do
                        {
                            _interPos += interStep;
                            if (_interPos >= 1)
                            {
                                _interPos--;
                            }
                            int var1 = (int)(_interPos * 0x100) - fix;
                            int var2 = (int)(_interPos * 0x10000) << 17;
                            int var3 = var1 - (var2 >> 27);
                            _pos = var3 + (_pos >> 1);

                            float samp = _pos / (float)0x100;

                            buffer[bufPos++] += samp * vol.LeftVol;
                            buffer[bufPos++] += samp * vol.RightVol;
                        } while (--samplesPerBuffer > 0);
                        break;
                    }
                    case GoldenSunPSGType.Triangle:
                    {
                        int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
                        do
                        {
                            _interPos += interStep;
                            if (_interPos >= 1)
                            {
                                _interPos--;
                            }
                            float samp = _interPos < 0.5f ? (_interPos * 4) - 1 : 3 - (_interPos * 4);

                            buffer[bufPos++] += samp * vol.LeftVol;
                            buffer[bufPos++] += samp * vol.RightVol;
                        } while (--samplesPerBuffer > 0);
                        break;
                    }
                }
            }
            else if (_bCompressed)
            {
                int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
                do
                {
                    float samp = _decompressedSample[_pos] / (float)0x80;

                    buffer[bufPos++] += samp * vol.LeftVol;
                    buffer[bufPos++] += samp * vol.RightVol;

                    _interPos += interStep;
                    int posDelta = (int)_interPos;
                    _interPos -= posDelta;
                    _pos += posDelta;
                    if (_pos >= _decompressedSample.Length)
                    {
                        Stop();
                        break;
                    }
                } while (--samplesPerBuffer > 0);
            }
            else
            {
                int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
                do
                {
                    float samp = (sbyte)_mixer.Config.ROM[_pos + _sampleOffset] / (float)0x80;

                    buffer[bufPos++] += samp * vol.LeftVol;
                    buffer[bufPos++] += samp * vol.RightVol;

                    _interPos += interStep;
                    int posDelta = (int)_interPos;
                    _interPos -= posDelta;
                    _pos += posDelta;
                    if (_pos >= _sampleHeader.Length)
                    {
                        if (_sampleHeader.DoesLoop == 0x40000000)
                        {
                            _pos = _sampleHeader.LoopOffset;
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
    internal abstract class PSGChannel : Channel
    {
        protected enum GBPan : byte
        {
            Left,
            Center,
            Right
        }

        private byte _processStep;
        private EnvelopeState _nextState;
        private byte _peakVelocity;
        private byte _sustainVelocity;
        protected GBPan _panpot = GBPan.Center;

        public PSGChannel(Mixer mixer) : base(mixer) { }
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
            _adsr.A = (byte)(env.A & 0x7);
            _adsr.D = (byte)(env.D & 0x7);
            _adsr.S = (byte)(env.S & 0xF);
            _adsr.R = (byte)(env.R & 0x7);
        }

        public override void Release()
        {
            if (State < EnvelopeState.Releasing)
            {
                if (_adsr.R == 0)
                {
                    _velocity = 0;
                    Stop();
                }
                else if (_velocity == 0)
                {
                    Stop();
                }
                else
                {
                    _nextState = EnvelopeState.Releasing;
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
                        if (_velocity == 0)
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
                LeftVol = _panpot == GBPan.Right ? 0 : _velocity / max,
                RightVol = _panpot == GBPan.Left ? 0 : _velocity / max
            };
        }
        public override void SetVolume(byte vol, sbyte pan)
        {
            if (State < EnvelopeState.Releasing)
            {
                _panpot = pan < -21 ? GBPan.Left : pan > 20 ? GBPan.Right : GBPan.Center;
                _peakVelocity = (byte)((Note.Velocity * vol) >> 10);
                _sustainVelocity = (byte)(((_peakVelocity * _adsr.S) + 0xF) >> 4); // TODO
                if (State == EnvelopeState.Playing)
                {
                    _velocity = _sustainVelocity;
                }
            }
        }

        protected void StepEnvelope()
        {
            void dec()
            {
                _processStep = 0;
                if (_velocity - 1 <= _sustainVelocity)
                {
                    _velocity = _sustainVelocity;
                    _nextState = EnvelopeState.Playing;
                }
                else if (_velocity != 0)
                {
                    _velocity--;
                }
            }
            void sus()
            {
                _processStep = 0;
            }
            void rel()
            {
                if (_adsr.R == 0)
                {
                    _velocity = 0;
                    Stop();
                }
                else
                {
                    _processStep = 0;
                    if (_velocity - 1 <= 0)
                    {
                        _nextState = EnvelopeState.Dying;
                        _velocity = 0;
                    }
                    else
                    {
                        _velocity--;
                    }
                }
            }

            switch (State)
            {
                case EnvelopeState.Initializing:
                {
                    _nextState = EnvelopeState.Rising;
                    _processStep = 0;
                    if ((_adsr.A | _adsr.D) == 0 || (_sustainVelocity == 0 && _peakVelocity == 0))
                    {
                        State = EnvelopeState.Playing;
                        _velocity = _sustainVelocity;
                        return;
                    }
                    else if (_adsr.A == 0 && _adsr.S < 0xF)
                    {
                        State = EnvelopeState.Decaying;
                        int next = _peakVelocity - 1;
                        if (next < 0)
                        {
                            next = 0;
                        }
                        _velocity = (byte)next;
                        if (_velocity < _sustainVelocity)
                        {
                            _velocity = _sustainVelocity;
                        }
                        return;
                    }
                    else if (_adsr.A == 0)
                    {
                        State = EnvelopeState.Playing;
                        _velocity = _sustainVelocity;
                        return;
                    }
                    else
                    {
                        State = EnvelopeState.Rising;
                        _velocity = 1;
                        return;
                    }
                }
                case EnvelopeState.Rising:
                {
                    if (++_processStep >= _adsr.A)
                    {
                        if (_nextState == EnvelopeState.Decaying)
                        {
                            State = EnvelopeState.Decaying;
                            dec(); return;
                        }
                        if (_nextState == EnvelopeState.Playing)
                        {
                            State = EnvelopeState.Playing;
                            sus(); return;
                        }
                        if (_nextState == EnvelopeState.Releasing)
                        {
                            State = EnvelopeState.Releasing;
                            rel(); return;
                        }
                        _processStep = 0;
                        if (++_velocity >= _peakVelocity)
                        {
                            if (_adsr.D == 0)
                            {
                                _nextState = EnvelopeState.Playing;
                            }
                            else if (_peakVelocity == _sustainVelocity)
                            {
                                _nextState = EnvelopeState.Playing;
                                _velocity = _peakVelocity;
                            }
                            else
                            {
                                _velocity = _peakVelocity;
                                _nextState = EnvelopeState.Decaying;
                            }
                        }
                    }
                    break;
                }
                case EnvelopeState.Decaying:
                {
                    if (++_processStep >= _adsr.D)
                    {
                        if (_nextState == EnvelopeState.Playing)
                        {
                            State = EnvelopeState.Playing;
                            sus(); return;
                        }
                        if (_nextState == EnvelopeState.Releasing)
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
                    if (++_processStep >= 1)
                    {
                        if (_nextState == EnvelopeState.Releasing)
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
                    if (++_processStep >= _adsr.R)
                    {
                        if (_nextState == EnvelopeState.Dying)
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
    internal class SquareChannel : PSGChannel
    {
        private float[] _pat;

        public SquareChannel(Mixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR env, SquarePattern pattern)
        {
            Init(owner, note, env);
            switch (pattern)
            {
                default: _pat = Utils.SquareD12; break;
                case SquarePattern.D25: _pat = Utils.SquareD25; break;
                case SquarePattern.D50: _pat = Utils.SquareD50; break;
                case SquarePattern.D75: _pat = Utils.SquareD75; break;
            }
        }

        public override void SetPitch(int pitch)
        {
            _frequency = 3520 * (float)Math.Pow(2, ((Note.Key - 69) / 12f) + (pitch / 768f));
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

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
    internal class PCM4Channel : PSGChannel
    {
        private float[] _sample;

        public PCM4Channel(Mixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR env, int sampleOffset)
        {
            Init(owner, note, env);
            _sample = Utils.PCM4ToFloat(sampleOffset);
        }

        public override void SetPitch(int pitch)
        {
            _frequency = 7040 * (float)Math.Pow(2, ((Note.Key - 69) / 12f) + (pitch / 768f));
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

            ChannelVolume vol = GetVolume();
            float interStep = _frequency * _mixer.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
            do
            {
                float samp = _sample[_pos];

                buffer[bufPos++] += samp * vol.LeftVol;
                buffer[bufPos++] += samp * vol.RightVol;

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _pos = (_pos + posDelta) & 0x1F;
            } while (--samplesPerBuffer > 0);
        }
    }
    internal class NoiseChannel : PSGChannel
    {
        private BitArray _pat;

        public NoiseChannel(Mixer mixer) : base(mixer) { }
        public void Init(Track owner, Note note, ADSR env, NoisePattern pattern)
        {
            Init(owner, note, env);
            _pat = pattern == NoisePattern.Fine ? Utils.NoiseFine : Utils.NoiseRough;
        }

        public override void SetPitch(int pitch)
        {
            int key = Note.Key + (int)Math.Round(pitch / 64f);
            if (key <= 20)
            {
                key = 0;
            }
            else
            {
                key -= 21;
                if (key > 59)
                {
                    key = 59;
                }
            }
            byte v = Utils.NoiseFrequencyTable[key];
            // The following emulates 0x0400007C - SOUND4CNT_H
            int r = v & 7; // Bits 0-2
            int s = v >> 4; // Bits 4-7
            _frequency = 524288f / (r == 0 ? 0.5f : r) / (float)Math.Pow(2, s + 1);
        }

        public override void Process(float[] buffer)
        {
            StepEnvelope();
            if (State == EnvelopeState.Dead)
            {
                return;
            }

            ChannelVolume vol = GetVolume();
            float interStep = _frequency * _mixer.SampleRateReciprocal;

            int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
            do
            {
                float samp = _pat[_pos & (_pat.Length - 1)] ? 0.5f : -0.5f;

                buffer[bufPos++] += samp * vol.LeftVol;
                buffer[bufPos++] += samp * vol.RightVol;

                _interPos += interStep;
                int posDelta = (int)_interPos;
                _interPos -= posDelta;
                _pos = (_pos + posDelta) & (_pat.Length - 1);
            } while (--samplesPerBuffer > 0);
        }
    }
}