﻿using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong
{
	private void AddEvent(byte trackIndex, long cmdOffset, ICommand command)
	{
		Events[trackIndex].Add(new SongEvent(cmdOffset, command));
	}
	private bool EventExists(byte trackIndex, long cmdOffset)
	{
		return Events[trackIndex].Exists(e => e.Offset == cmdOffset);
	}

	private void AddTrackEvents(byte trackIndex, EndianBinaryReader r)
	{
		Events[trackIndex] = new List<SongEvent>();

		uint lastNoteDuration = 0;
		uint lastRest = 0;
		bool cont = true;
		while (cont)
		{
			long cmdOffset = r.Stream.Position;
			byte cmd = r.ReadByte();
			if (cmd <= 0x7F)
			{
				byte arg = r.ReadByte();
				int numParams = (arg & 0xC0) >> 6;
				int oct = ((arg & 0x30) >> 4) - 2;
				int n = arg & 0xF;
				if (n >= 12)
				{
					throw new DSEInvalidNoteException(trackIndex, (int)cmdOffset, n);
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
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new NoteCommand { Note = (byte)n, OctaveChange = (sbyte)oct, Velocity = cmd, Duration = duration });
				}
			}
			else if (cmd >= 0x80 && cmd <= 0x8F)
			{
				lastRest = DSEUtils.FixedRests[cmd - 0x80];
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x91:
					{
						lastRest = (uint)(lastRest + r.ReadSByte());
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x92:
					{
						lastRest = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x93:
					{
						lastRest = r.ReadUInt16();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x94:
					{
						lastRest = (uint)(r.ReadByte() | (r.ReadByte() << 8) | (r.ReadByte() << 16));
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
						}
						break;
					}
					case 0x98:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new FinishCommand());
						}
						cont = false;
						break;
					}
					case 0x99:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new LoopStartCommand { Offset = r.Stream.Position });
						}
						break;
					}
					case 0xA0:
					{
						byte octave = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new OctaveSetCommand { Octave = octave });
						}
						break;
					}
					case 0xA1:
					{
						sbyte change = r.ReadSByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new OctaveAddCommand { OctaveChange = change });
						}
						break;
					}
					case 0xA4:
					case 0xA5: // The code for these two is identical
					{
						byte tempoArg = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new TempoCommand { Command = cmd, Tempo = tempoArg });
						}
						break;
					}
					case 0xAB:
					{
						byte[] bytes = new byte[1];
						r.ReadBytes(bytes);
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
						}
						break;
					}
					case 0xAC:
					{
						byte voice = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new VoiceCommand { Voice = voice });
						}
						break;
					}
					case 0xCB:
					case 0xF8:
					{
						byte[] bytes = new byte[2];
						r.ReadBytes(bytes);
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
						}
						break;
					}
					case 0xD7:
					{
						ushort bend = r.ReadUInt16();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new PitchBendCommand { Bend = bend });
						}
						break;
					}
					case 0xE0:
					{
						byte volume = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new VolumeCommand { Volume = volume });
						}
						break;
					}
					case 0xE3:
					{
						byte expression = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new ExpressionCommand { Expression = expression });
						}
						break;
					}
					case 0xE8:
					{
						byte panArg = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
						}
						break;
					}
					case 0x9D:
					case 0xB0:
					case 0xC0:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new UnknownCommand { Command = cmd, Args = Array.Empty<byte>() });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new UnknownCommand { Command = cmd, Args = args });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new UnknownCommand { Command = cmd, Args = args });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new UnknownCommand { Command = cmd, Args = args });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new UnknownCommand { Command = cmd, Args = args });
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
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new UnknownCommand { Command = cmd, Args = args });
						}
						break;
					}
					default: throw new DSEInvalidCMDException(trackIndex, (int)cmdOffset, cmd);
				}
			}
		}
	}

	public void SetTicks()
	{
		MaxTicks = 0;
		for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
		{
			List<SongEvent> evs = Events[trackIndex];
			evs.Sort((e1, e2) => e1.Offset.CompareTo(e2.Offset));

			DSETrack track = Tracks[trackIndex];
			track.Init();

			long elapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.CurOffset);
				if (e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(elapsedTicks);
				ExecuteNext(track);
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
			track.StopAllChannels();
		}
	}
	internal void SetCurTick(long ticks)
	{
		while (true)
		{
			if (_player.ElapsedTicks == ticks)
			{
				goto finish;
			}

			while (_player.TempoStack >= 240)
			{
				_player.TempoStack -= 240;
				for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
				{
					DSETrack track = Tracks[trackIndex];
					if (!track.Stopped)
					{
						track.Tick();
						while (track.Rest == 0 && !track.Stopped)
						{
							ExecuteNext(track);
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