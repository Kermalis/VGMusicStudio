namespace GBAMusicStudio.Core
{
    abstract class SongTable
    {
        public readonly uint Offset;
        readonly uint size;

        public SongTable(uint offset, uint size)
        {
            Offset = offset;
            this.size = size;
        }

        public Song this[uint i] { get => LoadSong(i); }

        protected abstract Song LoadSong(uint i);
    }

    class M4ASongTable : SongTable
    {
        public M4ASongTable(uint offset, uint size) : base(offset, size) { }

        protected override Song LoadSong(uint i)
        {
            var entry = ROM.Instance.ReadStruct<M4ASongEntry>(Offset + (i * 8));
            return new M4AROMSong(entry.Header);
        }
    }

    class MLSSSongTable : SongTable
    {
        public MLSSSongTable(uint offset, uint size) : base(offset, size) { }

        protected override Song LoadSong(uint i)
        {
            return new MLSSSong(ROM.Instance.ReadUInt32(Offset + (i * 4)));
        }
    }
}
