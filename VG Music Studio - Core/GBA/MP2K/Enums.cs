using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
	internal enum EnvelopeState : byte
	{
		Initializing,
		Rising,
		Decaying,
		Playing,
		Releasing,
		Dying,
		Dead,
	}
	internal enum ReverbType : byte
	{
		None,
		Normal,
		Camelot1,
		Camelot2,
		MGAT,
	}

	internal enum GoldenSunPSGType : byte
	{
		Square,
		Saw,
		Triangle,
	}
	internal enum LFOType : byte
	{
		Pitch,
		Volume,
		Panpot,
	}
	internal enum SquarePattern : byte
	{
		D12,
		D25,
		D50,
		D75,
	}
	internal enum NoisePattern : byte
	{
		Fine,
		Rough,
	}
	internal enum VoiceType : byte
	{
		PCM8,
		Square1,
		Square2,
		PCM4,
		Noise,
		Invalid5,
		Invalid6,
		Invalid7,
	}
	[Flags]
	internal enum VoiceFlags : byte
	{
		// These are flags that apply to the types
		/// <summary>PCM8</summary>
		Fixed = 0x08,
		/// <summary>Square1, Square2, PCM4, Noise</summary>
		OffWithNoise = 0x08,
		/// <summary>PCM8</summary>
		Reversed = 0x10,
		/// <summary>PCM8 (Only in Pokémon main series games)</summary>
		Compressed = 0x20,

		// These are flags that cancel out every other bit after them if set so they should only be checked with equality
		KeySplit = 0x40,
		Drum = 0x80,
	}
}
