using NAudio.Wave;
using System;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class Mixer : Core.Mixer
    {
        public readonly int SampleRate;
        public readonly int SamplesPerBuffer;
        public readonly float SampleRateReciprocal;
        private readonly float _samplesReciprocal;
        public readonly float PCM8MasterVolume;
        private bool _isFading;
        private long _fadeMicroFramesLeft;
        private float _fadePos;
        private float _fadeStepPerMicroframe;

        public readonly Config Config;
        private readonly WaveBuffer _audio;
        private readonly float[][] _trackBuffers;
        private readonly PCM8Channel[] _pcm8Channels;
        private readonly SquareChannel _sq1;
        private readonly SquareChannel _sq2;
        private readonly PCM4Channel _pcm4;
        private readonly NoiseChannel _noise;
        private readonly PSGChannel[] _psgChannels;
        private readonly BufferedWaveProvider _buffer;

        public Mixer(Config config)
        {
            Config = config;
            (SampleRate, SamplesPerBuffer) = Utils.FrequencyTable[config.SampleRate];
            SampleRateReciprocal = 1f / SampleRate;
            _samplesReciprocal = 1f / SamplesPerBuffer;
            PCM8MasterVolume = config.Volume / 15f;

            _pcm8Channels = new PCM8Channel[24];
            for (int i = 0; i < _pcm8Channels.Length; i++)
            {
                _pcm8Channels[i] = new PCM8Channel(this);
            }
            _psgChannels = new PSGChannel[] { _sq1 = new SquareChannel(this), _sq2 = new SquareChannel(this), _pcm4 = new PCM4Channel(this), _noise = new NoiseChannel(this) };

            int amt = SamplesPerBuffer * 2;
            _audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
            _trackBuffers = new float[0x10][];
            for (int i = 0; i < _trackBuffers.Length; i++)
            {
                _trackBuffers[i] = new float[amt];
            }
            _buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferLength = SamplesPerBuffer * 64
            };
            Init(_buffer);
        }
        public override void Dispose()
        {
            base.Dispose();
            CloseWaveWriter();
        }

        public PCM8Channel AllocPCM8Channel(Track owner, ADSR env, Note note, byte vol, sbyte pan, int instPan, int pitch, bool bFixed, bool bCompressed, int sampleOffset)
        {
            PCM8Channel nChn = null;
            IOrderedEnumerable<PCM8Channel> byOwner = _pcm8Channels.OrderByDescending(c => c.Owner == null ? 0xFF : c.Owner.Index);
            foreach (PCM8Channel i in byOwner) // Find free
            {
                if (i.State == EnvelopeState.Dead || i.Owner == null)
                {
                    nChn = i;
                    break;
                }
            }
            if (nChn == null) // Find releasing
            {
                foreach (PCM8Channel i in byOwner)
                {
                    if (i.State == EnvelopeState.Releasing)
                    {
                        nChn = i;
                        break;
                    }
                }
            }
            if (nChn == null) // Find prioritized
            {
                foreach (PCM8Channel i in byOwner)
                {
                    if (owner.Priority > i.Owner.Priority)
                    {
                        nChn = i;
                        break;
                    }
                }
            }
            if (nChn == null) // None available
            {
                PCM8Channel lowest = byOwner.First(); // Kill lowest track's instrument if the track is lower than this one
                if (lowest.Owner.Index >= owner.Index)
                {
                    nChn = lowest;
                }
            }
            if (nChn != null) // Could still be null from the above if
            {
                nChn.Init(owner, note, env, sampleOffset, vol, pan, instPan, pitch, bFixed, bCompressed);
            }
            return nChn;
        }
        public PSGChannel AllocPSGChannel(Track owner, ADSR env, Note note, byte vol, sbyte pan, int instPan, int pitch, VoiceType type, object arg)
        {
            PSGChannel nChn;
            switch (type)
            {
                case VoiceType.Square1:
                {
                    nChn = _sq1;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    _sq1.Init(owner, note, env, instPan, (SquarePattern)arg);
                    break;
                }
                case VoiceType.Square2:
                {
                    nChn = _sq2;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    _sq2.Init(owner, note, env, instPan, (SquarePattern)arg);
                    break;
                }
                case VoiceType.PCM4:
                {
                    nChn = _pcm4;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    _pcm4.Init(owner, note, env, instPan, (int)arg);
                    break;
                }
                case VoiceType.Noise:
                {
                    nChn = _noise;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    _noise.Init(owner, note, env, instPan, (NoisePattern)arg);
                    break;
                }
                default: return null;
            }
            nChn.SetVolume(vol, pan);
            nChn.SetPitch(pitch);
            return nChn;
        }

        public void BeginFadeIn()
        {
            _fadePos = 0f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * GBA.Utils.AGB_FPS);
            _fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
            _isFading = true;
        }
        public void BeginFadeOut()
        {
            _fadePos = 1f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * GBA.Utils.AGB_FPS);
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
            for (int i = 0; i < _trackBuffers.Length; i++)
            {
                float[] buf = _trackBuffers[i];
                Array.Clear(buf, 0, buf.Length);
            }
            _audio.Clear();

            for (int i = 0; i < _pcm8Channels.Length; i++)
            {
                PCM8Channel c = _pcm8Channels[i];
                if (c.Owner != null)
                {
                    c.Process(_trackBuffers[c.Owner.Index]);
                }
            }

            for (int i = 0; i < _psgChannels.Length; i++)
            {
                PSGChannel c = _psgChannels[i];
                if (c.Owner != null)
                {
                    c.Process(_trackBuffers[c.Owner.Index]);
                }
            }

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
            for (int i = 0; i < _trackBuffers.Length; i++)
            {
                if (!Mutes[i])
                {
                    float level = masterLevel;
                    float[] buf = _trackBuffers[i];
                    for (int j = 0; j < SamplesPerBuffer; j++)
                    {
                        _audio.FloatBuffer[j * 2] += buf[j * 2] * level;
                        _audio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1] * level;
                        level += masterStep;
                    }
                }
            }
            if (output)
            {
                _buffer.AddSamples(_audio.ByteBuffer, 0, _audio.ByteBufferCount);
            }
            if (recording)
            {
                _waveWriter.Write(_audio.ByteBuffer, 0, _audio.ByteBufferCount);
            }
        }
    }
}
