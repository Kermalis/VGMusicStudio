namespace GBAMusicStudio.Core
{
    abstract class SongTable : IOffset
    {
        protected uint offset;
        readonly uint size;

        public SongTable(uint offset, uint size)
        {
            SetOffset(offset);
            this.size = size;
        }

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset) => offset = newOffset;

        public Song this[uint i] { get => LoadSong(i); }

        protected abstract Song LoadSong(uint i);
    }

    class M4ASongTable : SongTable
    {
        public M4ASongTable(uint offset, uint size) : base(offset, size) { }

        protected override Song LoadSong(uint i)
        {
            var entry = ROM.Instance.Reader.ReadObject<M4ASongEntry>(offset + (i * 8));
            return new M4AROMSong(entry.Header - ROM.Pak);
        }
    }

    class MLSSSongTable : SongTable
    {
        public MLSSSongTable(uint offset, uint size) : base(offset, size) { }

        protected override Song LoadSong(uint i)
        {
            return new MLSSSong(ROM.Instance.Reader.ReadUInt32(offset + (i * 4)) - ROM.Pak);
        }
    }
}
