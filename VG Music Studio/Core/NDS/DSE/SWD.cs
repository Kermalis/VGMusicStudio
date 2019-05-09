using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class SWD
    {
        public interface IHeader
        {

        }
        private class Header_V402 : IHeader
        {
            [BinaryArrayFixedLength(10)]
            public byte[] Unknown1 { get; set; }
            public ushort Year { get; set; }
            public byte Month { get; set; }
            public byte Day { get; set; }
            public byte Hour { get; set; }
            public byte Minute { get; set; }
            public byte Second { get; set; }
            public byte Centisecond { get; set; }
            [BinaryStringFixedLength(16)]
            public string Label { get; set; }
            [BinaryArrayFixedLength(22)]
            public byte[] Unknown2 { get; set; }
            public byte NumWAVISlots { get; set; }
            public byte NumPRGISlots { get; set; }
            public byte NumKeyGroups { get; set; }
            [BinaryArrayFixedLength(7)]
            public byte[] Padding { get; set; }
        }
        private class Header_V415 : IHeader
        {
            [BinaryArrayFixedLength(10)]
            public byte[] Unknown1 { get; set; }
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
            public byte[] Unknown2 { get; set; }
            public uint PCMDLength { get; set; }
            [BinaryArrayFixedLength(2)]
            public byte[] Unknown3 { get; set; }
            public ushort NumWAVISlots { get; set; }
            public ushort NumPRGISlots { get; set; }
            [BinaryArrayFixedLength(2)]
            public byte[] Unknown4 { get; set; }
            public uint WAVILength { get; set; }
        }

        internal interface ISplitEntry
        {
            byte LowKey { get; }
            byte HighKey { get; }
            int SampleId { get; }
            byte SampleRootKey { get; }
            sbyte SampleTranspose { get; }
        }
        internal class SplitEntry_V402 : ISplitEntry
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
        internal class SplitEntry_V415 : ISplitEntry
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

        internal interface IProgramInfo
        {
            ISplitEntry[] SplitEntries { get; }
        }
        internal class ProgramInfo_V402 : IProgramInfo
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
            public SWD.KeyGroup[] KeyGroups { get; set; }
            [BinaryArrayVariableLength(nameof(NumLFOs))]
            public LFOInfo LFOInfos { get; set; }
            [BinaryArrayVariableLength(nameof(NumSplits))]
            public SplitEntry_V402[] SplitEntries { get; set; }

            [BinaryIgnore]
            ISplitEntry[] IProgramInfo.SplitEntries => SplitEntries;
        }
        internal class ProgramInfo_V415 : IProgramInfo
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

        public class SampleBlock
        {
            public IWavInfo WavInfo;
            public byte[] Data;
        }
        public class ProgramBank
        {
            public IProgramInfo[] ProgramInfos;
            public KeyGroup[] KeyGroups;
        }
        public class KeyGroup
        {
            public ushort Id { get; set; }
            public byte Poly { get; set; }
            public byte Priority { get; set; }
            public byte Low { get; set; }
            public byte High { get; set; }
            [BinaryArrayFixedLength(2)]
            public byte[] Unknown { get; set; }
        }
        internal class LFOInfo
        {
            [BinaryArrayFixedLength(16)]
            public byte[] Unknown { get; set; }
        }

        public string Type; // "swdb" or "swdl"
        public byte[] Unknown;
        public uint Length;
        public ushort Version;
        public IHeader Header;

        public ProgramBank Programs;
        public SampleBlock[] Samples;

        public SWD(string path)
        {
            using (var reader = new EndianBinaryReader(new MemoryStream(File.ReadAllBytes(path))))
            {
                Type = reader.ReadString(4);
                Unknown = reader.ReadBytes(4);
                Length = reader.ReadUInt32();
                Version = reader.ReadUInt16();
                switch (Version)
                {
                    case 0x402:
                    {
                        Header_V402 header = reader.ReadObject<Header_V402>();
                        Header = header;
                        Programs = ReadPrograms<ProgramInfo_V402>(reader, header.NumPRGISlots);
                        Samples = ReadSamples<WavInfo_V402>(reader, header.NumWAVISlots);
                        break;
                    }
                    case 0x415:
                    {
                        Header_V415 header = reader.ReadObject<Header_V415>();
                        Header = header;
                        Programs = ReadPrograms<ProgramInfo_V415>(reader, header.NumPRGISlots);
                        if (header.PCMDLength != 0 && (header.PCMDLength & 0xFFFF0000) != 0xAAAA0000)
                        {
                            Samples = ReadSamples<WavInfo_V415>(reader, header.NumWAVISlots);
                        }
                        break;
                    }
                    default: throw new InvalidDataException();
                }
            }
        }

        private static long FindChunk(EndianBinaryReader reader, string chunk)
        {
            long pos = -1;
            long oldPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string str = reader.ReadString(4);
                if (str == chunk)
                {
                    pos = reader.BaseStream.Position - 4;
                    break;
                }
                switch (str)
                {
                    case "swdb":
                    case "swdl":
                    {
                        reader.BaseStream.Position += 0x4C;
                        break;
                    }
                    default:
                    {
                        reader.BaseStream.Position += 0x8;
                        uint length = reader.ReadUInt32();
                        reader.BaseStream.Position += length;
                        // Align 4
                        while (reader.BaseStream.Position % 4 != 0)
                        {
                            reader.BaseStream.Position++;
                        }
                        break;
                    }
                }
            }
            reader.BaseStream.Position = oldPosition;
            return pos;
        }

        private static SampleBlock[] ReadSamples<T>(EndianBinaryReader reader, int numWAVISlots) where T : IWavInfo
        {
            long waviChunkOffset = FindChunk(reader, "wavi");
            long pcmdChunkOffset = FindChunk(reader, "pcmd");
            if (waviChunkOffset == -1 || pcmdChunkOffset == -1)
            {
                throw new InvalidDataException();
            }
            else
            {
                waviChunkOffset += 0x10;
                pcmdChunkOffset += 0x10;
                var samples = new SampleBlock[numWAVISlots];
                for (int i = 0; i < numWAVISlots; i++)
                {
                    ushort offset = reader.ReadUInt16(waviChunkOffset + (2 * i));
                    if (offset != 0)
                    {
                        T wavInfo = reader.ReadObject<T>(offset + waviChunkOffset);
                        samples[i] = new SampleBlock
                        {
                            WavInfo = wavInfo,
                            Data = reader.ReadBytes((int)((wavInfo.LoopStart + wavInfo.LoopEnd) * 4), pcmdChunkOffset + wavInfo.SampleOffset)
                        };
                    }
                }
                return samples;
            }
        }
        private static ProgramBank ReadPrograms<T>(EndianBinaryReader reader, int numPRGISlots) where T : IProgramInfo
        {
            long chunkOffset = FindChunk(reader, "prgi");
            if (chunkOffset == -1)
            {
                return null;
            }
            else
            {
                chunkOffset += 0x10;
                var programInfos = new IProgramInfo[numPRGISlots];
                for (int i = 0; i < programInfos.Length; i++)
                {
                    ushort offset = reader.ReadUInt16(chunkOffset + (2 * i));
                    if (offset != 0)
                    {
                        programInfos[i] = reader.ReadObject<T>(offset + chunkOffset);
                    }
                }
                return new ProgramBank
                {
                    ProgramInfos = programInfos,
                    KeyGroups = ReadKeyGroups(reader)
                };
            }
        }
        private static KeyGroup[] ReadKeyGroups(EndianBinaryReader reader)
        {
            long chunkOffset = FindChunk(reader, "kgrp");
            if (chunkOffset == -1)
            {
                return Array.Empty<KeyGroup>();
            }
            else
            {
                uint chunkLength = reader.ReadUInt32(chunkOffset + 0xC);
                var keyGroups = new KeyGroup[chunkLength / 8]; // 8 is the size of a KeyGroup
                for (int i = 0; i < keyGroups.Length; i++)
                {
                    keyGroups[i] = reader.ReadObject<KeyGroup>();
                }
                return keyGroups;
            }
        }
    }
}
