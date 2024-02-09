﻿using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong : ILoadedSong
{
	public List<SongEvent>[] Events { get; }
	public long MaxTicks { get; private set; }
	public int LongestTrack;

	private readonly DSEPlayer _player;
	private readonly SWD? LocalSWD;
	private readonly byte[] SMDFile;
	public SMD.SongChunk SongChunk;
	public SMD.Header Header;
	public readonly DSETrack[] Tracks;

	public DSELoadedSong(DSEPlayer player, string bgm)
	{
		_player = player;
		//StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;

		if (_player.LocalSWD != null)
		{
			LocalSWD = _player.LocalSWD;
		}
		else
		{
			// Check if a local SWD is accompaning a SMD
			if (new FileInfo(Path.ChangeExtension(bgm, "swd")).Exists)
			{
				LocalSWD = new SWD(Path.ChangeExtension(bgm, "swd")); // If it exists, this will be loaded as the local SWD
			}
		}

		SMDFile = File.ReadAllBytes(bgm);
		using var stream = new MemoryStream(SMDFile);
		var r = new EndianBinaryReader(stream, ascii: true);
		Header = new SMD.Header(r);
		if (Header.Version != 0x415) { throw new DSEInvalidHeaderVersionException(Header.Version); }
		SongChunk = new SMD.SongChunk(r);

		Tracks = new DSETrack[SongChunk.NumTracks];
		Events = new List<SongEvent>[SongChunk.NumTracks];
		for (byte trackIndex = 0; trackIndex < SongChunk.NumTracks; trackIndex++)
		{
			long chunkStart = r.Stream.Position;
			r.Stream.Position += 0x14; // Skip header
			Tracks[trackIndex] = new DSETrack(trackIndex, (int)r.Stream.Position);

			AddTrackEvents(trackIndex, r);

			r.Stream.Position = chunkStart + 0xC;
			uint chunkLength = r.ReadUInt32();
			r.Stream.Position += chunkLength;
			r.Stream.Align(4);
		}
	}
}
