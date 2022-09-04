using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class MetaMessage : MIDIMessage
{
	public MetaMessageType Type { get; }
	public byte[] Data { get; }

	internal MetaMessage(EndianBinaryReader r)
	{
		Type = r.ReadEnum<MetaMessageType>();
		if (Type >= MetaMessageType.MAX)
		{
			throw new InvalidDataException($"Invalid {nameof(MetaMessage)} type at 0x{r.Stream.Position - 1:X} ({Type})");
		}

		int len = MIDIFile.ReadVariableLength(r);
		if (len == 0)
		{
			Data = Array.Empty<byte>();
		}
		else
		{
			Data = new byte[len];
			r.ReadBytes(Data);
		}
	}

	public MetaMessage(MetaMessageType type, byte[] data)
	{
		if (type >= MetaMessageType.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}

		Type = type;
		Data = data;
	}

	public static MetaMessage CreateTempoMessage(int tempo)
	{
		if (tempo <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(tempo), tempo, null);
		}

		tempo = 60_000_000 / tempo;
		byte[] data = new byte[3];
		for (int i = 0; i < 3; i++)
		{
			data[2 - i] = (byte)(tempo >> (i * 8));
		}
		return new MetaMessage(MetaMessageType.Tempo, data);
	}

	public static MetaMessage CreateTimeSignatureMessage(byte numerator, byte denominator, byte clocksPerMetronomeClick = 24, byte num32ndNotesPerQuarterNote = 8)
	{
		if (numerator == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(numerator), numerator, null);
		}
		if (denominator < 2 || denominator > 32)
		{
			throw new ArgumentOutOfRangeException(nameof(denominator), denominator, null);
		}
		if ((denominator & (denominator - 1)) != 0)
		{
			throw new ArgumentException("Denominator must be a power of 2", nameof(denominator));
		}
		if (clocksPerMetronomeClick == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(clocksPerMetronomeClick), clocksPerMetronomeClick, null);
		}
		if (num32ndNotesPerQuarterNote == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(num32ndNotesPerQuarterNote), num32ndNotesPerQuarterNote, null);
		}

		byte[] data = new byte[4];
		data[0] = numerator;
		data[1] = (byte)Math.Log(denominator, 2);
		data[2] = clocksPerMetronomeClick;
		data[3] = num32ndNotesPerQuarterNote;
		return new MetaMessage(MetaMessageType.TimeSignature, data);
	}

	internal override byte GetCMDByte()
	{
		return 0xFF;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Type);
		MIDIFile.WriteVariableLength(w, Data.Length);
		w.WriteBytes(Data);
	}
}

public enum MetaMessageType : byte
{
	SequenceNumber,
	Text,
	Copyright,
	TrackName,
	InstrumentName,
	Lyric,
	Marker,
	CuePoint,
	ProgramName,
	DeviceName,
	EndOfTrack = 0x2F,
	Tempo = 0x51,
	SMPTEOffset = 0x54,
	TimeSignature = 0x58,
	KeySignature,
	ProprietaryEvent = 0x7F,
	MAX,
}