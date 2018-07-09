using GBAMusicStudio.Util;
using Humanizer;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core
{
    interface IVoice
    {
        byte GetRootNote();
    }

    internal class M4AStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SongEntry
        {
            internal uint Header;
            internal ushort Player;
            internal ushort Unknown;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SongHeader
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
        internal struct Sample
        {
            internal uint DoesLoop; // Will be 0x40000000 if true
            internal uint Frequency; // Divide this by 1024
            internal uint LoopPoint;
            internal uint Length;
            // 0x10 - byte[Length] of unsigned PCM8 data
        }

        internal class SVoice
        {
            internal readonly IVoice Voice;
            readonly string name;
            internal SVoice(IVoice i)
            {
                Voice = i;
                name = Voice.GetType().Name.Humanize();
            }

            public override string ToString() => name;
        }
        internal class SDirect : SVoice
        {
            internal readonly Sample Sample;

            internal SDirect(Direct_Sound direct) : base(direct) => Sample = ROM.Instance.ReadStruct<Sample>(direct.Address);
        }
        internal class SMulti : SVoice
        {
            internal readonly M4AVoiceTable Table;
            internal readonly Triple<byte, byte, byte>[] Keys;

            internal static readonly Dictionary<uint, M4AVoiceTable> LoadedMultis = new Dictionary<uint, M4AVoiceTable>(); // Prevent stack overflow
            internal SMulti(Split ks) : base(ks)
            {
                if (LoadedMultis.ContainsKey(ks.Table))
                {
                    Table = LoadedMultis[ks.Table];
                }
                else
                {
                    Table = new M4AVoiceTable();
                    LoadedMultis.Add(ks.Table, Table);
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
        internal class SDrum : SVoice
        {
            internal readonly M4AVoiceTable Table;

            internal static readonly Dictionary<uint, M4AVoiceTable> LoadedDrums = new Dictionary<uint, M4AVoiceTable>(); // Prevent stack overflow
            internal SDrum(Drum d) : base(d)
            {
                if (LoadedDrums.ContainsKey(d.Table))
                {
                    Table = LoadedDrums[d.Table];
                }
                else
                {
                    Table = new M4AVoiceTable();
                    LoadedDrums.Add(d.Table, Table);
                    Table.Load(d.Table);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class M4AVoice : IVoice
        {
            internal byte Type;
            internal byte RootNote;
            internal byte Padding;

            public byte GetRootNote() => RootNote;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class Direct_Sound : M4AVoice // 0x0, 0x8
        {
            internal byte Panpot;
            internal uint Address;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Square_1 : M4AVoice // 0x1, 0x9
        {
            internal byte Sweep;
            internal byte Pattern;
            internal byte Padding2, Padding3, Padding4;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Square_2 : M4AVoice // 0x2, 0xA
        {
            internal byte Padding2;
            internal byte Pattern;
            internal byte Padding3, Padding4, Padding5;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Wave : M4AVoice // 0x3, 0xB
        {
            internal byte Padding2;
            internal uint Address;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Noise : M4AVoice // 0x4, 0xC
        {
            internal byte Panpot;
            internal byte Pattern;
            internal byte Padding2, Padding3, Padding4;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Split : M4AVoice // 0x40
        {
            internal byte Padding2;
            internal uint Table, Keys;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Drum : M4AVoice // 0x80
        {
            internal byte Padding2;
            internal uint Table;
            internal uint Padding3;
        }
    }
}
