using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDATPlayer : Player
{
	protected override string Name => "SDAT Player";

	internal readonly byte Priority = 0x40;
	internal readonly short[] Vars = new short[0x20]; // 16 player variables, then 16 global variables
	internal readonly SDATTrack[] Tracks = new SDATTrack[0x10];
	private readonly string?[] _voiceTypeCache = new string?[256];
	internal readonly SDATConfig Config;
	internal readonly SDATMixer SMixer;
	private SDATLoadedSong? _loadedSong;

	internal byte Volume;
	internal ushort Tempo;
	internal int TempoStack;
	private long _elapsedLoops;

	private ushort? _prevBank;

	public override ILoadedSong? LoadedSong => _loadedSong;
	protected override Mixer Mixer => SMixer;

	internal SDATPlayer(SDATConfig config, SDATMixer mixer)
		: base(192)
	{
		Config = config;
		SMixer = mixer;

		for (byte i = 0; i < 0x10; i++)
		{
			Tracks[i] = new SDATTrack(i, this);
		}
	}

	public override void LoadSong(int index)
	{
		if (_loadedSong is not null)
		{
			_loadedSong = null;
		}

		SDAT.INFO.SequenceInfo? seqInfo = Config.SDAT.INFOBlock.SequenceInfos.Entries[index];
		if (seqInfo is null)
		{
			return;
		}

		// If there's an exception, this will remain null
		_loadedSong = new SDATLoadedSong(this, seqInfo);
		_loadedSong.SetTicks();

		ushort? old = _prevBank;
		ushort nu = _loadedSong.SEQInfo.Bank;
		if (old != nu)
		{
			_prevBank = nu;
			Array.Clear(_voiceTypeCache);
		}
	}
	public override void UpdateSongState(SongState info)
	{
		info.Tempo = Tempo;
		for (int i = 0; i < 0x10; i++)
		{
			SDATTrack track = Tracks[i];
			if (track.Enabled)
			{
				track.UpdateSongState(info.Tracks[i], _loadedSong!, _voiceTypeCache);
			}
		}
	}
	internal override void InitEmulation()
	{
		Tempo = 120; // Confirmed: default tempo is 120 (MKDS 75)
		TempoStack = 0;
		_elapsedLoops = 0;
		ElapsedTicks = 0;
		SMixer.ResetFade();
		_loadedSong!.InitEmulation();
		for (int i = 0; i < 0x10; i++)
		{
			Tracks[i].Init();
		}
		// Initialize player and global variables. Global variables should not have a global effect in this program.
		for (int i = 0; i < 0x20; i++)
		{
			Vars[i] = i % 8 == 0 ? short.MaxValue : (short)0;
		}
	}
	protected override void SetCurTick(long ticks)
	{
		_loadedSong!.SetCurTick(ticks);
	}
	protected override void OnStopped()
	{
		for (int i = 0; i < 0x10; i++)
		{
			Tracks[i].StopAllChannels();
		}
	}

	protected override bool Tick(bool playing, bool recording)
	{
		bool allDone = false;
		while (!allDone && TempoStack >= 240)
		{
			TempoStack -= 240;
			allDone = true;
			for (int i = 0; i < 0x10; i++)
			{
				TickTrack(i, ref allDone);
			}
			if (SMixer.IsFadeDone())
			{
				allDone = true;
			}
		}
		if (!allDone)
		{
			TempoStack += Tempo;
		}
		for (int i = 0; i < 0x10; i++)
		{
			SDATTrack track = Tracks[i];
			if (track.Enabled)
			{
				track.UpdateChannels();
			}
		}
		SMixer.ChannelTick();
		SMixer.Process(playing, recording);
		return allDone;
	}
	private void TickTrack(int trackIndex, ref bool allDone)
	{
		SDATTrack track = Tracks[trackIndex];
		if (!track.Enabled)
		{
			return;
		}

		track.Tick();
		SDATLoadedSong s = _loadedSong!;
		while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
		{
			s.ExecuteNext(track);
		}
		if (trackIndex == s.LongestTrack)
		{
			HandleTicksAndLoop(s, track);
		}
		if (!track.Stopped || track.Channels.Count != 0)
		{
			allDone = false;
		}
	}
	private void HandleTicksAndLoop(SDATLoadedSong s, SDATTrack track)
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
		//UpdateElapsedTicksAfterLoop(s.Events[track.Index], track.DataOffset, track.Rest); // TODO
		// Prevent crashes with songs that don't load all ticks yet (See SetTicks())
		List<SongEvent> evs = s.Events[track.Index]!;
		for (int i = 0; i < evs.Count; i++)
		{
			SongEvent ev = evs[i];
			if (ev.Offset == track.DataOffset)
			{
				//ElapsedTicks = ev.Ticks[0] - track.Rest;
				ElapsedTicks = ev.Ticks.Count == 0 ? 0 : ev.Ticks[0] - track.Rest;
				break;
			}
		}
		if (ShouldFadeOut && _elapsedLoops > NumLoops && !SMixer.IsFading())
		{
			SMixer.BeginFadeOut();
		}
	}
}
