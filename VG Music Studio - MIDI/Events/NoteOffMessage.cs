﻿using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class NoteOffMessage : MIDIMessage
{
	public byte Channel { get; }

	public MIDINote Note { get; }
	public byte Velocity { get; }

	internal NoteOffMessage(EndianBinaryReader r, byte channel)
	{
		Channel = channel;

		Note = r.ReadEnum<MIDINote>();
		if (Note >= MIDINote.MAX)
		{
			throw new InvalidDataException($"Invalid {nameof(NoteOffMessage)} note at 0x{r.Stream.Position - 1:X} ({Note})");
		}

		Velocity = r.ReadByte();
		if (Velocity > 127)
		{
			throw new InvalidDataException($"Invalid {nameof(NoteOffMessage)} velocity at 0x{r.Stream.Position - 1:X} ({Velocity})");
		}
	}

	public NoteOffMessage(byte channel, MIDINote note, byte velocity)
	{
		if (channel > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
		}
		if (note >= MIDINote.MAX)
		{
			throw new ArgumentOutOfRangeException(nameof(note), note, null);
		}
		if (velocity > 127)
		{
			throw new ArgumentOutOfRangeException(nameof(velocity), velocity, null);
		}

		Channel = channel;
		Note = note;
		Velocity = velocity;
	}

	internal override byte GetCMDByte()
	{
		return (byte)(0x80 + Channel);
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(Note);
		w.WriteByte(Velocity);
	}
}
