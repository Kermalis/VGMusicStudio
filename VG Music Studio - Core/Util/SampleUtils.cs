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
}
