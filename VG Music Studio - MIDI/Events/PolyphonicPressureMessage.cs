using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class PolyphonicPressureMessage : MIDIMessage
{
	public byte Channel { get; }

	public MIDINote Note { get; }
	public byte Pressure { get; }

	internal PolyphonicPressureMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Note = r.ReadEnum<MIDINote>();

		Pressure = r.ReadByte();
		if (Pressure > 127)
		{
			throw new InvalidDataException($"Invalid PolyphonicPressureMessage pressure at 0x{r.Stream.Position - 1:X} ({Pressure})");
		}
	}

	public PolyphonicPressureMessage(byte channel, MIDINote note, byte pressure)
	{
		if (channel > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
		}
		if (pressure > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(pressure), pressure, null);
		}

		Channel = channel;
		Note = note;
		Pressure = pressure;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xA0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Note);
		w.WriteByte(Pressure);
	}
}
