using Kermalis.EndianBinaryIO;

namespace Kermalis.DLS2
{
    // Region Header Chunk - Page 45 of spec
    public sealed class RegionHeaderChunk : DLSChunk
    {
        public Range KeyRange { get; set; }
        public Range VelocityRange { get; set; }
        public ushort Options { get; set; }
        public ushort KeyGroup { get; set; }
        public ushort Layer { get; set; }

        public RegionHeaderChunk() : base("rgnh")
        {
            KeyRange = new Range(0, 127);
            VelocityRange = new Range(0, 127);
        }
        internal RegionHeaderChunk(EndianBinaryReader reader) : base("rgnh", reader)
        {
            long endOffset = GetEndOffset(reader);
            KeyRange = new Range(reader);
            VelocityRange = new Range(reader);
            Options = reader.ReadUInt16();
            KeyGroup = reader.ReadUInt16();
            if (Size >= 14) // Size of 12 is also valid
            {
                Layer = reader.ReadUInt16();
            }
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 4 // KeyRange
                + 4 // VelocityRange
                + 2 // Options
                + 2 // KeyGroup
                + 2; // Layer
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            KeyRange.Write(writer);
            VelocityRange.Write(writer);
            writer.Write(Options);
            writer.Write(KeyGroup);
            writer.Write(Layer);
        }
    }
}
