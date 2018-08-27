using System.Collections;

namespace GBAMusicStudio.Core
{
    static class GBSamples
    {
        // Squares
        public static float[] SquareD12 = new float[] { 0.50f, 0.50f, 0.50f, 0.50f, -0.50f, -0.50f, -0.50f, -0.50f };
        public static float[] SquareD25 = new float[] { 0.875f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f };
        public static float[] SquareD50 = new float[] { 0.75f, 0.75f, -0.25f, -0.25f, -0.25f, -0.25f, -0.25f, -0.25f };
        public static float[] SquareD75 = new float[] { 0.25f, 0.25f, 0.25f, 0.25f, 0.25f, 0.25f, -0.75f, -0.75f };

        // Waves
        public static float[] PCM4ToFloat(uint address)
        {
            var sample = new float[0x20];

            byte[] data = ROM.Instance.ReadBytes(0x10, address);
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
    }
}
