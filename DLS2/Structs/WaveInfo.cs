using Kermalis.EndianBinaryIO;

namespace Kermalis.DLS2
{
    public sealed class WaveInfo
    {
        public WaveFormat FormatTag { get; set; }
        public ushort Channels { get; set; }
        public uint SamplesPerSec { get; set; }
        public uint AvgBytesPerSec { get; set; }
        public ushort BlockAlign { get; set; }

        internal WaveInfo() { }
        internal WaveInfo(EndianBinaryReader reader)
        {
            FormatTag = reader.ReadEnum<WaveFormat>();
            Channels = reader.ReadUInt16();
            SamplesPerSec = reader.ReadUInt32();
            AvgBytesPerSec = reader.ReadUInt32();
            BlockAlign = reader.ReadUInt16();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(FormatTag);
            writer.Write(Channels);
            writer.Write(SamplesPerSec);
            writer.Write(AvgBytesPerSec);
            writer.Write(BlockAlign);
        }
    }
}
