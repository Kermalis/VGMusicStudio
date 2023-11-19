using System;

namespace Kermalis.VGMusicStudio.Core.Util
{
	internal static class SampleUtils
	{
		// TODO: Span output?
		public static short[] PCMU8ToPCM16(ReadOnlySpan<byte> data)
		{
			short[] ret = new short[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				byte b = data[i];
				ret[i] = (short)((b - 0x80) << 8);
			}
			return ret;
		}
	}
}
