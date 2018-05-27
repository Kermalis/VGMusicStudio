using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GBAMusicStudio.Util
{
    public static class Utils
    {
        public static int ParseInt(string value)
        {
            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x")) value = value.Substring(2);
            if (int.TryParse(value, NumberStyles.HexNumber, provider, out int hex))
                return hex;
            if (int.TryParse(value, NumberStyles.Integer, provider, out int dec))
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
