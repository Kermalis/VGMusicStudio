using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal sealed partial class SDATLoadedSong : ILoadedSong
{
	public List<SongEvent>?[] Events { get; }
	public long MaxTicks { get; internal set; }
	public int LongestTrack;

	private readonly SDATPlayer _player;
	private readonly int _randSeed;
	private Random? _rand;
	public readonly SDAT.INFO.SequenceInfo SEQInfo; // TODO: Not public
	private readonly SSEQ _sseq;
	private readonly SBNK _sbnk;

	public SDATLoadedSong(SDATPlayer player, SDAT.INFO.SequenceInfo seqInfo)
	{
		_player = player;
		SEQInfo = seqInfo;

		SDAT sdat = player.Config.SDAT;
		_sseq = seqInfo.GetSSEQ(sdat);
		_sbnk = seqInfo.GetSBNK(sdat);
		_randSeed = Random.Shared.Next();
		// Cannot set random seed without creating a new object which is dumb

		Events = new List<SongEvent>[0x10];
		AddTrackEvents(0, 0);
	}

	private static SDATInvalidCMDException Invalid(byte trackIndex, int cmdOffset, byte cmd)
	{
		return new SDATInvalidCMDException(trackIndex, cmdOffset, cmd);
	}
}
