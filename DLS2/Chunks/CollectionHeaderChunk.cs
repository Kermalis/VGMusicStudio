using Kermalis.EndianBinaryIO;

namespace Kermalis.DLS2
{
    // Collection Header Chunk - Page 40 of spec
    public sealed class CollectionHeaderChunk : DLSChunk
    {
        public uint NumInstruments { get; internal set; }

        internal CollectionHeaderChunk() : base("colh") { }
        public CollectionHeaderChunk(EndianBinaryReader reader) : base("colh", reader)
        {
            long endOffset = GetEndOffset(reader);
            NumInstruments = reader.ReadUInt32();
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 4; // NumInstruments
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(NumInstruments);
        }
    }
}
