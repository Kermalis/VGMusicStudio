using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundFont
{
    internal class SF2Chunk
    {
        char[] chunk_name; // Length 4
        internal uint Size; // Size in bytes

        protected SF2 sf2;

        protected SF2Chunk(SF2 inSf2, string name)
        {
            chunk_name = name.ToCharArray();
            sf2 = inSf2;
        }

        internal virtual void Write()
        {
            sf2.Writer.Write(chunk_name);
            sf2.Writer.Write(Size);
        }
    }

    internal class ListChunk : SF2Chunk
    {
        char[] chunk_name; // Length 4

        protected ListChunk(SF2 inSf2, string name) : base(inSf2, "LIST")
        {
            chunk_name = name.ToCharArray();
            Size = 4;
        }

        internal override void Write()
        {
            base.Write();
            sf2.Writer.Write(chunk_name);
        }
    }

    internal class SF2PresetHeader
    {
        internal static uint Size = 38;
        SF2 sf2;

        char[] ach_preset_name; // Length 20
        ushort wPreset; // Patch #
        ushort wBank; // Bank #
        ushort wPresetBagNdx; // Index to "bag" of instruments
        const uint dwLibrary = 0;
        const uint dwGenre = 0;
        const uint dwMorphology = 0;

        internal SF2PresetHeader(SF2 inSf2, string name, ushort patch, ushort bank)
        {
            sf2 = inSf2;
            ach_preset_name = new char[20];
            var temp = name.ToCharArray().Take(20).ToArray();
            Buffer.BlockCopy(temp, 0, ach_preset_name, 0, temp.Length * 2);
            wPreset = patch;
            wBank = bank;
            wPresetBagNdx = (ushort)sf2.PBAGCount;
        }

        internal void Write()
        {
            sf2.Writer.Write(ach_preset_name);
            sf2.Writer.Write(wPreset);
            sf2.Writer.Write(wBank);
            sf2.Writer.Write(wPresetBagNdx);
            sf2.Writer.Write(dwLibrary);
            sf2.Writer.Write(dwGenre);
            sf2.Writer.Write(dwMorphology);
        }
    }

    internal class SF2Bag
    {
        internal static uint Size = 4;
        SF2 sf2;

        ushort wGenNdx; // Index of list of generators
        ushort wModNdx; // Index of list of modulators

        internal SF2Bag(SF2 inSf2, bool preset)
        {
            sf2 = inSf2;
            if (preset)
            {
                wGenNdx = (ushort)sf2.PGENCount;
                wModNdx = (ushort)sf2.PMODCount;
            }
            else
            {
                wGenNdx = (ushort)sf2.IGENCount;
                wModNdx = (ushort)sf2.IMODCount;
            }
        }

        internal void Write()
        {
            sf2.Writer.Write(wGenNdx);
            sf2.Writer.Write(wModNdx);
        }
    }

    internal class SF2ModList
    {
        internal static uint Size = 10;
        SF2 sf2;

        SF2Modulator sfModSrcOper;
        SF2Generator sfModDestOper;
        ushort modAmount;
        SF2Modulator sfModAmtSrcOper;
        SF2Transform sfModTransOper;

        internal SF2ModList(SF2 inSf2)
        {
            sf2 = inSf2;
        }

        internal void Write()
        {
            sf2.Writer.Write((ushort)sfModSrcOper);
            sf2.Writer.Write((ushort)sfModDestOper);
            sf2.Writer.Write(modAmount);
            sf2.Writer.Write((ushort)sfModAmtSrcOper);
            sf2.Writer.Write((ushort)sfModTransOper);
        }
    }

    internal class SF2GenList
    {
        internal static uint Size = 4;
        SF2 sf2;

        SF2Generator sfGenOper;
        GenAmountType genAmount;

        internal SF2GenList(SF2 inSf2)
        {
            sf2 = inSf2;
            genAmount = new GenAmountType();
        }
        internal SF2GenList(SF2 inSf2, SF2Generator operation, GenAmountType amount)
        {
            sf2 = inSf2;
            sfGenOper = operation;
            genAmount = amount;
        }

        internal void Write()
        {
            sf2.Writer.Write((ushort)sfGenOper);
            sf2.Writer.Write(genAmount.Value);
        }
    }

    internal class SF2Inst
    {
        internal static uint Size = 22;
        SF2 sf2;

        char[] achInstName; // Length 20
        ushort wInstBagNdx;

        internal SF2Inst(SF2 inSf2, string name)
        {
            sf2 = inSf2;
            achInstName = new char[20];
            var temp = name.ToCharArray().Take(20).ToArray();
            Buffer.BlockCopy(temp, 0, achInstName, 0, temp.Length * 2);
            wInstBagNdx = (ushort)sf2.IBAGCount;
        }

        internal void Write()
        {
            sf2.Writer.Write(achInstName);
            sf2.Writer.Write(wInstBagNdx);
        }
    }

    internal class SF2Sample
    {
        internal static uint Size = 46;
        SF2 sf2;

        char[] achSampleName; // Length 20
        uint dwStart;
        uint dwEnd;
        uint dwStartloop;
        uint dwEndloop;
        uint dwSampleRate;
        sbyte byOriginalPitch;
        sbyte chPitchCorrection;
        ushort wSampleLink;
        SF2SampleLink sfSampleType;

        internal SF2Sample(SF2 inSf2, string name, uint start, uint end, uint start_loop, uint end_loop, uint sample_rate, sbyte original_pitch, sbyte pitch_correction)
        {
            achSampleName = new char[20];
            var temp = name.ToCharArray().Take(20).ToArray();
            Buffer.BlockCopy(temp, 0, achSampleName, 0, temp.Length * 2);
            dwStart = start;
            dwEnd = end;
            dwStartloop = start_loop;
            dwEndloop = end_loop;
            dwSampleRate = sample_rate;
            byOriginalPitch = original_pitch;
            chPitchCorrection = pitch_correction;
            wSampleLink = 0;
            sfSampleType = SF2SampleLink.monoSample;
            sf2 = inSf2;
        }

        internal void Write()
        {
            sf2.Writer.Write(achSampleName);
            sf2.Writer.Write(dwStart);
            sf2.Writer.Write(dwEnd);
            sf2.Writer.Write(dwStartloop);
            sf2.Writer.Write(dwEndloop);
            sf2.Writer.Write(dwSampleRate);
            sf2.Writer.Write(byOriginalPitch);
            sf2.Writer.Write(chPitchCorrection);
            sf2.Writer.Write(wSampleLink);
            sf2.Writer.Write((ushort)sfSampleType);
        }
    }

    #region Sub-Chunks

    internal class VersionSubChunk : SF2Chunk
    {
        // Output format is SoundFont v2.1
        SFVersionTag revision;

        internal VersionSubChunk(SF2 inSf2, string subchunk_type, SFVersionTag version) : base(inSf2, subchunk_type)
        {
            revision = version;
            Size += SFVersionTag.Size;
        }

        internal override void Write()
        {
            base.Write();

            sf2.Writer.Write(revision.wMajor);
            sf2.Writer.Write(revision.wMinor);
        }
    }

    internal class HeaderSubChunk : SF2Chunk
    {
        char[] field;

        internal HeaderSubChunk(SF2 inSf2, string subchunk_type, string s, int max_size = 0x100) : base(inSf2, subchunk_type) // Maybe maxsize must go here
        {
            var test = s.ToCharArray().ToList();
            if (test.Count >= max_size) // Input too long, cut it down
            {
                test = test.Take(max_size).ToList();
                test[max_size - 1] = '\0';
            }
            else if (test.Count % 2 == 0) // Even amount of characters
            {
                test.Add('\0'); // Add two null-terminators to keep the byte count even
                test.Add('\0');
            }
            else // Odd amount of characters
            {
                test.Add('\0'); // Add one null-terminator since that would make byte the count even
            }
            field = test.ToArray();
            Size += (uint)field.Length;
        }

        internal override void Write()
        {
            base.Write();

            sf2.Writer.Write(field);
        }
    }

    internal class SMPLSubChunk : SF2Chunk
    {
        List<short[]> wave_list; // Samples
        List<bool> loop_flag_list; // Loop flag for samples
        List<uint> loop_pos_list; // Loop start data

        internal SMPLSubChunk(SF2 inSf2) : base(inSf2, "smpl")
        {
            wave_list = new List<short[]>();
            loop_flag_list = new List<bool>();
            loop_pos_list = new List<uint>();
        }

        // Returns directory index of the start of the sample
        internal uint AddSample(short[] pcm16, bool bLoop, uint loop_pos)
        {
            wave_list.Add(pcm16);
            loop_flag_list.Add(bLoop);
            loop_pos_list.Add(loop_pos);

            uint len = (uint)pcm16.Length;
            uint dir_offset = Size >> 1;
            // 2 bytes per sample
            // 8 samples after looping
            // 46 empty samples
            if (bLoop)
                Size += (len + 8 + 46) * 2;
            else
                Size += (len + 46) * 2;

            return dir_offset;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < wave_list.Count; i++)
            {
                // Write wave
                for (int j = 0; j < wave_list[i].Length; j++)
                    sf2.Writer.Write(wave_list[i][j]);

                // If looping is enabled, write 8 samples from after the loop point
                if (loop_flag_list[i])
                    for (int j = 0; j < 8; j++)
                        sf2.Writer.Write(wave_list[i][loop_pos_list[i] + j]);

                // Write 46 empty samples
                for (int j = 0; j < 46; j++)
                    sf2.Writer.Write((short)0);
            }
        }
    }

    internal class PHDRSubChunk : SF2Chunk
    {
        List<SF2PresetHeader> preset_list;
        internal int Count { get => preset_list.Count; }

        internal PHDRSubChunk(SF2 inSf2) : base(inSf2, "phdr")
        {
            preset_list = new List<SF2PresetHeader>();
        }

        internal void AddPreset(SF2PresetHeader preset)
        {
            preset_list.Add(preset);
            Size += SF2PresetHeader.Size;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < preset_list.Count; i++)
                preset_list[i].Write();
        }
    }

    internal class INSTSubChunk : SF2Chunk
    {
        List<SF2Inst> instrument_list;
        internal int Count { get => instrument_list.Count; }

        internal INSTSubChunk(SF2 inSf2) : base(inSf2, "inst")
        {
            instrument_list = new List<SF2Inst>();
        }

        internal void AddInstrument(SF2Inst instrument)
        {
            instrument_list.Add(instrument);
            Size += SF2Inst.Size;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < instrument_list.Count; i++)
                instrument_list[i].Write();
        }
    }

    internal class BAGSubChunk : SF2Chunk
    {
        List<SF2Bag> bag_list;
        internal int Count { get => bag_list.Count; }

        internal BAGSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pbag" : "ibag")
        {
            bag_list = new List<SF2Bag>();
        }

        internal void AddBag(SF2Bag bag)
        {
            bag_list.Add(bag);
            Size += SF2Bag.Size;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < bag_list.Count; i++)
                bag_list[i].Write();
        }
    }

    internal class MODSubChunk : SF2Chunk
    {
        List<SF2ModList> modulator_list;
        internal int Count { get => modulator_list.Count; }

        internal MODSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pmod" : "imod")
        {
            modulator_list = new List<SF2ModList>();
        }

        internal void AddModulator(SF2ModList modulator)
        {
            modulator_list.Add(modulator);
            Size += SF2ModList.Size;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < modulator_list.Count; i++)
                modulator_list[i].Write();
        }
    }

    internal class GENSubChunk : SF2Chunk
    {
        List<SF2GenList> generator_list;
        internal int Count { get => generator_list.Count; }

        internal GENSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pgen" : "igen")
        {
            generator_list = new List<SF2GenList>();
        }

        internal void AddGenerator(SF2GenList generator)
        {
            generator_list.Add(generator);
            Size += SF2GenList.Size;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < generator_list.Count; i++)
                generator_list[i].Write();
        }
    }

    internal class SHDRSubChunk : SF2Chunk
    {
        List<SF2Sample> sample_list;
        internal int Count { get => sample_list.Count; }

        internal SHDRSubChunk(SF2 inSf2) : base(inSf2, "shdr")
        {
            sample_list = new List<SF2Sample>();
        }

        internal void AddSample(SF2Sample sample)
        {
            sample_list.Add(sample);
            Size += SF2Sample.Size;
        }

        internal override void Write()
        {
            base.Write();

            for (int i = 0; i < sample_list.Count; i++)
                sample_list[i].Write();
        }
    }

    #endregion

    #region Main Chunks

    internal class InfoChunk : ListChunk
    {
        List<SF2Chunk> sub_chunks;

        internal InfoChunk(SF2 inSf2, string engine, string bank, string rom, SFVersionTag rom_revision, string date, string designer, string products, string copyright, string comment, string tools)
            : base(inSf2, "INFO")
        {
            sub_chunks = new List<SF2Chunk>() // Mandatory sub-chunks
            {
                new VersionSubChunk(inSf2, "ifil", new SFVersionTag(2, 1)),
                new HeaderSubChunk(inSf2, "isng", string.IsNullOrEmpty(engine) ? "EMU8000" : engine),
                new HeaderSubChunk(inSf2, "INAM", string.IsNullOrEmpty(bank) ? "General MIDI" : bank),
            };

            // Optional sub-chunks
            if (!string.IsNullOrEmpty(rom))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "irom", rom));
            if (rom_revision != null)
                sub_chunks.Add(new VersionSubChunk(inSf2, "iver", rom_revision));
            if (!string.IsNullOrEmpty(date))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "ICRD", date));
            if (!string.IsNullOrEmpty(designer))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "IENG", designer));
            if (!string.IsNullOrEmpty(products))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "IPRD", products));
            if (!string.IsNullOrEmpty(copyright))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "ICOP", copyright));
            if (!string.IsNullOrEmpty(comment))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "ICMT", comment, 0x10000));
            if (!string.IsNullOrEmpty(tools))
                sub_chunks.Add(new HeaderSubChunk(inSf2, "ISFT", tools));

            foreach (var sub in sub_chunks)
                Size += sub.Size + 8;
        }

        internal override void Write()
        {
            base.Write();

            foreach (var sub in sub_chunks)
                sub.Write();
        }
    }

    internal class SdtaChunk : ListChunk
    {
        internal readonly SMPLSubChunk smpl_subchunk;

        internal SdtaChunk(SF2 inSf2) : base(inSf2, "sdta")
        {
            smpl_subchunk = new SMPLSubChunk(inSf2);
        }

        internal uint CalcSize()
        {
            Size += smpl_subchunk.Size + 8;
            return Size;
        }

        internal override void Write()
        {
            base.Write();

            smpl_subchunk.Write();
        }
    }

    internal class HydraChunk : ListChunk
    {
        internal readonly PHDRSubChunk phdr_subchunk;
        internal readonly BAGSubChunk pbag_subchunk;
        internal readonly MODSubChunk pmod_subchunk;
        internal readonly GENSubChunk pgen_subchunk;
        internal readonly INSTSubChunk inst_subchunk;
        internal readonly BAGSubChunk ibag_subchunk;
        internal readonly MODSubChunk imod_subchunk;
        internal readonly GENSubChunk igen_subchunk;
        internal readonly SHDRSubChunk shdr_subchunk;

        internal HydraChunk(SF2 inSf2) : base(inSf2, "pdta")
        {
            phdr_subchunk = new PHDRSubChunk(inSf2);
            pbag_subchunk = new BAGSubChunk(inSf2, true);
            pmod_subchunk = new MODSubChunk(inSf2, true);
            pgen_subchunk = new GENSubChunk(inSf2, true);
            inst_subchunk = new INSTSubChunk(inSf2);
            ibag_subchunk = new BAGSubChunk(inSf2, false);
            imod_subchunk = new MODSubChunk(inSf2, false);
            igen_subchunk = new GENSubChunk(inSf2, false);
            shdr_subchunk = new SHDRSubChunk(inSf2);
        }

        internal uint CalcSize()
        {
            Size += phdr_subchunk.Size + 8;
            Size += pbag_subchunk.Size + 8;
            Size += pmod_subchunk.Size + 8;
            Size += pgen_subchunk.Size + 8;
            Size += inst_subchunk.Size + 8;
            Size += ibag_subchunk.Size + 8;
            Size += imod_subchunk.Size + 8;
            Size += igen_subchunk.Size + 8;
            Size += shdr_subchunk.Size + 8;

            return Size;
        }

        internal override void Write()
        {
            base.Write();

            phdr_subchunk.Write();
            pbag_subchunk.Write();
            pmod_subchunk.Write();
            pgen_subchunk.Write();
            inst_subchunk.Write();
            ibag_subchunk.Write();
            imod_subchunk.Write();
            igen_subchunk.Write();
            shdr_subchunk.Write();
        }
    }

    #endregion
}
