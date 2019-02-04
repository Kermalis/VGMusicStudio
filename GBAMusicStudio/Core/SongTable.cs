namespace GBAMusicStudio.Core
{
    abstract class SongTable : IOffset
    {
        protected int offset;
        readonly int size;

        public SongTable(int offset, int size)
        {
            SetOffset(offset);
            this.size = size;
        }

        public int GetOffset() => offset;
        public void SetOffset(int newOffset) => offset = newOffset;

        public Song this[int i] { get => LoadSong(i); }

        protected abstract Song LoadSong(int i);
    }

    class M4ASongTable : SongTable
    {
        public M4ASongTable(int offset, int size) : base(offset, size) { }

        protected override Song LoadSong(int i)
        {
            M4ASongEntry entry = ROM.Instance.Reader.ReadObject<M4ASongEntry>(offset + (i * 8));
            return new M4AROMSong(entry.Header - ROM.Pak);
        }
    }

    class MLSSSongTable : SongTable
    {
        public MLSSSongTable(int offset, int size) : base(offset, size) { }

        protected override Song LoadSong(int i)
        {
            return new MLSSROMSong(ROM.Instance.Reader.ReadInt32(offset + (i * 4)) - ROM.Pak);
        }
    }
}
