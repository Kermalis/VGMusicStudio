using System.Collections.Generic;

namespace GBAMusicStudio.Core
{
    internal abstract class SongTable
    {
        internal uint Offset;
        uint size;
        protected readonly Dictionary<int, Song> Songs;

        internal SongTable(uint offset, uint size)
        {
            Offset = offset;
            this.size = size;
            Songs = new Dictionary<int, Song>((int)size);
        }

        internal Song this[int i]
        {
            get
            {
                if (!Songs.ContainsKey(i))
                    Songs[i] = LoadSong(i);
                return Songs[i];
            }
        }

        protected abstract Song LoadSong(int i);
    }

    internal class M4ASongTable : SongTable
    {
        readonly Dictionary<int, M4ASongEntry> entries;

        internal M4ASongTable(uint offset, uint size) : base(offset, size)
        {
            entries = new Dictionary<int, M4ASongEntry>((int)size);
        }

        protected override Song LoadSong(int i)
        {
            entries[i] = ROM.Instance.ReadStruct<M4ASongEntry>((uint)(Offset + (i * 8)));
            return new M4AROMSong(entries[i].Header);
        }
    }

    internal class MLSSSongTable : SongTable
    {
        internal MLSSSongTable(uint offset, uint size) : base(offset, size) { }

        protected override Song LoadSong(int i)
        {
            return new MLSSSong(ROM.Instance.ReadUInt32((uint)(Offset + (i * 4))));
        }
    }
}
