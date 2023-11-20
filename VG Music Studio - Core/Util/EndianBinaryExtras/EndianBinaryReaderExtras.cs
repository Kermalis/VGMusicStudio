using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.Util.EndianBinaryExtras;

public partial class EndianBinaryReader : EndianBinaryIO.EndianBinaryReader
{
	public EndianBinaryReader(Stream stream, Endianness endianness = Endianness.LittleEndian, BooleanSize booleanSize = BooleanSize.U8, bool ascii = false)
		: base(stream, endianness, booleanSize, ascii)
	{
	}

	public int ReadInt24()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 3);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt24(buffer, Endianness);
	}
	public void ReadInt24s(Span<int> dest)
	{
		ReadArray(dest, 3, EndianBinaryPrimitivesExtras.ReadInt24s);
	}
	public uint ReadUInt24()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 3);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt24(buffer, Endianness);
	}
	public void ReadUInt24s(Span<uint> dest)
	{
		ReadArray(dest, 3, EndianBinaryPrimitivesExtras.ReadUInt24s);
	}
	public long ReadInt40()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 5);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt40(buffer, Endianness);
	}
	public void ReadInt40s(Span<long> dest)
	{
		ReadArray(dest, 5, EndianBinaryPrimitivesExtras.ReadInt40s);
	}
	public ulong ReadUInt40()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 5);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt40(buffer, Endianness);
	}
	public void ReadUInt40s(Span<ulong> dest)
	{
		ReadArray(dest, 5, EndianBinaryPrimitivesExtras.ReadUInt40s);
	}
	public long ReadInt48()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 6);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt48(buffer, Endianness);
	}
	public void ReadInt48s(Span<long> dest)
	{
		ReadArray(dest, 6, EndianBinaryPrimitivesExtras.ReadInt48s);
	}
	public ulong ReadUInt48()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 6);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt48(buffer, Endianness);
	}
	public void ReadUInt48s(Span<ulong> dest)
	{
		ReadArray(dest, 6, EndianBinaryPrimitivesExtras.ReadUInt48s);
	}
	public long ReadInt56()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 7);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt56(buffer, Endianness);
	}
	public void ReadInt56s(Span<long> dest)
	{
		ReadArray(dest, 7, EndianBinaryPrimitivesExtras.ReadInt56s);
	}
	public ulong ReadUInt56()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 7);
		ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt56(buffer, Endianness);
	}
	public void ReadUInt56s(Span<ulong> dest)
	{
		ReadArray(dest, 7, EndianBinaryPrimitivesExtras.ReadUInt56s);
	}
}

