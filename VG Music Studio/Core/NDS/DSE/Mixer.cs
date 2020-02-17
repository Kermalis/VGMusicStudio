using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Mixer : Core.Mixer
    {
        private const int _numChannels = 0x20;
        private readonly float _samplesReciprocal;
        private readonly int _samplesPerBuffer;
        private long _fadeMicroFramesLeft;
        private float _fadePos;
        private float _fadeStepPerMicroframe;

        private readonly Channel[] _channels;
        private readonly BufferedWaveProvider _buffer;

        public Mixer()
        {
            // The sampling frequency of the mixer is 1.04876 MHz with an amplitude resolution of 24 bits, but the sampling frequency after mixing with PWM modulation is 32.768 kHz with an amplitude resolution of 10 bits.
            // - gbatek
            // I'm not using either of those because the samples per buffer leads to an overflow eventually
            const int sampleRate = 65456;
            _samplesPerBuffer = 341; // TODO
            _samplesReciprocal = 1f / _samplesPerBuffer;

            _channels = new Channel[_numChannels];
            for (byte i = 0; i < _numChannels; i++)
            {
                _channels[i] = new Channel(i);
            }

            _buffer = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferLength = _samplesPerBuffer * 64
            };
            Init(_buffer);
        }
        public override void Dispose()
        {
            base.Dispose();
            CloseWaveWriter();
        }

        public Channel AllocateChannel()
        {
            int GetScore(Channel c)
            {
                // Free channels should be used before releasing channels
                return c.Owner == null ? -2 : Utils.IsStateRemovable(c.State) ? -1 : 0;
            }
            Channel nChan = null;
            for (int i = 0; i < _numChannels; i++)
            {
                Channel c = _channels[i];
                if (nChan != null)
                {
                    int nScore = GetScore(nChan);
                    int cScore = GetScore(c);
                    if (cScore <= nScore && (cScore < nScore || c.Volume <= nChan.Volume))
                    {
                        nChan = c;
                    }
                }
                else
                {
                    nChan = c;
                }
            }
            return nChan != null && 0 >= GetScore(nChan) ? nChan : null;
        }

        public void ChannelTick()
        {
            for (int i = 0; i < _numChannels; i++)
            {
                Channel chan = _channels[i];
                if (chan.Owner != null)
                {
                    chan.Volume = (byte)chan.StepEnvelope();
                    if (chan.NoteLength == 0 && !Utils.IsStateRemovable(chan.State))
                    {
                        chan.SetEnvelopePhase7_2074ED8();
                    }
                    int vol = SDAT.Utils.SustainTable[chan.NoteVelocity] + SDAT.Utils.SustainTable[chan.Volume] + SDAT.Utils.SustainTable[chan.Owner.Volume] + SDAT.Utils.SustainTable[chan.Owner.Expression];
                    //int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
                    int pitch = (chan.Key - chan.RootKey) << 6; // "<< 6" is "* 0x40"
                    if (Utils.IsStateRemovable(chan.State) && vol <= -92544)
                    {
                        chan.Stop();
                    }
                    else
                    {
                        chan.Volume = SDAT.Utils.GetChannelVolume(vol);
                        chan.Panpot = chan.Owner.Panpot;
                        chan.Timer = SDAT.Utils.GetChannelTimer(chan.BaseTimer, pitch);
                    }
                }
            }
        }

        public void BeginFadeIn()
        {
            _fadePos = 0f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
            _fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
        }
        public void BeginFadeOut()
        {
            _fadePos = 1f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
            _fadeStepPerMicroframe = -1f / _fadeMicroFramesLeft;
        }
        public bool IsFadeDone()
        {
            return _fadeMicroFramesLeft == 0;
        }
        public void ResetFade()
        {
            _fadeMicroFramesLeft = 0;
        }

        private WaveFileWriter _waveWriter;
        public void CreateWaveWriter(string fileName)
        {
            _waveWriter = new WaveFileWriter(fileName, _buffer.WaveFormat);
        }
        public void CloseWaveWriter()
        {
            _waveWriter?.Dispose();
        }
        public void Process(bool output, bool recording)
        {
            float fromMaster = 1f, toMaster = 1f;
            if (_fadeMicroFramesLeft > 0)
            {
                const float scale = 10f / 6f;
                fromMaster *= (_fadePos < 0f) ? 0f : (float)Math.Pow(_fadePos, scale);
                _fadePos += _fadeStepPerMicroframe;
                toMaster *= (_fadePos < 0f) ? 0f : (float)Math.Pow(_fadePos, scale);
                _fadeMicroFramesLeft--;
            }
            float masterStep = (toMaster - fromMaster) * _samplesReciprocal;
            float masterLevel = fromMaster;
            byte[] b = new byte[4];
            for (int i = 0; i < _samplesPerBuffer; i++)
            {
                int left = 0,
                    right = 0;
                for (int j = 0; j < _numChannels; j++)
                {
                    Channel chan = _channels[j];
                    if (chan.Owner != null)
                    {
                        bool muted = Mutes[chan.Owner.Index]; // Get mute first because chan.Process() can call chan.Stop() which sets chan.Owner to null
                        chan.Process(out short channelLeft, out short channelRight);
                        if (!muted)
                        {
                            left += channelLeft;
                            right += channelRight;
                        }
                    }
                }
                float f = left * masterLevel;
                if (f < short.MinValue)
                {
                    f = short.MinValue;
                }
                else if (f > short.MaxValue)
                {
                    f = short.MaxValue;
                }
                left = (int)f;
                b[0] = (byte)left;
                b[1] = (byte)(left >> 8);
                f = right * masterLevel;
                if (f < short.MinValue)
                {
                    f = short.MinValue;
                }
                else if (f > short.MaxValue)
                {
                    f = short.MaxValue;
                }
                right = (int)f;
                b[2] = (byte)right;
                b[3] = (byte)(right >> 8);
                masterLevel += masterStep;
                if (output)
                {
                    _buffer.AddSamples(b, 0, 4);
                }
                if (recording)
                {
                    _waveWriter.Write(b, 0, 4);
                }
            }
        }
    }
}
