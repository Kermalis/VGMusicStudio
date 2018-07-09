using System;
using static GBAMusicStudio.Core.M4AStructs;

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
        internal abstract IVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum);
        internal abstract FMOD.Sound GetSoundFromNote(byte voice, sbyte note);
    }

    internal class M4AVoiceTable : VoiceTable
    {
        void LoadDirect(SDirect direct)
        {
            var d = direct.Voice as Direct_Sound;
            var s = direct.Sample;

            if (SongPlayer.Sounds.ContainsKey(d.Address))
                return;
            if (d.Address == 0 || !ROM.IsValidRomOffset(d.Address - ROM.Pak))
                goto fail;
            if (s.Length == 0 || s.Length >= 0x1000000 || !ROM.IsValidRomOffset(s.Length + (d.Address + 0x10) - ROM.Pak)) // Invalid lengths
                goto fail;
            var buf = new byte[s.Length];
            var a = ROM.Instance.ReadBytes(s.Length, d.Address + 0x10);
            Buffer.BlockCopy(a, 0, buf, 0, (int)s.Length);
            for (int i = 0; i < s.Length; i++)
                buf[i] ^= 0x80; // Convert from s8 to u8
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = (int)s.Frequency / 1024,
                format = FMOD.SOUND_FORMAT.PCM8,
                length = s.Length,
                numchannels = 1
            };
            if (SongPlayer.System.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd) != FMOD.RESULT.OK)
                goto fail;
            if (s.DoesLoop != 0)
            {
                snd.setLoopPoints(s.LoopPoint, FMOD.TIMEUNIT.PCM, s.Length, FMOD.TIMEUNIT.PCM);
                snd.setMode(FMOD.MODE.LOOP_NORMAL);
            }
            else
            {
                snd.setLoopCount(0);
            }
            SongPlayer.Sounds.Add(d.Address, snd);
            return;

            fail:
            Console.WriteLine("Error loading instrument: 0x{0:X}", d.Address);
            return;
        }
        void LoadWave(PSG_Wave wave)
        {
            if (wave.Address == 0 || SongPlayer.Sounds.ContainsKey(wave.Address)) return;

            var buf = new byte[32];
            for (uint i = 0; i < 16; i++)
            {
                byte b = ROM.Instance.ReadByte(wave.Address + i);
                byte first = (byte)((b >> 4) * Config.PSGVolume); // Convert from u4 to u8
                byte second = (byte)((b & 0xF) * Config.PSGVolume);
                buf[i * 2] = first;
                buf[i * 2 + 1] = second;
            }
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = 7040,
                format = FMOD.SOUND_FORMAT.PCM8,
                length = 32,
                numchannels = 1
            };
            SongPlayer.System.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL, ref ex, out FMOD.Sound snd);
            SongPlayer.Sounds.Add(wave.Address, snd);
        }

        internal override void Load(uint table)
        {
            Offset = table;
            for (uint i = 0; i < 256; i++)
            {
                uint off = table + (i * 0xC);
                if (!ROM.IsValidRomOffset(off))
                    break;
                switch (ROM.Instance.ReadByte(off)) // Check type
                {
                    case 0x0:
                    case 0x8:
                        var d = ROM.Instance.ReadStruct<Direct_Sound>(off);
                        var direct = new SDirect(d);
                        voices[i] = direct;
                        LoadDirect(direct);
                        break;
                    case 0x1:
                    case 0x9:
                        voices[i] = new SVoice(ROM.Instance.ReadStruct<PSG_Square_1>(off));
                        break;
                    case 0x2:
                    case 0xA:
                        voices[i] = new SVoice(ROM.Instance.ReadStruct<PSG_Square_2>(off));
                        break;
                    case 0x3:
                    case 0xB:
                        var wave = ROM.Instance.ReadStruct<PSG_Wave>(off);
                        voices[i] = new SVoice(wave);
                        LoadWave(wave);
                        break;
                    case 0x4:
                    case 0xC:
                        voices[i] = new SVoice(ROM.Instance.ReadStruct<PSG_Noise>(off));
                        break;
                    case 0x40:
                        var keySplit = ROM.Instance.ReadStruct<Split>(off);
                        var multi = new SMulti(keySplit);
                        voices[i] = multi;
                        if (!ROM.IsValidRomOffset(keySplit.Table) || !ROM.IsValidRomOffset(keySplit.Keys))
                            break;

                        var keys = ROM.Instance.ReadBytes(256, keySplit.Keys);
                        for (uint j = 0; j < 256; j++)
                        {
                            byte key = keys[j];
                            if (key > 0x7F) continue;
                            uint mOffset = keySplit.Table + (uint)(key * 0xC);
                            switch (ROM.Instance.ReadByte(mOffset)) // Check type
                            {
                                case 0x0:
                                case 0x8:
                                    var ds = ROM.Instance.ReadStruct<Direct_Sound>(mOffset);
                                    var directsound = new SDirect(ds);
                                    multi.Table[key] = directsound;
                                    LoadDirect(directsound);
                                    break;
                                case 0x1:
                                case 0x9:
                                    multi.Table[key] = new SVoice(ROM.Instance.ReadStruct<PSG_Square_1>(mOffset));
                                    break;
                                case 0x2:
                                case 0xA:
                                    multi.Table[key] = new SVoice(ROM.Instance.ReadStruct<PSG_Square_2>(mOffset));
                                    break;
                                case 0x3:
                                case 0xB:
                                    var wv = ROM.Instance.ReadStruct<PSG_Wave>(mOffset);
                                    multi.Table[key] = new SVoice(wv);
                                    LoadWave(wv);
                                    break;
                                case 0x4:
                                case 0xC:
                                    multi.Table[key] = new SVoice(ROM.Instance.ReadStruct<PSG_Noise>(mOffset));
                                    break;
                            }
                        }
                        break;
                    case 0x80:
                        voices[i] = new SDrum(ROM.Instance.ReadStruct<Drum>(off));
                        break;
                }
            }
        }

        internal override IVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;

            SVoice sv = voices[voice];
            M4AVoice v = (M4AVoice)voices[voice].Voice;
            Read:
            switch (v.Type)
            {
                case 0x40:
                    var split = (Split)v;
                    var multi = (SMulti)sv;
                    byte inst = ROM.Instance.ReadByte((uint)(split.Keys + note));
                    v = (M4AVoice)multi.Table[inst].Voice;
                    fromDrum = false; // In case there is a multi within a drum
                    goto Read;
                case 0x80:
                    var drum = (SDrum)sv;
                    v = (M4AVoice)drum.Table[note].Voice;
                    fromDrum = true;
                    goto Read;
                default:
                    return v;
            }
        }
        internal override FMOD.Sound GetSoundFromNote(byte voice, sbyte note)
        {
            M4AVoice v = (M4AVoice)GetVoiceFromNote(voice, note, out bool idc);
            switch (v.Type)
            {
                case 0x0:
                case 0x8:
                    var direct = v as Direct_Sound;
                    return SongPlayer.Sounds[direct.Address];
                case 0x1:
                case 0x2:
                case 0x9:
                case 0xA:
                    dynamic dyn = v;
                    return SongPlayer.Sounds[SongPlayer.SQUARE12_ID - dyn.Pattern];
                case 0x3:
                case 0xB:
                    var wave = v as PSG_Wave;
                    return SongPlayer.Sounds[wave.Address];
                case 0x4:
                case 0xC:
                    var noise = v as PSG_Noise;
                    return SongPlayer.Sounds[SongPlayer.NOISE0_ID - noise.Pattern];
                default:
                    return null; // Will not occur
            }
        }
    }

    internal class MLSSVoiceTable : VoiceTable
    {
        PSG_Square_1 temp = new PSG_Square_1 { S = 15, RootNote = 60 };

        internal override void Load(uint table)
        {
            for (int i = 0; i < 256; i++)
                voices[i] = new SVoice(temp);
        }

        internal override FMOD.Sound GetSoundFromNote(byte voice, sbyte note)
        {
            return SongPlayer.Sounds[SongPlayer.SQUARE12_ID];
        }
        internal override IVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
        {
            fromDrum = false;
            return temp;
        }
    }
}
