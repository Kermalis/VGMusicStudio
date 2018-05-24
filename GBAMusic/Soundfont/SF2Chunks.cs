using System.Collections.Generic;
using System.Linq;
using static GBAMusic.SoundFont.SF2Types;

namespace GBAMusic.SoundFont
{
    class SF2Chunks
    {
        char[] chunk_name; // 4 max
        public uint Size; // Size in bytes of the subchunk

        protected SF2 sf2;

        protected SF2Chunks(SF2 inSf2, string subName, uint subSize = 0)
        {
            chunk_name = subName.ToCharArray();
            if (chunk_name.Length > 4) chunk_name = chunk_name.Take(4).ToArray();
            Size = subSize;
            sf2 = inSf2;
        }

        public virtual void Write()
        {
            sf2.Writer.Write(chunk_name);
            sf2.Writer.Write(Size);
        }
    }

    class SF2PresetHeader
    {
        char[] ach_preset_name; // Max 20
        ushort wPreset; // Patch #
        ushort wBank; // Bank #
        ushort wPresetBagNdx; // Index to "bag" of instruments
        const uint dwLibrary = 0;
        const uint dwGenre = 0;
        const uint dwMorphology = 0;
        SF2 sf2;

        public SF2PresetHeader(SF2 inSf2, string name, ushort patch, ushort bank)
        {
            sf2 = inSf2;
            ach_preset_name = name.ToCharArray();
            if (ach_preset_name.Length > 20) ach_preset_name = ach_preset_name.Take(20).ToArray();
            wPreset = patch;
            wBank = bank;
            wPresetBagNdx = (ushort)sf2.PBAGCount;
        }

        public void Write()
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

    class SF2Bag
    {
        ushort wGenNdx; // Index of list of generators
        ushort wModNdx; // Index of list of modulators
        SF2 sf2;

        public SF2Bag(SF2 inSf2, bool preset)
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

        public void Write()
        {
            sf2.Writer.Write(wGenNdx);
            sf2.Writer.Write(wModNdx);
        }
    }

    class SF2ModList
    {
        SF2Modulator sfModSrcOper;
        SF2Generator sfModDestOper;
        ushort modAmount;
        SF2Modulator sfModAmtSrcOper;
        SF2Transform sfModTransOper;
        SF2 sf2;

        public SF2ModList(SF2 inSf2)
        {
            sf2 = inSf2;
        }

        public void Write()
        {
            sf2.Writer.Write((ushort)sfModSrcOper);
            sf2.Writer.Write((ushort)sfModDestOper);
            sf2.Writer.Write(modAmount);
            sf2.Writer.Write((ushort)sfModAmtSrcOper);
            sf2.Writer.Write((ushort)sfModTransOper);
        }
    }

    class SF2GenList
    {
        SF2Generator sfGenOper;
        GenAmountType genAmount;
        SF2 sf2;

        public SF2GenList(SF2 inSf2)
        {
            sf2 = inSf2;
            genAmount = new GenAmountType();
        }
        public SF2GenList(SF2 inSf2, SF2Generator operation, GenAmountType amount)
        {
            sf2 = inSf2;
            sfGenOper = operation;
            genAmount = amount;
        }

        public void Write()
        {
            sf2.Writer.Write((ushort)sfGenOper);
            sf2.Writer.Write(genAmount.Value);
        }
    }

    class SF2Inst
    {
        char[] achInstName; // Max 20
        ushort wInstBagNdx;
        SF2 sf2;

        public SF2Inst(SF2 inSf2, string name)
        {
            sf2 = inSf2;
            achInstName = name.ToCharArray();
            if (achInstName.Length > 20) achInstName = achInstName.Take(20).ToArray();
            wInstBagNdx = (ushort)sf2.IBAGCount;
        }

        public void Write()
        {
            sf2.Writer.Write(achInstName);
            sf2.Writer.Write(wInstBagNdx);
        }
    }

    class SF2Sample
    {
        char[] achSampleName; // Max 20
        uint dwStart;
        uint dwEnd;
        uint dwStartloop;
        uint dwEndloop;
        uint dwSampleRate;
        sbyte byOriginalPitch;
        sbyte chPitchCorrection;
        ushort wSampleLink;
        SF2SampleLink sfSampleType;
        SF2 sf2;

