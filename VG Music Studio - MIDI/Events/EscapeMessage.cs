using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.VGMusicStudio.MIDI;

public sealed class EscapeMessage : MIDIMessage
{
	public byte[] Data { get; }

	internal EscapeMessage(EndianBinaryReader r)
	{
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

	public EscapeMessage(byte[] data)
	{
		Data = data;
	}

	internal override byte GetCMDByte()
	{
		return 0xF7;
	}

	internal override void Write(EndianBinaryWriter w)
	{
		MIDIFile.WriteVariableLength(w, Data.Length);
		w.WriteBytes(Data);
	}
}
