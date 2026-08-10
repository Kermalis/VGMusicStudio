using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.Util;

internal static class DataUtils
{
	public static void Align(this Stream s, int num)
	{
		while (s.Position % num != 0)
		{
			s.Position++;
		}
	}

	public static int RoundUp(int numToRound, int multiple)
	{
		if (multiple == 0)
		{
			return numToRound;
		}

		int remainder = Math.Abs(numToRound) % multiple;
		if (remainder == 0)
		{
			return numToRound;
		}

		return (numToRound < 0) ? -(Math.Abs(numToRound) - remainder) : (numToRound + multiple - remainder);
	}
}
