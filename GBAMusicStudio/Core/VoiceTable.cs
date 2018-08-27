using System;
using System.Collections;
using System.Collections.Generic;

namespace GBAMusicStudio.Core
{
    abstract class VoiceTable : IEnumerable<WrappedVoice>
    {
        static readonly Dictionary<uint, VoiceTable> cache = new Dictionary<uint, VoiceTable>();
        public static T LoadTable<T>(uint table, bool shouldCache = false) where T : VoiceTable
        {
            table = ROM.SanitizeOffset(table);
            if (cache.ContainsKey(table))
            {
                return (T)cache[table];
            }
            else
            {
                T vTable = Activator.CreateInstance<T>();
                if (shouldCache)
                    cache.Add(table, vTable);
                vTable.Load(table);
                return vTable;
            }
        }
        public static void ClearCache() => cache.Clear();

        public uint Offset { get; protected set; }
        protected uint Length { get; private set; }
        protected readonly WrappedVoice[] voices;

        public VoiceTable(uint capacity) => voices = new WrappedVoice[Length = capacity];
        protected abstract void Load(uint table);

        public WrappedVoice this[int i]
        {
            get => voices[i];
            protected set => voices[i] = value;
        }
        public IEnumerator<WrappedVoice> GetEnumerator() => ((IEnumerable<WrappedVoice>)voices).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual WrappedVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;
            return voices[voice];
        }
    }

    class M4AVoiceTable : VoiceTable
    {
        public M4AVoiceTable() : base(Config.All256Voices ? 256u : 128u) { }
        protected override void Load(uint table)
        {
            Offset = table;
            for (uint i = 0; i < Length; i++)
            {
                uint off = table + (i * 0xC);
                if (!ROM.IsValidRomOffset(off))
                    break;
                var voice = new M4AVoice(off);
                if (voice.Entry.Type == (int)M4AVoiceFlags.KeySplit)
                    voices[i] = new M4AWrappedKeySplit(voice);
                else if (voice.Entry.Type == (int)M4AVoiceFlags.Drum)
                    voices[i] = new M4AWrappedDrum(voice);
                else if ((voice.Entry.Type & 0x7) == (int)M4AVoiceType.Direct)
                    voices[i] = new M4AWrappedDirect(voice);
                else
                    voices[i] = new WrappedVoice(voice);
            }
        }

        public override WrappedVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;

            WrappedVoice sv = voices[voice];
            Read:
            M4AVoiceEntry v = ((M4AVoice)sv.Voice).Entry;
            switch (v.Type)
            {
                case (int)M4AVoiceFlags.KeySplit:
                    var keySplit = (M4AWrappedKeySplit)sv;
                    byte inst = ROM.Instance.ReadByte((uint)(v.Keys + note));
                    sv = keySplit.Table[inst];
                    fromDrum = false; // In case there is a multi within a drum
                    goto Read;
                case (int)M4AVoiceFlags.Drum:
                    var drum = (M4AWrappedDrum)sv;
                    sv = drum.Table[note];
                    fromDrum = true;
                    goto Read;
                default:
                    return sv;
            }
        }
    }

    class MLSSVoiceTable : VoiceTable
    {
        public MLSSWrappedSample[] Samples { get; private set; }

        public MLSSVoiceTable() : base(256) { }
        protected override void Load(uint table)
        {
            uint sampleCount = ROM.Instance.Game.SampleTableSize;

            Samples = new MLSSWrappedSample[sampleCount];
            Offset = ROM.Instance.Game.VoiceTable;

            for (uint i = 0; i < 256; i++)
            {
                var off = ROM.Instance.ReadInt16(Offset + (i * 2));
                var nextOff = ROM.Instance.ReadInt16(Offset + ((i + 1) * 2));
                uint numEntries = (uint)(nextOff - off) / 8; // Each entry is 8 bytes
                voices[i] = new MLSSWrappedVoice((uint)(Offset + off), numEntries);
            }

            uint sOffset = ROM.Instance.Game.SampleTable;
            for (uint i = 0; i < sampleCount; i++)
            {
                int off = ROM.Instance.ReadInt32(sOffset + (i * 4));
                Samples[i] = (off == 0) ? null : new MLSSWrappedSample((uint)(sOffset + off));
            }
        }
    }
}
