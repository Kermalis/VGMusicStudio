namespace GBAMusicStudio.Core
{
    internal abstract class VoiceTable
    {
        public uint Offset { get; protected set; }
        protected readonly SVoice[] voices;

        internal VoiceTable() => voices = new SVoice[256]; // It is possible to play notes outside of the 128 MIDI standard
        internal abstract void Load(uint table);

        internal SVoice this[int i]
        {
            get => voices[i];
            set => voices[i] = value;
        }

        // The following should only be called after Load()
        internal abstract SVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum);
    }

    internal class M4AVoiceTable : VoiceTable
    {
        internal override void Load(uint table)
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
                    case 0x1:
                    case 0x9:
                        voices[i] = new SVoice(voice);
                        break;
                    case 0x2:
                    case 0xA:
                        voices[i] = new SVoice(voice);
                        break;
                    case 0x3:
                    case 0xB:
                        voices[i] = new M4ASWave(voice);
                        break;
                    case 0x4:
                    case 0xC:
                        voices[i] = new SVoice(voice);
                        break;
                    case 0x40:
                        var multi = new M4ASMulti(voice);
                        voices[i] = multi;
                        if (!ROM.IsValidRomOffset(voice.Table) || !ROM.IsValidRomOffset(voice.Keys))
                            break;

                        var keys = ROM.Instance.ReadBytes(256, voice.Keys);
                        for (uint j = 0; j < 256; j++)
                        {
                            byte key = keys[j];
                            if (key > 0x7F) continue;
                            uint mOffset = voice.Table + (uint)(key * 0xC);
                            var subVoice = ROM.Instance.ReadStruct<M4AVoice>(mOffset);
                            switch (subVoice.Type) // Check type
                            {
                                case 0x0:
                                case 0x8:
                                    multi.Table[key] = new M4ASDirect(subVoice);
                                    break;
                                case 0x1:
                                case 0x9:
                                    multi.Table[key] = new SVoice(subVoice);
                                    break;
                                case 0x2:
                                case 0xA:
                                    multi.Table[key] = new SVoice(subVoice);
                                    break;
                                case 0x3:
                                case 0xB:
                                    multi.Table[key] = new M4ASWave(subVoice);
                                    break;
                                case 0x4:
                                case 0xC:
                                    multi.Table[key] = new SVoice(subVoice);
                                    break;
                            }
                        }
                        break;
                    case 0x80:
                        voices[i] = new M4ASDrum(voice);
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

        internal override void Load(uint table)
        {
            Offset = 0x21D1CC; // Voice table maybe?

            for (int i = 0; i < 256; i++)
            {
                var off = ROM.Instance.ReadUInt16((uint)(Offset + (i * 2)));
                voices[i] = new SVoice(ROM.Instance.ReadStruct<MLSSVoice>(Offset + off));
            }

            Offset = 0xA806B8; // Sample table
            for (uint i = 0; i < sampleCount; i++)
            {
                int off = ROM.Instance.ReadInt32(Offset + (i * 4));
                Samples[i] = (off == 0) ? null : new MLSSSSample((uint)(Offset + off));
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
