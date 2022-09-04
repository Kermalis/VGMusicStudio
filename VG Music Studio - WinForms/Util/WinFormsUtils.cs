using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Kermalis.VGMusicStudio.WinForms.Util;

internal static class WinFormsUtils
{
	private static readonly Random _rng = new();

	public static string Print<T>(this IEnumerable<T> source, bool parenthesis = true)
	{
		string str = parenthesis ? "( " : "";
		str += string.Join(", ", source);
		str += parenthesis ? " )" : "";
		return str;
	}
	/// <summary>Fisher-Yates Shuffle</summary>
	public static void Shuffle<T>(this IList<T> source)
	{
		for (int a = 0; a < source.Count - 1; a++)
		{
			int b = _rng.Next(a, source.Count);
			(source[b], source[a]) = (source[a], source[b]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static float Lerp(float progress, float from, float to)
	{
		return from + ((to - from) * progress);
	}
	/// <summary>Maps a value in the range [a1, a2] to [b1, b2]. Divide by zero occurs if a1 and a2 are equal</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static float Lerp(float value, float a1, float a2, float b1, float b2)
	{
		return b1 + ((value - a1) / (a2 - a1) * (b2 - b1));
	}
}
