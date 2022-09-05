using System;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.Util;

// https://www.rapidtables.com/convert/color/rgb-to-hsl.html
// https://www.rapidtables.com/convert/color/hsl-to-rgb.html
public readonly struct HSLColor
{
	/// <summary>[0, 1)</summary>
	public readonly double Hue;
	/// <summary>[0, 1]</summary>
	public readonly double Saturation;
	/// <summary>[0, 1]</summary>
	public readonly double Lightness;

	public HSLColor(double h, double s, double l)
	{
		Hue = h;
		Saturation = s;
		Lightness = l;
	}
	public HSLColor(in Color c)
	{
		double nR = c.R / 255.0;
		double nG = c.G / 255.0;
		double nB = c.B / 255.0;

		double max = Math.Max(Math.Max(nR, nG), nB);
		double min = Math.Min(Math.Min(nR, nG), nB);
		double delta = max - min;

		Lightness = (min + max) * 0.5;

		if (delta == 0)
		{
			Hue = 0;
		}
		else if (max == nR)
		{
			Hue = (nG - nB) / delta % 6 / 6;
		}
		else if (max == nG)
		{
			Hue = (((nB - nR) / delta) + 2) / 6;
		}
		else // max == nB
		{
			Hue = (((nR - nG) / delta) + 4) / 6;
		}

		if (delta == 0)
		{
			Saturation = 0;
		}
		else
		{
			Saturation = delta / (1 - Math.Abs((2 * Lightness) - 1));
		}
	}

	public Color ToColor()
	{
		return ToColor(Hue, Saturation, Lightness);
	}
	public static Color ToColor(double h, double s, double l)
	{
		h *= 360;

		double c = (1 - Math.Abs((2 * l) - 1)) * s;
		double x = c * (1 - Math.Abs((h / 60 % 2) - 1));
		double m = l - (c * 0.5);

		double r;
		double g;
		double b;
		if (h < 60)
		{
			r = c;
			g = x;
			b = 0;
		}
		else if (h < 120)
		{
			r = x;
			g = c;
			b = 0;
		}
		else if (h < 180)
		{
			r = 0;
			g = c;
			b = x;
		}
		else if (h < 240)
		{
			r = 0;
			g = x;
			b = c;
		}
		else if (h < 300)
		{
			r = x;
			g = 0;
			b = c;
		}
		else // h < 360
		{
			r = c;
			g = 0;
			b = x;
		}

		return Color.FromArgb((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
	}

	public override bool Equals(object? obj)
	{
		if (obj is HSLColor other)
		{
			return Hue == other.Hue && Saturation == other.Saturation && Lightness == other.Lightness;
		}
		return false;
	}
	public override int GetHashCode()
	{
		return HashCode.Combine(Hue, Saturation, Lightness);
	}

	public override string ToString()
	{
		return $"{Hue * 360}° {Saturation:P} {Lightness:P}";
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
