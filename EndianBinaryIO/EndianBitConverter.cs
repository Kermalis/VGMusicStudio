using System;

namespace Kermalis.EndianBinaryIO
{
    public static class EndianBitConverter
    {
        public static Endianness SystemEndianness { get; } = BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

        public static unsafe byte[] Int16ToBytes(short value, Endianness targetEndianness)
        {
            byte[] bytes = new byte[2];
            fixed (byte* b = bytes)
            {
                *(short*)b = value;
            }
            if (SystemEndianness != targetEndianness)
            {
                FlipPrimitives(bytes, 0, 1, 2);
            }
            return bytes;
        }
        public static unsafe byte[] Int16sToBytes(short[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[2 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((short*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 2);
                }
            }
            return array;
        }
        public static unsafe byte[] UInt16sToBytes(ushort[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[2 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((ushort*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 2);
                }
            }
            return array;
        }
        public static unsafe byte[] Int32ToBytes(int value, Endianness targetEndianness)
        {
            byte[] bytes = new byte[4];
            fixed (byte* b = bytes)
            {
                *(int*)b = value;
            }
            if (SystemEndianness != targetEndianness)
            {
                FlipPrimitives(bytes, 0, 1, 4);
            }
            return bytes;
        }
        public static unsafe byte[] Int32sToBytes(int[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[4 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((int*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 4);
                }
            }
            return array;
        }
        public static unsafe byte[] UInt32sToBytes(uint[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[4 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((uint*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 4);
                }
            }
            return array;
        }
        public static unsafe byte[] Int64ToBytes(long value, Endianness targetEndianness)
        {
            byte[] bytes = new byte[8];
            fixed (byte* b = bytes)
            {
                *(long*)b = value;
            }
            if (SystemEndianness != targetEndianness)
            {
                FlipPrimitives(bytes, 0, 1, 8);
            }
            return bytes;
        }
        public static unsafe byte[] Int64sToBytes(long[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[8 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((long*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 8);
                }
            }
            return array;
        }
        public static unsafe byte[] UInt64sToBytes(ulong[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[8 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((ulong*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 8);
                }
            }
            return array;
        }
        public static unsafe byte[] SingleToBytes(float value, Endianness targetEndianness)
        {
            byte[] bytes = new byte[4];
            fixed (byte* b = bytes)
            {
                *(float*)b = value;
            }
            if (SystemEndianness != targetEndianness)
            {
                FlipPrimitives(bytes, 0, 1, 4);
            }
            return bytes;
        }
        public static unsafe byte[] SinglesToBytes(float[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[4 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((float*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 4);
                }
            }
            return array;
        }
        public static unsafe byte[] DoubleToBytes(double value, Endianness targetEndianness)
        {
            byte[] bytes = new byte[8];
            fixed (byte* b = bytes)
            {
                *(double*)b = value;
            }
            if (SystemEndianness != targetEndianness)
            {
                FlipPrimitives(bytes, 0, 1, 8);
            }
            return bytes;
        }
        public static unsafe byte[] DoublesToBytes(double[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[8 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((double*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 8);
                }
            }
            return array;
        }
        public static unsafe byte[] DecimalToBytes(decimal value, Endianness targetEndianness)
        {
            byte[] bytes = new byte[16];
            fixed (byte* b = bytes)
            {
                *(decimal*)b = value;
            }
            if (SystemEndianness != targetEndianness)
            {
                FlipPrimitives(bytes, 0, 1, 16);
            }
            return bytes;
        }
        public static unsafe byte[] DecimalsToBytes(decimal[] value, int startIndex, int count, Endianness targetEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count))
            {
                return Array.Empty<byte>();
            }
            if (!Utils.ValidateReadArraySize(count, out byte[] array))
            {
                array = new byte[16 * count];
                fixed (byte* b = array)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ((decimal*)b)[i] = value[startIndex + i];
                    }
                }
                if (SystemEndianness != targetEndianness)
                {
                    FlipPrimitives(array, 0, count, 16);
                }
            }
            return array;
        }

        public static unsafe short BytesToInt16(byte[] value, int startIndex, Endianness sourceEndianness)
        {
            if (SystemEndianness != sourceEndianness)
            {
                FlipPrimitives(value, startIndex, 1, 2);
            }
            fixed (byte* b = &value[startIndex])
            {
                return *(short*)b;
            }
        }
        public static unsafe short[] BytesToInt16s(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 2))
            {
                return Array.Empty<short>();
            }
            if (!Utils.ValidateReadArraySize(count, out short[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 2);
                }
                array = new short[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((short*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe ushort[] BytesToUInt16s(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 2))
            {
                return Array.Empty<ushort>();
            }
            if (!Utils.ValidateReadArraySize(count, out ushort[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 2);
                }
                array = new ushort[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((ushort*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe int BytesToInt32(byte[] value, int startIndex, Endianness sourceEndianness)
        {
            if (SystemEndianness != sourceEndianness)
            {
                FlipPrimitives(value, startIndex, 1, 4);
            }
            fixed (byte* b = &value[startIndex])
            {
                return *(int*)b;
            }
        }
        public static unsafe int[] BytesToInt32s(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 4))
            {
                return Array.Empty<int>();
            }
            if (!Utils.ValidateReadArraySize(count, out int[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 4);
                }
                array = new int[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((int*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe uint[] BytesToUInt32s(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 4))
            {
                return Array.Empty<uint>();
            }
            if (!Utils.ValidateReadArraySize(count, out uint[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 4);
                }
                array = new uint[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((uint*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe long BytesToInt64(byte[] value, int startIndex, Endianness sourceEndianness)
        {
            if (SystemEndianness != sourceEndianness)
            {
                FlipPrimitives(value, startIndex, 1, 8);
            }
            fixed (byte* b = &value[startIndex])
            {
                return *(long*)b;
            }
        }
        public static unsafe long[] BytesToInt64s(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 8))
            {
                return Array.Empty<long>();
            }
            if (!Utils.ValidateReadArraySize(count, out long[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 8);
                }
                array = new long[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((long*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe ulong[] BytesToUInt64s(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 8))
            {
                return Array.Empty<ulong>();
            }
            if (!Utils.ValidateReadArraySize(count, out ulong[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 8);
                }
                array = new ulong[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((ulong*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe float BytesToSingle(byte[] value, int startIndex, Endianness sourceEndianness)
        {
            if (SystemEndianness != sourceEndianness)
            {
                FlipPrimitives(value, startIndex, 1, 4);
            }
            fixed (byte* b = &value[startIndex])
            {
                return *(float*)b;
            }
        }
        public static unsafe float[] BytesToSingles(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 4))
            {
                return Array.Empty<float>();
            }
            if (!Utils.ValidateReadArraySize(count, out float[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 4);
                }
                array = new float[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((float*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe double BytesToDouble(byte[] value, int startIndex, Endianness sourceEndianness)
        {
            if (SystemEndianness != sourceEndianness)
            {
                FlipPrimitives(value, startIndex, 1, 8);
            }
            fixed (byte* b = &value[startIndex])
            {
                return *(double*)b;
            }
        }
        public static unsafe double[] BytesToDoubles(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 8))
            {
                return Array.Empty<double>();
            }
            if (!Utils.ValidateReadArraySize(count, out double[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 8);
                }
                array = new double[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((double*)b)[i];
                    }
                }
            }
            return array;
        }
        public static unsafe decimal BytesToDecimal(byte[] value, int startIndex, Endianness sourceEndianness)
        {
            if (SystemEndianness != sourceEndianness)
            {
                FlipPrimitives(value, startIndex, 1, 16);
            }
            fixed (byte* b = &value[startIndex])
            {
                return *(decimal*)b;
            }
        }
        public static unsafe decimal[] BytesToDecimals(byte[] value, int startIndex, int count, Endianness sourceEndianness)
        {
            if (Utils.ValidateArrayIndexAndCount(value, startIndex, count * 16))
            {
                return Array.Empty<decimal>();
            }
            if (!Utils.ValidateReadArraySize(count, out decimal[] array))
            {
                if (SystemEndianness != sourceEndianness)
                {
                    FlipPrimitives(value, startIndex, count, 16);
                }
                array = new decimal[count];
                fixed (byte* b = &value[startIndex])
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = ((decimal*)b)[i];
                    }
                }
            }
            return array;
        }

        private static void FlipPrimitives(byte[] buffer, int startIndex, int primitiveCount, int primitiveSize)
        {
            int byteCount = primitiveCount * primitiveSize;
            for (int i = startIndex; i < byteCount + startIndex; i += primitiveSize)
            {
                int a = i;
                int b = i + primitiveSize - 1;
                while (a < b)
                {
                    byte by = buffer[a];
                    buffer[a] = buffer[b];
                    buffer[b] = by;
                    a++;
                    b--;
                }
            }
        }
    }
}
