using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong
{
	private void AddEvent(byte trackIndex, long cmdOffset, ICommand command)
	{
		Events[trackIndex].Add(new SongEvent(cmdOffset, command));
	}
	private bool EventExists(byte trackIndex, long cmdOffset)
	{
		return Events[trackIndex].Exists(e => e.Offset == cmdOffset);
	}

	private void EmulateNote(byte trackIndex, long cmdOffset, byte key, byte velocity, byte addedDuration, ref byte runCmd, ref byte prevKey, ref byte prevVelocity)
	{
		prevKey = key;
		prevVelocity = velocity;
		if (EventExists(trackIndex, cmdOffset))
		{
			return;
		}

		AddEvent(trackIndex, cmdOffset, new NoteCommand
		{
			Note = key,
			Velocity = velocity,
			Duration = runCmd == 0xCF ? -1 : (MP2KUtils.RestTable[runCmd - 0xCF] + addedDuration),
		});
	}

	private void AddTrackEvents(byte trackIndex, long trackStart)
	{
		Events[trackIndex] = new List<SongEvent>();
		byte runCmd = 0;
		byte prevKey = 0;
		byte prevVelocity = 0x7F;
		int callStackDepth = 0;
		AddEvents(trackIndex, trackStart, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
	}
	private void AddEvents(byte trackIndex, long startOffset, ref byte runCmd, ref byte prevKey, ref byte prevVelocity, ref int callStackDepth)
	{
		EndianBinaryReader r = _player.Config.Reader;
		r.Stream.Position = startOffset;

		Span<byte> peek = stackalloc byte[3];
		bool cont = true;
		while (cont)
		{
			long offset = r.Stream.Position;

			byte cmd = r.ReadByte();
			if (cmd >= 0xBD) // Commands that work within running status
			{
				runCmd = cmd;
			}

			#region TIE & Notes

			if (runCmd >= 0xCF && cmd <= 0x7F) // Within running status
			{
				byte velocity, addedDuration;
				r.PeekBytes(peek.Slice(0, 2));
				if (peek[0] > 0x7F)
				{
					velocity = prevVelocity;
					addedDuration = 0;
				}
				else if (peek[1] > 3)
				{
					velocity = r.ReadByte();
					addedDuration = 0;
				}
				else
				{
					velocity = r.ReadByte();
					addedDuration = r.ReadByte();
				}
				EmulateNote(trackIndex, offset, cmd, velocity, addedDuration, ref runCmd, ref prevKey, ref prevVelocity);
			}
			else if (cmd >= 0xCF)
			{
				byte key, velocity, addedDuration;
				r.PeekBytes(peek);
				if (peek[0] > 0x7F)
				{
					key = prevKey;
					velocity = prevVelocity;
					addedDuration = 0;
				}
				else if (peek[1] > 0x7F)
				{
					key = r.ReadByte();
					velocity = prevVelocity;
					addedDuration = 0;
				}
				// TIE (0xCF) cannot have an added duration so it needs to stop here
				else if (cmd == 0xCF || peek[2] > 3)
				{
					key = r.ReadByte();
					velocity = r.ReadByte();
					addedDuration = 0;
				}
				else
				{
					key = r.ReadByte();
					velocity = r.ReadByte();
					addedDuration = r.ReadByte();
				}
				EmulateNote(trackIndex, offset, key, velocity, addedDuration, ref runCmd, ref prevKey, ref prevVelocity);
			}

			#endregion

			#region Rests

			else if (cmd >= 0x80 && cmd <= 0xB0)
			{
				if (!EventExists(trackIndex, offset))
				{
					AddEvent(trackIndex, offset, new RestCommand { Rest = MP2KUtils.RestTable[cmd - 0x80] });
				}
			}

			#endregion

			#region Commands

			else if (runCmd < 0xCF && cmd <= 0x7F)
			{
				switch (runCmd)
				{
					case 0xBD:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new VoiceCommand { Voice = cmd });
						}
						break;
					}
					case 0xBE:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new VolumeCommand { Volume = cmd });
						}
						break;
					}
					case 0xBF:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PanpotCommand { Panpot = (sbyte)(cmd - 0x40) });
						}
						break;
					}
					case 0xC0:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PitchBendCommand { Bend = (sbyte)(cmd - 0x40) });
						}
						break;
					}
					case 0xC1:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PitchBendRangeCommand { Range = cmd });
						}
						break;
					}
					case 0xC2:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFOSpeedCommand { Speed = cmd });
						}
						break;
					}
					case 0xC3:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFODelayCommand { Delay = cmd });
						}
						break;
					}
					case 0xC4:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFODepthCommand { Depth = cmd });
						}
						break;
					}
					case 0xC5:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFOTypeCommand { Type = (LFOType)cmd });
						}
						break;
					}
					case 0xC8:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new TuneCommand { Tune = (sbyte)(cmd - 0x40) });
						}
						break;
					}
					case 0xCD:
					{
						byte arg = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LibraryCommand { Command = cmd, Argument = arg });
						}
						break;
					}
					case 0xCE:
					{
						prevKey = cmd;
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new EndOfTieCommand { Note = cmd });
						}
						break;
					}
					default: throw new MP2KInvalidRunningStatusCMDException(trackIndex, (int)offset, runCmd);
				}
			}
			else if (cmd > 0xB0 && cmd < 0xCF)
			{
				switch (cmd)
				{
					case 0xB1:
					case 0xB6:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new FinishCommand { Prev = cmd == 0xB6 });
						}
						cont = false;
						break;
					}
					case 0xB2:
					{
						int jumpOffset = r.ReadInt32() - GBAUtils.CARTRIDGE_OFFSET;
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new JumpCommand { Offset = jumpOffset });
							if (!EventExists(trackIndex, jumpOffset))
							{
								AddEvents(trackIndex, jumpOffset, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
							}
						}
						cont = false;
						break;
					}
					case 0xB3:
					{
						int callOffset = r.ReadInt32() - GBAUtils.CARTRIDGE_OFFSET;
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new CallCommand { Offset = callOffset });
						}
						if (callStackDepth < 3)
						{
							long backup = r.Stream.Position;
							callStackDepth++;
							AddEvents(trackIndex, callOffset, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
							r.Stream.Position = backup;
						}
						else
						{
							throw new MP2KTooManyNestedCallsException(trackIndex);
						}
						break;
					}
					case 0xB4:
					{
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new ReturnCommand());
						}
						if (callStackDepth != 0)
						{
							cont = false;
							callStackDepth--;
						}
						break;
					}
					/*case 0xB5: // TODO: Logic so this isn't an infinite loop
						{
							byte times = config.Reader.ReadByte();
							int repeatOffset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset;
							if (!EventExists(offset, trackEvents))
							{
								AddEvent(new RepeatCommand { Times = times, Offset = repeatOffset });
							}
							break;
						}*/
					case 0xB9:
					{
						byte op = r.ReadByte();
						byte address = r.ReadByte();
						byte data = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new MemoryAccessCommand { Operator = op, Address = address, Data = data });
						}
						break;
					}
					case 0xBA:
					{
						byte priority = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PriorityCommand { Priority = priority });
						}
						break;
					}
					case 0xBB:
					{
						byte tempoArg = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new TempoCommand { Tempo = (ushort)(tempoArg * 2) });
						}
						break;
					}
					case 0xBC:
					{
						sbyte transpose = r.ReadSByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new TransposeCommand { Transpose = transpose });
						}
						break;
					}
					// Commands that work within running status:
					case 0xBD:
					{
						byte voice = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new VoiceCommand { Voice = voice });
						}
						break;
					}
					case 0xBE:
					{
						byte volume = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new VolumeCommand { Volume = volume });
						}
						break;
					}
					case 0xBF:
					{
						byte panArg = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
						}
						break;
					}
					case 0xC0:
					{
						byte bendArg = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PitchBendCommand { Bend = (sbyte)(bendArg - 0x40) });
						}
						break;
					}
					case 0xC1:
					{
						byte range = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new PitchBendRangeCommand { Range = range });
						}
						break;
					}
					case 0xC2:
					{
						byte speed = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFOSpeedCommand { Speed = speed });
						}
						break;
					}
					case 0xC3:
					{
						byte delay = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFODelayCommand { Delay = delay });
						}
						break;
					}
					case 0xC4:
					{
						byte depth = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFODepthCommand { Depth = depth });
						}
						break;
					}
					case 0xC5:
					{
						byte type = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LFOTypeCommand { Type = (LFOType)type });
						}
						break;
					}
					case 0xC8:
					{
						byte tuneArg = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new TuneCommand { Tune = (sbyte)(tuneArg - 0x40) });
						}
						break;
					}
					case 0xCD:
					{
						byte command = r.ReadByte();
						byte arg = r.ReadByte();
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new LibraryCommand { Command = command, Argument = arg });
						}
						break;
					}
					case 0xCE:
					{
						int key = r.PeekByte() <= 0x7F ? (prevKey = r.ReadByte()) : -1;
						if (!EventExists(trackIndex, offset))
						{
							AddEvent(trackIndex, offset, new EndOfTieCommand { Note = key });
						}
						break;
					}
					default: throw new MP2KInvalidCMDException(trackIndex, (int)offset, cmd);
				}
			}

			#endregion
		}
	}

	public void SetTicks()
	{
		MaxTicks = 0;
		bool u = false;
		for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
		{
			List<SongEvent> evs = Events[trackIndex];
			evs.Sort((e1, e2) => e1.Offset.CompareTo(e2.Offset));

			MP2KTrack track = Tracks[trackIndex];
			track.Init();

			_player.ElapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.DataOffset);
				if (track.CallStackDepth == 0 && e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(_player.ElapsedTicks);
				ExecuteNext(track, ref u);
				if (track.Stopped)
				{
					break;
				}

				_player.ElapsedTicks += track.Rest;
				track.Rest = 0;
			}
			if (_player.ElapsedTicks > MaxTicks)
			{
				LongestTrack = trackIndex;
				MaxTicks = _player.ElapsedTicks;
			}
			track.StopAllChannels();
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
			while (_player.TempoStack >= 150)
			{
				_player.TempoStack -= 150;
				for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
				{
					MP2KTrack track = Tracks[trackIndex];
					if (!track.Stopped)
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
		for (int i = 0; i < Tracks.Length; i++)
		{
			Tracks[i].StopAllChannels();
		}
	}
}
