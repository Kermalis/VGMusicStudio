using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class SDATConfig : Config
    {
        public SDAT SDAT;

        public SDATConfig(SDAT sdat)
        {
            SDAT = sdat;
            IEnumerable<Song> songs = Enumerable.Range(0, sdat.INFOBlock.SequenceInfos.NumEntries)
                .Where(i => sdat.INFOBlock.SequenceInfos.Entries[i] != null)
                .Select(i => new Song(i, sdat.SYMBBlock == null ? i.ToString() : sdat.SYMBBlock.SequenceSymbols.Entries[i]));
            Playlists.Add(new Playlist("Music", songs));
        }
    }
}
