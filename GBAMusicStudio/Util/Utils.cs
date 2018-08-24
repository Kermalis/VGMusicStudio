using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Util
{
    static class Utils
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
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
                if (long.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out long hexp))
                    return hexp;
            if (long.TryParse(value, NumberStyles.Integer, provider, out long dec))
                return dec;
            if (long.TryParse(value, NumberStyles.HexNumber, provider, out long hex))
                return hex;
            throw new ArgumentException("\"value\" was invalid.");
        }

        public static T ReadStruct<T>(byte[] binary, uint offset = 0)
        {
            using (var reader = new BinaryReader(new MemoryStream(binary)))
            {
                reader.BaseStream.Position = offset;
                byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                T ret = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                handle.Free();
                return ret;
            }
        }

        public static IEnumerable<T> UniteAll<T>(this IEnumerable<IEnumerable<T>> source)
        {
            IEnumerable<T> output = new T[0];
            foreach (IEnumerable<T> i in source)
                output = output.Union(i);
            return output;
        }
        public static string Print<T>(this IEnumerable<T> arr, bool parenthesis = true)
        {
            string str = parenthesis ? "( " : "";
            str += string.Join(", ", arr);
            str += parenthesis ? " )" : "";
            return str;
        }
    }
}
