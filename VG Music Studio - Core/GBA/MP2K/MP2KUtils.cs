using System;
using System.Collections;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal static partial class MP2KUtils
{
    public static ReadOnlySpan<byte> RestTable =>
    [
        00, 01, 02, 03, 04, 05, 06, 07,
        08, 09, 10, 11, 12, 13, 14, 15,
        16, 17, 18, 19, 20, 21, 22, 23,
        24, 28, 30, 32, 36, 40, 42, 44,
        48, 52, 54, 56, 60, 64, 66, 68,
        72, 76, 78, 80, 84, 88, 90, 92,
        96,
    ];
    public static ReadOnlySpan<byte> VolumeTable =>
    [
        14, 16, 17, 19, 21, 22, 24, 27, 29, 32, 34, 36, 37, 39, 42, 44, 46, 47, 49, 51, 52, 54, 56, 57, 59, 62, 64, 66, 67, 69, 72, 77, 79, 80, 82, 84, 85, 87,
        94, 96, 97, 104, 105, 106, 109, 113, 114, 116, 119, 120, 127,
    ];
    public static ReadOnlySpan<(int sampleRate, int samplesPerBuffer)> FrequencyTable => new (int, int)[12]
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
    public static ReadOnlySpan<(byte Command, byte Length)> DelayLUT => new (byte, byte)[49]
    {
        (0x80, 0),  (0x81, 1),  (0x82, 2),  (0x83, 3),  (0x84, 4),  (0x85, 5),  (0x86, 6),
        (0x87, 7),  (0x88, 8),  (0x89, 9),  (0x8A, 10), (0x8B, 11), (0x8C, 12), (0x8D, 13),
        (0x8E, 14), (0x8F, 15), (0x90, 16), (0x91, 17), (0x92, 18), (0x93, 19), (0x94, 20),
        (0x95, 21), (0x96, 22), (0x97, 23), (0x98, 24), (0x99, 28), (0x9A, 30), (0x9B, 32),
        (0x9C, 36), (0x9D, 40), (0x9E, 42), (0x9F, 44), (0xA0, 48), (0xA1, 52), (0xA2, 54),
        (0xA3, 56), (0xA4, 60), (0xA5, 64), (0xA6, 66), (0xA7, 68), (0xA8, 72), (0xA9, 76),
        (0xAA, 78), (0xAB, 80), (0xAC, 84), (0xAD, 88), (0xAE, 90), (0xAF, 92), (0xB0, 96)
    };
    public static ReadOnlySpan<(byte Command, byte Length)> NoteLUT => new (byte, byte)[49]
    {
        (0xCF, 0),  (0xD0, 1),  (0xD1, 2),  (0xD2, 3),  (0xD3, 4),  (0xD4, 5),  (0xD5, 6),
        (0xD6, 7),  (0xD7, 8),  (0xD8, 9),  (0xD9, 10), (0xDA, 11), (0xDB, 12), (0xDC, 13),
        (0xDD, 14), (0xDE, 15), (0xDF, 16), (0xE0, 17), (0xE1, 18), (0xE2, 19), (0xE3, 20),
        (0xE4, 21), (0xE5, 22), (0xE6, 23), (0xE7, 24), (0xE8, 28), (0xE9, 30), (0xEA, 32),
        (0xEB, 36), (0xEC, 40), (0xED, 42), (0xEE, 44), (0xEF, 48), (0xF0, 52), (0xF1, 54),
        (0xF2, 56), (0xF3, 60), (0xF4, 64), (0xF5, 66), (0xF6, 68), (0xF7, 72), (0xF8, 76),
        (0xF9, 78), (0xFA, 80), (0xFB, 84), (0xFC, 88), (0xFD, 90), (0xFE, 92), (0xFF, 96)
    };

    // Squares (Use arrays since they are stored as references in MP2KSquareChannel)
    public static readonly float[] SquareD12 = [0.875f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f, -0.125f,];
    public static readonly float[] SquareD25 = [0.750f, 0.750f, -0.250f, -0.250f, -0.250f, -0.250f, -0.250f, -0.250f,];
    public static readonly float[] SquareD50 = [0.500f, 0.500f, 0.500f, 0.500f, -0.500f, -0.500f, -0.500f, -0.500f,];
    public static readonly float[] SquareD75 = [0.250f, 0.250f, 0.250f, 0.250f, 0.250f, 0.250f, -0.750f, -0.750f,];

    // Noises
    public static readonly BitArray NoiseFine;
    public static readonly BitArray NoiseRough;
    public static ReadOnlySpan<byte> NoiseFrequencyTable =>
    [
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
    ];

    // PCM4
    /// <summary>4-bit PCM Wave to Float conversion</summary>
    /// <remarks>The dest param must be 0x20 bytes in length</remarks>
    public static void PCM4ToFloat(ReadOnlySpan<byte> src, Span<float> dest)
    {
        float sum = 0;
        for (int i = 0; i < 0x10; i++)
        {
            byte b = src[i];
            float first = (b >> 4) / 16f;
            float second = (b & 0xF) / 16f;
            sum += dest[i * 2] = first;
            sum += dest[(i * 2) + 1] = second;
        }
        float dcCorrection = sum / 0x20;
        for (int i = 0; i < 0x20; i++)
        {
            dest[i] -= dcCorrection;
        }
    }

    static MP2KUtils()
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
