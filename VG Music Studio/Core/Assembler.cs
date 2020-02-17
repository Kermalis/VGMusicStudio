using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Kermalis.VGMusicStudio.Core
{
    internal class Assembler
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

        public int BaseOffset { get; private set; }
        private readonly List<string> _loaded = new List<string>();
        private readonly Dictionary<string, int> _defines;

        private readonly Dictionary<string, Pair> _labels = new Dictionary<string, Pair>();
        private readonly List<Pointer> _lPointers = new List<Pointer>();
        private readonly List<byte> _bytes = new List<byte>();

        public string FileName { get; }
        public int this[string Label] => _labels[FixLabel(Label)].Offset;
        public byte[] Binary => _bytes.ToArray();
        public int BinaryLength => _bytes.Count;

        public Assembler(string fileName, int baseOffset, Dictionary<string, int> initialDefines = null)
        {
            FileName = fileName;
            _defines = initialDefines ?? new Dictionary<string, int>();
            Console.WriteLine(Read(fileName));
            SetBaseOffset(baseOffset);
        }

        public void SetBaseOffset(int baseOffset)
        {
            foreach (Pointer p in _lPointers)
            {
                // Our example label is SEQ_STUFF at the binary offset 0x1000, curBaseOffset is 0x500, baseOffset is 0x1800
                // There is a pointer (p) to SEQ_STUFF at the binary offset 0x1DFC
                int oldPointer = BitConverter.ToInt32(Binary, p.BinaryOffset); // If there was a pointer to "SEQ_STUFF+4", the pointer would be 0x1504, at binary offset 0x1DFC
                int labelOffset = oldPointer - BaseOffset; // Then labelOffset is 0x1004 (SEQ_STUFF+4)
                byte[] newPointerBytes = BitConverter.GetBytes(baseOffset + labelOffset); // b will contain {0x04, 0x28, 0x00, 0x00} [0x2804] (SEQ_STUFF+4 + baseOffset)
                for (int i = 0; i < 4; i++)
                {
                    _bytes[p.BinaryOffset + i] = newPointerBytes[i]; // Copy the new pointer to binary offset 0x1DF4
                }
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
                string str = "";
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
                        _labels[str].Offset = _bytes.Count;
                        str = "";
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        if (readingCMD) // If reading the command, otherwise do nothing
                        {
                            cmd = str;
                            readingCMD = false;
                            str = "";
                        }
                    }
                    else if (c == ',')
                    {
                        args.Add(str);
                        str = "";
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
                            _bytes.Add(0);
                        }
                        break;
                    }
                    case "byte":
                    {
                        try
                        {
                            foreach (string a in args)
                            {
                                _bytes.Add((byte)ParseInt(a));
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
                                _bytes.AddRange(BitConverter.GetBytes((short)ParseInt(a)));
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
                                _bytes.AddRange(BitConverter.GetBytes(ParseInt(a)));
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
            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x"))
            {
                if (int.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out int hex))
                {
                    return hex;
                }
            }
            if (int.TryParse(value, NumberStyles.Integer, provider, out int dec))
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
                _lPointers.Add(new Pointer { Label = value, BinaryOffset = _bytes.Count });
                return pair.Offset;
            }

            // Then check if it's math
            bool foundMath = false;
            string str = "";
            int ret = 0;
            bool add = true, sub = false, mul = false, div = false; // Add first, so the initial value is set
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (char.IsWhiteSpace(c)) // White space does nothing here
                {
                    continue;
                }
                else if (c == '+' || c == '-' || c == '*' || c == '/')
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
                    add = c == '+'; sub = c == '-'; mul = c == '*'; div = c == '/';
                    str = "";
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
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}
