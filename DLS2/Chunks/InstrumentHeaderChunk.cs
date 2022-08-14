using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.DLS2
{
    // Instrument Header Chunk - Page 45 of spec
    public sealed class InstrumentHeaderChunk : DLSChunk
    {
        public uint NumRegions { get; set; }
        private MIDILocale _locale;
        public MIDILocale Locale
        {
            get => _locale;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _locale = value;
            }
        }

        public InstrumentHeaderChunk() : base("insh") { }
        internal InstrumentHeaderChunk(EndianBinaryReader reader) : base("insh", reader)
        {
            long endOffset = GetEndOffset(reader);
            NumRegions = reader.ReadUInt32();
            _locale = new MIDILocale(reader);
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 4 // NumRegions
                + 8; // Locale
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(NumRegions);
            _locale.Write(writer);
        }
    }
}
