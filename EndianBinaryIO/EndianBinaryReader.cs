using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Kermalis.EndianBinaryIO
{
    public class EndianBinaryReader
    {
        public Stream BaseStream { get; }
        private Endianness _endianness;
        public Endianness Endianness
        {
            get => _endianness;
            set
            {
                if (value >= Endianness.MAX)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _endianness = value;
            }
        }
        private BooleanSize _booleanSize;
        public BooleanSize BooleanSize
        {
            get => _booleanSize;
            set
            {
                if (value >= BooleanSize.MAX)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _booleanSize = value;
            }
        }
        public Encoding Encoding { get; set; }

        private byte[] _buffer;

        public EndianBinaryReader(Stream baseStream, Endianness endianness = Endianness.LittleEndian, BooleanSize booleanSize = BooleanSize.U8)
        {
            if (baseStream is null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }
            if (!baseStream.CanRead)
            {
                throw new ArgumentException(nameof(baseStream));
            }
            BaseStream = baseStream;
            Endianness = endianness;
            BooleanSize = booleanSize;
            Encoding = Encoding.ASCII;
        }
        public EndianBinaryReader(Stream baseStream, Encoding encoding, Endianness endianness = Endianness.LittleEndian, BooleanSize booleanSize = BooleanSize.U8)
        {
            if (baseStream is null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }
            if (!baseStream.CanRead)
            {
                throw new ArgumentException(nameof(baseStream));
            }
            BaseStream = baseStream;
            Endianness = endianness;
            BooleanSize = booleanSize;
            Encoding = encoding;
        }

        private void ReadBytesIntoBuffer(int byteCount)
        {
            if (_buffer is null || _buffer.Length < byteCount)
            {
                _buffer = new byte[byteCount];
            }
            if (BaseStream.Read(_buffer, 0, byteCount) != byteCount)
            {
                throw new EndOfStreamException();
            }
        }
        private char[] DecodeChars(Encoding encoding, int charCount)
        {
            Utils.ThrowIfCannotUseEncoding(encoding);
            int maxBytes = encoding.GetMaxByteCount(charCount);
            byte[] buffer = new byte[maxBytes];
            int amtRead = BaseStream.Read(buffer, 0, maxBytes); // Do not throw EndOfStreamException if there aren't enough bytes at the end of the stream
            if (amtRead == 0)
            {
                throw new EndOfStreamException();
            }
            // If the maxBytes would be 4, and the string only takes 2, we'd not have enough bytes, but if it's a proper string it doesn't matter
            char[] chars = encoding.GetChars(buffer);
            if (chars.Length < charCount)
            {
                throw new InvalidDataException(); // Too few chars means the decoding went wrong
            }
            // If we read too many chars, we need to shrink the array
            // For example, if we want 1 char and the max bytes is 2, but we manage to read 2 1-byte chars, we'd want to shrink back to 1 char
            Array.Resize(ref chars, charCount);
            int actualBytes = encoding.GetByteCount(chars);
            if (amtRead != actualBytes)
            {
                BaseStream.Position -= amtRead - actualBytes; // Set the stream back to compensate for the extra bytes we read
            }
            return chars;
        }

        public byte PeekByte()
        {
            long pos = BaseStream.Position;
            byte b = ReadByte();
            BaseStream.Position = pos;
            return b;
        }
        public byte PeekByte(long offset)
        {
            BaseStream.Position = offset;
            return PeekByte();
        }
        public byte[] PeekBytes(int count)
        {
            long pos = BaseStream.Position;
            byte[] b = ReadBytes(count);
            BaseStream.Position = pos;
            return b;
        }
        public byte[] PeekBytes(int count, long offset)
        {
            BaseStream.Position = offset;
            return PeekBytes(count);
        }
        public char PeekChar()
        {
            long pos = BaseStream.Position;
            char c = ReadChar();
            BaseStream.Position = pos;
            return c;
        }
        public char PeekChar(long offset)
        {
            BaseStream.Position = offset;
            return PeekChar();
        }
        public char PeekChar(Encoding encoding)
        {
            long pos = BaseStream.Position;
            char c = ReadChar(encoding);
            BaseStream.Position = pos;
            return c;
        }
        public char PeekChar(Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return PeekChar(encoding);
        }

        public bool ReadBoolean()
        {
            return ReadBoolean(BooleanSize);
        }
        public bool ReadBoolean(long offset)
        {
            BaseStream.Position = offset;
            return ReadBoolean(BooleanSize);
        }
        public bool ReadBoolean(BooleanSize booleanSize)
        {
            switch (booleanSize)
            {
                case BooleanSize.U8:
                {
                    ReadBytesIntoBuffer(1);
                    return _buffer[0] != 0;
                }
                case BooleanSize.U16:
                {
                    ReadBytesIntoBuffer(2);
                    return EndianBitConverter.BytesToInt16(_buffer, 0, Endianness) != 0;
                }
                case BooleanSize.U32:
                {
                    ReadBytesIntoBuffer(4);
                    return EndianBitConverter.BytesToInt32(_buffer, 0, Endianness) != 0;
                }
                default: throw new ArgumentOutOfRangeException(nameof(booleanSize));
            }
        }
        public bool ReadBoolean(BooleanSize booleanSize, long offset)
        {
            BaseStream.Position = offset;
            return ReadBoolean(booleanSize);
        }
        public bool[] ReadBooleans(int count)
        {
            return ReadBooleans(count, BooleanSize);
        }
        public bool[] ReadBooleans(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadBooleans(count, BooleanSize);
        }
        public bool[] ReadBooleans(int count, BooleanSize size)
        {
            if (!Utils.ValidateReadArraySize(count, out bool[] array))
            {
                array = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadBoolean(size);
                }
            }
            return array;
        }
        public bool[] ReadBooleans(int count, BooleanSize size, long offset)
        {
            BaseStream.Position = offset;
            return ReadBooleans(count, size);
        }
        public byte ReadByte()
        {
            ReadBytesIntoBuffer(1);
            return _buffer[0];
        }
        public byte ReadByte(long offset)
        {
            BaseStream.Position = offset;
            return ReadByte();
        }
        public byte[] ReadBytes(int count)
        {
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                ReadBytesIntoBuffer(count);
                array = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = _buffer[i];
                }
            }
            return array;
        }
        public byte[] ReadBytes(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadBytes(count);
        }
        public sbyte ReadSByte()
        {
            ReadBytesIntoBuffer(1);
            return (sbyte)_buffer[0];
        }
        public sbyte ReadSByte(long offset)
        {
            BaseStream.Position = offset;
            return ReadSByte();
        }
        public sbyte[] ReadSBytes(int count)
        {
            if (!Utils.ValidateReadArraySize(count, out sbyte[] array))
            {
                ReadBytesIntoBuffer(count);
                array = new sbyte[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = (sbyte)_buffer[i];
                }
            }
            return array;
        }
        public sbyte[] ReadSBytes(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadSBytes(count);
        }
        public char ReadChar()
        {
            return ReadChar(Encoding);
        }
        public char ReadChar(long offset)
        {
            BaseStream.Position = offset;
            return ReadChar();
        }
        public char ReadChar(Encoding encoding)
        {
            return DecodeChars(encoding, 1)[0];
        }
        public char ReadChar(Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return ReadChar(encoding);
        }
        public char[] ReadChars(int count, bool trimNullTerminators)
        {
            return ReadChars(count, trimNullTerminators, Encoding);
        }
        public char[] ReadChars(int count, bool trimNullTerminators, long offset)
        {
            BaseStream.Position = offset;
            return ReadChars(count, trimNullTerminators);
        }
        public char[] ReadChars(int count, bool trimNullTerminators, Encoding encoding)
        {
            if (Utils.ValidateReadArraySize(count, out char[] array))
            {
                return array;
            }
            array = DecodeChars(encoding, count);
            if (trimNullTerminators)
            {
                int i = Array.IndexOf(array, '\0');
                if (i != -1)
                {
                    Array.Resize(ref array, i);
                }
            }
            return array;
        }
        public char[] ReadChars(int count, bool trimNullTerminators, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return ReadChars(count, trimNullTerminators, encoding);
        }
        public string ReadStringNullTerminated()
        {
            return ReadStringNullTerminated(Encoding);
        }
        public string ReadStringNullTerminated(long offset)
        {
            BaseStream.Position = offset;
            return ReadStringNullTerminated();
        }
        public string ReadStringNullTerminated(Encoding encoding)
        {
            string text = string.Empty;
            while (true)
            {
                char c = ReadChar(encoding);
                if (c == '\0')
                {
                    break;
                }
                text += c;
            }
            return text;
        }
        public string ReadStringNullTerminated(Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return ReadStringNullTerminated(encoding);
        }
        public string ReadString(int charCount, bool trimNullTerminators)
        {
            return ReadString(charCount, trimNullTerminators, Encoding);
        }
        public string ReadString(int charCount, bool trimNullTerminators, long offset)
        {
            BaseStream.Position = offset;
            return ReadString(charCount, trimNullTerminators);
        }
        public string ReadString(int charCount, bool trimNullTerminators, Encoding encoding)
        {
            return new string(ReadChars(charCount, trimNullTerminators, encoding));
        }
        public string ReadString(int charCount, bool trimNullTerminators, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return ReadString(charCount, trimNullTerminators, encoding);
        }
        public string[] ReadStringsNullTerminated(int count)
        {
            return ReadStringsNullTerminated(count, Encoding);
        }
        public string[] ReadStringsNullTerminated(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadStringsNullTerminated(count);
        }
        public string[] ReadStringsNullTerminated(int count, Encoding encoding)
        {
            if (!Utils.ValidateReadArraySize(count, out string[] array))
            {
                array = new string[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadStringNullTerminated(encoding);
                }
            }
            return array;
        }
        public string[] ReadStringsNullTerminated(int count, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return ReadStringsNullTerminated(count, encoding);
        }
        public string[] ReadStrings(int count, int charCount, bool trimNullTerminators)
        {
            return ReadStrings(count, charCount, trimNullTerminators, Encoding);
        }
        public string[] ReadStrings(int count, int charCount, bool trimNullTerminators, long offset)
        {
            BaseStream.Position = offset;
            return ReadStrings(count, charCount, trimNullTerminators);
        }
        public string[] ReadStrings(int count, int charCount, bool trimNullTerminators, Encoding encoding)
        {
            if (!Utils.ValidateReadArraySize(count, out string[] array))
            {
                array = new string[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadString(charCount, trimNullTerminators, encoding);
                }
            }
            return array;
        }
        public string[] ReadStrings(int count, int charCount, bool trimNullTerminators, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            return ReadStrings(count, charCount, trimNullTerminators, encoding);
        }
        public short ReadInt16()
        {
            ReadBytesIntoBuffer(2);
            return EndianBitConverter.BytesToInt16(_buffer, 0, Endianness);
        }
        public short ReadInt16(long offset)
        {
            BaseStream.Position = offset;
            return ReadInt16();
        }
        public short[] ReadInt16s(int count)
        {
            ReadBytesIntoBuffer(count * 2);
            return EndianBitConverter.BytesToInt16s(_buffer, 0, count, Endianness);
        }
        public short[] ReadInt16s(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadInt16s(count);
        }
        public ushort ReadUInt16()
        {
            ReadBytesIntoBuffer(2);
            return (ushort)EndianBitConverter.BytesToInt16(_buffer, 0, Endianness);
        }
        public ushort ReadUInt16(long offset)
        {
            BaseStream.Position = offset;
            return ReadUInt16();
        }
        public ushort[] ReadUInt16s(int count)
        {
            ReadBytesIntoBuffer(count * 2);
            return EndianBitConverter.BytesToUInt16s(_buffer, 0, count, Endianness);
        }
        public ushort[] ReadUInt16s(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadUInt16s(count);
        }
        public int ReadInt32()
        {
            ReadBytesIntoBuffer(4);
            return EndianBitConverter.BytesToInt32(_buffer, 0, Endianness);
        }
        public int ReadInt32(long offset)
        {
            BaseStream.Position = offset;
            return ReadInt32();
        }
        public int[] ReadInt32s(int count)
        {
            ReadBytesIntoBuffer(count * 4);
            return EndianBitConverter.BytesToInt32s(_buffer, 0, count, Endianness);
        }
        public int[] ReadInt32s(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadInt32s(count);
        }
        public uint ReadUInt32()
        {
            ReadBytesIntoBuffer(4);
            return (uint)EndianBitConverter.BytesToInt32(_buffer, 0, Endianness);
        }
        public uint ReadUInt32(long offset)
        {
            BaseStream.Position = offset;
            return ReadUInt32();
        }
        public uint[] ReadUInt32s(int count)
        {
            ReadBytesIntoBuffer(count * 4);
            return EndianBitConverter.BytesToUInt32s(_buffer, 0, count, Endianness);
        }
        public uint[] ReadUInt32s(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadUInt32s(count);
        }
        public long ReadInt64()
        {
            ReadBytesIntoBuffer(8);
            return EndianBitConverter.BytesToInt64(_buffer, 0, Endianness);
        }
        public long ReadInt64(long offset)
        {
            BaseStream.Position = offset;
            return ReadInt64();
        }
        public long[] ReadInt64s(int count)
        {
            ReadBytesIntoBuffer(count * 8);
            return EndianBitConverter.BytesToInt64s(_buffer, 0, count, Endianness);
        }
        public long[] ReadInt64s(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadInt64s(count);
        }
        public ulong ReadUInt64()
        {
            ReadBytesIntoBuffer(8);
            return (ulong)EndianBitConverter.BytesToInt64(_buffer, 0, Endianness);
        }
        public ulong ReadUInt64(long offset)
        {
            BaseStream.Position = offset;
            return ReadUInt64();
        }
        public ulong[] ReadUInt64s(int count)
        {
            ReadBytesIntoBuffer(count * 8);
            return EndianBitConverter.BytesToUInt64s(_buffer, 0, count, Endianness);
        }
        public ulong[] ReadUInt64s(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadUInt64s(count);
        }
        public float ReadSingle()
        {
            ReadBytesIntoBuffer(4);
            return EndianBitConverter.BytesToSingle(_buffer, 0, Endianness);
        }
        public float ReadSingle(long offset)
        {
            BaseStream.Position = offset;
            return ReadSingle();
        }
        public float[] ReadSingles(int count)
        {
            ReadBytesIntoBuffer(count * 4);
            return EndianBitConverter.BytesToSingles(_buffer, 0, count, Endianness);
        }
        public float[] ReadSingles(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadSingles(count);
        }
        public double ReadDouble()
        {
            ReadBytesIntoBuffer(8);
            return EndianBitConverter.BytesToDouble(_buffer, 0, Endianness);
        }
        public double ReadDouble(long offset)
        {
            BaseStream.Position = offset;
            return ReadDouble();
        }
        public double[] ReadDoubles(int count)
        {
            ReadBytesIntoBuffer(count * 8);
            return EndianBitConverter.BytesToDoubles(_buffer, 0, count, Endianness);
        }
        public double[] ReadDoubles(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadDoubles(count);
        }
        public decimal ReadDecimal()
        {
            ReadBytesIntoBuffer(16);
            return EndianBitConverter.BytesToDecimal(_buffer, 0, Endianness);
        }
        public decimal ReadDecimal(long offset)
        {
            BaseStream.Position = offset;
            return ReadDecimal();
        }
        public decimal[] ReadDecimals(int count)
        {
            ReadBytesIntoBuffer(count * 16);
            return EndianBitConverter.BytesToDecimals(_buffer, 0, count, Endianness);
        }
        public decimal[] ReadDecimals(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadDecimals(count);
        }

        // Do not allow writing abstract "Enum" because there is no way to know which underlying type to read
        // Yes "struct" restriction on reads
        public TEnum ReadEnum<TEnum>() where TEnum : struct, Enum
        {
            Type enumType = typeof(TEnum);
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            object value;
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte: value = ReadByte(); break;
                case TypeCode.SByte: value = ReadSByte(); break;
                case TypeCode.Int16: value = ReadInt16(); break;
                case TypeCode.UInt16: value = ReadUInt16(); break;
                case TypeCode.Int32: value = ReadInt32(); break;
                case TypeCode.UInt32: value = ReadUInt32(); break;
                case TypeCode.Int64: value = ReadInt64(); break;
                case TypeCode.UInt64: value = ReadUInt64(); break;
                default: throw new ArgumentOutOfRangeException(nameof(underlyingType));
            }
            return (TEnum)Enum.ToObject(enumType, value);
        }
        public TEnum ReadEnum<TEnum>(long offset) where TEnum : struct, Enum
        {
            BaseStream.Position = offset;
            return ReadEnum<TEnum>();
        }
        public TEnum[] ReadEnums<TEnum>(int count) where TEnum : struct, Enum
        {
            if (!Utils.ValidateReadArraySize(count, out TEnum[] array))
            {
                array = new TEnum[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadEnum<TEnum>();
                }
            }
            return array;
        }
        public TEnum[] ReadEnums<TEnum>(int count, long offset) where TEnum : struct, Enum
        {
            BaseStream.Position = offset;
            return ReadEnums<TEnum>(count);
        }

        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }
        public DateTime ReadDateTime(long offset)
        {
            BaseStream.Position = offset;
            return ReadDateTime();
        }
        public DateTime[] ReadDateTimes(int count)
        {
            if (!Utils.ValidateReadArraySize(count, out DateTime[] array))
            {
                array = new DateTime[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadDateTime();
                }
            }
            return array;
        }
        public DateTime[] ReadDateTimes(int count, long offset)
        {
            BaseStream.Position = offset;
            return ReadDateTimes(count);
        }

        public T ReadObject<T>() where T : new()
        {
            return (T)ReadObject(typeof(T));
        }
        public object ReadObject(Type objType)
        {
            Utils.ThrowIfCannotReadWriteType(objType);
            object obj = Activator.CreateInstance(objType);
            ReadIntoObject(obj);
            return obj;
        }
        public T ReadObject<T>(long offset) where T : new()
        {
            BaseStream.Position = offset;
            return ReadObject<T>();
        }
        public object ReadObject(Type objType, long offset)
        {
            BaseStream.Position = offset;
            return ReadObject(objType);
        }
        public void ReadIntoObject(IBinarySerializable obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            obj.Read(this);
        }
        public void ReadIntoObject(IBinarySerializable obj, long offset)
        {
            BaseStream.Position = offset;
            ReadIntoObject(obj);
        }
        public void ReadIntoObject(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (obj is IBinarySerializable bs)
            {
                bs.Read(this);
                return;
            }

            Type objType = obj.GetType();
            Utils.ThrowIfCannotReadWriteType(objType);

            // Get public non-static properties
            foreach (PropertyInfo propertyInfo in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (Utils.AttributeValueOrDefault<BinaryIgnoreAttribute, bool>(propertyInfo, false))
                {
                    continue; // Skip properties with BinaryIgnoreAttribute
                }

                Type propertyType = propertyInfo.PropertyType;
                object value;

                if (propertyType.IsArray)
                {
                    int arrayLength = Utils.GetArrayLength(obj, objType, propertyInfo);
                    // Get array type
                    Type elementType = propertyType.GetElementType();
                    if (arrayLength == 0)
                    {
                        value = Array.CreateInstance(elementType, 0); // Create 0 length array regardless of type
                    }
                    else
                    {
                        if (elementType.IsEnum)
                        {
                            elementType = Enum.GetUnderlyingType(elementType);
                        }
                        switch (Type.GetTypeCode(elementType))
                        {
                            case TypeCode.Boolean:
                            {
                                BooleanSize booleanSize = Utils.AttributeValueOrDefault<BinaryBooleanSizeAttribute, BooleanSize>(propertyInfo, BooleanSize);
                                value = ReadBooleans(arrayLength, booleanSize);
                                break;
                            }
                            case TypeCode.Byte: value = ReadBytes(arrayLength); break;
                            case TypeCode.SByte: value = ReadSBytes(arrayLength); break;
                            case TypeCode.Char:
                            {
                                Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                                bool trimNullTerminators = Utils.AttributeValueOrDefault<BinaryStringTrimNullTerminatorsAttribute, bool>(propertyInfo, false);
                                value = ReadChars(arrayLength, trimNullTerminators, encoding);
                                break;
                            }
                            case TypeCode.Int16: value = ReadInt16s(arrayLength); break;
                            case TypeCode.UInt16: value = ReadUInt16s(arrayLength); break;
                            case TypeCode.Int32: value = ReadInt32s(arrayLength); break;
                            case TypeCode.UInt32: value = ReadUInt32s(arrayLength); break;
                            case TypeCode.Int64: value = ReadInt64s(arrayLength); break;
                            case TypeCode.UInt64: value = ReadUInt64s(arrayLength); break;
                            case TypeCode.Single: value = ReadSingles(arrayLength); break;
                            case TypeCode.Double: value = ReadDoubles(arrayLength); break;
                            case TypeCode.Decimal: value = ReadDecimals(arrayLength); break;
                            case TypeCode.DateTime: value = ReadDateTimes(arrayLength); break;
                            case TypeCode.String:
                            {
                                Utils.GetStringLength(obj, objType, propertyInfo, true, out bool? nullTerminated, out int stringLength);
                                Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                                if (nullTerminated == true)
                                {
                                    value = ReadStringsNullTerminated(arrayLength, encoding);
                                }
                                else
                                {
                                    bool trimNullTerminators = Utils.AttributeValueOrDefault<BinaryStringTrimNullTerminatorsAttribute, bool>(propertyInfo, false);
                                    value = ReadStrings(arrayLength, stringLength, trimNullTerminators, encoding);
                                }
                                break;
                            }
                            case TypeCode.Object:
                            {
                                value = Array.CreateInstance(elementType, arrayLength);
                                if (typeof(IBinarySerializable).IsAssignableFrom(elementType))
                                {
                                    for (int i = 0; i < arrayLength; i++)
                                    {
                                        var serializable = (IBinarySerializable)Activator.CreateInstance(elementType);
                                        serializable.Read(this);
                                        ((Array)value).SetValue(serializable, i);
                                    }
                                }
                                else // Element's type is not supported so try to read the array's objects
                                {
                                    for (int i = 0; i < arrayLength; i++)
                                    {
                                        object elementObj = ReadObject(elementType);
                                        ((Array)value).SetValue(elementObj, i);
                                    }
                                }
                                break;
                            }
                            default: throw new ArgumentOutOfRangeException(nameof(elementType));
                        }
                    }
                }
                else
                {
                    if (propertyType.IsEnum)
                    {
                        propertyType = Enum.GetUnderlyingType(propertyType);
                    }
                    switch (Type.GetTypeCode(propertyType))
                    {
                        case TypeCode.Boolean:
                        {
                            BooleanSize booleanSize = Utils.AttributeValueOrDefault<BinaryBooleanSizeAttribute, BooleanSize>(propertyInfo, BooleanSize);
                            value = ReadBoolean(booleanSize);
                            break;
                        }
                        case TypeCode.Byte: value = ReadByte(); break;
                        case TypeCode.SByte: value = ReadSByte(); break;
                        case TypeCode.Char:
                        {
                            Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                            value = ReadChar(encoding);
                            break;
                        }
                        case TypeCode.Int16: value = ReadInt16(); break;
                        case TypeCode.UInt16: value = ReadUInt16(); break;
                        case TypeCode.Int32: value = ReadInt32(); break;
                        case TypeCode.UInt32: value = ReadUInt32(); break;
                        case TypeCode.Int64: value = ReadInt64(); break;
                        case TypeCode.UInt64: value = ReadUInt64(); break;
                        case TypeCode.Single: value = ReadSingle(); break;
                        case TypeCode.Double: value = ReadDouble(); break;
                        case TypeCode.Decimal: value = ReadDecimal(); break;
                        case TypeCode.DateTime: value = ReadDateTime(); break;
                        case TypeCode.String:
                        {
                            Utils.GetStringLength(obj, objType, propertyInfo, true, out bool? nullTerminated, out int stringLength);
                            Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                            if (nullTerminated == true)
                            {
                                value = ReadStringNullTerminated(encoding);
                            }
                            else
                            {
                                bool trimNullTerminators = Utils.AttributeValueOrDefault<BinaryStringTrimNullTerminatorsAttribute, bool>(propertyInfo, false);
                                value = ReadString(stringLength, trimNullTerminators, encoding);
                            }
                            break;
                        }
                        case TypeCode.Object:
                        {
                            if (typeof(IBinarySerializable).IsAssignableFrom(propertyType))
                            {
                                value = Activator.CreateInstance(propertyType);
                                ((IBinarySerializable)value).Read(this);
                            }
                            else // The property's type is not supported so try to read the object
                            {
                                value = ReadObject(propertyType);
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(propertyType));
                    }
                }

                // Set the value into the property
                propertyInfo.SetValue(obj, value);
            }
        }
        public void ReadIntoObject(object obj, long offset)
        {
            BaseStream.Position = offset;
            ReadIntoObject(obj);
        }
    }
}
