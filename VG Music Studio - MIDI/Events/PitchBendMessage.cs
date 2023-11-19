using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class PitchBendMessage : MIDIMessage
{
	public byte Channel { get; }

	public byte LSB { get; }
	public byte MSB { get; }

	internal PitchBendMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		LSB = r.ReadByte();
		if (LSB > 127)
		{
			throw new InvalidDataException($"Invalid {nameof(PitchBendMessage)} LSB value at 0x{r.Stream.Position - 1:X} ({LSB})");
		}

		MSB = r.ReadByte();
		if (MSB > 127)
		{
			throw new InvalidDataException($"Invalid {nameof(PitchBendMessage)} MSB value at 0x{r.Stream.Position - 1:X} ({MSB})");
		}
	}

	public PitchBendMessage(byte channel, byte lsb, byte msb)
	{
		if (channel > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
		}
		if (lsb > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(lsb), lsb, null);
		}
		if (msb > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(msb), msb, null);
		}

		Channel = channel;
		LSB = lsb;
		MSB = msb;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0xE0 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteByte(LSB);
		w.WriteByte(MSB);
	}
}
