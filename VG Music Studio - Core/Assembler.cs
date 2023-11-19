using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Kermalis.VGMusicStudio.Core;

internal sealed class Assembler : IDisposable
{
	private class Pair
	{
		public bool Global;
		public int Offset;
	}
	private class Pointer
	{
		public string Label;
		public int BinaryOffset;
	}
	private const string _fileErrorFormat = "{0}{3}{3}Error reading file included in line {1}:{3}{2}";
	private const string _mathErrorFormat = "{0}{3}{3}Error parsing value in line {1} (Are you missing a definition?):{3}{2}";
	private const string _cmdErrorFormat = "{0}{3}{3}Unknown command in line {1}:{3}\"{2}\"";

	private static readonly CultureInfo _enUS = new("en-US");

	public int BaseOffset { get; private set; }
	private readonly List<string> _loaded;
	private readonly Dictionary<string, int> _defines;

	private readonly Dictionary<string, Pair> _labels;
	private readonly List<Pointer> _lPointers;
	private readonly MemoryStream _stream;
	private readonly EndianBinaryWriter _writer;

	public string FileName { get; }
	public Endianness Endianness { get; }
	public int this[string Label] => _labels[FixLabel(Label)].Offset;
	public int BinaryLength => (int)_stream.Length;

	public Assembler(string fileName, int baseOffset, Endianness endianness, Dictionary<string, int>? initialDefines = null)
	{
		FileName = fileName;
		Endianness = endianness;
		_defines = initialDefines ?? new Dictionary<string, int>();
		_lPointers = new List<Pointer>();
		_labels = new Dictionary<string, Pair>();
		_loaded = new List<string>();

		_stream = new MemoryStream();
		_writer = new EndianBinaryWriter(_stream, endianness: endianness);

		Debug.WriteLine(Read(fileName));
		SetBaseOffset(baseOffset);
	}

	public void SetBaseOffset(int baseOffset)
	{
		Span<byte> span = stackalloc byte[4];
		foreach (Pointer p in _lPointers)
		{
			// Our example label is SEQ_STUFF at the binary offset 0x1000, curBaseOffset is 0x500, baseOffset is 0x1800
			// There is a pointer (p) to SEQ_STUFF at the binary offset 0x1DFC
			_stream.Position = p.BinaryOffset;
			_stream.Read(span);
			int oldPointer = EndianBinaryPrimitives.ReadInt32(span, Endianness); // If there was a pointer to "SEQ_STUFF+4", the pointer would be 0x1504, at binary offset 0x1DFC
			int labelOffset = oldPointer - BaseOffset; // Then labelOffset is 0x1004 (SEQ_STUFF+4)

			_stream.Position = p.BinaryOffset;
			_writer.WriteInt32(baseOffset + labelOffset); // b will contain {0x04, 0x28, 0x00, 0x00} [0x2804] (SEQ_STUFF+4 + baseOffset)
														  // Copy the new pointer to binary offset 0x1DF4
														  // TODO: UPDATE THESE OLD COMMENTS LOL
		}
		BaseOffset = baseOffset;
	}

	public static string FixLabel(string label)
	{
		string ret = "";
		for (int i = 0; i < label.Length; i++)
		{
			char c = label[i];
			if ((c >= 'a' && c <= 'z') ||
				(c >= 'A' && c <= 'Z') ||
				(c >= '0' && c <= '9' && i > 0))
			{
				ret += c;
			}
			else
			{
				ret += '_';
			}
		}
		return ret;
	}

