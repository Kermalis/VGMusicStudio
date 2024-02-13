using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamPlayer : Player
{
	internal const int NUM_TRACKS = 12; // 8 PCM, 4 PSG

	protected override string Name => "AlphaDream Player";

	internal readonly AlphaDreamTrack[] Tracks;
	internal readonly AlphaDreamConfig Config;
	private readonly AlphaDreamMixer _mixer;
	private AlphaDreamLoadedSong? _loadedSong;

	internal byte Tempo;
	internal int TempoStack;
	private long _elapsedLoops;

	public override ILoadedSong? LoadedSong => _loadedSong;
	protected override Mixer Mixer => _mixer;

	internal AlphaDreamPlayer(AlphaDreamConfig config, AlphaDreamMixer mixer)
		: base(GBAUtils.AGB_FPS)
	{
		Config = config;
		_mixer = mixer;

		Tracks = new AlphaDreamTrack[NUM_TRACKS];
		for (byte i = 0; i < NUM_TRACKS; i++)
		{
			Tracks[i] = new AlphaDreamTrack(i, mixer);
		}
	}

	public override void LoadSong(int index)
	{
		if (_loadedSong is not null)
		{
			_loadedSong = null;
		}

		int songPtr = Config.SongTableOffsets[0] + (index * 4);
		int songOffset = ReadInt32LittleEndian(Config.ROM.AsSpan(songPtr));
		if (songOffset == 0)
		{
			return;
		}

		// If there's an exception, this will remain null
		_loadedSong = new AlphaDreamLoadedSong(this, songOffset);
		_loadedSong.SetTicks();
	}
	public override void UpdateSongState(SongState info)
	{
		info.Tempo = Tempo;
		for (int i = 0; i < NUM_TRACKS; i++)
		{
			AlphaDreamTrack track = Tracks[i];
			if (track.IsEnabled)
			{
				track.UpdateSongState(info.Tracks[i]);
			}
		}
	}
	internal override void InitEmulation()
	{
		Tempo = 120; // Player tempo is set to 75 on init, but I did not separate player and track tempo yet
		TempoStack = 0;
		_elapsedLoops = 0;
		ElapsedTicks = 0;
		_mixer.ResetFade();
		for (int i = 0; i < NUM_TRACKS; i++)
		{
			Tracks[i].Init();
		}
	}
	protected override void SetCurTick(long ticks)
	{
		_loadedSong!.SetCurTick(ticks);
	}
	protected override void OnStopped()
	{
		//
	}

	protected override bool Tick(bool playing, bool recording)
	{
		bool allDone = false; // TODO: Individual track tempo
		while (!allDone && TempoStack >= 75)
		{
			TempoStack -= 75;
			allDone = true;
			for (int i = 0; i < NUM_TRACKS; i++)
			{
				AlphaDreamTrack track = Tracks[i];
				if (track.IsEnabled)
				{
					TickTrack(track, ref allDone);
				}
			}
			if (_mixer.IsFadeDone())
			{
				allDone = true;
			}
		}
		if (!allDone)
		{
			TempoStack += Tempo;
		}
		_mixer.Process(Tracks, playing, recording);
		return allDone;
	}
	private void TickTrack(AlphaDreamTrack track, ref bool allDone)
	{
		byte prevDuration = track.NoteDuration;
		track.Tick();
		bool update = false;
		while (track.Rest == 0 && !track.Stopped)
		{
			_loadedSong!.ExecuteNext(track, ref update);
		}
		if (track.Index == _loadedSong!.LongestTrack)
		{
			HandleTicksAndLoop(_loadedSong, track);
		}
		if (prevDuration == 1 && track.NoteDuration == 0) // Note was not renewed
		{
			track.Channel.State = EnvelopeState.Release;
		}
		if (track.NoteDuration != 0) // A note is playing
		{
			allDone = false;
			if (update)
			{
				track.Channel.SetVolume(track.Volume, track.Panpot);
				track.Channel.SetPitch(track.GetPitch());
			}
		}
		if (!track.Stopped)
		{
			allDone = false;
		}
	}
	private void HandleTicksAndLoop(AlphaDreamLoadedSong s, AlphaDreamTrack track)
	{
		if (ElapsedTicks != s.MaxTicks)
		{
			ElapsedTicks++;
			return;
		}

		// Track reached the detected end, update loops/ticks accordingly
		if (track.Stopped)
		{
			return;
		}

		_elapsedLoops++;
		UpdateElapsedTicksAfterLoop(s.Events[track.Index]!, track.DataOffset, track.Rest);
		if (ShouldFadeOut && _elapsedLoops > NumLoops && !_mixer.IsFading())
		{
			_mixer.BeginFadeOut();
		}
	}
}
