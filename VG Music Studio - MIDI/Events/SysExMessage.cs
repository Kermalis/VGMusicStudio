using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class SysExMessage : MIDIMessage
{
	public byte[] Data { get; }

	public bool IsComplete => Data[Data.Length - 1] == 0xF7;

	internal SysExMessage(EndianBinaryReader r)
	{
		long offset = r.Stream.Position;

		int len = MIDIFile.ReadVariableLength(r);
		if (len == 0)
		{
			throw new InvalidDataException($"SysEx message at 0x{offset:X} was empty");
		}

		Data = new byte[len];
		r.ReadBytes(Data);
	}

	public SysExMessage(byte[] data)
	{
		if (data.Length == 0)
		{
			throw new ArgumentException("SysEx message must not be empty");
		}

		Data = data;
	}

	internal override byte GetCMDByte()
	{
		return 0xF0;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		MIDIFile.WriteVariableLength(w, Data.Length);
		w.WriteBytes(Data);
	}
}
