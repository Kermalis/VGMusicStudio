using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSENoSequencesException : Exception
{
	public string BGMPath { get; }

	internal DSENoSequencesException(string bgmPath)
	{
		BGMPath = bgmPath;
	}
}

public sealed class DSEInvalidHeaderVersionException : Exception
{
	public ushort Version { get; }

	internal DSEInvalidHeaderVersionException(ushort version)
	{
		Version = version;
	}
}

public sealed class DSEInvalidNoteException : Exception
{
	public byte TrackIndex { get; }
	public int Offset { get; }
	public int Note { get; }

	internal DSEInvalidNoteException(byte trackIndex, int offset, int note)
	{
		TrackIndex = trackIndex;
		Offset = offset;
		Note = note;
	}
}

public sealed class DSEInvalidCMDException : Exception
{
	public byte TrackIndex { get; }
	public int CmdOffset { get; }
	public byte Cmd { get; }

	internal DSEInvalidCMDException(byte trackIndex, int cmdOffset, byte cmd)
	{
		TrackIndex = trackIndex;
		CmdOffset = cmdOffset;
		Cmd = cmd;
	}
}