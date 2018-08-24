using GBAMusicStudio.Util;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core
{
    // Used in the VoiceTableEditor. GetName() is also used for the UI
    internal interface IVoiceTableInfo
    {
        uint Offset { get; }
        string GetName();
    }
    internal interface IVoice : IVoiceTableInfo
    {
        sbyte GetRootNote();
    }
    internal class WrappedVoice : IVoiceTableInfo
    {
        internal readonly IVoice Voice;
        public uint Offset => Voice.Offset;

        internal WrappedVoice(IVoice i) { Voice = i; }

        internal virtual IEnumerable<IVoiceTableInfo> GetSubVoices() => Enumerable.Empty<IVoiceTableInfo>();

        public string GetName() => Voice.GetName();
        public override string ToString() => Voice.ToString();
    }
    internal interface IWrappedSample
    {
        WrappedSample GetSample();
    }
    internal class WrappedSample
    {
        internal bool bLoop, bUnsigned;
        internal uint LoopPoint, Length;
        internal float Frequency;
        internal uint Offset; // Offset of the PCM buffer, not of the header

        internal WrappedSample(bool loop, uint loopPoint, uint length, float frequency, uint offset, bool unsigned)
        {
            bLoop = loop; LoopPoint = loopPoint; Length = length; Frequency = frequency; Offset = offset; bUnsigned = unsigned;
        }
    }

    #region M4A

    [StructLayout(LayoutKind.Sequential)]
    internal struct M4ASongEntry
    {
        internal uint Header;
        internal ushort Player;
        internal ushort Unknown;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct M4ASongHeader
    {
        internal byte NumTracks;
        internal byte NumBlocks;
        internal byte Priority;
        internal byte Reverb;
        internal uint VoiceTable;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal uint[] Tracks;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct M4AMLSSSample
    {
        internal uint DoesLoop; // Will be 0x40000000 if true
        internal uint Frequency; // Right shift 10 for value
        internal uint LoopPoint;
        internal uint Length;
        // 0x10 - byte[Length] of PCM8 data (Signed for M4A, Unsigned for MLSS)
    }

    internal class M4AVoice : IVoice
    {
        public uint Offset { get; }
        internal readonly M4AVoiceEntry Entry;
        string name = string.Empty; // Cache the name

        internal M4AVoice(uint offset) => Entry = ROM.Instance.ReadStruct<M4AVoiceEntry>(Offset = offset);

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
                var flags = (M4AVoiceFlags)Entry.Type;
                switch ((M4AVoiceType)(Entry.Type & 0x7))
                {
                    case M4AVoiceType.Direct:
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
                        bool bOWN = (flags & M4AVoiceFlags.OffWithNoise) == M4AVoiceFlags.OffWithNoise;
                        if (bOWN) str += " [ OWN ]";
                        break;
                }
            }
            return str;
        }
    }
    internal class M4AWrappedDirect : WrappedVoice
    {
        internal readonly M4AWrappedSample Sample;

        internal M4AWrappedDirect(M4AVoice direct) : base(direct) => Sample = new M4AWrappedSample(direct.Entry.Address);
    }
    internal class M4AWrappedKeySplit : WrappedVoice
    {
        internal readonly M4AVoiceTable Table;
        internal readonly Triple<byte, byte, byte>[] Keys;

        internal M4AWrappedKeySplit(M4AVoice keySplit) : base(keySplit)
        {
            try
            {
                Table = VoiceTable.LoadTable<M4AVoiceTable>(keySplit.Entry.Address, true);

                var keys = ROM.Instance.ReadBytes(256, keySplit.Entry.Keys);
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

        internal override IEnumerable<IVoiceTableInfo> GetSubVoices() => Table;
    }
    internal class M4AWrappedDrum : WrappedVoice
    {
        internal readonly M4AVoiceTable Table;

        internal M4AWrappedDrum(M4AVoice drum) : base(drum)
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

        internal override IEnumerable<IVoiceTableInfo> GetSubVoices() => Table;
    }
    internal class M4AWrappedSample : IWrappedSample
    {
        internal readonly uint Offset;
        readonly M4AMLSSSample sample;
        readonly WrappedSample gSample;

        internal M4AWrappedSample(uint offset)
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
    internal class M4AVoiceEntry
    {
        [FieldOffset(0)]
        internal byte Type;
        [FieldOffset(1)]
        internal sbyte RootNote;
        [FieldOffset(2)]
        internal byte Unknown;

        [FieldOffset(3)]
        internal byte Panpot; // Direct, Noise
        [FieldOffset(3)]
        internal byte Sweep; // Square1

        [FieldOffset(4)]
        internal SquarePattern SquarePattern; // Square1, Square2
        [FieldOffset(4)]
        internal NoisePattern NoisePattern; // Noise
        [FieldOffset(4)]
        internal uint Address; // Direct, Wave, Key Split, Drum

        [FieldOffset(8)]
        internal ADSR ADSR; // Direct, Square1, Square2, Wave, Noise
        [FieldOffset(8)]
        internal uint Keys; // Key Split
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GoldenSunPSG
    {
        internal sbyte Unknown; // Always signed 0
        internal GSPSGType Type;
        internal byte InitialCycle;
        internal byte CycleSpeed;
        internal byte CycleAmplitude;
        internal byte MinimumCycle;
    }

    #endregion

    #region MLSS

    internal class MLSSWrappedVoice : WrappedVoice
    {
        internal MLSSWrappedVoice(uint offset, uint numEntries) : base(new MLSSVoice(offset, numEntries)) { }

        internal override IEnumerable<IVoiceTableInfo> GetSubVoices() => ((MLSSVoice)Voice).Entries;
    }
    internal class MLSSVoice : IVoice
    {
        public uint Offset { get; }
        internal readonly MLSSWrappedVoiceEntry[] Entries;

        internal MLSSVoice(uint offset, uint numEntries)
        {
            Offset = offset;
            Entries = new MLSSWrappedVoiceEntry[numEntries];
            for (uint i = 0; i < numEntries; i++)
                Entries[i] = new MLSSWrappedVoiceEntry(offset + (i * 8));
        }

        // Throws exception if it can't find a single
        internal MLSSVoiceEntry GetEntryFromNote(sbyte note)
        {
            return Entries.Select(e => e.Entry).Single(e => e.MinKey <= note && note <= e.MaxKey);
        }

        public sbyte GetRootNote() => 60;
        public string GetName() => "MLSS";
        public override string ToString() => $"{GetName()} ({Entries.Length})";
    }
    internal class MLSSWrappedSample : IWrappedSample
    {
        internal readonly uint Offset;
        readonly M4AMLSSSample sample;
        readonly WrappedSample gSample;

        internal MLSSWrappedSample(uint offset)
        {
            Offset = offset;
            sample = ROM.Instance.ReadStruct<M4AMLSSSample>(offset);
            gSample = new WrappedSample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, offset + 0x10, true);
        }

        public WrappedSample GetSample() => gSample;
    }

    internal class MLSSWrappedVoiceEntry : IVoiceTableInfo
    {
        public uint Offset { get; }
        internal readonly MLSSVoiceEntry Entry;

        internal MLSSWrappedVoiceEntry(uint offset) => Entry = ROM.Instance.ReadStruct<MLSSVoiceEntry>(Offset = offset);

        public string GetName() => "Voice Entry";
        public override string ToString() => GetName();
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MLSSVoiceEntry
    {
        internal byte MinKey, MaxKey;
        internal byte Sample; // Index in sample table
        internal byte IsFixedFrequency, // 0x80 if true
            Unknown1, Unknown2, Unknown3, Unknown4; // Could be ADSR
    }

    #endregion
}
