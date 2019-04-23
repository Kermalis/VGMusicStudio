using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    class LFOInfo
    {
        [BinaryArrayFixedLength(16)]
        public byte[] Unknown { get; set; }
    }

    interface ISplitEntry
    {
        byte LowKey { get; }
        byte HighKey { get; }
        int SampleId { get; }
        byte SampleRootKey { get; }
        sbyte SampleTranspose { get; }
    }
    class SplitEntry_V402 : ISplitEntry
    {
        public ushort Id { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown1 { get; set; }
        public byte LowKey { get; set; }
        public byte HighKey { get; set; }
        public byte LowKey2 { get; set; }
        public byte HighKey2 { get; set; }
        public byte LowVelocity { get; set; }
        public byte HighVelocity { get; set; }
        public byte LowVelocity2 { get; set; }
        public byte HighVelocity2 { get; set; }
        [BinaryArrayFixedLength(5)]
        public byte[] Unknown2 { get; set; }
        public byte SampleId { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown3 { get; set; }
        public byte SampleRootKey { get; set; }
        public sbyte SampleTranspose { get; set; }
        public byte SampleVolume { get; set; }
        public sbyte SamplePanpot { get; set; }
        public byte KeyGroupId { get; set; }
        [BinaryArrayFixedLength(15)]
        public byte[] Unknown4 { get; set; }
        public byte AttackVolume { get; set; }
        public byte Attack { get; set; }
        public byte Decay { get; set; }
        public byte Sustain { get; set; }
        public byte Hold { get; set; }
        public byte Decay2 { get; set; }
        public byte Release { get; set; }
        public byte Unknown5 { get; set; }

        int ISplitEntry.SampleId => SampleId;
    }
    class SplitEntry_V415 : ISplitEntry
    {
        public ushort Id { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown1 { get; set; }
        public byte LowKey { get; set; }
        public byte HighKey { get; set; }
        public byte LowKey2 { get; set; }
        public byte HighKey2 { get; set; }
        public byte LowVelocity { get; set; }
        public byte HighVelocity { get; set; }
        public byte LowVelocity2 { get; set; }
        public byte HighVelocity2 { get; set; }
        [BinaryArrayFixedLength(6)]
        public byte[] Unknown2 { get; set; }
        public ushort SampleId { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown3 { get; set; }
        public byte SampleRootKey { get; set; }
        public sbyte SampleTranspose { get; set; }
        public byte SampleVolume { get; set; }
        public sbyte SamplePanpot { get; set; }
        public byte KeyGroupId { get; set; }
        [BinaryArrayFixedLength(13)]
        public byte[] Unknown4 { get; set; }
        public byte AttackVolume { get; set; }
        public byte Attack { get; set; }
        public byte Decay { get; set; }
        public byte Sustain { get; set; }
        public byte Hold { get; set; }
        public byte Decay2 { get; set; }
        public byte Release { get; set; }
        public byte Unknown5 { get; set; }

        int ISplitEntry.SampleId => SampleId;
    }

    interface IProgramInfo
    {
        ISplitEntry[] SplitEntries { get; }
    }
    class ProgramInfo_V402 : IProgramInfo
    {
        public byte Id { get; set; }
        public byte NumSplits { get; set; }
        [BinaryArrayFixedLength(2)]
        public byte[] Unknown1 { get; set; }
        public byte Volume { get; set; }
        public byte Panpot { get; set; }
        [BinaryArrayFixedLength(5)]
        public byte[] Unknown2 { get; set; }
        public byte NumLFOs { get; set; }
        [BinaryArrayFixedLength(4)]
        public byte[] Unknown3 { get; set; }
        [BinaryArrayFixedLength(16)]
        public SWDL.KeyGroup[] KeyGroups { get; set; }
        [BinaryArrayVariableLength(nameof(NumLFOs))]
        public LFOInfo LFOInfos { get; set; }
        [BinaryArrayVariableLength(nameof(NumSplits))]
        public SplitEntry_V402[] SplitEntries { get; set; }

        [BinaryIgnore]
        ISplitEntry[] IProgramInfo.SplitEntries => SplitEntries;
    }
    class ProgramInfo_V415 : IProgramInfo
    {
        public ushort Id { get; set; }
        public ushort NumSplits { get; set; }
        public byte Volume { get; set; }
        public byte Panpot { get; set; }
        [BinaryArrayFixedLength(5)]
        public byte[] Unknown1 { get; set; }
        public byte NumLFOs { get; set; }
        [BinaryArrayFixedLength(4)]
        public byte[] Unknown2 { get; set; }
        [BinaryArrayVariableLength(nameof(NumLFOs))]
        public LFOInfo[] LFOInfos { get; set; }
        [BinaryArrayFixedLength(16)]
        public byte[] Unknown3 { get; set; }
        [BinaryArrayVariableLength(nameof(NumSplits))]
        public SplitEntry_V415[] SplitEntries { get; set; }

        [BinaryIgnore]
        ISplitEntry[] IProgramInfo.SplitEntries => SplitEntries;
    }
}
