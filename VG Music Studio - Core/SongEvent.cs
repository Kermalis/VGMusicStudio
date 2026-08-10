using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core;

public sealed class SongEvent
{
	public long Offset { get; }
	public List<long> Ticks { get; }
	public ICommand Command { get; }

	public SongEvent(long offset, ICommand command)
	{
		Offset = offset;
		Ticks = new List<long>();
		Command = command;
	}
}
