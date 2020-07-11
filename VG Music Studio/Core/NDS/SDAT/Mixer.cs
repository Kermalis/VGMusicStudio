using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
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
            // The sampling frequency of the mixer is 1.04876 MHz with an amplitude resolution of 24 bits, but the sampling frequency after mixing with PWM modulation is 32.768 kHz with an amplitude resolution of 10 bits.
            // - gbatek
            // I'm not using either of those because the samples per buffer leads to an overflow eventually
            const int sampleRate = 65456;
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

        private static readonly int[] _pcmChanOrder = new int[] { 4, 5, 6, 7, 2, 0, 3, 1, 8, 9, 10, 11, 14, 12, 15, 13 };
        private static readonly int[] _psgChanOrder = new int[] { 8, 9, 10, 11, 12, 13 };
        private static readonly int[] _noiseChanOrder = new int[] { 14, 15 };
        public Channel AllocateChannel(InstrumentType type, Track track)
        {
            int[] allowedChannels;
            switch (type)
            {
                case InstrumentType.PCM: allowedChannels = _pcmChanOrder; break;
                case InstrumentType.PSG: allowedChannels = _psgChanOrder; break;
                case InstrumentType.Noise: allowedChannels = _noiseChanOrder; break;
                default: return null;
            }
            Channel nChan = null;
            for (int i = 0; i < allowedChannels.Length; i++)
            {
                Channel c = Channels[allowedChannels[i]];
                if (nChan != null && c.Priority >= nChan.Priority && (c.Priority != nChan.Priority || nChan.Volume <= c.Volume))
                {
                    continue;
                }
                nChan = c;
            }
            if (nChan == null || track.Priority < nChan.Priority)
            {
                return null;
            }
            return nChan;
        }

        public void ChannelTick()
        {
            for (int i = 0; i < 0x10; i++)
            {
                Channel chan = Channels[i];
                if (chan.Owner != null)
                {
                    chan.StepEnvelope();
                    if (chan.NoteDuration == 0 && !chan.Owner.WaitingForNoteToFinishBeforeContinuingXD)
                    {
                        chan.Priority = 1;
                        chan.State = EnvelopeState.Release;
                    }
                    int vol = Utils.SustainTable[chan.NoteVelocity] + chan.Velocity + chan.Owner.GetVolume();
                    int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
                    int pan = 0;
                    chan.LFOTick();
                    switch (chan.LFOType)
                    {
                        case LFOType.Pitch: pitch += chan.LFOParam; break;
                        case LFOType.Volume: vol += chan.LFOParam; break;
                        case LFOType.Panpot: pan += chan.LFOParam; break;
                    }
                    if (chan.State == EnvelopeState.Release && vol <= -92544)
                    {
                        chan.Stop();
                    }
                    else
                    {
                        chan.Volume = Utils.GetChannelVolume(vol);
                        chan.Timer = Utils.GetChannelTimer(chan.BaseTimer, pitch);
                        int p = chan.StartingPan + chan.Owner.GetPan() + pan;
                        if (p < -0x40)
                        {
                            p = -0x40;
                        }
                        else if (p > 0x3F)
                        {
                            p = 0x3F;
                        }
                        chan.Pan = (sbyte)p;
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
        public void EmulateProcess()
        {
            for (int i = 0; i < _samplesPerBuffer; i++)
            {
                for (int j = 0; j < 0x10; j++)
                {
                    Channel chan = Channels[j];
                    if (chan.Owner != null)
                    {
                        chan.EmulateProcess();
                    }
                }
            }
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
