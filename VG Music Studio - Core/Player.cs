using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core;

public enum PlayerState : byte
{
	Stopped,
	Playing,
	Paused,
	Recording,
	ShutDown,
}

public interface ILoadedSong
{
	List<SongEvent>[] Events { get; }
	long MaxTicks { get; }
	long ElapsedTicks { get; }
}

public interface IPlayer : IDisposable
{
	ILoadedSong? LoadedSong { get; }
	bool ShouldFadeOut { get; set; }
	long NumLoops { get; set; }

	PlayerState State { get; }
	event Action? SongEnded;

	void LoadSong(long index);
	void SetCurrentPosition(long ticks);
	void Play();
	void Pause();
	void Stop();
	void Record(string fileName);
	void UpdateSongState(SongState info);
}
