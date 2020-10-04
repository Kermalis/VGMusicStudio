using Kermalis.VGMusicStudio.Properties;
using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class Config : Core.Config
    {
        public readonly SDAT SDAT;

        public Config(SDAT sdat)
        {
            if (sdat.INFOBlock.SequenceInfos.NumEntries == 0)
            {
                throw new Exception(Strings.ErrorSDATNoSequences);
            }
            SDAT = sdat;
            var songs = new List<Song>(sdat.INFOBlock.SequenceInfos.NumEntries);
            for (int i = 0; i < sdat.INFOBlock.SequenceInfos.NumEntries; i++)
            {
                if (sdat.INFOBlock.SequenceInfos.Entries[i] != null)
                {
                    songs.Add(new Song(i, sdat.SYMBBlock is null ? i.ToString() : sdat.SYMBBlock.SequenceSymbols.Entries[i]));
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
}
