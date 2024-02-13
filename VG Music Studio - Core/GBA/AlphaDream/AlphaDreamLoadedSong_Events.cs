using Kermalis.EndianBinaryIO;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed partial class AlphaDreamLoadedSong
{
	private void AddEvent(byte trackIndex, long cmdOffset, ICommand command)
	{
		Events[trackIndex]!.Add(new SongEvent(cmdOffset, command));
	}
	private bool EventExists(byte trackIndex, long cmdOffset)
	{
		return Events[trackIndex]!.Exists(e => e.Offset == cmdOffset);
	}

	private void AddTrackEvents(byte trackIndex, int trackStart)
	{
		Events[trackIndex] = new List<SongEvent>();
		AddEvents(trackIndex, trackStart);
	}
	private void AddEvents(byte trackIndex, int startOffset)
	{
		EndianBinaryReader r = _player.Config.Reader;
		r.Stream.Position = startOffset;

		bool cont = true;
		while (cont)
		{
			long cmdOffset = r.Stream.Position;
			byte cmd = r.ReadByte();
			switch (cmd)
			{
				case 0x00:
				{
					byte keyArg = r.ReadByte();
					switch (_player.Config.AudioEngineVersion)
					{
						case AudioEngineVersion.Hamtaro:
						{
							byte volume = r.ReadByte();
							byte duration = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new FreeNoteHamtaroCommand { Note = (byte)(keyArg - 0x80), Volume = volume, Duration = duration });
							}
							break;
						}
						case AudioEngineVersion.MLSS:
						{
							byte duration = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new FreeNoteMLSSCommand { Note = (byte)(keyArg - 0x80), Duration = duration });
							}
							break;
						}
					}
					break;
				}
				case 0xF0:
				{
					byte voice = r.ReadByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new VoiceCommand { Voice = voice });
					}
					break;
				}
				case 0xF1:
				{
					byte volume = r.ReadByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new VolumeCommand { Volume = volume });
					}
					break;
				}
				case 0xF2:
				{
					byte panArg = r.ReadByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new PanpotCommand { Panpot = (sbyte)(panArg - 0x80) });
					}
					break;
				}
				case 0xF4:
				{
					byte range = r.ReadByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new PitchBendRangeCommand { Range = range });
					}
					break;
				}
				case 0xF5:
				{
					sbyte bend = r.ReadSByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new PitchBendCommand { Bend = bend });
					}
					break;
				}
				case 0xF6:
				{
					byte rest = r.ReadByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = rest });
					}
					break;
				}
				case 0xF8:
				{
					short jumpOffset = r.ReadInt16();
					if (!EventExists(trackIndex, cmdOffset))
					{
						int off = (int)(r.Stream.Position + jumpOffset);
						AddEvent(trackIndex, cmdOffset, new JumpCommand { Offset = off });
						if (!EventExists(trackIndex, off))
						{
							AddEvents(trackIndex, off);
						}
					}
					cont = false;
					break;
				}
				case 0xF9:
				{
					byte tempoArg = r.ReadByte();
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new TrackTempoCommand { Tempo = tempoArg });
					}
					break;
				}
				case 0xFF:
				{
					if (!EventExists(trackIndex, cmdOffset))
					{
						AddEvent(trackIndex, cmdOffset, new FinishCommand());
					}
					cont = false;
					break;
				}
				default:
				{
					if (cmd >= 0xE0)
					{
						throw new AlphaDreamInvalidCMDException(trackIndex, (int)cmdOffset, cmd);
					}

					byte key = r.ReadByte();
					switch (_player.Config.AudioEngineVersion)
					{
						case AudioEngineVersion.Hamtaro:
						{
							byte volume = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new NoteHamtaroCommand { Note = key, Volume = volume, Duration = cmd });
							}
							break;
						}
						case AudioEngineVersion.MLSS:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new NoteMLSSCommand { Note = key, Duration = cmd });
							}
							break;
						}
					}
					break;
				}
			}
		}
	}

	public void SetTicks()
	{
		MaxTicks = 0;
		bool u = false;
		for (int trackIndex = 0; trackIndex < AlphaDreamPlayer.NUM_TRACKS; trackIndex++)
		{
			List<SongEvent>? evs = Events[trackIndex];
			if (evs is null)
			{
				continue;
			}

			evs.Sort((e1, e2) => e1.Offset.CompareTo(e2.Offset));

			AlphaDreamTrack track = _player.Tracks[trackIndex];
			track.Init();

			long elapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.DataOffset);
				if (e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(elapsedTicks);
				ExecuteNext(track, ref u);
				if (track.Stopped)
				{
					break;
				}

				elapsedTicks += track.Rest;
				track.Rest = 0;
			}
			if (elapsedTicks > MaxTicks)
			{
				LongestTrack = trackIndex;
				MaxTicks = elapsedTicks;
			}
			track.NoteDuration = 0;
		}
	}
	internal void SetCurTick(long ticks)
	{
		bool u = false;
		while (true)
		{
			if (_player.ElapsedTicks == ticks)
			{
				goto finish;
			}

			while (_player.TempoStack >= 75)
			{
				_player.TempoStack -= 75;
				for (int trackIndex = 0; trackIndex < AlphaDreamPlayer.NUM_TRACKS; trackIndex++)
				{
					AlphaDreamTrack track = _player.Tracks[trackIndex];
					if (track.IsEnabled && !track.Stopped)
					{
						track.Tick();
						while (track.Rest == 0 && !track.Stopped)
						{
							ExecuteNext(track, ref u);
						}
					}
				}
				_player.ElapsedTicks++;
				if (_player.ElapsedTicks == ticks)
				{
					goto finish;
				}
			}
			_player.TempoStack += _player.Tempo;
		}
	finish:
		for (int i = 0; i < AlphaDreamPlayer.NUM_TRACKS; i++)
		{
			_player.Tracks[i].NoteDuration = 0;
		}
	}
}
