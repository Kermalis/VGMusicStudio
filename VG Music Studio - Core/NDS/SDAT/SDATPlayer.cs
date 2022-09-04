using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDATPlayer : IPlayer, ILoadedSong
{
	internal readonly byte Priority = 0x40;
	private readonly short[] _vars = new short[0x20]; // 16 player variables, then 16 global variables
	private readonly Track[] _tracks = new Track[0x10];
	private readonly SDATMixer _mixer;
	private readonly SDATConfig _config;
	private readonly TimeBarrier _time;
	private Thread? _thread;
	private int _randSeed;
	private Random _rand;
	private SDAT.INFO.SequenceInfo _seqInfo;
	private SSEQ _sseq;
	private SBNK _sbnk;
	internal byte Volume;
	private ushort _tempo;
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

	internal SDATPlayer(SDATConfig config, SDATMixer mixer)
	{
		_config = config;
		_mixer = mixer;

		for (byte i = 0; i < 0x10; i++)
		{
			_tracks[i] = new Track(i, this);
		}

		_time = new TimeBarrier(192);
	}
	private void CreateThread()
	{
		_thread = new Thread(Tick) { Name = "SDAT Player Tick" };
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
		_tempo = 120; // Confirmed: default tempo is 120 (MKDS 75)
		_tempoStack = 0;
		_elapsedLoops = 0;
		ElapsedTicks = 0;
		_mixer.ResetFade();
		Volume = _seqInfo.Volume;
		_rand = new Random(_randSeed);
		for (int i = 0; i < 0x10; i++)
		{
			_tracks[i].Init();
		}
		// Initialize player and global variables. Global variables should not have a global effect in this program.
		for (int i = 0; i < 0x20; i++)
		{
			_vars[i] = i % 8 == 0 ? short.MaxValue : (short)0;
		}
	}
	private void SetTicks()
	{
		// TODO: (NSMB 81) (Spirit Tracks 18) does not count all ticks because the songs keep jumping backwards while changing vars and then using ModIfCommand to change events
		// Should evaluate all branches if possible
		MaxTicks = 0;
		for (int i = 0; i < 0x10; i++)
		{
			if (Events[i] != null)
			{
				Events[i] = Events[i].OrderBy(e => e.Offset).ToList();
			}
		}
		InitEmulation();
		bool[] done = new bool[0x10]; // We use this instead of track.Stopped just to be certain that emulating Monophony works as intended
		while (_tracks.Any(t => t.Allocated && t.Enabled && !done[t.Index]))
		{
			while (_tempoStack >= 240)
			{
				_tempoStack -= 240;
				for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
				{
					Track track = _tracks[trackIndex];
					List<SongEvent> evs = Events[trackIndex];
					if (track.Enabled && !track.Stopped)
					{
						track.Tick();
						while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
						{
							SongEvent e = evs.Single(ev => ev.Offset == track.DataOffset);
							ExecuteNext(track);
							if (!done[trackIndex])
							{
								e.Ticks.Add(ElapsedTicks);
								bool b;
								if (track.Stopped)
								{
									b = true;
								}
								else
								{
									SongEvent newE = evs.Single(ev => ev.Offset == track.DataOffset);
									b = (track.CallStackDepth == 0 && newE.Ticks.Count > 0) // If we already counted the tick of this event and we're not looping/calling
									|| (track.CallStackDepth != 0 && track.CallStackLoops.All(l => l == 0) && newE.Ticks.Count > 0); // If we have "LoopStart (0)" and already counted the tick of this event
								}
								if (b)
								{
									done[trackIndex] = true;
									if (ElapsedTicks > MaxTicks)
									{
										_longestTrack = trackIndex;
										MaxTicks = ElapsedTicks;
									}
								}
							}
						}
					}
				}
				ElapsedTicks++;
			}
			_tempoStack += _tempo;
			_mixer.ChannelTick();
			_mixer.EmulateProcess();
		}
		for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
		{
			_tracks[trackIndex].StopAllChannels();
		}
	}
	public void LoadSong(long index)
	{
		Stop();
		SDAT.INFO.SequenceInfo oldSeqInfo = _seqInfo;
		_seqInfo = _config.SDAT.INFOBlock.SequenceInfos.Entries[index];
		if (_seqInfo == null)
		{
			_sseq = null;
			_sbnk = null;
			Events = null;
			return;
		}

		if (oldSeqInfo == null || _seqInfo.Bank != oldSeqInfo.Bank)
		{
			Array.Clear(_voiceTypeCache);
		}
		_sseq = new SSEQ(_config.SDAT.FATBlock.Entries[_seqInfo.FileId].Data);
		SDAT.INFO.BankInfo bankInfo = _config.SDAT.INFOBlock.BankInfos.Entries[_seqInfo.Bank];
		_sbnk = new SBNK(_config.SDAT.FATBlock.Entries[bankInfo.FileId].Data);
		for (int i = 0; i < 4; i++)
		{
			if (bankInfo.SWARs[i] != 0xFFFF)
			{
				_sbnk.SWARs[i] = new SWAR(_config.SDAT.FATBlock.Entries[_config.SDAT.INFOBlock.WaveArchiveInfos.Entries[bankInfo.SWARs[i]].FileId].Data);
			}
		}
		_randSeed = new Random().Next();

		// RECURSION INCOMING
		Events = new List<SongEvent>[0x10];
		AddTrackEvents(0, 0);
		void AddTrackEvents(byte i, int trackStartOffset)
		{
			if (Events[i] == null)
			{
				Events[i] = new List<SongEvent>();
			}
			int callStackDepth = 0;
			AddEvents(trackStartOffset);
			bool EventExists(long offset)
			{
				return Events[i].Any(e => e.Offset == offset);
			}
			void AddEvents(int startOffset)
			{
				int dataOffset = startOffset;
				int ReadArg(ArgType type)
				{
					switch (type)
					{
						case ArgType.Byte:
						{
							return _sseq.Data[dataOffset++];
						}
						case ArgType.Short:
						{
							return _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8);
						}
						case ArgType.VarLen:
						{
							int read = 0, value = 0;
							byte b;
							do
							{
								b = _sseq.Data[dataOffset++];
								value = (value << 7) | (b & 0x7F);
								read++;
							}
							while (read < 4 && (b & 0x80) != 0);
							return value;
						}
						case ArgType.Rand:
						{
							// Combine min and max into one int
							return _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16) | (_sseq.Data[dataOffset++] << 24);
						}
						case ArgType.PlayerVar:
						{
							// Return var index
							return _sseq.Data[dataOffset++];
						}
						default: throw new Exception();
					}
				}
				bool cont = true;
				while (cont)
				{
					bool @if = false;
					int offset = dataOffset;
					ArgType argOverrideType = ArgType.None;
				again:
					byte cmd = _sseq.Data[dataOffset++];
					void AddEvent<T>(T command) where T : SDATCommand, ICommand
					{
						command.RandMod = argOverrideType == ArgType.Rand;
						command.VarMod = argOverrideType == ArgType.PlayerVar;
						Events[i].Add(new SongEvent(offset, command));
					}
					void Invalid()
					{
						throw new SDATInvalidCMDException(i, offset, cmd);
					}

					if (cmd <= 0x7F)
					{
						byte velocity = _sseq.Data[dataOffset++];
						int duration = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
						if (!EventExists(offset))
						{
							AddEvent(new NoteComand { Note = cmd, Velocity = velocity, Duration = duration });
						}
					}
					else
					{
						int cmdGroup = cmd & 0xF0;
						if (cmdGroup == 0x80)
						{
							int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
							switch (cmd)
							{
								case 0x80:
								{
									if (!EventExists(offset))
									{
										AddEvent(new RestCommand { Rest = arg });
									}
									break;
								}
								case 0x81: // RAND PROGRAM: [BW2 (2249)]
								{
									if (!EventExists(offset))
									{
										AddEvent(new VoiceCommand { Voice = arg }); // TODO: Bank change
									}
									break;
								}
								default: Invalid(); break;
							}
						}
						else if (cmdGroup == 0x90)
						{
							switch (cmd)
							{
								case 0x93:
								{
									byte trackIndex = _sseq.Data[dataOffset++];
									int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
									if (!EventExists(offset))
									{
										AddEvent(new OpenTrackCommand { Track = trackIndex, Offset = offset24bit });
										AddTrackEvents(trackIndex, offset24bit);
									}
									break;
								}
								case 0x94:
								{
									int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
									if (!EventExists(offset))
									{
										AddEvent(new JumpCommand { Offset = offset24bit });
										if (!EventExists(offset24bit))
										{
											AddEvents(offset24bit);
										}
									}
									if (!@if)
									{
										cont = false;
									}
									break;
								}
								case 0x95:
								{
									int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
									if (!EventExists(offset))
									{
										AddEvent(new CallCommand { Offset = offset24bit });
									}
									if (callStackDepth < 3)
									{
										if (!EventExists(offset24bit))
										{
											callStackDepth++;
											AddEvents(offset24bit);
										}
									}
									else
									{
										throw new SDATTooManyNestedCallsException(i);
									}
									break;
								}
								default: Invalid(); break;
							}
						}
						else if (cmdGroup == 0xA0)
						{
							switch (cmd)
							{
								case 0xA0: // [New Super Mario Bros (BGM_AMB_CHIKA)] [BW2 (1917, 1918)]
								{
									if (!EventExists(offset))
									{
										AddEvent(new ModRandCommand());
									}
									argOverrideType = ArgType.Rand;
									offset++;
									goto again;
								}
								case 0xA1: // [New Super Mario Bros (BGM_AMB_SABAKU)]
								{
									if (!EventExists(offset))
									{
										AddEvent(new ModVarCommand());
									}
									argOverrideType = ArgType.PlayerVar;
									offset++;
									goto again;
								}
								case 0xA2: // [Mario Kart DS (75)] [BW2 (1917, 1918)]
								{
									if (!EventExists(offset))
									{
										AddEvent(new ModIfCommand());
									}
									@if = true;
									offset++;
									goto again;
								}
								default: Invalid(); break;
							}
						}
						else if (cmdGroup == 0xB0)
						{
							byte varIndex = _sseq.Data[dataOffset++];
							int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
							switch (cmd)
							{
								case 0xB0:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarSetCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB1:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarAddCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB2:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarSubCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB3:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarMulCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB4:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarDivCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB5:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarShiftCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB6: // [Mario Kart DS (75)]
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarRandCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB8:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarCmpEECommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xB9:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarCmpGECommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xBA:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarCmpGGCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xBB:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarCmpLECommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xBC:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarCmpLLCommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								case 0xBD:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarCmpNECommand { Variable = varIndex, Argument = arg });
									}
									break;
								}
								default: Invalid(); break;
							}
						}
						else if (cmdGroup == 0xC0 || cmdGroup == 0xD0)
						{
							int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType);
							switch (cmd)
							{
								case 0xC0:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PanpotCommand { Panpot = arg });
									}
									break;
								}
								case 0xC1:
								{
									if (!EventExists(offset))
									{
										AddEvent(new TrackVolumeCommand { Volume = arg });
									}
									break;
								}
								case 0xC2:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PlayerVolumeCommand { Volume = arg });
									}
									break;
								}
								case 0xC3:
								{
									if (!EventExists(offset))
									{
										AddEvent(new TransposeCommand { Transpose = arg });
									}
									break;
								}
								case 0xC4:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PitchBendCommand { Bend = arg });
									}
									break;
								}
								case 0xC5:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PitchBendRangeCommand { Range = arg });
									}
									break;
								}
								case 0xC6:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PriorityCommand { Priority = arg });
									}
									break;
								}
								case 0xC7:
								{
									if (!EventExists(offset))
									{
										AddEvent(new MonophonyCommand { Mono = arg });
									}
									break;
								}
								case 0xC8:
								{
									if (!EventExists(offset))
									{
										AddEvent(new TieCommand { Tie = arg });
									}
									break;
								}
								case 0xC9:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PortamentoControlCommand { Portamento = arg });
									}
									break;
								}
								case 0xCA:
								{
									if (!EventExists(offset))
									{
										AddEvent(new LFODepthCommand { Depth = arg });
									}
									break;
								}
								case 0xCB:
								{
									if (!EventExists(offset))
									{
										AddEvent(new LFOSpeedCommand { Speed = arg });
									}
									break;
								}
								case 0xCC:
								{
									if (!EventExists(offset))
									{
										AddEvent(new LFOTypeCommand { Type = arg });
									}
									break;
								}
								case 0xCD:
								{
									if (!EventExists(offset))
									{
										AddEvent(new LFORangeCommand { Range = arg });
									}
									break;
								}
								case 0xCE:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PortamentoToggleCommand { Portamento = arg });
									}
									break;
								}
								case 0xCF:
								{
									if (!EventExists(offset))
									{
										AddEvent(new PortamentoTimeCommand { Time = arg });
									}
									break;
								}
								case 0xD0:
								{
									if (!EventExists(offset))
									{
										AddEvent(new ForceAttackCommand { Attack = arg });
									}
									break;
								}
								case 0xD1:
								{
									if (!EventExists(offset))
									{
										AddEvent(new ForceDecayCommand { Decay = arg });
									}
									break;
								}
								case 0xD2:
								{
									if (!EventExists(offset))
									{
										AddEvent(new ForceSustainCommand { Sustain = arg });
									}
									break;
								}
								case 0xD3:
								{
									if (!EventExists(offset))
									{
										AddEvent(new ForceReleaseCommand { Release = arg });
									}
									break;
								}
								case 0xD4:
								{
									if (!EventExists(offset))
									{
										AddEvent(new LoopStartCommand { NumLoops = arg });
									}
									break;
								}
								case 0xD5:
								{
									if (!EventExists(offset))
									{
										AddEvent(new TrackExpressionCommand { Expression = arg });
									}
									break;
								}
								case 0xD6:
								{
									if (!EventExists(offset))
									{
										AddEvent(new VarPrintCommand { Variable = arg });
									}
									break;
								}
								default: Invalid(); break;
							}
						}
						else if (cmdGroup == 0xE0)
						{
							int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
							switch (cmd)
							{
								case 0xE0:
								{
									if (!EventExists(offset))
									{
										AddEvent(new LFODelayCommand { Delay = arg });
									}
									break;
								}
								case 0xE1:
								{
									if (!EventExists(offset))
									{
										AddEvent(new TempoCommand { Tempo = arg });
									}
									break;
								}
								case 0xE3:
								{
									if (!EventExists(offset))
									{
										AddEvent(new SweepPitchCommand { Pitch = arg });
									}
									break;
								}
								default: Invalid(); break;
							}
						}
						else // if (cmdGroup == 0xF0)
						{
							switch (cmd)
							{
								case 0xFC: // [HGSS(1353)]
								{
									if (!EventExists(offset))
									{
										AddEvent(new LoopEndCommand());
									}
									break;
								}
								case 0xFD:
								{
									if (!EventExists(offset))
									{
										AddEvent(new ReturnCommand());
									}
									if (!@if && callStackDepth != 0)
									{
										cont = false;
										callStackDepth--;
									}
									break;
								}
								case 0xFE:
								{
									ushort bits = (ushort)ReadArg(ArgType.Short);
									if (!EventExists(offset))
									{
										AddEvent(new AllocTracksCommand { Tracks = bits });
									}
									break;
								}
								case 0xFF:
								{
									if (!EventExists(offset))
									{
										AddEvent(new FinishCommand());
									}
									if (!@if)
									{
										cont = false;
									}
									break;
								}
								default: Invalid(); break;
							}
						}
					}
				}
			}
		}
		SetTicks();
	}

	public void SetCurrentPosition(long ticks)
	{
		if (_seqInfo is null)
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
			Pause();
		}
		InitEmulation();
		while (true)
		{
			if (ElapsedTicks == ticks)
			{
				goto finish;
			}

			while (_tempoStack >= 240)
			{
				_tempoStack -= 240;
				for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
				{
					Track track = _tracks[trackIndex];
					if (track.Enabled && !track.Stopped)
					{
						track.Tick();
						while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
						{
							ExecuteNext(track);
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
			_mixer.ChannelTick();
			_mixer.EmulateProcess();
		}
	finish:
		for (int i = 0; i < 0x10; i++)
		{
			_tracks[i].StopAllChannels();
		}
		Pause();
	}
	public void Play()
	{
		if (_seqInfo == null)
		{
			SongEnded?.Invoke();
			return;
		}
		if (State is PlayerState.Playing or PlayerState.Paused or PlayerState.Stopped)
		{
			Stop();
			InitEmulation();
			State = PlayerState.Playing;
			CreateThread();
		}
	}
	public void Pause()
	{
		if (State == PlayerState.Playing)
		{
			State = PlayerState.Paused;
			WaitThread();
		}
		else if (State == PlayerState.Paused || State == PlayerState.Stopped)
		{
			State = PlayerState.Playing;
			CreateThread();
		}
	}
	public void Stop()
	{
		if (State == PlayerState.Playing || State == PlayerState.Paused)
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
		if (State is PlayerState.Playing or PlayerState.Paused or PlayerState.Stopped)
		{
			State = PlayerState.ShutDown;
			WaitThread();
		}
	}
	private readonly string?[] _voiceTypeCache = new string?[256];
	public void UpdateSongState(SongState info)
	{
		info.Tempo = _tempo;
		for (int i = 0; i < 0x10; i++)
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
			tin.LFO = track.LFODepth * track.LFORange;
			ref string? cache = ref _voiceTypeCache[track.Voice];
			if (cache is null)
			{
				if (_sbnk.NumInstruments <= track.Voice)
				{
					cache = "Empty";
				}
				else
				{
					InstrumentType t = _sbnk.Instruments[track.Voice].Type;
					switch (t)
					{
						case InstrumentType.PCM: cache = "PCM"; break;
						case InstrumentType.PSG: cache = "PSG"; break;
						case InstrumentType.Noise: cache = "Noise"; break;
						case InstrumentType.Drum: cache = "Drum"; break;
						case InstrumentType.KeySplit: cache = "Key Split"; break;
						default: cache = "Invalid {0}" + (byte)t; break;
					}
				}
			}
			tin.Type = cache;
			tin.Volume = track.Volume;
			tin.PitchBend = track.GetPitch();
			tin.Extra = track.Portamento ? track.PortamentoTime : (byte)0;
			tin.Panpot = track.GetPan();

			Channel[] channels = track.Channels.ToArray();
			if (channels.Length == 0)
			{
				tin.Keys[0] = byte.MaxValue;
				tin.LeftVolume = 0f;
				tin.RightVolume = 0f;
			}
			else
			{
				int numKeys = 0;
				float left = 0f;
				float right = 0f;
				for (int j = 0; j < channels.Length; j++)
				{
					Channel c = channels[j];
					if (c.State != EnvelopeState.Release)
					{
						tin.Keys[numKeys++] = c.Note;
					}
					float a = (float)(-c.Pan + 0x40) / 0x80 * c.Volume / 0x7F;
					if (a > left)
					{
						left = a;
					}
					a = (float)(c.Pan + 0x40) / 0x80 * c.Volume / 0x7F;
					if (a > right)
					{
						right = a;
					}
				}
				tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
				tin.LeftVolume = left;
				tin.RightVolume = right;
			}
		}
	}

	private void TryStartChannel(SBNK.InstrumentData inst, Track track, byte note, byte velocity, int duration, out Channel? channel)
	{
		InstrumentType type = inst.Type;
		channel = _mixer.AllocateChannel(type, track);
		if (channel is null)
		{
			return;
		}

		if (track.Tie)
		{
			duration = -1;
		}
		SBNK.InstrumentData.DataParam param = inst.Param;
		byte release = param.Release;
		if (release == 0xFF)
		{
			duration = -1;
			release = 0;
		}
		bool started = false;
		switch (type)
		{
			case InstrumentType.PCM:
			{
				ushort[] info = param.Info;
				SWAR.SWAV? swav = _sbnk.GetSWAV(info[1], info[0]);
				if (swav is not null)
				{
					channel.StartPCM(swav, duration);
					started = true;
				}
				break;
			}
			case InstrumentType.PSG:
			{
				channel.StartPSG((byte)param.Info[0], duration);
				started = true;
				break;
			}
			case InstrumentType.Noise:
			{
				channel.StartNoise(duration);
				started = true;
				break;
			}
		}
		channel.Stop();
		if (!started)
		{
			return;
		}

		channel.Note = note;
		byte baseNote = param.BaseNote;
		channel.BaseNote = type != InstrumentType.PCM && baseNote == 0x7F ? (byte)60 : baseNote;
		channel.NoteVelocity = velocity;
		channel.SetAttack(param.Attack);
		channel.SetDecay(param.Decay);
		channel.SetSustain(param.Sustain);
		channel.SetRelease(release);
		channel.StartingPan = (sbyte)(param.Pan - 0x40);
		channel.Owner = track;
		channel.Priority = track.Priority;
		track.Channels.Add(channel);
	}
	internal void PlayNote(Track track, byte note, byte velocity, int duration)
	{
		Channel? channel = null;
		if (track.Tie && track.Channels.Count != 0)
		{
			channel = track.Channels.Last();
			channel.Note = note;
			channel.NoteVelocity = velocity;
		}
		else
		{
			SBNK.InstrumentData? inst = _sbnk.GetInstrumentData(track.Voice, note);
			if (inst is not null)
			{
				TryStartChannel(inst, track, note, velocity, duration, out channel);
			}

			if (channel is null)
			{
				return;
			}
		}

		if (track.Attack != 0xFF)
		{
			channel.SetAttack(track.Attack);
		}
		if (track.Decay != 0xFF)
		{
			channel.SetDecay(track.Decay);
		}
		if (track.Sustain != 0xFF)
		{
			channel.SetSustain(track.Sustain);
		}
		if (track.Release != 0xFF)
		{
			channel.SetRelease(track.Release);
		}
		channel.SweepPitch = track.SweepPitch;
		if (track.Portamento)
		{
			channel.SweepPitch += (short)((track.PortamentoKey - note) << 6); // "<< 6" is "* 0x40"
		}
		if (track.PortamentoTime != 0)
		{
			channel.SweepLength = (track.PortamentoTime * track.PortamentoTime * Math.Abs(channel.SweepPitch)) >> 11; // ">> 11" is "/ 0x800"
			channel.AutoSweep = true;
		}
		else
		{
			channel.SweepLength = duration;
			channel.AutoSweep = false;
		}
		channel.SweepCounter = 0;
	}
	private void ExecuteNext(Track track)
	{
		int ReadArg(ArgType type)
		{
			if (track.ArgOverrideType != ArgType.None)
			{
				type = track.ArgOverrideType;
			}
			switch (type)
			{
				case ArgType.Byte:
				{
					return _sseq.Data[track.DataOffset++];
				}
				case ArgType.Short:
				{
					return _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8);
				}
				case ArgType.VarLen:
				{
					int read = 0, value = 0;
					byte b;
					do
					{
						b = _sseq.Data[track.DataOffset++];
						value = (value << 7) | (b & 0x7F);
						read++;
					}
					while (read < 4 && (b & 0x80) != 0);
					return value;
				}
				case ArgType.Rand:
				{
					short min = (short)(_sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8));
					short max = (short)(_sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8));
					return _rand.Next(min, max + 1);
				}
				case ArgType.PlayerVar:
				{
					byte varIndex = _sseq.Data[track.DataOffset++];
					return _vars[varIndex];
				}
				default: throw new Exception();
			}
		}

		bool resetOverride = true;
		bool resetCmdWork = true;
		byte cmd = _sseq.Data[track.DataOffset++];
		if (cmd < 0x80) // Notes
		{
			byte velocity = _sseq.Data[track.DataOffset++];
			int duration = ReadArg(ArgType.VarLen);
			if (track.DoCommandWork)
			{
				int k = cmd + track.Transpose;
				if (k < 0)
				{
					k = 0;
				}
				else if (k > 0x7F)
				{
					k = 0x7F;
				}
				byte key = (byte)k;
				PlayNote(track, key, velocity, duration);
				track.PortamentoKey = key;
				if (track.Mono)
				{
					track.Rest = duration;
					if (duration == 0)
					{
						track.WaitingForNoteToFinishBeforeContinuingXD = true;
					}
				}
			}
		}
		else
		{
			int cmdGroup = cmd & 0xF0;
			switch (cmdGroup)
			{
				case 0x80:
				{
					int arg = ReadArg(ArgType.VarLen);
					if (track.DoCommandWork)
					{
						switch (cmd)
						{
							case 0x80: // Rest
							{
								track.Rest = arg;
								break;
							}
							case 0x81: // Program Change
							{
								if (arg <= byte.MaxValue)
								{
									track.Voice = (byte)arg;
								}
								break;
							}
						}
					}
					break;
				}
				case 0x90:
				{
					switch (cmd)
					{
						case 0x93: // Open Track
						{
							int index = _sseq.Data[track.DataOffset++];
							int offset24bit = _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8) | (_sseq.Data[track.DataOffset++] << 16);
							if (track.DoCommandWork && track.Index == 0)
							{
								Track other = _tracks[index];
								if (other.Allocated && !other.Enabled)
								{
									other.Enabled = true;
									other.DataOffset = offset24bit;
								}
							}
							break;
						}
						case 0x94: // Jump
						{
							int offset24bit = _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8) | (_sseq.Data[track.DataOffset++] << 16);
							if (track.DoCommandWork)
							{
								track.DataOffset = offset24bit;
							}
							break;
						}
						case 0x95: // Call
						{
							int offset24bit = _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8) | (_sseq.Data[track.DataOffset++] << 16);
							if (track.DoCommandWork && track.CallStackDepth < 3)
							{
								track.CallStack[track.CallStackDepth] = track.DataOffset;
								track.CallStackLoops[track.CallStackDepth] = byte.MaxValue; // This is only necessary for SetTicks() to deal with LoopStart (0)
								track.CallStackDepth++;
								track.DataOffset = offset24bit;
							}
							break;
						}
					}
					break;
				}
				case 0xA0:
				{
					if (track.DoCommandWork)
					{
						switch (cmd)
						{
							case 0xA0: // Rand Mod
							{
								track.ArgOverrideType = ArgType.Rand;
								resetOverride = false;
								break;
							}
							case 0xA1: // Var Mod
							{
								track.ArgOverrideType = ArgType.PlayerVar;
								resetOverride = false;
								break;
							}
							case 0xA2: // If Mod
							{
								track.DoCommandWork = track.VariableFlag;
								resetCmdWork = false;
								break;
							}
						}
					}
					break;
				}
				case 0xB0:
				{
					byte varIndex = _sseq.Data[track.DataOffset++];
					short mathArg = (short)ReadArg(ArgType.Short);
					if (track.DoCommandWork)
					{
						switch (cmd)
						{
							case 0xB0: // VarSet
							{
								_vars[varIndex] = mathArg;
								break;
							}
							case 0xB1: // VarAdd
							{
								_vars[varIndex] += mathArg;
								break;
							}
							case 0xB2: // VarSub
							{
								_vars[varIndex] -= mathArg;
								break;
							}
							case 0xB3: // VarMul
							{
								_vars[varIndex] *= mathArg;
								break;
							}
							case 0xB4: // VarDiv
							{
								if (mathArg != 0)
								{
									_vars[varIndex] /= mathArg;
								}
								break;
							}
							case 0xB5: // VarShift
							{
								_vars[varIndex] = mathArg < 0 ? (short)(_vars[varIndex] >> -mathArg) : (short)(_vars[varIndex] << mathArg);
								break;
							}
							case 0xB6: // VarRand
							{
								bool negate = false;
								if (mathArg < 0)
								{
									negate = true;
									mathArg = (short)-mathArg;
								}
								short val = (short)_rand.Next(mathArg + 1);
								if (negate)
								{
									val = (short)-val;
								}
								_vars[varIndex] = val;
								break;
							}
							case 0xB8: // VarCmpEE
							{
								track.VariableFlag = _vars[varIndex] == mathArg;
								break;
							}
							case 0xB9: // VarCmpGE
							{
								track.VariableFlag = _vars[varIndex] >= mathArg;
								break;
							}
							case 0xBA: // VarCmpGG
							{
								track.VariableFlag = _vars[varIndex] > mathArg;
								break;
							}
							case 0xBB: // VarCmpLE
							{
								track.VariableFlag = _vars[varIndex] <= mathArg;
								break;
							}
							case 0xBC: // VarCmpLL
							{
								track.VariableFlag = _vars[varIndex] < mathArg;
								break;
							}
							case 0xBD: // VarCmpNE
							{
								track.VariableFlag = _vars[varIndex] != mathArg;
								break;
							}
						}
					}
					break;
				}
				case 0xC0:
				case 0xD0:
				{
					int cmdArg = ReadArg(ArgType.Byte);
					if (track.DoCommandWork)
					{
						switch (cmd)
						{
							case 0xC0: // Panpot
							{
								track.Panpot = (sbyte)(cmdArg - 0x40);
								break;
							}
							case 0xC1: // Track Volume
							{
								track.Volume = (byte)cmdArg;
								break;
							}
							case 0xC2: // Player Volume
							{
								Volume = (byte)cmdArg;
								break;
							}
							case 0xC3: // Transpose
							{
								track.Transpose = (sbyte)cmdArg;
								break;
							}
							case 0xC4: // Pitch Bend
							{
								track.PitchBend = (sbyte)cmdArg;
								break;
							}
							case 0xC5: // Pitch Bend Range
							{
								track.PitchBendRange = (byte)cmdArg;
								break;
							}
							case 0xC6: // Priority
							{
								track.Priority = (byte)(Priority + (byte)cmdArg);
								break;
							}
							case 0xC7: // Mono
							{
								track.Mono = cmdArg == 1;
								break;
							}
							case 0xC8: // Tie
							{
								track.Tie = cmdArg == 1;
								track.StopAllChannels();
								break;
							}
							case 0xC9: // Portamento Control
							{
								int k = cmdArg + track.Transpose;
								if (k < 0)
								{
									k = 0;
								}
								else if (k > 0x7F)
								{
									k = 0x7F;
								}
								track.PortamentoKey = (byte)k;
								track.Portamento = true;
								break;
							}
							case 0xCA: // LFO Depth
							{
								track.LFODepth = (byte)cmdArg;
								break;
							}
							case 0xCB: // LFO Speed
							{
								track.LFOSpeed = (byte)cmdArg;
								break;
							}
							case 0xCC: // LFO Type
							{
								track.LFOType = (LFOType)cmdArg;
								break;
							}
							case 0xCD: // LFO Range
							{
								track.LFORange = (byte)cmdArg;
								break;
							}
							case 0xCE: // Portamento Toggle
							{
								track.Portamento = cmdArg == 1;
								break;
							}
							case 0xCF: // Portamento Time
							{
								track.PortamentoTime = (byte)cmdArg;
								break;
							}
							case 0xD0: // Forced Attack
							{
								track.Attack = (byte)cmdArg;
								break;
							}
							case 0xD1: // Forced Decay
							{
								track.Decay = (byte)cmdArg;
								break;
							}
							case 0xD2: // Forced Sustain
							{
								track.Sustain = (byte)cmdArg;
								break;
							}
							case 0xD3: // Forced Release
							{
								track.Release = (byte)cmdArg;
								break;
							}
							case 0xD4: // Loop Start
							{
								if (track.CallStackDepth < 3)
								{
									track.CallStack[track.CallStackDepth] = track.DataOffset;
									track.CallStackLoops[track.CallStackDepth] = (byte)cmdArg;
									track.CallStackDepth++;
								}
								break;
							}
							case 0xD5: // Track Expression
							{
								track.Expression = (byte)cmdArg;
								break;
							}
						}
					}
					break;
				}
				case 0xE0:
				{
					int cmdArg = ReadArg(ArgType.Short);
					if (track.DoCommandWork)
					{
						switch (cmd)
						{
							case 0xE0: // LFO Delay
							{
								track.LFODelay = (ushort)cmdArg;
								break;
							}
							case 0xE1: // Tempo
							{
								_tempo = (ushort)cmdArg;
								break;
							}
							case 0xE3: // Sweep Pitch
							{
								track.SweepPitch = (short)cmdArg;
								break;
							}
						}
					}
					break;
				}
				case 0xF0:
				{
					if (track.DoCommandWork)
					{
						switch (cmd)
						{
							case 0xFC: // Loop End
							{
								if (track.CallStackDepth != 0)
								{
									byte count = track.CallStackLoops[track.CallStackDepth - 1];
									if (count != 0)
									{
										count--;
										track.CallStackLoops[track.CallStackDepth - 1] = count;
										if (count == 0)
										{
											track.CallStackDepth--;
											break;
										}
									}
									track.DataOffset = track.CallStack[track.CallStackDepth - 1];
								}
								break;
							}
							case 0xFD: // Return
							{
								if (track.CallStackDepth != 0)
								{
									track.CallStackDepth--;
									track.DataOffset = track.CallStack[track.CallStackDepth];
									track.CallStackLoops[track.CallStackDepth] = 0; // This is only necessary for SetTicks() to deal with LoopStart (0)
								}
								break;
							}
							case 0xFE: // Alloc Tracks
							{
								// Must be in the beginning of the first track to work
								if (track.Index == 0 && track.DataOffset == 1) // == 1 because we read cmd already
								{
									// Track 1 enabled = bit 1 set, Track 4 enabled = bit 4 set, etc
									int trackBits = _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8);
									for (int i = 0; i < 0x10; i++)
									{
										if ((trackBits & (1 << i)) != 0)
										{
											_tracks[i].Allocated = true;
										}
									}
								}
								break;
							}
							case 0xFF: // Finish
							{
								track.Stopped = true;
								break;
							}
						}
					}
					break;
				}
			}
		}
		if (resetOverride)
		{
			track.ArgOverrideType = ArgType.None;
		}
		if (resetCmdWork)
		{
			track.DoCommandWork = true;
		}
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

			void MixerProcess()
			{
				for (int i = 0; i < 0x10; i++)
				{
					Track track = _tracks[i];
					if (track.Enabled)
					{
						track.UpdateChannels();
					}
				}
				_mixer.ChannelTick();
				_mixer.Process(playing, recording);
			}

			while (_tempoStack >= 240)
			{
				_tempoStack -= 240;
				bool allDone = true;
				for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
				{
					Track track = _tracks[trackIndex];
					if (!track.Enabled)
					{
						continue;
					}
					track.Tick();
					while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
					{
						ExecuteNext(track);
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
										//ElapsedTicks = ev.Ticks[0] - track.Rest;
										ElapsedTicks = ev.Ticks.Count == 0 ? 0 : ev.Ticks[0] - track.Rest; // Prevent crashes with songs that don't load all ticks yet (See SetTicks())
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
					if (!track.Stopped || track.Channels.Count != 0)
					{
						allDone = false;
					}
				}
				if (_mixer.IsFadeDone())
				{
					allDone = true;
				}
				if (allDone)
				{
					// TODO: lock state
					MixerProcess();
					_time.Stop();
					State = PlayerState.Stopped;
					SongEnded?.Invoke();
					return;
				}
			}
			_tempoStack += _tempo;
			MixerProcess();
			if (playing)
			{
				_time.Wait();
			}
		}
		_time.Stop();
	}
}
