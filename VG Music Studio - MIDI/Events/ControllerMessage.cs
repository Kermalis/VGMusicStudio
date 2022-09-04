using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class ControllerMessage : MIDIMessage
{
	public byte Channel { get; }

	public ControllerType Controller { get; }
	public byte Value { get; }

	internal ControllerMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Controller = r.ReadEnum<ControllerType>();
		if (Controller >= ControllerType.MAX)
		{
			throw new InvalidDataException($"Invalid {nameof(ControllerMessage)} controller at 0x{r.Stream.Position - 1:X} ({Controller})");
		}

		Value = r.ReadByte();
		if (Value > 127)
		{
			throw new InvalidDataException($"Invalid {nameof(ControllerMessage)} value at 0x{r.Stream.Position - 1:X} ({Value})");
		}
	}

	public ControllerMessage(byte channel, ControllerType controller, byte value)
	{
		if (channel > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
		}
		if (controller >= ControllerType.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(controller), controller, null);
		}
		if (value > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		}

		Channel = channel;
		Controller = controller;
		Value = value;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xB0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Controller);
		w.WriteByte(Value);
	}
}

public enum ControllerType : byte
{
	// MSB
	BankSelect,
	ModulationWheel,
	BreathController,
	FootController = 4,
	PortamentoTime,
	DataEntry,
	ChannelVolume,
	Balance,
	Pan = 10,
	ExpressionController,
	EffectControl1,
	EffectControl2,
	GeneralPurposeController1 = 16,
	GeneralPurposeController2,
	GeneralPurposeController3,
	GeneralPurposeController4,
	// LSB
	BankSelectLSB = 32,
	ModulationWheelLSB,
	BreathControllerLSB,
	FootControllerLSB = 36,
	PortamentoTimeLSB,
	DataEntryLSB,
	ChannelVolumeLSB,
	BalanceLSB,
	PanLSB = 42,
	ExpressionControllerLSB,
	EffectControl1LSB,
	EffectControl2LSB,
	GeneralPurposeController1LSB = 48,
	GeneralPurposeController2LSB,
	GeneralPurposeController3LSB,
	GeneralPurposeController4LSB,
	SustainToggle = 64,
	PortamentoToggle,
	SostenutoToggle,
	SoftPedalToggle,
	LegatoToggle,
	Hold2Toggle,
	SoundController1,
	SoundController2,
	SoundController3,
	SoundController4,
	SoundController5,
	SoundController6,
	SoundController7,
	SoundController8,
	SoundController9,
	SoundController10,
	GeneralPurposeController5,
	GeneralPurposeController6,
	GeneralPurposeController7,
	GeneralPurposeController8,
	PortamentoControl,
	HighResolutionVelocityPrefix = 88,
	Effects1Depth = 91,
	Effects2Depth,
	Effects3Depth,
	Effects4Depth,
	Effects5Depth,
	DataIncrement,
	DataDecrement,
	NonRegisteredParameterNumberLSB,
	NonRegisteredParameterNumberMSB,
	RegisteredParameterNumberLSB,
	RegisteredParameterNumberMSB,
	AllSoundOff = 120,
	ResetAllControllers,
	LocalControlToggle,
	AllNotesOff,
	OmniModeOff,
	OmniModeOn,
	MonoModeOn,
	PolyModeOn,
	MAX,
}
