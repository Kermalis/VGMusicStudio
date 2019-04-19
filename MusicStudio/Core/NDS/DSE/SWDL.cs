using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.MusicStudio.Core.NDS.DSE
{
    class SWDL
    {
        public interface IHeader
        {

        }
        class Header_V402 : IHeader
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
        class Header_V415 : IHeader
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

        public string Type; // "swdl"
        public byte[] Unknown;
        public uint Length;
        public ushort Version;
        public IHeader Header;

        public ProgramBank Programs;
        public SampleBlock[] Samples;

        public SWDL(string path)
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

        static SampleBlock[] ReadSamples<T>(EndianBinaryReader reader, int numWAVISlots) where T : IWavInfo
        {
            long waviChunkOffset = DSE.FindChunk(reader, "wavi");
            long pcmdChunkOffset = DSE.FindChunk(reader, "pcmd");
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
        static ProgramBank ReadPrograms<T>(EndianBinaryReader reader, int numPRGISlots) where T : IProgramInfo
        {
            long chunkOffset = DSE.FindChunk(reader, "prgi");
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
        static KeyGroup[] ReadKeyGroups(EndianBinaryReader reader)
        {
            long chunkOffset = DSE.FindChunk(reader, "kgrp");
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
