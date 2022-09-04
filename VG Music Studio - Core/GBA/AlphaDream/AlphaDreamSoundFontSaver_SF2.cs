using Kermalis.SoundFont2;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public static class AlphaDreamSoundFontSaver_SF2
{
	public static void Save(AlphaDreamConfig config, string path)
	{
		var sf2 = new SF2();
		AddInfo(config, sf2.InfoChunk);
		Dictionary<int, (SampleHeader, int)> sampleDict = AddSamples(config, sf2);
		AddInstruments(config, sf2, sampleDict);
		sf2.Save(path);
	}

	private static void AddInfo(AlphaDreamConfig config, InfoListChunk chunk)
	{
		chunk.Bank = config.Name;
		//chunk.Copyright = config.Creator;
		chunk.Tools = ConfigUtils.PROGRAM_NAME + " by Kermalis";
	}

	private static Dictionary<int, (SampleHeader, int)> AddSamples(AlphaDreamConfig config, SF2 sf2)
	{
		var sampleDict = new Dictionary<int, (SampleHeader, int)>((int)config.SampleTableSize);
		for (int i = 0; i < config.SampleTableSize; i++)
		{
			int ofs = BinaryPrimitives.ReadInt32LittleEndian(config.ROM.AsSpan(config.SampleTableOffset + (i * 4)));
			if (ofs == 0)
			{
				continue;
			}

			ofs += config.SampleTableOffset;
			ref SampleHeader sh = ref MemoryMarshal.AsRef<SampleHeader>(config.ROM.AsSpan(ofs));

			short[] pcm16 = SampleUtils.PCMU8ToPCM16(config.ROM.AsSpan(ofs + 0x10, sh.Length));
			int sf2Index = (int)sf2.AddSample(pcm16, $"Sample {i}", sh.DoesLoop == SampleHeader.LOOP_TRUE, (uint)sh.LoopOffset, (uint)sh.SampleRate >> 10, 60, 0);
			sampleDict.Add(i, (sh, sf2Index));
		}
		return sampleDict;
	}
	private static void AddInstruments(AlphaDreamConfig config, SF2 sf2, Dictionary<int, (SampleHeader, int)> sampleDict)
	{
		for (ushort v = 0; v < 256; v++)
		{
			short off = BinaryPrimitives.ReadInt16LittleEndian(config.ROM.AsSpan(config.VoiceTableOffset + (v * 2)));
			short nextOff = BinaryPrimitives.ReadInt16LittleEndian(config.ROM.AsSpan(config.VoiceTableOffset + ((v + 1) * 2)));
			int numEntries = (nextOff - off) / 8; // Each entry is 8 bytes
			if (numEntries == 0)
			{
				continue;
			}

			string name = "Instrument " + v;
			sf2.AddPreset(name, v, 0);
			sf2.AddPresetBag();
			sf2.AddPresetGenerator(SF2Generator.Instrument, new SF2GeneratorAmount { Amount = (short)sf2.AddInstrument(name) });
			for (int e = 0; e < numEntries; e++)
			{
				ref VoiceEntry entry = ref MemoryMarshal.AsRef<VoiceEntry>(config.ROM.AsSpan(config.VoiceTableOffset + off + (e * 8)));
				sf2.AddInstrumentBag();
				// Key range
				if (!(entry.MinKey == 0 && entry.MaxKey == 0x7F))
				{
					sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = entry.MinKey, HighByte = entry.MaxKey });
				}
				// Fixed frequency
				if (entry.IsFixedFrequency == VoiceEntry.FIXED_FREQ_TRUE)
				{
					sf2.AddInstrumentGenerator(SF2Generator.ScaleTuning, new SF2GeneratorAmount { Amount = 0 });
				}
				// Sample
				if (entry.Sample < config.SampleTableSize)
				{
					if (!sampleDict.TryGetValue(entry.Sample, out (SampleHeader, int) value))
					{
						Debug.WriteLine(string.Format("Voice {0} uses a null sample id ({1})", v, entry.Sample));
					}
					else
					{
						sf2.AddInstrumentGenerator(SF2Generator.SampleModes, new SF2GeneratorAmount { Amount = (short)(value.Item1.DoesLoop == SampleHeader.LOOP_TRUE ? 1 : 0), });
						sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { UAmount = (ushort)value.Item2, });
					}
				}
				else
				{
					Debug.WriteLine(string.Format("Voice {0} uses an invalid sample id ({1})", v, entry.Sample));
				}
			}
		}
	}
}
