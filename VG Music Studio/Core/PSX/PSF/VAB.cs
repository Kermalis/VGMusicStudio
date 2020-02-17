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

        public class Tone
        {
            public byte Priority { get; set; }
            public byte Mode { get; set; }
            public byte Volume { get; set; } // Out of 127
            public byte Panpot { get; set; } // 0x40 is middle
            public byte BaseKey { get; set; }
            public byte PitchTune { get; set; }
            public byte LowKey { get; set; }
            public byte HighKey { get; set; }
            public byte VibratoWidth { get; set; }
            public byte VibratoTime { get; set; }
            public byte PortamentoWidth { get; set; }
            public byte PortamentoTime { get; set; }
            public byte PitchBendMin { get; set; }
            public byte PitchBendMax { get; set; }
            [BinaryArrayFixedLength(2)]
            public byte[] Unknown { get; set; }
            public byte Attack { get; set; }
            public byte Decay { get; set; }
            public byte Sustain { get; set; }
            public byte Release { get; set; }
            public ushort ParentProgram { get; set; }
            public ushort SampleId { get; set; }
            [BinaryArrayFixedLength(8)]
            public byte[] Unknown2 { get; set; }
        }

        public class Instrument
        {
            [BinaryArrayFixedLength(0x10)]
            public Tone[] Tones { get; set; }
        }

        private const long _instrumentsOffset = 0x130000; // Crash Bandicoot 2

        public ushort NumPrograms { get; }
        public ushort NumTones { get; }
        public ushort NumVAGs { get; }
        public Program[] Programs { get; }
        public Instrument[] Instruments { get; }
        public (long Offset, long Size)[] VAGs { get; }

        public VAB(EndianBinaryReader reader)
        {
            // Header
            reader.BaseStream.Position = _instrumentsOffset;
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
            for (int i = 0; i < 0x80; i++)
            {
                Programs[i] = reader.ReadObject<Program>();
            }

            // Instruments
            Instruments = new Instrument[NumPrograms];
            for (int i = 0; i < NumPrograms; i++)
            {
                Instruments[i] = reader.ReadObject<Instrument>();
            }

            // VAG Pointers
            VAGs = new (long Offset, long Size)[0xFF];
            long offset = reader.ReadUInt16() * 8;
            for (int i = 0; i < 0xFF; i++)
            {
                long size = reader.ReadUInt16() * 8;
                VAGs[i] = (offset, size);
                offset += size;
            }
        }
    }
}
