using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class MLSSMixer : Mixer
    {
        public readonly float SampleRateReciprocal, SamplesReciprocal;
        public readonly int SamplesPerBuffer;

        public readonly MLSSConfig Config;
        private readonly WaveBuffer audio;
        private readonly float[][] trackBuffers = new float[0x10][];
        private readonly BufferedWaveProvider buffer;

        public MLSSMixer(MLSSConfig config)
        {
            Config = config;
            SamplesPerBuffer = 224; // TODO
            SampleRateReciprocal = 1f / 13379; // TODO: Actual frequency unknown
            SamplesReciprocal = 1f / SamplesPerBuffer;

            Mutes = new bool[0x10];

            int amt = SamplesPerBuffer * 2;
            audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
            for (int i = 0; i < 0x10; i++)
            {
                trackBuffers[i] = new float[amt];
            }
            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(13379, 2)) // TODO
            {
                DiscardOnBufferOverflow = true,
                BufferLength = SamplesPerBuffer * 64
            };
            Init(buffer);
        }

        public void Process(Track[] tracks)
        {
            audio.Clear();
            for (int i = 0; i < 0x10; i++)
            {
                Track track = tracks[i];
                if (track.Enabled && track.NoteDuration != 0 && !track.Channel.Stopped && !Mutes[i])
                {
                    float[] buf = trackBuffers[i];
                    Array.Clear(buf, 0, buf.Length);
                    track.Channel.Process(buf);
                    for (int j = 0; j < SamplesPerBuffer; j++)
                    {
                        audio.FloatBuffer[j * 2] += buf[j * 2];
                        audio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1];
                    }
                }
            }
            buffer.AddSamples(audio, 0, audio.ByteBufferCount);
        }
    }
}

