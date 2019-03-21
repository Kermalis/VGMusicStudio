using System;

namespace Kermalis.GBAMusicStudio.Core
{
    // All of this was written in C++ by ipatix; I just converted it
    class Reverb
    {
        protected readonly byte[] reverbBuffer;
        protected readonly byte intensity;
        protected readonly int bufferLen;
        protected int bufferPos1, bufferPos2;

        public Reverb(byte intensity, byte numBuffers)
        {
            bufferLen = (int)(ROM.Instance.Game.Engine.Frequency / Engine.AGB_FPS);
            bufferPos2 = bufferLen;
            this.intensity = intensity;
            reverbBuffer = new byte[bufferLen * 2 * numBuffers];
        }

        public void Process(byte[] buffer, int samplesPerBuffer)
        {
            int index = 0;
            while (samplesPerBuffer > 0)
            {
                int left = Process(buffer, samplesPerBuffer, ref index);
                index += (samplesPerBuffer - left) * 2;
                samplesPerBuffer = left;
            }
        }

        protected virtual int Process(byte[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;

            int count = Math.Min(
                Math.Min(rSamplesPerBuffer - bufferPos1, rSamplesPerBuffer - bufferPos2),
                samplesPerBuffer
                );
            bool reset1 = rSamplesPerBuffer - bufferPos1 == count,
                reset2 = rSamplesPerBuffer - bufferPos2 == count;
            int c = count;
            do
            {
                byte rev = (byte)(((reverbBuffer[bufferPos1 * 2] * 2) + (reverbBuffer[(bufferPos1 * 2) + 1] * 2)) * intensity / 0x200);

                reverbBuffer[bufferPos1 * 2] = buffer[index++] += rev;
                reverbBuffer[bufferPos1 * 2 + 1] = buffer[index++] += rev;
                bufferPos1++; bufferPos2++;
            } while (--c > 0);
            if (reset1)
            {
                bufferPos1 = 0;
            }
            if (reset2)
            {
                bufferPos2 = 0;
            }
            return samplesPerBuffer - count;
        }
    }

    class ReverbCamelot1 : Reverb
    {
        byte[] cBuffer;
        public ReverbCamelot1(byte intensity, byte numBuffers) : base(intensity, numBuffers)
        {
            bufferPos2 = 0;
            cBuffer = new byte[bufferLen * 2];
        }

        protected override int Process(byte[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;
            int cSamplesPerBuffer = cBuffer.Length / 2;
            int count = Math.Min(
                Math.Min(rSamplesPerBuffer - bufferPos1, cSamplesPerBuffer - bufferPos2),
                samplesPerBuffer
                );
            bool reset1 = count == rSamplesPerBuffer - bufferPos1,
                resetC = count == cSamplesPerBuffer - bufferPos2;
            int c = count;
            do
            {
                byte mixL = (byte)(buffer[index] + cBuffer[bufferPos2 * 2]);
                byte mixR = (byte)(buffer[index + 1] + cBuffer[bufferPos2 * 2 + 1]);

                byte lA = reverbBuffer[bufferPos1 * 2];
                byte rA = reverbBuffer[bufferPos1 * 2 + 1];

                buffer[index] = reverbBuffer[bufferPos1 * 2] = mixL;
                buffer[index + 1] = reverbBuffer[bufferPos1 * 2 + 1] = mixR;

                byte lRMix = (byte)((mixL / 4) + (rA / 4));
                byte rRMix = (byte)((mixR / 4) + (lA / 4));

                cBuffer[bufferPos2 * 2] = lRMix;
                cBuffer[bufferPos2 * 2 + 1] = rRMix;

                index += 2;
                bufferPos1++; bufferPos2++;
            } while (--c > 0);

            if (reset1)
            {
                bufferPos1 = 0;
            }
            if (resetC)
            {
                bufferPos2 = 0;
            }
            return samplesPerBuffer - count;
        }
    }

    class ReverbCamelot2 : Reverb
    {
        byte[] cBuffer; int cPos;
        readonly float primary, secondary;
        internal ReverbCamelot2(byte intensity, byte numBuffers, float primary, float secondary) : base(intensity, numBuffers)
        {
            cBuffer = new byte[bufferLen * 2];
            bufferPos2 = reverbBuffer.Length / 2 - (cBuffer.Length / 2 / 3);
            this.primary = primary; this.secondary = secondary;
        }

        protected override int Process(byte[] buffer, int samplesPerBuffer, ref int index)
        {
            int rSamplesPerBuffer = reverbBuffer.Length / 2;
            int count = Math.Min(
                    Math.Min(rSamplesPerBuffer - bufferPos1, rSamplesPerBuffer - bufferPos2),
                    Math.Min(samplesPerBuffer, cBuffer.Length / 2 - cPos)
                    );
            bool reset = rSamplesPerBuffer - bufferPos1 == count,
                reset2 = rSamplesPerBuffer - bufferPos2 == count,
                resetC = (cBuffer.Length / 2) - cPos == count;

            int c = count;
            do
            {
                byte mixL = (byte)(buffer[index] + cBuffer[cPos * 2]);
                byte mixR = (byte)(buffer[index + 1] + cBuffer[cPos * 2 + 1]);

                byte lA = reverbBuffer[bufferPos1 * 2];
                byte rA = reverbBuffer[bufferPos1 * 2 + 1];

                buffer[index] = reverbBuffer[bufferPos1 * 2] = mixL;
                buffer[index + 1] = reverbBuffer[bufferPos1 * 2 + 1] = mixR;

                byte lRMix = (byte)((byte)(lA * primary) + (byte)(rA * secondary));
                byte rRMix = (byte)((byte)(rA * primary) + (byte)(lA * secondary));

                byte lB = (byte)(reverbBuffer[bufferPos2 * 2 + 1] / 4);
                byte rB = (byte)(mixR / 4);

                cBuffer[cPos * 2] = (byte)(lRMix + lB);
                cBuffer[cPos * 2 + 1] = (byte)(rRMix + rB);

                index += 2;
                bufferPos1++; bufferPos2++; cPos++;
            } while (--c > 0);
            if (reset)
            {
                bufferPos1 = 0;
            }
            if (reset2)
            {
                bufferPos2 = 0;
            }
            if (resetC)
            {
                cPos = 0;
            }
            return samplesPerBuffer - count;
        }
    }
}