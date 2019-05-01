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
        public long Ticks { get; set; }
        public ICommand Command { get; }

        public SongEvent(long offset, ICommand command)
        {
            Offset = offset;
            Command = command;
        }
    }
}
