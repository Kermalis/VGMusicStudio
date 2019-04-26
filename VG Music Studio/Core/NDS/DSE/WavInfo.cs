using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal interface IWavInfo
    {
        byte RootKey { get; }
        sbyte Transpose { get; }
        SampleFormat SampleFormat { get; }
        bool Loop { get; }
        uint SampleRate { get; }
        uint SampleOffset { get; }
        uint LoopStart { get; }
        uint LoopEnd { get; }
        byte Attack { get; }
        byte Decay { get; }
        byte Sustain { get; }
        byte Release { get; }
    }
    internal class WavInfo_V402 : IWavInfo
    {
        public byte Unknown1 { get; set; }
        public byte Id { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown2 { get; set; }
        public byte RootKey { get; set; }
        public sbyte Transpose { get; set; }
        public byte Volume { get; set; }
        public sbyte Panpot { get; set; }
        public SampleFormat SampleFormat { get; set; }
        [BinaryArrayFixedLength(7)]
        public byte[] Unknown3 { get; set; }
        public bool Loop { get; set; }
        public uint SampleRate { get; set; }
        public uint SampleOffset { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopEnd { get; set; }
        [BinaryArrayFixedLength(16)]
        public byte[] Unknown4 { get; set; }
        public byte EnvOn { get; set; }
        public byte EnvMult { get; set; }
        [BinaryArrayFixedLength(6)]
        public byte[] Unknown5 { get; set; }
        public byte AttackVolume { get; set; }
        public byte Attack { get; set; }
        public byte Decay { get; set; }
        public byte Sustain { get; set; }
        public byte Hold { get; set; }
        public byte Decay2 { get; set; }
        public byte Release { get; set; }
        public byte Unknown6 { get; set; }
    }
    internal class WavInfo_V415 : IWavInfo
    {
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown1 { get; set; }
        public ushort Id { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown2 { get; set; }
        public byte RootKey { get; set; }
        public sbyte Transpose { get; set; }
        public byte Volume { get; set; }
        public sbyte Panpot { get; set; }
        [BinaryArrayFixedLength(6)]
        public byte[] Unknown3 { get; set; }
        public ushort Version { get; set; }
        public SampleFormat SampleFormat { get; set; }
        public byte Unknown4 { get; set; }
        public bool Loop { get; set; }
        public byte Unknown5 { get; set; }
        public byte SamplesPer32Bits { get; set; }
        public byte Unknown6 { get; set; }
        public byte BitDepth { get; set; }
        [BinaryArrayFixedLength(6)]
        public byte[] Unknown7 { get; set; }
        public uint SampleRate { get; set; }
        public uint SampleOffset { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopEnd { get; set; }
        public byte EnvOn { get; set; }
        public byte EnvMult { get; set; }
        [BinaryArrayFixedLength(6)]
        public byte[] Unknown8 { get; set; }
        public byte AttackVolume { get; set; }
        public byte Attack { get; set; }
        public byte Decay { get; set; }
        public byte Sustain { get; set; }
        public byte Hold { get; set; }
        public byte Decay2 { get; set; }
        public byte Release { get; set; }
        public byte Unknown9 { get; set; }
    }
}
