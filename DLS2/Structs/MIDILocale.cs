using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.DLS2
{
    // MIDILOCALE - Page 45 of spec
    public sealed class MIDILocale
    {
        public uint Bank_Raw { get; set; }
        public uint Instrument_Raw { get; set; }

        public byte CC32
        {
            get => (byte)(Bank_Raw & 0x7F);
            set
            {
                if (value > 0x7F)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                Bank_Raw &= unchecked((uint)~0x7F);
                Bank_Raw |= value;
            }
        }
        public byte CC0
        {
            get => (byte)((Bank_Raw >> 7) & 0x7F);
            set
            {
                if (value > 0x7F)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                Bank_Raw &= unchecked((uint)~(0x7F << 7));
                Bank_Raw |= (uint)(value << 7);
            }
        }
        public bool IsDrum
        {
            get => (Bank_Raw >> 31) != 0;
            set
            {
                if (value)
                {
                    Bank_Raw |= 1u << 31;
                }
                else
                {
                    Bank_Raw &= ~(1 << 31);
                }
            }
        }
        public byte Instrument
        {
            get => (byte)(Instrument_Raw & 0x7F);
            set
            {
                if (value > 0x7F)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                Instrument_Raw &= unchecked((uint)~0x7F);
                Instrument_Raw |= value;
            }
        }

        public MIDILocale() { }
        public MIDILocale(byte cc32, byte cc0, bool isDrum, byte instrument)
        {
            CC32 = cc32;
            CC0 = cc0;
            IsDrum = isDrum;
            Instrument = instrument;
        }
        internal MIDILocale(EndianBinaryReader reader)
        {
            Bank_Raw = reader.ReadUInt32();
            Instrument_Raw = reader.ReadUInt32();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(Bank_Raw);
            writer.Write(Instrument_Raw);
        }
    }
}
