using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Kermalis.EndianBinaryIO
{
    public class EndianBinaryWriter
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

        public EndianBinaryWriter(Stream baseStream, Endianness endianness = Endianness.LittleEndian, BooleanSize booleanSize = BooleanSize.U8)
        {
            if (baseStream is null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }
            if (!baseStream.CanWrite)
            {
                throw new ArgumentException(nameof(baseStream));
            }
            BaseStream = baseStream;
            Endianness = endianness;
            BooleanSize = booleanSize;
            Encoding = Encoding.Default;
        }
        public EndianBinaryWriter(Stream baseStream, Encoding encoding, Endianness endianness = Endianness.LittleEndian, BooleanSize booleanSize = BooleanSize.U8)
        {
            if (baseStream is null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }
            if (!baseStream.CanWrite)
            {
                throw new ArgumentException(nameof(baseStream));
            }
            BaseStream = baseStream;
            Endianness = endianness;
            BooleanSize = booleanSize;
            Encoding = encoding;
        }

        private void SetBufferSize(int size)
        {
            if (_buffer is null || _buffer.Length < size)
            {
                _buffer = new byte[size];
            }
        }
        private void WriteBytesFromBuffer(int byteCount)
        {
            BaseStream.Write(_buffer, 0, byteCount);
        }

        public void Write(bool value)
        {
            Write(value, BooleanSize);
        }
        public void Write(bool value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, BooleanSize);
        }
        public void Write(bool value, BooleanSize booleanSize)
        {
            switch (booleanSize)
            {
                case BooleanSize.U8:
                {
                    SetBufferSize(1);
                    _buffer[0] = value ? (byte)1 : (byte)0;
                    WriteBytesFromBuffer(1);
                    break;
                }
                case BooleanSize.U16:
                {
                    _buffer = EndianBitConverter.Int16ToBytes(value ? (short)1 : (short)0, Endianness);
                    WriteBytesFromBuffer(2);
                    break;
                }
                case BooleanSize.U32:
                {
                    _buffer = EndianBitConverter.Int32ToBytes(value ? 1 : 0, Endianness);
                    WriteBytesFromBuffer(4);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(booleanSize));
            }
        }
        public void Write(bool value, BooleanSize booleanSize, long offset)
        {
            BaseStream.Position = offset;
            Write(value, booleanSize);
        }
        public void Write(bool[] value)
        {
            Write(value, 0, value.Length, BooleanSize);
        }
        public void Write(bool[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length, BooleanSize);
        }
        public void Write(bool[] value, BooleanSize booleanSize)
        {
            Write(value, 0, value.Length, booleanSize);
        }
        public void Write(bool[] value, BooleanSize booleanSize, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length, booleanSize);
        }
        public void Write(bool[] value, int startIndex, int count)
        {
            Write(value, startIndex, count, BooleanSize);
        }
        public void Write(bool[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, BooleanSize);
        }
        public void Write(bool[] value, int startIndex, int count, BooleanSize booleanSize)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            for (int i = startIndex; i < count; i++)
            {
                Write(value[i], booleanSize);
            }
        }
        public void Write(bool[] value, int startIndex, int count, BooleanSize booleanSize, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, booleanSize);
        }
        public void Write(byte value)
        {
            SetBufferSize(1);
            _buffer[0] = value;
            WriteBytesFromBuffer(1);
        }
        public void Write(byte value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(byte[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(byte[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(byte[] value, int startIndex, int count)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            SetBufferSize(count);
            for (int i = 0; i < count; i++)
            {
                _buffer[i] = value[i + startIndex];
            }
            WriteBytesFromBuffer(count);
        }
        public void Write(byte[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(sbyte value)
        {
            SetBufferSize(1);
            _buffer[0] = (byte)value;
            WriteBytesFromBuffer(1);
        }
        public void Write(sbyte value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(sbyte[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(sbyte[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(sbyte[] value, int startIndex, int count)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            SetBufferSize(count);
            for (int i = 0; i < count; i++)
            {
                _buffer[i] = (byte)value[i + startIndex];
            }
            WriteBytesFromBuffer(count);
        }
        public void Write(sbyte[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(char value)
        {
            Write(value, Encoding);
        }
        public void Write(char value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, Encoding);
        }
        public void Write(char value, Encoding encoding)
        {
            Utils.ThrowIfCannotUseEncoding(encoding);
            _buffer = encoding.GetBytes(new[] { value });
            WriteBytesFromBuffer(_buffer.Length);
        }
        public void Write(char value, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, encoding);
        }
        public void Write(char[] value)
        {
            Write(value, 0, value.Length, Encoding);
        }
        public void Write(char[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length, Encoding);
        }
        public void Write(char[] value, Encoding encoding)
        {
            Write(value, 0, value.Length, encoding);
        }
        public void Write(char[] value, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length, encoding);
        }
        public void Write(char[] value, int startIndex, int count)
        {
            Write(value, startIndex, count, Encoding);
        }
        public void Write(char[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, Encoding);
        }
        public void Write(char[] value, int startIndex, int count, Encoding encoding)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            Utils.ThrowIfCannotUseEncoding(encoding);
            _buffer = encoding.GetBytes(value, startIndex, count);
            WriteBytesFromBuffer(_buffer.Length);
        }
        public void Write(char[] value, int startIndex, int count, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, encoding);
        }
        public void Write(string value, bool nullTerminated)
        {
            Write(value, nullTerminated, Encoding);
        }
        public void Write(string value, bool nullTerminated, long offset)
        {
            BaseStream.Position = offset;
            Write(value, nullTerminated, Encoding);
        }
        public void Write(string value, bool nullTerminated, Encoding encoding)
        {
            Write(value.ToCharArray(), encoding);
            if (nullTerminated)
            {
                Write('\0', encoding);
            }
        }
        public void Write(string value, bool nullTerminated, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, nullTerminated, encoding);
        }
        public void Write(string value, int charCount)
        {
            Write(value, charCount, Encoding);
        }
        public void Write(string value, int charCount, long offset)
        {
            BaseStream.Position = offset;
            Write(value, charCount, Encoding);
        }
        public void Write(string value, int charCount, Encoding encoding)
        {
            Utils.TruncateString(value, charCount, out char[] chars);
            Write(chars, encoding);
        }
        public void Write(string value, int charCount, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, charCount, encoding);
        }
        public void Write(string[] value, int startIndex, int count, bool nullTerminated)
        {
            Write(value, startIndex, count, nullTerminated, Encoding);
        }
        public void Write(string[] value, int startIndex, int count, bool nullTerminated, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, nullTerminated, Encoding);
        }
        public void Write(string[] value, int startIndex, int count, bool nullTerminated, Encoding encoding)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Write(value[i + startIndex], nullTerminated, encoding);
            }
        }
        public void Write(string[] value, int startIndex, int count, bool nullTerminated, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, nullTerminated, encoding);
        }
        public void Write(string[] value, int startIndex, int count, int charCount)
        {
            Write(value, startIndex, count, charCount, Encoding);
        }
        public void Write(string[] value, int startIndex, int count, int charCount, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, charCount, Encoding);
        }
        public void Write(string[] value, int startIndex, int count, int charCount, Encoding encoding)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Write(value[i + startIndex], charCount, encoding);
            }
        }
        public void Write(string[] value, int startIndex, int count, int charCount, Encoding encoding, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count, charCount, encoding);
        }
        public void Write(short value)
        {
            _buffer = EndianBitConverter.Int16ToBytes(value, Endianness);
            WriteBytesFromBuffer(2);
        }
        public void Write(short value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(short[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(short[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(short[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.Int16sToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 2);
        }
        public void Write(short[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(ushort value)
        {
            _buffer = EndianBitConverter.Int16ToBytes((short)value, Endianness);
            WriteBytesFromBuffer(2);
        }
        public void Write(ushort value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(ushort[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(ushort[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(ushort[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.UInt16sToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 2);
        }
        public void Write(ushort[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(int value)
        {
            _buffer = EndianBitConverter.Int32ToBytes(value, Endianness);
            WriteBytesFromBuffer(4);
        }
        public void Write(int value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(int[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(int[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(int[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.Int32sToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 4);
        }
        public void Write(int[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(uint value)
        {
            _buffer = EndianBitConverter.Int32ToBytes((int)value, Endianness);
            WriteBytesFromBuffer(4);
        }
        public void Write(uint value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(uint[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(uint[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(uint[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.UInt32sToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 4);
        }
        public void Write(uint[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(long value)
        {
            _buffer = EndianBitConverter.Int64ToBytes(value, Endianness);
            WriteBytesFromBuffer(8);
        }
        public void Write(long value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(long[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(long[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(long[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.Int64sToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 8);
        }
        public void Write(long[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(ulong value)
        {
            _buffer = EndianBitConverter.Int64ToBytes((long)value, Endianness);
            WriteBytesFromBuffer(8);
        }
        public void Write(ulong value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(ulong[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(ulong[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(ulong[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.UInt64sToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 8);
        }
        public void Write(ulong[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(float value)
        {
            _buffer = EndianBitConverter.SingleToBytes(value, Endianness);
            WriteBytesFromBuffer(4);
        }
        public void Write(float value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(float[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(float[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(float[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.SinglesToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 4);
        }
        public void Write(float[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(double value)
        {
            _buffer = EndianBitConverter.DoubleToBytes(value, Endianness);
            WriteBytesFromBuffer(8);
        }
        public void Write(double value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(double[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(double[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(double[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.DoublesToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 8);
        }
        public void Write(double[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }
        public void Write(decimal value)
        {
            _buffer = EndianBitConverter.DecimalToBytes(value, Endianness);
            WriteBytesFromBuffer(16);
        }
        public void Write(decimal value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(decimal[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(decimal[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(decimal[] value, int startIndex, int count)
        {
            _buffer = EndianBitConverter.DecimalsToBytes(value, startIndex, count, Endianness);
            WriteBytesFromBuffer(count * 16);
        }
        public void Write(decimal[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }

        // #13 - Handle "Enum" abstract type so we get the correct type in that case
        // For example, writer.Write((Enum)Enum.Parse(enumType, value))
        // No "struct" restriction on writes
        public void Write<TEnum>(TEnum value) where TEnum : Enum
        {
            Type underlyingType = Enum.GetUnderlyingType(value.GetType());
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte: Write(Convert.ToByte(value)); break;
                case TypeCode.SByte: Write(Convert.ToSByte(value)); break;
                case TypeCode.Int16: Write(Convert.ToInt16(value)); break;
                case TypeCode.UInt16: Write(Convert.ToUInt16(value)); break;
                case TypeCode.Int32: Write(Convert.ToInt32(value)); break;
                case TypeCode.UInt32: Write(Convert.ToUInt32(value)); break;
                case TypeCode.Int64: Write(Convert.ToInt64(value)); break;
                case TypeCode.UInt64: Write(Convert.ToUInt64(value)); break;
                default: throw new ArgumentOutOfRangeException(nameof(underlyingType));
            }
        }
        public void Write<TEnum>(TEnum value, long offset) where TEnum : Enum
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write<TEnum>(TEnum[] value) where TEnum : Enum
        {
            Write(value, 0, value.Length);
        }
        public void Write<TEnum>(TEnum[] value, long offset) where TEnum : Enum
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write<TEnum>(TEnum[] value, int startIndex, int count) where TEnum : Enum
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Write(value[i + startIndex]);
            }
        }
        public void Write<TEnum>(TEnum[] value, int startIndex, int count, long offset) where TEnum : Enum
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }

        public void Write(DateTime value)
        {
            Write(value.ToBinary());
        }
        public void Write(DateTime value, long offset)
        {
            BaseStream.Position = offset;
            Write(value);
        }
        public void Write(DateTime[] value)
        {
            Write(value, 0, value.Length);
        }
        public void Write(DateTime[] value, long offset)
        {
            BaseStream.Position = offset;
            Write(value, 0, value.Length);
        }
        public void Write(DateTime[] value, int startIndex, int count)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Write(value[i + startIndex]);
            }
        }
        public void Write(DateTime[] value, int startIndex, int count, long offset)
        {
            BaseStream.Position = offset;
            Write(value, startIndex, count);
        }

        public void Write(IBinarySerializable obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            obj.Write(this);
        }
        public void Write(IBinarySerializable obj, long offset)
        {
            BaseStream.Position = offset;
            Write(obj);
        }
        public void Write(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (obj is IBinarySerializable bs)
            {
                bs.Write(this);
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
                object value = propertyInfo.GetValue(obj);

                if (propertyType.IsArray)
                {
                    int arrayLength = Utils.GetArrayLength(obj, objType, propertyInfo);
                    if (arrayLength != 0) // Do not need to do anything for length 0
                    {
                        // Get array type
                        Type elementType = propertyType.GetElementType();
                        if (elementType.IsEnum)
                        {
                            elementType = Enum.GetUnderlyingType(elementType);
                        }
                        switch (Type.GetTypeCode(elementType))
                        {
                            case TypeCode.Boolean:
                            {
                                BooleanSize booleanSize = Utils.AttributeValueOrDefault<BinaryBooleanSizeAttribute, BooleanSize>(propertyInfo, BooleanSize);
                                Write((bool[])value, 0, arrayLength, booleanSize);
                                break;
                            }
                            case TypeCode.Byte: Write((byte[])value, 0, arrayLength); break;
                            case TypeCode.SByte: Write((sbyte[])value, 0, arrayLength); break;
                            case TypeCode.Char:
                            {
                                Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                                Write((char[])value, 0, arrayLength, encoding);
                                break;
                            }
                            case TypeCode.Int16: Write((short[])value, 0, arrayLength); break;
                            case TypeCode.UInt16: Write((ushort[])value, 0, arrayLength); break;
                            case TypeCode.Int32: Write((int[])value, 0, arrayLength); break;
                            case TypeCode.UInt32: Write((uint[])value, 0, arrayLength); break;
                            case TypeCode.Int64: Write((long[])value, 0, arrayLength); break;
                            case TypeCode.UInt64: Write((ulong[])value, 0, arrayLength); break;
                            case TypeCode.Single: Write((float[])value, 0, arrayLength); break;
                            case TypeCode.Double: Write((double[])value, 0, arrayLength); break;
                            case TypeCode.Decimal: Write((decimal[])value, 0, arrayLength); break;
                            case TypeCode.DateTime: Write((DateTime[])value, 0, arrayLength); break;
                            case TypeCode.String:
                            {
                                Utils.GetStringLength(obj, objType, propertyInfo, false, out bool? nullTerminated, out int stringLength);
                                Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                                if (nullTerminated.HasValue)
                                {
                                    Write((string[])value, 0, arrayLength, nullTerminated.Value, encoding);
                                }
                                else
                                {
                                    Write((string[])value, 0, arrayLength, stringLength, encoding);
                                }
                                break;
                            }
                            case TypeCode.Object:
                            {
                                if (typeof(IBinarySerializable).IsAssignableFrom(elementType))
                                {
                                    for (int i = 0; i < arrayLength; i++)
                                    {
                                        var serializable = (IBinarySerializable)((Array)value).GetValue(i);
                                        serializable.Write(this);
                                    }
                                }
                                else // Element's type is not supported so try to write the array's objects
                                {
                                    for (int i = 0; i < arrayLength; i++)
                                    {
                                        object elementObj = ((Array)value).GetValue(i);
                                        Write(elementObj);
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
                            Write((bool)value, booleanSize);
                            break;
                        }
                        case TypeCode.Byte: Write((byte)value); break;
                        case TypeCode.SByte: Write((sbyte)value); break;
                        case TypeCode.Char:
                        {
                            Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                            Write((char)value, encoding);
                            break;
                        }
                        case TypeCode.Int16: Write((short)value); break;
                        case TypeCode.UInt16: Write((ushort)value); break;
                        case TypeCode.Int32: Write((int)value); break;
                        case TypeCode.UInt32: Write((uint)value); break;
                        case TypeCode.Int64: Write((long)value); break;
                        case TypeCode.UInt64: Write((ulong)value); break;
                        case TypeCode.Single: Write((float)value); break;
                        case TypeCode.Double: Write((double)value); break;
                        case TypeCode.Decimal: Write((decimal)value); break;
                        case TypeCode.DateTime: Write((DateTime)value); break;
                        case TypeCode.String:
                        {
                            Utils.GetStringLength(obj, objType, propertyInfo, false, out bool? nullTerminated, out int stringLength);
                            Encoding encoding = Utils.AttributeValueOrDefault<BinaryEncodingAttribute, Encoding>(propertyInfo, Encoding);
                            if (nullTerminated.HasValue)
                            {
                                Write((string)value, nullTerminated.Value, encoding);
                            }
                            else
                            {
                                Write((string)value, stringLength, encoding);
                            }
                            break;
                        }
                        case TypeCode.Object:
                        {
                            if (typeof(IBinarySerializable).IsAssignableFrom(propertyType))
                            {
                                ((IBinarySerializable)value).Write(this);
                            }
                            else // property's type is not supported so try to write the object
                            {
                                Write(value);
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(propertyType));
                    }
                }
            }
        }
        public void Write(object obj, long offset)
        {
            BaseStream.Position = offset;
            Write(obj);
        }
    }
}
