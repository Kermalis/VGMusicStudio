using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

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
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
internal struct VoiceEntry
{
	public const byte FIXED_FREQ_TRUE = 0x80;

	public byte MinKey;
	public byte MaxKey;
	public byte Sample;
	/// <summary>0x80 if True</summary>
	public byte IsFixedFrequency;
	public byte Unknown1;
	public byte Unknown2;
	public byte Unknown3;
	public byte Unknown4;
}

internal struct ChannelVolume
{
	public float LeftVol, RightVol;
}
internal class ADSR // TODO
{
	public byte A, D, S, R;
}
