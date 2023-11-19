using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class ProgramChangeMessage : MIDIMessage
{
	public byte Channel { get; }

	public MIDIProgram Program { get; }

	internal ProgramChangeMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Program = r.ReadEnum<MIDIProgram>();
		if (Program >= MIDIProgram.MAX)
		{
			throw new InvalidDataException($"Invalid {nameof(ProgramChangeMessage)} program at 0x{r.Stream.Position - 1:X} ({Program})");
		}
	}

	public ProgramChangeMessage(byte channel, MIDIProgram program)
	{
		if (channel > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
		}
		if (program >= MIDIProgram.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(program), program, null);
		}

		Channel = channel;
		Program = program;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xC0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Program);
	}
}

public enum MIDIProgram : byte
{
	AcousticGrandPiano,
	BrightAcousticPiano,
	ElectricGrandPiano,
	HonkyTonkPiano,
	ElectricPiano1,
	ElectricPiano2,
	Harpsichord,
	Clavinet,
	Celesta,
	Glockenspiel,
	MusicBox,
	Vibraphone,
	Marimba,
	Xylophone,
	TubularBells,
	Dulcimer,
	DrawbarOrgan,
	PercussiveOrgan,
	RockOrgan,
	ChurchOrgan,
	ReedOrgan,
	Accordion,
	Harmonica,
	TangoAccordion,
	AcousticGuitarNylon,
	AcousticGuitarSteel,
	ElectricGuitarJazz,
	ElectricGuitarClean,
	ElectricGuitarMuted,
	OverdrivenGuitar,
	DistortionGuitar,
	GuitarHarmonics,
	AcousticBass,
	ElectricBassFinger,
	ElectricBassPick,
	FretlessBass,
	SlapBass1,
	SlapBass2,
	SynthBass1,
	SynthBass2,
	Violin,
	Viola,
	Cello,
	Contrabass,
	TremoloStrings,
	PizzicatoStrings,
	OrchestralHarp,
	Timpani,
	StringEnsemble1,
	StringEnsemble2,
	SynthStrings1,
	SynthStrings2,
	ChoirAahs,
	VoiceOohs,
	SynthVoice,
	OrchestraHit,
	Trumpet,
	Trombone,
	Tuba,
	MutedTrumpet,
	FrenchHorn,
	BrassSection,
	SynthBrass1,
	SynthBrass2,
	SopranoSax,
	AltoSax,
	TenorSax,
	BaritoneSax,
	Oboe,
	EnglishHorn,
	Bassoon,
	Clarinet,
	Piccolo,
	Flute,
	Recorder,
	PanFlute,
	BlownBottle,
	Shakuhachi,
	Whistle,
	Ocarina,
	Lead1Square,
	Lead2Sawtooth,
	Lead3Calliope,
	Lead4Chiff,
	Lead5Charang,
	Lead6Voice,
	Lead7Fifths,
	Lead8BassAndLead,
	Pad1NewAge,
	Pad2Warm,
	Pad3Polysynth,
	Pad4Choir,
	Pad5Bowed,
	Pad6Metallic,
	Pad7Halo,
	Pad8Sweep,
	Fx1Rain,
	Fx2Soundtrack,
	Fx3Crystal,
	Fx4Atmosphere,
	Fx5Brightness,
	Fx6Goblins,
	Fx7Echoes,
	Fx8SciFi,
	Sitar,
	Banjo,
	Shamisen,
	Koto,
	Kalimba,
	BagPipe,
	Fiddle,
	Shanai,
	TinkleBell,
	Agogo,
	SteelDrums,
	Woodblock,
	TaikoDrum,
	MelodicTom,
	SynthDrum,
	ReverseCymbal,
	GuitarFretNoise,
	BreathNoise,
	Seashore,
	BirdTweet,
	TelephoneRing,
	Helicopter,
	Applause,
	Gunshot,
	MAX,
}