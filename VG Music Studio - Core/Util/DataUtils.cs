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
}
