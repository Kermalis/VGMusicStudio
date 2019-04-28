using Kermalis.VGMusicStudio.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Util
{
    internal static class Utils
    {
        public const string ProgramName = "VG Music Studio";

        private static readonly Random rng = new Random();

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            return val.CompareTo(min) < 0 ? min : val.CompareTo(max) > 0 ? max : val;
        }
        public static bool TryParseValue(string value, long minRange, long maxRange, out long outValue)
        {
            try { outValue = ParseValue(string.Empty, value, minRange, maxRange); return true; }
            catch { outValue = 0; return false; }
        }
        /// <exception cref="InvalidValueException" />
        public static long ParseValue(string valueName, string value, long minRange, long maxRange)
        {
            string GetMessage()
            {
                return $"\"{valueName}\" must be between {minRange} and {maxRange}.";
            }

            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x") && long.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out long hexp))
            {
                if (hexp < minRange || hexp > maxRange)
                {
                    throw new InvalidValueException(hexp, GetMessage());
                }
                return hexp;
            }
            else if (long.TryParse(value, NumberStyles.Integer, provider, out long dec))
            {
                if (dec < minRange || dec > maxRange)
                {
                    throw new InvalidValueException(dec, GetMessage());
                }
                return dec;
            }
            else if (long.TryParse(value, NumberStyles.HexNumber, provider, out long hex))
            {
                if (hex < minRange || hex > maxRange)
                {
                    throw new InvalidValueException(hex, GetMessage());
                }
                return hex;
            }
            throw new InvalidValueException(value, $"\"{valueName}\" is not a value.");
        }
        /// <exception cref="BetterKeyNotFoundException" />
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            try
            {
                return dictionary[key];
            }
            catch (KeyNotFoundException ex)
            {
                throw new BetterKeyNotFoundException(key, ex.InnerException);
            }
        }
        /// <exception cref="BetterKeyNotFoundException" />
        /// <exception cref="InvalidValueException" />
        public static long GetValidValue(this YamlMappingNode yamlNode, string key, long minRange, long maxRange)
        {
            return ParseValue(key, yamlNode.Children.GetValue(key).ToString(), minRange, maxRange);
        }
        /// <exception cref="BetterKeyNotFoundException" />
        /// <exception cref="Exception" />
        public static bool GetValidBoolean(this YamlMappingNode yamlNode, string key)
        {
            if (bool.TryParse(yamlNode.Children.GetValue(key).ToString(), out bool value))
            {
                return value;
            }
            else
            {
                throw new InvalidValueException(key, $"\"{key}\" must be True or False.");
            }
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

        private static string[] notes = null;
        public static string GetNoteName(byte note)
        {
            if (notes == null)
            {
                notes = Strings.Notes.Split(';');
            }
            return notes[note % 12] + ((note / 12) - 2);
        }
    }
}
