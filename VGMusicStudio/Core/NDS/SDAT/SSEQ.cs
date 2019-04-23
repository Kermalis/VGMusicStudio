using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    class SSEQ
    {
        public FileHeader FileHeader; // "SSEQ"
        public string BlockType; // "DATA"
        public int BlockSize;
        public int DataOffset;

        public byte[] Data;

        public SSEQ(byte[] bytes)
        {
            using (var er = new EndianBinaryReader(new MemoryStream(bytes)))
            {
                FileHeader = er.ReadObject<FileHeader>();
                BlockType = er.ReadString(4);
                BlockSize = er.ReadInt32();
                DataOffset = er.ReadInt32();

                Data = er.ReadBytes(FileHeader.FileSize - DataOffset, DataOffset);
            }
        }
    }
}
