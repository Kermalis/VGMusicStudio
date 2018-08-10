using GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
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
        Sample ToSample();
    }
    internal class Sample
    {
        internal bool bLoop;
        internal uint LoopPoint, Length;
        internal float Frequency;
        internal sbyte[] Data; // Signed 8-bit

        internal Sample(bool loop, uint loopPoint, uint length, float frequency, sbyte[] data)
        {
            bLoop = loop; LoopPoint = loopPoint; Length = length; Frequency = frequency; Data = data;
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
    internal struct M4ASample
    {
        internal uint DoesLoop; // Will be 0x40000000 if true
        internal uint Frequency; // Divide this by 1024
        internal uint LoopPoint;
        internal uint Length;
        // 0x10 - byte[Length] of signed PCM8 data
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

        internal static readonly Dictionary<uint, M4AVoiceTable> Cache = new Dictionary<uint, M4AVoiceTable>(); // Prevent stack overflow
        internal M4ASMulti(M4AVoice ks) : base(ks)
        {
            if (Cache.ContainsKey(ks.Table))
            {
                Table = Cache[ks.Table];
            }
            else
            {
                Table = new M4AVoiceTable();
                Cache.Add(ks.Table, Table);
                Table.Load(ks.Table);
            }

            var keys = ROM.Instance.ReadBytes(256, ks.Keys);
            var loading = new List<Triple<byte, byte, byte>>();
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

        internal static readonly Dictionary<uint, M4AVoiceTable> Cache = new Dictionary<uint, M4AVoiceTable>(); // Prevent stack overflow
        internal M4ASDrum(M4AVoice d) : base(d)
        {
            if (Cache.ContainsKey(d.Table))
            {
                Table = Cache[d.Table];
            }
            else
            {
                Table = new M4AVoiceTable();
                Cache.Add(d.Table, Table);
                Table.Load(d.Table);
            }
        }
    }
    internal class M4ASSample : ISample
    {
        internal readonly uint Offset;
        internal readonly M4ASample Sample;

        internal M4ASSample(uint offset)
        {
            Offset = offset;
            Sample = ROM.Instance.ReadStruct<M4ASample>(offset);
        }

        public Sample ToSample()
        {
            // 8 for Golden Sun
            var buf = ROM.Instance.ReadBytes(Sample.Length == 0 ? 8 : Sample.Length, Offset + 0x10);
            return new Sample(Sample.DoesLoop == 0x40000000, Sample.LoopPoint, Sample.Length, Sample.Frequency / 1024, (sbyte[])(Array)buf);
        }
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
        internal byte Pattern; // Square1, Square2, Noise
        [FieldOffset(4)]
        internal uint Address; // Direct, Wave
        [FieldOffset(4)]
        internal uint Table; // Multi, Drum

        [FieldOffset(8)]
        internal ADSR ADSR; // Direct, Square1, Square2, Wave, Noise
        [FieldOffset(8)]
        internal uint Keys; // Multi


        public sbyte GetRootNote() => RootNote;
        public override string ToString()
        {
            switch (Type)
            {
                case 0x0:
                case 0x8: return "Direct Sound";
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

    #endregion

    #region MLSS

    [StructLayout(LayoutKind.Sequential)]
    internal struct MLSSVoice : IVoice
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        internal byte[] Bytes;

        public sbyte GetRootNote() => 60;
        public override string ToString() => "MLSS";
    }

    #endregion
}
