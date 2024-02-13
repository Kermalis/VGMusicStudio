using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEPlayer : Player
{
	protected override string Name => "DSE Player";

	private readonly DSEConfig _config;
	internal readonly DSEMixer DMixer;
	internal readonly SWD MasterSWD;
	private DSELoadedSong? _loadedSong;

	internal byte Tempo;
	internal int TempoStack;
	private long _elapsedLoops;

	public override ILoadedSong? LoadedSong => _loadedSong;
	protected override Mixer Mixer => DMixer;

	public DSEPlayer(DSEConfig config, DSEMixer mixer)
		: base(192)
	{
		DMixer = mixer;
		_config = config;

		MasterSWD = new SWD(Path.Combine(config.BGMPath, "bgm.swd"));
	}

	public override void LoadSong(int index)
	{
		if (_loadedSong is not null)
		{
			_loadedSong = null;
		}

		// If there's an exception, this will remain null
		_loadedSong = new DSELoadedSong(this, _config.BGMFiles[index]);
		_loadedSong.SetTicks();
	}
	public override void UpdateSongState(SongState info)
	{
		info.Tempo = Tempo;
		_loadedSong!.UpdateSongState(info);
	}
	internal override void InitEmulation()
	{
		Tempo = 120;
		TempoStack = 0;
		_elapsedLoops = 0;
		ElapsedTicks = 0;
		DMixer.ResetFade();
		DSETrack[] tracks = _loadedSong!.Tracks;
		for (int i = 0; i < tracks.Length; i++)
		{
			tracks[i].Init();
		}
	}
	protected override void SetCurTick(long ticks)
	{
		_loadedSong!.SetCurTick(ticks);
	}
	protected override void OnStopped()
	{
		DSETrack[] tracks = _loadedSong!.Tracks;
		for (int i = 0; i < tracks.Length; i++)
		{
			tracks[i].StopAllChannels();
		}
	}

	protected override bool Tick(bool playing, bool recording)
	{
		DSELoadedSong s = _loadedSong!;

		bool allDone = false;
		while (!allDone && TempoStack >= 240)
		{
			TempoStack -= 240;
			allDone = true;
			for (int i = 0; i < s.Tracks.Length; i++)
			{
				TickTrack(s, s.Tracks[i], ref allDone);
			}
			if (DMixer.IsFadeDone())
			{
				allDone = true;
			}
		}
		if (!allDone)
		{
			TempoStack += Tempo;
		}
		DMixer.ChannelTick();
		DMixer.Process(playing, recording);
		return allDone;
	}
	private void TickTrack(DSELoadedSong s, DSETrack track, ref bool allDone)
	{
		track.Tick();
		while (track.Rest == 0 && !track.Stopped)
		{
			s.ExecuteNext(track);
		}
		if (track.Index == s.LongestTrack)
		{
			HandleTicksAndLoop(s, track);
		}
		if (!track.Stopped || track.Channels.Count != 0)
		{
			allDone = false;
		}
	}
	private void HandleTicksAndLoop(DSELoadedSong s, DSETrack track)
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
		UpdateElapsedTicksAfterLoop(s.Events[track.Index], track.CurOffset, track.Rest);
		if (ShouldFadeOut && _elapsedLoops > NumLoops && !DMixer.IsFading())
		{
			DMixer.BeginFadeOut();
		}
	}
}
