using System.Collections;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal static class Samples
    {
        // Squares
        public static readonly float[] SquareD12 = new float[] {  0.500f,  0.500f,  0.500f,  0.500f, -0.500f, -0.500f, -0.500f, -0.500f };
        public static readonly float[] SquareD25 = new float[] {  0.875f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f };
        public static readonly float[] SquareD50 = new float[] {  0.750f,  0.750f, -0.250f, -0.250f, -0.250f, -0.250f, -0.250f, -0.250f };
        public static readonly float[] SquareD75 = new float[] {  0.250f,  0.250f,  0.250f,  0.250f,  0.250f,  0.250f, -0.750f, -0.750f };

        // Noises
        private static BitArray noiseFine = null, noiseRough = null;
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

        public static float[] PCM4ToFloat(int sampleOffset)
        {
            var config = (M4AConfig)Engine.Instance.Config;
            float[] sample = new float[0x20];
            float sum = 0;
            for (int i = 0; i < 0x10; i++)
            {
                byte b = config.ROM[sampleOffset + i];
                float first = (b >> 4) / 16f;
                float second = (b & 0xF) / 16f;
                sum += sample[i * 2] = first;
                sum += sample[(i * 2) + 1] = second;
            }
            float dcCorrection = sum / 0x20;
            for (int i = 0; i < 0x20; i++)
            {
                sample[i] -= dcCorrection;
            }
            return sample;
        }

        // Pokémon Only
        public static readonly sbyte[] CompressionLookup = { 0, 1, 4, 9, 16, 25, 36, 49, -64, -49, -36, -25, -16, -9, -4, -1 };
        public static sbyte[] Decompress(int sampleOffset, int sampleLength)
        {
            var config = (M4AConfig)Engine.Instance.Config;
            var samples = new List<sbyte>();
            sbyte compressionLevel = 0;
            int compressionByte = 0, compressionIdx = 0;

            for (int i = 0; true; i++)
            {
                byte b = config.ROM[sampleOffset + i];
                if (compressionByte == 0)
                {
                    compressionByte = 0x20;
                    compressionLevel = (sbyte)b;
                    samples.Add(compressionLevel);
                    if (++compressionIdx >= sampleLength)
                    {
                        break;
                    }
                }
                else
                {
                    if (compressionByte < 0x20)
                    {
                        compressionLevel += CompressionLookup[b >> 4];
                        samples.Add(compressionLevel);
                        if (++compressionIdx >= sampleLength)
                        {
                            break;
                        }
                    }
                    compressionByte--;
                    compressionLevel += CompressionLookup[b & 0xF];
                    samples.Add(compressionLevel);
                    if (++compressionIdx >= sampleLength)
                    {
                        break;
                    }
                }
            }

            return samples.ToArray();
        }
    }
}