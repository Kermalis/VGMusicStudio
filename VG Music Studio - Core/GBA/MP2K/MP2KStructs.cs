using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct SongEntry
{
    public const int SIZE = 8;

    public readonly int HeaderOffset;
    public readonly short Player;
    public readonly byte Unknown1;
    public readonly byte Unknown2;

    public SongEntry(ReadOnlySpan<byte> src)
    {
        if (BitConverter.IsLittleEndian)
        {
            this = MemoryMarshal.AsRef<SongEntry>(src);
        }
        else
        {
            HeaderOffset = ReadInt32LittleEndian(src.Slice(0));
            Player = ReadInt16LittleEndian(src.Slice(4));
            Unknown1 = src[6];
            Unknown2 = src[7];
        }
    }

    public static SongEntry Get(byte[] rom, long songTableOffset, int songNum)
    {
        return new SongEntry(rom.AsSpan((int)songTableOffset + (songNum * SIZE)));
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct SongHeader
{
    public const int SIZE = 8;

    public readonly byte NumTracks;
    public readonly byte NumBlocks;
    public readonly byte Priority;
    public readonly byte Reverb;
    public readonly int SoundBankOffset;
    // int[NumTracks] TrackOffset;

    public SongHeader(ReadOnlySpan<byte> src)
    {
        if (BitConverter.IsLittleEndian)
        {
            this = MemoryMarshal.AsRef<SongHeader>(src);
        }
        else
        {
            NumTracks = src[0];
            NumBlocks = src[1];
            Priority = src[2];
            Reverb = src[3];
            SoundBankOffset = ReadInt32LittleEndian(src.Slice(4));
        }
    }

    public static SongHeader Get(byte[] rom, int offset, out int tracksOffset)
    {
        tracksOffset = offset + SIZE;
        return new SongHeader(rom.AsSpan(offset));
    }
    public static int GetTrackOffset(byte[] rom, int tracksOffset, int trackIndex)
    {
        return ReadInt32LittleEndian(rom.AsSpan(tracksOffset + (trackIndex * 4)));
    }
}

internal struct SongTableInfo
{
    public const int PosAuto = 0;
    public const ushort CountAuto = 0xFFFF;

    public long Position = PosAuto;
    public ushort Count = CountAuto;
    public int TableIndex = 0;

    public SongTableInfo()
    {
    }

    public readonly bool IsAuto()
    {
        return Position == PosAuto || Count == CountAuto;
    }
}

internal struct PlayerInfo
{
    public byte MaxTracks;
    public byte UsePriority;
}

internal struct WrappedVoice : IVoice
{
    public VoiceEntry VoiceEntry { get; private set; }

    public int Offset { get; set; }
    public readonly string? Name { get; }
    public sbyte RootNote { get; }
    public byte Sweep { get; }
    public bool IsValidVoiceEntry { get; }

    public readonly MP2KSoundBank? Table;
    public readonly byte[]? Keys;
    public readonly MP2KSample? Sample;

    internal WrappedVoice(VoiceEntry voice, bool isSubVoiceGroup = false)
    {
        switch (voice.Type)
        {
            case (byte)VoiceType.PCM8:
                {
                    VoiceEntry = voice;
                    if (voice.Int4 - GBAUtils.CARTRIDGE_OFFSET >= 0)
                    {
                        Sample = new MP2KSample(Engine.Instance!.Config.ROM!.AsSpan(voice.Int4 - GBAUtils.CARTRIDGE_OFFSET));
                        Name = voice.Name;
                        RootNote = (sbyte)voice.RootNote;
                        IsValidVoiceEntry = IsValidADSR();
                    }
                    else
                    {
                        IsValidVoiceEntry = false;
                    }
                    break;
                }
            case (byte)VoiceType.Square1:
                {
                    VoiceEntry = voice;
                    Name = voice.Name;
                    RootNote = (sbyte)voice.RootNote;
                    Sweep = voice.PanSweep;
                    IsValidVoiceEntry = IsValidADSR();
                    break;
                }
            case (byte)VoiceType.Square2:
                {
                    VoiceEntry = voice;
                    Name = voice.Name;
                    RootNote = (sbyte)voice.RootNote;
                    IsValidVoiceEntry = IsValidADSR();
                    break;
                }
            case (byte)VoiceType.PCM4:
                {
                    VoiceEntry = voice;
                    if (voice.Int4 - GBAUtils.CARTRIDGE_OFFSET >= 0)
                    {
                        Sample = new MP2KSample(Engine.Instance!.Config.ROM!.AsSpan(voice.Int4 - GBAUtils.CARTRIDGE_OFFSET), true);
                        Name = voice.Name;
                        RootNote = (sbyte)voice.RootNote;
                        IsValidVoiceEntry = IsValidADSR();
                    }
                    else
                    {
                        IsValidVoiceEntry = false;
                    }
                    break;
                }
            case (byte)VoiceType.Noise:
                {
                    VoiceEntry = voice;
                    Name = voice.Name;
                    RootNote = (sbyte)voice.RootNote;
                    IsValidVoiceEntry = IsValidADSR();
                    break;
                }

            case (byte)VoiceType.Invalid5:
            case (byte)VoiceType.Invalid6:
            case (byte)VoiceType.Invalid7:
            default:
                {
                    Debug.WriteLine($"Invalid instrument type detected. If this was reading from a while loop, it was likely due to reaching the end of the voicegroup table.");
                    IsValidVoiceEntry = false;
                    break;
                }

            case (byte)VoiceFlags.Fixed:
                {
                    VoiceEntry = voice;
                    if (voice.Int4 - GBAUtils.CARTRIDGE_OFFSET >= 0)
                    {
                        Sample = new MP2KSample(Engine.Instance!.Config.ROM!.AsSpan(voice.Int4 - GBAUtils.CARTRIDGE_OFFSET));
                        Name = voice.Name;
                        RootNote = (sbyte)voice.RootNote;
                        IsValidVoiceEntry = IsValidADSR();
                    }
                    else
                    {
                        IsValidVoiceEntry = false;
                    }
                    break;
                }
            case (byte)VoiceFlags.OffWithNoise:
                {
                    VoiceEntry = voice;
                    Name = voice.Name;
                    RootNote = (sbyte)voice.RootNote;
                    IsValidVoiceEntry = IsValidADSR();
                    break;
                }
            case 0xA:
                {
                    goto case (byte)VoiceType.Square2;
                }
            case 0xB:
                {
                    goto case (byte)VoiceType.PCM4;
                }
            case 0xC:
                {
                    goto case (byte)VoiceType.Noise;
                }
            case (byte)VoiceFlags.Reversed:
            case (byte)VoiceFlags.Compressed:
                {
                    goto case (byte)VoiceType.PCM8;
                }
            case (byte)VoiceFlags.KeySplit:
                {
                    VoiceEntry = voice;
                    RootNote = (sbyte)voice.RootNote;
                    if (!isSubVoiceGroup)
                    {
                        try
                        {
                            Table = MP2KSoundBank.LoadTable<MP2KSoundBank>(voice.Int4 - GBAUtils.CARTRIDGE_OFFSET, true, true);
                            Keys = Table.GetKeys(voice.Int8 - GBAUtils.CARTRIDGE_OFFSET).ToArray();
                        }
                        catch
                        {
                            Table = null;
                            Keys = null;
                        }
                        // Name = $"Key Split ({Keys!.Select(k => k.Item1).Distinct().Count()})";
                    }
                    Name = $"Key Split";
                    IsValidVoiceEntry = IsValidTableOffset();
                    break;
                }
            case (byte)VoiceFlags.Drum:
                {
                    VoiceEntry = voice;
                    Name = voice.Name;
                    RootNote = (sbyte)voice.RootNote;
                    if (!isSubVoiceGroup)
                    {
                        Table = MP2KSoundBank.LoadTable<MP2KSoundBank>(voice.Int4 - GBAUtils.CARTRIDGE_OFFSET, true, true);
                    }
                    IsValidVoiceEntry = IsValidTableOffset();
                    break;
                }
        }
    }

    private readonly bool IsValidVoiceType(VoiceEntry voice)
    {
        switch (voice.Type)
        {
            case (byte)VoiceType.PCM8:
            case (byte)VoiceType.Square1:
            case (byte)VoiceType.Square2:
            case (byte)VoiceType.PCM4:
            case (byte)VoiceType.Noise:
                {
                    return true;
                }

            case (byte)VoiceType.Invalid5:
            case (byte)VoiceType.Invalid6:
            case (byte)VoiceType.Invalid7:
            default:
                {
                    return false;
                }

            case (byte)VoiceFlags.Fixed:
            case (byte)VoiceFlags.OffWithNoise:
            case 0xA:
            case 0xB:
            case 0xC:
            case (byte)VoiceFlags.Reversed:
            case (byte)VoiceFlags.Compressed:
            case (byte)VoiceFlags.KeySplit:
            case (byte)VoiceFlags.Drum:
                {
                    return true;
                }
        }
    }
    private readonly bool IsValidTableOffset()
    {
        var offset = VoiceEntry.Int4 - GBAUtils.CARTRIDGE_OFFSET;
        if (GBAUtils.IsValidRomOffset(Engine.Instance!.Config.ROM!, offset))
        {
            VoiceEntry[] vTable = new VoiceEntry[128];
            for (int i = 0; i < vTable.Length; i++)
            {
                vTable[i] = new VoiceEntry(Engine.Instance!.Config.ROM!.AsSpan(offset + (i * 12)));
                if (!IsValidVoiceType(vTable[i]))
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    private readonly bool IsValidADSR()
    {
        switch (VoiceEntry.Type)
        {
            case (byte)VoiceType.PCM8:
                {
                    return true;
                }
            case (byte)VoiceType.Square1:
            case (byte)VoiceType.Square2:
            case (byte)VoiceType.PCM4:
            case (byte)VoiceType.Noise:
                {
                    return (VoiceEntry.ADSR.A <= 0x7) && (VoiceEntry.ADSR.D <= 0x7) && (VoiceEntry.ADSR.S <= 0xF) && (VoiceEntry.ADSR.R <= 0x7);
                }

            case (byte)VoiceType.Invalid5:
            case (byte)VoiceType.Invalid6:
            case (byte)VoiceType.Invalid7:
            default:
                {
                    return false;
                }

            case (byte)VoiceFlags.Fixed:
                {
                    goto case (byte)VoiceType.PCM8;
                }
            case (byte)VoiceFlags.OffWithNoise:
                {
                    goto case (byte)VoiceType.Square1;
                }
            case 0xA:
                {
                    goto case (byte)VoiceType.Square2;
                }
            case 0xB:
                {
                    goto case (byte)VoiceType.PCM4;
                }
            case 0xC:
                {
                    goto case (byte)VoiceType.Noise;
                }
            case (byte)VoiceFlags.Reversed:
            case (byte)VoiceFlags.Compressed:
                {
                    goto case (byte)VoiceType.PCM8;
                }
        }
    }
    public IEnumerable<IVoiceInfo> GetSubVoices() => Enumerable.Empty<IVoiceInfo>();

    public override readonly string ToString() => Name!;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal struct VoiceEntry
{
    public const int SIZE = 12;

    public byte Type; // 0
    public byte RootNote; // 1
    /// <summary>Hardware microseconds for Square1, Square2 and Noise PSG types only, while all other types set it to 0 because it's skipped</summary>
    public byte TimeLength; // 2
    public byte PanSweep; // 3
    /// <summary>SquarePattern for Square1/Square2, NoisePattern for Noise, Address for PCM8/PCM4/KeySplit/Drum</summary>
    public int Int4; // 4
    /// <summary>ADSR for PCM8/Square1/Square2/PCM4/Noise, KeysAddress for KeySplit</summary>
    public ADSR ADSR; // 8

    public readonly int Int8 => (ADSR.R << 24) | (ADSR.S << 16) | (ADSR.D << 8) | ADSR.A;

    public readonly string? Name
    {
        get
        {
            var name = "";
            if (Type == (int)VoiceFlags.KeySplit)
            {
                name = "Key Split";
            }
            else if (Type == (int)VoiceFlags.Drum)
            {
                name = "Drum";
            }
            else
            {
                switch ((VoiceType)(Type & 0x7))
                {
                    case VoiceType.PCM8: name = IsGoldenSunPSG() ? $"GS {SynthPSG.Get(Engine.Instance!.Config.ROM!.AsSpan(Int8 /*- GBAUtils.CARTRIDGE_OFFSET*/ + 0x10)).Type}" : "PCM8"; break;
                    case VoiceType.Square1: name = "Square 1"; break;
                    case VoiceType.Square2: name = "Square 2"; break;
                    case VoiceType.PCM4: name = "PCM4"; break;
                    case VoiceType.Noise: name = "Noise"; break;
                    case VoiceType.Invalid5: name = "Invalid 5"; break;
                    case VoiceType.Invalid6: name = "Invalid 6"; break;
                    case VoiceType.Invalid7: name = "Invalid 7"; break;
                }
            }
            return name;
        }
    }

    public VoiceEntry(ReadOnlySpan<byte> src)
    {
        if (BitConverter.IsLittleEndian)
        {
            this = MemoryMarshal.AsRef<VoiceEntry>(src);
        }
        else
        {
            Type = src[0];
            RootNote = src[1];
            TimeLength = src[2];
            PanSweep = src[3];
            Int4 = ReadInt32LittleEndian(src.Slice(4));
            ADSR = ADSR.Get(src.Slice(8));
        }
    }

    public void SetAddressPointer(int address)
    {
        Int4 = address;
    }
    public readonly bool IsPSGInstrument()
    {
        if (Type == (int)VoiceFlags.KeySplit || Type == (int)VoiceFlags.Drum)
        {
            return false;
        }
        VoiceType vType = (VoiceType)(Type & 0x7);
        return vType >= VoiceType.Square1 && vType <= VoiceType.Noise;
    }
    public readonly bool IsGoldenSunPSG()
    {
        if (!MP2KEngine.MP2KInstance!.Config.HasGoldenSunSynths || (Type & 0x7) != (int)VoiceType.PCM8
            || Type == (int)VoiceFlags.KeySplit || Type == (int)VoiceFlags.Drum)
        {
            return false;
        }
        var gSample = new SampleHeader(Engine.Instance!.Config.ROM!.AsSpan(Int8 - GBAUtils.CARTRIDGE_OFFSET));
        return gSample.DoesLoop == 0 && gSample.LoopOffset == 0 && gSample.Length == 0;
    }
    public bool IsInvalid()
    {
        return (Type & 0x7) >= (int)VoiceType.Invalid5;
    }
    public string GetBytesToString()
    {
        return $"{Type:X2} {RootNote:X2} {TimeLength:X2} {PanSweep:X2} " +
            $"{(byte)Int8:X2} {(byte)(Int8 >> 8):X2} {(byte)(Int8 >> 16):X2} {(byte)(Int8 >> 24):X2} " +
            $"{ADSR.A:X2} {ADSR.D:X2} {ADSR.S:X2} {ADSR.R:X2}";
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
public struct ADSR
{
    public const int SIZE = 4;

    public byte A;
    public byte D;
    public byte S;
    public byte R;

    public static ref readonly ADSR Get(ReadOnlySpan<byte> src)
    {
        return ref MemoryMarshal.AsRef<ADSR>(src);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal readonly struct SynthPSG
{
    public const int SIZE = 6;

    /// <summary>Always 0x80</summary>
    public readonly byte Unknown;
    public readonly SynthType Type;
    public readonly byte InitialCycle;
    public readonly byte CycleSpeed;
    public readonly byte CycleAmplitude;
    public readonly byte MinimumCycle;

    public static ref readonly SynthPSG Get(ReadOnlySpan<byte> src)
    {
        return ref MemoryMarshal.AsRef<SynthPSG>(src);
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
internal struct SampleHeader
{
    public const int SIZE = 16;
    public const short LOOP_TRUE = 0x4_000;

    public CodecType Codec;
    /// <summary>0x4_000 if True</summary>
    public short DoesLoop;
    /// <summary>Right shift 10 for value</summary>
    public int SampleRate;
    public int LoopOffset;
    public int Length;
    // byte[Length] Sample;

    public SampleHeader(ReadOnlySpan<byte> src)
    {
        if (BitConverter.IsLittleEndian)
        {
            this = MemoryMarshal.AsRef<SampleHeader>(src);
        }
        else
        {
            Codec = (CodecType)ReadInt16LittleEndian(src[..2]);
            DoesLoop = ReadInt16LittleEndian(src.Slice(2, 2));
            SampleRate = ReadInt32LittleEndian(src.Slice(4, 4));
            LoopOffset = ReadInt32LittleEndian(src.Slice(8, 4));
            Length = ReadInt32LittleEndian(src.Slice(12, 4));
        }
    }

    public static SampleHeader Get(byte[] rom, int offset, out int sampleOffset)
    {
        sampleOffset = offset + SIZE;
        return new SampleHeader(rom.AsSpan(offset));
    }
}

internal struct SampleInfo
{
    public SampleHeader Header;
    public sbyte[] SampleData;

    public int SampleOffset;
    public int Position;
    public float MidCFrequency;
    public bool LoopEnabled;

    internal SampleInfo(byte[] src, int offset)
    {
        Header = SampleHeader.Get(src, offset, out SampleOffset);
        SampleData = new sbyte[Header.Length];
        Span<byte> samples = src.AsSpan(offset + 16, Header.Length);
        for (int i = 0; i < Header.Length; i++)
        {
            SampleData[i] = (sbyte)samples[i];
        }
        Position = 0;
        MidCFrequency = Header.SampleRate / 1024.0f;
        LoopEnabled = Convert.ToBoolean(Header.DoesLoop);

        if (Header.LoopOffset > Header.Length)
        {
            Header.LoopOffset = 0;
        }
        if (Header.LoopOffset == Header.Length)
        {
            LoopEnabled = false;
        }
    }
}

internal struct MixingArgs
{
    public float Volume;
    public int FixedModeRate;
    public float SampleRateInv;
    public float SamplesPerBufferInv;
};

internal struct ChannelVolume
{
    public float FromVolLeft, FromVolRight;
    public float ToVolLeft, ToVolRight;
}
internal struct NoteInfo
{
    /// <summary>-1 if forever</summary>
    public int Duration;
    public byte Note, OriginalNote;
    public byte Velocity;
    public byte Priority;
    public sbyte RhythmPan;
    public byte PseudoEchoVolume, PseudoEchoLength;
    public byte TrackIndex, PlayerIndex;
    public byte PSGLength;
}

internal struct MP2KSoundMode
{
    public const byte VOL_AUTO = 0xFF;
    public const byte REV_MASK_VAL = 0x7F;
    public const byte REV_MASK_SET = 0x80;
    public const byte FREQ_AUTO = 0xFF;
    public const byte CHN_AUTO = 0xFF;
    public const byte DAC_AUTO = 0xFF;

    public byte Volume = VOL_AUTO;
    public byte Reverb = 0;
    public byte FrequencyIndex = FREQ_AUTO;
    public byte MaxChannels = CHN_AUTO;    // currently unused
    public byte DACConfig = DAC_AUTO;      // currently unused

    public MP2KSoundMode() { }

    public readonly bool IsAuto()
    {
        return Volume == VOL_AUTO || Reverb == REV_MASK_VAL || FrequencyIndex == FREQ_AUTO || MaxChannels == CHN_AUTO || DACConfig == DAC_AUTO;
    }
}

internal struct PlayerSoundMode
{
    internal ResamplerType ResamplerTypeNormal = ResamplerType.Linear;
    internal ResamplerType ResamplerTypeFixed = ResamplerType.Nearest;
    internal ReverbType ReverbType = ReverbType.Normal;
    internal byte ReverbForce = 0;
    internal PSGPolyphony PSGPolyphony = PSGPolyphony.MONO_STRICT;
    internal uint DMABufferLength = 0x630;
    internal bool AccurateCh3Quantization = true;
    internal bool AccurateCh3Volume = true;
    internal bool EmulatePSGSustainBug = true;

    public PlayerSoundMode()
    {
    }
}

internal struct ScanResult
{
    internal MP2KSoundMode MP2KSoundMode;
    internal List<PlayerInfo> PlayerTableInfo;
    internal SongTableInfo SongTableInfo;
}

