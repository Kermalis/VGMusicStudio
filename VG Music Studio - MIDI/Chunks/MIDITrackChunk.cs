using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class MIDITrackChunk : MIDIChunk
{
	internal const string EXPECTED_NAME = "MTrk";

	public MIDIEvent? First { get; private set; }
	public MIDIEvent? Last { get; private set; }

	/// <summary>Includes the end of track event</summary>
	public int NumEvents { get; private set; }
	public int NumTicks => Last is null ? 0 : Last.Ticks;

	internal MIDITrackChunk()
	{
		//
	}
	internal MIDITrackChunk(uint size, EndianBinaryReader r)
	{
		long endOffset = GetEndOffset(r, size);

		int ticks = 0;
		byte runningStatus = 0;
		bool foundEnd = false;
		bool sysexContinue = false;
		while (r.Stream.Position < endOffset)
		{
			if (foundEnd)
			{
				throw new InvalidDataException("Events found after the EndOfTrack MetaMessage");
			}

			ticks += MIDIFile.ReadVariableLength(r);

			// Get command
			byte cmd = r.ReadByte();
			if (sysexContinue && cmd != 0xF7)
			{
				throw new InvalidDataException($"SysExContinuationMessage was missing at 0x{r.Stream.Position - 1:X}");
			}
			if (cmd < 0x80)
			{
				cmd = runningStatus;
				r.Stream.Position--;
			}

			// Check which message it is
			if (cmd >= 0x80 && cmd <= 0xEF)
			{
				runningStatus = cmd;
				byte channel = (byte)(cmd & 0xF);
				switch (cmd & ~0xF)
				{
					case 0x80: Insert(ticks, new NoteOnMessage(r, channel)); break;
					case 0x90: Insert(ticks, new NoteOffMessage(r, channel)); break;
					case 0xA0: Insert(ticks, new PolyphonicPressureMessage(r, channel)); break;
					case 0xB0: Insert(ticks, new ControllerMessage(r, channel)); break;
					case 0xC0: Insert(ticks, new ProgramChangeMessage(r, channel)); break;
					case 0xD0: Insert(ticks, new ChannelPressureMessage(r, channel)); break;
					case 0xE0: Insert(ticks, new PitchBendMessage(r, channel)); break;
				}
			}
			else if (cmd == 0xF0)
			{
				runningStatus = 0;
				var msg = new SysExMessage(r);
				if (!msg.IsComplete)
				{
					sysexContinue = true;
				}
			}
			else if (cmd == 0xF7)
			{
				runningStatus = 0;
				if (sysexContinue)
				{
					var msg = new SysExContinuationMessage(r);
					if (msg.IsFinished)
					{
						sysexContinue = false;
					}
				}
				else
				{
					Insert(ticks, new EscapeMessage(r));
				}
			}
			else if (cmd == 0xFF)
			{
				var msg = new MetaMessage(r);
				if (msg.Type == MetaMessageType.EndOfTrack)
				{
					foundEnd = true;
				}
				Insert(ticks, msg);
			}
			else
			{
				throw new InvalidDataException($"Unknown MIDI command found at 0x{r.Stream.Position - 1:X} (0x{cmd:X})");
			}
		}

		if (!foundEnd)
		{
			throw new InvalidDataException("Could not find EndOfTrack MetaMessage");
		}
		if (r.Stream.Position > endOffset)
		{
			throw new InvalidDataException("Expected to read a certain amount of events, but the data was read incorrectly...");
		}
	}

	public void Insert(int ticks, MIDIMessage msg)
	{
		if (ticks < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ticks), ticks, null);
		}

		var e = new MIDIEvent(ticks, msg);

		if (NumEvents == 0)
		{
			First = e;
			Last = e;
		}
		else if (ticks < First!.Ticks)
		{
			e.Next = First;
			First.Prev = e;
			First = e;
		}
		else if (ticks >= Last!.Ticks)
		{
			e.Prev = Last;
			Last.Next = e;
			Last = e;
		}
		else // Somewhere between
		{
			MIDIEvent next = First;

			while (next.Ticks <= ticks)
			{
				next = next.Next!;
			}

			MIDIEvent prev = next.Prev!;

			e.Next = next;
			e.Prev = prev;
			prev.Next = e;
			next.Prev = e;
		}

		NumEvents++;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		w.WriteChars_Count(EXPECTED_NAME, 4);

		long sizeOffset = w.Stream.Position;
		w.WriteUInt32(0); // We will update the size later

		byte runningStatus = 0;
		bool foundEnd = false;
		bool sysexContinue = false;
		for (MIDIEvent? e = First; e is not null; e = e.Next)
		{
			if (foundEnd)
			{
				throw new InvalidDataException("Events found after the EndOfTrack MetaMessage");
			}

			MIDIFile.WriteVariableLength(w, e.DeltaTicks);

			MIDIMessage msg = e.Message;
			byte cmd = msg.GetCMDByte();
			if (sysexContinue && cmd != 0xF7)
			{
				throw new InvalidDataException("SysExContinuationMessage was missing");
			}

			if (cmd >= 0x80 && cmd <= 0xEF)
			{
				if (runningStatus != cmd)
				{
					runningStatus = cmd;
					w.WriteByte(cmd);
				}
			}
			else if (cmd == 0xF0)
			{
				runningStatus = 0;
				var sysex = (SysExMessage)msg;
				if (!sysex.IsComplete)
				{
					sysexContinue = true;
				}
				w.WriteByte(0xF0);
			}
			else if (cmd == 0xF7)
			{
				runningStatus = 0;
				if (sysexContinue)
				{
					var sysex = (SysExContinuationMessage)msg;
					if (sysex.IsFinished)
					{
						sysexContinue = false;
					}
				}
				w.WriteByte(0xF0);
			}
			else if (cmd == 0xFF)
			{
				var meta = (MetaMessage)msg;
				if (meta.Type == MetaMessageType.EndOfTrack)
				{
					foundEnd = true;
				}
				w.WriteByte(0xFF);
			}
			else
			{
				throw new InvalidDataException($"Unknown MIDI command 0x{cmd:X}");
			}

			msg.Write(w);
		}
		if (!foundEnd)
		{
			throw new InvalidDataException("You must insert an EndOfTrack MetaMessage");
		}

		// Update size now
		uint size = (uint)(w.Stream.Position - sizeOffset + 4);
		w.Stream.Position = sizeOffset;
		w.WriteUInt32(size);
	}
}
