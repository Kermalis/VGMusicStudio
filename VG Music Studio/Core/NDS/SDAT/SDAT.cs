using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class SDAT
    {
        public class SYMB : IBinarySerializable
        {
            public class Record
            {
                public int NumEntries;
                public int[] EntryOffsets;

                public string[] Entries;

                public Record(EndianBinaryReader er, long baseOffset)
                {
                    NumEntries = er.ReadInt32();
                    EntryOffsets = er.ReadInt32s(NumEntries);

                    long p = er.BaseStream.Position;
                    Entries = new string[NumEntries];
                    for (int i = 0; i < NumEntries; i++)
                    {
                        if (EntryOffsets[i] != 0)
                        {
                            Entries[i] = er.ReadStringNullTerminated(baseOffset + EntryOffsets[i]);
                        }
                    }
                    er.BaseStream.Position = p;
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

            public void Read(EndianBinaryReader er)
            {
                long baseOffset = er.BaseStream.Position;
                BlockType = er.ReadString(4);
                BlockSize = er.ReadInt32();
                RecordOffsets = er.ReadInt32s(8);
                Padding = er.ReadBytes(24);
                er.BaseStream.Position = baseOffset + RecordOffsets[0];
                SequenceSymbols = new Record(er, baseOffset);
                er.BaseStream.Position = baseOffset + RecordOffsets[2];
                BankSymbols = new Record(er, baseOffset);
                er.BaseStream.Position = baseOffset + RecordOffsets[3];
                WaveArchiveSymbols = new Record(er, baseOffset);
                er.BaseStream.Position = baseOffset + BlockSize;
            }
            public void Write(EndianBinaryWriter ew)
            {
                throw new NotImplementedException();
            }
        }

        public class INFO : IBinarySerializable
        {
            public class Record<T> where T : new()
            {
                public int NumEntries;
                public int[] EntryOffsets;

                public T[] Entries;

                public Record(EndianBinaryReader er, long baseOffset)
                {
                    NumEntries = er.ReadInt32();
                    EntryOffsets = er.ReadInt32s(NumEntries);

                    long p = er.BaseStream.Position;
                    Entries = new T[NumEntries];
                    for (int i = 0; i < NumEntries; i++)
                    {
                        if (EntryOffsets[i] != 0)
                        {
                            Entries[i] = er.ReadObject<T>(baseOffset + EntryOffsets[i]);
                        }
                    }
                    er.BaseStream.Position = p;
                }
            }

            public class SequenceInfo
            {
                public ushort FileId { get; set; }
                [BinaryArrayFixedLength(2)]
                public byte[] Unknown1 { get; set; }
                public ushort Bank { get; set; }
                public byte Volume { get; set; }
                public byte ChannelPriority { get; set; }
                public byte PlayerPriority { get; set; }
                public byte PlayerNum { get; set; }
                [BinaryArrayFixedLength(2)]
                public byte[] Unknown2 { get; set; }
            }
            public class BankInfo
            {
                public ushort FileId { get; set; }
                [BinaryArrayFixedLength(2)]
                public byte[] Unknown { get; set; }
                [BinaryArrayFixedLength(4)]
                public ushort[] SWARs { get; set; }
            }
            public class WaveArchiveInfo
            {
                public ushort FileId { get; set; }
                [BinaryArrayFixedLength(2)]
                public byte[] Unknown { get; set; }
            }

            public string BlockType; // "INFO"
            public int BlockSize;
            public int[] InfoOffsets;

            public Record<SequenceInfo> SequenceInfos;
            //SequenceArchiveInfos;
            public Record<BankInfo> BankInfos;
            public Record<WaveArchiveInfo> WaveArchiveInfos;
            //PlayerInfos;
            //GroupInfos;
            //StreamPlayerInfos;
            //StreamInfos;

            public void Read(EndianBinaryReader er)
            {
                long baseOffset = er.BaseStream.Position;
                BlockType = er.ReadString(4);
                BlockSize = er.ReadInt32();
                InfoOffsets = er.ReadInt32s(8);
                er.ReadBytes(24);
                er.BaseStream.Position = baseOffset + InfoOffsets[0];
                SequenceInfos = new Record<SequenceInfo>(er, baseOffset);
                er.BaseStream.Position = baseOffset + InfoOffsets[2];
                BankInfos = new Record<BankInfo>(er, baseOffset);
                er.BaseStream.Position = baseOffset + InfoOffsets[3];
                WaveArchiveInfos = new Record<WaveArchiveInfo>(er, baseOffset);
                er.BaseStream.Position = baseOffset;
            }
            public void Write(EndianBinaryWriter ew)
            {
                throw new NotImplementedException();
            }
        }

        public class FAT
        {
            public class FATEntry : IBinarySerializable
            {
                public int DataOffset;
                public int DataLength;
                public byte[] Padding;

                public byte[] Data;

                public void Read(EndianBinaryReader er)
                {
                    DataOffset = er.ReadInt32();
                    DataLength = er.ReadInt32();
                    Padding = er.ReadBytes(8);

                    long p = er.BaseStream.Position;
                    Data = er.ReadBytes(DataLength, DataOffset);
                    er.BaseStream.Position = p;
                }
                public void Write(EndianBinaryWriter ew)
                {
                    throw new NotImplementedException();
                }
            }

            [BinaryStringFixedLength(4)]
            public string BlockType { get; set; } // "FAT "
            public int BlockSize { get; set; }
            public int NumEntries { get; set; }
            [BinaryArrayVariableLength(nameof(NumEntries))]
            public FATEntry[] Entries { get; set; }
        }

        public FileHeader FileHeader; // "SDAT"
        public int SYMBOffset;
        public int SYMBLength;
        public int INFOOffset;
        public int INFOLength;
        public int FATOffset;
        public int FATLength;
        public int FILEOffset;
        public int FILELength;
        public byte[] Padding;

        public SYMB SYMBBlock;
        public INFO INFOBlock;
        public FAT FATBlock;
        //FILEBlock

        public SDAT(byte[] bytes)
        {
            using (var er = new EndianBinaryReader(new MemoryStream(bytes)))
            {
                FileHeader = er.ReadObject<FileHeader>();
                SYMBOffset = er.ReadInt32();
                SYMBLength = er.ReadInt32();
                INFOOffset = er.ReadInt32();
                INFOLength = er.ReadInt32();
                FATOffset = er.ReadInt32();
                FATLength = er.ReadInt32();
                FILEOffset = er.ReadInt32();
                FILELength = er.ReadInt32();
                Padding = er.ReadBytes(16);

                if (SYMBOffset != 0 && SYMBLength != 0)
                {
                    SYMBBlock = er.ReadObject<SYMB>(SYMBOffset);
                }
                INFOBlock = er.ReadObject<INFO>(INFOOffset);
                FATBlock = er.ReadObject<FAT>(FATOffset);
            }
        }
    }
}
