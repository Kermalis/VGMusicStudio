using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class SampleHeader
    {
        /// <summary>0x40000000 if True</summary>
        public int DoesLoop { get; set; }
        /// <summary>Right shift 10 for value</summary>
        public int SampleRate { get; set; }
        public int LoopOffset { get; set; }
        public int Length { get; set; }
    }
    internal class VoiceEntry
    {
        public byte MinKey { get; set; }
        public byte MaxKey { get; set; }
        public byte Sample { get; set; }
        /// <summary>0x80 if True</summary>
        public byte IsFixedFrequency { get; set; }
        [BinaryArrayFixedLength(4)]
        public byte[] Unknown { get; set; }
    }

    internal struct ChannelVolume
    {
        public float LeftVol, RightVol;
    }
    internal class ADSR // TODO
    {
        public byte A, D, S, R;
    }
}
