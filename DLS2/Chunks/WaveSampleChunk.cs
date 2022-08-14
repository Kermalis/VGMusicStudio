using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.DLS2
{
    public sealed class WaveSampleChunk : DLSChunk
    {
        public ushort UnityNote { get; set; }
        public short FineTune { get; set; }
        public int Gain { get; set; }
        public WaveSampleOptions Options { get; set; }

        public WaveSampleLoop Loop { get; set; } // Combining "SampleLoops" and the loop list

        public WaveSampleChunk() : base("wsmp")
        {
            UnityNote = 60;
            Loop = null;
        }
        internal WaveSampleChunk(EndianBinaryReader reader) : base("wsmp", reader)
        {
            long endOffset = GetEndOffset(reader);
            uint byteSize = reader.ReadUInt32();
            if (byteSize != 20)
            {
                throw new InvalidDataException();
            }
            UnityNote = reader.ReadUInt16();
            FineTune = reader.ReadInt16();
            Gain = reader.ReadInt32();
            Options = reader.ReadEnum<WaveSampleOptions>();
            if (reader.ReadUInt32() == 1)
            {
                Loop = new WaveSampleLoop(reader);
            }
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 4 // byteSize
                + 2 // UnityNote
                + 2 // FineTune
                + 4 // Gain
                + 4 // Options
                + 4 // DoesLoop
                + (Loop is null ? 0u : 16u); // Loop
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(20u);
            writer.Write(UnityNote);
            writer.Write(FineTune);
            writer.Write(Gain);
            writer.Write(Options);
            if (Loop is null)
            {
                writer.Write(0u);
            }
            else
            {
                writer.Write(1u);
                Loop.Write(writer);
            }
        }
    }
}
