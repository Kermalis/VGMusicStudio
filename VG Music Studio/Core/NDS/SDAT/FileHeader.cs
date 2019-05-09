using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class FileHeader : IBinarySerializable
    {
        public string FileType;
        public ushort Endianness;
        public ushort Version;
        public int FileSize;
        public ushort HeaderSize; // 16
        public ushort NumBlocks;

        public void Read(EndianBinaryReader er)
        {
            FileType = er.ReadString(4);
            er.Endianness = EndianBinaryIO.Endianness.BigEndian;
            Endianness = er.ReadUInt16();
            er.Endianness = Endianness == 0xFFFE ? EndianBinaryIO.Endianness.LittleEndian : EndianBinaryIO.Endianness.BigEndian;
            Version = er.ReadUInt16();
            FileSize = er.ReadInt32();
            HeaderSize = er.ReadUInt16();
            NumBlocks = er.ReadUInt16();
        }
        public void Write(EndianBinaryWriter ew)
        {
            throw new NotImplementedException();
        }
    }
}
