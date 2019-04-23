using Kermalis.VGMusicStudio.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Kermalis.VGMusicStudio.Util
{
    static class Utils
    {
        public const string ProgramName = "VG Music Studio";

        static readonly Random rng = new Random();

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }
            else if (val.CompareTo(max) > 0)
            {
                return max;
            }
            else
            {
                return val;
            }
        }
        public static bool TryParseValue(string value, out long outValue)
        {
            try { outValue = ParseValue(value); return true; }
            catch { outValue = 0; return false; }
        }
        public static long ParseValue(string value)
        {
            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x"))
            {
                if (long.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out long hexp))
                {
                    return hexp;
                }
            }
            if (long.TryParse(value, NumberStyles.Integer, provider, out long dec))
            {
                return dec;
            }
            if (long.TryParse(value, NumberStyles.HexNumber, provider, out long hex))
            {
                return hex;
            }
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        public static IEnumerable<T> UniteAll<T>(this IEnumerable<IEnumerable<T>> source)
        {
            IEnumerable<T> output = new T[0];
            foreach (IEnumerable<T> i in source)
            {
                output = output.Union(i);
            }
            return output;
        }
        public static string Print<T>(this IEnumerable<T> source, bool parenthesis = true)
        {
            string str = parenthesis ? "( " : "";
            str += string.Join(", ", source);
            str += parenthesis ? " )" : "";
            return str;
        }
        // Fisher-Yates Shuffle
        public static void Shuffle<T>(this IList<T> source)
        {
            for (int a = 0; a < source.Count - 1; a++)
            {
                int b = rng.Next(a, source.Count);
                T value = source[a];
                source[a] = source[b];
                source[b] = value;
            }
        }

        static string[] notes = null;
        public static string GetNoteName(byte note)
        {
            if (notes == null)
            {
                notes = Strings.Notes.Split(';');
            }
            if (note < 0)
            {
                return note.ToString();
            }
            return notes[note % 12] + ((note / 12) - 2);
        }
    }
}
