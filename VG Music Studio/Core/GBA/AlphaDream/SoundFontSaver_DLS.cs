using Kermalis.DLS2;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream
{
    internal sealed class SoundFontSaver_DLS
    {
        // Since every key will use the same articulation data, just store one instance
        private static readonly Level2ArticulatorChunk _art2 = new Level2ArticulatorChunk
        {
            new Level2ArticulatorConnectionBlock { Destination = Level2ArticulatorDestination.LFOFrequency, Scale = 2786 },
            new Level2ArticulatorConnectionBlock { Destination = Level2ArticulatorDestination.VIBFrequency, Scale = 2786 },
            new Level2ArticulatorConnectionBlock { Source = Level2ArticulatorSource.KeyNumber, Destination = Level2ArticulatorDestination.Pitch },
            new Level2ArticulatorConnectionBlock { Source = Level2ArticulatorSource.Vibrato, Control = Level2ArticulatorSource.Modulation_CC1, Destination = Level2ArticulatorDestination.Pitch, BipolarSource = true, Scale = 0x320000 },
            new Level2ArticulatorConnectionBlock { Source = Level2ArticulatorSource.Vibrato, Control = Level2ArticulatorSource.ChannelPressure, Destination = Level2ArticulatorDestination.Pitch, BipolarSource = true, Scale = 0x320000 },
            new Level2ArticulatorConnectionBlock { Source = Level2ArticulatorSource.Pan_CC10, Destination = Level2ArticulatorDestination.Pan, BipolarSource = true, Scale = 0xFE0000 },
            new Level2ArticulatorConnectionBlock { Source = Level2ArticulatorSource.ChorusSend_CC91, Destination = Level2ArticulatorDestination.Reverb, Scale = 0xC80000 },
            new Level2ArticulatorConnectionBlock { Source = Level2ArticulatorSource.Reverb_SendCC93, Destination = Level2ArticulatorDestination.Chorus, Scale = 0xC80000 }
        };

        public static void Save(Config config, string path)
        {
            var dls = new DLS();
            AddInfo(config, dls);
            Dictionary<int, (WaveSampleChunk, int)> sampleDict = AddSamples(config, dls);
            AddInstruments(config, dls, sampleDict);
            dls.Save(path);
        }

        private static void AddInfo(Config config, DLS dls)
        {
            var info = new ListChunk("INFO");
            dls.Add(info);
            info.Add(new InfoSubChunk("INAM", config.Name));
            //info.Add(new InfoSubChunk("ICOP", config.Creator));
            info.Add(new InfoSubChunk("IENG", "Kermalis"));
            info.Add(new InfoSubChunk("ISFT", Util.Utils.ProgramName));
        }

        private static Dictionary<int, (WaveSampleChunk, int)> AddSamples(Config config, DLS dls)
        {
            ListChunk waves = dls.WavePool;
            var sampleDict = new Dictionary<int, (WaveSampleChunk, int)>((int)config.SampleTableSize);
            for (int i = 0; i < config.SampleTableSize; i++)
            {
                int ofs = config.Reader.ReadInt32(config.SampleTableOffset + (i * 4));
                if (ofs == 0)
                {
                    continue; // Skip null samples
                }

                ofs += config.SampleTableOffset;
                SampleHeader sh = config.Reader.ReadObject<SampleHeader>(ofs);

                // Create format chunk
                var fmt = new FormatChunk(WaveFormat.PCM);
                fmt.WaveInfo.Channels = 1;
                fmt.WaveInfo.SamplesPerSec = (uint)(sh.SampleRate >> 10);
                fmt.WaveInfo.AvgBytesPerSec = fmt.WaveInfo.SamplesPerSec;
                fmt.WaveInfo.BlockAlign = 1;
                fmt.FormatInfo.BitsPerSample = 8;
                // Create wave sample chunk and add loop if there is one
                var wsmp = new WaveSampleChunk
                {
                    UnityNote = 60,
                    Options = WaveSampleOptions.NoTruncation | WaveSampleOptions.NoCompression
                };
                if (sh.DoesLoop == 0x40000000)
                {
                    wsmp.Loop = new WaveSampleLoop
                    {
                        LoopStart = (uint)sh.LoopOffset,
                        LoopLength = (uint)(sh.Length - sh.LoopOffset),
                        LoopType = LoopType.Forward
                    };
                }
                // Get PCM sample
                byte[] pcm = new byte[sh.Length];
                Array.Copy(config.ROM, ofs + 0x10, pcm, 0, sh.Length);

                // Add
                int dlsIndex = waves.Count;
                waves.Add(new ListChunk("wave")
                {
                    fmt,
                    wsmp,
                    new DataChunk(pcm),
                    new ListChunk("INFO")
                    {
                        new InfoSubChunk("INAM", $"Sample {i}")
                    }
                });
                sampleDict.Add(i, (wsmp, dlsIndex));
            }
            return sampleDict;
        }

        private static void AddInstruments(Config config, DLS dls, Dictionary<int, (WaveSampleChunk, int)> sampleDict)
        {
            ListChunk lins = dls.InstrumentList;
            for (int v = 0; v < 256; v++)
            {
                short off = config.Reader.ReadInt16(config.VoiceTableOffset + (v * 2));
                short nextOff = config.Reader.ReadInt16(config.VoiceTableOffset + ((v + 1) * 2));
                int numEntries = (nextOff - off) / 8; // Each entry is 8 bytes
                if (numEntries == 0)
                {
                    continue; // Skip empty entries
                }

                var ins = new ListChunk("ins ");
                ins.Add(new InstrumentHeaderChunk
                {
                    NumRegions = (uint)numEntries,
                    Locale = new MIDILocale(0, (byte)(v / 128), false, (byte)(v % 128))
                });
                var lrgn = new ListChunk("lrgn");
                ins.Add(lrgn);
                ins.Add(new ListChunk("INFO")
                {
                    new InfoSubChunk("INAM", $"Instrument {v}")
                });
                lins.Add(ins);
                for (int e = 0; e < numEntries; e++)
                {
                    VoiceEntry entry = config.Reader.ReadObject<VoiceEntry>(config.VoiceTableOffset + off + (e * 8));
                    // Sample
                    if (entry.Sample >= config.SampleTableSize)
                    {
                        Debug.WriteLine(string.Format("Voice {0} uses an invalid sample id ({1})", v, entry.Sample));
                        continue;
                    }
                    if (!sampleDict.TryGetValue(entry.Sample, out (WaveSampleChunk, int) value))
                    {
                        Debug.WriteLine(string.Format("Voice {0} uses a null sample id ({1})", v, entry.Sample));
                        continue;
                    }
                    void Add(ushort low, ushort high, ushort baseKey)
                    {
                        var rgnh = new RegionHeaderChunk();
                        rgnh.KeyRange.Low = low;
                        rgnh.KeyRange.High = high;
                        lrgn.Add(new ListChunk("rgn2")
                        {
                            rgnh,
                            new WaveSampleChunk
                            {
                                UnityNote = baseKey,
                                Options = WaveSampleOptions.NoTruncation | WaveSampleOptions.NoCompression,
                                Loop = value.Item1.Loop
                            },
                            new WaveLinkChunk
                            {
                                Channels = WaveLinkChannels.Left,
                                TableIndex = (uint)value.Item2
                            },
                            new ListChunk("lar2")
                            {
                                _art2
                            }
                        });
                    }
                    // Fixed frequency - Since DLS does not support it, we need to manually add every key with its own base note
                    if (entry.IsFixedFrequency == 0x80)
                    {
                        for (ushort i = entry.MinKey; i <= entry.MaxKey; i++)
                        {
                            Add(i, i, i);
                        }
                    }
                    else
                    {
                        Add(entry.MinKey, entry.MaxKey, 60);
                    }
                }
            }
        }
    }
}