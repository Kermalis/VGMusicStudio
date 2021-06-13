using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class SongEntry
    {
        public int HeaderOffset { get; set; }
        public short Player { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown { get; set; }
    }
    internal class SongHeader
    {
        public byte NumTracks { get; set; }
        public byte NumBlocks { get; set; }
        public byte Priority { get; set; }
        public byte Reverb { get; set; }
        public int VoiceTableOffset { get; set; }
        [BinaryArrayVariableLength(nameof(NumTracks))]
        public int[] TrackOffsets { get; set; }
    }
    internal class VoiceEntry
    {
        public byte Type { get; set; } // 0
        public byte RootKey { get; set; } // 1
        public byte Unknown { get; set; } // 2
        public byte Pan { get; set; } // 3
        /// <summary>SquarePattern for Square1/Square2, NoisePattern for Noise, Address for PCM8/PCM4/KeySplit/Drum</summary>
        public int Int4 { get; set; } // 4
        /// <summary>ADSR for PCM8/Square1/Square2/PCM4/Noise, KeysAddress for KeySplit</summary>
        public ADSR ADSR { get; set; } // 8
        [BinaryIgnore]
        public int Int8 => (ADSR.R << 24) | (ADSR.S << 16) | (ADSR.D << 8) | (ADSR.A);
    }
    internal struct ADSR // Used as a struct in GBChannel
    {
        public byte A { get; set; }
        public byte D { get; set; }
        public byte S { get; set; }
        public byte R { get; set; }
    }
    internal class GoldenSunPSG
    {
        /// <summary>Always 0x80</summary>
        public byte Unknown { get; set; }
        public GoldenSunPSGType Type { get; set; }
        public byte InitialCycle { get; set; }
        public byte CycleSpeed { get; set; }
        public byte CycleAmplitude { get; set; }
        public byte MinimumCycle { get; set; }
    }
    internal class SampleHeader
    {
        /// <summary>0x40000000 if True</summary>
        public int DoesLoop { get; set; }
        /// <summary>Right shift 10 for value</summary>
        public int SampleRate { get; set; }
        public int LoopOffset { get; set; }
        public int Length { get; set; }
    }

    internal struct ChannelVolume
    {
        public float LeftVol, RightVol;
    }
    internal struct Note
    {
        public byte Key, OriginalKey;
        public byte Velocity;
        /// <summary>-1 if forever</summary>
        public int Duration;
    }
}
