using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class Mixer : Core.Mixer
    {
        public readonly float SampleRateReciprocal;
        private readonly float samplesReciprocal;
        public readonly int SamplesPerBuffer;
        private long fadeMicroFramesLeft;
        private float fadePos;
        private float fadeStepPerMicroframe;

        public readonly Config Config;
        private readonly WaveBuffer audio;
        private readonly float[][] trackBuffers = new float[0x10][];
        private readonly BufferedWaveProvider buffer;

        public Mixer(Config config)
        {
            Config = config;
            const int sampleRate = 13379; // TODO: Actual value unknown
            SamplesPerBuffer = 224; // TODO
            SampleRateReciprocal = 1f / sampleRate;
            samplesReciprocal = 1f / SamplesPerBuffer;

            int amt = SamplesPerBuffer * 2;
            audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
            for (int i = 0; i < 0x10; i++)
            {
                trackBuffers[i] = new float[amt];
            }
            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2)) // TODO
            {
                DiscardOnBufferOverflow = true,
                BufferLength = SamplesPerBuffer * 64
            };
            Init(buffer);
        }
        public override void Dispose()
        {
            base.Dispose();
            CloseWaveWriter();
        }

        public void BeginFadeIn()
        {
            fadePos = 0f;
            fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * Utils.AGB_FPS);
            fadeStepPerMicroframe = 1f / fadeMicroFramesLeft;
        }
        public void BeginFadeOut()
        {
            fadePos = 1f;
            fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * Utils.AGB_FPS);
            fadeStepPerMicroframe = -1f / fadeMicroFramesLeft;
        }
        public bool IsFadeDone()
        {
            return fadeMicroFramesLeft == 0;
        }
        public void ResetFade()
        {
            fadeMicroFramesLeft = 0;
        }

        private WaveFileWriter waveWriter;
        public void CreateWaveWriter(string fileName)
        {
            waveWriter = new WaveFileWriter(fileName, buffer.WaveFormat);
        }
        public void CloseWaveWriter()
        {
            waveWriter?.Dispose();
        }
        public void Process(Track[] tracks, bool output, bool recording)
        {
            audio.Clear();
            float fromMaster = 1f, toMaster = 1f;
            if (fadeMicroFramesLeft > 0)
            {
                const float scale = 10f / 6f;
                fromMaster *= (fadePos < 0f) ? 0f : (float)Math.Pow(fadePos, scale);
                fadePos += fadeStepPerMicroframe;
                toMaster *= (fadePos < 0f) ? 0f : (float)Math.Pow(fadePos, scale);
                fadeMicroFramesLeft--;
            }
            float masterStep = (toMaster - fromMaster) * samplesReciprocal;
            for (int i = 0; i < 0x10; i++)
            {
                Track track = tracks[i];
                if (track.Enabled && track.NoteDuration != 0 && !track.Channel.Stopped && !Mutes[i])
                {
                    float masterLevel = fromMaster;
                    float[] buf = trackBuffers[i];
                    Array.Clear(buf, 0, buf.Length);
                    track.Channel.Process(buf);
                    for (int j = 0; j < SamplesPerBuffer; j++)
                    {
                        audio.FloatBuffer[j * 2] += buf[j * 2] * masterLevel;
                        audio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1] * masterLevel;
                        masterLevel += masterStep;
                    }
                }
            }
            if (output)
            {
                buffer.AddSamples(audio.ByteBuffer, 0, audio.ByteBufferCount);
            }
            if (recording)
            {
                waveWriter.Write(audio.ByteBuffer, 0, audio.ByteBufferCount);
            }
        }
    }
}
