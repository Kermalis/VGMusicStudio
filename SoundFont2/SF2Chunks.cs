using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;

namespace Kermalis.SoundFont2
{
    public class SF2Chunk
    {
        protected readonly SF2 _sf2;

        /// <summary>Length 4</summary>
        public string ChunkName { get; }
        /// <summary>Size in bytes</summary>
        public uint Size { get; protected set; }

        protected SF2Chunk(SF2 inSf2, string name)
        {
            _sf2 = inSf2;
            ChunkName = name;
        }
        protected SF2Chunk(SF2 inSf2, EndianBinaryReader reader)
        {
            _sf2 = inSf2;
            ChunkName = reader.ReadString(4, false);
            Size = reader.ReadUInt32();
        }

        internal virtual void Write(EndianBinaryWriter writer)
        {
            writer.Write(ChunkName, 4);
            writer.Write(Size);
        }
    }

    public abstract class SF2ListChunk : SF2Chunk
    {
        ///<summary>Length 4</summary>
        public string ListChunkName { get; }

        protected SF2ListChunk(SF2 inSf2, string name) : base(inSf2, "LIST")
        {
            ListChunkName = name;
            Size = 4;
        }
        protected SF2ListChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            ListChunkName = reader.ReadString(4, false);
        }

        internal abstract uint UpdateSize();

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(ListChunkName, 4);
        }
    }

    public sealed class SF2PresetHeader
    {
        public const uint Size = 38;

        /// <summary>Length 20</summary>
        public string PresetName { get; set; }
        public ushort Preset { get; set; }
        public ushort Bank { get; set; }
        public ushort PresetBagIndex { get; set; }
        // Reserved for future implementations
        private readonly uint _library;
        private readonly uint _genre;
        private readonly uint _morphology;

        internal SF2PresetHeader(string name, ushort preset, ushort bank, ushort index)
        {
            PresetName = name;
            Preset = preset;
            Bank = bank;
            PresetBagIndex = index;
        }
        internal SF2PresetHeader(EndianBinaryReader reader)
        {
            PresetName = reader.ReadString(20, true);
            Preset = reader.ReadUInt16();
            Bank = reader.ReadUInt16();
            PresetBagIndex = reader.ReadUInt16();
            _library = reader.ReadUInt32();
            _genre = reader.ReadUInt32();
            _morphology = reader.ReadUInt32();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(PresetName, 20);
            writer.Write(Preset);
            writer.Write(Bank);
            writer.Write(PresetBagIndex);
            writer.Write(_library);
            writer.Write(_genre);
            writer.Write(_morphology);
        }

        public override string ToString()
        {
            return $"Preset Header - Bank = {Bank}" +
                $",\nPreset = {Preset}" +
                $",\nName = \"{PresetName}\"";
        }
    }

    /// <summary>Covers sfPresetBag and sfInstBag</summary>
    public sealed class SF2Bag
    {
        public const uint Size = 4;

        /// <summary>Index in list of generators</summary>
        public ushort GeneratorIndex { get; set; }
        /// <summary>Index in list of modulators</summary>
        public ushort ModulatorIndex { get; set; }

        internal SF2Bag(SF2 inSf2, bool isPresetBag)
        {
            if (isPresetBag)
            {
                GeneratorIndex = (ushort)inSf2.HydraChunk.PGENSubChunk.Count;
                ModulatorIndex = (ushort)inSf2.HydraChunk.PMODSubChunk.Count;
            }
            else
            {
                GeneratorIndex = (ushort)inSf2.HydraChunk.IGENSubChunk.Count;
                ModulatorIndex = (ushort)inSf2.HydraChunk.IMODSubChunk.Count;
            }
        }
        internal SF2Bag(EndianBinaryReader reader)
        {
            GeneratorIndex = reader.ReadUInt16();
            ModulatorIndex = reader.ReadUInt16();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(GeneratorIndex);
            writer.Write(ModulatorIndex);
        }

        public override string ToString()
        {
            return $"Bag - Generator index = {GeneratorIndex}" +
                $",\nModulator index = {ModulatorIndex}";
        }
    }

    /// <summary>Covers sfModList and sfInstModList</summary>
    public sealed class SF2ModulatorList
    {
        public const uint Size = 10;

        public SF2Modulator ModulatorSource { get; set; }
        public SF2Generator ModulatorDestination { get; set; }
        public short ModulatorAmount { get; set; }
        public SF2Modulator ModulatorAmountSource { get; set; }
        public SF2Transform ModulatorTransform { get; set; }

        internal SF2ModulatorList() { }
        internal SF2ModulatorList(EndianBinaryReader reader)
        {
            ModulatorSource = reader.ReadEnum<SF2Modulator>();
            ModulatorDestination = reader.ReadEnum<SF2Generator>();
            ModulatorAmount = reader.ReadInt16();
            ModulatorAmountSource = reader.ReadEnum<SF2Modulator>();
            ModulatorTransform = reader.ReadEnum<SF2Transform>();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(ModulatorSource);
            writer.Write(ModulatorDestination);
            writer.Write(ModulatorAmount);
            writer.Write(ModulatorAmountSource);
            writer.Write(ModulatorTransform);
        }

        public override string ToString()
        {
            return $"Modulator List - Modulator source = {ModulatorSource}" +
                $",\nModulator destination = {ModulatorDestination}" +
                $",\nModulator amount = {ModulatorAmount}" +
                $",\nModulator amount source = {ModulatorAmountSource}" +
                $",\nModulator transform = {ModulatorTransform}";
        }
    }

    public sealed class SF2GeneratorList
    {
        public const uint Size = 4;

        public SF2Generator Generator { get; set; }
        public SF2GeneratorAmount GeneratorAmount { get; set; }

        internal SF2GeneratorList() { }
        internal SF2GeneratorList(SF2Generator generator, SF2GeneratorAmount amount)
        {
            Generator = generator;
            GeneratorAmount = amount;
        }
        internal SF2GeneratorList(EndianBinaryReader reader)
        {
            Generator = reader.ReadEnum<SF2Generator>();
            GeneratorAmount = new SF2GeneratorAmount { Amount = reader.ReadInt16() };
        }

        public void Write(EndianBinaryWriter writer)
        {
            writer.Write(Generator);
            writer.Write(GeneratorAmount.Amount);
        }

        public override string ToString()
        {
            return $"Generator List - Generator = {Generator}" +
                $",\nGenerator amount = \"{GeneratorAmount}\"";
        }
    }

    public sealed class SF2Instrument
    {
        public const uint Size = 22;

        /// <summary>Length 20</summary>
        public string InstrumentName { get; set; }
        public ushort InstrumentBagIndex { get; set; }

        internal SF2Instrument(string name, ushort index)
        {
            InstrumentName = name;
            InstrumentBagIndex = index;
        }
        internal SF2Instrument(EndianBinaryReader reader)
        {
            InstrumentName = reader.ReadString(20, true);
            InstrumentBagIndex = reader.ReadUInt16();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(InstrumentName, 20);
            writer.Write(InstrumentBagIndex);
        }

        public override string ToString()
        {
            return $"Instrument - Name = \"{InstrumentName}\"";
        }
    }

    public sealed class SF2SampleHeader
    {
        public const uint Size = 46;

        /// <summary>Length 20</summary>
        public string SampleName { get; set; }
        public uint Start { get; set; }
        public uint End { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopEnd { get; set; }
        public uint SampleRate { get; set; }
        public byte OriginalKey { get; set; }
        public sbyte PitchCorrection { get; set; }
        public ushort SampleLink { get; set; }
        public SF2SampleLink SampleType { get; set; }

        internal SF2SampleHeader(string name, uint start, uint end, uint loopStart, uint loopEnd, uint sampleRate, byte originalKey, sbyte pitchCorrection)
        {
            SampleName = name;
            Start = start;
            End = end;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            SampleRate = sampleRate;
            OriginalKey = originalKey;
            PitchCorrection = pitchCorrection;
            SampleType = SF2SampleLink.MonoSample;
        }
        internal SF2SampleHeader(EndianBinaryReader reader)
        {
            SampleName = reader.ReadString(20, true);
            Start = reader.ReadUInt32();
            End = reader.ReadUInt32();
            LoopStart = reader.ReadUInt32();
            LoopEnd = reader.ReadUInt32();
            SampleRate = reader.ReadUInt32();
            OriginalKey = reader.ReadByte();
            PitchCorrection = reader.ReadSByte();
            SampleLink = reader.ReadUInt16();
            SampleType = reader.ReadEnum<SF2SampleLink>();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(SampleName, 20);
            writer.Write(Start);
            writer.Write(End);
            writer.Write(LoopStart);
            writer.Write(LoopEnd);
            writer.Write(SampleRate);
            writer.Write(OriginalKey);
            writer.Write(PitchCorrection);
            writer.Write(SampleLink);
            writer.Write(SampleType);
        }

        public override string ToString()
        {
            return $"Sample - Name = \"{SampleName}\"" +
                $",\nType = {SampleType}";
        }
    }

    #region Sub-Chunks

    public sealed class VersionSubChunk : SF2Chunk
    {
        public SF2VersionTag Version { get; set; }

        internal VersionSubChunk(SF2 inSf2, string subChunkName) : base(inSf2, subChunkName)
        {
            Size = SF2VersionTag.Size;
            inSf2.UpdateSize();
        }
        internal VersionSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            Version = new SF2VersionTag(reader);
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            Version.Write(writer);
        }

        public override string ToString()
        {
            return $"Version Chunk - Revision = {Version}";
        }
    }

    public sealed class HeaderSubChunk : SF2Chunk
    {
        public int MaxSize { get; }
        private int _fieldTargetLength;
        private string _field;
        /// <summary>Length <see cref="MaxSize"/></summary>
        public string Field
        {
            get => _field;
            set
            {
                if (value.Length >= MaxSize) // Input too long; cut it down
                {
                    _fieldTargetLength = MaxSize;
                }
                else if (value.Length % 2 == 0) // Even amount of characters
                {
                    _fieldTargetLength = value.Length + 2; // Add two null-terminators to keep the byte count even
                }
                else // Odd amount of characters
                {
                    _fieldTargetLength = value.Length + 1; // Add one null-terminator since that would make byte the count even
                }
                _field = value;
                Size = (uint)_fieldTargetLength;
                _sf2.UpdateSize();
            }
        }

        internal HeaderSubChunk(SF2 inSf2, string subChunkName, int maxSize = 0x100) : base(inSf2, subChunkName)
        {
            MaxSize = maxSize;
        }
        internal HeaderSubChunk(SF2 inSf2, EndianBinaryReader reader, int maxSize = 0x100) : base(inSf2, reader)
        {
            MaxSize = maxSize;
            _field = reader.ReadString((int)Size, true);
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(_field, _fieldTargetLength);
        }

        public override string ToString()
        {
            return $"Header Chunk - Name = \"{ChunkName}\"" +
                $",\nField Max Size = {MaxSize}" +
                $",\nField = \"{Field}\"";
        }
    }

    public sealed class SMPLSubChunk : SF2Chunk
    {
        private readonly List<short> _samples = new List<short>(); // Block of sample data

        internal SMPLSubChunk(SF2 inSf2) : base(inSf2, "smpl") { }
        internal SMPLSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / sizeof(short); i++)
            {
                _samples.Add(reader.ReadInt16());
            }
        }

        // Returns index of the start of the sample
        internal uint AddSample(short[] pcm16, bool bLoop, uint loopPos)
        {
            uint start = (uint)_samples.Count;

            // Write wave
            _samples.AddRange(pcm16);

            // If looping is enabled, write 8 samples from the loop point
            if (bLoop)
            {
                // In case (loopPos + i) is greater than the sample length
                uint max = (uint)pcm16.Length - loopPos;
                for (uint i = 0; i < 8; i++)
                {
                    _samples.Add(pcm16[loopPos + (i % max)]);
                }
            }

            // Write 46 empty samples
            _samples.AddRange(new short[46]);

            Size = (uint)_samples.Count * sizeof(short);
            _sf2.UpdateSize();
            return start;
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            foreach (short s in _samples)
            {
                writer.Write(s);
            }
        }

        public override string ToString()
        {
            return $"Sample Data Chunk";
        }
    }

    public sealed class PHDRSubChunk : SF2Chunk
    {
        private readonly List<SF2PresetHeader> _presets = new List<SF2PresetHeader>();
        public uint Count => (uint)_presets.Count;

        internal PHDRSubChunk(SF2 inSf2) : base(inSf2, "phdr") { }
        internal PHDRSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / SF2PresetHeader.Size; i++)
            {
                _presets.Add(new SF2PresetHeader(reader));
            }
        }

        internal void AddPreset(SF2PresetHeader preset)
        {
            _presets.Add(preset);
            Size = Count * SF2PresetHeader.Size;
            _sf2.UpdateSize();
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            for (int i = 0; i < Count; i++)
            {
                _presets[i].Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Preset Header Chunk - Preset count = {Count}";
        }
    }

    public sealed class INSTSubChunk : SF2Chunk
    {
        private readonly List<SF2Instrument> _instruments = new List<SF2Instrument>();
        public uint Count => (uint)_instruments.Count;

        internal INSTSubChunk(SF2 inSf2) : base(inSf2, "inst") { }
        internal INSTSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / SF2Instrument.Size; i++)
            {
                _instruments.Add(new SF2Instrument(reader));
            }
        }

        internal uint AddInstrument(SF2Instrument instrument)
        {
            _instruments.Add(instrument);
            Size = Count * SF2Instrument.Size;
            _sf2.UpdateSize();
            return Count - 1;
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            for (int i = 0; i < Count; i++)
            {
                _instruments[i].Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Instrument Chunk - Instrument count = {Count}";
        }
    }

    public sealed class BAGSubChunk : SF2Chunk
    {
        private readonly List<SF2Bag> _bags = new List<SF2Bag>();
        public uint Count => (uint)_bags.Count;

        internal BAGSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pbag" : "ibag") { }
        internal BAGSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / SF2Bag.Size; i++)
            {
                _bags.Add(new SF2Bag(reader));
            }
        }

        internal void AddBag(SF2Bag bag)
        {
            _bags.Add(bag);
            Size = Count * SF2Bag.Size;
            _sf2.UpdateSize();
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            for (int i = 0; i < Count; i++)
            {
                _bags[i].Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Bag Chunk - Name = \"{ChunkName}\"" +
                $",\nBag count = {Count}";
        }
    }

    public sealed class MODSubChunk : SF2Chunk
    {
        private readonly List<SF2ModulatorList> _modulators = new List<SF2ModulatorList>();
        public uint Count => (uint)_modulators.Count;

        internal MODSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pmod" : "imod") { }
        internal MODSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / SF2ModulatorList.Size; i++)
            {
                _modulators.Add(new SF2ModulatorList(reader));
            }
        }

        internal void AddModulator(SF2ModulatorList modulator)
        {
            _modulators.Add(modulator);
            Size = Count * SF2ModulatorList.Size;
            _sf2.UpdateSize();
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            for (int i = 0; i < Count; i++)
            {
                _modulators[i].Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Modulator Chunk - Name = \"{ChunkName}\"" +
                $",\nModulator count = {Count}";
        }
    }

    public sealed class GENSubChunk : SF2Chunk
    {
        private readonly List<SF2GeneratorList> _generators = new List<SF2GeneratorList>();
        public uint Count => (uint)_generators.Count;

        internal GENSubChunk(SF2 inSf2, bool preset) : base(inSf2, preset ? "pgen" : "igen") { }
        internal GENSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / SF2GeneratorList.Size; i++)
            {
                _generators.Add(new SF2GeneratorList(reader));
            }
        }

        internal void AddGenerator(SF2GeneratorList generator)
        {
            _generators.Add(generator);
            Size = Count * SF2GeneratorList.Size;
            _sf2.UpdateSize();
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            for (int i = 0; i < Count; i++)
            {
                _generators[i].Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Generator Chunk - Name = \"{ChunkName}\"" +
                $",\nGenerator count = {Count}";
        }
    }

    public sealed class SHDRSubChunk : SF2Chunk
    {
        private readonly List<SF2SampleHeader> _samples = new List<SF2SampleHeader>();
        public uint Count => (uint)_samples.Count;

        internal SHDRSubChunk(SF2 inSf2) : base(inSf2, "shdr") { }
        internal SHDRSubChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            for (int i = 0; i < Size / SF2SampleHeader.Size; i++)
            {
                _samples.Add(new SF2SampleHeader(reader));
            }
        }

        internal uint AddSample(SF2SampleHeader sample)
        {
            _samples.Add(sample);
            Size = Count * SF2SampleHeader.Size;
            _sf2.UpdateSize();
            return Count - 1;
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            for (int i = 0; i < Count; i++)
            {
                _samples[i].Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Sample Header Chunk - Sample header count = {Count}";
        }
    }

    #endregion

    #region Main Chunks

    public sealed class InfoListChunk : SF2ListChunk
    {
        private readonly List<SF2Chunk> _subChunks = new List<SF2Chunk>();
        private const string DefaultEngine = "EMU8000";
        public string Engine
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "isng") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "isng") { Field = DefaultEngine });
                    return DefaultEngine;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "isng") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "isng") { Field = value });
                }
            }
        }

        private const string DefaultBank = "General MIDI";
        public string Bank
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "INAM") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "INAM") { Field = DefaultBank });
                    return DefaultBank;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "INAM") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "INAM") { Field = value });
                }
            }
        }
        public string ROM
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "irom") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "irom") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "irom") { Field = value });
                }
            }
        }
        public SF2VersionTag ROMVersion
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "iver") is VersionSubChunk chunk)
                {
                    return chunk.Version;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "iver") is VersionSubChunk chunk)
                {
                    chunk.Version = value;
                }
                else
                {
                    _subChunks.Add(new VersionSubChunk(_sf2, "iver") { Version = value });
                }
            }
        }
        public string Date
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "ICRD") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "ICRD") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "ICRD") { Field = value });
                }
            }
        }
        public string Designer
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "IENG") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "IENG") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "IENG") { Field = value });
                }
            }
        }
        public string Products
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "IPRD") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "IPRD") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "IPRD") { Field = value });
                }
            }
        }
        public string Copyright
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "ICOP") is HeaderSubChunk icop)
                {
                    return icop.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "ICOP") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "ICOP") { Field = value });
                }
            }
        }

        private const int CommentMaxSize = 0x10000;
        public string Comment
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "ICMT") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "ICMT") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "ICMT", maxSize: CommentMaxSize) { Field = value });
                }
            }
        }
        public string Tools
        {
            get
            {
                if (_subChunks.Find(s => s.ChunkName == "ISFT") is HeaderSubChunk chunk)
                {
                    return chunk.Field;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_subChunks.Find(s => s.ChunkName == "ISFT") is HeaderSubChunk chunk)
                {
                    chunk.Field = value;
                }
                else
                {
                    _subChunks.Add(new HeaderSubChunk(_sf2, "ISFT") { Field = value });
                }
            }
        }

        internal InfoListChunk(SF2 inSf2) : base(inSf2, "INFO")
        {
            // Mandatory sub-chunks
            _subChunks.Add(new VersionSubChunk(inSf2, "ifil") { Version = new SF2VersionTag(2, 1) });
            _subChunks.Add(new HeaderSubChunk(inSf2, "isng") { Field = DefaultEngine });
            _subChunks.Add(new HeaderSubChunk(inSf2, "INAM") { Field = DefaultBank });
            inSf2.UpdateSize();
        }
        internal InfoListChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            long startOffset = reader.BaseStream.Position;
            while (reader.BaseStream.Position < startOffset + Size - 4) // The 4 represents the INFO that was already read
            {
                // Peek 4 chars for the chunk name
                string name = reader.ReadString(4, false);
                reader.BaseStream.Position -= 4;
                switch (name)
                {
                    case "ICMT": _subChunks.Add(new HeaderSubChunk(inSf2, reader, maxSize: CommentMaxSize)); break;
                    case "ifil":
                    case "iver": _subChunks.Add(new VersionSubChunk(inSf2, reader)); break;
                    case "isng":
                    case "INAM":
                    case "ICRD":
                    case "IENG":
                    case "IPRD":
                    case "ICOP":
                    case "ISFT":
                    case "irom": _subChunks.Add(new HeaderSubChunk(inSf2, reader)); break;
                    default: throw new NotSupportedException($"Unsupported chunk name at 0x{reader.BaseStream.Position:X}: \"{name}\"");
                }
            }
        }

        internal override uint UpdateSize()
        {
            Size = 4;
            foreach (SF2Chunk sub in _subChunks)
            {
                Size += sub.Size + 8;
            }

            return Size;
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            foreach (SF2Chunk sub in _subChunks)
            {
                sub.Write(writer);
            }
        }

        public override string ToString()
        {
            return $"Info List Chunk - Sub-chunk count = {_subChunks.Count}";
        }
    }

    public sealed class SdtaListChunk : SF2ListChunk
    {
        public SMPLSubChunk SMPLSubChunk { get; }

        internal SdtaListChunk(SF2 inSf2) : base(inSf2, "sdta")
        {
            SMPLSubChunk = new SMPLSubChunk(inSf2);
            inSf2.UpdateSize();
        }
        internal SdtaListChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            SMPLSubChunk = new SMPLSubChunk(inSf2, reader);
        }

        internal override uint UpdateSize()
        {
            return Size = 4
                + SMPLSubChunk.Size + 8;
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            SMPLSubChunk.Write(writer);
        }

        public override string ToString()
        {
            return $"Sample Data List Chunk";
        }
    }

    public sealed class PdtaListChunk : SF2ListChunk
    {
        public PHDRSubChunk PHDRSubChunk { get; }
        public BAGSubChunk PBAGSubChunk { get; }
        public MODSubChunk PMODSubChunk { get; }
        public GENSubChunk PGENSubChunk { get; }
        public INSTSubChunk INSTSubChunk { get; }
        public BAGSubChunk IBAGSubChunk { get; }
        public MODSubChunk IMODSubChunk { get; }
        public GENSubChunk IGENSubChunk { get; }
        public SHDRSubChunk SHDRSubChunk { get; }

        internal PdtaListChunk(SF2 inSf2) : base(inSf2, "pdta")
        {
            PHDRSubChunk = new PHDRSubChunk(inSf2);
            PBAGSubChunk = new BAGSubChunk(inSf2, true);
            PMODSubChunk = new MODSubChunk(inSf2, true);
            PGENSubChunk = new GENSubChunk(inSf2, true);
            INSTSubChunk = new INSTSubChunk(inSf2);
            IBAGSubChunk = new BAGSubChunk(inSf2, false);
            IMODSubChunk = new MODSubChunk(inSf2, false);
            IGENSubChunk = new GENSubChunk(inSf2, false);
            SHDRSubChunk = new SHDRSubChunk(inSf2);
            inSf2.UpdateSize();
        }
        internal PdtaListChunk(SF2 inSf2, EndianBinaryReader reader) : base(inSf2, reader)
        {
            PHDRSubChunk = new PHDRSubChunk(inSf2, reader);
            PBAGSubChunk = new BAGSubChunk(inSf2, reader);
            PMODSubChunk = new MODSubChunk(inSf2, reader);
            PGENSubChunk = new GENSubChunk(inSf2, reader);
            INSTSubChunk = new INSTSubChunk(inSf2, reader);
            IBAGSubChunk = new BAGSubChunk(inSf2, reader);
            IMODSubChunk = new MODSubChunk(inSf2, reader);
            IGENSubChunk = new GENSubChunk(inSf2, reader);
            SHDRSubChunk = new SHDRSubChunk(inSf2, reader);
        }

        internal override uint UpdateSize()
        {
            return Size = 4
                + PHDRSubChunk.Size + 8
                + PBAGSubChunk.Size + 8
                + PMODSubChunk.Size + 8
                + PGENSubChunk.Size + 8
                + INSTSubChunk.Size + 8
                + IBAGSubChunk.Size + 8
                + IMODSubChunk.Size + 8
                + IGENSubChunk.Size + 8
                + SHDRSubChunk.Size + 8;
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            PHDRSubChunk.Write(writer);
            PBAGSubChunk.Write(writer);
            PMODSubChunk.Write(writer);
            PGENSubChunk.Write(writer);
            INSTSubChunk.Write(writer);
            IBAGSubChunk.Write(writer);
            IMODSubChunk.Write(writer);
            IGENSubChunk.Write(writer);
            SHDRSubChunk.Write(writer);
        }

        public override string ToString()
        {
            return $"Hydra List Chunk";
        }
    }

    #endregion
}
