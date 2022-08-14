using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core
{
    internal enum PlayerState : byte
    {
        Stopped = 0,
        Playing,
        Paused,
        Recording,
        ShutDown
    }

    internal delegate void SongEndedEvent();

    internal interface IPlayer : IDisposable
    {
        List<SongEvent>[] Events { get; }
        long MaxTicks { get; }
        long ElapsedTicks { get; }
        bool ShouldFadeOut { get; set; }
        long NumLoops { get; set; }

        PlayerState State { get; }
        event SongEndedEvent SongEnded;

        void LoadSong(long index);
        void SetCurrentPosition(long ticks);
        void Play();
        void Pause();
        void Stop();
        void Record(string fileName);
        void GetSongState(UI.SongInfoControl.SongInfo info);
    }
}
