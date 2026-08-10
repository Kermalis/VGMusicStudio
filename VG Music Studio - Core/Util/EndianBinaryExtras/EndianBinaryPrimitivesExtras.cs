using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.Util.EndianBinaryExtras;

public static class EndianBinaryPrimitivesExtras
{
	public static readonly Endianness SystemEndianness = BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

	#region Read

	// 24-bits (3 bytes)
	public static int ReadInt24(ReadOnlySpan<byte> src, Endianness endianness)
	{
		uint val; // Do a "sign extend" to maintain the sign from 24->32 bits
		if (endianness == Endianness.LittleEndian)
		{
			val = ((uint)src[0] << 8) | ((uint)src[1] << 16) | ((uint)src[2] << 24);
		}
		else
		{
			val = ((uint)src[2] << 8) | ((uint)src[1] << 16) | ((uint)src[0] << 24);
		}
		return (int)val >> 8;
	}
	public static void ReadInt24s(ReadOnlySpan<byte> src, Span<int> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadInt24(src.Slice(i * 3, 3), endianness);
		}
	}
	public static uint ReadUInt24(ReadOnlySpan<byte> src, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			return (uint)((src[0]) | (src[1] << 8) | (src[2] << 16));
		}
		else
		{
			return (uint)((src[2]) | (src[1] << 8) | (src[0] << 16));
		}
	}
	public static void ReadUInt24s(ReadOnlySpan<byte> src, Span<uint> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadUInt24(src.Slice(i * 3, 3), endianness);
		}
	}

	// 40-bits (5 bytes)
	public static long ReadInt40(ReadOnlySpan<byte> src, Endianness endianness)
	{
		ulong val; // Do a "sign extend" to maintain the sign from 40->64 bits
		if (endianness == Endianness.LittleEndian)
		{
			val = ((uint)src[0] << 8) | ((uint)src[1] << 16) | ((uint)src[2] << 24) | ((uint)src[3] << 32) | ((uint)src[4] << 40);
		}
		else
		{
			val = ((uint)src[4] << 8) | ((uint)src[3] << 16) | ((uint)src[2] << 24) | ((uint)src[1] << 32) | ((uint)src[0] << 40);
		}
		return (long)val >> 24;
	}
	public static void ReadInt40s(ReadOnlySpan<byte> src, Span<long> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadInt40(src.Slice(i * 5, 5), endianness);
		}
	}
	public static ulong ReadUInt40(ReadOnlySpan<byte> src, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			return (uint)((src[0]) | (src[1] << 8) | (src[2] << 16) | (src[3] << 24) | (src[4] << 32));
		}
		else
		{
			return (uint)((src[4]) | (src[3] << 8) | (src[2] << 16) | (src[1] << 24) | (src[0] << 32));
		}
	}
	public static void ReadUInt40s(ReadOnlySpan<byte> src, Span<ulong> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadUInt40(src.Slice(i * 5, 5), endianness);
		}
	}

	// 48-bits (6 bytes)
	public static long ReadInt48(ReadOnlySpan<byte> src, Endianness endianness)
	{
		ulong val; // Do a "sign extend" to maintain the sign from 48->64 bits
		if (endianness == Endianness.LittleEndian)
		{
			val = ((uint)src[0] << 8) | ((uint)src[1] << 16) | ((uint)src[2] << 24) | ((uint)src[3] << 32) | ((uint)src[4] << 40) | ((uint)src[5] << 48);
		}
		else
		{
			val = ((uint)src[5] << 8) | ((uint)src[4] << 16) | ((uint)src[3] << 24) | ((uint)src[2] << 32) | ((uint)src[1] << 40) | ((uint)src[0] << 48);
		}
		return (long)val >> 16;
	}
	public static void ReadInt48s(ReadOnlySpan<byte> src, Span<long> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadInt48(src.Slice(i * 6, 6), endianness);
		}
	}
	public static ulong ReadUInt48(ReadOnlySpan<byte> src, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			return (uint)((src[0]) | (src[1] << 8) | (src[2] << 16) | (src[3] << 24) | (src[4] << 32) | (src[5] << 40));
		}
		else
		{
			return (uint)((src[5]) | (src[4] << 8) | (src[3] << 16) | (src[2] << 24) | (src[1] << 32) | (src[0] << 40));
		}
	}
	public static void ReadUInt48s(ReadOnlySpan<byte> src, Span<ulong> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadUInt48(src.Slice(i * 6, 6), endianness);
		}
	}

	// 56-bits (7 bytes)
	public static long ReadInt56(ReadOnlySpan<byte> src, Endianness endianness)
	{
		ulong val; // Do a "sign extend" to maintain the sign from 56->64 bits
		if (endianness == Endianness.LittleEndian)
		{
			val = ((uint)src[0] << 8) | ((uint)src[1] << 16) | ((uint)src[2] << 24) | ((uint)src[3] << 32) | ((uint)src[4] << 40) | ((uint)src[5] << 48) | ((uint)src[6] << 56);
		}
		else
		{
			val = ((uint)src[6] << 8) | ((uint)src[5] << 16) | ((uint)src[4] << 24) | ((uint)src[3] << 32) | ((uint)src[2] << 40) | ((uint)src[1] << 48) | ((uint)src[0] << 56);
		}
		return (long)val >> 8;
	}
	public static void ReadInt56s(ReadOnlySpan<byte> src, Span<long> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadInt56(src.Slice(i * 7, 7), endianness);
		}
	}
	public static ulong ReadUInt56(ReadOnlySpan<byte> src, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			return (uint)((src[0]) | (src[1] << 8) | (src[2] << 16) | (src[3] << 24) | (src[4] << 32) | (src[5] << 40) | (src[6] << 48));
		}
		else
		{
			return (uint)((src[6]) | (src[5] << 8) | (src[4] << 16) | (src[3] << 24) | (src[2] << 32) | (src[1] << 40) | (src[0] << 48));
		}
	}
	public static void ReadUInt56s(ReadOnlySpan<byte> src, Span<ulong> dest, Endianness endianness)
	{
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = ReadUInt56(src.Slice(i * 7, 7), endianness);
		}
	}

	#endregion

	#region Write

	public static void WriteInt24(Span<byte> dest, int value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16);
		}
		else
		{
			dest[2] = (byte)value; dest[1] = (byte)(value >> 8); dest[0] = (byte)(value >> 16);
		}
	}
	public static void WriteInt24s(Span<byte> dest, ReadOnlySpan<int> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteInt24(dest.Slice(i * 3, 3), src[i], endianness);
		}
	}
	public static void WriteUInt24(Span<byte> dest, uint value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16);
		}
		else
		{
			dest[2] = (byte)value; dest[1] = (byte)(value >> 8); dest[0] = (byte)(value >> 16);
		}
	}
	public static void WriteUInt24s(Span<byte> dest, ReadOnlySpan<uint> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteUInt24(dest.Slice(i * 3, 3), src[i], endianness);
		}
	}

	public static void WriteInt40(Span<byte> dest, long value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[4] = (byte)(value >> 32);
		}
		else
		{
			dest[4] = (byte)value; dest[3] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[1] = (byte)(value >> 24); dest[0] = (byte)(value >> 32);
		}
	}
	public static void WriteInt40s(Span<byte> dest, ReadOnlySpan<long> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteInt40(dest.Slice(i * 5, 5), src[i], endianness);
		}
	}

	public static void WriteUInt40(Span<byte> dest, ulong value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[4] = (byte)(value >> 32);
		}
		else
		{
			dest[4] = (byte)value; dest[3] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[1] = (byte)(value >> 24); dest[0] = (byte)(value >> 32);
		}
	}
	public static void WriteUInt40s(Span<byte> dest, ReadOnlySpan<ulong> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteUInt40(dest.Slice(i * 5, 5), src[i], endianness);
		}
	}

	public static void WriteInt48(Span<byte> dest, long value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[4] = (byte)(value >> 32); dest[5] = (byte)(value >> 40);
		}
		else
		{
			dest[5] = (byte)value; dest[4] = (byte)(value >> 8); dest[3] = (byte)(value >> 16); dest[2] = (byte)(value >> 24); dest[1] = (byte)(value >> 32); dest[0] = (byte)(value >> 40);
		}
	}
	public static void WriteInt48s(Span<byte> dest, ReadOnlySpan<long> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteInt48(dest.Slice(i * 6, 6), src[i], endianness);
		}
	}

	public static void WriteUInt48(Span<byte> dest, ulong value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[4] = (byte)(value >> 32); dest[5] = (byte)(value >> 40);
		}
		else
		{
			dest[5] = (byte)value; dest[4] = (byte)(value >> 8); dest[3] = (byte)(value >> 16); dest[2] = (byte)(value >> 24); dest[1] = (byte)(value >> 32); dest[0] = (byte)(value >> 40);
		}
	}
	public static void WriteUInt48s(Span<byte> dest, ReadOnlySpan<ulong> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteUInt48(dest.Slice(i * 6, 6), src[i], endianness);
		}
	}

	public static void WriteInt56(Span<byte> dest, long value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[4] = (byte)(value >> 32); dest[5] = (byte)(value >> 40); dest[6] = (byte)(value >> 48);
		}
		else
		{
			dest[6] = (byte)value; dest[5] = (byte)(value >> 8); dest[4] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[2] = (byte)(value >> 32); dest[1] = (byte)(value >> 40); dest[0] = (byte)(value >> 48);
		}
	}
	public static void WriteInt56s(Span<byte> dest, ReadOnlySpan<long> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteInt56(dest.Slice(i * 7, 7), src[i], endianness);
		}
	}

	public static void WriteUInt56(Span<byte> dest, ulong value, Endianness endianness)
	{
		if (endianness == Endianness.LittleEndian)
		{
			dest[0] = (byte)value; dest[1] = (byte)(value >> 8); dest[2] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[4] = (byte)(value >> 32); dest[5] = (byte)(value >> 40); dest[6] = (byte)(value >> 48);
		}
		else
		{
			dest[6] = (byte)value; dest[5] = (byte)(value >> 8); dest[4] = (byte)(value >> 16); dest[3] = (byte)(value >> 24); dest[2] = (byte)(value >> 32); dest[1] = (byte)(value >> 40); dest[0] = (byte)(value >> 48);
		}
	}
	public static void WriteUInt56s(Span<byte> dest, ReadOnlySpan<ulong> src, Endianness endianness)
	{
		for (int i = 0; i < src.Length; i++)
		{
			WriteUInt56(dest.Slice(i * 7, 7), src[i], endianness);
		}
	}

	#endregion
}

