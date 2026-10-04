using Kermalis.EndianBinaryIO;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.DLS2
{
    public abstract class DLSChunk
    {
        /// <summary>Length 4</summary>
        public string ChunkName { get; }
        /// <summary>Size in bytes</summary>
        protected internal uint Size { get; protected set; }

        protected DLSChunk(string name)
        {
            ChunkName = name;
        }
        protected DLSChunk(string name, EndianBinaryReader reader)
        {
            ChunkName = name;
            Size = reader.ReadUInt32();
        }

        protected long GetEndOffset(EndianBinaryReader reader)
        {
            return reader.BaseStream.Position + Size;
        }
        protected void EatRemainingBytes(EndianBinaryReader reader, long endOffset)
        {
            if (reader.BaseStream.Position > endOffset)
            {
                throw new InvalidDataException();
            }
            reader.BaseStream.Position = endOffset;
        }

        internal abstract void UpdateSize();

        internal virtual void Write(EndianBinaryWriter writer)
        {
            UpdateSize();
            writer.Write(ChunkName, 4);
            writer.Write(Size);
        }

        internal static List<DLSChunk> GetAllChunks(EndianBinaryReader reader, long endOffset)
        {
            var chunks = new List<DLSChunk>();
            while (reader.BaseStream.Position < endOffset)
            {
                chunks.Add(SwitchNextChunk(reader));
            }
            if (reader.BaseStream.Position > endOffset)
            {
                throw new InvalidDataException();
            }
            return chunks;
        }
        private static DLSChunk SwitchNextChunk(EndianBinaryReader reader)
        {
            string str = reader.ReadString(4, false);
            switch (str)
            {
                case "art1": return new Level1ArticulatorChunk(reader);
                case "art2": return new Level2ArticulatorChunk(reader);
                case "colh": return new CollectionHeaderChunk(reader);
                case "data": return new DataChunk(reader);
                case "dlid": return new DLSIDChunk(reader);
                case "fmt ": return new FormatChunk(reader);
                case "insh": return new InstrumentHeaderChunk(reader);
                case "LIST": return new ListChunk(reader);
                case "ptbl": return new PoolTableChunk(reader);
                case "rgnh": return new RegionHeaderChunk(reader);
                case "wlnk": return new WaveLinkChunk(reader);
                case "wsmp": return new WaveSampleChunk(reader);
                // InfoSubChunks
                case "IARL":
                case "IART":
                case "ICMS":
                case "ICMD":
                case "ICOP":
                case "ICRD":
                case "IENG":
                case "IGNR":
                case "IKEY":
                case "IMED":
                case "INAM":
                case "IPRD":
                case "ISBJ":
                case "ISFT":
                case "ISRC":
                case "ISRF":
                case "ITCH": return new InfoSubChunk(str, reader);
                default: return new UnsupportedChunk(str, reader);
            }
        }
    }
}
