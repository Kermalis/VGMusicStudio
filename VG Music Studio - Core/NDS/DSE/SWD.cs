using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Codec;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Diagnostics;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class SWD
{
    #region Header
    public interface IHeader
	{
		//
	}
	public class Header : IHeader // Size 0x40
	{
        public string Type { get; set; }
        public byte[]? Unknown1 { get; set; }
        public uint Length { get; set; }
        public ushort Version { get; set; }
        public byte[]? Unknown2 { get; set; }
        public byte[]? Padding1 { get; set; }
        public ushort Year { get; set; }
        public byte Month { get; set; }
        public byte Day { get; set; }
        public byte Hour { get; set; }
        public byte Minute { get; set; }
        public byte Second { get; set; }
        public byte Centisecond { get; set; }
		public string Label { get; set; }
		public byte[]? Unknown3 { get; set; }
		public uint PCMDLength { get; set; }
		public byte[]? Unknown4 { get; set; }
		public ushort NumWAVISlots { get; set; }
		public ushort NumPRGISlots { get; set; }
        public byte NumKeyGroups { get; set; }
        public byte[]? Unknown5 { get; set; }
		public uint WAVILength { get; set; }
        public byte[]? Padding2 { get; set; }

        public Header(EndianBinaryReader r)
        {
            // File type metadata - The file type, version, and size of the file
            Type = r.ReadString_Count(4);
            if (Type == "swdb")
            {
                r.Endianness = Endianness.BigEndian;
            }
            Unknown1 = new byte[4];
            r.ReadBytes(Unknown1);
            Length = r.ReadUInt32();
            Version = r.ReadUInt16();
            Unknown2 = new byte[2];
            r.ReadBytes(Unknown2);

            // Timestamp metadata - The time the SWD was published
            r.Endianness = Endianness.LittleEndian; // Timestamp is always Little Endian, regardless of version or type, so it must be set to Little Endian to be read

            Padding1 = new byte[8]; // Padding
            r.ReadBytes(Padding1);
            Year = r.ReadUInt16(); // Year
            Month = r.ReadByte(); // Month
            Day = r.ReadByte(); // Day
            Hour = r.ReadByte(); // Hour
            Minute = r.ReadByte(); // Minute
            Second = r.ReadByte(); // Second
            Centisecond = r.ReadByte(); // Centisecond
            if (Type == "swdb") { r.Endianness = Endianness.BigEndian; } // If type is swdb, restore back to Big Endian


            // Info table
            Label = r.ReadString_Count(16);

            switch (Version) // To ensure the version differences apply beyond this point
            {
                case 1026:
                    {
                        Unknown3 = new byte[22];
                        r.ReadBytes(Unknown3);

                        NumWAVISlots = r.ReadByte();

                        NumPRGISlots = r.ReadByte();

                        NumKeyGroups = r.ReadByte();

                        Padding2 = new byte[7];
                        r.ReadBytes(Padding2);

                        break;
                    }
                case 1045:
                    {
                        Unknown3 = new byte[16];
                        r.ReadBytes(Unknown3);

                        PCMDLength = r.ReadUInt32();

                        Unknown4 = new byte[2];
                        r.ReadBytes(Unknown4);

                        NumWAVISlots = r.ReadUInt16();

                        NumPRGISlots = r.ReadUInt16();

                        Unknown5 = new byte[2];
                        r.ReadBytes(Unknown5);

                        WAVILength = r.ReadUInt32();

                        break;
                    }
            }
        }
	}

	public class ChunkHeader : IHeader // Size 0x10
	{
        public string Name { get; set; }
        public byte[] Padding { get; set; }
        public ushort Version { get; set; }
        public uint ChunkBegin { get; set; }
        public uint ChunkEnd { get; set; }

        public ChunkHeader(EndianBinaryReader r, long chunkOffset, SWD swd)
        {
            long oldOffset = r.Stream.Position;
            r.Stream.Position = chunkOffset;

            // Chunk Name
            Name = r.ReadString_Count(4);

            // Padding
            Padding = new byte[2];
            r.ReadBytes(Padding);

            // Version
            Version = r.ReadUInt16();

            // Chunk Begin
            r.Endianness = Endianness.LittleEndian; // To ensure this is read in Little Endian in all versions and types
            ChunkBegin = r.ReadUInt32();
            if (swd.Type == "swdb") { r.Endianness = Endianness.BigEndian; } // To revert back to Big Endian when the type is "swdb"

            // Chunk End
            ChunkEnd = r.ReadUInt32();

            r.Stream.Position = oldOffset;
        }
    }
    #endregion

    #region SplitEntry
    public interface ISplitEntry
	{
		byte LowKey { get; }
		byte HighKey { get; }
		ushort SampleId { get; }
		byte SampleRootKey { get; }
		sbyte SampleTranspose { get; }
		byte AttackVolume { get; set; }
		byte Attack { get; set; }
		byte Decay1 { get; set; }
		byte Sustain { get; set; }
		byte Hold { get; set; }
		byte Decay2 { get; set; }
		byte Release { get; set; }
	}
	public class SplitEntry : ISplitEntry // 0x30
	{
        public byte Unknown1 { get; set; }
		public byte Id { get; set; }
		public byte[] Unknown2 { get; set; }
		public byte LowKey { get; set; }
		public byte HighKey { get; set; }
		public byte LowKey2 { get; set; }
		public byte HighKey2 { get; set; }
		public byte LowVelocity { get; set; }
		public byte HighVelocity { get; set; }
		public byte LowVelocity2 { get; set; }
		public byte HighVelocity2 { get; set; }
		public byte[] Unknown3 { get; set; }
		public ushort SampleId { get; set; }
		public byte[] Unknown4 { get; set; }
		public byte SampleRootKey { get; set; }
		public sbyte SampleTranspose { get; set; }
		public byte SampleVolume { get; set; }
		public sbyte SamplePanpot { get; set; }
		public byte KeyGroupId { get; set; }
		public byte[]? Unknown5 { get; set; }
		public byte AttackVolume { get; set; }
		public byte Attack { get; set; }
		public byte Decay1 { get; set; }
		public byte Sustain { get; set; }
		public byte Hold { get; set; }
		public byte Decay2 { get; set; }
		public byte Release { get; set; }
		public byte Break { get; set; }

		ushort ISplitEntry.SampleId => SampleId;

        public SplitEntry(EndianBinaryReader r, SWD swd)
        {
            Unknown1 = r.ReadByte();

            Id = r.ReadByte();

            Unknown2 = new byte[2];
            r.ReadBytes(Unknown2);

            LowKey = r.ReadByte();

            HighKey = r.ReadByte();

            LowKey2 = r.ReadByte();

            HighKey2 = r.ReadByte();

            LowVelocity = r.ReadByte();

            HighVelocity = r.ReadByte();

            LowVelocity2 = r.ReadByte();

            HighVelocity2 = r.ReadByte();

            switch (swd.Version)
            {
                case 1026:
                    {
                        Unknown3 = new byte[5];
                        r.ReadBytes(Unknown3);

                        SampleId = r.ReadByte();

                        Unknown4 = new byte[2];
                        r.ReadBytes(Unknown4);

                        SampleRootKey = r.ReadByte();

                        SampleTranspose = r.ReadSByte();

                        SampleVolume = r.ReadByte();

                        SamplePanpot = r.ReadSByte();

                        KeyGroupId = r.ReadByte();

                        Unknown5 = new byte[15];
                        r.ReadBytes(Unknown5);

                        AttackVolume = r.ReadByte();

                        Attack = r.ReadByte();

                        Decay1 = r.ReadByte();

                        Sustain = r.ReadByte();

                        Hold = r.ReadByte();

                        Decay2 = r.ReadByte();

                        Release = r.ReadByte();

                        Break = r.ReadByte();

                        break;
                    }
                case 1045:
                    {
                        Unknown2 = new byte[6];
                        r.ReadBytes(Unknown2);

                        SampleId = r.ReadUInt16();

                        Unknown3 = new byte[2];
                        r.ReadBytes(Unknown3);

                        SampleRootKey = r.ReadByte();

                        SampleTranspose = r.ReadSByte();

                        SampleVolume = r.ReadByte();

                        SamplePanpot = r.ReadSByte();

                        KeyGroupId = r.ReadByte();

                        Unknown4 = new byte[13];
                        r.ReadBytes(Unknown4);

                        AttackVolume = r.ReadByte();

                        Attack = r.ReadByte();

                        Decay1 = r.ReadByte();

                        Sustain = r.ReadByte();

                        Hold = r.ReadByte();

                        Decay2 = r.ReadByte();

                        Release = r.ReadByte();

                        Break = r.ReadByte();

                        break;
                    }

                // In the event that there's a SWD version that hasn't been discovered yet
                default: throw new NotImplementedException("This version of the SWD specification has not been implemented into VG Music Studio.");
            }
        }
	}
#endregion

    #region ProgramInfo
    public interface IProgramInfo
	{
		ISplitEntry[] SplitEntries { get; }
	}
	public class ProgramInfo : IProgramInfo
	{
		public ushort Id { get; set; }
		public byte NumSplits { get; set; }
        public byte[] Unknown1 { get; set; }
		public byte Volume { get; set; }
		public byte Panpot { get; set; }
		public byte[] Unknown2 { get; set; }
		public byte NumLFOs { get; set; }
		public byte[] Unknown3 { get; set; }
		public LFOInfo[] LFOInfos { get; set; }
		public byte[]? Unknown4 { get; set; }
        public KeyGroup[]? KeyGroups { get; set; }
		public SplitEntry[] SplitEntries { get; set; }

		ISplitEntry[] IProgramInfo.SplitEntries => SplitEntries;

        public ProgramInfo(EndianBinaryReader r, SWD swd)
        {
            switch(swd.Version)
            {
                case 1026:
                    {
                        Id = r.ReadByte();

                        NumSplits = r.ReadByte();

                        Unknown1 = new byte[2];
                        r.ReadBytes(Unknown1);

                        Volume = r.ReadByte();

                        Panpot = r.ReadByte();

                        Unknown2 = new byte[5];
                        r.ReadBytes(Unknown2);

                        NumLFOs = r.ReadByte();

                        Unknown3 = new byte[4];
                        r.ReadBytes(Unknown3);

                        KeyGroups = new KeyGroup[16];

                        LFOInfos = new LFOInfo[NumLFOs];
                        for (int i = 0; i < NumLFOs; i++)
                        {
                            LFOInfos[i] = new LFOInfo(r);
                        };

                        SplitEntries = new SplitEntry[NumSplits];

                        break;
                    }

                case 1045:
                    {
                        Id = r.ReadUInt16();

                        NumSplits = r.ReadByte();

                        Unknown1 = new byte[1];
                        r.ReadBytes(Unknown1);

                        Volume = r.ReadByte();

                        Panpot = r.ReadByte();

                        Unknown2 = new byte[5];
                        r.ReadBytes(Unknown2);

                        NumLFOs = r.ReadByte();

                        Unknown3 = new byte[4];
                        r.ReadBytes(Unknown3);

                        LFOInfos = new LFOInfo[NumLFOs];
                        for (int i = 0; i < NumLFOs; i++)
                        {
                            LFOInfos[i] = new LFOInfo(r);
                        };

                        Unknown4 = new byte[16];
                        r.ReadBytes(Unknown4);

                        SplitEntries = new SplitEntry[NumSplits];
                        for (int i = 0; i < NumSplits; i++)
                        {
                            SplitEntries[i] = new SplitEntry(r, swd);
                        }

                        break;
                    }

                // In the event that there's a version that hasn't been discovered yet
                default: throw new NotImplementedException("This Digital Sound Elements version has not been implemented into VG Music Studio.");
            }

        }

    }
    #endregion

    #region WavInfo
    public interface IWavInfo
	{
		byte RootNote { get; }
		sbyte Transpose { get; }
		SampleFormat SampleFormat { get; }
		bool Loop { get; }
		uint SampleRate { get; }
		uint SampleOffset { get; }
		uint LoopStart { get; }
		uint LoopEnd { get; }
		byte EnvMulti { get; }
		byte AttackVolume { get; }
		byte Attack { get; }
		byte Decay1 { get; }
		byte Sustain { get; }
		byte Hold { get; }
		byte Decay2 { get; }
		byte Release { get; }
	}

    public class WavInfo : IWavInfo // Size 0x40
	{
        public byte[] Entry { get; set; }
        public ushort Id { get; set; }
        public byte[] Unknown2 { get; set; }
        public byte RootNote { get; set; }
        public sbyte Transpose { get; set; }
        public byte Volume { get; set; }
        public sbyte Panpot { get; set; }
        public byte[] Unknown3 { get; set; }
        public ushort Version { get; set; }
        public SampleFormat SampleFormat { get; set; }
        public byte Unknown4 { get; set; }
        public bool Loop { get; set; }
        public byte Unknown5 { get; set; }
        public byte SamplesPer32Bits { get; set; }
        public byte Unknown6 { get; set; }
        public byte BitDepth { get; set; }
        public byte[] Unknown7 { get; set; }
        public uint SampleRate { get; set; }
        public uint SampleOffset { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopEnd { get; set; }
        public byte EnvOn { get; set; }
        public byte EnvMulti { get; set; }
        public byte[] Unknown8 { get; set; }
        public byte AttackVolume { get; set; }
        public byte Attack { get; set; }
        public byte Decay1 { get; set; }
        public byte Sustain { get; set; }
        public byte Hold { get; set; }
        public byte Decay2 { get; set; }
        public byte Release { get; set; }
        public byte Break { get; set; }

        public WavInfo(EndianBinaryReader r, SWD swd)
        {
            // SWD version format check
			switch(swd.Version)
			{

                case 1026: 
                    {
                        // The wave table Entry Variable
                        Entry = new byte[1]; // Specify a variable with a byte array before doing EndianBinaryReader.ReadBytes()
                        r.ReadBytes(Entry); // Reads the byte

                        // Wave ID
                        Id = r.ReadByte(); // Reads the ID of the wave sample

                        // Currently undocumented variable(s)
                        Unknown2 = new byte[2]; // Specify a variable with a byte array before doing EndianBinaryReader.ReadBytes()
                        r.ReadBytes(Unknown2); // Reads the bytes

                        // Root Note
                        RootNote = r.ReadByte();

                        // Transpose
                        Transpose = r.ReadSByte();

                        // Volume
                        Volume = r.ReadByte();

                        // Panpot
                        Panpot = r.ReadSByte();

                        // Sample Format
                        if (swd.Type == "swdb")
                        {
                            r.Endianness = Endianness.LittleEndian;
                            SampleFormat = (SampleFormat)r.ReadUInt16();
                            r.Endianness = Endianness.BigEndian;
                        }
                        else
                        {
                            r.Endianness = Endianness.BigEndian;
                            SampleFormat = (SampleFormat)r.ReadUInt16();
                            r.Endianness = Endianness.LittleEndian;
                        }

                        // Undocumented variable(s)
                        Unknown3 = new byte[7];
                        r.ReadBytes(Unknown3);

                        // Version
                        Version = r.ReadUInt16();

                        // Loop enable and disable
                        Loop = r.ReadBoolean();

                        // Sample Rate
                        SampleRate = r.ReadUInt32();

                        // Sample Offset
                        SampleOffset = r.ReadUInt32();

                        // Loop Start
                        LoopStart = r.ReadUInt32();

                        // Loop End
                        LoopEnd = r.ReadUInt32();

                        // Undocumented variable(s)
                        Unknown7 = new byte[16];
                        r.ReadBytes(Unknown7);

                        // Volume Envelop On
                        EnvOn = r.ReadByte();

                        // Volume Envelop Multiple
                        EnvMulti = r.ReadByte();

                        // Undocumented variable(s)
                        Unknown8 = new byte[6];
                        r.ReadBytes(Unknown8);

                        // Attack Volume
                        AttackVolume = r.ReadByte();

                        // Attack
                        Attack = r.ReadByte();

                        // Decay 1
                        Decay1 = r.ReadByte();

                        // Sustain
                        Sustain = r.ReadByte();

                        // Hold
                        Hold = r.ReadByte();

                        // Decay 2
                        Decay2 = r.ReadByte();

                        // Release
                        Release = r.ReadByte();

                        // The wave table Break Variable
                        Break = r.ReadByte();

                        break;
                    }

                case 1045: // Digital Sound Elements - SWD Specification 4.21
                    {
                        // The wave table Entry Variable
                        Entry = new byte[2]; // Specify a variable with a byte array before doing EndianBinaryReader.ReadBytes()
                        r.ReadBytes(Entry); // Reads the bytes

                        // Wave ID
                        r.Endianness = Endianness.LittleEndian; // Changes the reader to Little Endian
                        Id = r.ReadUInt16(); // Reads the ID of the wave sample as Little Endian
                        if (swd.Type == "swdb") // Checks if the str string value matches "swdb"
                        {
                           r.Endianness = Endianness.BigEndian; // Restores the reader back to Big Endian
                        }

                        // Currently undocumented variable
                        Unknown2 = new byte[2]; // Same as the one before
                        r.ReadBytes(Unknown2);

                        // Root Note
                        RootNote = r.ReadByte();

                        // Transpose
                        Transpose = r.ReadSByte();

                        // Volume
                        Volume = r.ReadByte();

                        // Panpot
                        Panpot = r.ReadSByte();

                        // Undocumented variable
                        Unknown3 = new byte[6]; // Same as before, except we need to read 6 bytes instead of 2
                        r.ReadBytes(Unknown3);

                        // Version
                        Version = r.ReadUInt16();

                        // Sample Format
                        if (swd.Type == "swdb")
                        {
                            r.Endianness = Endianness.LittleEndian;
                            SampleFormat = (SampleFormat)r.ReadUInt16();
                            r.Endianness = Endianness.BigEndian;
                        }
                        else
                        {
                            r.Endianness = Endianness.BigEndian;
                            SampleFormat = (SampleFormat)r.ReadUInt16();
                            r.Endianness = Endianness.LittleEndian;
                        }

                        // Undocumented variable(s)
                        Unknown4 = r.ReadByte();

                        // Loop enable or disable
                        Loop = r.ReadBoolean();

                        // Undocumented variable(s)
                        Unknown5 = r.ReadByte();

                        // Samples per 32 bits
                        SamplesPer32Bits = r.ReadByte();

                        // Undocumented variable(s)
                        Unknown6 = r.ReadByte();

                        // Bit Depth
                        BitDepth = r.ReadByte();

                        // Undocumented variable(s)
                        Unknown7 = new byte[6]; // Once again, create a variable to specify 6 bytes and to read using it
                        r.ReadBytes(Unknown7);

                        // Sample Rate
                        SampleRate = r.ReadUInt32();

                        // Sample Offset
                        SampleOffset = r.ReadUInt32();

                        // Loop Start
                        LoopStart = r.ReadUInt32();

                        // Loop End
                        LoopEnd = r.ReadUInt32();

                        // Volume Envelop On
                        EnvOn = r.ReadByte();

                        // Volume Envelop Multiple
                        EnvMulti = r.ReadByte();

                        // Undocumented variable(s)
                        Unknown8 = new byte[6]; // Same as before
                        r.ReadBytes(Unknown8);

                        // Attack Volume
                        AttackVolume = r.ReadByte();

                        // Attack
                        Attack = r.ReadByte();

                        // Decay 1
                        Decay1 = r.ReadByte();

                        // Sustain
                        Sustain = r.ReadByte();

                        // Hold
                        Hold = r.ReadByte();

                        // Decay 2
                        Decay2 = r.ReadByte();

                        // Release
                        Release = r.ReadByte();

                        // The wave table Break Variable
                        Break = r.ReadByte();

                        break;
					}

					// In the event that there's a version that hasn't been discovered yet
					default: throw new NotImplementedException("This version of the SWD specification has not yet been implemented into VG Music Studio.");
			}
        }
    }
    #endregion

    public class SampleBlock
	{
		public WavInfo? WavInfo;
        public DSPADPCM? DSPADPCM;
		public byte[]? Data;
        //public short[]? Data16Bit;
	}
	public class ProgramBank
	{
		public ProgramInfo[]? ProgramInfos;
		public KeyGroup[]? KeyGroups;
	}
	public class KeyGroup // Size 0x8
	{
		public ushort Id { get; set; }
		public byte Poly { get; set; }
		public byte Priority { get; set; }
		public byte LowNote { get; set; }
		public byte HighNote { get; set; }
		public ushort Unknown { get; set; }

        public KeyGroup(EndianBinaryReader r, SWD swd)
        {
            r.Endianness = Endianness.LittleEndian;
            Id = r.ReadUInt16();
            if (swd.Type == "swdb") { r.Endianness = Endianness.BigEndian; }

            Poly = r.ReadByte();

            Priority = r.ReadByte();

            LowNote = r.ReadByte();

            HighNote = r.ReadByte();

            Unknown = r.ReadUInt16();
        }
	}
	public class LFOInfo
	{
		public byte Unknown1 { get; set; }
		public byte HasData { get; set; }
		public byte Type { get; set; } // LFOType enum
		public byte CallbackType { get; set; }
		public uint Unknown4 { get; set; }
		public ushort Unknown8 { get; set; }
		public ushort UnknownA { get; set; }
		public ushort UnknownC { get; set; }
		public byte UnknownE { get; set; }
		public byte UnknownF { get; set; }

        public LFOInfo(EndianBinaryReader r)
        {
            Unknown1 = r.ReadByte();

            HasData = r.ReadByte();

            Type = r.ReadByte();

            CallbackType = r.ReadByte();

            Unknown4 = r.ReadUInt32();

            Unknown8 = r.ReadUInt16();

            UnknownA = r.ReadUInt16();

            UnknownC = r.ReadUInt16();

            UnknownE = r.ReadByte();

            UnknownF = r.ReadByte();
        }
	}

    public Header? Info;
    public string Type; // "swdb" or "swdl"
	public uint Length;
	public ushort Version;

    public long WaviChunkOffset, WaviDataOffset,
        PrgiChunkOffset, PrgiDataOffset,
        KgrpChunkOffset, KgrpDataOffset,
        PcmdChunkOffset, PcmdDataOffset,
        EodChunkOffset;
    public ChunkHeader? WaviInfo, PrgiInfo, KgrpInfo, PcmdInfo, EodInfo;

	public ProgramBank? Programs;
	public SampleBlock[]? Samples;

	public SWD(string path)
	{
		using (var stream = new MemoryStream(File.ReadAllBytes(path)))
		{
			var r = new EndianBinaryReader(stream, ascii: true);
            Info = new Header(r);
            Type = Info.Type;
            Length = Info.Length;
            Version = Info.Version;
            Programs = ReadPrograms(r, Info.NumPRGISlots, this);

            switch (Version)
            {
                case 0x402:
                    {
                        Samples = ReadSamples(r, Info.NumWAVISlots, this);
                        break;
                    }
                case 0x415:
                    {
                        if (Info.PCMDLength != 0 && (Info.PCMDLength & 0xFFFF0000) != 0xAAAA0000)
                        {
                            Samples = ReadSamples(r, Info.NumWAVISlots, this);
                        }
                        break;
                    }
                default: throw new InvalidDataException();
            }
        }
		return;
	}

    #region FindChunk
    private static long FindChunk(EndianBinaryReader r, string chunk)
	{
		long pos = -1;
		long oldPosition = r.Stream.Position;
        r.Stream.Position = 0;
        while (r.Stream.Position < r.Stream.Length)
		{
			string str = r.ReadString_Count(4);
            if (str == chunk)
			{
				pos = r.Stream.Position - 4;
                break;
			}
            switch (str)
			{
				case "swdb":
                {
                    r.Stream.Position += 0x4C;
                    break;
                }
                case "swdl":
				{
					r.Stream.Position += 0x4C;
					break;
				}
				default:
				{
                    Debug.WriteLine($"Ignoring {str} chunk");
					r.Stream.Position += 0x8;
					uint length = r.ReadUInt32();
					r.Stream.Position += length;
					r.Stream.Align(16);
                    break;
				}
            }
        }
		r.Stream.Position = oldPosition;
        return pos;
	}
    #endregion

    #region SampleBlock
    private SampleBlock[] ReadSamples(EndianBinaryReader r, int numWAVISlots, SWD swd)
	{
        // These apply the chunk offsets that are found to both local and the field functions, chunk header constructors are available here incase they're needed
        long waviChunkOffset = swd.WaviChunkOffset = FindChunk(r, "wavi");
        long pcmdChunkOffset = swd.PcmdChunkOffset = FindChunk(r, "pcmd");
        long eodChunkOffset = swd.EodChunkOffset = FindChunk(r, "eod ");
        if (waviChunkOffset == -1 || pcmdChunkOffset == -1)
		{
			throw new InvalidDataException();
		}
		else
		{
            WaviInfo = new ChunkHeader(r, waviChunkOffset, swd);
            long waviDataOffset = WaviDataOffset = waviChunkOffset + 0x10;
            PcmdInfo = new ChunkHeader(r, pcmdChunkOffset, swd);
            long pcmdDataOffset = PcmdDataOffset = pcmdChunkOffset + 0x10;
            EodInfo = new ChunkHeader(r, eodChunkOffset, swd);
            var samples = new SampleBlock[numWAVISlots];
            for (int i = 0; i < numWAVISlots; i++)
			{
				r.Stream.Position = waviDataOffset + (2 * i);
				ushort offset = r.ReadUInt16();
                if (offset != 0)
				{
					r.Stream.Position = offset + waviDataOffset;
                    WavInfo wavInfo = new WavInfo(r, swd);
                    switch (Type)
                    {
                        case "swdm":
                            {
                                throw new NotImplementedException("This Digital Sound Elements type has not yet been implemented.");
                            }

                        case "swdl":
                            {
                                samples[i] = new SampleBlock
                                {
                                    WavInfo = wavInfo,
                                    Data = new byte[(int)((wavInfo.LoopStart + wavInfo.LoopEnd) * 4)],
                                };
                                r.Stream.Position = pcmdDataOffset + wavInfo.SampleOffset;
                                r.ReadBytes(samples[i].Data);

                                break;
                            }

                        case "swdb":
                            {
                                samples[i] = new SampleBlock
                                {
                                    WavInfo = wavInfo,
                                    //Data = new byte[samples[i].DSPADPCM!.Info.num_samples]
                                };
                                r.Stream.Position = pcmdDataOffset + wavInfo.SampleOffset;
                                samples[i].DSPADPCM = new DSPADPCM(r);
                                Span<short> data = new short[samples[i].DSPADPCM!.Info.num_samples / 2];
                                data = DSPADPCM.DSPADPCMToPCM16(samples[i].DSPADPCM!.Data, samples[i].DSPADPCM!.Info.num_samples, samples[i].DSPADPCM!.Info);
                                samples[i].Data = new byte[samples[i].DSPADPCM!.Info.ea];
                                //wavInfo.LoopStart = wavInfo.LoopStart / 4;
                                //wavInfo.LoopEnd = wavInfo.LoopEnd / 4;
                                //samples[i].Data16Bit = new short[(int)((wavInfo.LoopStart + wavInfo.LoopEnd) / 4) / 4 + 85];
                                //DSPADPCM.Decode(samples[i].DSPADPCM!.Data, samples[i].Data16Bit, ref samples[i].DSPADPCM!.Info, samples[i].DSPADPCM!.Info.num_samples);
                                int e = 0;
                                for (int d = 0; d < samples[i].DSPADPCM!.Info.num_adpcm_nibbles; d++)
                                {
                                    samples[i].Data![e] = (byte)(data[d] >> 8);
                                    if (e < samples[i].Data!.Length) { e += 1; }
                                    if (e >= samples[i].Data!.Length) { break; }
                                    samples[i].Data![e] = (byte)(data[d]);
                                    if (e < samples[i].Data!.Length) { e += 1; }
                                    if (e >= samples[i].Data!.Length) { break; }
                                }

                                // Trying to implement an error message that informs anyone of a EndOfStreamException caused by a different encoding type.
                                //if (swd.PcmdDataOffset + samples[i].WavInfo!.SampleOffset + (samples[i].DSPADPCM!.NumADPCMNibbles / 2) + 9 > swd.EodChunkOffset)
                                //{
                                //    throw new EndOfStreamException("End of Stream Exception:\n" +
                                //        "The number of ADPCM nibbles, divided by 2, plus 9 bytes, reads the sample data beyond this SWD.\n" +
                                //        "\n" +
                                //        "This is because VG Music Studio is incorrectly reading the actual size of the DSP-ADPCM sample data.\n" +
                                //        "\n" +
                                //        "If you are a developer of VG Music Studio, please check the code in DSPADPCM.cs\n" +
                                //        "and verify the SWD file in a hex editor to make sure it's being read correctly.\n" +
                                //        "\n" +
                                //        "Call Stack:");
                                //}

                                //samples[i].DSPADPCM.GetSamples(samples[i].DSPADPCM.Info, samples[i].DSPADPCM.Info.ea);
                                //samples[i].Data = samples[i].DSPADPCM!.Data;
                                //Array.Resize(ref samples[i].Data, (int)((wavInfo.LoopStart + wavInfo.LoopEnd) / 6));
                                //samples[i].Data = samples[i].DSPADPCM.Data;
                                break;
                            }
                        default:
                            {
                                throw new NotImplementedException("This Digital Sound Elements type has not yet been implemented.");
                            }
                    }
                }
			}
			return samples;
		}
	}
    #endregion

    #region ProgramBank and KeyGroup
    private static ProgramBank? ReadPrograms(EndianBinaryReader r, int numPRGISlots, SWD swd)
	{
		long chunkOffset = swd.PrgiChunkOffset = FindChunk(r, "prgi");
        if (chunkOffset == -1)
		{
			return null;
		}

        swd.PrgiInfo = new ChunkHeader(r, chunkOffset, swd);
        long dataOffset = swd.PrgiDataOffset = chunkOffset + 0x10;
        var programInfos = new ProgramInfo[numPRGISlots];
		for (int i = 0; i < programInfos.Length; i++)
		{
			r.Stream.Position = dataOffset + (2 * i);
            ushort offset = r.ReadUInt16();
            if (offset != 0)
			{
				r.Stream.Position = offset + dataOffset;
                programInfos[i] = new ProgramInfo(r, swd);
            }
		}
		return new ProgramBank
		{
			ProgramInfos = programInfos,
			KeyGroups = ReadKeyGroups(r, swd),
		};
	}
	private static KeyGroup[] ReadKeyGroups(EndianBinaryReader r, SWD swd)
	{
		long chunkOffset = swd.KgrpChunkOffset = FindChunk(r, "kgrp");
        if (chunkOffset == -1)
		{
			return Array.Empty<KeyGroup>();
		}

        ChunkHeader info = swd.KgrpInfo = new ChunkHeader(r, chunkOffset, swd);
        swd.KgrpDataOffset = chunkOffset + 0x10;
        r.Stream.Position = swd.KgrpDataOffset;
        var keyGroups = new KeyGroup[info.ChunkEnd / 8]; // 8 is the size of a KeyGroup
        for (int i = 0; i < keyGroups.Length; i++)
		{
			keyGroups[i] = new KeyGroup(r, swd);
        }
		return keyGroups;
	}
    #endregion
}
