using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using static Kermalis.VGMusicStudio.Core.NDS.DSE.SMD;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong : ILoadedSong
{
	public List<SongEvent>[] Events { get; }
	public long MaxTicks { get; private set; }
	public int LongestTrack;

	private readonly DSEPlayer _player;
    private readonly string SWDFileName;
    private readonly string SMDFileName;
    private readonly SWD LocalSWD;
	private readonly byte[] SMDFile;
	public readonly DSETrack[] Tracks;

	public DSELoadedSong(DSEPlayer player, string bgm)
	{
		_player = player;
		SWDFileName = bgm;
		SMDFileName = bgm;
		StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
		//if (SWDFileName.StartsWith("bgm", comparison) == SMDFileName.StartsWith("bgm", comparison))
		//{
  //          LocalSWD = new SWD(Path.ChangeExtension(bgm, "swd"));
  //      }
  //      else if (SWDFileName.StartsWith("me") == SMDFileName.StartsWith("me"))
  //      {
  //          LocalSWD = new SWD(Path.ChangeExtension(bgm, "swd"));
  //      }
  //      else if (SWDFileName.StartsWith("se") == SMDFileName.StartsWith("se"))
  //      {
  //          LocalSWD = new SWD(Path.ChangeExtension(bgm, "swd"));
  //      }
		//else { }
		SMDFile = File.ReadAllBytes(bgm);
		using (var stream = new MemoryStream(SMDFile))
		{
			var r = new EndianBinaryReader(stream, ascii: true);
			Header header = new Header(r);
			if (header.Version != 0x415) { throw new DSEInvalidHeaderVersionException(header.Version); }
            SongChunk songChunk = new SongChunk(r);

			Tracks = new DSETrack[songChunk.NumTracks];
			Events = new List<SongEvent>[songChunk.NumTracks];
			for (byte trackIndex = 0; trackIndex < songChunk.NumTracks; trackIndex++)
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
}
