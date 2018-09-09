using GBAMusicStudio.Util;
using Humanizer;
using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#pragma warning disable CS0649

namespace GBAMusicStudio.Core
{
    class WrappedVoice : IVoiceTableInfo
    {
        public readonly IVoice Voice;

        public WrappedVoice(IVoice i) { Voice = i; }

        public uint GetOffset() => Voice.GetOffset();
        public void SetOffset(uint newOffset) => Voice.SetOffset(newOffset);

        public virtual IEnumerable<IVoiceTableInfo> GetSubVoices() => Enumerable.Empty<IVoiceTableInfo>();

        public string GetName() => Voice.GetName();
        public override string ToString() => Voice.ToString();
    }
    class WrappedSample : IOffset
    {
        uint offset; // Offset of the PCM buffer, not of the header
        public readonly bool bLoop, bUnsigned;
        public readonly uint LoopPoint, Length;
        public readonly float Frequency;

        public WrappedSample(bool loop, uint loopPoint, uint length, float frequency, bool unsigned)
        {
            bLoop = loop; LoopPoint = loopPoint; Length = length; Frequency = frequency; bUnsigned = unsigned;
        }

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset) => offset = newOffset;
    }

    #region M4A

    class M4ASongEntry
    {
        public uint Header;
        public ushort Player;
        public ushort Unknown;
    }
    class M4ASongHeader
    {
        public byte NumTracks;
        public byte NumBlocks;
        public byte Priority;
        public byte Reverb;
        public uint VoiceTable;
        [BinaryArrayVariableLength("NumTracks")]
        public uint[] Tracks;
    }
    class M4AMLSSSample
    {
        public uint DoesLoop; // Will be 0x40000000 if true
        public uint Frequency; // Right shift 10 for value
        public uint LoopPoint;
        public uint Length;
        // 0x10 - byte[Length] of PCM8 data (Signed for M4A, Unsigned for MLSS)
    }

    [StructLayout(LayoutKind.Explicit)]
    class M4AVoiceEntry : IVoice
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

        [BinaryIgnore]
        [FieldOffset(12)]
        uint offset;
        [BinaryIgnore]
        [FieldOffset(16)]
        string name = string.Empty; // Cache the name

        public bool IsGoldenSunPSG()
        {
            if ((Type & 0x7) != (int)M4AVoiceType.Direct) return false;
            var gSample = new M4AWrappedSample(Address - ROM.Pak).GetSample();
            if (gSample == null) return false;
            return (gSample.bLoop && gSample.LoopPoint == 0 && gSample.Length == 0);
        }
        public bool IsGBInstrument()
        {
            M4AVoiceType vType = (M4AVoiceType)(Type & 0x7);
            return vType >= M4AVoiceType.Square1 && vType <= M4AVoiceType.Noise;
        }
        public bool IsInvalid()
        {
            return (Type & 0x7) >= (int)M4AVoiceType.Invalid5;
        }

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset) => offset = newOffset;

        public string GetName()
        {
            if (name == string.Empty)
            {
                if (Type == (int)M4AVoiceFlags.KeySplit)
                    name = "Key Split";
                else if (Type == (int)M4AVoiceFlags.Drum)
                    name = "Drum";
                else
                {
                    var type = (M4AVoiceType)(Type & 0x7);
                    if (type == M4AVoiceType.Direct)
                        name = IsGoldenSunPSG() ? $"GS {ROM.Instance.Reader.ReadObject<GoldenSunPSG>(Address - ROM.Pak + 0x10).Type}" : "Direct Sound";
                    else
                        name = type.Humanize();
                }
            }
            return name;
        }
        public sbyte GetRootNote() => RootNote;
        public override string ToString()
        {
            string str = GetName();
            var flags = (M4AVoiceFlags)Type;
            if (flags != M4AVoiceFlags.KeySplit && flags != M4AVoiceFlags.Drum)
            {
                if((Type & 0x7) == (int)M4AVoiceType.Direct)
                {
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
                }
                else
                {
                    bool bOFN = (flags & M4AVoiceFlags.OffWithNoise) == M4AVoiceFlags.OffWithNoise;
                    if (bOFN) str += " [ OWN ]";
                }
            }
            return str;
        }
    }
    class M4AWrappedDirect : WrappedVoice
    {
        public readonly M4AWrappedSample Sample;

        public M4AWrappedDirect(M4AVoiceEntry direct) : base(direct) => Sample = new M4AWrappedSample(direct.Address - ROM.Pak);
    }
    class M4AWrappedKeySplit : WrappedVoice
    {
        public readonly M4AVoiceTable Table;
        // VoiceTableSaver helper
        public readonly Triple<byte, byte, byte>[] Keys;

        public M4AWrappedKeySplit(M4AVoiceEntry keySplit) : base(keySplit)
        {
            try
            {
                Table = VoiceTable.LoadTable<M4AVoiceTable>(keySplit.Address - ROM.Pak, true);

                var keys = ROM.Instance.Reader.ReadBytes(128, keySplit.Keys - ROM.Pak);
                var loading = new List<Triple<byte, byte, byte>>(); // Key, min, max
                int prev = -1;
                for (int i = 0; i < 128; i++)
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
        public override string ToString() => $"Key Split ({Keys.Select(k => k.Item1).Distinct().Count()})";
    }
    class M4AWrappedDrum : WrappedVoice
    {
        public readonly M4AVoiceTable Table;

        public M4AWrappedDrum(M4AVoiceEntry drum) : base(drum)
        {
            try
            {
                Table = VoiceTable.LoadTable<M4AVoiceTable>(drum.Address - ROM.Pak, true);
            }
            catch
            {
                Table = null;
            }
        }

        public override IEnumerable<IVoiceTableInfo> GetSubVoices() => Table;
    }
    class M4AWrappedSample : IWrappedSample, IOffset
    {
        uint offset;
        readonly M4AMLSSSample sample;
        readonly WrappedSample gSample;

        public M4AWrappedSample(uint offset)
        {
            this.offset = offset;

            if (offset == 0 || !ROM.IsValidRomOffset(offset))
                goto fail;

            sample = ROM.Instance.Reader.ReadObject<M4AMLSSSample>(offset);

            if (!ROM.IsValidRomOffset(sample.Length + (offset + 0x10)))
                goto fail;

            gSample = new WrappedSample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, false);
            gSample.SetOffset(offset + 0x10);
            return;

            fail:
            sample = new M4AMLSSSample();
            gSample = null;
            Console.WriteLine("Error loading instrument at 0x{0:X}.", offset);
        }

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset)
        {
            offset = newOffset;
            // Yes there will be errors if you set the offset of a bad sample, but only for now
            gSample.SetOffset(offset + 0x10);
        }

        public WrappedSample GetSample() => gSample;
    }

    class GoldenSunPSG
    {
        public byte Unknown; // Always 0x80
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
        uint offset;
        public readonly MLSSVoiceEntry[] Entries;

        public MLSSVoice(uint offset, uint numEntries)
        {
            SetOffset(offset);
            Entries = new MLSSVoiceEntry[numEntries];
            for (uint i = 0; i < numEntries; i++)
            {
                uint off = offset + (i * 8);
                Entries[i] = ROM.Instance.Reader.ReadObject<MLSSVoiceEntry>(off);
                Entries[i].SetOffset(off);
            }
        }

        // Throws exception if it can't find a single
        public MLSSVoiceEntry GetEntryFromNote(sbyte note)
        {
            return Entries.Single(e => e.MinKey <= note && note <= e.MaxKey);
        }

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset) => offset = newOffset;

        public sbyte GetRootNote() => 60;
        public string GetName() => "MLSS";
        public override string ToString() => $"{GetName()} ({Entries.Length})";
    }
    class MLSSWrappedSample : IWrappedSample
    {
        uint offset;
        readonly M4AMLSSSample sample;
        readonly WrappedSample gSample;

        public MLSSWrappedSample(uint offset)
        {
            sample = ROM.Instance.Reader.ReadObject<M4AMLSSSample>(offset);
            gSample = new WrappedSample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, true);
            SetOffset(offset);
        }

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset)
        {
            offset = newOffset;
            gSample.SetOffset(offset + 0x10);
        }

        public WrappedSample GetSample() => gSample;
    }

    class MLSSVoiceEntry : IVoiceTableInfo
    {
        public byte MinKey, MaxKey;
        public byte Sample; // Index in sample table
        public byte IsFixedFrequency, // 0x80 if true
            Unknown1, Unknown2, Unknown3, Unknown4; // Could be ADSR

        [BinaryIgnore]
        uint offset;

        public uint GetOffset() => offset;
        public void SetOffset(uint newOffset) => offset = newOffset;

        public string GetName() => "Voice Entry";
        public override string ToString() => GetName();
    }

    #endregion
}
