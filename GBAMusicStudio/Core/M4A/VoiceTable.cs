using System;
using System.Collections.Generic;
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

        void LoadDirect(Direct_Sound direct, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds)
        {
            if (direct.Address == 0 || !ROM.IsValidRomOffset(direct.Address) || sounds.ContainsKey(direct.Address)) return;
            Sample s = ROM.Instance.ReadStruct<Sample>(direct.Address);
            if (s.Length == 0 || s.Length >= 0x1000000) return; // Invalid lengths
            var buf = new byte[16 + s.Length + 16]; // FMOD API requires 16 bytes of padding on each side
            Buffer.BlockCopy(ROM.Instance.ReadBytes(s.Length, direct.Address + 0x10), 0, buf, 16, (int)s.Length);
            for (int i = 0; i < s.Length; i++)
                buf[i + 16] ^= 0x80; // Convert from u8 to s8
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = (int)s.Frequency / 1024,
                format = FMOD.SOUND_FORMAT.PCM8,
                length = s.Length,
                numchannels = 1
            };
            system.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd);
            if (s.DoesLoop != 0)
            {
                snd.setLoopPoints(s.LoopPoint, FMOD.TIMEUNIT.PCM, s.Length, FMOD.TIMEUNIT.PCM);
                snd.setMode(FMOD.MODE.LOOP_NORMAL);
            }
            else
            {
                snd.setLoopCount(0);
            }
            sounds.Add(direct.Address, snd);
        }
        void LoadWave(GB_Wave wave, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds)
        {
            if (wave.Address == 0 || sounds.ContainsKey(wave.Address)) return;
            uint rept = 4;
            uint byteLen = 16 * rept * 2 * 2;
            var buf16 = new short[byteLen / 2];
            for (uint i = 0, j = 0; i < 16; i++)
            {
                byte b = ROM.Instance.ReadByte(wave.Address + i);

                short[] simple = { -0x4000, -0x3800, -0x3000, -0x2800, -0x2000, -0x1800, -0x0100, -0x0800,
                        0x0000, 0x0800, 0x1000, 0x1800, 0x2000, 0x2800, 0x3000, 0x3800 };

                for (int k = 0; k < rept; k++, j++)
                    buf16[j] = simple[b >> 4];
                for (int k = 0; k < rept; k++, j++)
                    buf16[j] = simple[b & 0xF];
            }
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = (int)(22050 * Math.Pow(2, (-14 / 12f))), // Still trying to figure it out
                format = FMOD.SOUND_FORMAT.PCM16,
                length = byteLen,
                numchannels = 1
            };
            var buf8 = new byte[16 + byteLen + 16]; // FMOD API requires 16 bytes of padding on each side
            Buffer.BlockCopy(buf16, 0, buf8, 16, (int)byteLen);
            system.createSound(buf8, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL, ref ex, out FMOD.Sound snd);
            sounds.Add(wave.Address, snd);
        }

        internal VoiceTable() => voices = new SVoice[256]; // It is possible to play notes outside of the 128 range
        internal void LoadPCMSamples(uint table, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds)
        {
            for (uint i = 0; i < 128; i++)
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
                        LoadDirect(direct, system, sounds);
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
                        var wave = ROM.Instance.ReadStruct<GB_Wave>(offset);
                        voices[i] = new SVoice(wave);
                        LoadWave(wave, system, sounds);
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

                        for (uint j = 0; j < 128; j++)
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
                                    LoadDirect(ds, system, sounds);
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
                                    var wv = ROM.Instance.ReadStruct<GB_Wave>(mOffset);
                                    multi.Table[key] = new SVoice(wv);
                                    LoadWave(wv, system, sounds);
                                    break;
                                case 0x4:
                                case 0xC:
                                    multi.Table[key] = new SVoice(ROM.Instance.ReadStruct<PSG_Noise>(mOffset));
                                    break;
                            }
                        }
                        break;
                    case 0x80:
                        voices[i] = new SDrum(ROM.Instance.ReadStruct<Drum>(offset), system, sounds);
                        break;
                }
            }
        }
    }
}
