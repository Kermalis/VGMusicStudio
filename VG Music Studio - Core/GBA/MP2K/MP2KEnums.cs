using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal enum ResamplerType : byte
{
	Nearest,
	Linear,
	Sinc,
	Blep,
	Blamp,
}
internal enum EnvelopeState : byte
{
	Initializing,
	Rising,
	Decaying,
	Playing,
	Releasing,
	PseudoEcho,
	Dying,
	Dead,
}
internal enum SequenceCommand : byte
{
	Fine = 0xB1 | 0xB6,
	Goto = 0xB2,
	Pattern = 0xB3,
	PatternEnd = 0xB4,
	Repeat = 0xB5,
	MemoryAccess = 0xB9,
	Priority = 0xBA,
	Tempo = 0xBB,
	KeyShift = 0xBC,
	Voice = 0xBD,
	Volume = 0xBE,
	Panpot = 0xBF,
	Bend = 0xC0,
	BendRange = 0xC1,
	LFOSpeed = 0xC2,
	LFODelay = 0xC3,
	Modulation = 0xC4,
	ModulationType = 0xC5,
	Tune = 0xC8,
	ExtendedCommand = 0xCD,
	EndOfTie = 0xCE,
}
internal enum ReverbType : byte
{
	None,
	Normal,
	Camelot1,
	Camelot2,
	MGAT,
}

internal enum SynthType : byte
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
/// <summary>
/// These flags determines the voice type used, whenever it be either PCM8 data or a PSG format
/// </summary>
internal enum VoiceType : byte
{
	/// <summary>Raw PCM8 data</summary>
	PCM8,
	/// <summary>PSG Square Wave Type 1</summary>
	Square1,
	/// <summary>PSG Square Wave Type 2</summary>
	Square2,
	/// <summary>PSG PCM4 Wave Data</summary>
	PCM4,
	/// <summary>PSG Noise Wave</summary>
	Noise,
	/// <summary>Unused</summary>
	Invalid5,
	/// <summary>Unused</summary>
	Invalid6,
	/// <summary>Unused</summary>
	Invalid7,
}
/// <summary>
/// These are flags that apply to the voice types
/// </summary>
[Flags]
internal enum VoiceFlags : byte
{
	/// <summary>PCM8 only</summary>
	Fixed = 0x08,
	/// <summary>Only used with PSG types: Square1, Square2, PCM4, Noise</summary>
	OffWithNoise = 0x09,
	/// <summary>PCM8 only</summary>
	Reversed = 0x10,
	/// <summary>PCM8 (Only in Pokémon main series games)</summary>
	Compressed = 0x20,

	// These are flags that cancel out every other bit after them if set so they should only be checked with equality
	KeySplit = 0x40,
	Drum = 0x80,
}
internal enum VoiceConfigType : int
{
	None = 0x0,
	PCM = 0x1,
	DPCM = 0x2,
	IMAADPCM = 0x4,
	SynthPWM = 0x8,
	SynthSawtooth = 0x10,
	SynthTriangle = 0x20,
	PSGSquare12 = 0x40,
	PSGSquare25 = 0x80,
	PSGSquare50 = 0x100,
	PSGSquare75 = 0x200,
	PSGSquare12Sweep = 0x400,
	PSGSquare25Sweep = 0x800,
	PSGSquare50Sweep = 0x1000,
	PSGSquare75Sweep = 0x2000,
	PSGPCM4 = 0x4000,
	PSGNoise7 = 0x8000,
	PSGNoise15 = 0x10000,
	Invalid = -0x1,
}
internal enum CodecType : ushort
{
	/// <summary>
	/// Standalone signed PCM8, provides no compression whatsoever, most commonly used in most games, and used by default
	/// </summary>
	PCM8 = 0,
	/// <summary>
	/// Used in some Game Freak games, and other third party developers
	/// </summary>
	DPCM = 1,
	/// <summary>
	/// Used in some Camelot games, and other third party developers
	/// </summary>
	IMAADPCM = 4,
}
internal enum MemoryOperatorType : byte
{
	MemSet,
	MemAdd,
	MemSub,
	MemMemSet,
	MemMemAdd,
	MemMemSub,
	MemBEq,
	MemBNEq,
	MemBHi,
	MemBHS,
	MemBLS,
	MemBLo,
	MemMemBEq,
	MemMemBNEq,
	MemMemBHi,
	MemMemBHS,
	MemMemBLS,
	MemMemBLo,
}
internal enum ExtendedCommandType : byte
{
	xWAVE = 1,
	xTYPE = 2,
	xATTA = 4,
	xDECA = 5,
	xSUST = 6,
	xRELA = 7,
	xIECV = 8,
	xIECL = 9,
	xLENG = 10,
	xSWEE = 11,
	xWAIT = 12,
	xSOFF = 13,
}
internal enum PSGPolyphony : byte
{
	MONO_STRICT,
	MONO_SMOOTH,
	POLY
}
