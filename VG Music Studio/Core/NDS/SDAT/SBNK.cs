using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class SBNK
    {
        public class InstrumentData
        {
            public class DataParam
            {
                [BinaryArrayFixedLength(2)]
                public ushort[] Info { get; set; }
                public byte BaseKey { get; set; }
                public byte Attack { get; set; }
                public byte Decay { get; set; }
                public byte Sustain { get; set; }
                public byte Release { get; set; }
                public byte Pan { get; set; }
            }

            public InstrumentType Type { get; set; }
            public byte Padding { get; set; }
            public DataParam Param { get; set; }
        }
        public class Instrument : IBinarySerializable
        {
            public class DefaultData
            {
                public InstrumentData.DataParam Param { get; set; }
            }
            public class DrumSetData : IBinarySerializable
            {
                public byte MinNote;
                public byte MaxNote;
                public InstrumentData[] SubInstruments;

                public void Read(EndianBinaryReader er)
                {
                    MinNote = er.ReadByte();
                    MaxNote = er.ReadByte();
                    SubInstruments = new InstrumentData[MaxNote - MinNote + 1];
                    for (int i = 0; i < SubInstruments.Length; i++)
                    {
                        SubInstruments[i] = er.ReadObject<InstrumentData>();
                    }
                }
                public void Write(EndianBinaryWriter ew)
                {
                    throw new NotImplementedException();
                }
            }
            public class KeySplitData : IBinarySerializable
            {
                public byte[] KeyRegions;
                public InstrumentData[] SubInstruments;

                public void Read(EndianBinaryReader er)
                {
                    KeyRegions = er.ReadBytes(8);
                    int numSubInstruments = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (KeyRegions[i] == 0)
                        {
                            break;
                        }
                        numSubInstruments++;
                    }
                    SubInstruments = new InstrumentData[numSubInstruments];
                    for (int i = 0; i < numSubInstruments; i++)
                    {
                        SubInstruments[i] = er.ReadObject<InstrumentData>();
                    }
                }
                public void Write(EndianBinaryWriter ew)
                {
                    throw new NotImplementedException();
                }
            }

            public InstrumentType Type;
            public ushort DataOffset;
            public byte Padding;

            public object Data;

            public void Read(EndianBinaryReader er)
            {
                Type = (InstrumentType)er.ReadByte();
                DataOffset = er.ReadUInt16();
                Padding = er.ReadByte();

                long p = er.BaseStream.Position;
                switch (Type)
                {
                    case InstrumentType.PCM:
                    case InstrumentType.PSG:
                    case InstrumentType.Noise: Data = er.ReadObject<DefaultData>(DataOffset); break;
                    case InstrumentType.Drum: Data = er.ReadObject<DrumSetData>(DataOffset); break;
                    case InstrumentType.KeySplit: Data = er.ReadObject<KeySplitData>(DataOffset); break;
                    default: break;
                }
                er.BaseStream.Position = p;
            }
            public void Write(EndianBinaryWriter ew)
            {
                throw new NotImplementedException();
            }
        }

        public FileHeader FileHeader { get; set; } // "SBNK"
        [BinaryStringFixedLength(4)]
        public string BlockType { get; set; } // "DATA"
        public int BlockSize { get; set; }
        [BinaryArrayFixedLength(32)]
        public byte[] Padding { get; set; }
        public int NumInstruments { get; set; }
        [BinaryArrayVariableLength(nameof(NumInstruments))]
        public Instrument[] Instruments { get; set; }

        [BinaryIgnore]
        public SWAR[] SWARs { get; } = new SWAR[4];

        public SBNK(byte[] bytes)
        {
            using (var er = new EndianBinaryReader(new MemoryStream(bytes)))
            {
                er.ReadIntoObject(this);
            }
        }

        public InstrumentData GetInstrumentData(int voice, int key)
        {
            if (voice >= NumInstruments)
            {
                return null;
            }
            else
            {
                switch (Instruments[voice].Type)
                {
                    case InstrumentType.PCM:
                    case InstrumentType.PSG:
                    case InstrumentType.Noise:
                    {
                        var d = (Instrument.DefaultData)Instruments[voice].Data;
                        // TODO: Better way?
                        return new InstrumentData
                        {
                            Type = Instruments[voice].Type,
                            Param = d.Param
                        };
                    }
                    case InstrumentType.Drum:
                    {
                        var d = (Instrument.DrumSetData)Instruments[voice].Data;
                        return key < d.MinNote || key > d.MaxNote ? null : d.SubInstruments[key - d.MinNote];
                    }
                    case InstrumentType.KeySplit:
                    {
                        var d = (Instrument.KeySplitData)Instruments[voice].Data;
                        for (int i = 0; i < 8; i++)
                        {
                            if (key <= d.KeyRegions[i])
                            {
                                return d.SubInstruments[i];
                            }
                        }
                        return null;
                    }
                    default: return null;
                }
            }
        }

        public SWAR.SWAV GetSWAV(int swarIndex, int swavIndex)
        {
            SWAR swar = SWARs[swarIndex];
            return swar == null || swavIndex >= swar.NumWaves ? null : swar.Waves[swavIndex];
        }
    }
}
