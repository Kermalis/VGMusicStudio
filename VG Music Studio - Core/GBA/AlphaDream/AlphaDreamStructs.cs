using System;
using System.Runtime.InteropServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct SampleHeader
{
	public const int SIZE = 16;
	public const int LOOP_TRUE = 0x40_000_000;

	/// <summary>0x40_000_000 if True</summary>
	public readonly int DoesLoop;
	/// <summary>Right shift 10 for value</summary>
	public readonly int SampleRate;
	public readonly int LoopOffset;
	public readonly int Length;
	// byte[Length] Sample;

	public SampleHeader(byte[] rom, int offset, out int sampleOffset)
	{
		ReadOnlySpan<byte> data = rom.AsSpan(offset, SIZE);
		if (BitConverter.IsLittleEndian)
		{
			this = MemoryMarshal.AsRef<SampleHeader>(data);
		}
		else
		{
			DoesLoop = ReadInt32LittleEndian(data.Slice(0, 4));
			SampleRate = ReadInt32LittleEndian(data.Slice(4, 4));
			LoopOffset = ReadInt32LittleEndian(data.Slice(8, 4));
			Length = ReadInt32LittleEndian(data.Slice(12, 4));
		}
		sampleOffset = offset + SIZE;
	}
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct VoiceEntry
{
	public const int SIZE = 8;
	public const byte FIXED_FREQ_TRUE = 0x80;

	public readonly byte MinKey;
	public readonly byte MaxKey;
	public readonly byte Sample;
	/// <summary>0x80 if True</summary>
	public readonly byte IsFixedFrequency;
	public readonly byte Unknown1;
	public readonly byte Unknown2;
	public readonly byte Unknown3;
	public readonly byte Unknown4;

	public static ref readonly VoiceEntry Get(ReadOnlySpan<byte> src)
	{
		return ref MemoryMarshal.AsRef<VoiceEntry>(src);
	}
}

internal struct ChannelVolume
{
	public float LeftVol, RightVol;
}
internal struct ADSR // TODO
{
	public byte A, D, S, R;
}
