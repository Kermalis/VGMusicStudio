namespace GBAMusicStudio.Core
{
    internal enum EngineType { M4A, MLSS }

    internal enum ADSRState { Initializing, Rising, Decaying, Playing, Releasing, Dying, Dead }
    internal enum PlayerState { Stopped, Playing, Paused, ShutDown }

    internal enum MODType : byte { Vibrate, Volume, Panpot }
    internal enum GBType { Square1, Square2, Wave, Noise }
    internal enum ReverbType { None, Normal }
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
