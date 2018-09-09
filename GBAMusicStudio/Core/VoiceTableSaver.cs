using Kermalis.SoundFont2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GBAMusicStudio.Core
{
    class VoiceTableSaver
    {
        /*static readonly string[] instrumentNames = {
            "Acoustic Grand Piano", "Bright Acoustic Piano", "Electric Grand Piano", "Honky-tonk Piano", "Rhodes Piano", "Chorused Piano",
            "Harpsichord", "Clavinet", "Celesta", "Glockenspiel", "Music Box", "Vibraphone", "Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
            "Hammond Organ", "Percussive Organ", "Rock Organ", "Church Organ", "Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
            "Acoustic Guitar (Nylon)", "Acoustic Guitar (Steel)", "Electric Guitar (Jazz)", "Electric Guitar (Clean)", "Electric Guitar (Muted)",
            "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics", "Acoustic Bass", "Electric Bass (Finger)", "Electric Bass (Pick)",
            "Fretless Bass", "Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2", "Violin", "Viola", "Cello", "Contrabass",
            "Tremelo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani", "String Ensemble 1", "String Ensemble 2", "SynthStrings 1",
            "SynthStrings 2", "Choir Aahs", "Voice Oohs", "Synth Voice", "Orchestra Hit", "Trumpet", "Trombone", "Tuba", "Muted Trumpet",
            "French Horn", "Brass Section", "Synth Brass 1", "Synth Brass 2", "Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax",
            "Oboe", "English Horn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "Pan Flute", "Bottle Blow", "Shakuhachi", "Whistle",
            "Ocarina", "Lead 1 (Square)", "Lead 2 (Sawtooth)", "Lead 3 (Calliope Lead)", "Lead 4 (Chiff Lead)", "Lead 5 (Charang)",
            "Lead 6 (Voice)", "Lead 7 (Fifths)", "Lead 8 (Bass + Lead)", "Pad 1 (New Age)", "Pad 2 (Warm)", "Pad 3 (Polysynth)", "Pad 4 (Choir)",
            "Pad 5 (Bowed)", "Pad 6 (Metallic)", "Pad 7 (Halo)", "Pad 8 (Sweep)", "FX 1 (Rain)", "FX 2 (Soundtrack)", "FX 3 (Crystal)",
            "FX 4 (Atmosphere)", "FX 5 (Brightness)", "FX 6 (Goblins)", "FX 7 (Echoes)", "FX 8 (Sci-Fi)", "Sitar", "Banjo", "Shamisen", "Koto",
            "Kalimba", "Bagpipe", "Fiddle", "Shanai", "Tinkle Bell", "Agogo", "Steel Drums", "Woodblock", "Taiko Drum", "Melodic Tom",
            "Synth Drum", "Reverse Cymbal", "Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet", "Telephone Ring", "Helicopter",
            "Applause", "Gunshot" };*/

        public static void Save(string fileName)
        {
            switch (ROM.Instance.Game.Engine.Type)
            {
                case EngineType.M4A: new M4AVoiceTableSaver(fileName, false); return;
                case EngineType.MLSS: new MLSSVoiceTableSaver(fileName); return;
                default: throw new PlatformNotSupportedException("Exporting to SF2 from this game engine is not supported at this time.");
            }
        }
        static void AddInfo(SF2 sf2)
        {
            sf2.InfoChunk.Bank = ROM.Instance.Game.Name;
            sf2.InfoChunk.Copyright = ROM.Instance.Game.Creator;
            sf2.InfoChunk.Tools = "GBA Music Studio by Kermalis";
        }

        static short[] PCM8ToPCM16(byte[] pcm8) => pcm8.Select(i => (short)(i << 8)).ToArray();
        static short[] PCMU8ToPCM16(byte[] pcm8) => pcm8.Select(i => (short)((i - 0x80) << 8)).ToArray();
        static short[] FloatToPCM16(float[] ieee) => ieee.Select(i => (short)(i * short.MaxValue)).ToArray();
        static short[] BitArrayToPCM16(BitArray bitArray)
        {
            short[] ret = new short[bitArray.Length];
            for (int i = 0; i < bitArray.Length; i++)
                ret[i] = (short)((bitArray[i] ? short.MaxValue : short.MinValue) / 2);
            return ret;
        }

        private class M4AVoiceTableSaver
        {
            SF2 sf2;

            public M4AVoiceTableSaver(string fileName, bool saveAfter7F)
            {
                sf2 = new SF2();
                AddInfo(sf2);

                AddSquaresAndNoises();
                AddTable((M4AVoiceTable)SongPlayer.Instance.Song.VoiceTable, saveAfter7F, false);

                sf2.Save(fileName);
            }

            List<int> addedTables = new List<int>();
            void AddTable(M4AVoiceTable table, bool saveAfter7F, bool fromDrum)
            {
                int tableOffset = table.GetOffset();
                if (addedTables.Contains(tableOffset))
                    return;
                addedTables.Add(tableOffset);

                int amt = saveAfter7F ? 0xFF : 0x7F;

                for (int i = 0; i <= amt; i++)
                {
                    var voice = table[i];
                    //Console.WriteLine("{0} {1} {2}", i, fromDrum, voice);

                    if (!fromDrum)
                    {
                        string name = "Instrument " + i;
                        sf2.AddPreset(name, (ushort)i, 0);
                        //sf2.AddPreset(name, (ushort)i, (ushort)(voice is M4AWrappedDrum ? 128 : 0));
                        sf2.AddPresetBag();
                        sf2.AddPresetGenerator(SF2Generator.Instrument,
                            new SF2GeneratorAmount { Amount = (short)sf2.AddInstrument(name) });
                    }

                    if (voice is M4AWrappedDirect direct)
                    {
                        if (fromDrum)
                        {
                            AddDirect(direct, (byte)i, (byte)i);
                            sf2.AddInstrumentGenerator(SF2Generator.OverridingRootKey,
                                new SF2GeneratorAmount { Amount = (short)(i - (direct.Voice.GetRootNote() - 60)) });
                        }
                        else
                        {
                            AddDirect(direct);
                        }
                    }
                    else if (voice is M4AWrappedKeySplit keySplit)
                    {
                        if (fromDrum)
                        {
                            Console.WriteLine("Skipping nested key split within a drum at table 0x{0:X7} index {1}.", tableOffset, i);
                            continue;
                        }
                        foreach (var key in keySplit.Keys)
                        {
                            if (key.Item1 > amt || key.Item2 >= 0x80) continue;
                            var subvoice = keySplit.Table[key.Item1];

                            var m4 = (M4AVoiceEntry)voice.Voice;
                            if (subvoice is M4AWrappedDirect subdirect)
                            {
                                AddDirect(subdirect, key.Item2, key.Item3);
                            }
                            else if (m4.Type == (int)M4AVoiceFlags.KeySplit)
                            {
                                Console.WriteLine("Skipping nested key split within a key split at table 0x{0:X7} index {1}.", tableOffset, i);
                            }
                            else if (m4.Type == (int)M4AVoiceFlags.Drum)
                            {
                                Console.WriteLine("Skipping nested drum within a key split at table 0x{0:X7} index {1}.", tableOffset, i);
                            }
                            else if (m4.IsGBInstrument())
                            {
                                AddPSG(m4);
                            }
                            else // Invalid
                            {
                                Console.WriteLine("Skipping invalid instrument within a key split at table 0x{0:X7} index {1}.", tableOffset, i);
                            }
                        }
                    }
                    else if (voice is M4AWrappedDrum drum)
                    {
                        if (fromDrum)
                        {
                            Console.WriteLine("Skipping nested drum within a drum at table 0x{0:X7} index {1}.", tableOffset, i);
                            continue;
                        }
                        AddTable(drum.Table, saveAfter7F, true);
                    }
                    else
                    {
                        var m4 = (M4AVoiceEntry)voice.Voice;
                        if (m4.IsInvalid())
                        {
                            Console.WriteLine("Skipping invalid instrument at table 0x{0:X7} index {1}.", tableOffset, i);
                            continue;
                        }
                        if (fromDrum)
                        {
                            AddPSG(m4, (byte)i, (byte)i);
                            sf2.AddInstrumentGenerator(SF2Generator.OverridingRootKey,
                                new SF2GeneratorAmount { Amount = (short)(i - (m4.GetRootNote() - 60)) });
                        }
                        else
                        {
                            AddPSG(m4);
                        }
                    }
                }
            }

            void AddPSG(M4AVoiceEntry entry, byte low = 0, byte high = 0x7F)
            {
                int sample;

                M4AVoiceType type = (M4AVoiceType)(entry.Type & 0x7);
                if (type == M4AVoiceType.Square1 || type == M4AVoiceType.Square2)
                    sample = (int)entry.SquarePattern;
                else if (type == M4AVoiceType.Wave)
                    sample = AddWave(entry.Address - ROM.Pak);
                else if (type == M4AVoiceType.Noise)
                    sample = (int)entry.NoisePattern + 4;
                else
                    return;

                sf2.AddInstrumentBag();

                high = Math.Min((byte)0x7F, high);
                if (!(low == 0 && high == 0x7F))
                    sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = low, HighByte = high });

                // ADSR
                if (entry.ADSR.A != 0)
                {
                    // Compute attack time - the sound engine is called 60 times per second
                    // and adds "attack" to envelope every time the engine is called
                    double att_time = entry.ADSR.A / 5d;
                    double att = 1200 * Math.Log(att_time, 2);
                    sf2.AddInstrumentGenerator(SF2Generator.AttackVolEnv, new SF2GeneratorAmount { Amount = (short)att });
                }
                if (entry.ADSR.S != 15)
                {
                    double sus;
                    // Compute attenuation in cB if sustain is non-zero
                    if (entry.ADSR.S != 0) sus = 100 * Math.Log(15d / entry.ADSR.S);
                    // Special case where attenuation is infinite -> use max value
                    else sus = 1000;

                    sf2.AddInstrumentGenerator(SF2Generator.SustainVolEnv, new SF2GeneratorAmount { Amount = (short)sus });

                    double dec_time = entry.ADSR.D / 5d;
                    double dec = 1200 * Math.Log(dec_time + 1, 2);
                    sf2.AddInstrumentGenerator(SF2Generator.DecayVolEnv, new SF2GeneratorAmount { Amount = (short)dec });
                }
                if (entry.ADSR.R != 0)
                {
                    double rel_time = entry.ADSR.R / 5d;
                    double rel = 1200 * Math.Log(rel_time, 2);
                    sf2.AddInstrumentGenerator(SF2Generator.ReleaseVolEnv, new SF2GeneratorAmount { Amount = (short)rel });
                }

                if (type == M4AVoiceType.Noise && entry.Panpot != 0)
                    sf2.AddInstrumentGenerator(SF2Generator.Pan, new SF2GeneratorAmount { Amount = (short)((entry.Panpot - 0xC0) * (500d / 0x80)) });
                sf2.AddInstrumentGenerator(SF2Generator.SampleModes, new SF2GeneratorAmount { Amount = 1 });
                sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { Amount = (short)sample });
            }
            void AddDirect(M4AWrappedDirect direct, byte low = 0, byte high = 0x7F)
            {
                var entry = (M4AVoiceEntry)direct.Voice;

                var gSample = direct.Sample.GetSample();
                if (gSample == null)
                    return;

                int sample = AddDirectSample(direct.Sample);

                sf2.AddInstrumentBag();

                high = Math.Min((byte)0x7F, high);
                if (!(low == 0 && high == 0x7F))
                    sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = low, HighByte = high });

                // Fixed frequency
                if ((entry.Type & (int)M4AVoiceFlags.Fixed) == (int)M4AVoiceFlags.Fixed)
                    sf2.AddInstrumentGenerator(SF2Generator.ScaleTuning, new SF2GeneratorAmount { Amount = 0 });

                // ADSR
                if (entry.ADSR.A != 0xFF)
                {
                    // Compute attack time - the sound engine is called 60 times per second
                    // and adds "attack" to envelope every time the engine is called
                    double att_time = (0x100 / 60d) / entry.ADSR.A;
                    double att = 1200 * Math.Log(att_time, 2);
                    sf2.AddInstrumentGenerator(SF2Generator.AttackVolEnv, new SF2GeneratorAmount { Amount = (short)att });
                }
                if (entry.ADSR.S != 0xFF)
                {
                    double sus;
                    // Compute attenuation in cB if sustain is non-zero
                    if (entry.ADSR.S != 0) sus = 100 * Math.Log((double)0x100 / entry.ADSR.S);
                    // Special case where attenuation is infinite -> use max value
                    else sus = 1000;

                    sf2.AddInstrumentGenerator(SF2Generator.SustainVolEnv, new SF2GeneratorAmount { Amount = (short)sus });

                    double dec_time = (Math.Log(0x100) / (Math.Log(0x100) - Math.Log(entry.ADSR.D))) / 60;
                    dec_time *= 10 / Math.Log(0x100);
                    double dec = 1200 * Math.Log(dec_time, 2);
                    sf2.AddInstrumentGenerator(SF2Generator.DecayVolEnv, new SF2GeneratorAmount { Amount = (short)dec });
                }
                if (entry.ADSR.R != 0x00)
                {
                    double rel_time = (Math.Log(0x100) / (Math.Log(0x100) - Math.Log(entry.ADSR.R))) / 60;
                    double rel = 1200 * Math.Log(rel_time, 2);
                    sf2.AddInstrumentGenerator(SF2Generator.ReleaseVolEnv, new SF2GeneratorAmount { Amount = (short)rel });
                }

                if (entry.Panpot != 0)
                    sf2.AddInstrumentGenerator(SF2Generator.Pan, new SF2GeneratorAmount { Amount = (short)((entry.Panpot - 0xC0) * (500d / 0x80)) });
                sf2.AddInstrumentGenerator(SF2Generator.SampleModes, new SF2GeneratorAmount { Amount = (short)(gSample.bLoop ? 1 : 0) });
                sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { Amount = (short)sample });
            }


            List<int> savedSamples = new List<int>();
            int AddWave(int address)
            {
                if (savedSamples.Contains(address))
                    return 6 + savedSamples.IndexOf(address);
                savedSamples.Add(address);

                float[] ieee = GBSamples.PCM4ToFloat(address);
                short[] pcm16 = FloatToPCM16(ieee);
                return (int)sf2.AddSample(pcm16, string.Format("Wave 0x{0:X7}", address), true, 0, 7040, 69, 0);
            }
            int AddDirectSample(M4AWrappedSample sample)
            {
                int sampleOffset = sample.GetOffset();
                if (savedSamples.Contains(sampleOffset))
                    return 6 + savedSamples.IndexOf(sampleOffset);
                savedSamples.Add(sampleOffset);

                var gSample = sample.GetSample();
                byte[] pcm8 = ROM.Instance.Reader.ReadBytes(gSample.Length, gSample.GetOffset());
                short[] pcm16 = PCM8ToPCM16(pcm8);
                return (int)sf2.AddSample(pcm16, string.Format("Sample 0x{0:X7}", sampleOffset),
                    gSample.bLoop, (uint)gSample.LoopPoint, (uint)gSample.Frequency, 60, 0);
            }

            void AddSquaresAndNoises()
            {
                sf2.AddSample(FloatToPCM16(GBSamples.SquareD12), "Square Wave D12", true, 0, 3520, 69, 0);
                sf2.AddSample(FloatToPCM16(GBSamples.SquareD25), "Square Wave D25", true, 0, 3520, 69, 0);
                sf2.AddSample(FloatToPCM16(GBSamples.SquareD50), "Square Wave D50", true, 0, 3520, 69, 0);
                sf2.AddSample(FloatToPCM16(GBSamples.SquareD75), "Square Wave D75", true, 0, 3520, 69, 0);

                sf2.AddSample(BitArrayToPCM16(GBSamples.NoiseFine), "Noise Fine", true, 0, 4096, 60, 0);
                sf2.AddSample(BitArrayToPCM16(GBSamples.NoiseRough), "Noise Rough", true, 0, 4096, 60, 0);
            }
        }

        private class MLSSVoiceTableSaver
        {
            SF2 sf2;
            // value is index in sf2
            Dictionary<MLSSWrappedSample, int> addedSamples = new Dictionary<MLSSWrappedSample, int>();

            public MLSSVoiceTableSaver(string fileName)
            {
                sf2 = new SF2();
                AddInfo(sf2);

                var table = (MLSSVoiceTable)SongPlayer.Instance.Song.VoiceTable;
                AddSamples(table);
                AddInstruments(table);

                sf2.Save(fileName);
            }

            void AddSamples(MLSSVoiceTable table)
            {
                for (int i = 0; i < table.Samples.Length; i++)
                {
                    var sample = table.Samples[i];
                    if (sample == null)
                        continue;

                    var gSample = sample.GetSample();
                    byte[] pcmU8 = ROM.Instance.Reader.ReadBytes(gSample.Length, gSample.GetOffset());
                    short[] pcm16 = PCMU8ToPCM16(pcmU8);
                    addedSamples.Add(sample,
                        (int)sf2.AddSample(pcm16, string.Format("Sample {0}", i),
                        gSample.bLoop, (uint)gSample.LoopPoint, (uint)gSample.Frequency, 60, 0));
                }
            }
            void AddInstruments(MLSSVoiceTable table)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    var voice = (MLSSWrappedVoice)table[i];
                    var entries = voice.GetSubVoices().Cast<MLSSVoiceEntry>();
                    if (entries.Count() == 0)
                        continue;

                    string name = "Instrument " + i;
                    sf2.AddPreset(name, (ushort)i, 0);
                    sf2.AddPresetBag();
                    sf2.AddPresetGenerator(SF2Generator.Instrument,
                        new SF2GeneratorAmount { Amount = (short)sf2.AddInstrument(name) });
                    foreach (var entry in entries)
                    {
                        sf2.AddInstrumentBag();
                        if (!(entry.MinKey == 0 && entry.MaxKey == 0x7F))
                            sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = entry.MinKey, HighByte = entry.MaxKey });
                        if (entry.IsFixedFrequency == 0x80)
                            sf2.AddInstrumentGenerator(SF2Generator.ScaleTuning, new SF2GeneratorAmount { Amount = 0 });
                        if (entry.Sample < table.Samples.Length)
                        {
                            var sample = table.Samples[entry.Sample];
                            if (sample != null)
                            {
                                sf2.AddInstrumentGenerator(SF2Generator.SampleModes, new SF2GeneratorAmount { Amount = (short)(sample.GetSample().bLoop ? 1 : 0) });
                                sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { Amount = (short)addedSamples[sample] });
                            }
                            else
                                Console.WriteLine("Voice {0} uses a null sample id ({1})", i, entry.Sample);
                        }
                        else
                            Console.WriteLine("Voice {0} uses an invalid sample id ({1})", i, entry.Sample);
                    }
                }
            }
        }
    }
}
