using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.MIDI;

public abstract class MIDIMessage
{
	internal abstract byte GetCMDByte();

	internal abstract void Write(EndianBinaryWriter w);
}
