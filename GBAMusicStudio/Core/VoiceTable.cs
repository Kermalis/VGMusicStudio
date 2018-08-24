using System;
using System.Collections;
using System.Collections.Generic;

namespace GBAMusicStudio.Core
{
    internal abstract class VoiceTable : IEnumerable<WrappedVoice>
    {
        static readonly Dictionary<uint, VoiceTable> cache = new Dictionary<uint, VoiceTable>();
        internal static T LoadTable<T>(uint table, bool shouldCache = false) where T : VoiceTable
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
        internal static void ClearCache() => cache.Clear();

        public uint Offset { get; protected set; }
        protected readonly WrappedVoice[] voices;

        internal VoiceTable(int capacity = 256) // It is possible to play notes outside of the 128 MIDI standard
        {
            voices = new WrappedVoice[capacity];
        }
        protected abstract void Load(uint table);

        protected internal WrappedVoice this[int i]
        {
            get => voices[i];
            protected set => voices[i] = value;
        }
        public IEnumerator<WrappedVoice> GetEnumerator() => ((IEnumerable<WrappedVoice>)voices).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        internal virtual WrappedVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;
            return voices[voice];
        }
    }

    internal class M4AVoiceTable : VoiceTable
    {
        protected override void Load(uint table)
        {
            Offset = table;
            for (uint i = 0; i < 256; i++)
            {
                uint off = table + (i * 0xC);
                if (!ROM.IsValidRomOffset(off))
                    break;
                var voice = new M4AVoice(off);
                switch (voice.Entry.Type)
                {
                    case 0x0:
                    case 0x8:
                        voices[i] = new M4AWrappedDirect(voice);
                        break;
                    case 0x40:
                        voices[i] = new M4AWrappedMulti(voice);
                        break;
                    case 0x80:
                        voices[i] = new M4AWrappedDrum(voice);
                        break;
                    default:
                        voices[i] = new WrappedVoice(voice);
                        break;
                }
            }
        }

        internal override WrappedVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;

            WrappedVoice sv = voices[voice];
            Read:
            M4AVoiceEntry v = ((M4AVoice)sv.Voice).Entry;
            switch (v.Type)
            {
                case 0x40:
                    var multi = (M4AWrappedMulti)sv;
                    byte inst = ROM.Instance.ReadByte((uint)(v.Keys + note));
                    sv = multi.Table[inst];
                    fromDrum = false; // In case there is a multi within a drum
                    goto Read;
                case 0x80:
                    var drum = (M4AWrappedDrum)sv;
                    sv = drum.Table[note];
                    fromDrum = true;
                    goto Read;
                default:
                    return sv;
            }
        }
    }

    internal class MLSSVoiceTable : VoiceTable
    {
        internal MLSSWrappedSample[] Samples { get; private set; }

        protected override void Load(uint table)
        {
            uint sampleCount = ROM.Instance.Game.SampleTableSize;

            Samples = new MLSSWrappedSample[sampleCount];
            Offset = ROM.Instance.Game.VoiceTable;

            for (int i = 0; i < 256; i++)
            {
                var off = ROM.Instance.ReadInt16((uint)(Offset + (i * 2)));
                var nextOff = ROM.Instance.ReadInt16((uint)(Offset + ((i + 1) * 2)));
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
