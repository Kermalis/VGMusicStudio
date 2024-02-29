using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong : ILoadedSong
{
	public List<SongEvent>[] Events { get; }
	public long MaxTicks { get; private set; }
	public int LongestTrack;

	private readonly DSEPlayer _player;
	private readonly SWD LocalSWD;
	private readonly byte[] SMDFile;
	public readonly DSETrack[] Tracks;

	public DSELoadedSong(DSEPlayer player, string bgm)
	{
		_player = player;

		LocalSWD = new SWD(Path.ChangeExtension(bgm, "swd"));
		SMDFile = File.ReadAllBytes(bgm);
		using (var stream = new MemoryStream(SMDFile))
		{
			var r = new EndianBinaryReader(stream, ascii: true);
			SMD.Header header = r.ReadObject<SMD.Header>();
			SMD.ISongChunk songChunk;
			switch (header.Version)
			{
				case 0x402:
				{
					songChunk = r.ReadObject<SMD.SongChunk_V402>();
					break;
				}
				case 0x415:
				{
					songChunk = r.ReadObject<SMD.SongChunk_V415>();
					break;
				}
				default: throw new DSEInvalidHeaderVersionException(header.Version);
			}

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
