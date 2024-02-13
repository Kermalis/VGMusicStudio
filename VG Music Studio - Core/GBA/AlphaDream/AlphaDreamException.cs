using System;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamInvalidCMDException : Exception
{
	public byte TrackIndex { get; }
	public int CmdOffset { get; }
	public byte Cmd { get; }

	internal AlphaDreamInvalidCMDException(byte trackIndex, int cmdOffset, byte cmd)
	{
		TrackIndex = trackIndex;
		CmdOffset = cmdOffset;
		Cmd = cmd;
	}
}
