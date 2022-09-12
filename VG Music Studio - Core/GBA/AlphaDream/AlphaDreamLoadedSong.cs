using Kermalis.EndianBinaryIO;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed partial class AlphaDreamLoadedSong : ILoadedSong
{
	public List<SongEvent>?[] Events { get; }
	public long MaxTicks { get; private set; }
	public int LongestTrack;

	private readonly AlphaDreamPlayer _player;

	public AlphaDreamLoadedSong(AlphaDreamPlayer player, int songOffset)
	{
		_player = player;

		Events = new List<SongEvent>[AlphaDreamPlayer.NUM_TRACKS];
		songOffset -= GBAUtils.CARTRIDGE_OFFSET;
		EndianBinaryReader r = player.Config.Reader;
		r.Stream.Position = songOffset;
		ushort trackBits = r.ReadUInt16();
		int usedTracks = 0;
		for (byte trackIndex = 0; trackIndex < AlphaDreamPlayer.NUM_TRACKS; trackIndex++)
		{
			AlphaDreamTrack track = player.Tracks[trackIndex];
			if ((trackBits & (1 << trackIndex)) == 0)
			{
				track.IsEnabled = false;
				track.StartOffset = 0;
				continue;
			}

			track.IsEnabled = true;
			r.Stream.Position = songOffset + 2 + (2 * usedTracks++);
			track.StartOffset = songOffset + r.ReadInt16();

			AddTrackEvents(trackIndex, track.StartOffset);
		}
	}
}
