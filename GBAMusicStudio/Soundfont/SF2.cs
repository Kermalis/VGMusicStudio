using System.IO;

namespace Kermalis.SoundFont2
{
    public sealed class SF2
    {
        uint size;
        InfoListChunk infoChunk;
        SdtaListChunk soundChunk;
        PdtaListChunk hydraChunk;

        internal BinaryWriter Writer { get; private set; }

        internal int IBAGCount => hydraChunk.ibag_subchunk.Count;
        internal int IGENCount => hydraChunk.igen_subchunk.Count;
        internal int IMODCount => hydraChunk.imod_subchunk.Count;
        internal int PBAGCount => hydraChunk.pbag_subchunk.Count;
        internal int PGENCount => hydraChunk.pgen_subchunk.Count;
        internal int PMODCount => hydraChunk.pmod_subchunk.Count;

        public SF2(string engine = "", string bank = "", string rom = "", ushort rom_revision_major = 0, ushort rom_revision_minor = 0, string date = "", string designer = "", string products = "", string copyright = "", string comment = "", string tools = "")
        {
            SFVersionTag rom_revision = rom_revision_major == 0 && rom_revision_minor == 0 ? null : new SFVersionTag(rom_revision_major, rom_revision_minor);
            infoChunk = new InfoListChunk(this, engine, bank, rom, rom_revision, date, designer, products, copyright, comment, tools);
            soundChunk = new SdtaListChunk(this);
            hydraChunk = new PdtaListChunk(this);
        }

        public void AddSample(short[] pcm16, string name, bool bLoop, uint loop_pos, uint sample_rate, sbyte original_pitch, sbyte pitch_correction)
        {
            uint dir_offset = soundChunk.smpl_subchunk.AddSample(pcm16, bLoop, loop_pos);
            // If the sample is looped the standard requires us to add the 8 bytes from the start of the loop to the end
            uint dir_end, dir_loop_end, dir_loop_start;

            uint len = (uint)pcm16.Length;
            if (bLoop)
            {
                dir_end = dir_offset + len + 8;
                dir_loop_end = dir_offset + len;
                dir_loop_start = dir_offset + loop_pos;
            }
            else
            {
                dir_end = dir_offset + len;
                dir_loop_end = 0;
                dir_loop_start = 0;
            }

            AddSampleHeader(name, dir_offset, dir_end, dir_loop_start, dir_loop_end, sample_rate, original_pitch, pitch_correction);
        }

        public void AddInstrument(string name)
        {
            hydraChunk.inst_subchunk.AddInstrument(new SF2Inst(this, name));
        }
        public void AddINSTBag()
        {
            hydraChunk.ibag_subchunk.AddBag(new SF2Bag(this, false));
        }
        public void AddINSTModulator()
        {
            hydraChunk.imod_subchunk.AddModulator(new SF2ModList(this));
        }
        public void AddINSTGenerator()
        {
            hydraChunk.igen_subchunk.AddGenerator(new SF2GenList(this));
        }
        public void AddINSTGenerator(SF2Generator operation, GenAmountType genAmountType)
        {
            hydraChunk.igen_subchunk.AddGenerator(new SF2GenList(this, operation, genAmountType));
        }

        public void AddPreset(string name, ushort patch, ushort bank)
        {
            hydraChunk.phdr_subchunk.AddPreset(new SF2PresetHeader(this, name, patch, bank));
        }
        public void AddPresetBag()
        {
            hydraChunk.pbag_subchunk.AddBag(new SF2Bag(this, true));
        }
        public void AddPresetModulator()
        {
            hydraChunk.pmod_subchunk.AddModulator(new SF2ModList(this));
        }
        public void AddPresetGenerator()
        {
            hydraChunk.pgen_subchunk.AddGenerator(new SF2GenList(this));
        }
        public void AddPresetGenerator(SF2Generator operation, GenAmountType genAmountType)
        {
            hydraChunk.pgen_subchunk.AddGenerator(new SF2GenList(this, operation, genAmountType));
        }

        
        public void Save(string path)
        {
            Writer = new BinaryWriter(File.Open(path, FileMode.Create));

            AddTerminals();

            size = 4;
            size += infoChunk.Size + 8;
            size += soundChunk.CalcSize() + 8;
            size += hydraChunk.CalcSize() + 8;

            Writer.Write("RIFF".ToCharArray());
            Writer.Write(size);
            Writer.Write("sfbk".ToCharArray());

            infoChunk.Write();
            soundChunk.Write();
            hydraChunk.Write();

            Writer.Close();
        }

        // Required by the standard
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

        void AddSampleHeader(string name, uint start, uint end, uint start_loop, uint end_loop, uint sample_rate, sbyte original_pitch, sbyte pitch_correction)
        {
            hydraChunk.shdr_subchunk.AddSample(new SF2Sample(this, name, start, end, start_loop, end_loop, sample_rate, original_pitch, pitch_correction));
        }
    }
}
