using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.Util.EndianBinaryExtras;

public partial class EndianBinaryWriter : EndianBinaryIO.EndianBinaryWriter
{
	public EndianBinaryWriter(Stream stream, Endianness endianness = Endianness.LittleEndian, BooleanSize booleanSize = BooleanSize.U8, bool ascii = false)
		: base(stream, endianness)
	{
	}

	public void WriteInt24(int value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 3);
		EndianBinaryPrimitivesExtras.WriteInt24(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteInt24s(ReadOnlySpan<int> values)
	{
		WriteArray(values, 3, EndianBinaryPrimitivesExtras.WriteInt24s);
	}
	public void WriteUInt24(uint value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 3);
		EndianBinaryPrimitivesExtras.WriteUInt24(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteUInt24s(ReadOnlySpan<uint> values)
	{
		WriteArray(values, 3, EndianBinaryPrimitivesExtras.WriteUInt24s);
	}

	public void WriteInt40(long value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 5);
		EndianBinaryPrimitivesExtras.WriteInt40(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteInt40s(ReadOnlySpan<long> values)
	{
		WriteArray(values, 5, EndianBinaryPrimitivesExtras.WriteInt40s);
	}
	public void WriteUInt40(ulong value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 5);
		EndianBinaryPrimitivesExtras.WriteUInt40(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteUInt40s(ReadOnlySpan<ulong> values)
	{
		WriteArray(values, 5, EndianBinaryPrimitivesExtras.WriteUInt40s);
	}

	public void WriteInt48(long value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 6);
		EndianBinaryPrimitivesExtras.WriteInt48(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteInt48s(ReadOnlySpan<long> values)
	{
		WriteArray(values, 6, EndianBinaryPrimitivesExtras.WriteInt48s);
	}
	public void WriteUInt48(ulong value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 6);
		EndianBinaryPrimitivesExtras.WriteUInt48(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteUInt48s(ReadOnlySpan<ulong> values)
	{
		WriteArray(values, 6, EndianBinaryPrimitivesExtras.WriteUInt48s);
	}
	public void WriteInt56(long value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 7);
		EndianBinaryPrimitivesExtras.WriteInt56(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteInt56s(ReadOnlySpan<long> values)
	{
		WriteArray(values, 7, EndianBinaryPrimitivesExtras.WriteInt56s);
	}
	public void WriteUInt56(ulong value)
	{
		Span<byte> buffer = _buffer.AsSpan(0, 7);
		EndianBinaryPrimitivesExtras.WriteUInt56(buffer, value, Endianness);
		Stream.Write(buffer);
	}
	public void WriteUInt56s(ReadOnlySpan<ulong> values)
	{
		WriteArray(values, 7, EndianBinaryPrimitivesExtras.WriteUInt56s);
	}
}

