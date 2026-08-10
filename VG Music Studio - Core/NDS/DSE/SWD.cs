using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Codec;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
		public byte BankID { get; set; }
		public byte WaveID { get; set; }
		public byte[]? Padding { get; set; }
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
		public byte[]? HeaderEndPadding { get; set; } // TODO: Check if there's anything other than padding in this array

		public Header(EndianBinaryReader r)
		{
			// File type metadata - The file type, version, and size of the file
			Type = r.ReadString_Count(4);
			if (Type.StartsWith("swd") == false) // Failsafe, to check if the file is a valid SWD file
			{
				throw new InvalidDataException("Invalid Data Exception:\nThis file is not a Wave Data (.SWD) file, please make sure the file extension is correct before opening.\nCall Stack:");
			}
			if (Type == "swdb")
			{
				r.Endianness = Endianness.BigEndian;
			}
			Unknown1 = new byte[4];
			r.ReadBytes(Unknown1);
			Length = r.ReadUInt32();
			Version = r.ReadUInt16();
			BankID = r.ReadByte();
			WaveID = r.ReadByte();

			// Timestamp metadata - The time the SWD was published
			r.Endianness = Endianness.LittleEndian; // Timestamp is always Little Endian, regardless of version or type, so it must be set to Little Endian to be read

			Padding = new byte[8]; // Padding
			r.ReadBytes(Padding);
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

						HeaderEndPadding = new byte[7];
						r.ReadBytes(HeaderEndPadding);

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
		sbyte LowKey { get; }
		sbyte HighKey { get; }
		ushort SampleId { get; }
		sbyte SampleRootKey { get; }
		sbyte SampleTranspose { get; }
		byte EnvelopeVolume { get; }
		byte EnvelopeMultiplier { get; }
		byte AttackVolume { get; }
		byte AttackTime { get; }
		byte Decay { get; }
		byte Sustain { get; }
		byte Hold { get; }
		byte Fade { get; }
		byte Release { get; }
	}
	public class SplitEntry : ISplitEntry // 0x30
	{
		public ushort Id { get; set; }
		public byte BendRange { get; set; }
		public bool Enabled { get; set; }
		public sbyte LowKey { get; set; }
		public sbyte HighKey { get; set; }
		public sbyte LowKey2 { get; set; }
		public sbyte HighKey2 { get; set; }
		public sbyte LowVelocity { get; set; }
		public sbyte HighVelocity { get; set; }
		public sbyte LowVelocity2 { get; set; }
		public sbyte HighVelocity2 { get; set; }
		public byte[]? Padding { get; set; }
		public byte[]? Padding2 { get; set; }
		public byte[]? Unknown3 { get; set; }
		public ushort SampleId { get; set; }
		public byte FineTune { get; set; }
		public sbyte CoarseTune { get; set; }
		public byte[] Unknown4 { get; set; }
		public sbyte SampleRootKey { get; set; }
		public sbyte SampleTranspose { get; set; }
		public sbyte SampleVolume { get; set; }
		public sbyte SamplePanpot { get; set; }
		public byte KeyGroupId { get; set; }
		public byte KeyGroupFlag { get; set; }
		public ushort UnusedValue { get; set; }
		public byte EnvelopeVolume { get; set; }
		public byte EnvelopeMultiplier { get; set; }
		public byte[]? Unknown5 { get; set; }
		public byte AttackVolume { get; set; }
		public byte AttackTime { get; set; }
		public byte Decay { get; set; }
		public byte Sustain { get; set; }
		public byte Hold { get; set; }
		public byte Fade { get; set; }
		public byte Release { get; set; }
		public byte Break { get; set; }

		ushort ISplitEntry.SampleId => SampleId;

		public SplitEntry(EndianBinaryReader r, SWD swd)
		{
			if (swd.Type == "swdl")
			{
				r.Endianness = Endianness.BigEndian;
				Id = r.ReadUInt16(); // ID for the SplitEntry is always read in Big Endian format
				r.Endianness = Endianness.LittleEndian;
			}
			else
			{
				Id = r.ReadUInt16();
			}

			BendRange = r.ReadByte();

			Enabled = r.ReadBoolean();

			LowKey = r.ReadSByte();

			HighKey = r.ReadSByte();

			LowKey2 = r.ReadSByte();

			HighKey2 = r.ReadSByte();

			LowVelocity = r.ReadSByte();

			HighVelocity = r.ReadSByte();

			LowVelocity2 = r.ReadSByte();

			HighVelocity2 = r.ReadSByte();

			switch (swd.Version)
			{
				case 1026:
					{
						Padding = new byte[5];
						r.ReadBytes(Padding);

						SampleId = r.ReadByte();

						FineTune = r.ReadByte();

						CoarseTune = r.ReadSByte();

						SampleRootKey = r.ReadSByte();

						SampleTranspose = r.ReadSByte();

						SampleVolume = r.ReadSByte();

						SamplePanpot = r.ReadSByte();

						KeyGroupId = r.ReadByte();

						KeyGroupFlag = r.ReadByte();

						UnusedValue = r.ReadUInt16();

						Padding2 = new byte[4];
						r.ReadBytes(Padding2);

						EnvelopeVolume = r.ReadByte();

						EnvelopeMultiplier = r.ReadByte();

						Unknown4 = new byte[6];
						r.ReadBytes(Unknown4);

						AttackVolume = r.ReadByte();

						AttackTime = r.ReadByte();

						Decay = r.ReadByte();

						Sustain = r.ReadByte();

						Hold = r.ReadByte();

						Fade = r.ReadByte();

						Release = r.ReadByte();

						Break = r.ReadByte();

						break;
					}
				case 1045:
					{
						Padding = new byte[6];
						r.ReadBytes(Padding);

						SampleId = r.ReadUInt16();

						FineTune = r.ReadByte();

						CoarseTune = r.ReadSByte();

						SampleRootKey = r.ReadSByte();

						SampleTranspose = r.ReadSByte();

						SampleVolume = r.ReadSByte();

						SamplePanpot = r.ReadSByte();

						KeyGroupId = r.ReadByte();

						KeyGroupFlag = r.ReadByte();

						UnusedValue = r.ReadUInt16();

						Padding2 = new byte[2];
						r.ReadBytes(Padding2);

						EnvelopeVolume = r.ReadByte();

						EnvelopeMultiplier = r.ReadByte();

						Unknown4 = new byte[6];
						r.ReadBytes(Unknown4);

						AttackVolume = r.ReadByte();

						AttackTime = r.ReadByte();

						Decay = r.ReadByte();

						Sustain = r.ReadByte();

						Hold = r.ReadByte();

						Fade = r.ReadByte();

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
		public ushort NumSplits { get; set; }
		public byte[]? Unknown1 { get; set; }
		public byte Volume { get; set; }
		public byte Panpot { get; set; }
		public byte[] Unknown2 { get; set; }
		public byte NumLFOs { get; set; }
		public byte[] HeaderPadding { get; set; }
		public LFOInfo[] LFOInfos { get; set; }
		public byte[]? LFOPadding { get; set; }
		public KeyGroup[]? KeyGroups { get; set; }
		public SplitEntry[] SplitEntries { get; set; }

		ISplitEntry[] IProgramInfo.SplitEntries => SplitEntries;

		public ProgramInfo(EndianBinaryReader r, SWD swd)
		{
			switch (swd.Version)
			{
				case 1026:
					{
						Id = r.ReadByte();

						if (swd.Type == "swdb")
						{
							r.Endianness = Endianness.LittleEndian;
							NumSplits = r.ReadUInt16();
							r.Endianness = Endianness.BigEndian;
						}
						else
						{
							NumSplits = r.ReadUInt16();
						}

						Unknown1 = new byte[2];
						r.ReadBytes(Unknown1);

						Volume = r.ReadByte();

						Panpot = r.ReadByte();

						Unknown2 = new byte[5];
						r.ReadBytes(Unknown2);

						NumLFOs = r.ReadByte();

						HeaderPadding = new byte[4];
						r.ReadBytes(HeaderPadding);

						KeyGroups = new KeyGroup[16];

						LFOInfos = new LFOInfo[NumLFOs];
						for (int i = 0; i < NumLFOs; i++)
						{
							LFOInfos[i] = new LFOInfo(r);
						}

						SplitEntries = new SplitEntry[NumSplits];
						for (int i = 0; i < NumSplits; i++)
						{
							SplitEntries[i] = new SplitEntry(r, swd);
							if (SplitEntries[i].Id != i)
							{
								throw new DSEArrayIndexAndHeaderIDMismatchException(i, SplitEntries[i].Id);
							}
						}

						break;
					}

				case 1045:
					{
						Id = r.ReadUInt16();

						if (swd.Type == "swdb")
						{
							r.Endianness = Endianness.LittleEndian;
							NumSplits = r.ReadUInt16(); // NumSplits is always read in Little Endian format
							r.Endianness = Endianness.BigEndian;
						}
						else
						{
							NumSplits = r.ReadUInt16();
						}

						Volume = r.ReadByte();

						Panpot = r.ReadByte();

						Unknown2 = new byte[5];
						r.ReadBytes(Unknown2);

						NumLFOs = r.ReadByte();

						HeaderPadding = new byte[4];
						r.ReadBytes(HeaderPadding);

						LFOInfos = new LFOInfo[NumLFOs];
						for (int i = 0; i < NumLFOs; i++)
						{
							LFOInfos[i] = new LFOInfo(r);
						}

						LFOPadding = new byte[16];
						r.ReadBytes(LFOPadding);

						SplitEntries = new SplitEntry[NumSplits];
						for (int i = 0; i < NumSplits; i++)
						{
							SplitEntries[i] = new SplitEntry(r, swd);
							if (SplitEntries[i].Id != i)
							{
								throw new DSEArrayIndexAndHeaderIDMismatchException(i, SplitEntries[i].Id);
							}
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
		byte EnvMult { get; }
		byte AttackVolume { get; }
		byte AttackTime { get; }
		byte Decay { get; }
		byte Sustain { get; }
		byte Hold { get; }
		byte Fade { get; }
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
		public byte EnvMult { get; set; }
		public byte[] Unknown8 { get; set; }
		public byte AttackVolume { get; set; }
		public byte AttackTime { get; set; }
		public byte Decay { get; set; }
		public byte Sustain { get; set; }
		public byte Hold { get; set; }
		public byte Fade { get; set; }
		public byte Release { get; set; }
		public byte Break { get; set; }

		public WavInfo(EndianBinaryReader r, SWD swd)
		{
			// SWD version format check
			switch (swd.Version)
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

						// Volume Envelop Multiplier
						EnvMult = r.ReadByte();

						// Undocumented variable(s)
						Unknown8 = new byte[6];
						r.ReadBytes(Unknown8);

						// Attack Volume
						AttackVolume = r.ReadByte();

						// Attack
						AttackTime = r.ReadByte();

						// Decay
						Decay = r.ReadByte();

						// Sustain
						Sustain = r.ReadByte();

						// Hold
						Hold = r.ReadByte();

						// Fade
						Fade = r.ReadByte();

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

						// Volume Envelop Multiplier
						EnvMult = r.ReadByte();

						// Undocumented variable(s)
						Unknown8 = new byte[6]; // Same as before
						r.ReadBytes(Unknown8);

						// Attack Volume
						AttackVolume = r.ReadByte();

						// Attack
						AttackTime = r.ReadByte();

						// Decay
						Decay = r.ReadByte();

						// Sustain
						Sustain = r.ReadByte();

						// Hold
						Hold = r.ReadByte();

						// Fade
						Fade = r.ReadByte();

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
		public DSPADPCM DSPADPCM;
		public byte[]? Data;
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
	public class LFOInfo(EndianBinaryReader r)
	{
		public byte Entry { get; set; } = r.ReadByte();
		public byte HasData { get; set; } = r.ReadByte();
		public ModulationType ModulationType { get; set; } = (ModulationType)r.ReadByte();
		public WaveformType WaveformType { get; set; } = (WaveformType)r.ReadByte();
		public ushort Rate { get; set; } = r.ReadUInt16();
		public ushort Unused { get; set; } = r.ReadUInt16();
		public ushort Depth { get; set; } = r.ReadUInt16();
		public ushort Delay { get; set; } = r.ReadUInt16();
		public short Fade { get; set; } = r.ReadInt16();
		public ushort Break { get; set; } = r.ReadUInt16();
	}

	public string FileName;
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
		FileName = new FileInfo(path).Name;
		var stream = File.OpenRead(path);
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
		return;
	}

	#region SampleBlock
	private SampleBlock[] ReadSamples(EndianBinaryReader r, int numWAVISlots, SWD swd)
	{
		// These apply the chunk offsets that are found to both local and the field functions, chunk header constructors are available here incase they're needed
		long waviChunkOffset = swd.WaviChunkOffset = DSEUtils.FindChunk(r, "wavi");
		long pcmdChunkOffset = swd.PcmdChunkOffset = DSEUtils.FindChunk(r, "pcmd");
		long eodChunkOffset = swd.EodChunkOffset = DSEUtils.FindChunk(r, "eod ");
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
					var wavInfo = new WavInfo(r, swd);
					if (wavInfo.Id != i)
					{
						throw new DSEArrayIndexAndHeaderIDMismatchException(i, wavInfo.Id);
					}
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
									WavInfo = wavInfo, // This is the only variable we can use for this initializer declarator, since the samples are DSP-ADPCM compressed
								};
								r.Stream.Position = pcmdDataOffset + wavInfo.SampleOffset; // This sets the EndianBinaryReader stream position offset to the DSP-ADPCM header

								samples[i].DSPADPCM = new DSPADPCM(r, null); // Reads the entire DSP-ADPCM header and encoded data, also the SWD spec doesn't define number of channels
								samples[i].DSPADPCM.Decode(); // Decodes all bytes into PCM16 data
#if DEBUG
								// This is for dumping both the encoded and decoded samples, for ensuring that the decoder works correctly
								new FileInfo("./ExtractedSamples/" + FileName + "/dsp/").Directory!.Create();
								File.WriteAllBytes("./ExtractedSamples/" + FileName + "/dsp/" + "sample" + i.ToString() + ".dsp", [.. samples[i].DSPADPCM.Info[0].ToBytes(), .. samples[i].DSPADPCM.Data]);
								new FileInfo("./ExtractedSamples/" + FileName + "/wav/").Directory!.Create();
								File.WriteAllBytes("./ExtractedSamples/" + FileName + "/wav/" + "sample" + i.ToString() + ".wav", samples[i].DSPADPCM.ConvertToWav());
#endif
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
		long chunkOffset = swd.PrgiChunkOffset = DSEUtils.FindChunk(r, "prgi");
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
				if (programInfos[i].Id != i)
				{
					throw new DSEArrayIndexAndHeaderIDMismatchException(i, programInfos[i].Id);
				}
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
		long chunkOffset = swd.KgrpChunkOffset = DSEUtils.FindChunk(r, "kgrp");
		if (chunkOffset == -1)
		{
			return [];
		}

		ChunkHeader info = swd.KgrpInfo = new ChunkHeader(r, chunkOffset, swd);
		swd.KgrpDataOffset = chunkOffset + 0x10;
		r.Stream.Position = swd.KgrpDataOffset;
		var keyGroups = new KeyGroup[info.ChunkEnd / 8]; // 8 is the size of a KeyGroup
		for (int i = 0; i < keyGroups.Length; i++)
		{
			keyGroups[i] = new KeyGroup(r, swd);
			if (keyGroups[i].Id != i)
			{
				throw new DSEArrayIndexAndHeaderIDMismatchException(i, keyGroups[i].Id);
			}
		}
		return keyGroups;
	}
	#endregion
}
