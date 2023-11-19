using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

// Section 2.1
public enum MIDIFormat : ushort
{
	/// <summary>Contains a single multi-channel track</summary>
	Format0,
	/// <summary>Contains one or more simultaneous tracks</summary>
	Format1,
	/// <summary>Contains one or more independent single-track patterns</summary>
	Format2,
}

// https://www.music.mcgill.ca/~ich/classes/mumt306/StandardMIDIfileformat.html
// http://www.somascape.org/midi/tech/mfile.html
public sealed class MIDIFile
{
	private readonly List<MIDIChunk> _nonHeaderChunks; // Not really important to expose this at the moment

	public MIDIHeaderChunk HeaderChunk { get; }

	private readonly List<MIDITrackChunk> _tracks;

	public MIDITrackChunk this[int index]
	{
		get => _tracks[index];
		set => _tracks[index] = value;
	}

	public MIDIFile(MIDIFormat format, TimeDivisionValue timeDivision, int tracksInitialCapacity)
	{
		if (format == MIDIFormat.Format0 && tracksInitialCapacity != 1)
		{
			throw new ArgumentException("Format 0 must have 1 track", nameof(tracksInitialCapacity));
		}

		HeaderChunk = new MIDIHeaderChunk(format, timeDivision);
		_nonHeaderChunks = new List<MIDIChunk>(tracksInitialCapacity);
		_tracks = new List<MIDITrackChunk>(tracksInitialCapacity);
	}
	public MIDIFile(Stream stream)
	{
		var r = new EndianBinaryReader(stream, endianness: Endianness.BigEndian, ascii: true);
		string chunkName = r.ReadString_Count(4);
		if (chunkName != MIDIHeaderChunk.EXPECTED_NAME)
		{
			throw new InvalidDataException("MIDI header was not at the start of the file");
		}

		HeaderChunk = (MIDIHeaderChunk)ReadChunk(r, alreadyReadName: chunkName);
		_nonHeaderChunks = new List<MIDIChunk>(HeaderChunk.NumTracks);
		_tracks = new List<MIDITrackChunk>(HeaderChunk.NumTracks);

		while (stream.Position < stream.Length)
		{
			MIDIChunk c = ReadChunk(r);
			_nonHeaderChunks.Add(c);
			if (c is MIDITrackChunk tc)
			{
				_tracks.Add(tc);
			}
		}

		if (_tracks.Count != HeaderChunk.NumTracks)
		{
			throw new InvalidDataException($"Unexpected track count: (Expected {HeaderChunk.NumTracks} but found {_tracks.Count}");
		}
	}

	private static MIDIChunk ReadChunk(EndianBinaryReader r, string? alreadyReadName = null)
	{
		string chunkName = alreadyReadName ?? r.ReadString_Count(4);
		uint chunkSize = r.ReadUInt32();
		switch (chunkName)
		{
			case MIDIHeaderChunk.EXPECTED_NAME: return new MIDIHeaderChunk(chunkSize, r);
			case MIDITrackChunk.EXPECTED_NAME: return new MIDITrackChunk(chunkSize, r);
			default: return new MIDIUnsupportedChunk(chunkName, chunkSize, r);
		}
	}

	internal static int ReadVariableLength(EndianBinaryReader r)
	{
		int value = r.ReadByte();

		if ((value & 0x80) != 0)
		{
			value &= 0x7F;

			byte c;
			do
			{
				c = r.ReadByte();
				value = (value << 7) + (c & 0x7F);
			} while ((c & 0x80) != 0);
		}

		return value;
	}
	internal static void WriteVariableLength(EndianBinaryWriter w, int value)
	{
		int buffer = value & 0x7F;
		while ((value >>= 7) > 0)
		{
			buffer <<= 8;
			buffer |= 0x80;
			buffer += value & 0x7F;
		}

		while (true)
		{
			w.WriteByte((byte)buffer);
			if ((buffer & 0x80) == 0)
			{
				break;
			}
			buffer >>= 8;
		}
	}
	internal static int GetVariableLengthNumBytes(int value)
	{
		int buffer = value & 0x7F;
		while ((value >>= 7) > 0)
		{
			buffer <<= 8;
			buffer |= 0x80;
			buffer += value & 0x7F;
		}

		int numBytes = 0;
		while (true)
		{
			numBytes++;
			if ((buffer & 0x80) == 0)
			{
				break;
			}
			buffer >>= 8;
		}

		return numBytes;
	}

	public MIDITrackChunk CreateTrack()
	{
		var tc = new MIDITrackChunk();
		_nonHeaderChunks.Add(tc);
		_tracks.Add(tc);
		HeaderChunk.NumTracks++;

		return tc;
	}

	public void Save(string fileName)
	{
		using (FileStream stream = File.Create(fileName))
		{
			var w = new EndianBinaryWriter(stream, endianness: Endianness.BigEndian, ascii: true);

			HeaderChunk.Write(w);

			foreach (MIDIChunk c in _nonHeaderChunks)
			{
				c.Write(w);
			}
		}
	}
}
