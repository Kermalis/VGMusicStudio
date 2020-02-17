using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream
{
    internal class Mixer : Core.Mixer
    {
        public readonly float SampleRateReciprocal;
        private readonly float _samplesReciprocal;
        public readonly int SamplesPerBuffer;
        private bool _isFading;
        private long _fadeMicroFramesLeft;
        private float _fadePos;
        private float _fadeStepPerMicroframe;

        public readonly Config Config;
        private readonly WaveBuffer _audio;
        private readonly float[][] _trackBuffers = new float[0x10][];
        private readonly BufferedWaveProvider _buffer;

        public Mixer(Config config)
        {
            Config = config;
            const int sampleRate = 13379; // TODO: Actual value unknown
            SamplesPerBuffer = 224; // TODO
            SampleRateReciprocal = 1f / sampleRate;
            _samplesReciprocal = 1f / SamplesPerBuffer;

            int amt = SamplesPerBuffer * 2;
            _audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
            for (int i = 0; i < 0x10; i++)
            {
                _trackBuffers[i] = new float[amt];
            }
            _buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2)) // TODO
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

        public void BeginFadeIn()
        {
            _fadePos = 0f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * Utils.AGB_FPS);
            _fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
            _isFading = true;
        }
        public void BeginFadeOut()
        {
            _fadePos = 1f;
            _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * Utils.AGB_FPS);
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
        public void Process(Track[] tracks, bool output, bool recording)
        {
            _audio.Clear();
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
            for (int i = 0; i < 0x10; i++)
            {
                Track track = tracks[i];
                if (track.Enabled && track.NoteDuration != 0 && !track.Channel.Stopped && !Mutes[i])
                {
                    float level = masterLevel;
                    float[] buf = _trackBuffers[i];
                    Array.Clear(buf, 0, buf.Length);
                    track.Channel.Process(buf);
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
