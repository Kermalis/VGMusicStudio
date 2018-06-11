using System;
using System.IO;

namespace GBAMusicStudio.Core
{
    public class ROMReader
    {
        BinaryReader Reader;
        internal void InitReader() => Reader = new BinaryReader(new MemoryStream(ROM.Instance.ROMFile));

        object Parse(uint offset, uint amt, bool signed = false, bool array = false)
        {
            if (ROM.IsValidRomOffset(offset))
                SetOffset(offset);
            if (array)
                return Reader.ReadBytes((int)amt);
            switch (amt)
            {
                case 1: return signed ? (object)Reader.ReadSByte() : (object)Reader.ReadByte();
                case 2: return signed ? (object)Reader.ReadInt16() : (object)Reader.ReadUInt16();
                case 4: return signed ? (object)Reader.ReadInt32() : (object)Reader.ReadUInt32();
            }
            return null;
        }

        public byte PeekByte(uint offset = 0xFFFFFFFF)
        {
            var org = Reader.BaseStream.Position;
            byte ret = (byte)Parse(offset, 1);
            Reader.BaseStream.Position = org;
            return ret;
        }

        public byte[] ReadBytes(uint amt, uint offset = 0xFFFFFFFF) => (byte[])Parse(offset, amt, false, true);

        public sbyte ReadSByte(uint offset = 0xFFFFFFFF) => (sbyte)Parse(offset, 1, true);
        public byte ReadByte(uint offset = 0xFFFFFFFF) => (byte)Parse(offset, 1);
        public ushort ReadUInt16(uint offset = 0xFFFFFFFF) => (ushort)Parse(offset, 2);
        public int ReadInt32(uint offset = 0xFFFFFFFF) => (int)Parse(offset, 4, true);
        public uint ReadUInt32(uint offset = 0xFFFFFFFF) => (uint)Parse(offset, 4);
        public uint ReadPointer(uint offset = 0xFFFFFFFF) => ReadUInt32(offset) - ROM.Pak;

        public void SetOffset(uint offset)
        {
            if (offset > ROM.Capacity)
                offset -= ROM.Pak;
            if (!ROM.IsValidRomOffset(offset))
                throw new ArgumentOutOfRangeException("\"offset\" was invalid.");
            Reader.BaseStream.Position = offset;
        }
        public uint Position { get { return (uint)Reader.BaseStream.Position; } }
    }
}
