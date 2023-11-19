using System.Collections;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
	internal static class Utils
	{
		public static readonly byte[] RestTable = new byte[49]
		{
			00, 01, 02, 03, 04, 05, 06, 07,
			08, 09, 10, 11, 12, 13, 14, 15,
			16, 17, 18, 19, 20, 21, 22, 23,
			24, 28, 30, 32, 36, 40, 42, 44,
			48, 52, 54, 56, 60, 64, 66, 68,
			72, 76, 78, 80, 84, 88, 90, 92,
			96,
		};
		public static readonly (int sampleRate, int samplesPerBuffer)[] FrequencyTable = new (int, int)[12]
		{
			(05734, 096), // 59.72916666666667
            (07884, 132), // 59.72727272727273
            (10512, 176), // 59.72727272727273
            (13379, 224), // 59.72767857142857
            (15768, 264), // 59.72727272727273
            (18157, 304), // 59.72697368421053
            (21024, 352), // 59.72727272727273
            (26758, 448), // 59.72767857142857
            (31536, 528), // 59.72727272727273
            (36314, 608), // 59.72697368421053
            (40137, 672), // 59.72767857142857
            (42048, 704), // 59.72727272727273
        };

		// Squares
		public static readonly float[] SquareD12 = new float[8] {  0.875f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, };
		public static readonly float[] SquareD25 = new float[8] {  0.750f,  0.750f, -0.250f, -0.250f, -0.250f, -0.250f, -0.250f, -0.250f, };
		public static readonly float[] SquareD50 = new float[8] {  0.500f,  0.500f,  0.500f,  0.500f, -0.500f, -0.500f, -0.500f, -0.500f, };
		public static readonly float[] SquareD75 = new float[8] {  0.250f,  0.250f,  0.250f,  0.250f,  0.250f,  0.250f, -0.750f, -0.750f, };

		// Noises
		public static readonly BitArray NoiseFine;
		public static readonly BitArray NoiseRough;
		public static readonly byte[] NoiseFrequencyTable = new byte[60]
		{
			0xD7, 0xD6, 0xD5, 0xD4,
			0xC7, 0xC6, 0xC5, 0xC4,
			0xB7, 0xB6, 0xB5, 0xB4,
			0xA7, 0xA6, 0xA5, 0xA4,
			0x97, 0x96, 0x95, 0x94,
			0x87, 0x86, 0x85, 0x84,
			0x77, 0x76, 0x75, 0x74,
			0x67, 0x66, 0x65, 0x64,
			0x57, 0x56, 0x55, 0x54,
			0x47, 0x46, 0x45, 0x44,
			0x37, 0x36, 0x35, 0x34,
			0x27, 0x26, 0x25, 0x24,
			0x17, 0x16, 0x15, 0x14,
			0x07, 0x06, 0x05, 0x04,
			0x03, 0x02, 0x01, 0x00,
		};

		// PCM4
		// TODO: Do runtime instead of make arrays
		public static float[] PCM4ToFloat(int sampleOffset)
		{
			var config = (MP2KConfig)Engine.Instance.Config;
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
		private static readonly sbyte[] _compressionLookup = new sbyte[16]
		{
			0, 1, 4, 9, 16, 25, 36, 49, -64, -49, -36, -25, -16, -9, -4, -1,
		};
		public static sbyte[] Decompress(int sampleOffset, int sampleLength)
		{
			var config = (MP2KConfig)Engine.Instance.Config;
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
						compressionLevel += _compressionLookup[b >> 4];
						samples.Add(compressionLevel);
						if (++compressionIdx >= sampleLength)
						{
							break;
						}
					}
					compressionByte--;
					compressionLevel += _compressionLookup[b & 0xF];
					samples.Add(compressionLevel);
					if (++compressionIdx >= sampleLength)
					{
						break;
					}
				}
			}

			return samples.ToArray();
		}

		static Utils()
		{
			NoiseFine = new BitArray(0x8_000);
			int reg = 0x4_000;
			for (int i = 0; i < NoiseFine.Length; i++)
			{
				if ((reg & 1) == 1)
				{
					reg >>= 1;
					reg ^= 0x6_000;
					NoiseFine[i] = true;
				}
				else
				{
					reg >>= 1;
					NoiseFine[i] = false;
				}
			}
			NoiseRough = new BitArray(0x80);
			reg = 0x40;
			for (int i = 0; i < NoiseRough.Length; i++)
			{
				if ((reg & 1) == 1)
				{
					reg >>= 1;
					reg ^= 0x60;
					NoiseRough[i] = true;
				}
				else
				{
					reg >>= 1;
					NoiseRough[i] = false;
				}
			}
		}
		public static int Tri(int index)
		{
			index = (index - 64) & 0xFF;
			return (index < 128) ? (index * 12) - 768 : 2_304 - (index * 12);
		}
	}
}
