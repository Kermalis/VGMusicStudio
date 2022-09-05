using Kermalis.VGMusicStudio.Core.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.Util;

public static class ConfigUtils
{
	public const string PROGRAM_NAME = "VG Music Studio";
	private static readonly string[] _notes = Strings.Notes.Split(';');
	private static readonly CultureInfo _enUS = new("en-US");

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

		if (value.StartsWith("0x") && long.TryParse(value.AsSpan(2), NumberStyles.HexNumber, _enUS, out long hexp))
		{
			if (hexp < minValue || hexp > maxValue)
			{
				throw new InvalidValueException(hexp, GetMessage());
			}
			return hexp;
		}
		else if (long.TryParse(value, NumberStyles.Integer, _enUS, out long dec))
		{
			if (dec < minValue || dec > maxValue)
			{
				throw new InvalidValueException(dec, GetMessage());
			}
			return dec;
		}
		else if (long.TryParse(value, NumberStyles.HexNumber, _enUS, out long hex))
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
	public static TEnum ParseEnum<TEnum>(string valueName, string value) where TEnum : unmanaged
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
	public static TEnum GetValidEnum<TEnum>(this YamlMappingNode yamlNode, string key) where TEnum : unmanaged
	{
		return ParseEnum<TEnum>(key, yamlNode.Children.GetValue(key).ToString());
	}

	public static string CombineWithBaseDirectory(string path)
	{
		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
	}

	public static string GetNoteName(int note)
	{
		return _notes[note];
	}
	// TODO: Cache results?
	public static string GetKeyName(int note)
	{
		return _notes[note % 12] + ((note / 12) + (GlobalConfig.Instance.MiddleCOctave - 5));
	}
}
