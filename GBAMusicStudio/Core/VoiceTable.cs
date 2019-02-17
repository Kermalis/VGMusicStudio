using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.GBAMusicStudio.Core
{
    abstract class VoiceTable : IEnumerable<WrappedVoice>, IOffset
    {
        static readonly Dictionary<int, VoiceTable> cache = new Dictionary<int, VoiceTable>();
        public static T LoadTable<T>(int table, bool shouldCache = false) where T : VoiceTable
        {
            if (cache.ContainsKey(table))
            {
                return (T)cache[table];
            }
            else
            {
                T vTable = Activator.CreateInstance<T>();
                if (shouldCache)
                {
                    cache.Add(table, vTable);
                }
                vTable.Load(table);
                return vTable;
            }
        }
        public static void ClearCache() => cache.Clear();

        protected int offset;
        public int Length { get; private set; }
        protected readonly WrappedVoice[] voices;

        public VoiceTable(int capacity) => voices = new WrappedVoice[Length = capacity];
        protected abstract void Load(int table);

        public int GetOffset() => offset;
        public void SetOffset(int newOffset) => offset = newOffset;

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
        public M4AVoiceTable() : base(Config.Instance.All256Voices ? 256 : 128) { }
        protected override void Load(int table)
        {
            SetOffset(table);
            for (int i = 0; i < Length; i++)
            {
                int off = table + (i * 0xC);
                if (!ROM.IsValidRomOffset(off))
                {
                    break;
                }
                M4AVoiceEntry voice = ROM.Instance.Reader.ReadObject<M4AVoiceEntry>(off);
                voice.SetOffset(off);
                if (voice.Type == (int)M4AVoiceFlags.KeySplit)
                {
                    voices[i] = new M4AWrappedKeySplit(voice);
                }
                else if (voice.Type == (int)M4AVoiceFlags.Drum)
                {
                    voices[i] = new M4AWrappedDrum(voice);
                }
                else if ((voice.Type & 0x7) == (int)M4AVoiceType.Direct)
                {
                    voices[i] = new M4AWrappedDirect(voice);
                }
                else
                {
                    voices[i] = new WrappedVoice(voice);
                }
            }
        }

        public override WrappedVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;

            WrappedVoice sv = voices[voice];
        Read:
            M4AVoiceEntry v = (M4AVoiceEntry)sv.Voice;
            switch (v.Type)
            {
                case (int)M4AVoiceFlags.KeySplit:
                    {
                        fromDrum = false; // In case there is a multi within a drum
                        var keySplit = (M4AWrappedKeySplit)sv;
                        byte inst = ROM.Instance.Reader.ReadByte(v.Keys - ROM.Pak + note);
                        sv = keySplit.Table[inst];
                        goto Read;
                    }
                case (int)M4AVoiceFlags.Drum:
                    {
                        fromDrum = true;
                        var drum = (M4AWrappedDrum)sv;
                        sv = drum.Table[note];
                        goto Read;
                    }
                default: return sv;
            }
        }
    }

    class MLSSVoiceTable : VoiceTable
    {
        public MLSSWrappedSample[] Samples { get; private set; }

        public MLSSVoiceTable() : base(256) { }
        protected override void Load(int table)
        {
            int sampleCount = ROM.Instance.Game.SampleTableSize;

            Samples = new MLSSWrappedSample[sampleCount];
            SetOffset(ROM.SanitizeOffset(ROM.Instance.Game.VoiceTable));

            for (int i = 0; i < 256; i++)
            {
                short off = ROM.Instance.Reader.ReadInt16(offset + (i * 2));
                short nextOff = ROM.Instance.Reader.ReadInt16(offset + ((i + 1) * 2));
                int numEntries = (nextOff - off) / 8; // Each entry is 8 bytes
                voices[i] = new MLSSWrappedVoice(offset + off, numEntries);
            }

            int sOffset = ROM.SanitizeOffset(ROM.Instance.Game.SampleTable);
            for (int i = 0; i < sampleCount; i++)
            {
                int off = ROM.Instance.Reader.ReadInt32(sOffset + (i * 4));
                Samples[i] = (off == 0) ? null : new MLSSWrappedSample(sOffset + off);
            }
        }
    }
}
