namespace Kermalis.MusicStudio.Core
{
    public enum PlayerState : byte
    {
        Stopped,
        Playing,
        Paused,
        ShutDown
    }

    delegate void SongEndedEvent();

    interface IPlayer
    {
        PlayerState State { get; }
        event SongEndedEvent SongEnded;

        string LoadSong(int index);
        void Play();
        void Pause();
        void Stop();
        void ShutDown();
        void GetSongState(UI.TrackInfo info);
    }
}
