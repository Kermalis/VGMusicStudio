using System;

namespace GBAMusicStudio.Core
{
    // All of this was written in C++ by ipatix; I just converted it
    class Reverb
    {
        protected readonly float[] reverbBuffer;
        protected readonly float intensity;
        protected readonly uint bufferLen;
        protected uint bufferPos1, bufferPos2;

        public Reverb(byte intensity, byte numBuffers)
        {
            bufferLen = Config.Instance.SampleRate / Engine.AGB_FPS;
            bufferPos2 = bufferLen;
            this.intensity = intensity / (float)0x80;
            reverbBuffer = new float[bufferLen * 2 * numBuffers];
        }

        public void Process(float[] buffer, int samplesPerBuffer)
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
                Math.Min(rSamplesPerBuffer - bufferPos1, rSamplesPerBuffer - bufferPos2),
                samplesPerBuffer
                );
            bool reset1 = (rSamplesPerBuffer - bufferPos1 == count),
                reset2 = (rSamplesPerBuffer - bufferPos2 == count);
            var c = count;
            do
            {
                float rev = (reverbBuffer[bufferPos1 * 2] * 2
                    + reverbBuffer[bufferPos1 * 2 + 1] * 2) * intensity / 4;

                reverbBuffer[bufferPos1 * 2] = buffer[index++] += rev;
                reverbBuffer[bufferPos1 * 2 + 1] = buffer[index++] += rev;
                bufferPos1++; bufferPos2++;
            } while (--c > 0);
            if (reset1) bufferPos1 = 0; if (reset2) bufferPos2 = 0;
            return samplesPerBuffer - count;
        }
    }

    class ReverbCamelot1 : Reverb
    {
        float[] cBuffer;
        public ReverbCamelot1(byte intensity, byte numBuffers) : base(intensity, numBuffers)
        {
            bufferPos2 = 0;
            cBuffer = new float[bufferLen * 2];
        }

        protected override int Process(float[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;
            int cSamplesPerBuffer = cBuffer.Length / 2;
            int count = (int)Math.Min(
                Math.Min(rSamplesPerBuffer - bufferPos1, cSamplesPerBuffer - bufferPos2),
                samplesPerBuffer
                );
            bool reset1 = (count == rSamplesPerBuffer - bufferPos1),
                resetC = (count == cSamplesPerBuffer - bufferPos2);
            int c = count;
            do
            {
                float mixL = buffer[index] + cBuffer[bufferPos2 * 2];
                float mixR = buffer[index + 1] + cBuffer[bufferPos2 * 2 + 1];

                float lA = reverbBuffer[bufferPos1 * 2];
                float rA = reverbBuffer[bufferPos1 * 2 + 1];

                buffer[index] = reverbBuffer[bufferPos1 * 2] = mixL;
                buffer[index + 1] = reverbBuffer[bufferPos1 * 2 + 1] = mixR;

                float lRMix = mixL / 4f + rA / 4f;
                float rRMix = mixR / 4f + lA / 4f;

                cBuffer[bufferPos2 * 2] = lRMix;
                cBuffer[bufferPos2 * 2 + 1] = rRMix;

                index += 2;
                bufferPos1++; bufferPos2++;
            } while (--c > 0);

            if (reset1) bufferPos1 = 0; if (resetC) bufferPos2 = 0;
            return samplesPerBuffer - count;
        }
    }

    internal class ReverbCamelot2 : Reverb
    {
        float[] cBuffer; int cPos;
        readonly float primary, secondary;
        internal ReverbCamelot2(byte intensity, byte numBuffers, float primary, float secondary) : base(intensity, numBuffers)
        {
            cBuffer = new float[bufferLen * 2];
            bufferPos2 = (uint)(reverbBuffer.Length / 2 - (cBuffer.Length / 2 / 3));
            this.primary = primary; this.secondary = secondary;
        }

        protected override int Process(float[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;
            int count = (int)Math.Min(
                    Math.Min(rSamplesPerBuffer - bufferPos1, rSamplesPerBuffer - bufferPos2),
                    Math.Min(samplesPerBuffer, cBuffer.Length / 2 - cPos)
                    );
            bool reset = (rSamplesPerBuffer - bufferPos1 == count),
                reset2 = (rSamplesPerBuffer - bufferPos2 == count),
                resetC = (cBuffer.Length / 2 - cPos == count);

            int c = count;
            do
            {
                float mixL = buffer[index] + cBuffer[cPos * 2];
                float mixR = buffer[index + 1] + cBuffer[cPos * 2 + 1];

                float lA = reverbBuffer[bufferPos1 * 2];
                float rA = reverbBuffer[bufferPos1 * 2 + 1];

                buffer[index] = reverbBuffer[bufferPos1 * 2] = mixL;
                buffer[index + 1] = reverbBuffer[bufferPos1 * 2 + 1] = mixR;

                float lRMix = lA * primary + rA * secondary;
                float rRMix = rA * primary + lA * secondary;

                float lB = reverbBuffer[bufferPos2 * 2 + 1] / 4f;
                float rB = mixR / 4f;

                cBuffer[cPos * 2] = lRMix + lB;
                cBuffer[cPos * 2 + 1] = rRMix + rB;

                index += 2;
                bufferPos1++; bufferPos2++; cPos++;
            } while (--c > 0);
            if (reset) bufferPos1 = 0; if (reset2) bufferPos2 = 0; if (resetC) cPos = 0;
            return samplesPerBuffer - count;
        }
    }
}