        public SF2Sample(SF2 inSf2, string name, uint start, uint end, uint start_loop, uint end_loop, uint sample_rate, sbyte original_pitch, sbyte pitch_correction)
        {
            achSampleName = name.ToCharArray();
            if (achSampleName.Length > 20) achSampleName = achSampleName.Take(20).ToArray();
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

        public void Write()
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

    class IFILSubChunk : SF2Chunks
    {
        // Output format is SoundFont v2.1
        ushort wMajor;
        ushort wMinor;

        public IFILSubChunk(SF2 inSf2) : base(inSf2, "ifil", 4)
        {
            wMajor = 2; wMinor = 1;
        }

        public override void Write()
        {
            base.Write();

            sf2.Writer.Write(wMajor);
            sf2.Writer.Write(wMinor);
        }
    }

    class HeaderSubChunk : SF2Chunks
    {
        char[] field;

        public HeaderSubChunk(SF2 inSf2, string subchunk_type, string s) : base(inSf2, subchunk_type, (uint)s.Length + 1)
        {
            field = s.ToCharArray();
        }

        public override void Write()
        {
            base.Write();

            sf2.Writer.Write(field);
            sf2.Writer.Write((byte)0); // Write the string followed by a null byte
        }
    }

    class SMPLSubChunk : SF2Chunks
    {
        List<short[]> wave_list; // Samples
        List<ushort> size_list; // Size of the data sample
        List<bool> loop_flag_list; // Loop flag for samples (required as we need to copy data after the loop)
        List<uint> loop_pos_list; // Loop start data (irrelevent if loop flag is clear - add dummy data)

        public SMPLSubChunk(SF2 inSf2) : base(inSf2, "smpl") { }

        // Returns directory index of the start of the sample
        public uint AddSample(short[] pcm16, ushort len, bool bLoop, uint loop_pos)
        {
            wave_list = new List<short[]>() { pcm16 };
            size_list = new List<ushort>() { len };
            loop_flag_list = new List<bool>() { bLoop };
            loop_pos_list = new List<uint>() { loop_pos };

            uint dir_offset = (uint)(len >> 1);
            // 2 bytes per sample
            // Compute size including the 8 samples after loop point
            // And 46 dummy samples
            if (bLoop)
                Size += (uint)(len + 8 + 46) * 2;
            else
                Size += (uint)(len + 46) * 2;

            return dir_offset;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < wave_list.Count; i++)
            {
                // Write wave
                for (int j = 0; j < size_list[i]; j++)
                    sf2.Writer.Write(wave_list[i][j]);

                // If looping is enabled, you must write 8 samples after the loop point
                if (loop_flag_list[i])
                    for (int j = 0; j < 8; j++)
                        sf2.Writer.Write(wave_list[i][loop_pos_list[i] + j]);

                // Write 46 dummy samples at the end because it is required
                for (int j = 0; j < 46; j++)
                    sf2.Writer.Write((short)0);
            }
        }
    }

    class PHDRSubChunk : SF2Chunks
    {
        List<SF2PresetHeader> preset_list;
        public int Count { get => preset_list.Count; }

        public PHDRSubChunk(SF2 inSf2) : base(inSf2, "phdr")
        {
            preset_list = new List<SF2PresetHeader>();
        }

        public void AddPreset(SF2PresetHeader preset)
        {
            preset_list.Add(preset);
            Size += 38;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < preset_list.Count; i++)
                preset_list[i].Write();
        }
    }

    class INSTSubChunk : SF2Chunks
    {
        List<SF2Inst> instrument_list;
        public int Count { get => instrument_list.Count; }

        public INSTSubChunk(SF2 inSf2) : base(inSf2, "inst")
        {
            instrument_list = new List<SF2Inst>();
        }

        public void AddInstrument(SF2Inst instrument)
        {
            instrument_list.Add(instrument);
            Size += 22;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < instrument_list.Count; i++)
                instrument_list[i].Write();
        }
    }

