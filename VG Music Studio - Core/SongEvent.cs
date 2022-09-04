using System.Collections.Generic;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core;

public interface ICommand
{
	Color Color { get; }
	string Label { get; }
	string Arguments { get; }
}
public sealed class SongEvent
{
	public long Offset { get; }
	public List<long> Ticks { get; }
	public ICommand Command { get; }

	internal SongEvent(long offset, ICommand command)
	{
		Offset = offset;
		Ticks = new List<long>();
		Command = command;
	}
}
