using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class SWAR
    {
        public class SWAV : IBinarySerializable
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
                Format = er.ReadEnum<SWAVFormat>();
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

        public FileHeader FileHeader; // "SWAR"
        public string BlockType; // "DATA"
        public int BlockSize;
        public byte[] Padding;
        public int NumWaves;
        public int[] WaveOffsets;

        public SWAV[] Waves;

        public SWAR(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var er = new EndianBinaryReader(stream);
                FileHeader = er.ReadObject<FileHeader>();
                BlockType = er.ReadString(4, false);
                BlockSize = er.ReadInt32();
                Padding = er.ReadBytes(32);
                NumWaves = er.ReadInt32();
                WaveOffsets = er.ReadInt32s(NumWaves);

                Waves = new SWAV[NumWaves];
                for (int i = 0; i < NumWaves; i++)
                {
                    Waves[i] = er.ReadObject<SWAV>(WaveOffsets[i]);
                }
            }
        }
    }
}
