using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GBAMusicStudio.Core
{
    internal class Assembler
    {
        private class Pair
        {
            internal bool Global;
            internal int Offset;
        }
        private class Pointer
        {
            internal string Label;
            internal int Offset;
            internal Pointer(string l, int o)
            {
                Label = l;
                Offset = o;
            }
        }
        readonly string fileErrorFormat = "{0}{3}{3}Error reading file included in line {1}:{3}{2}",
            mathErrorFormat = "{0}{3}{3}Error parsing value in line {1} (Are you missing a definition?):{3}{2}",
            cmdErrorFormat = "{0}{3}{3}Unknown command in line {1}:{3}\"{2}\"";

        internal uint BaseOffset { get; private set; } = ROM.Pak;
        List<string> loaded = new List<string>();
        Dictionary<string, int> defines;

        Dictionary<string, Pair> labels = new Dictionary<string, Pair>();
        List<Pointer> lPointers = new List<Pointer>();
        List<byte> bytes = new List<byte>();

        internal readonly string FileName;
        internal int this[string Label]
        {
            get => labels[FixLabel(Label)].Offset;
        }
        internal byte[] Binary => bytes.ToArray();
        internal int BinaryLength => bytes.Count;

        internal Assembler(string fileName, uint baseOffset = ROM.Pak, Dictionary<string, int> initialDefines = null)
        {
            FileName = fileName;
            defines = initialDefines ?? new Dictionary<string, int>();
            Console.WriteLine(Read(fileName));
            SetBaseOffset(baseOffset);
        }

        internal void SetBaseOffset(uint baseOffset)
        {
            if (BaseOffset == baseOffset) return;
            foreach (var p in lPointers)
            {
                // Our example label is SEQ_STUFF at the binary offset 0x1000, curBase is 0x500, baseOffset is 0x1800
                // There is a pointer (p) to SEQ_STUFF at the binary offset 0x1DF4
                uint old = BitConverter.ToUInt32(Binary, p.Offset); // If there was a pointer to "SEQ_STUFF+4", the pointer would be 0x1504, at binary offset 0x1DF4
                var off = old - BaseOffset; // Then off is 0x1004 (SEQ_STUFF+4)
                var b = BitConverter.GetBytes(baseOffset + off); // b will contain {0x04, 0x28, 0x00, 0x00} [0x2804] (SEQ_STUFF+4 + baseOffset)
                for (int i = 0; i < 4; i++)
                    bytes[p.Offset + i] = b[i]; // Copy the new pointer to binary offset 0x1DF4
            }
            BaseOffset = baseOffset;
        }

        internal static string FixLabel(string label)
        {
            string ret = "";
            for (int i = 0; i < label.Length; i++)
            {
                if ((label[i] >= 'a' && label[i] <= 'z') ||
                    (label[i] >= 'A' && label[i] <= 'Z') ||
                    (label[i] >= '0' && label[i] <= '9' && i > 0))
                    ret += label[i];
                else
                    ret += '_';
            }
            return ret;
        }

        // Returns a status
        string Read(string fileName)
        {
            if (loaded.Contains(fileName)) return $"{fileName} was already loaded";

            string[] file = File.ReadAllLines(fileName);
            loaded.Add(fileName);

            for (int i = 0; i < file.Length; i++)
            {
                string line = file[i];
                if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

                bool readingCMD = false; // If it's reading the command
                string cmd = null;
                var args = new List<string>();
                string str = "";
                foreach (char c in line)
                {
                    if (c == '@') break; // Ignore comments from this point
                    else if (c == '.' && cmd == null)
                    {
                        readingCMD = true;
                    }
                    else if (c == ':') // Labels
                    {
                        if (!labels.ContainsKey(str))
                            labels.Add(str, new Pair());
                        labels[str].Offset = bytes.Count;
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
                if (cmd == null) continue; // Commented line
                args.Add(str); // Add last string before the newline

                switch (cmd.ToLower())
                {
                    case "include":
                        try
                        {
                            Read(args[0].Replace("\"", string.Empty));
                        }
                        catch
                        {
                            throw new IOException(string.Format(fileErrorFormat, fileName, i, args[0], Environment.NewLine));
                        }
                        break;
                    case "equ":
                        try
                        {
                            defines.Add(args[0], ParseInt(args[1]));
                        }
                        catch
                        {
                            throw new ArithmeticException(string.Format(mathErrorFormat, fileName, i, line, Environment.NewLine));
                        }
                        break;
                    case "global":
                        if (!labels.ContainsKey(args[0]))
                            labels.Add(args[0], new Pair());
                        labels[args[0]].Global = true;
                        break;
                    case "align":
                        int align = ParseInt(args[0]);
                        for (int a = BinaryLength % align; a < align; a++)
                            bytes.Add(0);
                        break;
                    case "byte":
                        try
                        {
                            foreach (var a in args)
                                bytes.Add((byte)ParseInt(a));
                        }
                        catch
                        {
                            throw new ArithmeticException(string.Format(mathErrorFormat, fileName, i, line, Environment.NewLine));
                        }
                        break;
                    case "hword":
                        try
                        {
                            foreach (var a in args)
                                bytes.AddRange(BitConverter.GetBytes((short)ParseInt(a)));
                        }
                        catch
                        {
                            throw new ArithmeticException(string.Format(mathErrorFormat, fileName, i, line, Environment.NewLine));
                        }
                        break;
                    case "int":
                    case "word":
                        try
                        {
                            foreach (var a in args)
                                bytes.AddRange(BitConverter.GetBytes(ParseInt(a)));
                        }
                        catch
                        {
                            throw new ArithmeticException(string.Format(mathErrorFormat, fileName, i, line, Environment.NewLine));
                        }
                        break;
                    case "end":
                        goto end;
                    case "section": // Ignore
                        break;
                    default: throw new NotSupportedException(string.Format(cmdErrorFormat, fileName, i, cmd, Environment.NewLine));
                }
            }
            end:
            return $"{fileName} loaded with no issues";
        }

        int ParseInt(string value)
        {
            // First try regular values like "40" and "0x20"
            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x"))
                if (int.TryParse(value.Substring(2), NumberStyles.HexNumber, provider, out int hex))
                    return hex;
            if (int.TryParse(value, NumberStyles.Integer, provider, out int dec))
                return dec;

            // Then check if it's defined
            if (defines.TryGetValue(value, out int def)) return def;
            if (labels.TryGetValue(value, out Pair pair))
            {
                lPointers.Add(new Pointer(value, bytes.Count));
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

                if (char.IsWhiteSpace(c)) continue; // White space does nothing here
                else if (c == '+' || c == '-' || c == '*' || c == '/')
                {
                    if (add) ret += ParseInt(str);
                    else if (sub) ret -= ParseInt(str);
                    else if (mul) ret *= ParseInt(str);
                    else if (div) ret /= ParseInt(str);
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
                if (add) ret += ParseInt(str); // Handle last
                else if (sub) ret -= ParseInt(str);
                else if (mul) ret *= ParseInt(str);
                else if (div) ret /= ParseInt(str);
                return ret;
            }

            // If not then RIP
            throw new ArgumentException("\"value\" was invalid.");
        }
    }
}
