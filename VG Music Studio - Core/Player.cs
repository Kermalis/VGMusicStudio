using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Kermalis.VGMusicStudio.Core.Util;

namespace Kermalis.VGMusicStudio.Core;

public enum PlayerState : byte
{
	Stopped,
	Playing,
	Paused,
	Recording,
	ShutDown,
}

public abstract class Player(double ticksPerSecond) : IDisposable
{
	protected abstract string Name { get; }
	protected abstract Mixer Mixer { get; }

	public abstract ILoadedSong? LoadedSong { get; }
	public abstract ushort Tempo { get; set; }
	public bool ShouldFadeOut { get; set; }
	public long NumLoops { get; set; }
	public SongState? Info { get; set; }

	public long ElapsedTicks { get; internal set; }
	public PlayerState State { get; protected set; }
	public Exception? ErrorDetails { get; set; }

	public event Action? SongEnded;

	private readonly BetterTimer _timer = new(ticksPerSecond);
	private Thread? _thread;
	private double? _deltaTimeElapsed;
	public bool IsStreamStopped = true;
	public bool IsPauseToggled = false;

	public abstract void LoadSong(int index);
	public virtual void LoadSong(LoadedSong song) { }
	public virtual void RefreshSong() { }
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
			if (State is not PlayerState.Stopped)
			{
				Stop();
			}
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
			ElapsedTicks = 0L;
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

		if (State is PlayerState.Stopped)
		{
			Play();
		}

		if (State is PlayerState.Playing)
		{
			TogglePlaying();
		}
		InitEmulation();
		SetCurTick(ticks);
		if (State is PlayerState.Paused && !IsPauseToggled || State is PlayerState.Stopped)
		{
			TogglePlaying();
		}
	}

	private void TimerTick()
	{
		_deltaTimeElapsed = 0;
		_timer.Start();
		while (true)
		{
			var state = State;
			var playing = state == PlayerState.Playing;
			var recording = state == PlayerState.Recording;
			if (!playing && !recording)
			{
				break;
			}
			_deltaTimeElapsed += _timer.GetDeltaTime();
			while (_deltaTimeElapsed >= _timer.GetDeltaTick())
			{
				_deltaTimeElapsed -= _timer.GetDeltaTick();
				try
				{
					bool allDone = Tick(playing, recording);
					if (Info is not null)
					{
						UpdateSongState(Info);
					}
					if (allDone)
					{
						// TODO: lock state
						_timer.Stop(); // TODO: Don't need timer if recording
						SongEnded?.Invoke();
						return;
					}
				}
				catch (Exception ex)
				{
					ErrorDetails = ex;
				}
			}
		}
		_timer.Stop();
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
