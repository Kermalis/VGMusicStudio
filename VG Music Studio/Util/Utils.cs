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

        private static readonly Random _rng = new Random();
        private static readonly string[] _notes = Strings.Notes.Split(';');
        private static readonly char[] _spaceArray = new char[1] { ' ' };

        public static bool TryParseValue(string value, long minValue, long maxValue, out long outValue)
        {
            try
            {
                outValue = ParseValue(string.Empty, value, minValue, maxValue);
                return true;
            }
            catch
            {
                outValue = default;
                return false;
            }
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
        /// <exception cref="InvalidValueException" />
        public static bool ParseBoolean(string valueName, string value)
        {
            if (!bool.TryParse(value, out bool result))
            {
                throw new InvalidValueException(value, string.Format(Strings.ErrorBoolParse, valueName));
            }
            return result;
        }
        /// <exception cref="InvalidValueException" />
        public static TEnum ParseEnum<TEnum>(string valueName, string value) where TEnum : struct
        {
            if (!Enum.TryParse(value, out TEnum result))
            {
                throw new InvalidValueException(value, string.Format(Strings.ErrorConfigKeyInvalid, valueName));
            }
            return result;
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
            return ParseBoolean(key, yamlNode.Children.GetValue(key).ToString());
        }
        /// <exception cref="BetterKeyNotFoundException" />
        /// <exception cref="InvalidValueException" />
        public static TEnum GetValidEnum<TEnum>(this YamlMappingNode yamlNode, string key) where TEnum : struct
        {
            return ParseEnum<TEnum>(key, yamlNode.Children.GetValue(key).ToString());
        }
        public static string[] SplitSpace(this string str, StringSplitOptions options)
        {
            return str.Split(_spaceArray, options);
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
                int b = _rng.Next(a, source.Count);
                T value = source[a];
                source[a] = source[b];
                source[b] = value;
            }
        }

        public static string GetPianoKeyName(int key)
        {
            return _notes[key];
        }
        public static string GetNoteName(int key)
        {
            return _notes[key % 12] + ((key / 12) - 2);
        }

        public static string CombineWithBaseDirectory(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}
