using System;
using System.Runtime.InteropServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct SongEntry
{
	public const int SIZE = 8;

	public readonly int HeaderOffset;
	public readonly short Player;
	public readonly byte Unknown1;
	public readonly byte Unknown2;

	public SongEntry(ReadOnlySpan<byte> src)
	{
		if (BitConverter.IsLittleEndian)
		{
			this = MemoryMarshal.AsRef<SongEntry>(src);
		}
		else
		{
			HeaderOffset = ReadInt32LittleEndian(src.Slice(0));
			Player = ReadInt16LittleEndian(src.Slice(4));
			Unknown1 = src[6];
			Unknown2 = src[7];
		}
	}

	public static SongEntry Get(byte[] rom, int songTableOffset, int songNum)
	{
		return new SongEntry(rom.AsSpan(songTableOffset + (songNum * SIZE)));
	}
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct SongHeader
{
	public const int SIZE = 8;

	public readonly byte NumTracks;
	public readonly byte NumBlocks;
	public readonly byte Priority;
	public readonly byte Reverb;
	public readonly int VoiceTableOffset;
	// int[NumTracks] TrackOffset;

	public SongHeader(ReadOnlySpan<byte> src)
	{
		if (BitConverter.IsLittleEndian)
		{
			this = MemoryMarshal.AsRef<SongHeader>(src);
		}
		else
		{
			NumTracks = src[0];
			NumBlocks = src[1];
			Priority = src[2];
			Reverb = src[3];
			VoiceTableOffset = ReadInt32LittleEndian(src.Slice(4));
		}
	}

	public static SongHeader Get(byte[] rom, int offset, out int tracksOffset)
	{
		tracksOffset = offset + SIZE;
		return new SongHeader(rom.AsSpan(offset));
	}
	public static int GetTrackOffset(byte[] rom, int tracksOffset, int trackIndex)
	{
		return ReadInt32LittleEndian(rom.AsSpan(tracksOffset + (trackIndex * 4)));
	}
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct VoiceEntry
{
	public const int SIZE = 12;

	public readonly byte Type; // 0
	public readonly byte RootNote; // 1
	public readonly byte Unknown; // 2
	public readonly byte Pan; // 3
	/// <summary>SquarePattern for Square1/Square2, NoisePattern for Noise, Address for PCM8/PCM4/KeySplit/Drum</summary>
	public readonly int Int4; // 4
	/// <summary>ADSR for PCM8/Square1/Square2/PCM4/Noise, KeysAddress for KeySplit</summary>
	public readonly ADSR ADSR; // 8

	public int Int8 => (ADSR.R << 24) | (ADSR.S << 16) | (ADSR.D << 8) | (ADSR.A);

	public VoiceEntry(ReadOnlySpan<byte> src)
	{
		if (BitConverter.IsLittleEndian)
		{
			this = MemoryMarshal.AsRef<VoiceEntry>(src);
		}
		else
		{
			Type = src[0];
			RootNote = src[1];
			Unknown = src[2];
			Pan = src[3];
			Int4 = ReadInt32LittleEndian(src.Slice(4));
			ADSR = ADSR.Get(src.Slice(8));
		}
	}
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal struct ADSR
{
	public const int SIZE = 4;

	public byte A;
	public byte D;
	public byte S;
	public byte R;

	public static ref readonly ADSR Get(ReadOnlySpan<byte> src)
	{
		return ref MemoryMarshal.AsRef<ADSR>(src);
	}
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct GoldenSunPSG
{
	public const int SIZE = 6;

	/// <summary>Always 0x80</summary>
	public readonly byte Unknown;
	public readonly GoldenSunPSGType Type;
	public readonly byte InitialCycle;
	public readonly byte CycleSpeed;
	public readonly byte CycleAmplitude;
	public readonly byte MinimumCycle;

	public static ref readonly GoldenSunPSG Get(ReadOnlySpan<byte> src)
	{
		return ref MemoryMarshal.AsRef<GoldenSunPSG>(src);
	}
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal struct SampleHeader
{
	public const int SIZE = 16;
	public const int LOOP_TRUE = 0x40_000_000;

	/// <summary>0x40_000_000 if True</summary>
	public int DoesLoop;
	/// <summary>Right shift 10 for value</summary>
	public int SampleRate;
	public int LoopOffset;
	public int Length;
	// byte[Length] Sample;

	public SampleHeader(ReadOnlySpan<byte> src)
	{
		if (BitConverter.IsLittleEndian)
		{
			this = MemoryMarshal.AsRef<SampleHeader>(src);
		}
		else
		{
			DoesLoop = ReadInt32LittleEndian(src.Slice(0, 4));
			SampleRate = ReadInt32LittleEndian(src.Slice(4, 4));
			LoopOffset = ReadInt32LittleEndian(src.Slice(8, 4));
			Length = ReadInt32LittleEndian(src.Slice(12, 4));
		}
	}

	public static SampleHeader Get(byte[] rom, int offset, out int sampleOffset)
	{
		sampleOffset = offset + SIZE;
		return new SampleHeader(rom.AsSpan(offset));
	}
}

internal struct ChannelVolume
{
	public float LeftVol, RightVol;
}
internal struct NoteInfo
{
	public byte Note, OriginalNote;
	public byte Velocity;
	/// <summary>-1 if forever</summary>
	public int Duration;
}
