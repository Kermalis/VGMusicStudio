using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEPlayer : IPlayer, ILoadedSong
{
	private readonly DSEMixer _mixer;
	private readonly DSEConfig _config;
	private readonly TimeBarrier _time;
	private Thread? _thread;
	private readonly SWD _masterSWD;
	private SWD _localSWD;
	private byte[] _smdFile;
	private Track[] _tracks;
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

	public DSEPlayer(DSEConfig config, DSEMixer mixer)
	{
		_mixer = mixer;
		_config = config;
		_masterSWD = new SWD(Path.Combine(config.BGMPath, "bgm.swd"));

		_time = new TimeBarrier(192);
	}
	private void CreateThread()
	{
		_thread = new Thread(Tick) { Name = "DSE Player Tick" };
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
		_tempo = 120;
		_tempoStack = 0;
		_elapsedLoops = 0;
		ElapsedTicks = 0;
		_mixer.ResetFade();
		for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
		{
			_tracks[trackIndex].Init();
		}
	}
	private void SetTicks()
	{
		MaxTicks = 0;
		for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
		{
			Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
			List<SongEvent> evs = Events[trackIndex];
			Track track = _tracks[trackIndex];
			track.Init();
			ElapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.CurOffset);
				if (e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(ElapsedTicks);
				ExecuteNext(track);
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
			track.StopAllChannels();
		}
	}
	public void LoadSong(long index)
	{
		if (_tracks != null)
		{
			for (int i = 0; i < _tracks.Length; i++)
			{
				_tracks[i].StopAllChannels();
			}
			_tracks = null;
		}

		string bgm = _config.BGMFiles[index];
		_localSWD = new SWD(Path.ChangeExtension(bgm, "swd"));
		_smdFile = File.ReadAllBytes(bgm);
		using (var stream = new MemoryStream(_smdFile))
		{
			var r = new EndianBinaryReader(stream, ascii: true);
			SMD.Header header = r.ReadObject<SMD.Header>();
			SMD.ISongChunk songChunk;
			switch (header.Version)
			{
				case 0x402:
				{
					songChunk = r.ReadObject<SMD.SongChunk_V402>();
					break;
				}
				case 0x415:
				{
					songChunk = r.ReadObject<SMD.SongChunk_V415>();
					break;
				}
				default: throw new DSEInvalidHeaderVersionException(header.Version);
			}

			_tracks = new Track[songChunk.NumTracks];
			Events = new List<SongEvent>[songChunk.NumTracks];
			for (byte trackIndex = 0; trackIndex < songChunk.NumTracks; trackIndex++)
			{
				Events[trackIndex] = new List<SongEvent>();
				bool EventExists(long offset)
				{
					return Events[trackIndex].Any(e => e.Offset == offset);
				}

				long chunkStart = r.Stream.Position;
				r.Stream.Position += 0x14; // Skip header
				_tracks[trackIndex] = new Track(trackIndex, (int)r.Stream.Position);

				uint lastNoteDuration = 0, lastRest = 0;
				bool cont = true;
				while (cont)
				{
					long offset = r.Stream.Position;
					void AddEvent(ICommand command)
					{
						Events[trackIndex].Add(new SongEvent(offset, command));
					}
					byte cmd = r.ReadByte();
					if (cmd <= 0x7F)
					{
						byte arg = r.ReadByte();
						int numParams = (arg & 0xC0) >> 6;
						int oct = ((arg & 0x30) >> 4) - 2;
						int n = arg & 0xF;
						if (n >= 12)
						{
							throw new DSEInvalidNoteException(trackIndex, (int)offset, n);
						}

						uint duration;
						if (numParams == 0)
						{
							duration = lastNoteDuration;
						}
						else // Big Endian reading of 8, 16, or 24 bits
						{
							duration = 0;
							for (int b = 0; b < numParams; b++)
							{
								duration = (duration << 8) | r.ReadByte();
							}
							lastNoteDuration = duration;
						}
						if (!EventExists(offset))
						{
							AddEvent(new NoteCommand { Note = (byte)n, OctaveChange = (sbyte)oct, Velocity = cmd, Duration = duration });
						}
					}
					else if (cmd >= 0x80 && cmd <= 0x8F)
					{
						lastRest = Utils.FixedRests[cmd - 0x80];
						if (!EventExists(offset))
						{
							AddEvent(new RestCommand { Rest = lastRest });
						}
					}
					else // 0x90-0xFF
					{
						// TODO: 0x95 - a rest that may or may not repeat depending on some condition within channels
						// TODO: 0x9E - may or may not jump somewhere else depending on an unknown structure
						switch (cmd)
						{
							case 0x90:
							{
								if (!EventExists(offset))
								{
									AddEvent(new RestCommand { Rest = lastRest });
								}
								break;
							}
							case 0x91:
							{
								lastRest = (uint)(lastRest + r.ReadSByte());
								if (!EventExists(offset))
								{
									AddEvent(new RestCommand { Rest = lastRest });
								}
								break;
							}
							case 0x92:
							{
								lastRest = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new RestCommand { Rest = lastRest });
								}
								break;
							}
							case 0x93:
							{
								lastRest = r.ReadUInt16();
								if (!EventExists(offset))
								{
									AddEvent(new RestCommand { Rest = lastRest });
								}
								break;
							}
							case 0x94:
							{
								lastRest = (uint)(r.ReadByte() | (r.ReadByte() << 8) | (r.ReadByte() << 16));
								if (!EventExists(offset))
								{
									AddEvent(new RestCommand { Rest = lastRest });
								}
								break;
							}
							case 0x96:
							case 0x97:
							case 0x9A:
							case 0x9B:
							case 0x9F:
							case 0xA2:
							case 0xA3:
							case 0xA6:
							case 0xA7:
							case 0xAD:
							case 0xAE:
							case 0xB7:
							case 0xB8:
							case 0xB9:
							case 0xBA:
							case 0xBB:
							case 0xBD:
							case 0xC1:
							case 0xC2:
							case 0xC4:
							case 0xC5:
							case 0xC6:
							case 0xC7:
							case 0xC8:
							case 0xC9:
							case 0xCA:
							case 0xCC:
							case 0xCD:
							case 0xCE:
							case 0xCF:
							case 0xD9:
							case 0xDA:
							case 0xDE:
							case 0xE6:
							case 0xEB:
							case 0xEE:
							case 0xF4:
							case 0xF5:
							case 0xF7:
							case 0xF9:
							case 0xFA:
							case 0xFB:
							case 0xFC:
							case 0xFD:
							case 0xFE:
							case 0xFF:
							{
								if (!EventExists(offset))
								{
									AddEvent(new InvalidCommand { Command = cmd });
								}
								break;
							}
							case 0x98:
							{
								if (!EventExists(offset))
								{
									AddEvent(new FinishCommand());
								}
								cont = false;
								break;
							}
							case 0x99:
							{
								if (!EventExists(offset))
								{
									AddEvent(new LoopStartCommand { Offset = r.Stream.Position });
								}
								break;
							}
							case 0xA0:
							{
								byte octave = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new OctaveSetCommand { Octave = octave });
								}
								break;
							}
							case 0xA1:
							{
								sbyte change = r.ReadSByte();
								if (!EventExists(offset))
								{
									AddEvent(new OctaveAddCommand { OctaveChange = change });
								}
								break;
							}
							case 0xA4:
							case 0xA5: // The code for these two is identical
							{
								byte tempoArg = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new TempoCommand { Command = cmd, Tempo = tempoArg });
								}
								break;
							}
							case 0xAB:
							{
								byte[] bytes = new byte[1];
								r.ReadBytes(bytes);
								if (!EventExists(offset))
								{
									AddEvent(new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
								}
								break;
							}
							case 0xAC:
							{
								byte voice = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new VoiceCommand { Voice = voice });
								}
								break;
							}
							case 0xCB:
							case 0xF8:
							{
								byte[] bytes = new byte[2];
								r.ReadBytes(bytes);
								if (!EventExists(offset))
								{
									AddEvent(new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
								}
								break;
							}
							case 0xD7:
							{
								ushort bend = r.ReadUInt16();
								if (!EventExists(offset))
								{
									AddEvent(new PitchBendCommand { Bend = bend });
								}
								break;
							}
							case 0xE0:
							{
								byte volume = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new VolumeCommand { Volume = volume });
								}
								break;
							}
							case 0xE3:
							{
								byte expression = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new ExpressionCommand { Expression = expression });
								}
								break;
							}
							case 0xE8:
							{
								byte panArg = r.ReadByte();
								if (!EventExists(offset))
								{
									AddEvent(new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
								}
								break;
							}
							case 0x9D:
							case 0xB0:
							case 0xC0:
							{
								if (!EventExists(offset))
								{
									AddEvent(new UnknownCommand { Command = cmd, Args = Array.Empty<byte>() });
								}
								break;
							}
							case 0x9C:
							case 0xA9:
							case 0xAA:
							case 0xB1:
							case 0xB2:
							case 0xB3:
							case 0xB5:
							case 0xB6:
							case 0xBC:
							case 0xBE:
							case 0xBF:
							case 0xC3:
							case 0xD0:
							case 0xD1:
							case 0xD2:
							case 0xDB:
							case 0xDF:
							case 0xE1:
							case 0xE7:
							case 0xE9:
							case 0xEF:
							case 0xF6:
							{
								byte[] args = new byte[1];
								r.ReadBytes(args);
								if (!EventExists(offset))
								{
									AddEvent(new UnknownCommand { Command = cmd, Args = args });
								}
								break;
							}
							case 0xA8:
							case 0xB4:
							case 0xD3:
							case 0xD5:
							case 0xD6:
							case 0xD8:
							case 0xF2:
							{
								byte[] args = new byte[2];
								r.ReadBytes(args);
								if (!EventExists(offset))
								{
									AddEvent(new UnknownCommand { Command = cmd, Args = args });
								}
								break;
							}
							case 0xAF:
							case 0xD4:
							case 0xE2:
							case 0xEA:
							case 0xF3:
							{
								byte[] args = new byte[3];
								r.ReadBytes(args);
								if (!EventExists(offset))
								{
									AddEvent(new UnknownCommand { Command = cmd, Args = args });
								}
								break;
							}
							case 0xDD:
							case 0xE5:
							case 0xED:
							case 0xF1:
							{
								byte[] args = new byte[4];
								r.ReadBytes(args);
								if (!EventExists(offset))
								{
									AddEvent(new UnknownCommand { Command = cmd, Args = args });
								}
								break;
							}
							case 0xDC:
							case 0xE4:
							case 0xEC:
							case 0xF0:
							{
								byte[] args = new byte[5];
								r.ReadBytes(args);
								if (!EventExists(offset))
								{
									AddEvent(new UnknownCommand { Command = cmd, Args = args });
								}
								break;
							}
							default: throw new DSEInvalidCMDException(trackIndex, (int)offset, cmd);
						}
					}
				}
				r.Stream.Position = chunkStart + 0xC;
				uint chunkLength = r.ReadUInt32();
				r.Stream.Position += chunkLength;
				// Align 4
				while (r.Stream.Position % 4 != 0)
				{
					r.Stream.Position++;
				}
			}
			SetTicks();
		}
	}
	public void SetCurrentPosition(long ticks)
	{
		if (_tracks is null)
		{
			SongEnded?.Invoke();
			return;
		}

		if (State is PlayerState.Playing or PlayerState.Paused or PlayerState.Stopped)
		{
			if (State == PlayerState.Playing)
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
					for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
					{
						Track track = _tracks[trackIndex];
						if (!track.Stopped)
						{
							track.Tick();
							while (track.Rest == 0 && !track.Stopped)
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
			}
		finish:
			for (int i = 0; i < _tracks.Length; i++)
			{
				_tracks[i].StopAllChannels();
			}
			Pause();
		}
	}
	public void Play()
	{
		if (_tracks == null)
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
		else if (State is PlayerState.Paused or PlayerState.Stopped)
		{
			State = PlayerState.Playing;
			CreateThread();
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
		if (State is PlayerState.Playing or PlayerState.Paused or PlayerState.Stopped)
		{
			State = PlayerState.ShutDown;
			WaitThread();
		}
	}
	public void UpdateSongState(SongState info)
	{
		info.Tempo = _tempo;
		for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
		{
			Track track = _tracks[trackIndex];
			SongState.Track tin = info.Tracks[trackIndex];
			tin.Position = track.CurOffset;
			tin.Rest = track.Rest;
			tin.Voice = track.Voice;
			tin.Type = "PCM";
			tin.Volume = track.Volume;
			tin.PitchBend = track.PitchBend;
			tin.Extra = track.Octave;
			tin.Panpot = track.Panpot;

			Channel[] channels = track.Channels.ToArray();
			if (channels.Length == 0)
			{
				tin.Keys[0] = byte.MaxValue;
				tin.LeftVolume = 0f;
				tin.RightVolume = 0f;
				//tin.Type = string.Empty;
			}
			else
			{
				int numKeys = 0;
				float left = 0f;
				float right = 0f;
				for (int j = 0; j < channels.Length; j++)
				{
					Channel c = channels[j];
					if (!Utils.IsStateRemovable(c.State))
					{
						tin.Keys[numKeys++] = c.Key;
					}
					float a = (float)(-c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
					if (a > left)
					{
						left = a;
					}
					a = (float)(c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
					if (a > right)
					{
						right = a;
					}
				}
				tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
				tin.LeftVolume = left;
				tin.RightVolume = right;
				//tin.Type = string.Join(", ", channels.Select(c => c.State.ToString()));
			}
		}
	}

	private void ExecuteNext(Track track)
	{
		byte cmd = _smdFile[track.CurOffset++];
		if (cmd <= 0x7F)
		{
			byte arg = _smdFile[track.CurOffset++];
			int numParams = (arg & 0xC0) >> 6;
			int oct = ((arg & 0x30) >> 4) - 2;
			int n = arg & 0xF;
			if (n >= 12)
			{
				throw new DSEInvalidNoteException(track.Index, track.CurOffset - 2, n);
			}

			uint duration;
			if (numParams == 0)
			{
				duration = track.LastNoteDuration;
			}
			else
			{
				duration = 0;
				for (int b = 0; b < numParams; b++)
				{
					duration = (duration << 8) | _smdFile[track.CurOffset++];
				}
				track.LastNoteDuration = duration;
			}
			Channel? channel = _mixer.AllocateChannel();
			if (channel is null)
			{
				throw new Exception("Not enough channels");
			}

			channel.Stop();
			track.Octave = (byte)(track.Octave + oct);
			if (channel.StartPCM(_localSWD, _masterSWD, track.Voice, n + (12 * track.Octave), duration))
			{
				channel.NoteVelocity = cmd;
				channel.Owner = track;
				track.Channels.Add(channel);
			}
		}
		else if (cmd >= 0x80 && cmd <= 0x8F)
		{
			track.LastRest = Utils.FixedRests[cmd - 0x80];
			track.Rest = track.LastRest;
		}
		else // 0x90-0xFF
		{
			// TODO: 0x95, 0x9E
			switch (cmd)
			{
				case 0x90:
				{
					track.Rest = track.LastRest;
					break;
				}
				case 0x91:
				{
					track.LastRest = (uint)(track.LastRest + (sbyte)_smdFile[track.CurOffset++]);
					track.Rest = track.LastRest;
					break;
				}
				case 0x92:
				{
					track.LastRest = _smdFile[track.CurOffset++];
					track.Rest = track.LastRest;
					break;
				}
				case 0x93:
				{
					track.LastRest = (uint)(_smdFile[track.CurOffset++] | (_smdFile[track.CurOffset++] << 8));
					track.Rest = track.LastRest;
					break;
				}
				case 0x94:
				{
					track.LastRest = (uint)(_smdFile[track.CurOffset++] | (_smdFile[track.CurOffset++] << 8) | (_smdFile[track.CurOffset++] << 16));
					track.Rest = track.LastRest;
					break;
				}
				case 0x96:
				case 0x97:
				case 0x9A:
				case 0x9B:
				case 0x9F:
				case 0xA2:
				case 0xA3:
				case 0xA6:
				case 0xA7:
				case 0xAD:
				case 0xAE:
				case 0xB7:
				case 0xB8:
				case 0xB9:
				case 0xBA:
				case 0xBB:
				case 0xBD:
				case 0xC1:
				case 0xC2:
				case 0xC4:
				case 0xC5:
				case 0xC6:
				case 0xC7:
				case 0xC8:
				case 0xC9:
				case 0xCA:
				case 0xCC:
				case 0xCD:
				case 0xCE:
				case 0xCF:
				case 0xD9:
				case 0xDA:
				case 0xDE:
				case 0xE6:
				case 0xEB:
				case 0xEE:
				case 0xF4:
				case 0xF5:
				case 0xF7:
				case 0xF9:
				case 0xFA:
				case 0xFB:
				case 0xFC:
				case 0xFD:
				case 0xFE:
				case 0xFF:
				{
					track.Stopped = true;
					break;
				}
				case 0x98:
				{
					if (track.LoopOffset == -1)
					{
						track.Stopped = true;
					}
					else
					{
						track.CurOffset = track.LoopOffset;
					}
					break;
				}
				case 0x99:
				{
					track.LoopOffset = track.CurOffset;
					break;
				}
				case 0xA0:
				{
					track.Octave = _smdFile[track.CurOffset++];
					break;
				}
				case 0xA1:
				{
					track.Octave = (byte)(track.Octave + (sbyte)_smdFile[track.CurOffset++]);
					break;
				}
				case 0xA4:
				case 0xA5:
				{
					_tempo = _smdFile[track.CurOffset++];
					break;
				}
				case 0xAB:
				{
					track.CurOffset++;
					break;
				}
				case 0xAC:
				{
					track.Voice = _smdFile[track.CurOffset++];
					break;
				}
				case 0xCB:
				case 0xF8:
				{
					track.CurOffset += 2;
					break;
				}
				case 0xD7:
				{
					track.PitchBend = (ushort)(_smdFile[track.CurOffset++] | (_smdFile[track.CurOffset++] << 8));
					break;
				}
				case 0xE0:
				{
					track.Volume = _smdFile[track.CurOffset++];
					break;
				}
				case 0xE3:
				{
					track.Expression = _smdFile[track.CurOffset++];
					break;
				}
				case 0xE8:
				{
					track.Panpot = (sbyte)(_smdFile[track.CurOffset++] - 0x40);
					break;
				}
				case 0x9D:
				case 0xB0:
				case 0xC0:
				{
					break;
				}
				case 0x9C:
				case 0xA9:
				case 0xAA:
				case 0xB1:
				case 0xB2:
				case 0xB3:
				case 0xB5:
				case 0xB6:
				case 0xBC:
				case 0xBE:
				case 0xBF:
				case 0xC3:
				case 0xD0:
				case 0xD1:
				case 0xD2:
				case 0xDB:
				case 0xDF:
				case 0xE1:
				case 0xE7:
				case 0xE9:
				case 0xEF:
				case 0xF6:
				{
					track.CurOffset++;
					break;
				}
				case 0xA8:
				case 0xB4:
				case 0xD3:
				case 0xD5:
				case 0xD6:
				case 0xD8:
				case 0xF2:
				{
					track.CurOffset += 2;
					break;
				}
				case 0xAF:
				case 0xD4:
				case 0xE2:
				case 0xEA:
				case 0xF3:
				{
					track.CurOffset += 3;
					break;
				}
				case 0xDD:
				case 0xE5:
				case 0xED:
				case 0xF1:
				{
					track.CurOffset += 4;
					break;
				}
				case 0xDC:
				case 0xE4:
				case 0xEC:
				case 0xF0:
				{
					track.CurOffset += 5;
					break;
				}
				default: throw new DSEInvalidCMDException(track.Index, track.CurOffset - 1, cmd);
			}
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

			while (_tempoStack >= 240)
			{
				_tempoStack -= 240;
				bool allDone = true;
				for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
				{
					Track track = _tracks[trackIndex];
					track.Tick();
					while (track.Rest == 0 && !track.Stopped)
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
									if (ev.Offset == track.CurOffset)
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
					_mixer.ChannelTick();
					_mixer.Process(playing, recording);
					_time.Stop();
					State = PlayerState.Stopped;
					SongEnded?.Invoke();
					return;
				}
			}
			_tempoStack += _tempo;
			_mixer.ChannelTick();
			_mixer.Process(playing, recording);
			if (playing)
			{
				_time.Wait();
			}
		}
		_time.Stop();
	}
}
