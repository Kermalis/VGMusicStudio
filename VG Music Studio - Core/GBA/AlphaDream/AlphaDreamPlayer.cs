using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamPlayer : IPlayer, ILoadedSong
{
	internal const int NUM_TRACKS = 12; // 8 PCM, 4 PSG

	private readonly Track[] _tracks = new Track[NUM_TRACKS];
	private readonly AlphaDreamMixer _mixer;
	private readonly AlphaDreamConfig _config;
	private readonly TimeBarrier _time;
	private Thread? _thread;
	private byte _tempo;
	private int _tempoStack;
	private long _elapsedLoops;

	public List<SongEvent>[] Events { get; private set; }
	public long MaxTicks { get; private set; }
	public long ElapsedTicks { get; private set; }
	public ILoadedSong LoadedSong => this;
	public bool ShouldFadeOut { get; set; }
	public long NumLoops { get; set; }
	private int _longestTrack;

	public PlayerState State { get; private set; }
	public event Action? SongEnded;

	internal AlphaDreamPlayer(AlphaDreamConfig config, AlphaDreamMixer mixer)
	{
		_config = config;
		_mixer = mixer;

		for (byte i = 0; i < NUM_TRACKS; i++)
		{
			_tracks[i] = new Track(i, mixer);
		}

		_time = new TimeBarrier(GBAUtils.AGB_FPS);
	}
	private void CreateThread()
	{
		_thread = new Thread(Tick) { Name = "AlphaDream Player Tick" };
		_thread.Start();
	}
	private void WaitThread()
	{
		if (_thread is not null && (_thread.ThreadState is ThreadState.Running or ThreadState.WaitSleepJoin))
		{
			_thread.Join();
		}
	}

	private void InitEmulation()
	{
		_tempo = 120; // Player tempo is set to 75 on init, but I did not separate player and track tempo yet
		_tempoStack = 0;
		_elapsedLoops = 0;
		ElapsedTicks = 0;
		_mixer.ResetFade();
		for (int i = 0; i < NUM_TRACKS; i++)
		{
			_tracks[i].Init();
		}
	}
	private void SetTicks()
	{
		MaxTicks = 0;
		bool u = false;
		for (int trackIndex = 0; trackIndex < NUM_TRACKS; trackIndex++)
		{
			if (Events[trackIndex] == null)
			{
				continue;
			}

			Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
			List<SongEvent> evs = Events[trackIndex];
			Track track = _tracks[trackIndex];
			track.Init();
			ElapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.DataOffset);
				if (e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(ElapsedTicks);
				ExecuteNext(track, ref u);
				if (track.Stopped)
				{
					break;
				}

				ElapsedTicks += track.Rest;
				track.Rest = 0;
			}
			if (ElapsedTicks > MaxTicks)
			{
				_longestTrack = trackIndex;
				MaxTicks = ElapsedTicks;
			}
			track.NoteDuration = 0;
		}
	}
	public void LoadSong(long index)
	{
		_config.Reader.Stream.Position = _config.SongTableOffsets[0] + (index * 4);
		int songOffset = _config.Reader.ReadInt32();
		if (songOffset == 0)
		{
			Events = null;
			return;
		}

		Events = new List<SongEvent>[NUM_TRACKS];
		songOffset -= GBAUtils.CartridgeOffset;
		_config.Reader.Stream.Position = songOffset;
		ushort trackBits = _config.Reader.ReadUInt16();
		for (byte i = 0, usedTracks = 0; i < NUM_TRACKS; i++)
		{
			Track track = _tracks[i];
			if ((trackBits & (1 << i)) == 0)
			{
				track.Enabled = false;
				track.StartOffset = 0;
				continue;
			}

			track.Enabled = true;
			Events[i] = new List<SongEvent>();
			bool EventExists(long offset)
			{
				return Events[i].Any(e => e.Offset == offset);
			}

			_config.Reader.Stream.Position = songOffset + 2 + (2 * usedTracks++);
			AddEvents(track.StartOffset = songOffset + _config.Reader.ReadInt16());
			void AddEvents(int startOffset)
			{
				_config.Reader.Stream.Position = startOffset;
				bool cont = true;
				while (cont)
				{
					long offset = _config.Reader.Stream.Position;
					void AddEvent(ICommand command)
					{
						Events[i].Add(new SongEvent(offset, command));
					}
					byte cmd = _config.Reader.ReadByte();
					switch (cmd)
					{
						case 0x00:
						{
							byte keyArg = _config.Reader.ReadByte();
							switch (_config.AudioEngineVersion)
							{
								case AudioEngineVersion.Hamtaro:
								{
									byte volume = _config.Reader.ReadByte();
									byte duration = _config.Reader.ReadByte();
									if (!EventExists(offset))
									{
										AddEvent(new FreeNoteHamtaroCommand { Note = (byte)(keyArg - 0x80), Volume = volume, Duration = duration });
									}
									break;
								}
								case AudioEngineVersion.MLSS:
								{
									byte duration = _config.Reader.ReadByte();
									if (!EventExists(offset))
									{
										AddEvent(new FreeNoteMLSSCommand { Note = (byte)(keyArg - 0x80), Duration = duration });
									}
									break;
								}
							}
							break;
						}
						case 0xF0:
						{
							byte voice = _config.Reader.ReadByte();
							if (!EventExists(offset))
							{
								AddEvent(new VoiceCommand { Voice = voice });
							}
							break;
						}
						case 0xF1:
						{
							byte volume = _config.Reader.ReadByte();
							if (!EventExists(offset))
							{
								AddEvent(new VolumeCommand { Volume = volume });
							}
							break;
						}
						case 0xF2:
						{
							byte panArg = _config.Reader.ReadByte();
							if (!EventExists(offset))
							{
								AddEvent(new PanpotCommand { Panpot = (sbyte)(panArg - 0x80) });
							}
							break;
						}
						case 0xF4:
						{
							byte range = _config.Reader.ReadByte();
							if (!EventExists(offset))
							{
								AddEvent(new PitchBendRangeCommand { Range = range });
							}
							break;
						}
						case 0xF5:
						{
							sbyte bend = _config.Reader.ReadSByte();
							if (!EventExists(offset))
							{
								AddEvent(new PitchBendCommand { Bend = bend });
							}
							break;
						}
						case 0xF6:
						{
							byte rest = _config.Reader.ReadByte();
							if (!EventExists(offset))
							{
								AddEvent(new RestCommand { Rest = rest });
							}
							break;
						}
						case 0xF8:
						{
							short jumpOffset = _config.Reader.ReadInt16();
							if (!EventExists(offset))
							{
								int off = (int)(_config.Reader.Stream.Position + jumpOffset);
								AddEvent(new JumpCommand { Offset = off });
								if (!EventExists(off))
								{
									AddEvents(off);
								}
							}
							cont = false;
							break;
						}
						case 0xF9:
						{
							byte tempoArg = _config.Reader.ReadByte();
							if (!EventExists(offset))
							{
								AddEvent(new TrackTempoCommand { Tempo = tempoArg });
							}
							break;
						}
						case 0xFF:
						{
							if (!EventExists(offset))
							{
								AddEvent(new FinishCommand());
							}
							cont = false;
							break;
						}
						default:
						{
							if (cmd >= 0xE0)
							{
								throw new AlphaDreamInvalidCMDException(i, (int)offset, cmd);
							}

							byte key = _config.Reader.ReadByte();
							switch (_config.AudioEngineVersion)
							{
								case AudioEngineVersion.Hamtaro:
								{
									byte volume = _config.Reader.ReadByte();
									if (!EventExists(offset))
									{
										AddEvent(new NoteHamtaroCommand { Note = key, Volume = volume, Duration = cmd });
									}
									break;
								}
								case AudioEngineVersion.MLSS:
								{
									if (!EventExists(offset))
									{
										AddEvent(new NoteMLSSCommand { Note = key, Duration = cmd });
									}
									break;
								}
							}
							break;
						}
					}
				}
			}
		}
		SetTicks();
	}
	public void SetCurrentPosition(long ticks)
	{
		if (Events == null)
		{
			SongEnded?.Invoke();
		}
		else if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
		{
			if (State == PlayerState.Playing)
			{
				Pause();
			}
			InitEmulation();
			bool u = false;
			while (true)
			{
				if (ElapsedTicks == ticks)
				{
					goto finish;
				}

				while (_tempoStack >= 75)
				{
					_tempoStack -= 75;
					for (int trackIndex = 0; trackIndex < NUM_TRACKS; trackIndex++)
					{
						Track track = _tracks[trackIndex];
						if (track.Enabled && !track.Stopped)
						{
							track.Tick();
							while (track.Rest == 0 && !track.Stopped)
							{
								ExecuteNext(track, ref u);
							}
						}
					}
					ElapsedTicks++;
					if (ElapsedTicks == ticks)
					{
						goto finish;
					}
				}
				_tempoStack += _tempo;
			}
		finish:
			for (int i = 0; i < NUM_TRACKS; i++)
			{
				_tracks[i].NoteDuration = 0;
			}
			Pause();
		}
	}
	public void Play()
	{
		if (State is PlayerState.ShutDown or PlayerState.Recording)
		{
			return;
		}

		if (Events is null)
		{
			SongEnded?.Invoke();
			return;
		}

		Stop();
		InitEmulation();
		State = PlayerState.Playing;
		CreateThread();
	}
	public void Pause()
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
		}
	}
	public void Record(string fileName)
	{
		_mixer.CreateWaveWriter(fileName);
		InitEmulation();
		State = PlayerState.Recording;
		CreateThread();
		WaitThread();
		_mixer.CloseWaveWriter();
	}
	public void Dispose()
	{
		if (State != PlayerState.ShutDown)
		{
			State = PlayerState.ShutDown;
			WaitThread();
		}
	}
	public void UpdateSongState(SongState info)
	{
		info.Tempo = _tempo;
		for (int i = 0; i < NUM_TRACKS; i++)
		{
			Track track = _tracks[i];
			if (!track.Enabled)
			{
				continue;
			}

			SongState.Track tin = info.Tracks[i];
			tin.Position = track.DataOffset;
			tin.Rest = track.Rest;
			tin.Voice = track.Voice;
			tin.Type = track.Type;
			tin.Volume = track.Volume;
			tin.PitchBend = track.GetPitch();
			tin.Panpot = track.Panpot;
			if (track.NoteDuration != 0 && !track.Channel.Stopped)
			{
				tin.Keys[0] = track.Channel.Key;
				ChannelVolume vol = track.Channel.GetVolume();
				tin.LeftVolume = vol.LeftVol;
				tin.RightVolume = vol.RightVol;
			}
			else
			{
				tin.Keys[0] = byte.MaxValue;
				tin.LeftVolume = 0f;
				tin.RightVolume = 0f;
			}
		}
	}

	private bool TryGetVoiceEntry(byte voice, byte key, out VoiceEntry e)
	{
		int vto = _config.VoiceTableOffset;
		byte[] rom = _config.ROM;
		short voiceOffset = BinaryPrimitives.ReadInt16LittleEndian(rom.AsSpan(vto + (voice * 2)));
		short nextVoiceOffset = BinaryPrimitives.ReadInt16LittleEndian(rom.AsSpan(vto + ((voice + 1) * 2)));
		if (voiceOffset == nextVoiceOffset)
		{
			e = default;
			return false;
		}

		int pos = vto + voiceOffset; // Prevent object creation in the last iteration
		ref VoiceEntry refE = ref MemoryMarshal.AsRef<VoiceEntry>(rom.AsSpan(pos));
		while (refE.MinKey > key || refE.MaxKey < key)
		{
			pos += 8;
			if (pos == nextVoiceOffset)
			{
				e = default;
				return false;
			}
			refE = ref MemoryMarshal.AsRef<VoiceEntry>(rom.AsSpan(pos));
		}
		e = refE;
		return true;
	}
	private void PlayNote(Track track, byte key, byte duration)
	{
		if (!TryGetVoiceEntry(track.Voice, key, out VoiceEntry entry))
		{
			return;
		}

		track.NoteDuration = duration;
		if (track.Index >= 8)
		{
			// TODO: "Sample" byte in VoiceEntry
			var sqr = (SquareChannel)track.Channel;
			sqr.Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, track.Volume, track.Panpot, track.GetPitch());
		}
		else
		{
			int sto = _config.SampleTableOffset;
			byte[] rom = _config.ROM;
			int sampleOffset = BinaryPrimitives.ReadInt32LittleEndian(rom.AsSpan(sto + (entry.Sample * 4))); // Some entries are 0. If you play them, are they silent, or does it not care if they are 0?

			var pcm = (PCMChannel)track.Channel;
			pcm.Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, sto + sampleOffset, entry.IsFixedFrequency == 0x80);
			pcm.SetVolume(track.Volume, track.Panpot);
			pcm.SetPitch(track.GetPitch());
		}
	}
	private void ExecuteNext(Track track, ref bool update)
	{
		byte[] rom = _config.ROM;
		byte cmd = rom[track.DataOffset++];
		switch (cmd)
		{
			case 0x00: // Free Note
			{
				byte note = (byte)(rom[track.DataOffset++] - 0x80);
				if (_config.AudioEngineVersion == AudioEngineVersion.Hamtaro)
				{
					track.Volume = rom[track.DataOffset++];
					update = true;
				}

				byte duration = rom[track.DataOffset++];
				track.Rest += duration;
				if (track.PrevCommand == 0 && track.Channel.Key == note)
				{
					track.NoteDuration += duration;
				}
				else
				{
					PlayNote(track, note, duration);
				}
				break;
			}
			case <= 0xDF: // Note
			{
				byte key = rom[track.DataOffset++];
				if (_config.AudioEngineVersion == AudioEngineVersion.Hamtaro)
				{
					track.Volume = rom[track.DataOffset++];
					update = true;
				}

				track.Rest += cmd;
				if (track.PrevCommand == 0 && track.Channel.Key == key)
				{
					track.NoteDuration += cmd;
				}
				else
				{
					PlayNote(track, key, cmd);
				}
				break;
			}
			case 0xF0: // Voice
			{
				track.Voice = rom[track.DataOffset++];
				break;
			}
			case 0xF1: // Volume
			{
				track.Volume = rom[track.DataOffset++];
				update = true;
				break;
			}
			case 0xF2: // Panpot
			{
				track.Panpot = (sbyte)(rom[track.DataOffset++] - 0x80);
				update = true;
				break;
			}
			case 0xF4: // Pitch Bend Range
			{
				track.PitchBendRange = rom[track.DataOffset++];
				update = true;
				break;
			}
			case 0xF5: // Pitch Bend
			{
				track.PitchBend = (sbyte)rom[track.DataOffset++];
				update = true;
				break;
			}
			case 0xF6: // Rest
			{
				track.Rest = rom[track.DataOffset++];
				break;
			}
			case 0xF8: // Jump
			{
				track.DataOffset += 2 + BinaryPrimitives.ReadInt16LittleEndian(rom.AsSpan(track.DataOffset, 2));
				break;
			}
			case 0xF9: // Track Tempo
			{
				_tempo = rom[track.DataOffset++];
				break;
			}
			case 0xFF: // Finish
			{
				track.Stopped = true;
				break;
			}
			default: throw new AlphaDreamInvalidCMDException(track.Index, track.DataOffset - 1, cmd);
		}

		track.PrevCommand = cmd;
	}

	private void Tick()
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

			while (_tempoStack >= 75)
			{
				_tempoStack -= 75;
				bool allDone = true;
				for (int trackIndex = 0; trackIndex < NUM_TRACKS; trackIndex++)
				{
					Track track = _tracks[trackIndex];
					if (track.Enabled)
					{
						byte prevDuration = track.NoteDuration;
						track.Tick();
						bool update = false;
						while (track.Rest == 0 && !track.Stopped)
						{
							ExecuteNext(track, ref update);
						}
						if (trackIndex == _longestTrack)
						{
							if (ElapsedTicks == MaxTicks)
							{
								if (!track.Stopped)
								{
									List<SongEvent> evs = Events[trackIndex];
									for (int i = 0; i < evs.Count; i++)
									{
										SongEvent ev = evs[i];
										if (ev.Offset == track.DataOffset)
										{
											ElapsedTicks = ev.Ticks[0] - track.Rest;
											break;
										}
									}
									_elapsedLoops++;
									if (ShouldFadeOut && !_mixer.IsFading() && _elapsedLoops > NumLoops)
									{
										_mixer.BeginFadeOut();
									}
								}
							}
							else
							{
								ElapsedTicks++;
							}
						}
						if (prevDuration == 1 && track.NoteDuration == 0) // Note was not renewed
						{
							track.Channel.State = EnvelopeState.Release;
						}
						if (!track.Stopped)
						{
							allDone = false;
						}
						if (track.NoteDuration != 0)
						{
							allDone = false;
							if (update)
							{
								track.Channel.SetVolume(track.Volume, track.Panpot);
								track.Channel.SetPitch(track.GetPitch());
							}
						}
					}
				}
				if (_mixer.IsFadeDone())
				{
					allDone = true;
				}
				if (allDone)
				{
					// TODO: lock state
					_mixer.Process(_tracks, playing, recording);
					_time.Stop();
					State = PlayerState.Stopped;
					SongEnded?.Invoke();
					return;
				}
			}
			_tempoStack += _tempo;
			_mixer.Process(_tracks, playing, recording);
			if (playing)
			{
				_time.Wait();
			}
		}
		_time.Stop();
	}
}
