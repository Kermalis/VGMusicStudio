using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    class SMDL
    {
        public class Header
        {
            [BinaryStringFixedLength(4)]
            public string Type { get; set; }
            [BinaryArrayFixedLength(4)]
            public byte[] Unknown1 { get; set; }
            public uint Length { get; set; }
            public ushort Version { get; set; }
            [BinaryArrayFixedLength(10)]
            public byte[] Unknown2 { get; set; }
            public ushort Year { get; set; }
            public byte Month { get; set; }
            public byte Day { get; set; }
            public byte Hour { get; set; }
            public byte Minute { get; set; }
            public byte Second { get; set; }
            public byte Centisecond { get; set; }
            [BinaryStringFixedLength(16)]
            public string Label { get; set; }
            [BinaryArrayFixedLength(16)]
            public byte[] Unknown3 { get; set; }
        }

        public interface ISongChunk
        {
            byte NumTracks { get; }
        }
        public class SongChunk_V402 : ISongChunk
        {
            [BinaryStringFixedLength(4)]
            public string Type { get; set; }
            [BinaryArrayFixedLength(16)]
            public byte[] Unknown1 { get; set; }
            public byte NumTracks { get; set; }
            public byte NumChannels { get; set; }
            [BinaryArrayFixedLength(4)]
            public byte[] Unknown2 { get; set; }
            public sbyte MasterVolume { get; set; }
            public sbyte MasterPanpot { get; set; }
            [BinaryArrayFixedLength(4)]
            public byte[] Unknown3 { get; set; }
        }
        public class SongChunk_V415 : ISongChunk
        {
            [BinaryStringFixedLength(4)]
            public string Type { get; set; }
            [BinaryArrayFixedLength(18)]
            public byte[] Unknown1 { get; set; }
            public byte NumTracks { get; set; }
            public byte NumChannels { get; set; }
            [BinaryArrayFixedLength(40)]
            public byte[] Unknown2 { get; set; }
        }
    }
}
