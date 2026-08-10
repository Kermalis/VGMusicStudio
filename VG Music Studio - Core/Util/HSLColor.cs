using System;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.Util;

// https://www.rapidtables.com/convert/color/rgb-to-hsl.html
// https://www.rapidtables.com/convert/color/hsl-to-rgb.html
// Currently being used for the Cairo colors in the GTK4 GUI, as it uses 0.0 to 1.0, like OpenGL.
// It's also useful for OpenGL-specific tasks too
public readonly struct HSLColor
{
	/// <summary>[0, 1)</summary>
	public readonly double Hue;
	/// <summary>[0, 1]</summary>
	public readonly double Saturation;
	/// <summary>[0, 1]</summary>
	public readonly double Lightness;
	/// <summary>[0, 1]</summary>
	public readonly double R;
	/// <summary>[0, 1]</summary>
	public readonly double G;
	/// <summary>[0, 1]</summary>
	public readonly double B;

	public HSLColor(double h, double s, double l)
	{
		Hue = h;
		Saturation = s;
		Lightness = l;
		
		ToRGB(h, s, l, out R, out G, out B);
	}
	public HSLColor(in Color c)
	{
		R = c.R / 255.0;
		G = c.G / 255.0;
		B = c.B / 255.0;

		double max = Math.Max(Math.Max(R, G), B);
		double min = Math.Min(Math.Min(R, G), B);
		double delta = max - min;

		Lightness = (min + max) * 0.5;

		if (delta == 0)
		{
			Hue = 0;
		}
		else if (max == R)
		{
			Hue = (G - B) / delta % 6 / 6;
		}
		else if (max == G)
		{
			Hue = (((B - R) / delta) + 2) / 6;
		}
		else // max == B
		{
			Hue = (((R - G) / delta) + 4) / 6;
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
	public static void ToRGB(double h, double s, double l, out double r, out double g, out double b)
	{
		h *= 360;

		double c = (1 - Math.Abs((2 * l) - 1)) * s;
		double x = c * (1 - Math.Abs((h / 60 % 2) - 1));
		double m = l - (c * 0.5);

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

		r += m;
		g += m;
		b += m;
	}
	public static Color ToColor(double h, double s, double l)
	{
		ToRGB(h, s, l, out double r, out double g, out double b);
		return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
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
