namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal enum EnvelopeState : byte
{
	Initialize = 0,
	PlayNote = 1,
	Attack = 2,
	Hold = 3,
	Decay = 4,
	Fade = 5,
	Sustain = 6,
	End = 7,
	Release = 8,
}

internal enum ModulationType : byte
{
	None = 0,
	Pitch = 1,
	Volume = 2,
	Panpot = 3,
	FineTune = 4,
	CoarseTune = 5
}

internal enum WaveformType : byte
{
	None = 0,
	Square = 1,
	Triangle = 2,
	Sine = 3,
	Pulse = 4,
	Sawtooth = 5,
	Noise = 6,
	Random = 7
}

internal enum ParameterType : byte
{
	LFOTarget = 1,
	Enabled = 2,
	Route = 3,
	SelectWaveform = 4,
	LFORate = 5,
	LFODepth = 6,
	LFODelay = 7,
	LFODelayFine = 8,
	LFODelayCoarse = 9,
	LFOFade = 10
}

internal enum LFOType : byte
{
	Disconnected = 0,
	Pitch = 1,
	Volume = 2,
	Panpot = 3,
	Filter = 4
}

internal enum SampleFormat : ushort
{
	PCM8 = 0,
	PCM16 = 1,
	ADPCM = 2,
	PSG = 3
}
