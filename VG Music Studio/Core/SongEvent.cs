using System.Collections.Generic;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core
{
    internal interface ICommand
    {
        Color Color { get; }
        string Label { get; }
        string Arguments { get; }
    }
    internal class SongEvent
    {
        public long Offset { get; }
        public List<long> Ticks { get; } = new List<long>();
        public ICommand Command { get; }

        public SongEvent(long offset, ICommand command)
        {
            Offset = offset;
            Command = command;
        }
    }
}
