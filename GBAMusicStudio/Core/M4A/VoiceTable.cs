using System;
using static GBAMusicStudio.Core.M4A.M4AStructs;

namespace GBAMusicStudio.Core.M4A
{
    internal class VoiceTable
    {
        readonly SVoice[] voices;
        internal SVoice this[int i]
        {
            get => voices[i];
            private set => voices[i] = value;
        }

        void LoadDirect(Direct_Sound direct)
        {
            if (direct.Address == 0 || !ROM.IsValidRomOffset(direct.Address) || MusicPlayer.Sounds.ContainsKey(direct.Address)) return;
            Sample s = ROM.Instance.ReadStruct<Sample>(direct.Address);
            if (s.Length == 0 || s.Length >= 0x1000000) return; // Invalid lengths
            var buf = new byte[s.Length];
            Buffer.BlockCopy(ROM.Instance.ReadBytes(s.Length, direct.Address + 0x10), 0, buf, 0, (int)s.Length);
            for (int i = 0; i < s.Length; i++)
                buf[i] ^= 0x80; // Convert from s8 to u8
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = (int)s.Frequency / 1024,
                format = FMOD.SOUND_FORMAT.PCM8,
                length = s.Length,
                numchannels = 1
            };
            if (MusicPlayer.System.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd) != FMOD.RESULT.OK)
                return;
            if (s.DoesLoop != 0)
            {
                snd.setLoopPoints(s.LoopPoint, FMOD.TIMEUNIT.PCM, s.Length, FMOD.TIMEUNIT.PCM);
                snd.setMode(FMOD.MODE.LOOP_NORMAL);
            }
            else
            {
                snd.setLoopCount(0);
            }
            MusicPlayer.Sounds.Add(direct.Address, snd);
        }
        void LoadWave(PSG_Wave wave)
        {
            if (wave.Address == 0 || MusicPlayer.Sounds.ContainsKey(wave.Address)) return;
            
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
            MusicPlayer.System.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL, ref ex, out FMOD.Sound snd);
            MusicPlayer.Sounds.Add(wave.Address, snd);
        }

        internal VoiceTable() => voices = new SVoice[256]; // It is possible to play notes outside of the 128 MIDI standard
        internal void Load(uint table)
        {
            for (uint i = 0; i < 256; i++)
            {
                uint offset = table + (i * 0xC);
                if (!ROM.IsValidRomOffset(offset))
                    break;
                switch (ROM.Instance.ReadByte(offset)) // Check type
                {
                    case 0x0:
                    case 0x8:
                        var direct = ROM.Instance.ReadStruct<Direct_Sound>(offset);
                        voices[i] = new SVoice(direct);
                        LoadDirect(direct);
                        break;
                    case 0x1:
                    case 0x9:
                        voices[i] = new SVoice(ROM.Instance.ReadStruct<PSG_Square_1>(offset));
                        break;
                    case 0x2:
                    case 0xA:
                        voices[i] = new SVoice(ROM.Instance.ReadStruct<PSG_Square_2>(offset));
                        break;
                    case 0x3:
                    case 0xB:
                        var wave = ROM.Instance.ReadStruct<PSG_Wave>(offset);
                        voices[i] = new SVoice(wave);
                        LoadWave(wave);
                        break;
                    case 0x4:
                    case 0xC:
                        voices[i] = new SVoice(ROM.Instance.ReadStruct<PSG_Noise>(offset));
                        break;
                    case 0x40:
                        var keySplit = ROM.Instance.ReadStruct<Split>(offset);
                        var multi = new SMulti(keySplit);
                        voices[i] = multi;
                        if (!ROM.IsValidRomOffset(keySplit.Table) || !ROM.IsValidRomOffset(keySplit.Keys))
                            break;

                        for (uint j = 0; j < 256; j++)
                        {
                            byte key = ROM.Instance.ReadByte(keySplit.Keys + j);
                            if (key > 0x7F) continue;
                            uint mOffset = keySplit.Table + (uint)(key * 0xC);
                            switch (ROM.Instance.ReadByte(mOffset)) // Check type
                            {
                                case 0x0:
                                case 0x8:
                                    var ds = ROM.Instance.ReadStruct<Direct_Sound>(mOffset);
                                    multi.Table[key] = new SVoice(ds);
                                    LoadDirect(ds);
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
                        voices[i] = new SDrum(ROM.Instance.ReadStruct<Drum>(offset));
                        break;
                }
            }
        }

        // The following should only be called after Load()
        internal Voice GetVoiceFromNote(byte voice, byte note, out byte forcedNote)
        {
            forcedNote = note;

            SVoice sv = voices[voice];
            Voice v = voices[voice].Voice;
            Read:
            switch (v.Type)
            {
                case 0x40:
                    var split = (Split)v;
                    var multi = (SMulti)sv;
                    byte inst = ROM.Instance.ReadByte(split.Keys + note);
                    v = multi.Table[inst].Voice;
                    forcedNote = note; // In case there is a multi within a drum
                    goto Read;
                case 0x80:
                    var drum = (SDrum)sv;
                    v = drum.Table[note].Voice;
                    forcedNote = 60;
                    goto Read;
                default:
                    return v;
            }
        }
        internal FMOD.Sound GetSoundFromNote(byte voice, byte note)
        {
            Voice v = GetVoiceFromNote(voice, note, out byte idc);
            switch (v.Type)
            {
                case 0x0:
                case 0x8:
                    var direct = v as Direct_Sound;
                    return MusicPlayer.Sounds[direct.Address];
                case 0x1:
                case 0x2:
                case 0x9:
                case 0xA:
                    dynamic dyn = v;
                    return MusicPlayer.Sounds[MusicPlayer.SQUARE12_ID - dyn.Pattern];
                case 0x3:
                case 0xB:
                    var wave = v as PSG_Wave;
                    return MusicPlayer.Sounds[wave.Address];
                case 0x4:
                case 0xC:
                    var noise = v as PSG_Noise;
                    return MusicPlayer.Sounds[MusicPlayer.NOISE0_ID - noise.Pattern];
                default:
                    return null; // Will not occur
            }
        }
    }
}
