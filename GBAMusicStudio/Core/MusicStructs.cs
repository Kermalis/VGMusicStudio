using GBAMusicStudio.Util;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core
{
    // Used in the VoiceTableEditor. GetName() is also used for the UI
    interface IVoiceTableInfo
    {
        uint Offset { get; }
        string GetName();
    }
    interface IVoice : IVoiceTableInfo
    {
        sbyte GetRootNote();
    }
    class WrappedVoice : IVoiceTableInfo
    {
        public readonly IVoice Voice;
        public uint Offset => Voice.Offset;

        public WrappedVoice(IVoice i) { Voice = i; }

        public virtual IEnumerable<IVoiceTableInfo> GetSubVoices() => Enumerable.Empty<IVoiceTableInfo>();

        public string GetName() => Voice.GetName();
        public override string ToString() => Voice.ToString();
    }
    interface IWrappedSample
    {
        WrappedSample GetSample();
    }
    class WrappedSample
    {
        public readonly bool bLoop, bUnsigned;
        public readonly uint LoopPoint, Length;
        public readonly float Frequency;
        public readonly uint Offset; // Offset of the PCM buffer, not of the header

        public WrappedSample(bool loop, uint loopPoint, uint length, float frequency, uint offset, bool unsigned)
        {
            bLoop = loop; LoopPoint = loopPoint; Length = length; Frequency = frequency; Offset = offset; bUnsigned = unsigned;
        }
    }

    #region M4A

    [StructLayout(LayoutKind.Sequential)]
    struct M4ASongEntry
    {
        public uint Header;
        public ushort Player;
        public ushort Unknown;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct M4ASongHeader
    {
        public byte NumTracks;
        public byte NumBlocks;
        public byte Priority;
        public byte Reverb;
        public uint VoiceTable;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[] Tracks;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct M4AMLSSSample
    {
        public uint DoesLoop; // Will be 0x40000000 if true
        public uint Frequency; // Right shift 10 for value
        public uint LoopPoint;
        public uint Length;
        // 0x10 - byte[Length] of PCM8 data (Signed for M4A, Unsigned for MLSS)
    }

    class M4AVoice : IVoice
    {
        public uint Offset { get; }
        public readonly M4AVoiceEntry Entry;
        string name = string.Empty; // Cache the name

        public M4AVoice(uint offset) => Entry = ROM.Instance.ReadStruct<M4AVoiceEntry>(Offset = offset);

        public bool IsGoldenSunPSG()
        {
            if ((Entry.Type & 0x7) != (int)M4AVoiceType.Direct) return false;
            var gSample = new M4AWrappedSample(Entry.Address).GetSample();
            if (gSample == null) return false;
            return (gSample.bLoop && gSample.LoopPoint == 0 && gSample.Length == 0);
        }

        public string GetName()
        {
            if (name == string.Empty)
            {
                if (Entry.Type == (int)M4AVoiceFlags.KeySplit)
                    name = "Key Split";
                else if (Entry.Type == (int)M4AVoiceFlags.Drum)
                    name = "Drum";
                else
                {
                    var type = (M4AVoiceType)(Entry.Type & 0x7);
                    switch (type)
                    {
                        case M4AVoiceType.Direct:
                            name = IsGoldenSunPSG() ? $"GS {ROM.Instance.ReadStruct<GoldenSunPSG>(Entry.Address + 0x10).Type}" : "Direct Sound";
                            break;
                        default: name = type.Humanize(); break;
                    }
                }
            }
            return name;
        }
        public sbyte GetRootNote() => Entry.RootNote;
        public override string ToString()
        {
            string str = GetName();
            if (Entry.Type != (int)M4AVoiceFlags.KeySplit && Entry.Type != (int)M4AVoiceFlags.Drum)
            {
                switch ((M4AVoiceType)(Entry.Type & 0x7))
                {
                    case M4AVoiceType.Direct:
                        var flags = (M4AVoiceFlags)Entry.Type;
                        bool bFixed = (flags & M4AVoiceFlags.Fixed) == M4AVoiceFlags.Fixed,
                            bReversed = (flags & M4AVoiceFlags.Reversed) == M4AVoiceFlags.Reversed,
                            bCompressed = (flags & M4AVoiceFlags.Compressed) == M4AVoiceFlags.Compressed;
                        if (bFixed || bReversed || bCompressed)
                        {
                            str += " [ ";
                            if (bFixed) str += "Fixed ";
                            if (bReversed) str += "Reversed ";
                            if (bCompressed) str += "Compressed ";
                            str += ']';
                        }
                        break;
                    default:
                        flags = (M4AVoiceFlags)Entry.Type;
                        bool bOFN = (flags & M4AVoiceFlags.OffWithNoise) == M4AVoiceFlags.OffWithNoise;
                        if (bOFN) str += " [ OWN ]";
                        break;
                }
            }
            return str;
        }
    }
    class M4AWrappedDirect : WrappedVoice
    {
        public readonly M4AWrappedSample Sample;

        public M4AWrappedDirect(M4AVoice direct) : base(direct) => Sample = new M4AWrappedSample(direct.Entry.Address);
    }
    class M4AWrappedMulti : WrappedVoice
    {
        public readonly M4AVoiceTable Table;
        public readonly Triple<byte, byte, byte>[] Keys;

        public M4AWrappedMulti(M4AVoice multi) : base(multi)
        {
            try
            {
                Table = VoiceTable.LoadTable<M4AVoiceTable>(multi.Entry.Address, true);

                var keys = ROM.Instance.ReadBytes(256, multi.Entry.Keys);
                var loading = new List<Triple<byte, byte, byte>>(); // Key, min, max
                int prev = -1;
                for (int i = 0; i < 256; i++)
                {
                    byte a = keys[i];
                    byte bi = (byte)i;
                    if (prev == a)
                        loading[loading.Count - 1].Item3 = bi;
                    else
                    {
                        prev = a;
                        loading.Add(new Triple<byte, byte, byte>(a, bi, bi));
                    }
                }
                Keys = loading.ToArray();
            }
            catch
            {
                Table = null;
                Keys = null;
            }
        }

        public override IEnumerable<IVoiceTableInfo> GetSubVoices() => Table;
    }
    class M4AWrappedDrum : WrappedVoice
    {
        public readonly M4AVoiceTable Table;

        public M4AWrappedDrum(M4AVoice drum) : base(drum)
        {
            try
            {
                Table = VoiceTable.LoadTable<M4AVoiceTable>(drum.Entry.Address, true);
            }
            catch
            {
                Table = null;
            }
        }

        public override IEnumerable<IVoiceTableInfo> GetSubVoices() => Table;
    }
    class M4AWrappedSample : IWrappedSample
    {
        public readonly uint Offset;
        readonly M4AMLSSSample sample;
        readonly WrappedSample gSample;

        public M4AWrappedSample(uint offset)
        {
            Offset = offset;

            if (offset == 0 || !ROM.IsValidRomOffset(offset - ROM.Pak))
                goto fail;

            sample = ROM.Instance.ReadStruct<M4AMLSSSample>(offset);

            if (!ROM.IsValidRomOffset(sample.Length + (offset + 0x10) - ROM.Pak))
                goto fail;

            gSample = new WrappedSample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, offset + 0x10, false);
            return;

            fail:
            sample = new M4AMLSSSample();
            gSample = null;
            Console.WriteLine("Error loading instrument at 0x{0:X}.", offset);
        }

        public WrappedSample GetSample() => gSample;
    }

    [StructLayout(LayoutKind.Explicit)]
    class M4AVoiceEntry
    {
        [FieldOffset(0)]
        public byte Type;
        [FieldOffset(1)]
        public sbyte RootNote;
        [FieldOffset(2)]
        public byte Unknown;

        [FieldOffset(3)]
        public byte Panpot; // Direct, Noise
        [FieldOffset(3)]
        public byte Sweep; // Square1

        [FieldOffset(4)]
        public SquarePattern SquarePattern; // Square1, Square2
        [FieldOffset(4)]
        public NoisePattern NoisePattern; // Noise
        [FieldOffset(4)]
        public uint Address; // Direct, Wave, KeySplit, Drum

        [FieldOffset(8)]
        public ADSR ADSR; // Direct, Square1, Square2, Wave, Noise
        [FieldOffset(8)]
        public uint Keys; // KeySplit
    }

    [StructLayout(LayoutKind.Sequential)]
    struct GoldenSunPSG
    {
        public sbyte Unknown; // Always signed 0
        public GSPSGType Type;
        public byte InitialCycle;
        public byte CycleSpeed;
        public byte CycleAmplitude;
        public byte MinimumCycle;
    }

    #endregion

    #region MLSS

    class MLSSWrappedVoice : WrappedVoice
    {
        public MLSSWrappedVoice(uint offset, uint numEntries) : base(new MLSSVoice(offset, numEntries)) { }

        public override IEnumerable<IVoiceTableInfo> GetSubVoices() => ((MLSSVoice)Voice).Entries;
    }
    class MLSSVoice : IVoice
    {
        public uint Offset { get; }
        public readonly MLSSWrappedVoiceEntry[] Entries;

        public MLSSVoice(uint offset, uint numEntries)
        {
            Offset = offset;
            Entries = new MLSSWrappedVoiceEntry[numEntries];
            for (uint i = 0; i < numEntries; i++)
                Entries[i] = new MLSSWrappedVoiceEntry(offset + (i * 8));
        }

        // Throws exception if it can't find a single
        public MLSSVoiceEntry GetEntryFromNote(sbyte note)
        {
            return Entries.Select(e => e.Entry).Single(e => e.MinKey <= note && note <= e.MaxKey);
        }

        public sbyte GetRootNote() => 60;
        public string GetName() => "MLSS";
        public override string ToString() => $"{GetName()} ({Entries.Length})";
    }
    class MLSSWrappedSample : IWrappedSample
    {
        public readonly uint Offset;
        readonly M4AMLSSSample sample;
        readonly WrappedSample gSample;

        public MLSSWrappedSample(uint offset)
        {
            Offset = offset;
            sample = ROM.Instance.ReadStruct<M4AMLSSSample>(offset);
            gSample = new WrappedSample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, offset + 0x10, true);
        }

        public WrappedSample GetSample() => gSample;
    }

    class MLSSWrappedVoiceEntry : IVoiceTableInfo
    {
        public uint Offset { get; }
        public readonly MLSSVoiceEntry Entry;

        public MLSSWrappedVoiceEntry(uint offset) => Entry = ROM.Instance.ReadStruct<MLSSVoiceEntry>(Offset = offset);

        public string GetName() => "Voice Entry";
        public override string ToString() => GetName();
    }
    [StructLayout(LayoutKind.Sequential)]
    struct MLSSVoiceEntry
    {
        public byte MinKey, MaxKey;
        public byte Sample; // Index in sample table
        public byte IsFixedFrequency, // 0x80 if true
            Unknown1, Unknown2, Unknown3, Unknown4; // Could be ADSR
    }

    #endregion
}
