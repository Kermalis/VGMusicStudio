using System;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDATInvalidCMDException : Exception
{
	public byte TrackIndex { get; }
	public int CmdOffset { get; }
	public byte Cmd { get; }

	internal SDATInvalidCMDException(byte trackIndex, int cmdOffset, byte cmd)
	{
		TrackIndex = trackIndex;
		CmdOffset = cmdOffset;
		Cmd = cmd;
	}
}

public sealed class SDATTooManyNestedCallsException : Exception
{
	public byte TrackIndex { get; }

	internal SDATTooManyNestedCallsException(byte trackIndex)
	{
		TrackIndex = trackIndex;
	}
}
