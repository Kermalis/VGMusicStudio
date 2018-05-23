using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GBAMusic.Core
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
            public ushort Padding;
            public ushort DoesLoop; // Will be 0x4000 if true
            public uint Frequency; // Divide this by 1024
            public uint LoopPoint;
            public uint Length;
            // 0x10 - byte[Length] of raw data
        }

        internal class Voice
        {
            internal SVoice Instrument;
            internal Voice(SVoice i) => Instrument = i;

            public override string ToString() => Instrument.GetType().Name;
        }
        internal class Multi : Voice
        {
            internal VoiceTable Table;

            internal Multi(KeySplit ks) : base(ks) => Table = new VoiceTable();
        }
        internal class Drum : Voice
        {
            internal VoiceTable Table;

            static Dictionary<uint, VoiceTable> LoadedDrums = new Dictionary<uint, VoiceTable>(); // Prevent stack overflow
            internal Drum(SDrum d, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds) : base(d)
            {
                if (LoadedDrums.ContainsKey(d.Address))
                {
                    Table = LoadedDrums[d.Address];
                }
                else
                {
                    Table = new VoiceTable();
                    LoadedDrums.Add(d.Address, Table);
                    Table.LoadDirectSamples(d.Address, system, sounds);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SVoice
        {
            public byte VoiceType;
            public byte RootNote;
            public byte Padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class DirectSound : SVoice // 0x0, 0x8
        {
            public byte Panpot;
            public uint Address;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class SquareWave1 : SVoice // 0x1, 0x9
        {
            public byte Sweep;
            public byte Pattern;
            public byte Padding2, Padding3, Padding4;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class SquareWave2 : SVoice // 0x2, 0xA
        {
            public byte Padding2;
            public byte Pattern;
            public byte Padding3, Padding4, Padding5;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class GBWave : SVoice // 0x3, 0xB
        {
            public byte Padding2;
            public uint Address;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class Noise : SVoice // 0x4, 0xC
        {
            public byte Padding2;
            public byte Pattern;
            public byte Padding3, Padding4, Padding5;
            public byte A, D, S, R;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class KeySplit : SVoice // 0x40
        {
            public byte Padding2;
            public uint Table, Keys;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class SDrum : SVoice // 0x80
        {
            public byte Padding2;
            public uint Address;
            public uint Padding3;
        }
    }
}
