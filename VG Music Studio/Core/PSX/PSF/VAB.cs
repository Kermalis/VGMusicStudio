using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class VAB
    {
        public class Program
        {
            public byte NumTones { get; set; }
            public byte Volume { get; set; } // Out of 127
            public byte Priority { get; set; }
            public byte Mode { get; set; }
            public byte Panpot { get; set; } // 0x40 is middle
            [BinaryArrayFixedLength(11)]
            public byte[] Unknown { get; set; }
        }

        private const long InstrumentsOffset = 0x130000; // Crash Bandicoot 2

        public ushort NumPrograms { get; }
        public ushort NumTones { get; }
        public ushort NumVAGs { get; }
        public Program[] Programs { get; }
        public byte[][] Tones { get; }
        public (long Offset, long Size)[] VAGs { get; }

        public VAB(EndianBinaryReader reader)
        {
            // Header
            reader.BaseStream.Position = InstrumentsOffset;
            reader.Endianness = Endianness.LittleEndian;
            reader.ReadString(4); // "pBAV"
            reader.ReadUInt32(); // Version
            reader.ReadUInt32(); // VAB ID
            reader.ReadUInt32(); // Size
            reader.ReadBytes(2); // Unknown
            NumPrograms = reader.ReadUInt16();
            NumTones = reader.ReadUInt16();
            NumVAGs = reader.ReadUInt16();
            reader.ReadByte(); // MasterVolume (out of 100?)
            reader.ReadByte(); // MasterPanpot (0x40 is middle)
            reader.ReadByte(); // BankAttributes1
            reader.ReadByte(); // BankAttributes2
            reader.ReadBytes(4); // Padding

            // Programs
            Programs = new Program[0x80];
            for (int i = 0; i < Programs.Length; i++)
            {
                Programs[i] = reader.ReadObject<Program>();
            }

            // Tones (Unknown structure)
            Tones = new byte[0x10 * NumPrograms][];
            for (int i = 0; i < Tones.Length; i++)
            {
                Tones[i] = reader.ReadBytes(0x20);
            }

            // VAG Pointers
            VAGs = new (long Offset, long Size)[0xFF];
            long offset = reader.ReadUInt16() * 8;
            for (int i = 0; i < VAGs.Length; i++)
            {
                long size = reader.ReadUInt16() * 8;
                VAGs[i] = (offset, size);
                offset += size;
            }
        }
    }
}
