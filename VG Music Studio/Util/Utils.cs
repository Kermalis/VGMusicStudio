using Kermalis.VGMusicStudio.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Util
{
    internal static class Utils
    {
        public const string ProgramName = "VG Music Studio";

        private static readonly Random rng = new Random();

        private static readonly string[] notes = null;
        static Utils()
        {
            notes = Strings.Notes.Split(';');
        }

        public static bool TryParseValue(string value, long minValue, long maxValue, out long outValue)
        {
            try { outValue = ParseValue(string.Empty, value, minValue, maxValue); return true; }
            catch { outValue = 0; return false; }
        }
        /// <exception cref="InvalidValueException" />
        public static long ParseValue(string valueName, string value, long minValue, long maxValue)
        {
            string GetMessage()
            {
                return string.Format(Strings.ErrorValueParseRanged, valueName, minValue, maxValue);
            }

            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x") && long.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out long hexp))
            {
                if (hexp < minValue || hexp > maxValue)
                {
                    throw new InvalidValueException(hexp, GetMessage());
                }
                return hexp;
            }
            else if (long.TryParse(value, NumberStyles.Integer, provider, out long dec))
            {
                if (dec < minValue || dec > maxValue)
                {
                    throw new InvalidValueException(dec, GetMessage());
                }
                return dec;
            }
            else if (long.TryParse(value, NumberStyles.HexNumber, provider, out long hex))
            {
                if (hex < minValue || hex > maxValue)
                {
                    throw new InvalidValueException(hex, GetMessage());
                }
                return hex;
            }
            throw new InvalidValueException(value, string.Format(Strings.ErrorValueParse, valueName));
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
        /// <exception cref="InvalidValueException" />
        public static bool GetValidBoolean(this YamlMappingNode yamlNode, string key)
        {
            if (bool.TryParse(yamlNode.Children.GetValue(key).ToString(), out bool value))
            {
                return value;
            }
            else
            {
                throw new InvalidValueException(key, string.Format(Strings.ErrorBoolParse, key));
            }
        }

        public static string Print<T>(this IEnumerable<T> source, bool parenthesis = true)
        {
            string str = parenthesis ? "( " : "";
            str += string.Join(", ", source);
            str += parenthesis ? " )" : "";
            return str;
        }
        /// <summary> Fisher-Yates Shuffle</summary>
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

        public static string GetPianoKeyName(int key)
        {
            return notes[key];
        }
        public static string GetNoteName(int key)
        {
            return notes[key % 12] + ((key / 12) - 2);
        }

        public static string CombineWithBaseDirectory(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}
