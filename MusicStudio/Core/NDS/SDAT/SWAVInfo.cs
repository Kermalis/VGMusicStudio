using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.MusicStudio.Core.NDS.SDAT
{
    class SWAVInfo : IBinarySerializable
    {
        public SWAVFormat Format;
        public bool DoesLoop;
        public ushort SampleRate;
        public ushort Timer; // (NDSUtils.ARM7_CLOCK / SampleRate)
        public ushort LoopOffset;
        public int Length;

        public byte[] Samples;

        public void Read(EndianBinaryReader er)
        {
            Format = (SWAVFormat)er.ReadByte();
            DoesLoop = er.ReadBoolean();
            SampleRate = er.ReadUInt16();
            Timer = er.ReadUInt16();
            LoopOffset = er.ReadUInt16();
            Length = er.ReadInt32();

            Samples = er.ReadBytes((LoopOffset * 4) + (Length * 4));
        }
        public void Write(EndianBinaryWriter ew)
        {
            throw new NotImplementedException();
        }
    }
}
