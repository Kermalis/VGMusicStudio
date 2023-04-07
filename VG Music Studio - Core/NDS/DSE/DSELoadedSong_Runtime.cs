using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong
{
	public void UpdateSongState(SongState info)
	{
		for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
		{
			Tracks[trackIndex].UpdateSongState(info.Tracks[trackIndex]);
		}
	}

	public void ExecuteNext(DSETrack track)
	{
		byte cmd = SMDFile[track.CurOffset++];
		if (cmd <= 0x7F)
		{
			byte arg = SMDFile[track.CurOffset++];
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
					duration = (duration << 8) | SMDFile[track.CurOffset++];
				}
				track.LastNoteDuration = duration;
			}
			DSEChannel? channel = _player.DMixer.AllocateChannel();
			if (channel is null)
			{
				throw new Exception("Not enough channels");
			}

			channel.Stop();
			track.Octave = (byte)(track.Octave + oct);
			if (channel.StartPCM(LocalSWD, _player.MasterSWD, track.Voice, n + (12 * track.Octave), duration))
			{
				channel.NoteVelocity = cmd;
				channel.Owner = track;
				track.Channels.Add(channel);
			}
		}
		else if (cmd is >= 0x80 and <= 0x8F)
		{
			track.LastRest = DSEUtils.FixedRests[cmd - 0x80];
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
					track.LastRest = (uint)(track.LastRest + (sbyte)SMDFile[track.CurOffset++]);
					track.Rest = track.LastRest;
					break;
				}
				case 0x92:
				{
					track.LastRest = SMDFile[track.CurOffset++];
					track.Rest = track.LastRest;
					break;
				}
				case 0x93:
				{
					track.LastRest = (uint)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
					track.Rest = track.LastRest;
					break;
				}
				case 0x94:
				{
					track.LastRest = (uint)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8) | (SMDFile[track.CurOffset++] << 16));
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
					track.Octave = SMDFile[track.CurOffset++];
					break;
				}
				case 0xA1:
				{
					track.Octave = (byte)(track.Octave + (sbyte)SMDFile[track.CurOffset++]);
					break;
				}
				case 0xA4:
				case 0xA5:
				{
					_player.Tempo = SMDFile[track.CurOffset++];
					break;
				}
				case 0xAB:
				{
					track.CurOffset++;
					break;
				}
				case 0xAC:
				{
					track.Voice = SMDFile[track.CurOffset++];
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
					track.PitchBend = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
					break;
				}
				case 0xE0:
				{
					track.Volume = SMDFile[track.CurOffset++];
					break;
				}
				case 0xE3:
				{
					track.Expression = SMDFile[track.CurOffset++];
					break;
				}
				case 0xE8:
				{
					track.Panpot = (sbyte)(SMDFile[track.CurOffset++] - 0x40);
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
}
