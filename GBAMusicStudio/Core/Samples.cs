using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GBAMusicStudio.Core
{
    static class Samples
    {
        // Squares
        public static float[] SquareD12 = new float[] { 0.50f, 0.50f, 0.50f, 0.50f, -0.50f, -0.50f, -0.50f, -0.50f };
        public static float[] SquareD25 = new float[] { 0.875f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f };
        public static float[] SquareD50 = new float[] { 0.75f, 0.75f, -0.25f, -0.25f, -0.25f, -0.25f, -0.25f, -0.25f };
        public static float[] SquareD75 = new float[] { 0.25f, 0.25f, 0.25f, 0.25f, 0.25f, 0.25f, -0.75f, -0.75f };

        // Noises
        static BitArray noiseFine = null, noiseRough = null;
        public static BitArray NoiseFine
        {
            get
            {
                if (noiseFine == null)
                {
                    noiseFine = new BitArray(0x8000);
                    int reg = 0x4000;
                    for (int i = 0; i < noiseFine.Length; i++)
                    {
                        if ((reg & 1) == 1)
                        {
                            reg >>= 1;
                            reg ^= 0x6000;
                            noiseFine[i] = true;
                        }
                        else
                        {
                            reg >>= 1;
                            noiseFine[i] = false;
                        }
                    }
                }
                return noiseFine;
            }
        }
        public static BitArray NoiseRough
        {
            get
            {
                if (noiseRough == null)
                {
                    noiseRough = new BitArray(0x80);
                    int reg = 0x40;
                    for (int i = 0; i < noiseRough.Length; i++)
                    {
                        if ((reg & 1) == 1)
                        {
                            reg >>= 1;
                            reg ^= 0x60;
                            noiseRough[i] = true;
                        }
                        else
                        {
                            reg >>= 1;
                            noiseRough[i] = false;
                        }
                    }
                }
                return noiseRough;
            }
        }
        

        public static float[] PCM4ToFloat(int address)
        {
            var sample = new float[0x20];

            byte[] data = ROM.Instance.Reader.ReadBytes(0x10, address);
            float sum = 0;
            for (int i = 0; i < 0x10; i++)
            {
                byte b = data[i];
                float first = (b >> 4) / 16f;
                float second = (b & 0xF) / 16f;
                sum += sample[i * 2] = first;
                sum += sample[i * 2 + 1] = second;
            }
            float dcCorrection = sum / 0x20;
            for (int i = 0; i < 0x20; i++)
                sample[i] -= dcCorrection;

            return sample;
        }
        public static short[] PCM8ToPCM16(byte[] pcm8) => pcm8.Select(i => (short)(i << 8)).ToArray();
        public static short[] PCMU8ToPCM16(byte[] pcm8) => pcm8.Select(i => (short)((i - 0x80) << 8)).ToArray();
        public static short[] FloatToPCM16(float[] ieee) => ieee.Select(i => (short)(i * short.MaxValue)).ToArray();
        public static short[] BitArrayToPCM16(BitArray bitArray)
        {
            short[] ret = new short[bitArray.Length];
            for (int i = 0; i < bitArray.Length; i++)
                ret[i] = (short)((bitArray[i] ? short.MaxValue : short.MinValue) / 2);
            return ret;
        }

        // Pokemon Only
        public static readonly sbyte[] CompressionLookup = { 0, 1, 4, 9, 16, 25, 36, 49, -64, -49, -36, -25, -16, -9, -4, -1 };
        public static sbyte[] Decompress(WrappedSample sample)
        {
            List<sbyte> samples = new List<sbyte>();
            sbyte compressionLevel = 0;
            int compressionByte = 0, compressionIdx = 0;

            for (int i = 0; true; i++)
            {
                byte b = ROM.Instance.Reader.ReadByte(sample.GetOffset() + i);
                if (compressionByte == 0)
                {
                    compressionByte = 0x20;
                    compressionLevel = (sbyte)b;
                    samples.Add(compressionLevel);
                    if (++compressionIdx >= sample.Length)
                        break;
                }
                else
                {
                    if(compressionByte < 0x20)
                    {
                        compressionLevel += CompressionLookup[b >> 4];
                        samples.Add(compressionLevel);
                        if (++compressionIdx >= sample.Length)
                            break;
                    }
                    compressionByte--;
                    compressionLevel += CompressionLookup[b & 0xF];
                    samples.Add(compressionLevel);
                    if (++compressionIdx >= sample.Length)
                        break;
                }
            }

            return samples.ToArray();
        }
    }
}
