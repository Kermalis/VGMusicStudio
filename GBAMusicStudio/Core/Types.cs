using System;

namespace GBAMusicStudio.Core
{
    internal enum EngineType { M4A, MLSS }

    internal enum ADSRState { Initializing, Rising, Decaying, Playing, Releasing, Dying, Dead }
    internal enum PlayerState { Stopped, Playing, Paused, ShutDown }

    internal enum M4AVoiceType { Direct, Square1, Square2, Wave, Noise, Invalid5, Invalid6, Invalid7}
    [Flags]
    internal enum M4AVoiceFlags : byte
    {
        // These are flags that apply to the types
        Fixed = 0x08, // Direct
        OffWithNoise = 0x08, // Applies to the others
        Reversed = 0x10, // Direct
        Compressed = 0x20, // Direct

        // These are flags that cancel out every other bit after them if set
        // Therefore, you should check with equality only
        KeySplit = 0x40,
        Drum = 0x80
    }

    internal enum MODType : byte { Vibrate, Volume, Panpot }
    internal enum GSPSGType : byte { Square, Saw, Triangle }
    internal enum ReverbType { Normal, Camelot1, Camelot2, MGAT, None }
    internal enum SquarePattern : byte { D12, D25, D50, D75 }
    internal enum NoisePattern : byte { Fine, Rough }


    internal struct ChannelVolume
    {
        internal float FromLeftVol, FromRightVol,
            ToLeftVol, ToRightVol;
    }
    internal struct ADSR { internal byte A, D, S, R; }
    internal struct Note
    {
        internal sbyte Key;
        internal sbyte OriginalKey;
        internal byte Velocity;
        internal int Duration; // -1 = forever
    }
}
