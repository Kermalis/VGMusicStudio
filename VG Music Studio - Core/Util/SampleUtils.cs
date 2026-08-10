using System;

namespace Kermalis.VGMusicStudio.Core.Util;

internal static class SampleUtils
{
    public static void PCMU8ToPCM16(ReadOnlySpan<byte> src, Span<short> dest)
    {
        for (int i = 0; i < src.Length; i++)
        {
            byte b = src[i];
            dest[i] = (short)((b - 0x80) << 8);
        }
    }
    public static void PCM16ToPCMU8(ReadOnlySpan<short> src, Span<byte> dest)
    {
        for (int i = 0; i < src.Length; i++)
        {
            short b = src[i];
            dest[i] = (byte)((b + 0x8000) >> 8);
        }
    }
    public static void PCM16ToPCM24(ReadOnlySpan<short> src, Span<Int24> dest)
    {
        for (int i = 0; i < src.Length; i++)
        {
            short b = src[i];
            dest[i] = (Int24)(b << 8);
        }
    }
    public static void PCM24ToPCM16(ReadOnlySpan<Int24> src, Span<short> dest)
    {
        for (int i = 0; i < src.Length; i++)
        {
            Int24 b = src[i];
            dest[i] = (short)(b >> 8);
        }
    }
}
