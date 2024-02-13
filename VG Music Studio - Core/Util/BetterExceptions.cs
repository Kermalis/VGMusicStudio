using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.Util;

internal sealed class InvalidValueException : Exception
{
	public object Value { get; }

	public InvalidValueException(object value, string message)
		: base(message)
	{
		Value = value;
	}
}
internal sealed class BetterKeyNotFoundException : KeyNotFoundException
{
	public object Key { get; }

	public BetterKeyNotFoundException(object key, Exception? innerException)
		: base($"\"{key}\" was not present in the dictionary.", innerException)
	{
		Key = key;
	}
}
