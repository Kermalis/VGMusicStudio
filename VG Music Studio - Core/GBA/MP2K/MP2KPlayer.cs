using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed partial class MP2KPlayer : IPlayer
{
	private readonly MP2KMixer _mixer;
	private readonly MP2KConfig _config;
	private readonly TimeBarrier _time;
	private Thread? _thread;
	private ushort _tempo;
	private int _tempoStack;
	private long _elapsedLoops;

	private MP2KLoadedSong? _loadedSong;
	public ILoadedSong? LoadedSong => _loadedSong;
	public bool ShouldFadeOut { get; set; }
	public long NumLoops { get; set; }

	public PlayerState State { get; private set; }
	public event Action? SongEnded;

	private readonly string?[] _voiceTypeCache;

	internal MP2KPlayer(MP2KConfig config, MP2KMixer mixer)
	{
		_config = config;
		_mixer = mixer;

		_voiceTypeCache = new string[256];

		_time = new TimeBarrier(GBA.GBAUtils.AGB_FPS);
	}
	private void CreateThread()
	{
		_thread = new Thread(Tick) { Name = "MP2K Player Tick" };
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
		_tempo = 150;
		_tempoStack = 0;
		_elapsedLoops = 0;
		_loadedSong!.ElapsedTicks = 0;
		_mixer.ResetFade();
		for (int trackIndex = 0; trackIndex < _loadedSong.Tracks.Length; trackIndex++)
		{
			_loadedSong.Tracks[trackIndex].Init();
		}
	}
	public void LoadSong(long index)
	{
		int? oldVoiceTableOffset = _loadedSong?.VoiceTableOffset;
		if (_loadedSong is not null)
		{
			_loadedSong.Dispose();
			_loadedSong = null;
		}

		// If there's an exception, this will remain null
		_loadedSong = new MP2KLoadedSong(index, this, _config, oldVoiceTableOffset, _voiceTypeCache);
		_loadedSong.SetTicks();
		if (_loadedSong.Events.Length == 0)
		{
			_loadedSong.Dispose();
			_loadedSong = null;
		}
	}
	public void SetCurrentPosition(long ticks)
	{
		if (_loadedSong is null)
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
		MP2KLoadedSong s = _loadedSong;
		bool u = false;
		while (ticks != s.ElapsedTicks)
		{
			while (_tempoStack >= 150)
			{
				_tempoStack -= 150;
				for (int trackIndex = 0; trackIndex < s.Tracks.Length; trackIndex++)
				{
					Track track = s.Tracks[trackIndex];
					if (!track.Stopped)
					{
						track.Tick();
						while (track.Rest == 0 && !track.Stopped)
						{
							ExecuteNext(track, ref u);
						}
					}
				}
				s.ElapsedTicks++;
				if (s.ElapsedTicks == ticks)
				{
					break;
				}
			}
			_tempoStack += _tempo;
		}

		for (int i = 0; i < s.Tracks.Length; i++)
		{
			s.Tracks[i].StopAllChannels();
		}
		Pause();
	}
	public void Play()
	{
		if (_loadedSong is null)
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

	public void SaveAsMIDI(string fileName, MIDISaveArgs args)
	{
		_loadedSong!.SaveAsMIDI(fileName, args);
	}
	public void UpdateSongState(SongState info)
	{
		info.Tempo = _tempo;
		for (int trackIndex = 0; trackIndex < _loadedSong!.Tracks.Length; trackIndex++)
		{
			Track track = _loadedSong.Tracks[trackIndex];
			SongState.Track tin = info.Tracks[trackIndex];
			tin.Position = track.DataOffset;
			tin.Rest = track.Rest;
			tin.Voice = track.Voice;
			tin.LFO = track.LFODepth;
			ref string? voiceType = ref _voiceTypeCache[track.Voice];
			if (voiceType is null)
			{
				byte t = _config.ROM[_loadedSong.VoiceTableOffset + (track.Voice * 12)];
				if (t == (byte)VoiceFlags.KeySplit)
				{
					voiceType = "Key Split";
				}
				else if (t == (byte)VoiceFlags.Drum)
				{
					voiceType = "Drum";
				}
				else
				{
					switch ((VoiceType)(t & 0x7)) // Disregard the other flags
					{
						case VoiceType.PCM8: voiceType = "PCM8"; break;
						case VoiceType.Square1: voiceType = "Square 1"; break;
						case VoiceType.Square2: voiceType = "Square 2"; break;
						case VoiceType.PCM4: voiceType = "PCM4"; break;
						case VoiceType.Noise: voiceType = "Noise"; break;
						case VoiceType.Invalid5: voiceType = "Invalid 5"; break;
						case VoiceType.Invalid6: voiceType = "Invalid 6"; break;
						default: voiceType = "Invalid 7"; break; // VoiceType.Invalid7
					}
				}
			}
			tin.Type = voiceType;
			tin.Volume = track.GetVolume();
			tin.PitchBend = track.GetPitch();
			tin.Panpot = track.GetPanpot();

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
					if (c.State < EnvelopeState.Releasing)
					{
						tin.Keys[numKeys++] = c.Note.OriginalNote;
					}
					ChannelVolume vol = c.GetVolume();
					if (vol.LeftVol > left)
					{
						left = vol.LeftVol;
					}
					if (vol.RightVol > right)
					{
						right = vol.RightVol;
					}
				}
				tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
				tin.LeftVolume = left;
				tin.RightVolume = right;
			}
		}
	}

	private void PlayNote(Track track, byte note, byte velocity, byte addedDuration)
	{
		int n = note + track.Transpose;
		if (n < 0)
		{
			n = 0;
		}
		else if (n > 0x7F)
		{
			n = 0x7F;
		}
		note = (byte)n;
		track.PrevNote = note;
		track.PrevVelocity = velocity;
		if (!track.Ready)
		{
			return; // Tracks do not play unless they have had a voice change event
		}

		bool fromDrum = false;
		int offset = _loadedSong!.VoiceTableOffset + (track.Voice * 12);
		while (true)
		{
			ref VoiceEntry v = ref MemoryMarshal.AsRef<VoiceEntry>(_config.ROM.AsSpan(offset));
			if (v.Type == (int)VoiceFlags.KeySplit)
			{
				fromDrum = false; // In case there is a multi within a drum
				byte inst = _config.ROM[v.Int8 - GBAUtils.CartridgeOffset + note];
				offset = v.Int4 - GBAUtils.CartridgeOffset + (inst * 12);
			}
			else if (v.Type == (int)VoiceFlags.Drum)
			{
				fromDrum = true;
				offset = v.Int4 - GBAUtils.CartridgeOffset + (note * 12);
			}
			else
			{
				var ni = new NoteInfo
				{
					Duration = track.RunCmd == 0xCF ? -1 : (Utils.RestTable[track.RunCmd - 0xCF] + addedDuration),
					Velocity = velocity,
					OriginalNote = note,
					Note = fromDrum ? v.RootNote : note,
				};
				var type = (VoiceType)(v.Type & 0x7);
				int instPan = v.Pan;
				instPan = (instPan & 0x80) != 0 ? instPan - 0xC0 : 0;
				switch (type)
				{
					case VoiceType.PCM8:
					{
						bool bFixed = (v.Type & (int)VoiceFlags.Fixed) != 0;
						bool bCompressed = _config.HasPokemonCompression && ((v.Type & (int)VoiceFlags.Compressed) != 0);
						_mixer.AllocPCM8Channel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							bFixed, bCompressed, v.Int4 - GBAUtils.CartridgeOffset);
						return;
					}
					case VoiceType.Square1:
					case VoiceType.Square2:
					{
						_mixer.AllocPSGChannel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							type, (SquarePattern)v.Int4);
						return;
					}
					case VoiceType.PCM4:
					{
						_mixer.AllocPSGChannel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							type, v.Int4 - GBAUtils.CartridgeOffset);
						return;
					}
					case VoiceType.Noise:
					{
						_mixer.AllocPSGChannel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							type, (NoisePattern)v.Int4);
						return;
					}
				}
				return; // Prevent infinite loop with invalid instruments
			}
		}
	}
	internal void ExecuteNext(Track track, ref bool update)
	{
		byte[] rom = _config.ROM;
		byte cmd = rom[track.DataOffset++];
		if (cmd >= 0xBD) // Commands that work within running status
		{
			track.RunCmd = cmd;
		}

		if (track.RunCmd >= 0xCF && cmd <= 0x7F) // Within running status
		{
			byte peek0 = rom[track.DataOffset];
			byte peek1 = rom[track.DataOffset + 1];
			byte velocity, addedDuration;
			if (peek0 > 0x7F)
			{
				velocity = track.PrevVelocity;
				addedDuration = 0;
			}
			else if (peek1 > 3)
			{
				track.DataOffset++;
				velocity = peek0;
				addedDuration = 0;
			}
			else
			{
				track.DataOffset += 2;
				velocity = peek0;
				addedDuration = peek1;
			}
			PlayNote(track, cmd, velocity, addedDuration);
		}
		else if (cmd >= 0xCF)
		{
			byte peek0 = rom[track.DataOffset];
			byte peek1 = rom[track.DataOffset + 1];
			byte peek2 = rom[track.DataOffset + 2];
			byte key, velocity, addedDuration;
			if (peek0 > 0x7F)
			{
				key = track.PrevNote;
				velocity = track.PrevVelocity;
				addedDuration = 0;
			}
			else if (peek1 > 0x7F)
			{
				track.DataOffset++;
				key = peek0;
				velocity = track.PrevVelocity;
				addedDuration = 0;
			}
			else if (cmd == 0xCF || peek2 > 3)
			{
				track.DataOffset += 2;
				key = peek0;
				velocity = peek1;
				addedDuration = 0;
			}
			else
			{
				track.DataOffset += 3;
				key = peek0;
				velocity = peek1;
				addedDuration = peek2;
			}
			PlayNote(track, key, velocity, addedDuration);
		}
		else if (cmd >= 0x80 && cmd <= 0xB0)
		{
			track.Rest = Utils.RestTable[cmd - 0x80];
		}
		else if (track.RunCmd < 0xCF && cmd <= 0x7F)
		{
			switch (track.RunCmd)
			{
				case 0xBD:
				{
					track.Voice = cmd;
					//track.Ready = true; // This is unnecessary because if we're in running status of a voice command, then Ready was already set
					break;
				}
				case 0xBE:
				{
					track.Volume = cmd;
					update = true;
					break;
				}
				case 0xBF:
				{
					track.Panpot = (sbyte)(cmd - 0x40);
					update = true;
					break;
				}
				case 0xC0:
				{
					track.PitchBend = (sbyte)(cmd - 0x40);
					update = true;
					break;
				}
				case 0xC1:
				{
					track.PitchBendRange = cmd;
					update = true;
					break;
				}
				case 0xC2:
				{
					track.LFOSpeed = cmd;
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC3:
				{
					track.LFODelay = cmd;
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC4:
				{
					track.LFODepth = cmd;
					update = true;
					break;
				}
				case 0xC5:
				{
					track.LFOType = (LFOType)cmd;
					update = true;
					break;
				}
				case 0xC8:
				{
					track.Tune = (sbyte)(cmd - 0x40);
					update = true;
					break;
				}
				case 0xCD:
				{
					track.DataOffset++;
					break;
				}
				case 0xCE:
				{
					track.PrevNote = cmd;
					int k = cmd + track.Transpose;
					if (k < 0)
					{
						k = 0;
					}
					else if (k > 0x7F)
					{
						k = 0x7F;
					}
					track.ReleaseChannels(k);
					break;
				}
				default: throw new MP2KInvalidRunningStatusCMDException(track.Index, track.DataOffset - 1, track.RunCmd);
			}
		}
		else if (cmd > 0xB0 && cmd < 0xCF)
		{
			switch (cmd)
			{
				case 0xB1:
				case 0xB6:
				{
					track.Stopped = true;
					//track.ReleaseAllTieingChannels(); // Necessary?
					break;
				}
				case 0xB2:
				{
					track.DataOffset = (rom[track.DataOffset++] | (rom[track.DataOffset++] << 8) | (rom[track.DataOffset++] << 16) | (rom[track.DataOffset++] << 24)) - GBA.GBAUtils.CartridgeOffset;
					break;
				}
				case 0xB3:
				{
					if (track.CallStackDepth >= 3)
					{
						throw new MP2KTooManyNestedCallsException(track.Index);
					}

					int callOffset = (rom[track.DataOffset++] | (rom[track.DataOffset++] << 8) | (rom[track.DataOffset++] << 16) | (rom[track.DataOffset++] << 24)) - GBA.GBAUtils.CartridgeOffset;
					track.CallStack[track.CallStackDepth] = track.DataOffset;
					track.CallStackDepth++;
					track.DataOffset = callOffset;
					break;
				}
				case 0xB4:
				{
					if (track.CallStackDepth != 0)
					{
						track.CallStackDepth--;
						track.DataOffset = track.CallStack[track.CallStackDepth];
					}
					break;
				}
				/*case 0xB5: // TODO: Logic so this isn't an infinite loop
                    {
                        byte times = config.Reader.ReadByte();
                        int repeatOffset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset;
                        if (!EventExists(offset))
                        {
                            AddEvent(new RepeatCommand { Times = times, Offset = repeatOffset });
                        }
                        break;
                    }*/
				case 0xB9:
				{
					track.DataOffset += 3;
					break;
				}
				case 0xBA:
				{
					track.Priority = rom[track.DataOffset++];
					break;
				}
				case 0xBB:
				{
					_tempo = (ushort)(rom[track.DataOffset++] * 2);
					break;
				}
				case 0xBC:
				{
					track.Transpose = (sbyte)rom[track.DataOffset++];
					break;
				}
				// Commands that work within running status:
				case 0xBD:
				{
					track.Voice = rom[track.DataOffset++];
					track.Ready = true;
					break;
				}
				case 0xBE:
				{
					track.Volume = rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xBF:
				{
					track.Panpot = (sbyte)(rom[track.DataOffset++] - 0x40);
					update = true;
					break;
				}
				case 0xC0:
				{
					track.PitchBend = (sbyte)(rom[track.DataOffset++] - 0x40);
					update = true;
					break;
				}
				case 0xC1:
				{
					track.PitchBendRange = rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xC2:
				{
					track.LFOSpeed = rom[track.DataOffset++];
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC3:
				{
					track.LFODelay = rom[track.DataOffset++];
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC4:
				{
					track.LFODepth = rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xC5:
				{
					track.LFOType = (LFOType)rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xC8:
				{
					track.Tune = (sbyte)(rom[track.DataOffset++] - 0x40);
					update = true;
					break;
				}
				case 0xCD:
				{
					track.DataOffset += 2;
					break;
				}
				case 0xCE:
				{
					byte peek = rom[track.DataOffset];
					if (peek > 0x7F)
					{
						track.ReleaseChannels(track.PrevNote);
					}
					else
					{
						track.DataOffset++;
						track.PrevNote = peek;
						int k = peek + track.Transpose;
						if (k < 0)
						{
							k = 0;
						}
						else if (k > 0x7F)
						{
							k = 0x7F;
						}
						track.ReleaseChannels(k);
					}
					break;
				}
				default: throw new MP2KInvalidCMDException(track.Index, track.DataOffset - 1, cmd);
			}
		}
	}

	private void Tick()
	{
		MP2KLoadedSong s = _loadedSong!;
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

			while (_tempoStack >= 150)
			{
				_tempoStack -= 150;
				bool allDone = true;
				for (int trackIndex = 0; trackIndex < s.Tracks.Length; trackIndex++)
				{
					Track track = s.Tracks[trackIndex];
					track.Tick();
					bool update = false;
					while (track.Rest == 0 && !track.Stopped)
					{
						ExecuteNext(track, ref update);
					}
					if (trackIndex == s.LongestTrack)
					{
						if (s.ElapsedTicks == s.MaxTicks)
						{
							if (!track.Stopped)
							{
								List<SongEvent> evs = s.Events[trackIndex];
								for (int i = 0; i < evs.Count; i++)
								{
									SongEvent ev = evs[i];
									if (ev.Offset == track.DataOffset)
									{
										s.ElapsedTicks = ev.Ticks[0] - track.Rest;
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
							s.ElapsedTicks++;
						}
					}
					if (!track.Stopped)
					{
						allDone = false;
					}
					if (track.Channels.Count > 0)
					{
						allDone = false;
						if (update || track.LFODepth > 0)
						{
							track.UpdateChannels();
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
					_mixer.Process(playing, recording);
					_time.Stop();
					State = PlayerState.Stopped;
					SongEnded?.Invoke();
					return;
				}
			}
			_tempoStack += _tempo;
			_mixer.Process(playing, recording);
			if (playing)
			{
				_time.Wait();
			}
		}
		_time.Stop();
	}

	public void Dispose()
	{
		if (State is not PlayerState.ShutDown)
		{
			State = PlayerState.ShutDown;
			WaitThread();
		}
	}
}
