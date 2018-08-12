namespace GBAMusicStudio.Core
{
    internal abstract class SongTable
    {
        internal uint Offset;
        readonly uint size;

        internal SongTable(uint offset, uint size)
        {
            Offset = offset;
            this.size = size;
        }

        internal Song this[int i] { get => LoadSong(i); }

        protected abstract Song LoadSong(int i);
    }

    internal class M4ASongTable : SongTable
    {
        internal M4ASongTable(uint offset, uint size) : base(offset, size) { }

        protected override Song LoadSong(int i)
        {
            var entry = ROM.Instance.ReadStruct<M4ASongEntry>((uint)(Offset + (i * 8)));
            return new M4AROMSong(entry.Header);
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
