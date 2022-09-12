using System;

namespace Kermalis.VGMusicStudio.Core.Util;

internal static class LanguageUtils
{
	// Try to handle lang strings like "мелодий|0_0|мелодия|1_1|мелодии|2_4|мелодий|5_*|"
	public static string HandlePlural(int count, string str)
	{
		string[] split = str.Split('|', StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < split.Length; i += 2)
		{
			string text = split[i];
			string range = split[i + 1];

			int rangeSplit = range.IndexOf('_');
			int rangeStart = GetPluralRangeValue(range.AsSpan(0, rangeSplit), int.MinValue);
			int rangeEnd = GetPluralRangeValue(range.AsSpan(rangeSplit + 1), int.MaxValue);
			if (count >= rangeStart && count <= rangeEnd)
			{
				return text;
			}
		}
		throw new ArgumentOutOfRangeException(nameof(str), str, "Could not find plural entry");
	}
	private static int GetPluralRangeValue(ReadOnlySpan<char> chars, int star)
	{
		return chars.Length == 1 && chars[0] == '*' ? star : int.Parse(chars);
	}
}
