using System;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    enum GoldenSunPSGType : byte
    {
        Square,
        Saw,
        Triangle
    }
    enum LFOType : byte
    {
        Pitch,
        Volume,
        Panpot
    }
    enum SquarePattern : byte
    {
        D12,
        D25,
        D50,
        D75
    }
    enum NoisePattern : byte
    {
        Fine,
        Rough
    }
    enum VoiceType
    {
        PCM8,
        Square1,
        Square2,
        PCM4,
        Noise,
        Invalid5,
        Invalid6,
        Invalid7
    }
    [Flags]
    enum VoiceFlags : byte
    {
        // These are flags that apply to the types
        Fixed = 0x08, // PCM8
        OffWithNoise = 0x08, // Square1, Square2, PCM4, Noise
        Reversed = 0x10, // PCM8
        Compressed = 0x20, // PCM8 (Only in Pokémon main series games)

        // These are flags that cancel out every other bit after them if set so they should only be checked with equality
        KeySplit = 0x40,
        Drum = 0x80
    }

    enum EnvelopeState
    {
        Initializing,
        Rising,
        Decaying,
        Playing,
        Releasing,
        Dying,
        Dead
    }
}
