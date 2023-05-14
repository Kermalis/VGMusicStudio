using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
	List<SongEvent>?[] Events { get; }
	long MaxTicks { get; }
}

public abstract class Player : IDisposable
{
	protected abstract string Name { get; }
	protected abstract Mixer Mixer { get; }

	public abstract ILoadedSong? LoadedSong { get; }
	public bool ShouldFadeOut { get; set; }
	public long NumLoops { get; set; }

	public long ElapsedTicks { get; internal set; }
	public PlayerState State { get; protected set; }
	public event Action? SongEnded;

	private readonly TimeBarrier _time;
	private Thread? _thread;

	protected Player(double ticksPerSecond)
	{
		_time = new TimeBarrier(ticksPerSecond);
	}

	public abstract void LoadSong(int index);
	public abstract void UpdateSongState(SongState info);
	internal abstract void InitEmulation();
	protected abstract void SetCurTick(long ticks);
	protected abstract void OnStopped();

	protected abstract bool Tick(bool playing, bool recording);

	protected void CreateThread()
	{
		_thread = new Thread(TimerTick) { Name = Name + " Tick" };
		_thread.Start();
	}
	protected void WaitThread()
	{
		if (_thread is not null && (_thread.ThreadState is ThreadState.Running or ThreadState.WaitSleepJoin))
		{
			_thread.Join();
		}
	}
	protected void UpdateElapsedTicksAfterLoop(List<SongEvent> evs, long trackEventOffset, long trackRest)
	{
		for (int i = 0; i < evs.Count; i++)
		{
			SongEvent ev = evs[i];
			if (ev.Offset == trackEventOffset)
			{
				ElapsedTicks = ev.Ticks[0] - trackRest;
				return;
			}
		}
		throw new InvalidDataException("No loop point found");
	}

	public void Play()
	{
		if (LoadedSong is null)
		{
			SongEnded?.Invoke();
			return;
		}

		if (State is not PlayerState.ShutDown)
		{
			Stop();
			InitEmulation();
			State = PlayerState.Playing;
			CreateThread();
		}
	}
	public void TogglePlaying()
	{
		switch (State)
		{
			case PlayerState.Playing:
			{
				State = PlayerState.Paused;
				WaitThread();
				break;
			}
			case PlayerState.Paused:
			case PlayerState.Stopped:
			{
				State = PlayerState.Playing;
				CreateThread();
				break;
			}
		}
	}
	public void Stop()
	{
		if (State is PlayerState.Playing or PlayerState.Paused)
		{
			State = PlayerState.Stopped;
			WaitThread();
			OnStopped();
		}
	}
	public void Record(string fileName)
	{
		Mixer.CreateWaveWriter(fileName);

		InitEmulation();
		State = PlayerState.Recording;
		CreateThread();
		WaitThread();

		Mixer.CloseWaveWriter();
	}
	public void SetSongPosition(long ticks)
	{
		if (LoadedSong is null)
		{
			SongEnded?.Invoke();
			return;
		}

		if (State is not PlayerState.Playing and not PlayerState.Paused and not PlayerState.Stopped)
		{
			return;
		}

		if (State is PlayerState.Playing)
		{
			TogglePlaying();
		}
		InitEmulation();
		SetCurTick(ticks);
		TogglePlaying();
	}

	private void TimerTick()
	{
		_time.Start();
		while (true)
		{
			PlayerState state = State;
			bool playing = state == PlayerState.Playing;
			bool recording = state == PlayerState.Recording;
			if (!playing && !recording)
			{
				break;
			}

			bool allDone = Tick(playing, recording);
			if (allDone)
			{
				// TODO: lock state
				_time.Stop(); // TODO: Don't need timer if recording
				State = PlayerState.Stopped;
				SongEnded?.Invoke();
				return;
			}
			if (playing)
			{
				_time.Wait();
			}
		}
		_time.Stop();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		if (State != PlayerState.ShutDown)
		{
			State = PlayerState.ShutDown;
			WaitThread();
		}
		SongEnded = null;
	}
}
