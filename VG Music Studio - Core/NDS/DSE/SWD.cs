using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Diagnostics;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class SWD
{
	public interface IHeader
	{
		//
	}
	private sealed class Header_V402 : IHeader // Size 0x40
	{
		[BinaryArrayFixedLength(8)]
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
	private sealed class Header_V415 : IHeader // Size 0x40
	{
		[BinaryArrayFixedLength(8)]
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

	public interface ISplitEntry
	{
		byte LowKey { get; }
		byte HighKey { get; }
		int SampleId { get; }
		byte SampleRootKey { get; }
		sbyte SampleTranspose { get; }
		byte AttackVolume { get; set; }
		byte Attack { get; set; }
		byte Decay { get; set; }
		byte Sustain { get; set; }
		byte Hold { get; set; }
		byte Decay2 { get; set; }
		byte Release { get; set; }
	}
	public sealed class SplitEntry_V402 : ISplitEntry // Size 0x30
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

		[BinaryIgnore]
		int ISplitEntry.SampleId => SampleId;
	}
	public sealed class SplitEntry_V415 : ISplitEntry // 0x30
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

		[BinaryIgnore]
		int ISplitEntry.SampleId => SampleId;
	}

	public interface IProgramInfo
	{
		ISplitEntry[] SplitEntries { get; }
	}
	public sealed class ProgramInfo_V402 : IProgramInfo
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
		public KeyGroup[] KeyGroups { get; set; }
		[BinaryArrayVariableLength(nameof(NumLFOs))]
		public LFOInfo LFOInfos { get; set; }
		[BinaryArrayVariableLength(nameof(NumSplits))]
		public SplitEntry_V402[] SplitEntries { get; set; }

		[BinaryIgnore]
		ISplitEntry[] IProgramInfo.SplitEntries => SplitEntries;
	}
	public sealed class ProgramInfo_V415 : IProgramInfo
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
		byte Attack { get; }
		byte Decay { get; }
		byte Sustain { get; }
		byte Hold { get; }
		byte Decay2 { get; }
		byte Release { get; }
	}
	public sealed class WavInfo_V402 : IWavInfo // Size 0x40
	{
		public byte Unknown1 { get; set; }
		public byte Id { get; set; }
		[BinaryArrayFixedLength(2)]
		public byte[] Unknown2 { get; set; }
		public byte RootNote { get; set; }
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
	public sealed class WavInfo_V415 : IWavInfo // 0x40
	{
		[BinaryArrayFixedLength(2)]
		public byte[] Unknown1 { get; set; }
		public ushort Id { get; set; }
		[BinaryArrayFixedLength(2)]
		public byte[] Unknown2 { get; set; }
		public byte RootNote { get; set; }
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
		public IProgramInfo?[] ProgramInfos;
		public KeyGroup[] KeyGroups;
	}
	public class KeyGroup // Size 0x8
	{
		public ushort Id { get; set; }
		public byte Poly { get; set; }
		public byte Priority { get; set; }
		public byte LowNote { get; set; }
		public byte HighNote { get; set; }
		public ushort Unknown { get; set; }
	}
	public sealed class LFOInfo
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
	}

	public string Type; // "swdb" or "swdl"
	public byte[] Unknown1;
	public uint Length;
	public ushort Version;
	public IHeader Header;
	public byte[] Unknown2;

	public ProgramBank Programs;
	public SampleBlock[] Samples;

	public SWD(string path)
	{
		using (var stream = new MemoryStream(File.ReadAllBytes(path)))
		{
			var r = new EndianBinaryReader(stream, ascii: true);
			Type = r.ReadString_Count(4);
			Unknown1 = new byte[4];
			r.ReadBytes(Unknown1);
			Length = r.ReadUInt32();
			Version = r.ReadUInt16();
			Unknown2 = new byte[2];
			r.ReadBytes(Unknown2);
			switch (Version)
			{
				case 0x402:
				{
					Header_V402 header = r.ReadObject<Header_V402>();
					Header = header;
					Programs = ReadPrograms<ProgramInfo_V402>(r, header.NumPRGISlots);
					Samples = ReadSamples<WavInfo_V402>(r, header.NumWAVISlots);
					break;
				}
				case 0x415:
				{
					Header_V415 header = r.ReadObject<Header_V415>();
					Header = header;
					Programs = ReadPrograms<ProgramInfo_V415>(r, header.NumPRGISlots);
					if (header.PCMDLength != 0 && (header.PCMDLength & 0xFFFF0000) != 0xAAAA0000)
					{
						Samples = ReadSamples<WavInfo_V415>(r, header.NumWAVISlots);
					}
					break;
				}
				default: throw new InvalidDataException();
			}
		}
	}

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
					r.Stream.Align(4);
					break;
				}
			}
		}
		r.Stream.Position = oldPosition;
		return pos;
	}

	private static SampleBlock[] ReadSamples<T>(EndianBinaryReader r, int numWAVISlots) where T : IWavInfo, new()
	{
		long waviChunkOffset = FindChunk(r, "wavi");
		long pcmdChunkOffset = FindChunk(r, "pcmd");
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
				r.Stream.Position = waviChunkOffset + (2 * i);
				ushort offset = r.ReadUInt16();
				if (offset != 0)
				{
					r.Stream.Position = offset + waviChunkOffset;
					T wavInfo = r.ReadObject<T>();
					samples[i] = new SampleBlock
					{
						WavInfo = wavInfo,
						Data = new byte[(int)((wavInfo.LoopStart + wavInfo.LoopEnd) * 4)],
					};
					r.Stream.Position = pcmdChunkOffset + wavInfo.SampleOffset;
					r.ReadBytes(samples[i].Data);
				}
			}
			return samples;
		}
	}
	private static ProgramBank? ReadPrograms<T>(EndianBinaryReader r, int numPRGISlots) where T : IProgramInfo, new()
	{
		long chunkOffset = FindChunk(r, "prgi");
		if (chunkOffset == -1)
		{
			return null;
		}

		chunkOffset += 0x10;
		var programInfos = new IProgramInfo?[numPRGISlots];
		for (int i = 0; i < programInfos.Length; i++)
		{
			r.Stream.Position = chunkOffset + (2 * i);
			ushort offset = r.ReadUInt16();
			if (offset != 0)
			{
				r.Stream.Position = offset + chunkOffset;
				programInfos[i] = r.ReadObject<T>();
			}
		}
		return new ProgramBank
		{
			ProgramInfos = programInfos,
			KeyGroups = ReadKeyGroups(r),
		};
	}
	private static KeyGroup[] ReadKeyGroups(EndianBinaryReader r)
	{
		long chunkOffset = FindChunk(r, "kgrp");
		if (chunkOffset == -1)
		{
			return Array.Empty<KeyGroup>();
		}

		r.Stream.Position = chunkOffset + 0xC;
		uint chunkLength = r.ReadUInt32();
		var keyGroups = new KeyGroup[chunkLength / 8]; // 8 is the size of a KeyGroup
		for (int i = 0; i < keyGroups.Length; i++)
		{
			keyGroups[i] = r.ReadObject<KeyGroup>();
		}
		return keyGroups;
	}
}
