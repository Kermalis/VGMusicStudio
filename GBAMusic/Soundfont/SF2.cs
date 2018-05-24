using System.IO;
using static GBAMusic.SoundFont.SF2Types;

namespace GBAMusic.SoundFont
{
    class SF2
    {
        uint size;
        InfoListChunk infolist_chunk;
        SdtaListChunk sdtalist_chunk;
        HydraChunk pdtalist_chunk;

        public readonly uint default_sample_rate;
        public BinaryWriter Writer { get; private set; }

        public int IBAGCount { get => pdtalist_chunk.ibag_subchunk.Count; }
        public int IGENCount { get => pdtalist_chunk.igen_subchunk.Count; }
        public int IMODCount { get => pdtalist_chunk.imod_subchunk.Count; }
        public int PBAGCount { get => pdtalist_chunk.pbag_subchunk.Count; }
        public int PGENCount { get => pdtalist_chunk.pgen_subchunk.Count; }
        public int PMODCount { get => pdtalist_chunk.pmod_subchunk.Count; }

        public SF2(uint sample_rate = 22050)
        {
            size = 0;
            default_sample_rate = sample_rate;
            infolist_chunk = new InfoListChunk(this);
            sdtalist_chunk = new SdtaListChunk(this);
            pdtalist_chunk = new HydraChunk(this);
        }
        
        // Add a new sample and create corresponding header
        public void AddSample(short[] pcm16, string name, bool bLoop, uint loop_pos, uint sample_rate, sbyte original_pitch, sbyte pitch_correction)
        {
            ushort len = (ushort)pcm16.Length;
            uint dir_offset = sdtalist_chunk.smpl_subchunk.AddSample(pcm16, len, bLoop, loop_pos);
            // If the sample is looped const SF2 standard requires we add the 8 bytes from the start of the loop at the end
            uint dir_end, dir_loop_end, dir_loop_start;

            if (bLoop)
            {
                dir_end = dir_offset + len + 8;
                dir_loop_end = dir_offset + len;
                dir_loop_start = dir_offset + loop_pos;
            }
            else
            {
                dir_end = dir_offset + size;
                dir_loop_end = 0;
                dir_loop_start = 0;
            }

            // Create sample header and add it to the list
            AddSampleHeader(name, dir_offset, dir_end, dir_loop_start, dir_loop_end, sample_rate, original_pitch, pitch_correction);
        }

        public void Write(string path)
        {
            Writer = new BinaryWriter(File.Open(path, FileMode.Create));

            // This function adds the "terminal" data in subchunks that are required
            AddTerminals();

            // Compute size of the entire file
            // This will also compute the size of the chunks
            size = 4;
            size += infolist_chunk.CalcSize() + 8;
            size += sdtalist_chunk.CalcSize() + 8;
            size += pdtalist_chunk.CalcSize() + 8;

            // Write RIFF header
            Writer.Write("RIFF".ToCharArray());
            Writer.Write(size);
            Writer.Write("sfbk".ToCharArray());

            // Write all 3 chunks
            infolist_chunk.Write();
            sdtalist_chunk.Write();
            pdtalist_chunk.Write();

            // Close output file
            Writer.Close();
        }

        // Add terminal data in subchunks where it is required by the standard
        void AddTerminals()
        {
            AddSampleHeader("EOS", 0, 0, 0, 0, 0, 0, 0);
            AddInstrument("EOI");
            AddINSTBag();
            AddINSTGenerator();
            AddINSTModulator();
            AddPreset("EOP", 255, 255);
            AddPresetBag();
            AddPresetGenerator();
            AddPresetModulator();
        }

        // Add a brand new preset header to the list
        void AddPreset(string name, ushort patch, ushort bank)
        {
            pdtalist_chunk.phdr_subchunk.AddPreset(new SF2PresetHeader(this, name, patch, bank));
        }

        // Add a brand new instrument
        void AddInstrument(string name)
        {
            pdtalist_chunk.inst_subchunk.AddInstrument(new SF2Inst(this, name));
        }

        // Add a new instrument bag to the instrument bag list
        // DO NOT use this to add a preset bag !
        void AddINSTBag()
        {
            pdtalist_chunk.ibag_subchunk.AddBag(new SF2Bag(this, false));
        }

        // Add a new preset bag to the preset bag list
        // DO NOT use this to add an instrument bag !
        void AddPresetBag()
        {
            pdtalist_chunk.pbag_subchunk.AddBag(new SF2Bag(this, true));
        }

        // Add a new modulator to the list
        void AddPresetModulator()
        {
            pdtalist_chunk.pmod_subchunk.AddModulator(new SF2ModList(this));
        }

        // Add a new blank generator to the list
        void AddPresetGenerator()
        {
            pdtalist_chunk.pgen_subchunk.AddGenerator(new SF2GenList(this));
        }

        // Add a new customized generator to the list
        void AddPresetGenerator(SF2Generator operation, GenAmountType genAmountType)
        {
            pdtalist_chunk.pgen_subchunk.AddGenerator(new SF2GenList(this, operation, genAmountType));
        }

        // Add a new modulator to the list
        void AddINSTModulator()
        {
            pdtalist_chunk.imod_subchunk.AddModulator(new SF2ModList(this));
        }

        // Add a new blank generator to the list
        void AddINSTGenerator()
        {
            pdtalist_chunk.igen_subchunk.AddGenerator(new SF2GenList(this));
        }

        // Add a new customized generator to the list
        void AddINSTGenerator(SF2Generator operation, GenAmountType genAmountType)
        {
            pdtalist_chunk.igen_subchunk.AddGenerator(new SF2GenList(this, operation, genAmountType));
        }

        // Add a brand new header
        void AddSampleHeader(string name, uint start, uint end, uint start_loop, uint end_loop, uint sample_rate, sbyte original_pitch, sbyte pitch_correction)
        {
            pdtalist_chunk.shdr_subchunk.AddSample(new SF2Sample(this, name, start, end, start_loop, end_loop, sample_rate, original_pitch, pitch_correction));
        }
    }
}
