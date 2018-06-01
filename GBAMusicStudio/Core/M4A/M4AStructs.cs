using Humanizer;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core.M4A
{
    internal class M4AStructs
    {
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
            internal readonly Voice Voice;
            readonly string name;
            internal SVoice(Voice i)
            {
                Voice = i;
                name = Voice.GetType().Name.Humanize();
            }

            public override string ToString() => name;
        }
        internal class SMulti : SVoice
        {
            internal readonly VoiceTable Table;

            internal SMulti(Split ks) : base(ks) => Table = new VoiceTable();
        }
        internal class SDrum : SVoice
        {
            internal readonly VoiceTable Table;

            static Dictionary<uint, VoiceTable> LoadedDrums = new Dictionary<uint, VoiceTable>(); // Prevent stack overflow
            internal SDrum(Drum d) : base(d)
            {
                if (LoadedDrums.ContainsKey(d.Address))
                {
                    Table = LoadedDrums[d.Address];
                }
                else
                {
                    Table = new VoiceTable();
                    LoadedDrums.Add(d.Address, Table);
                    Table.Load(d.Address);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class Voice
        {
            internal byte Type;
            internal byte RootNote;
            internal byte Padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class Direct_Sound : Voice // 0x0, 0x8
        {
            internal byte Panpot;
            internal uint Address;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Square_1 : Voice // 0x1, 0x9
        {
            internal byte Sweep;
            internal byte Pattern;
            internal byte Padding2, Padding3, Padding4;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Square_2 : Voice // 0x2, 0xA
        {
            internal byte Padding2;
            internal byte Pattern;
            internal byte Padding3, Padding4, Padding5;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Wave : Voice // 0x3, 0xB
        {
            internal byte Padding2;
            internal uint Address;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Noise : Voice // 0x4, 0xC
        {
            internal byte Panpot;
            internal byte Pattern;
            internal byte Padding2, Padding3, Padding4;
            internal byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Split : Voice // 0x40
        {
            internal byte Padding2;
            internal uint Table, Keys;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Drum : Voice // 0x80
        {
            internal byte Padding2;
            internal uint Address;
            internal uint Padding3;
        }
    }
}
