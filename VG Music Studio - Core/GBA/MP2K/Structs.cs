using Kermalis.EndianBinaryIO;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
internal struct SongEntry
{
	public int HeaderOffset;
	public short Player;
	public byte Unknown1;
	public byte Unknown2;
}
internal class SongHeader
{
	public byte NumTracks { get; set; }
	public byte NumBlocks { get; set; }
	public byte Priority { get; set; }
	public byte Reverb { get; set; }
	public int VoiceTableOffset { get; set; }
	[BinaryArrayVariableLength(nameof(NumTracks))]
	public int[] TrackOffsets { get; set; }
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
internal struct VoiceEntry
{
	public byte Type; // 0
	public byte RootNote; // 1
	public byte Unknown; // 2
	public byte Pan; // 3
	/// <summary>SquarePattern for Square1/Square2, NoisePattern for Noise, Address for PCM8/PCM4/KeySplit/Drum</summary>
	public int Int4; // 4
	/// <summary>ADSR for PCM8/Square1/Square2/PCM4/Noise, KeysAddress for KeySplit</summary>
	public ADSR ADSR; // 8

	public int Int8 => (ADSR.R << 24) | (ADSR.S << 16) | (ADSR.D << 8) | (ADSR.A);
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
internal struct ADSR
{
	public byte A;
	public byte D;
	public byte S;
	public byte R;
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 6)]
internal struct GoldenSunPSG
{
	/// <summary>Always 0x80</summary>
	public byte Unknown;
	public GoldenSunPSGType Type;
	public byte InitialCycle;
	public byte CycleSpeed;
	public byte CycleAmplitude;
	public byte MinimumCycle;
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
internal struct SampleHeader
{
	public const int LOOP_TRUE = 0x40_000_000;

	/// <summary>0x40_000_000 if True</summary>
	public int DoesLoop;
	/// <summary>Right shift 10 for value</summary>
	public int SampleRate;
	public int LoopOffset;
	public int Length;
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
