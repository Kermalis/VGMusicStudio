using Kermalis.VGMusicStudio.Core.Properties;
using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDATConfig : Config
{
	public readonly SDAT SDAT;

	internal SDATConfig(SDAT sdat)
	{
		if (sdat.INFOBlock.SequenceInfos.NumEntries == 0)
		{
			throw new Exception(Strings.ErrorSDATNoSequences);
		}

		SDAT = sdat;
		var songs = new List<Song>(sdat.INFOBlock.SequenceInfos.NumEntries);
		for (int i = 0; i < sdat.INFOBlock.SequenceInfos.NumEntries; i++)
		{
			if (sdat.INFOBlock.SequenceInfos.Entries[i] is not null)
			{
				songs.Add(new Song(i, sdat.SYMBBlock?.SequenceSymbols.Entries[i] ?? i.ToString()));
			}
		}
		Playlists.Add(new Playlist(Strings.PlaylistMusic, songs));
	}

	public override string GetGameName()
	{
		return "SDAT";
	}
	public override string GetSongName(long index)
	{
		return SDAT.SYMBBlock is null || index < 0 || index >= SDAT.SYMBBlock.SequenceSymbols.NumEntries
			? index.ToString()
			: '\"' + SDAT.SYMBBlock.SequenceSymbols.Entries[index] + '\"';
	}
}
