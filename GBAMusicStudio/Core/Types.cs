using System;

namespace GBAMusicStudio.Core
{
    enum EngineType { M4A, MLSS }

    enum ADSRState { Initializing, Rising, Decaying, Playing, Releasing, Dying, Dead }
    enum PlayerState { Stopped, Playing, Paused, ShutDown }

    enum M4AVoiceType { Direct, Square1, Square2, Wave, Noise, Invalid5, Invalid6, Invalid7 }
    [Flags]
    enum M4AVoiceFlags : byte
    {
        // These are flags that apply to the types
        Fixed = 0x08, // Direct
        OffWithNoise = 0x08, // Applies to the others
        Reversed = 0x10, // Direct
        Compressed = 0x20, // Direct

        // These are flags that cancel out every other bit after them if set
        // Therefore you should check with equality only
        KeySplit = 0x40,
        Drum = 0x80
    }

    enum MODType : byte { Vibrate, Volume, Panpot }
    enum GSPSGType : byte { Square, Saw, Triangle }
    enum ReverbType { Normal, Camelot1, Camelot2, MGAT, None }
    enum SquarePattern : byte { D12, D25, D50, D75 }
    enum NoisePattern : byte { Fine, Rough }


    struct ChannelVolume
    {
        public float FromLeftVol, FromRightVol,
            ToLeftVol, ToRightVol;
    }
    struct ADSR { public byte A, D, S, R; }
    struct Note
    {
        public sbyte Key;
        public sbyte OriginalKey;
        public byte Velocity;
        public int Duration; // -1 = forever
    }
}
