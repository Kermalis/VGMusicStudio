using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed class MP2KInvalidCMDException : Exception
{
	public byte TrackIndex { get; }
	public int CmdOffset { get; }
	public byte Cmd { get; }

	internal MP2KInvalidCMDException(byte trackIndex, int cmdOffset, byte cmd)
	{
		TrackIndex = trackIndex;
		CmdOffset = cmdOffset;
		Cmd = cmd;
	}
}

public sealed class MP2KInvalidRunningStatusCMDException : Exception
{
	public byte TrackIndex { get; }
	public int CmdOffset { get; }
	public byte RunCmd { get; }

	internal MP2KInvalidRunningStatusCMDException(byte trackIndex, int cmdOffset, byte runCmd)
	{
		TrackIndex = trackIndex;
		CmdOffset = cmdOffset;
		RunCmd = runCmd;
	}
}

public sealed class MP2KTooManyNestedCallsException : Exception
{
	public byte TrackIndex { get; }

	internal MP2KTooManyNestedCallsException(byte trackIndex)
	{
		TrackIndex = trackIndex;
	}
}