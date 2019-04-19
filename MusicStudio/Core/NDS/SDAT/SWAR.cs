using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.MusicStudio.Core.NDS.SDAT
{
    class SWAR
    {
        public FileHeader FileHeader; // "SWAR"
        public string BlockType; // "DATA"
        public int BlockSize;
        public byte[] Padding;
        public int NumWaves;
        public int[] WaveOffsets;

        public SWAVInfo[] Waves;

        public SWAR(byte[] bytes)
        {
            using (var er = new EndianBinaryReader(new MemoryStream(bytes)))
            {
                FileHeader = er.ReadObject<FileHeader>();
                BlockType = er.ReadString(4);
                BlockSize = er.ReadInt32();
                Padding = er.ReadBytes(32);
                NumWaves = er.ReadInt32();
                WaveOffsets = er.ReadInt32s(NumWaves);

                Waves = new SWAVInfo[NumWaves];
                for (int i = 0; i < NumWaves; i++)
                {
                    Waves[i] = er.ReadObject<SWAVInfo>(WaveOffsets[i]);
                }
            }
        }
    }
}