	// Returns a status
	private string Read(string fileName)
	{
		if (_loaded.Contains(fileName))
		{
			return $"{fileName} was already loaded";
		}

		string[] file = File.ReadAllLines(fileName);
		_loaded.Add(fileName);

		for (int i = 0; i < file.Length; i++)
		{
			string line = file[i];
			if (string.IsNullOrWhiteSpace(line))
			{
				continue; // Skip empty lines
			}

			bool readingCMD = false; // If it's reading the command
			string cmd = null;
			var args = new List<string>();
			string str = string.Empty;
			foreach (char c in line)
			{
				if (c == '@') // Ignore comments from this point
				{
					break;
				}
				else if (c == '.' && cmd == null)
				{
					readingCMD = true;
				}
				else if (c == ':') // Labels
				{
					if (!_labels.ContainsKey(str))
					{
						_labels.Add(str, new Pair());
					}
					_labels[str].Offset = BinaryLength;
					str = string.Empty;
				}
				else if (char.IsWhiteSpace(c))
				{
					if (readingCMD) // If reading the command, otherwise do nothing
					{
						cmd = str;
						readingCMD = false;
						str = string.Empty;
					}
				}
				else if (c == ',')
				{
					args.Add(str);
					str = string.Empty;
				}
				else
				{
					str += c;
				}
			}
			if (cmd == null)
			{
				continue; // Commented line
			}

			args.Add(str); // Add last string before the newline

			switch (cmd.ToLower())
			{
				case "include":
				{
					try
					{
						Read(args[0].Replace("\"", string.Empty));
					}
					catch
					{
						throw new IOException(string.Format(_fileErrorFormat, fileName, i, args[0], Environment.NewLine));
					}
					break;
				}
				case "equ":
				{
					try
					{
						_defines.Add(args[0], ParseInt(args[1]));
					}
					catch
					{
						throw new ArithmeticException(string.Format(_mathErrorFormat, fileName, i, line, Environment.NewLine));
					}
					break;
				}
				case "global":
				{
					if (!_labels.ContainsKey(args[0]))
					{
						_labels.Add(args[0], new Pair());
					}
					_labels[args[0]].Global = true;
					break;
				}
				case "align":
				{
					int align = ParseInt(args[0]);
					for (int a = BinaryLength % align; a < align; a++)
					{
						_writer.WriteByte(0);
					}
					break;
				}
				case "byte":
				{
					try
					{
						foreach (string a in args)
						{
							_writer.WriteByte((byte)ParseInt(a));
						}
					}
					catch
					{
						throw new ArithmeticException(string.Format(_mathErrorFormat, fileName, i, line, Environment.NewLine));
					}
					break;
				}
				case "hword":
				{
					try
					{
						foreach (string a in args)
						{
							_writer.WriteInt16((short)ParseInt(a));
						}
					}
					catch
					{
						throw new ArithmeticException(string.Format(_mathErrorFormat, fileName, i, line, Environment.NewLine));
					}
					break;
				}
				case "int":
				case "word":
				{
					try
					{
						foreach (string a in args)
						{
							_writer.WriteInt32(ParseInt(a));
						}
					}
					catch
					{
						throw new ArithmeticException(string.Format(_mathErrorFormat, fileName, i, line, Environment.NewLine));
					}
					break;
				}
				case "end":
				{
					goto end;
				}
				case "section": // Ignore
				{
					break;
				}
				default: throw new NotSupportedException(string.Format(_cmdErrorFormat, fileName, i, cmd, Environment.NewLine));
			}
		}
	end:
		return $"{fileName} loaded with no issues";
	}

	private int ParseInt(string value)
	{
		// First try regular values like "40" and "0x20"
		if (value.StartsWith("0x") && int.TryParse(value.AsSpan(2), NumberStyles.HexNumber, _enUS, out int hex))
		{
			return hex;
		}
		if (int.TryParse(value, NumberStyles.Integer, _enUS, out int dec))
		{
			return dec;
		}
		// Then check if it's defined
		if (_defines.TryGetValue(value, out int def))
		{
			return def;
		}
		if (_labels.TryGetValue(value, out Pair pair))
		{
			_lPointers.Add(new Pointer { Label = value, BinaryOffset = BinaryLength });
			return pair.Offset;
		}

		// Then check if it's math
		bool foundMath = false;
		string str = string.Empty;
		int ret = 0;
		bool add = true; // Add first, so the initial value is set
		bool sub = false;
		bool mul = false;
		bool div = false;
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];

			if (char.IsWhiteSpace(c)) // White space does nothing here
			{
				continue;
			}
			if (c == '+' || c == '-' || c == '*' || c == '/')
			{
				if (add)
				{
					ret += ParseInt(str);
				}
				else if (sub)
				{
					ret -= ParseInt(str);
				}
				else if (mul)
				{
					ret *= ParseInt(str);
				}
				else if (div)
				{
					ret /= ParseInt(str);
				}
				add = c == '+';
				sub = c == '-';
				mul = c == '*';
				div = c == '/';
				str = string.Empty;
				foundMath = true;
			}
			else
			{
				str += c;
			}
		}

		if (foundMath)
		{
			if (add) // Handle last
			{
				ret += ParseInt(str);
			}
			else if (sub)
			{
				ret -= ParseInt(str);
			}
			else if (mul)
			{
				ret *= ParseInt(str);
			}
			else if (div)
			{
				ret /= ParseInt(str);
			}
			return ret;
		}

		throw new ArgumentOutOfRangeException(nameof(value));
	}

	public void Dispose()
	{
		_stream.Dispose();
	}
}
