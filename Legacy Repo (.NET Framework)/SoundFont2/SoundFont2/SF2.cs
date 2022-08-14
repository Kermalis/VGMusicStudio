using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.SoundFont2
{
    public sealed class SF2
    {
        private uint _size;
        public InfoListChunk InfoChunk { get; }
        public SdtaListChunk SoundChunk { get; }
        public PdtaListChunk HydraChunk { get; }

        /// <summary>For creating</summary>
        public SF2()
        {
            InfoChunk = new InfoListChunk(this);
            SoundChunk = new SdtaListChunk(this);
            HydraChunk = new PdtaListChunk(this);
        }

        /// <summary>For reading</summary>
        public SF2(string path)
        {
            using (var reader = new EndianBinaryReader(File.Open(path, FileMode.Open)))
            {
                string str = reader.ReadString(4, false);
                if (str != "RIFF")
                {
                    throw new InvalidDataException("RIFF header was not found at the start of the file.");
                }
                _size = reader.ReadUInt32();
                str = reader.ReadString(4, false);
                if (str != "sfbk")
                {
                    throw new InvalidDataException("sfbk header was not found at the expected offset.");
                }
                InfoChunk = new InfoListChunk(this, reader);
                SoundChunk = new SdtaListChunk(this, reader);
                HydraChunk = new PdtaListChunk(this, reader);
            }
        }

        public void Save(string path)
        {
            using (var writer = new EndianBinaryWriter(File.Open(path, FileMode.Create)))
            {
                AddTerminals();

                writer.Write("RIFF", 4);
                writer.Write(_size);
                writer.Write("sfbk", 4);

                InfoChunk.Write(writer);
                SoundChunk.Write(writer);
                HydraChunk.Write(writer);
            }
        }


        /// <summary>Returns sample index</summary>
        public uint AddSample(short[] pcm16, string name, bool bLoop, uint loopPos, uint sampleRate, byte originalKey, sbyte pitchCorrection)
        {
            uint start = SoundChunk.SMPLSubChunk.AddSample(pcm16, bLoop, loopPos);
            // If the sample is looped the standard requires us to add the 8 bytes from the start of the loop to the end
            uint end, loopEnd, loopStart;

            uint len = (uint)pcm16.Length;
            if (bLoop)
            {
                end = start + len + 8;
                loopStart = start + loopPos; loopEnd = start + len;
            }
            else
            {
                end = start + len;
                loopStart = 0; loopEnd = 0;
            }

            return AddSampleHeader(name, start, end, loopStart, loopEnd, sampleRate, originalKey, pitchCorrection);
        }
        /// <summary>Returns instrument index</summary>
        public uint AddInstrument(string name)
        {
            return HydraChunk.INSTSubChunk.AddInstrument(new SF2Instrument(name, (ushort)HydraChunk.IBAGSubChunk.Count));
        }
        public void AddInstrumentBag()
        {
            HydraChunk.IBAGSubChunk.AddBag(new SF2Bag(this, false));
        }
        public void AddInstrumentModulator()
        {
            HydraChunk.IMODSubChunk.AddModulator(new SF2ModulatorList());
        }
        public void AddInstrumentGenerator()
        {
            HydraChunk.IGENSubChunk.AddGenerator(new SF2GeneratorList());
        }
        public void AddInstrumentGenerator(SF2Generator generator, SF2GeneratorAmount amount)
        {
            HydraChunk.IGENSubChunk.AddGenerator(new SF2GeneratorList(generator, amount));
        }
        public void AddPreset(string name, ushort preset, ushort bank)
        {
            HydraChunk.PHDRSubChunk.AddPreset(new SF2PresetHeader(name, preset, bank, (ushort)HydraChunk.PBAGSubChunk.Count));
        }
        public void AddPresetBag()
        {
            HydraChunk.PBAGSubChunk.AddBag(new SF2Bag(this, true));
        }
        public void AddPresetModulator()
        {
            HydraChunk.PMODSubChunk.AddModulator(new SF2ModulatorList());
        }
        public void AddPresetGenerator()
        {
            HydraChunk.PGENSubChunk.AddGenerator(new SF2GeneratorList());
        }
        public void AddPresetGenerator(SF2Generator generator, SF2GeneratorAmount amount)
        {
            HydraChunk.PGENSubChunk.AddGenerator(new SF2GeneratorList(generator, amount));
        }

        private uint AddSampleHeader(string name, uint start, uint end, uint loopStart, uint loopEnd, uint sampleRate, byte originalKey, sbyte pitchCorrection)
        {
            return HydraChunk.SHDRSubChunk.AddSample(new SF2SampleHeader(name, start, end, loopStart, loopEnd, sampleRate, originalKey, pitchCorrection));
        }
        private void AddTerminals()
        {
            AddSampleHeader("EOS", 0, 0, 0, 0, 0, 0, 0);
            AddInstrument("EOI");
            AddInstrumentBag();
            AddInstrumentGenerator();
            AddInstrumentModulator();
            AddPreset("EOP", 0xFF, 0xFF);
            AddPresetBag();
            AddPresetGenerator();
            AddPresetModulator();
        }

        internal void UpdateSize()
        {
            if (InfoChunk == null || SoundChunk == null || HydraChunk == null)
            {
                return;
            }
            _size = 4
                + InfoChunk.UpdateSize() + 8
                + SoundChunk.UpdateSize() + 8
                + HydraChunk.UpdateSize() + 8;
        }
    }
}
