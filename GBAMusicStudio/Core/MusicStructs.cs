using GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core
{
    internal interface IVoice
    {
        sbyte GetRootNote();
    }
    internal class SVoice
    {
        internal readonly IVoice Voice;
        readonly string name;
        internal SVoice(IVoice i)
        {
            Voice = i;
            name = Voice.ToString();
        }

        public override string ToString() => name;
    }
    internal interface ISample
    {
        Sample GetSample();
    }
    internal class Sample
    {
        internal bool bLoop, bUnsigned;
        internal uint LoopPoint, Length;
        internal float Frequency;
        internal uint Offset; // Offset of the PCM buffer, not of the header

        internal Sample(bool loop, uint loopPoint, uint length, float frequency, uint offset, bool unsigned)
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

    internal class M4ASDirect : SVoice
    {
        internal readonly M4ASSample Sample;

        internal M4ASDirect(M4AVoice direct) : base(direct) => Sample = new M4ASSample(direct.Address);
    }
    internal class M4ASWave : SVoice
    {
        internal readonly byte[] sample;

        internal M4ASWave(M4AVoice wave) : base(wave) => sample = ROM.Instance.ReadBytes(16, wave.Address);
    }
    internal class M4ASMulti : SVoice
    {
        internal readonly M4AVoiceTable Table;
        internal readonly Triple<byte, byte, byte>[] Keys;

        internal M4ASMulti(M4AVoice ks) : base(ks)
        {
            Table = VoiceTable.LoadTable<M4AVoiceTable>(ks.Table, true);

            var keys = ROM.Instance.ReadBytes(256, ks.Keys);
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
    }
    internal class M4ASDrum : SVoice
    {
        internal readonly M4AVoiceTable Table;

        internal M4ASDrum(M4AVoice d) : base(d)
        {
            Table = VoiceTable.LoadTable<M4AVoiceTable>(d.Table, true);
        }
    }
    internal class M4ASSample : ISample
    {
        internal readonly uint Offset;
        readonly M4AMLSSSample sample;
        readonly Sample gSample;

        internal M4ASSample(uint offset)
        {
            Offset = offset;
            sample = ROM.Instance.ReadStruct<M4AMLSSSample>(offset);

            if (offset == 0 || !ROM.IsValidRomOffset(offset - ROM.Pak) || !ROM.IsValidRomOffset(sample.Length + (offset + 0x10) - ROM.Pak))
                goto fail;
            
            gSample = new Sample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, offset + 0x10, false);
            return;

            fail:
            Console.WriteLine("Error loading instrument at 0x{0:X}.", offset);
        }

        public Sample GetSample() => gSample;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal class M4AVoice : IVoice
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
        internal uint Address; // Direct, Wave
        [FieldOffset(4)]
        internal uint Table; // Multi, Drum

        [FieldOffset(8)]
        internal ADSR ADSR; // Direct, Square1, Square2, Wave, Noise
        [FieldOffset(8)]
        internal uint Keys; // Multi


        public sbyte GetRootNote() => RootNote;
        public bool IsGoldenSunPSG()
        {
            if (Type != 0x0 && Type != 0x8) return false;
            var gSample = new M4ASSample(Address).GetSample();
            if (gSample == null) return false;
            return (gSample.bLoop && gSample.LoopPoint == 0 && gSample.Length == 0);
        }
        public override string ToString()
        {
            switch (Type)
            {
                case 0x0:
                case 0x8: return IsGoldenSunPSG() ? $"GS {ROM.Instance.ReadStruct<GoldenSunPSG>(Address + 0x10).Type.ToString()}" : "Direct Sound";
                case 0x1:
                case 0x9: return "Square 1";
                case 0x2:
                case 0xA: return "Square 2";
                case 0x3:
                case 0xB: return "Wave";
                case 0x4:
                case 0xC: return "Noise";
                case 0x40: return "Key Split";
                case 0x80: return "Drum";
                default: return Type.ToString();
            }
        }
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

    internal class MLSSVoice : IVoice
    {
        internal readonly uint Offset;
        internal readonly MLSSVoiceEntry[] Entries;

        internal MLSSVoice(uint offset, uint numEntries)
        {
            Offset = offset;
            Entries = new MLSSVoiceEntry[numEntries];
            for (int i = 0; i < numEntries; i++)
                Entries[i] = ROM.Instance.ReadStruct<MLSSVoiceEntry>((uint)(offset + (i * 8)));
        }

        // Throws exception if it can't find a single
        internal MLSSVoiceEntry GetEntryFromNote(sbyte note)
        {
            return Entries.Single(e => e.MinKey <= note && note <= e.MaxKey);
        }

        public sbyte GetRootNote() => 60;
        public override string ToString() => "MLSS";
    }
    internal class MLSSSSample : ISample
    {
        internal readonly uint Offset;
        readonly M4AMLSSSample sample;
        readonly Sample gSample;

        internal MLSSSSample(uint offset)
        {
            Offset = offset;
            sample = ROM.Instance.ReadStruct<M4AMLSSSample>(offset);
            gSample = new Sample(sample.DoesLoop == 0x40000000, sample.LoopPoint, sample.Length, sample.Frequency >> 10, offset + 0x10, true);
        }

        public Sample GetSample() => gSample;
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
