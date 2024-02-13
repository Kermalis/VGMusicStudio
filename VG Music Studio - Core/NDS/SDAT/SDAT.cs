using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDAT
{
	public sealed class SYMB
	{
		public sealed class Record
		{
			public int NumEntries;
			public int[] EntryOffsets;

			public string?[] Entries;

			public Record(EndianBinaryReader er, long baseOffset)
			{
				NumEntries = er.ReadInt32();
				EntryOffsets = new int[NumEntries];
				er.ReadInt32s(EntryOffsets);

				long p = er.Stream.Position;
				Entries = new string[NumEntries];
				for (int i = 0; i < NumEntries; i++)
				{
					if (EntryOffsets[i] != 0)
					{
						er.Stream.Position = baseOffset + EntryOffsets[i];
						Entries[i] = er.ReadString_NullTerminated();
					}
				}
				er.Stream.Position = p;
			}
		}

		public string BlockType; // "SYMB"
		public int BlockSize;
		public int[] RecordOffsets;
		public byte[] Padding;

		public Record SequenceSymbols;
		//SequenceArchiveSymbols;
		public Record BankSymbols;
		public Record WaveArchiveSymbols;
		//PlayerSymbols;
		//GroupSymbols;
		//StreamPlayerSymbols;
		//StreamSymbols;

		public SYMB(EndianBinaryReader er, long baseOffset)
		{
			er.Stream.Position = baseOffset;

			BlockType = er.ReadString_Count(4);
			BlockSize = er.ReadInt32();
			RecordOffsets = new int[8];
			er.ReadInt32s(RecordOffsets);
			Padding = new byte[24];
			er.ReadBytes(Padding);

			er.Stream.Position = baseOffset + RecordOffsets[0];
			SequenceSymbols = new Record(er, baseOffset);

			er.Stream.Position = baseOffset + RecordOffsets[2];
			BankSymbols = new Record(er, baseOffset);

			er.Stream.Position = baseOffset + RecordOffsets[3];
			WaveArchiveSymbols = new Record(er, baseOffset);
		}
	}

	public sealed class INFO
	{
		public sealed class Record<T> where T : new()
		{
			public int NumEntries;
			public int[] EntryOffsets;

			public T?[] Entries;

			public Record(EndianBinaryReader er, long baseOffset)
			{
				NumEntries = er.ReadInt32();
				EntryOffsets = new int[NumEntries];
				er.ReadInt32s(EntryOffsets);

				long p = er.Stream.Position;
				Entries = new T?[NumEntries];
				for (int i = 0; i < NumEntries; i++)
				{
					if (EntryOffsets[i] != 0)
					{
						er.Stream.Position = baseOffset + EntryOffsets[i];
						Entries[i] = er.ReadObject<T>();
					}
				}
				er.Stream.Position = p;
			}
		}

		public sealed class SequenceInfo
		{
			public ushort FileId { get; set; }
			public byte Unknown1 { get; set; }
			public byte Unknown2 { get; set; }
			public ushort Bank { get; set; }
			public byte Volume { get; set; }
			public byte ChannelPriority { get; set; }
			public byte PlayerPriority { get; set; }
			public byte PlayerNum { get; set; }
			public byte Unknown3 { get; set; }
			public byte Unknown4 { get; set; }

			internal SSEQ GetSSEQ(SDAT sdat)
			{
				return new SSEQ(sdat.FATBlock.Entries[FileId].Data);
			}
			internal SBNK GetSBNK(SDAT sdat)
			{
				BankInfo bankInfo = sdat.INFOBlock.BankInfos.Entries[Bank]!;
				var sbnk = new SBNK(sdat.FATBlock.Entries[bankInfo.FileId].Data);
				for (int i = 0; i < 4; i++)
				{
					if (bankInfo.SWARs[i] != 0xFFFF)
					{
						sbnk.SWARs[i] = new SWAR(sdat.FATBlock.Entries[sdat.INFOBlock.WaveArchiveInfos.Entries[bankInfo.SWARs[i]]!.FileId].Data);
					}
				}
				return sbnk;
			}
		}
		public sealed class BankInfo
		{
			public ushort FileId { get; set; }
			public byte Unknown1 { get; set; }
			public byte Unknown2 { get; set; }
			[BinaryArrayFixedLength(4)]
			public ushort[] SWARs { get; set; } = null!;
		}
		public sealed class WaveArchiveInfo
		{
			public ushort FileId { get; set; }
			public byte Unknown1 { get; set; }
			public byte Unknown2 { get; set; }
		}

		public string BlockType; // "INFO"
		public int BlockSize;
		public int[] InfoOffsets;
		public byte[] Padding;

		public Record<SequenceInfo> SequenceInfos;
		//SequenceArchiveInfos;
		public Record<BankInfo> BankInfos;
		public Record<WaveArchiveInfo> WaveArchiveInfos;
		//PlayerInfos;
		//GroupInfos;
		//StreamPlayerInfos;
		//StreamInfos;

		public INFO(EndianBinaryReader er, long baseOffset)
		{
			er.Stream.Position = baseOffset;

			BlockType = er.ReadString_Count(4);
			BlockSize = er.ReadInt32();
			InfoOffsets = new int[8];
			er.ReadInt32s(InfoOffsets);
			Padding = new byte[24];
			er.ReadBytes(Padding);

			er.Stream.Position = baseOffset + InfoOffsets[0];
			SequenceInfos = new Record<SequenceInfo>(er, baseOffset);

			er.Stream.Position = baseOffset + InfoOffsets[2];
			BankInfos = new Record<BankInfo>(er, baseOffset);

			er.Stream.Position = baseOffset + InfoOffsets[3];
			WaveArchiveInfos = new Record<WaveArchiveInfo>(er, baseOffset);
		}
	}

	public sealed class FAT
	{
		public sealed class FATEntry
		{
			public int DataOffset;
			public int DataLength;
			public byte[] Padding;

			public byte[] Data;

			public FATEntry(EndianBinaryReader er)
			{
				DataOffset = er.ReadInt32();
				DataLength = er.ReadInt32();
				Padding = new byte[8];
				er.ReadBytes(Padding);

				long p = er.Stream.Position;
				Data = new byte[DataLength];
				er.Stream.Position = DataOffset;
				er.ReadBytes(Data);
				er.Stream.Position = p;
			}
		}

		public string BlockType; // "FAT "
		public int BlockSize;
		public int NumEntries;
		public FATEntry[] Entries;

		public FAT(EndianBinaryReader er)
		{
			BlockType = er.ReadString_Count(4);
			BlockSize = er.ReadInt32();
			NumEntries = er.ReadInt32();
			Entries = new FATEntry[NumEntries];
			for (int i = 0; i < Entries.Length; i++)
			{
				Entries[i] = new FATEntry(er);
			}
		}
	}

	public SDATFileHeader FileHeader; // "SDAT"
	public int SYMBOffset;
	public int SYMBLength;
	public int INFOOffset;
	public int INFOLength;
	public int FATOffset;
	public int FATLength;
	public int FILEOffset;
	public int FILELength;
	public byte[] Padding;

	public SYMB? SYMBBlock;
	public INFO INFOBlock;
	public FAT FATBlock;
	//FILEBlock

	public SDAT(Stream stream)
	{
		var er = new EndianBinaryReader(stream, ascii: true);
		FileHeader = new SDATFileHeader(er);
		SYMBOffset = er.ReadInt32();
		SYMBLength = er.ReadInt32();
		INFOOffset = er.ReadInt32();
		INFOLength = er.ReadInt32();
		FATOffset = er.ReadInt32();
		FATLength = er.ReadInt32();
		FILEOffset = er.ReadInt32();
		FILELength = er.ReadInt32();
		Padding = new byte[16];
		er.ReadBytes(Padding);

		if (SYMBOffset != 0 && SYMBLength != 0)
		{
			SYMBBlock = new SYMB(er, SYMBOffset);
		}
		INFOBlock = new INFO(er, INFOOffset);
		stream.Position = FATOffset;
		FATBlock = new FAT(er);
	}
}
