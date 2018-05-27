using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core.M4A
{
    internal class M4AStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SongHeader
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
        internal struct Sample
        {
            public uint DoesLoop; // Will be 0x40000000 if true
            public uint Frequency; // Divide this by 1024
            public uint LoopPoint;
            public uint Length;
            // 0x10 - byte[Length] of unsigned PCM8 data
        }

        internal class SVoice
        {
            internal Voice Instrument;
            string name;
            internal SVoice(Voice i)
            {
                Instrument = i;
                name = Instrument.GetType().Name.Replace('_', ' ');
            }

            public override string ToString() => name;
        }
        internal class SMulti : SVoice
        {
            internal VoiceTable Table;

            internal SMulti(Split ks) : base(ks) => Table = new VoiceTable();
        }
        internal class SDrum : SVoice
        {
            internal VoiceTable Table;

            static Dictionary<uint, VoiceTable> LoadedDrums = new Dictionary<uint, VoiceTable>(); // Prevent stack overflow
            internal SDrum(Drum d, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds) : base(d)
            {
                if (LoadedDrums.ContainsKey(d.Address))
                {
                    Table = LoadedDrums[d.Address];
                }
                else
                {
                    Table = new VoiceTable();
                    LoadedDrums.Add(d.Address, Table);
                    Table.LoadPCMSamples(d.Address, system, sounds);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class Voice
        {
            public byte VoiceType;
            public byte RootNote;
            public byte Padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class Direct_Sound : Voice // 0x0, 0x8
        {
            public byte Panpot;
            public uint Address;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Square_1 : Voice // 0x1, 0x9
        {
            public byte Sweep;
            public byte Pattern;
            public byte Padding2, Padding3, Padding4;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Square_2 : Voice // 0x2, 0xA
        {
            public byte Padding2;
            public byte Pattern;
            public byte Padding3, Padding4, Padding5;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class GB_Wave : Voice // 0x3, 0xB
        {
            public byte Padding2;
            public uint Address;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class PSG_Noise : Voice // 0x4, 0xC
        {
            public byte Padding2;
            public byte Pattern;
            public byte Padding3, Padding4, Padding5;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Split : Voice // 0x40
        {
            public byte Padding2;
            public uint Table, Keys;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Drum : Voice // 0x80
        {
            public byte Padding2;
            public uint Address;
            public uint Padding3;
        }
    }
}
