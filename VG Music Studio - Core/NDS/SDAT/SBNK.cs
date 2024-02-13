using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal sealed class SBNK
{
	public sealed class InstrumentData
	{
		public sealed class DataParam
		{
			public ushort[] Info;
			public byte BaseNote;
			public byte Attack;
			public byte Decay;
			public byte Sustain;
			public byte Release;
			public byte Pan;

			public DataParam(EndianBinaryReader er)
			{
				Info = new ushort[2];
				er.ReadUInt16s(Info);
				BaseNote = er.ReadByte();
				Attack = er.ReadByte();
				Decay = er.ReadByte();
				Sustain = er.ReadByte();
				Release = er.ReadByte();
				Pan = er.ReadByte();
			}
		}

		public InstrumentType Type;
		public byte Padding;
		public DataParam Param;

		public InstrumentData(InstrumentType type, DataParam param)
		{
			Type = type;
			Param = param;
		}
		public InstrumentData(EndianBinaryReader er)
		{
			Type = er.ReadEnum<InstrumentType>();
			Padding = er.ReadByte();
			Param = new DataParam(er);
		}
	}
	public sealed class Instrument
	{
		public sealed class DrumSetData
		{
			public byte MinNote;
			public byte MaxNote;
			public InstrumentData[] SubInstruments;

			public DrumSetData(EndianBinaryReader er)
			{
				MinNote = er.ReadByte();
				MaxNote = er.ReadByte();
				SubInstruments = new InstrumentData[MaxNote - MinNote + 1];
				for (int i = 0; i < SubInstruments.Length; i++)
				{
					SubInstruments[i] = new InstrumentData(er);
				}
			}
		}
		public sealed class KeySplitData
		{
			public byte[] KeyRegions;
			public InstrumentData[] SubInstruments;

			public KeySplitData(EndianBinaryReader er)
			{
				KeyRegions = new byte[8];
				er.ReadBytes(KeyRegions);

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
					SubInstruments[i] = new InstrumentData(er);
				}
			}
		}

		public InstrumentType Type;
		public ushort DataOffset;
		public byte Padding;

		public object? Data;

		public Instrument(EndianBinaryReader er)
		{
			Type = er.ReadEnum<InstrumentType>();
			DataOffset = er.ReadUInt16();
			Padding = er.ReadByte();

			long p = er.Stream.Position;
			switch (Type)
			{
				case InstrumentType.PCM:
				case InstrumentType.PSG:
				case InstrumentType.Noise: er.Stream.Position = DataOffset; Data = new InstrumentData.DataParam(er); break;
				case InstrumentType.Drum: er.Stream.Position = DataOffset; Data = new DrumSetData(er); break;
				case InstrumentType.KeySplit: er.Stream.Position = DataOffset; Data = new KeySplitData(er); break;
				default: break;
			}
			er.Stream.Position = p;
		}
	}

	public SDATFileHeader FileHeader; // "SBNK"
	public string BlockType; // "DATA"
	public int BlockSize;
	public byte[] Padding;
	public int NumInstruments;
	public Instrument[] Instruments;

	public SWAR[] SWARs { get; }

	public SBNK(byte[] bytes)
	{
		using (var stream = new MemoryStream(bytes))
		{
			var er = new EndianBinaryReader(stream, ascii: true);
			FileHeader = new SDATFileHeader(er);
			BlockType = er.ReadString_Count(4);
			BlockSize = er.ReadInt32();
			Padding = new byte[32];
			er.ReadBytes(Padding);
			NumInstruments = er.ReadInt32();
			Instruments = new Instrument[NumInstruments];
			for (int i = 0; i < Instruments.Length; i++)
			{
				Instruments[i] = new Instrument(er);
			}
		}

		SWARs = new SWAR[4];
	}

	public InstrumentData? GetInstrumentData(int voice, int note)
	{
		if (voice >= NumInstruments)
		{
			return null;
		}

		switch (Instruments[voice].Type)
		{
			case InstrumentType.PCM:
			case InstrumentType.PSG:
			case InstrumentType.Noise:
			{
				var d = (InstrumentData.DataParam)Instruments[voice].Data!;
				// TODO: Better way?
				return new InstrumentData(Instruments[voice].Type, d);
			}
			case InstrumentType.Drum:
			{
				var d = (Instrument.DrumSetData)Instruments[voice].Data!;
				return note < d.MinNote || note > d.MaxNote ? null : d.SubInstruments[note - d.MinNote];
			}
			case InstrumentType.KeySplit:
			{
				var d = (Instrument.KeySplitData)Instruments[voice].Data!;
				for (int i = 0; i < 8; i++)
				{
					if (note <= d.KeyRegions[i])
					{
						return d.SubInstruments[i];
					}
				}
				return null;
			}
			default: return null;
		}
	}

	public SWAR.SWAV? GetSWAV(int swarIndex, int swavIndex)
	{
		SWAR swar = SWARs[swarIndex];
		return swar is null || swavIndex >= swar.NumWaves ? null : swar.Waves[swavIndex];
	}
}
