using GBAMusicStudio.Core.M4A;
using Kermalis.SoundFont2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static GBAMusicStudio.Core.M4A.M4AStructs;

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

        SF2 sf2;
        List<SVoice> instruments = new List<SVoice>();
        List<FMOD.Sound> samples = new List<FMOD.Sound>();

        internal VoiceTableSaver(VoiceTable table, string filename, bool saveAfter7F = false)
        {
            sf2 = new SF2("", "", "", 0, 0, "", ROM.Instance.Game.Creator, "", "GBA Music Studio by Kermalis");

            AddTable(table, saveAfter7F, true);

            sf2.Save(filename);
        }

        void AddTable(VoiceTable table, bool saveAfter7F, bool isNewInst)
        {
            int amt = saveAfter7F ? 0xFF : 0x7F;

            for (ushort i = 0; i <= amt; i++)
            {
                var voice = table[i];
                if (instruments.Contains(voice)) continue;
                instruments.Add(voice);

                if (isNewInst)
                {
                    string name = "Instrument " + i;
                    AddPreset(name, i);
                    sf2.AddInstrument(name);
                }

                if (voice is SDirect direct)
                {
                    if (!isNewInst)
                    {
                        AddDirect(direct, (byte)i, (byte)i);
                        sf2.AddINSTGenerator(SF2Generator.overridingRootKey, new GenAmountType((ushort)(i - (direct.Voice.RootNote - 60))));
                    }
                    else
                        AddDirect(direct);
                }
                else if (voice.Voice is PSG_Square_1 || voice.Voice is PSG_Square_2 || voice.Voice is PSG_Wave || voice.Voice is PSG_Noise)
                {
                    if (!isNewInst)
                    {
                        if (voice.Voice is PSG_Noise)
                        {
                            AddPSG(voice.Voice, (byte)i, (byte)i);
                            sf2.AddINSTGenerator(SF2Generator.overridingRootKey, new GenAmountType((ushort)(i - (voice.Voice.RootNote - 60))));
                        }
                    }
                    else
                    {
                        AddPSG(voice.Voice);
                        if (!(voice.Voice is PSG_Noise))
                            sf2.AddINSTGenerator(SF2Generator.overridingRootKey, new GenAmountType(69));
                    }
                }
                else if (isNewInst && voice is SMulti multi)
                {
                    foreach (var key in multi.Keys)
                    {
                        if (key.Item1 > amt || key.Item2 > amt) continue;
                        var subvoice = multi.Table[key.Item1];

                        if (subvoice is SDirect subdirect)
                        {
                            AddDirect(subdirect, key.Item2, key.Item3);
                        }
                    }
                }
                else if (voice is SDrum drum)
                {
                    AddTable(drum.Table, saveAfter7F, false);
                }
            }
        }

        void AddPreset(string name, ushort instrument)
        {
            sf2.AddPreset(name, instrument, 0);
            sf2.AddPresetBag();
            sf2.AddPresetGenerator(SF2Generator.instrument, new GenAmountType(instrument));
        }
        void AddPSG(Voice voice, byte low = 0, byte high = 127)
        {
            dynamic v = voice;
            int sample;

            if (voice is PSG_Square_1 || voice is PSG_Square_2)
                sample = AddSample(SongPlayer.Sounds[SongPlayer.SQUARE12_ID - v.Pattern], "Square Wave " + v.Pattern);
            else if (voice is PSG_Wave wave)
                sample = AddSample(SongPlayer.Sounds[wave.Address], string.Format("PSG Wave 0x{0:X}", wave.Address));
            else
                sample = AddSample(SongPlayer.Sounds[SongPlayer.NOISE0_ID - v.Pattern], "Noise " + v.Pattern);

            sf2.AddINSTBag();

            // ADSR
            if (v.A != 0)
            {
                // Compute attack time - the sound engine is called 60 times per second
                // and adds "attack" to envelope every time the engine is called
                double att_time = v.A / 5.0;
                double att = 1200 * Math.Log(att_time, 2);
                sf2.AddINSTGenerator(SF2Generator.attackVolEnv, new GenAmountType((ushort)att));
            }
            if (v.S != 15)
            {
                double sus;
                // Compute attenuation in cB if sustain is non-zero
                if (v.S != 0) sus = 100 * Math.Log(15d / v.S);
                // Special case where attenuation is infinite -> use max value
                else sus = 1000;

                sf2.AddINSTGenerator(SF2Generator.sustainVolEnv, new GenAmountType((ushort)sus));

                double dec_time = v.D / 5d;
                double dec = 1200 * Math.Log(dec_time + 1, 2);
                sf2.AddINSTGenerator(SF2Generator.decayVolEnv, new GenAmountType((ushort)dec));
            }
            if (v.R != 0)
            {
                double rel_time = v.R / 5d;
                double rel = 1200 * Math.Log(rel_time, 2);
                sf2.AddINSTGenerator(SF2Generator.releaseVolEnv, new GenAmountType((ushort)rel));
            }

            high = Math.Min((byte)127, high);
            if (!(low == 0 && high == 127))
                sf2.AddINSTGenerator(SF2Generator.keyRange, new GenAmountType(low, high));
            if (voice is PSG_Noise noise && noise.Panpot != 0)
                sf2.AddINSTGenerator(SF2Generator.pan, new GenAmountType((ushort)((noise.Panpot - 0xC0) * (500d / 0x80))));
            sf2.AddINSTGenerator(SF2Generator.sampleModes, new GenAmountType(1));
            sf2.AddINSTGenerator(SF2Generator.sampleID, new GenAmountType((ushort)(sample)));
        }
        void AddDirect(SDirect direct, byte low = 0, byte high = 127)
        {
            var d = direct.Voice as Direct_Sound;

            if (!SongPlayer.Sounds.TryGetValue(d.Address, out FMOD.Sound sound))
                return;
            int sample = AddSample(sound, string.Format("Sample 0x{0:X}", d.Address));

            sf2.AddINSTBag();

            // Fixed frequency
            if (d.Type == 0x8)
                sf2.AddINSTGenerator(SF2Generator.scaleTuning, new GenAmountType(0));

            // ADSR
            if (d.A != 0xFF)
            {
                // Compute attack time - the sound engine is called 60 times per second
                // and adds "attack" to envelope every time the engine is called
                double att_time = (256 / 60d) / d.A;
                double att = 1200 * Math.Log(att_time, 2);
                sf2.AddINSTGenerator(SF2Generator.attackVolEnv, new GenAmountType((ushort)att));
            }
            if (d.S != 0xFF)
            {
                double sus;
                // Compute attenuation in cB if sustain is non-zero
                if (d.S != 0) sus = 100 * Math.Log(256d / d.S);
                // Special case where attenuation is infinite -> use max value
                else sus = 1000;

                sf2.AddINSTGenerator(SF2Generator.sustainVolEnv, new GenAmountType((ushort)sus));

                double dec_time = (Math.Log(256) / (Math.Log(256) - Math.Log(d.D))) / 60;
                dec_time *= 10 / Math.Log(256);
                double dec = 1200 * Math.Log(dec_time, 2);
                sf2.AddINSTGenerator(SF2Generator.decayVolEnv, new GenAmountType((ushort)dec));
            }
            if (d.R != 0x00)
            {
                double rel_time = (Math.Log(256) / (Math.Log(256) - Math.Log(d.R))) / 60;
                double rel = 1200 * Math.Log(rel_time, 2);
                sf2.AddINSTGenerator(SF2Generator.releaseVolEnv, new GenAmountType((ushort)rel));
            }

            high = Math.Min((byte)127, high);
            if (!(low == 0 && high == 127))
                sf2.AddINSTGenerator(SF2Generator.keyRange, new GenAmountType(low, high));
            if (d.Panpot != 0)
                sf2.AddINSTGenerator(SF2Generator.pan, new GenAmountType((ushort)((d.Panpot - 0xC0) * (500d / 0x80))));
            sf2.AddINSTGenerator(SF2Generator.sampleModes, new GenAmountType((ushort)(direct.Sample.DoesLoop != 0 ? 1 : 0)));
            sf2.AddINSTGenerator(SF2Generator.sampleID, new GenAmountType((ushort)(sample)));
        }
        int AddSample(FMOD.Sound sound, string name)
        {
            if (samples.Contains(sound)) return samples.IndexOf(sound);

            // Get properties
            sound.getLength(out uint length, FMOD.TIMEUNIT.PCMBYTES);
            sound.getLoopPoints(out uint loop_start, FMOD.TIMEUNIT.PCMBYTES, out uint loop_end, FMOD.TIMEUNIT.PCMBYTES);
            sound.getLoopCount(out int loopCount);
            sound.getDefaults(out float frequency, out int priority);

            // Get sample data
            sound.@lock(0, length, out IntPtr snd, out IntPtr idc, out uint len, out uint idc2);
            var pcm8 = new byte[len];
            Marshal.Copy(snd, pcm8, 0, (int)len);
            sound.unlock(snd, idc, len, idc2);
            short[] pcm16 = pcm8.Select(i => (short)(i << 8)).ToArray();

            // Add to file
            sf2.AddSample(pcm16, name, loopCount == -1, loop_start, (uint)frequency, 60, 0);
            samples.Add(sound);
            return samples.Count - 1;
        }
    }
}
