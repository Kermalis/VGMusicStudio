using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GBAMusicStudio.Util
{
    public static class Utils
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static uint ParseUInt(string value)
        {
            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x"))
                if (uint.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out uint hex))
                    return hex;
            if (uint.TryParse(value, NumberStyles.Integer, provider, out uint dec))
                return dec;
            throw new ArgumentException("\"value\" was invalid.");
        }

        public static IEnumerable<T> UniteAll<T>(this IEnumerable<IEnumerable<T>> source)
        {
            IEnumerable<T> output = new T[0];
            foreach (IEnumerable<T> i in source)
                output = output.Union(i);
            return output;
        }

        public static string ToSafeFileName(this string s)
        {
            return s
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("\"", "")
                .Replace("*", "")
                .Replace(":", "")
                .Replace("?", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("|", "");
        }
    }
}
