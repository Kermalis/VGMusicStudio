using NAudio.Wave;
using System;

namespace GBAMusicStudio.Core
{
    internal class Reverb
    {
        WaveBuffer reverbBuffer;
        float intensity;
        uint bufferPos, bufferPos2, bufferLen;

        internal Reverb()
        {
            bufferLen = Config.SampleRate / Engine.AGB_FPS;
            bufferPos = 0;
            bufferPos2 = bufferLen;
            Init(0, 1);
        }
        internal void Init(byte intensity, byte numBuffers)
        {
            this.intensity = intensity / 128f;
            int amt = (int)(bufferLen * Engine.N_CHANNELS * numBuffers);
            reverbBuffer = new WaveBuffer(amt * 4) { FloatBufferCount = amt };
        }

        internal void Process(float[] buffer, int samplesPerBuffer)
        {
            int index = 0;
            while (samplesPerBuffer > 0)
            {
                var left = Process(buffer, samplesPerBuffer, ref index);
                index += (samplesPerBuffer - left) * Engine.N_CHANNELS;
                samplesPerBuffer = left;
            }
        }

        int Process(float[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.FloatBufferCount / Engine.N_CHANNELS;

            var count = (int)Math.Min(Math.Min(rSamplesPerBuffer - bufferPos2, rSamplesPerBuffer - bufferPos), samplesPerBuffer);
            bool reset = (rSamplesPerBuffer - bufferPos == count),
                reset2 = (rSamplesPerBuffer - bufferPos2 == count);
            var c = count;
            do
            {
                float rev = (reverbBuffer.FloatBuffer[bufferPos * Engine.N_CHANNELS] * 2
                    + reverbBuffer.FloatBuffer[bufferPos * Engine.N_CHANNELS + 1] * 2) * intensity / 4;

                reverbBuffer.FloatBuffer[bufferPos * Engine.N_CHANNELS] = buffer[index++] += rev;
                reverbBuffer.FloatBuffer[bufferPos * Engine.N_CHANNELS + 1] = buffer[index++] += rev;
                bufferPos++; bufferPos2++;
            } while (--c > 0);
            if (reset) bufferPos = 0; if (reset2) bufferPos2 = 0;
            return samplesPerBuffer - count;
        }
    }
}