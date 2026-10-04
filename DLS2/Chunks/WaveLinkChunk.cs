using Kermalis.EndianBinaryIO;

namespace Kermalis.DLS2
{
    public sealed class WaveLinkChunk : DLSChunk
    {
        public WaveLinkOptions Options { get; set; }
        public ushort PhaseGroup { get; set; }
        public WaveLinkChannels Channels { get; set; }
        public uint TableIndex { get; set; }

        public WaveLinkChunk() : base("wlnk")
        {
            Channels = WaveLinkChannels.Left;
        }
        internal WaveLinkChunk(EndianBinaryReader reader) : base("wlnk", reader)
        {
            long endOffset = GetEndOffset(reader);
            Options = reader.ReadEnum<WaveLinkOptions>();
            PhaseGroup = reader.ReadUInt16();
            Channels = reader.ReadEnum<WaveLinkChannels>();
            TableIndex = reader.ReadUInt32();
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 2 // Options
                + 2 // PhaseGroup
                + 4 // Channel
                + 4; // TableIndex
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(Options);
            writer.Write(PhaseGroup);
            writer.Write(Channels);
            writer.Write(TableIndex);
        }
    }
}
