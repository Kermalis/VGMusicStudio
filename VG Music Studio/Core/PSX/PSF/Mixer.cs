using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Mixer : Core.Mixer
    {
        private readonly float _samplesReciprocal;
        private readonly int _samplesPerBuffer;
        private bool _isFading;
        private long _fadeMicroFramesLeft;
        private float _fadePos;
        private float _fadeStepPerMicroframe;

        public Channel[] Channels;
        private readonly BufferedWaveProvider _buffer;

        public Mixer()
        {
            const int sampleRate = 65456; // TODO
            _samplesPerBuffer = 341; // TODO
            _samplesReciprocal = 1f / _samplesPerBuffer;

            Channels = new Channel[0x10];
            for (byte i = 0; i < 0x10; i++)
            {
                Channels[i] = new Channel(i);
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
                return c.Owner == null ? -2 : false ? -1 : 0;
                //return c.Owner == null ? -2 : c.State == EnvelopeState.Release ? -1 : 0;
            }
            Channel nChan = null;
            for (int i = 0; i < 0x10; i++)
            {
                Channel c = Channels[i];
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
            for (int i = 0; i < 0x10; i++)
            {
                Channel chan = Channels[i];
                if (chan.Owner != null)
                {
                    //chan.StepEnvelope();
                    int vol = NDS.SDAT.Utils.SustainTable[chan.NoteVelocity] + 0;
                    int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.PitchTune; // "<< 6" is "* 0x40"
                    if (/*chan.State == EnvelopeState.Release && */vol <= -92544)
                    {
                        chan.Stop();
                    }
                    else
                    {
                        chan.Volume = NDS.SDAT.Utils.GetChannelVolume(vol);
                        chan.Timer = NDS.SDAT.Utils.GetChannelTimer(chan.BaseTimer, pitch);
                        chan.Pan = 0;
                    }
                }
            }
        }

        public void BeginFadeIn()
        {
            _fadePos = 0f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
            _fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
            _isFading = true;
        }
        public void BeginFadeOut()
        {
            _fadePos = 1f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
            _fadeStepPerMicroframe = -1f / _fadeMicroFramesLeft;
            _isFading = true;
        }
        public bool IsFading()
        {
            return _isFading;
        }
        public bool IsFadeDone()
        {
            return _isFading && _fadeMicroFramesLeft == 0;
        }
        public void ResetFade()
        {
            _isFading = false;
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
            float masterStep;
            float masterLevel;
            if (_isFading && _fadeMicroFramesLeft == 0)
            {
                masterStep = 0;
                masterLevel = 0;
            }
            else
            {
                float fromMaster = 1f;
                float toMaster = 1f;
                if (_fadeMicroFramesLeft > 0)
                {
                    const float scale = 10f / 6f;
                    fromMaster *= (_fadePos < 0f) ? 0f : (float)Math.Pow(_fadePos, scale);
                    _fadePos += _fadeStepPerMicroframe;
                    toMaster *= (_fadePos < 0f) ? 0f : (float)Math.Pow(_fadePos, scale);
                    _fadeMicroFramesLeft--;
                }
                masterStep = (toMaster - fromMaster) * _samplesReciprocal;
                masterLevel = fromMaster;
            }
            byte[] b = new byte[4];
            for (int i = 0; i < _samplesPerBuffer; i++)
            {
                int left = 0,
                    right = 0;
                for (int j = 0; j < 0x10; j++)
                {
                    Channel chan = Channels[j];
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
