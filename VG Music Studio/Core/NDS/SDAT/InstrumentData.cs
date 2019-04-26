using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class InstrumentData
    {
        public class DataParam
        {
            [BinaryArrayFixedLength(2)]
            public ushort[] Info { get; set; }
            public byte BaseKey { get; set; }
            public byte Attack { get; set; }
            public byte Decay { get; set; }
            public byte Sustain { get; set; }
            public byte Release { get; set; }
            public byte Pan { get; set; }
        }

        public InstrumentType Type { get; set; }
        public byte Padding { get; set; }
        public DataParam Param { get; set; }
    }
}
