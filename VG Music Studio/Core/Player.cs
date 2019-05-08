using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core
{
    internal enum PlayerState : byte
    {
        Stopped,
        Playing,
        Paused,
        ShutDown
    }

    internal delegate void SongEndedEvent();

    internal interface IPlayer : IDisposable
    {
        List<SongEvent>[] Events { get; }
        long NumTicks { get; }

        PlayerState State { get; }
        event SongEndedEvent SongEnded;

        void LoadSong(long index);
        void SetCurrentPosition(long ticks);
        void Play();
        void Pause();
        void Stop();
        void GetSongState(UI.TrackInfoControl.TrackInfo info);
    }
}
