using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.Util.EndianBinaryExtras;

public partial class EndianBinaryReaderExtras
{
	protected delegate void ReadArrayMethod<TDest>(ReadOnlySpan<byte> src, Span<TDest> dest, Endianness endianness);
	EndianBinaryReader Reader { get; set; }

	protected readonly byte[] _buffer;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EndianBinaryReaderExtras(EndianBinaryReader reader)
	{
		Reader = reader;
		_buffer = new byte[64];
	}

	protected void ReadArray<TDest>(Span<TDest> dest, int elementSize, ReadArrayMethod<TDest> readArray)
	{
		int num = dest.Length * elementSize;
		int num2 = 0;
		while (num != 0)
		{
			int num3 = Math.Min(num, 64);
			Span<byte> span = _buffer.AsSpan(0, num3);
			Reader.ReadBytes(span);
			readArray(span, dest.Slice(num2, num3 / elementSize), Reader.Endianness);
			num -= num3;
			num2 += num3 / elementSize;
		}
	}

	public int ReadInt24()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 3);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt24(buffer, Reader.Endianness);
	}
	public void ReadInt24s(Span<int> dest)
	{
		ReadArray(dest, 3, EndianBinaryPrimitivesExtras.ReadInt24s);
	}
	public uint ReadUInt24()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 3);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt24(buffer, Reader.Endianness);
	}
	public void ReadUInt24s(Span<uint> dest)
	{
		ReadArray(dest, 3, EndianBinaryPrimitivesExtras.ReadUInt24s);
	}
	public long ReadInt40()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 5);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt40(buffer, Reader.Endianness);
	}
	public void ReadInt40s(Span<long> dest)
	{
		ReadArray(dest, 5, EndianBinaryPrimitivesExtras.ReadInt40s);
	}
	public ulong ReadUInt40()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 5);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt40(buffer, Reader.Endianness);
	}
	public void ReadUInt40s(Span<ulong> dest)
	{
		ReadArray(dest, 5, EndianBinaryPrimitivesExtras.ReadUInt40s);
	}
	public long ReadInt48()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 6);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt48(buffer, Reader.Endianness);
	}
	public void ReadInt48s(Span<long> dest)
	{
		ReadArray(dest, 6, EndianBinaryPrimitivesExtras.ReadInt48s);
	}
	public ulong ReadUInt48()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 6);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt48(buffer, Reader.Endianness);
	}
	public void ReadUInt48s(Span<ulong> dest)
	{
		ReadArray(dest, 6, EndianBinaryPrimitivesExtras.ReadUInt48s);
	}
	public long ReadInt56()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 7);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadInt56(buffer, Reader.Endianness);
	}
	public void ReadInt56s(Span<long> dest)
	{
		ReadArray(dest, 7, EndianBinaryPrimitivesExtras.ReadInt56s);
	}
	public ulong ReadUInt56()
	{
		Span<byte> buffer = _buffer.AsSpan(0, 7);
		Reader.ReadBytes(buffer);
		return EndianBinaryPrimitivesExtras.ReadUInt56(buffer, Reader.Endianness);
	}
	public void ReadUInt56s(Span<ulong> dest)
	{
		ReadArray(dest, 7, EndianBinaryPrimitivesExtras.ReadUInt56s);
	}
}

