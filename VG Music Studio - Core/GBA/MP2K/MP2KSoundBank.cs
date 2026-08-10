using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal class MP2KSoundBank : SoundBank
{
    public override int Offset { get; set; }
    public int Length { get; private set; }
    public IVoice[] Voices;
    private static readonly Dictionary<int, SoundBank> _cache = new Dictionary<int, SoundBank>();
    public static void ClearCache() => _cache.Clear();

    public MP2KSoundBank(int tableOffset, bool isSubVoiceGroup = false)
    {
        Offset = tableOffset;
        if (isSubVoiceGroup)
        {
            Voices = new IVoice[Length = 128];
        }
        else
        {
            List<IVoice> v = [];
            int i = 0;
            while (true)
            {
                int off = Offset + (i++ * 0xC);
                if (!GBAUtils.IsValidRomOffset(Engine.Instance!.Config.ROM!, off))
                {
                    break;
                }
                WrappedVoice voice = new(new(Engine.Instance!.Config.ROM!.AsSpan(off)), isSubVoiceGroup);
                if (!voice.IsValidVoiceEntry)
                {
                    Debug.WriteLine("Reached the end of the voicegroup table.");
                    break;
                }
                v.Add(voice);
                Length = v.Count;
            }
            Voices = new IVoice[Length];
            Voices = [.. v];
        }
    }

    public WrappedVoice this[int i]
    {
        get => (WrappedVoice)Voices[i];
        protected set => Voices[i] = value;
    }
    public override IEnumerator<IVoiceInfo> GetEnumerator() => ((IEnumerable<IVoiceInfo>)Voices).GetEnumerator();

    public ReadOnlySpan<byte> GetKeys(int offset)
    {
        Span<byte> keys = stackalloc byte[128];
        Engine.Instance!.Config.ROM!.AsSpan(offset, 128).CopyTo(keys);
        // var loading = new List<Tuple<byte, byte, byte>>(); // Key, min, max
        // int prev = -1;
        // for (int i = 0; i < 128; i++)
        // {
        //     byte a = keys[i];
        //     byte bi = (byte)i;
        //     if (prev == a)
        //     {
        //         loading[loading.Count - 1] = new(loading[loading.Count - 1].Item1, loading[loading.Count - 1].Item2, bi);
        //     }
        //     else
        //     {
        //         prev = a;
        //         loading.Add(new Tuple<byte, byte, byte>(a, bi, bi));
        //     }
        // }
        return keys.ToArray();
    }

    public static T LoadTable<T>(int tableOffset, bool shouldCache = false, bool isSubVoiceGroup = false) where T : MP2KSoundBank
    {
        if (_cache.ContainsKey(tableOffset))
        {
            return (T)_cache[tableOffset];
        }
        else
        {
            T vTable = (T)new MP2KSoundBank(tableOffset, isSubVoiceGroup);
            if (shouldCache)
            {
                _cache.Add(tableOffset, vTable);
            }
            vTable.Load();
            return vTable;
        }
    }
    protected override void Load()
    {
        Span<byte> vBytes = stackalloc byte[12];
        for (int i = 0; i < Length; i++)
        {
            int off = Offset + (i * 0xC);
            if (!GBAUtils.IsValidRomOffset(Engine.Instance!.Config.ROM!, off))
            {
                break;
            }
            vBytes = Engine.Instance!.Config.ROM!.AsSpan(off);
            VoiceEntry voice = new(vBytes);
            Voices[i] = new WrappedVoice(voice)
            {
                Offset = off
            };
        }
    }
    public override SoundBank LoadFromAddress(int tableOffset)
    {
        return LoadTable<MP2KSoundBank>(tableOffset);
    }
    public override object GetSubVoiceEntry(IVoiceInfo voiceInfo)
    {
        return ((IVoice)voiceInfo).VoiceEntry!;
    }
    public override string GetBytesToString(int voiceIndex)
    {
        if (Voices[voiceIndex].VoiceEntry is VoiceEntry entry)
        {
            return entry.GetBytesToString();
        }
        else
        {
            return Voices[voiceIndex].VoiceEntry.GetBytesToString();
        }
    }
    public override bool IsTableAddress(int voiceIndex)
    {
        if (Voices[voiceIndex].VoiceEntry.Type is (byte)VoiceFlags.KeySplit || Voices[voiceIndex].VoiceEntry.Type is (byte)VoiceFlags.Drum)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public override bool HasADSR(int voiceIndex)
    {
        switch (Voices[voiceIndex].VoiceEntry.Type)
        {
            case (byte)VoiceFlags.Drum:
                {
                    return false;
                }
            case (byte)VoiceFlags.KeySplit:
                {
                    return false;
                }
            default:
                {
                    return true;
                }
        }
    }
    public override bool IsValidVoiceAddress(int voiceIndex)
    {
        var flags = (VoiceFlags)Voices[voiceIndex].VoiceEntry.Type;
        var type = (VoiceType)(Voices[voiceIndex].VoiceEntry.Type & 0x7);
        return type == VoiceType.PCM8 || type == VoiceType.PCM4 || flags == VoiceFlags.KeySplit || flags == VoiceFlags.Drum;
    }
    public override int GetVoiceAddress(int voiceIndex)
    {
        return Voices[voiceIndex].VoiceEntry.Int4 - GBAUtils.CARTRIDGE_OFFSET;
    }

    public override bool IsValidADSR(int voiceIndex)
    {
        var flags = Voices[voiceIndex].VoiceEntry.Type;
        return flags != (byte)VoiceFlags.KeySplit && flags != (byte)VoiceFlags.Drum && !Voices[voiceIndex].VoiceEntry.IsInvalid();
    }
    public override bool IsPSGInstrument(int voiceIndex)
    {
        return Voices[voiceIndex].VoiceEntry.IsPSGInstrument();
    }
    public override void GetADSRValues(int voiceIndex, out byte attackValue, out byte decayValue, out byte sustainValue, out byte releaseValue)
    {
        attackValue = Voices[voiceIndex].VoiceEntry.ADSR.A;
        decayValue = Voices[voiceIndex].VoiceEntry.ADSR.D;
        sustainValue = Voices[voiceIndex].VoiceEntry.ADSR.S;
        releaseValue = Voices[voiceIndex].VoiceEntry.ADSR.R;
    }
    public override void SetADSRValues(int voiceIndex, in byte attackValue, in byte decayValue, in byte sustainValue, in byte releaseValue)
    {
        var adsr = Voices[voiceIndex].VoiceEntry.ADSR;
        adsr.A = attackValue;
        adsr.D = decayValue;
        adsr.S = sustainValue;
        adsr.R = releaseValue;
    }
    public override void SetAddressPointer(int voiceIndex, int address)
    {
        Voices[voiceIndex].VoiceEntry.SetAddressPointer(address);
        // Engine.Instance!.Config.ROM.AsSpan(address + GBAUtils.CARTRIDGE_CAPACITY);
    }
    public WrappedVoice GetVoiceFromNote(byte voice, sbyte note, out bool fromDrum)
    {
        fromDrum = false;

        IVoice sv = (WrappedVoice)Voices[voice];
    Read:
        VoiceEntry v = (VoiceEntry)sv.VoiceEntry!;
        switch (v.Type)
        {
            case (int)VoiceFlags.KeySplit:
                {
                    fromDrum = false; // In case there is a multi within a drum
                    var keySplit = (WrappedVoice)sv;
                    byte inst = Engine.Instance!.Config.ROM!.AsSpan(v.Int4 - GBAUtils.CARTRIDGE_OFFSET + note)[0];
                    sv = keySplit.Table![inst];
                    goto Read;
                }
            case (int)VoiceFlags.Drum:
                {
                    fromDrum = true;
                    var drum = (WrappedVoice)sv;
                    sv = drum.Table![note];
                    goto Read;
                }
            default: return (WrappedVoice)sv;
        }
    }

    public override IVoiceInfo ElementAt(int selectedIndex)
    {
        return this.ElementAt<IVoiceInfo>(selectedIndex);
    }
}
