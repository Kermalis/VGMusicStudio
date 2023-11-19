using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong : ILoadedSong
{
	public List<SongEvent>[] Events { get; private set; }
	public long MaxTicks { get; private set; }
	public long ElapsedTicks { get; internal set; }
	internal int LongestTrack;

	private readonly MP2KPlayer _player;
	public readonly int VoiceTableOffset;
	internal readonly Track[] Tracks;

	public MP2KLoadedSong(long index, MP2KPlayer player, MP2KConfig cfg, int? oldVoiceTableOffset, string?[] voiceTypeCache)
	{
		_player = player;

		ref SongEntry entry = ref MemoryMarshal.AsRef<SongEntry>(cfg.ROM.AsSpan(cfg.SongTableOffsets[0] + ((int)index * 8)));
		cfg.Reader.Stream.Position = entry.HeaderOffset - GBA.GBAUtils.CartridgeOffset;
		SongHeader header = cfg.Reader.ReadObject<SongHeader>(); // TODO: Can I RefStruct this? If not, should still ditch reader and use pointer
		VoiceTableOffset = header.VoiceTableOffset - GBA.GBAUtils.CartridgeOffset;
		if (oldVoiceTableOffset != VoiceTableOffset)
		{
			Array.Clear(voiceTypeCache);
		}

		Tracks = new Track[header.NumTracks];
		Events = new List<SongEvent>[header.NumTracks];
		for (byte trackIndex = 0; trackIndex < header.NumTracks; trackIndex++)
		{
			int trackStart = header.TrackOffsets[trackIndex] - GBA.GBAUtils.CartridgeOffset;
			Tracks[trackIndex] = new Track(trackIndex, trackStart);
			Events[trackIndex] = new List<SongEvent>();

			byte runCmd = 0, prevKey = 0, prevVelocity = 0x7F;
			int callStackDepth = 0;
			AddEvents(trackStart, cfg, trackIndex, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
		}
	}

	private static void AddEvent(List<SongEvent> trackEvents, long offset, ICommand command)
	{
		trackEvents.Add(new SongEvent(offset, command));
	}
	private static bool EventExists(List<SongEvent> trackEvents, long offset)
	{
		return trackEvents.Any(e => e.Offset == offset);
	}
	private static void EmulateNote(List<SongEvent> trackEvents, long offset, byte key, byte velocity, byte addedDuration,
		ref byte runCmd, ref byte prevKey, ref byte prevVelocity)
	{
		prevKey = key;
		prevVelocity = velocity;
		if (!EventExists(trackEvents, offset))
		{
			AddEvent(trackEvents, offset, new NoteCommand
			{
				Note = key,
				Velocity = velocity,
				Duration = runCmd == 0xCF ? -1 : (Utils.RestTable[runCmd - 0xCF] + addedDuration),
			});
		}
	}
	private void AddEvents(long startOffset, MP2KConfig cfg, byte trackIndex,
		ref byte runCmd, ref byte prevKey, ref byte prevVelocity, ref int callStackDepth)
	{
		cfg.Reader.Stream.Position = startOffset;
		List<SongEvent> trackEvents = Events[trackIndex];

		Span<byte> peek = stackalloc byte[3];
		bool cont = true;
		while (cont)
		{
			long offset = cfg.Reader.Stream.Position;

			byte cmd = cfg.Reader.ReadByte();
			if (cmd >= 0xBD) // Commands that work within running status
			{
				runCmd = cmd;
			}

			#region TIE & Notes

			if (runCmd >= 0xCF && cmd <= 0x7F) // Within running status
			{
				byte velocity, addedDuration;
				cfg.Reader.PeekBytes(peek.Slice(0, 2));
				if (peek[0] > 0x7F)
				{
					velocity = prevVelocity;
					addedDuration = 0;
				}
				else if (peek[1] > 3)
				{
					velocity = cfg.Reader.ReadByte();
					addedDuration = 0;
				}
				else
				{
					velocity = cfg.Reader.ReadByte();
					addedDuration = cfg.Reader.ReadByte();
				}
				EmulateNote(trackEvents, offset, cmd, velocity, addedDuration, ref runCmd, ref prevKey, ref prevVelocity);
			}
			else if (cmd >= 0xCF)
			{
				byte key, velocity, addedDuration;
				cfg.Reader.PeekBytes(peek);
				if (peek[0] > 0x7F)
				{
					key = prevKey;
					velocity = prevVelocity;
					addedDuration = 0;
				}
				else if (peek[1] > 0x7F)
				{
					key = cfg.Reader.ReadByte();
					velocity = prevVelocity;
					addedDuration = 0;
				}
				// TIE (0xCF) cannot have an added duration so it needs to stop here
				else if (cmd == 0xCF || peek[2] > 3)
				{
					key = cfg.Reader.ReadByte();
					velocity = cfg.Reader.ReadByte();
					addedDuration = 0;
				}
				else
				{
					key = cfg.Reader.ReadByte();
					velocity = cfg.Reader.ReadByte();
					addedDuration = cfg.Reader.ReadByte();
				}
				EmulateNote(trackEvents, offset, key, velocity, addedDuration, ref runCmd, ref prevKey, ref prevVelocity);
			}

			#endregion

			#region Rests

			else if (cmd >= 0x80 && cmd <= 0xB0)
			{
				if (!EventExists(trackEvents, offset))
				{
					AddEvent(trackEvents, offset, new RestCommand { Rest = Utils.RestTable[cmd - 0x80] });
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
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new VoiceCommand { Voice = cmd });
						}
						break;
					}
					case 0xBE:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new VolumeCommand { Volume = cmd });
						}
						break;
					}
					case 0xBF:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PanpotCommand { Panpot = (sbyte)(cmd - 0x40) });
						}
						break;
					}
					case 0xC0:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PitchBendCommand { Bend = (sbyte)(cmd - 0x40) });
						}
						break;
					}
					case 0xC1:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PitchBendRangeCommand { Range = cmd });
						}
						break;
					}
					case 0xC2:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFOSpeedCommand { Speed = cmd });
						}
						break;
					}
					case 0xC3:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFODelayCommand { Delay = cmd });
						}
						break;
					}
					case 0xC4:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFODepthCommand { Depth = cmd });
						}
						break;
					}
					case 0xC5:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFOTypeCommand { Type = (LFOType)cmd });
						}
						break;
					}
					case 0xC8:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new TuneCommand { Tune = (sbyte)(cmd - 0x40) });
						}
						break;
					}
					case 0xCD:
					{
						byte arg = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LibraryCommand { Command = cmd, Argument = arg });
						}
						break;
					}
					case 0xCE:
					{
						prevKey = cmd;
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new EndOfTieCommand { Note = cmd });
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
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new FinishCommand { Prev = cmd == 0xB6 });
						}
						cont = false;
						break;
					}
					case 0xB2:
					{
						int jumpOffset = cfg.Reader.ReadInt32() - GBA.GBAUtils.CartridgeOffset;
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new JumpCommand { Offset = jumpOffset });
							if (!EventExists(trackEvents, jumpOffset))
							{
								AddEvents(jumpOffset, cfg, trackIndex, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
							}
						}
						cont = false;
						break;
					}
					case 0xB3:
					{
						int callOffset = cfg.Reader.ReadInt32() - GBA.GBAUtils.CartridgeOffset;
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new CallCommand { Offset = callOffset });
						}
						if (callStackDepth < 3)
						{
							long backup = cfg.Reader.Stream.Position;
							callStackDepth++;
							AddEvents(callOffset, cfg, trackIndex, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
							cfg.Reader.Stream.Position = backup;
						}
						else
						{
							throw new MP2KTooManyNestedCallsException(trackIndex);
						}
						break;
					}
					case 0xB4:
					{
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new ReturnCommand());
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
						byte op = cfg.Reader.ReadByte();
						byte address = cfg.Reader.ReadByte();
						byte data = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new MemoryAccessCommand { Operator = op, Address = address, Data = data });
						}
						break;
					}
					case 0xBA:
					{
						byte priority = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PriorityCommand { Priority = priority });
						}
						break;
					}
					case 0xBB:
					{
						byte tempoArg = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new TempoCommand { Tempo = (ushort)(tempoArg * 2) });
						}
						break;
					}
					case 0xBC:
					{
						sbyte transpose = cfg.Reader.ReadSByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new TransposeCommand { Transpose = transpose });
						}
						break;
					}
					// Commands that work within running status:
					case 0xBD:
					{
						byte voice = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new VoiceCommand { Voice = voice });
						}
						break;
					}
					case 0xBE:
					{
						byte volume = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new VolumeCommand { Volume = volume });
						}
						break;
					}
					case 0xBF:
					{
						byte panArg = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
						}
						break;
					}
					case 0xC0:
					{
						byte bendArg = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PitchBendCommand { Bend = (sbyte)(bendArg - 0x40) });
						}
						break;
					}
					case 0xC1:
					{
						byte range = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new PitchBendRangeCommand { Range = range });
						}
						break;
					}
					case 0xC2:
					{
						byte speed = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFOSpeedCommand { Speed = speed });
						}
						break;
					}
					case 0xC3:
					{
						byte delay = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFODelayCommand { Delay = delay });
						}
						break;
					}
					case 0xC4:
					{
						byte depth = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFODepthCommand { Depth = depth });
						}
						break;
					}
					case 0xC5:
					{
						byte type = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LFOTypeCommand { Type = (LFOType)type });
						}
						break;
					}
					case 0xC8:
					{
						byte tuneArg = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new TuneCommand { Tune = (sbyte)(tuneArg - 0x40) });
						}
						break;
					}
					case 0xCD:
					{
						byte command = cfg.Reader.ReadByte();
						byte arg = cfg.Reader.ReadByte();
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new LibraryCommand { Command = command, Argument = arg });
						}
						break;
					}
					case 0xCE:
					{
						int key = cfg.Reader.PeekByte() <= 0x7F ? (prevKey = cfg.Reader.ReadByte()) : -1;
						if (!EventExists(trackEvents, offset))
						{
							AddEvent(trackEvents, offset, new EndOfTieCommand { Note = key });
						}
						break;
					}
					default: throw new MP2KInvalidCMDException(trackIndex, (int)offset, cmd);
				}
			}

			#endregion
		}
	}

	internal void SetTicks()
	{
		MaxTicks = 0;
		bool u = false;
		for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
		{
			Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
			List<SongEvent> evs = Events[trackIndex];
			Track track = Tracks[trackIndex];
			track.Init();
			ElapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.DataOffset);
				if (track.CallStackDepth == 0 && e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(ElapsedTicks);
				_player.ExecuteNext(track, ref u);
				if (track.Stopped)
				{
					break;
				}

				ElapsedTicks += track.Rest;
				track.Rest = 0;
			}
			if (ElapsedTicks > MaxTicks)
			{
				LongestTrack = trackIndex;
				MaxTicks = ElapsedTicks;
			}
			track.StopAllChannels();
		}
	}

	public void Dispose()
	{
		for (int i = 0; i < Tracks.Length; i++)
		{
			Tracks[i].StopAllChannels();
		}
	}
}
