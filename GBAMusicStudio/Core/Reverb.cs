using System;

namespace GBAMusicStudio.Core
{
    // All of this was written in C++ by ipatix; I just converted it
    internal class Reverb
    {
        protected readonly float[] reverbBuffer;
        protected readonly float intensity;
        protected readonly uint bufferLen;
        protected uint bufferPos, bufferPos2;

        internal Reverb(byte intensity, byte numBuffers)
        {
            bufferLen = Config.SampleRate / Engine.AGB_FPS;
            bufferPos2 = bufferLen;
            this.intensity = intensity / (float)0x80;
            reverbBuffer = new float[bufferLen * 2 * numBuffers];
        }

        internal void Process(float[] buffer, int samplesPerBuffer)
        {
            int index = 0;
            while (samplesPerBuffer > 0)
            {
                var left = Process(buffer, samplesPerBuffer, ref index);
                index += (samplesPerBuffer - left) * 2;
                samplesPerBuffer = left;
            }
        }

        protected virtual int Process(float[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;

            var count = (int)Math.Min(
                Math.Min(rSamplesPerBuffer - bufferPos2, rSamplesPerBuffer - bufferPos),
                samplesPerBuffer
                );
            bool reset = (rSamplesPerBuffer - bufferPos == count),
                reset2 = (rSamplesPerBuffer - bufferPos2 == count);
            var c = count;
            do
            {
                float rev = (reverbBuffer[bufferPos * 2] * 2
                    + reverbBuffer[bufferPos * 2 + 1] * 2) * intensity / 4;

                reverbBuffer[bufferPos * 2] = buffer[index++] += rev;
                reverbBuffer[bufferPos * 2 + 1] = buffer[index++] += rev;
                bufferPos++; bufferPos2++;
            } while (--c > 0);
            if (reset) bufferPos = 0; if (reset2) bufferPos2 = 0;
            return samplesPerBuffer - count;
        }
    }

    internal class ReverbCamelot1 : Reverb
    {
        float[] cBuffer;
        internal ReverbCamelot1(byte intensity, byte numBuffers) : base(intensity, numBuffers)
        {
            bufferPos2 = 0;
            cBuffer = new float[bufferLen * 2];
        }

        protected override int Process(float[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;
            int rGSSamplesPerBuffer = cBuffer.Length / 2;
            int count = (int)Math.Min(
                Math.Min(rSamplesPerBuffer - bufferPos, rGSSamplesPerBuffer - bufferPos2),
                samplesPerBuffer
                );
            bool reset = (count == rSamplesPerBuffer - bufferPos),
                resetGS = (count == rGSSamplesPerBuffer - bufferPos2);
            int c = count;
            do
            {
                float mixL = buffer[index] + cBuffer[bufferPos2 * 2];
                float mixR = buffer[index + 1] + cBuffer[bufferPos2 * 2 + 1];

                float lA = reverbBuffer[bufferPos * 2];
                float rA = reverbBuffer[bufferPos * 2 + 1];

                buffer[index] = reverbBuffer[bufferPos * 2] = mixL;
                buffer[index + 1] = reverbBuffer[bufferPos * 2 + 1] = mixR;

                float lRMix = mixL / 4f + rA / 4f;
                float rRMix = mixR / 4f + lA / 4f;

                cBuffer[bufferPos2 * 2] = lRMix;
                cBuffer[bufferPos2 * 2 + 1] = rRMix;

                index += 2;
                bufferPos++; bufferPos2++;
            } while (--c > 0);

            if (reset) bufferPos = 0; if (resetGS) bufferPos2 = 0;
            return samplesPerBuffer - count;
        }
    }
}