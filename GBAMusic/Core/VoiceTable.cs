using System;
using System.Collections.Generic;
using static GBAMusic.Core.M4AStructs;

namespace GBAMusic.Core
{
    internal class VoiceTable
    {
        readonly Voice[] voices;

        internal Voice this[int i]
        {
            get => voices[i];
            private set => voices[i] = value;
        }

        void LoadDirect(DirectSound direct, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds)
        {
            if (direct.Address == 0 || sounds.ContainsKey(direct.Address)) return;
            Sample s = ROM.Instance.ReadStruct<Sample>(direct.Address);
            if (s.Length == 0) return;
            var buf = new byte[s.Length + 32]; // FMOD API requires 16 bytes of padding on each side
            Buffer.BlockCopy(ROM.Instance.ReadBytes(s.Length, direct.Address + 0x10), 0, buf, 16, (int)s.Length);
            for (int i = 0; i < s.Length; i++)
                buf[i + 16] ^= 0x80; // unencrypt
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = (int)s.Frequency / 1024,
                format = FMOD.SOUND_FORMAT.PCM8,
                length = s.Length,
                numchannels = 1
            };
            if (system.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd) == FMOD.RESULT.OK)
            {
                if (s.DoesLoop != 0)
                {
                    snd.setLoopPoints(s.LoopPoint, FMOD.TIMEUNIT.PCM, s.Length - 1, FMOD.TIMEUNIT.PCM);
                    snd.setMode(FMOD.MODE.LOOP_NORMAL);
                }
                sounds.Add(direct.Address, snd);
            }
        }
        void LoadWave(GBWave wave, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds)
        {
            if (wave.Address == 0 || sounds.ContainsKey(wave.Address)) return;
            var buf = new byte[32 + 32]; // FMOD API requires 16 bytes of padding on each side
            for (uint i = 0; i < 16; i++)
            {
                byte b = ROM.Instance.ReadByte(wave.Address + i);
                int h_nibble = b >> 4;
                int l_nibble = b & 0xF;
                h_nibble = (h_nibble > 8) ?
                        0x80 + (9 * (h_nibble - 8)) :
                        0x40 + (8 * h_nibble);
                l_nibble = (l_nibble > 8) ?
                        0x80 + (9 * (l_nibble - 8)) :
                        0x40 + (8 * l_nibble);
                buf[(i * 2) + 16] = (byte)h_nibble;
                buf[(i * 2) + 17] = (byte)l_nibble;
            }
            var ex = new FMOD.CREATESOUNDEXINFO()
            {
                defaultfrequency = 112640, // Wrong
                format = FMOD.SOUND_FORMAT.PCM16,
                length = 32,
                numchannels = 1
            };
            if (system.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL, ref ex, out FMOD.Sound snd) == FMOD.RESULT.OK)
                sounds.Add(wave.Address, snd); // Wrong frequency
        }

        internal VoiceTable() => voices = new Voice[256]; // It is possible to play notes outside of the 128 range
        internal void LoadDirectSamples(uint table, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds)
        {
            for (uint i = 0; i < 128; i++)
            {
                uint offset = table + (i * 0xC);
                switch (ROM.Instance.ReadByte(offset)) // Check type
                {
                    case 0x0:
                    case 0x8:
                        var direct = ROM.Instance.ReadStruct<DirectSound>(offset);
                        voices[i] = new Voice(direct);
                        LoadDirect(direct, system, sounds);
                        break;
                    case 0x1:
                    case 0x9:
                        voices[i] = new Voice(ROM.Instance.ReadStruct<SquareWave1>(offset));
                        break;
                    case 0x2:
                    case 0xA:
                        voices[i] = new Voice(ROM.Instance.ReadStruct<SquareWave2>(offset));
                        break;
                    case 0x3:
                    case 0xB:
                        var wave = ROM.Instance.ReadStruct<GBWave>(offset);
                        voices[i] = new Voice(wave);
                        LoadWave(wave, system, sounds);
                        break;
                    case 0x4:
                    case 0xC:
                        voices[i] = new Voice(ROM.Instance.ReadStruct<Noise>(offset));
                        break;
                    case 0x40:
                        var keySplit = ROM.Instance.ReadStruct<KeySplit>(offset);
                        var multi = new Multi(keySplit);
                        voices[i] = multi;
                        for (uint j = 0; j < 128; j++)
                        {
                            byte key = ROM.Instance.ReadByte(keySplit.Keys + j);
                            var ds = ROM.Instance.ReadStruct<DirectSound>(keySplit.Table + (uint)(key * 0xC));
                            if (ds.VoiceType == 0x0 || ds.VoiceType == 0x8)
                            {
                                multi.Table[key] = new Voice(ds);
                                LoadDirect(ds, system, sounds);
                            }
                        }
                        break;
                    case 0x80:
                        voices[i] = new Drum(ROM.Instance.ReadStruct<SDrum>(offset), system, sounds);
                        break;
                }
            }
        }
    }
}
