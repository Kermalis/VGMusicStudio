using System;
using System.Collections.Generic;

namespace GBAMusicStudio.Core
{
    internal abstract class VoiceTable
    {
        static readonly Dictionary<uint, VoiceTable> cache = new Dictionary<uint, VoiceTable>();
        internal static T LoadTable<T>(uint table) where T : VoiceTable
        {
            if (cache.ContainsKey(table))
            {
                return (T)cache[table];
            }
            else
            {
                T vTable = Activator.CreateInstance<T>();
                cache.Add(table, vTable);
                vTable.Load(table);
                return vTable;
            }
        }
        internal static void ClearCache() => cache.Clear();

        protected internal uint Offset { get; protected set; }
        protected readonly SVoice[] voices;

        internal VoiceTable() => voices = new SVoice[256]; // It is possible to play notes outside of the 128 MIDI standard
        protected abstract void Load(uint table);

        protected internal SVoice this[int i]
        {
            get => voices[i];
            protected set => voices[i] = value;
        }

        // The following should only be called after Load()
        internal abstract SVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum);
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
                var voice = ROM.Instance.ReadStruct<M4AVoice>(off);
                switch (voice.Type) // Check type
                {
                    case 0x0:
                    case 0x8:
                        voices[i] = new M4ASDirect(voice);
                        break;
                    case 0x3:
                    case 0xB:
                        voices[i] = new M4ASWave(voice);
                        break;
                    case 0x40:
                        voices[i] = new M4ASMulti(voice);
                        break;
                    case 0x80:
                        voices[i] = new M4ASDrum(voice);
                        break;
                    default:
                        voices[i] = new SVoice(voice);
                        break;
                }
            }
        }

        internal override SVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;

            SVoice sv = voices[voice];
            Read:
            M4AVoice v = (M4AVoice)sv.Voice;
            switch (v.Type)
            {
                case 0x40:
                    var multi = (M4ASMulti)sv;
                    byte inst = ROM.Instance.ReadByte((uint)(v.Keys + note));
                    sv = multi.Table[inst];
                    fromDrum = false; // In case there is a multi within a drum
                    goto Read;
                case 0x80:
                    var drum = (M4ASDrum)sv;
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
        const uint sampleCount = 235;
        internal readonly MLSSSSample[] Samples = new MLSSSSample[sampleCount];

        protected override void Load(uint table)
        {
            Offset = 0x21D1CC; // Voice table

            for (int i = 0; i < 256; i++)
            {
                var off = ROM.Instance.ReadInt16((uint)(Offset + (i * 2)));
                var nextOff = ROM.Instance.ReadInt16((uint)(Offset + ((i + 1) * 2)));
                uint numEntries = (uint)(nextOff - off) / 8; // Each entry is 8 bytes
                voices[i] = new SVoice(new MLSSVoice((uint)(Offset + off), numEntries));
            }

            uint sOffset = 0xA806B8; // Sample table
            for (uint i = 0; i < sampleCount; i++)
            {
                int off = ROM.Instance.ReadInt32(sOffset + (i * 4));
                Samples[i] = (off == 0) ? null : new MLSSSSample((uint)(sOffset + off));
            }
            ;
        }

        internal override SVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;
            return voices[voice];
        }
    }
}