    class BAGSubChunk : SF2Chunks
    {
        List<SF2Bag> bag_list;
        public int Count { get => bag_list.Count; }

        public BAGSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pbag" : "ibag")
        {
            bag_list = new List<SF2Bag>();
        }

        public void AddBag(SF2Bag bag)
        {
            bag_list.Add(bag);
            Size += 4;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < bag_list.Count; i++)
                bag_list[i].Write();
        }
    }

    class MODSubChunk : SF2Chunks
    {
        List<SF2ModList> modulator_list;
        public int Count { get => modulator_list.Count; }

        public MODSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pmod" : "imod")
        {
            modulator_list = new List<SF2ModList>();
        }

        public void AddModulator(SF2ModList modulator)
        {
            modulator_list.Add(modulator);
            Size += 10;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < modulator_list.Count; i++)
                modulator_list[i].Write();
        }
    }

    class GENSubChunk : SF2Chunks
    {
        List<SF2GenList> generator_list;
        public int Count { get => generator_list.Count; }

        public GENSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pgen" : "igen")
        {
            generator_list = new List<SF2GenList>();
        }

        public void AddGenerator(SF2GenList generator)
        {
            generator_list.Add(generator);
            Size += 4;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < generator_list.Count; i++)
                generator_list[i].Write();
        }
    }

    class SHDRSubChunk : SF2Chunks
    {
        List<SF2Sample> sample_list;
        public int Count { get => sample_list.Count; }

        public SHDRSubChunk(SF2 inSf2) : base(inSf2, "shdr")
        {
            sample_list = new List<SF2Sample>();
        }

        public void AddSample(SF2Sample sample)
        {
            sample_list.Add(sample);
            Size += 46;
        }

        public override void Write()
        {
            base.Write();

            for (int i = 0; i < sample_list.Count; i++)
                sample_list[i].Write();
        }
    }

    #endregion

    #region Main Chunks

    class InfoListChunk : SF2Chunks
    {
        List<SF2Chunks> subs;

        public InfoListChunk(SF2 inSf2) : base(inSf2, "LIST", 4)
        {
            subs = new List<SF2Chunks>() {
                new IFILSubChunk(inSf2),
                new HeaderSubChunk(inSf2, "isng", "EMU8000"),
                new HeaderSubChunk(inSf2, "INAM", "Nintendo Game Boy Advance SoundFont"),
                new HeaderSubChunk(inSf2, "ICOP", "GBAMusic by Kermalis"),
            };
        }

        public uint CalcSize()
        {
            foreach (var sub in subs)
                Size += sub.Size + 8;
            return Size;
        }

        public override void Write()
        {
            base.Write();

            sf2.Writer.Write("INFO".ToCharArray());
            foreach (var sub in subs)
                sub.Write();
        }
    }

    class SdtaListChunk : SF2Chunks
    {
        public readonly SMPLSubChunk smpl_subchunk;

        public SdtaListChunk(SF2 inSf2) : base(inSf2, "LIST", 4)
        {
            smpl_subchunk = new SMPLSubChunk(inSf2);
        }

        public uint CalcSize()
        {
            Size += smpl_subchunk.Size + 8;
            return Size;
        }

        public override void Write()
        {
            base.Write();

            sf2.Writer.Write("sdta".ToCharArray());
            smpl_subchunk.Write();
        }
    }

    class HydraChunk : SF2Chunks
    {
        public readonly PHDRSubChunk phdr_subchunk;
        public readonly BAGSubChunk pbag_subchunk;
        public readonly MODSubChunk pmod_subchunk;
        public readonly GENSubChunk pgen_subchunk;
        public readonly INSTSubChunk inst_subchunk;
        public readonly BAGSubChunk ibag_subchunk;
        public readonly MODSubChunk imod_subchunk;
        public readonly GENSubChunk igen_subchunk;
        public readonly SHDRSubChunk shdr_subchunk;

        public HydraChunk(SF2 inSf2) : base(inSf2, "LIST", 4)
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

        public uint CalcSize()
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

        public override void Write()
        {
            base.Write();

            sf2.Writer.Write("pdta".ToCharArray());
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
