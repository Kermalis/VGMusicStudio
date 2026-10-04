using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.DLS2
{
    public sealed class WaveSampleLoop
    {
        public LoopType LoopType { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopLength { get; set; }

        public WaveSampleLoop() { }
        internal WaveSampleLoop(EndianBinaryReader reader)
        {
            uint byteSize = reader.ReadUInt32();
            if (byteSize != 16)
            {
                throw new InvalidDataException();
            }
            LoopType = reader.ReadEnum<LoopType>();
            LoopStart = reader.ReadUInt32();
            LoopLength = reader.ReadUInt32();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(16u);
            writer.Write(LoopType);
            writer.Write(LoopStart);
            writer.Write(LoopLength);
        }
    }
}
