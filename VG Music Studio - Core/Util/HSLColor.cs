using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.Util;

public readonly struct HSLColor
{
	public readonly int H;
	public readonly byte S;
	public readonly byte L;

	public HSLColor(int h, byte s, byte l)
	{
		H = h;
		S = s;
		L = l;
	}
	public HSLColor(in Color c)
	{
		double modifiedR, modifiedG, modifiedB, min, max, delta, h, s, l;

		modifiedR = c.R / 255.0;
		modifiedG = c.G / 255.0;
		modifiedB = c.B / 255.0;

		min = new List<double>(3) { modifiedR, modifiedG, modifiedB }.Min();
		max = new List<double>(3) { modifiedR, modifiedG, modifiedB }.Max();
		delta = max - min;
		l = (min + max) / 2;

		if (delta == 0)
		{
			h = 0;
			s = 0;
		}
		else
		{
			s = (l <= 0.5) ? (delta / (min + max)) : (delta / (2 - max - min));

			if (modifiedR == max)
			{
				h = (modifiedG - modifiedB) / 6 / delta;
			}
			else if (modifiedG == max)
			{
				h = (1.0 / 3) + ((modifiedB - modifiedR) / 6 / delta);
			}
			else
			{
				h = (2.0 / 3) + ((modifiedR - modifiedG) / 6 / delta);
			}

			h = (h < 0) ? ++h : h;
			h = (h > 1) ? --h : h;
		}

		H = (int)Math.Round(h * 360);
		S = (byte)Math.Round(s * 100);
		L = (byte)Math.Round(l * 100);
	}

	public Color ToColor()
	{
		return ToColor(H, S, L);
	}
	// https://github.com/iamartyom/ColorHelper/blob/master/ColorHelper/Converter/ColorConverter.cs
	public static Color ToColor(int h, byte s, byte l)
	{
		double modifiedH, modifiedS, modifiedL,
			r = 1, g = 1, b = 1,
			q, p;

		modifiedH = h / 360.0;
		modifiedS = s / 100.0;
		modifiedL = l / 100.0;

		q = (modifiedL < 0.5) ? modifiedL * (1 + modifiedS) : modifiedL + modifiedS - modifiedL * modifiedS;
		p = 2 * modifiedL - q;

		if (modifiedL == 0) // If the lightness value is 0 it will always be black
		{
			r = 0;
			g = 0;
			b = 0;
		}
		else if (modifiedS != 0)
		{
			r = GetHue(p, q, modifiedH + 1.0 / 3);
			g = GetHue(p, q, modifiedH);
			b = GetHue(p, q, modifiedH - 1.0 / 3);
		}

		return Color.FromArgb(255, (byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
	}
	private static double GetHue(double p, double q, double t)
	{
		double value = p;

		if (t < 0)
		{
			t++;
		}
		else if (t > 1)
		{
			t--;
		}

		if (t < 1.0 / 6)
		{
			value = p + (q - p) * 6 * t;
		}
		else if (t < 1.0 / 2)
		{
			value = q;
		}
		else if (t < 2.0 / 3)
		{
			value = p + (q - p) * (2.0 / 3 - t) * 6;
		}

		return value;
	}

	public override bool Equals(object? obj)
	{
		if (obj is HSLColor other)
		{
			return H == other.H && S == other.S && L == other.L;
		}
		return false;
	}
	public override int GetHashCode()
	{
		return HashCode.Combine(H, S, L);
	}

	public override string ToString()
	{
		return $"{H}° {S}% {L}%";
	}

	public static bool operator ==(HSLColor left, HSLColor right)
	{
		return left.Equals(right);
	}
	public static bool operator !=(HSLColor left, HSLColor right)
	{
		return !(left == right);
	}
}
