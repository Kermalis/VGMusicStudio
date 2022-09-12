using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong : ILoadedSong
{
	public List<SongEvent>[] Events { get; }
	public long MaxTicks { get; private set; }
	public int LongestTrack;

	private readonly MP2KPlayer _player;
	private readonly int _voiceTableOffset;
	public readonly MP2KTrack[] Tracks;

	public MP2KLoadedSong(MP2KPlayer player, int index)
	{
		_player = player;

		MP2KConfig cfg = player.Config;
		var entry = SongEntry.Get(cfg.ROM, cfg.SongTableOffsets[0], index);
		int headerOffset = entry.HeaderOffset - GBAUtils.CARTRIDGE_OFFSET;

		var header = SongHeader.Get(cfg.ROM, headerOffset, out int tracksOffset);
		_voiceTableOffset = header.VoiceTableOffset - GBAUtils.CARTRIDGE_OFFSET;

		Tracks = new MP2KTrack[header.NumTracks];
		Events = new List<SongEvent>[header.NumTracks];
		for (byte trackIndex = 0; trackIndex < header.NumTracks; trackIndex++)
		{
			int trackStart = SongHeader.GetTrackOffset(cfg.ROM, tracksOffset, trackIndex) - GBAUtils.CARTRIDGE_OFFSET;
			Tracks[trackIndex] = new MP2KTrack(trackIndex, trackStart);

			AddTrackEvents(trackIndex, trackStart);
		}
	}

	public void CheckVoiceTypeCache(ref int? old, string?[] voiceTypeCache)
	{
		if (old != _voiceTableOffset)
		{
			old = _voiceTableOffset;
			Array.Clear(voiceTypeCache);
		}
	}
}
