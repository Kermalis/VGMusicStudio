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

    internal interface IPlayer
    {
        PlayerState State { get; }
        event SongEndedEvent SongEnded;

        string LoadSong(int index);
        void Play();
        void Pause();
        void Stop();
        void ShutDown();
        void GetSongState(UI.TrackInfoControl.TrackInfo info);
    }
}
