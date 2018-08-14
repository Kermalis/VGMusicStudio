using System;
using System.IO;

namespace GBAMusicStudio.Core
{
    internal class ROMReader
    {
        object _lock = new object();
        BinaryReader Reader;
        protected internal void InitReader(byte[] binary = null) => Reader = new BinaryReader(new MemoryStream(binary ?? ROM.Instance.ROMFile));
        internal uint Position => (uint)Reader.BaseStream.Position;

        object Parse(uint offset, uint amt, bool signed = false, bool array = false)
        {
            lock (_lock)
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
        }

        internal byte PeekByte(uint offset = 0xFFFFFFFF)
        {
            var org = Reader.BaseStream.Position;
            byte ret = (byte)Parse(offset, 1);
            Reader.BaseStream.Position = org;
            return ret;
        }

        internal byte[] ReadBytes(uint amt, uint offset = 0xFFFFFFFF) => (byte[])Parse(offset, amt, false, true);

        internal sbyte ReadSByte(uint offset = 0xFFFFFFFF) => (sbyte)Parse(offset, 1, true);
        internal byte ReadByte(uint offset = 0xFFFFFFFF) => (byte)Parse(offset, 1);
        internal short ReadInt16(uint offset = 0xFFFFFFFF) => (short)Parse(offset, 2, true);
        internal ushort ReadUInt16(uint offset = 0xFFFFFFFF) => (ushort)Parse(offset, 2);
        internal int ReadInt32(uint offset = 0xFFFFFFFF) => (int)Parse(offset, 4, true);
        internal uint ReadUInt32(uint offset = 0xFFFFFFFF) => (uint)Parse(offset, 4);
        internal uint ReadPointer(uint offset = 0xFFFFFFFF) => ReadUInt32(offset) - ROM.Pak;

        internal void SetOffset(uint offset)
        {
            if (offset > ROM.Capacity)
                offset -= ROM.Pak;
            if (!ROM.IsValidRomOffset(offset))
                throw new ArgumentOutOfRangeException("\"offset\" was invalid.");
            Reader.BaseStream.Position = offset;
        }
    }
}
