using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class ChannelPressureMessage : MIDIMessage
{
	public byte Channel { get; }

	public byte Pressure { get; }

	internal ChannelPressureMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Pressure = r.ReadByte();
		if (Pressure > 127)
		{
			throw new InvalidDataException($"Invalid {nameof(ChannelPressureMessage)} pressure at 0x{r.Stream.Position - 1:X} ({Pressure})");
		}
	}

	public ChannelPressureMessage(byte channel, byte pressure)
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
		Pressure = pressure;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xD0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteByte(Pressure);
	}
}
