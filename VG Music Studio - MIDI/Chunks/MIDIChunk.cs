using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.MIDI;

public abstract class MIDIChunk
{
	protected static long GetEndOffset(EndianBinaryReader r, uint size)
	{
		return r.Stream.Position + size;
	}
	protected static void EatRemainingBytes(EndianBinaryReader r, long endOffset, string chunkName, uint size)
	{
		if (r.Stream.Position > endOffset)
		{
			throw new InvalidDataException($"Chunk was too short ({chunkName} = {size})");
		}
		r.Stream.Position = endOffset;
	}

	internal abstract void Write(EndianBinaryWriter w);
}
