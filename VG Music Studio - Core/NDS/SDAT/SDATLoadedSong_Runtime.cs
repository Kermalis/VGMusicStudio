using System;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal sealed partial class SDATLoadedSong
{
	public void InitEmulation()
	{
		_player.Volume = SEQInfo.Volume;
		_rand = new Random(_randSeed);
	}

	public void UpdateInstrumentCache(byte voice, out string str)
	{
		if (_sbnk.NumInstruments <= voice)
		{
			str = "Empty";
		}
		else
		{
			InstrumentType t = _sbnk.Instruments[voice].Type;
			switch (t)
			{
				case InstrumentType.PCM: str = "PCM"; break;
				case InstrumentType.PSG: str = "PSG"; break;
				case InstrumentType.Noise: str = "Noise"; break;
				case InstrumentType.Drum: str = "Drum"; break;
				case InstrumentType.KeySplit: str = "Key Split"; break;
				default: str = "Invalid " + (byte)t; break;
			}
		}
	}

	private int ReadArg(SDATTrack track, ArgType type)
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
				return _rand!.Next(min, max + 1);
			}
			case ArgType.PlayerVar:
			{
				byte varIndex = _sseq.Data[track.DataOffset++];
				return _player.Vars[varIndex];
			}
			default: throw new Exception();
		}
	}
	private void TryStartChannel(SBNK.InstrumentData inst, SDATTrack track, byte note, byte velocity, int duration, out SDATChannel? channel)
	{
		InstrumentType type = inst.Type;
		channel = _player.SMixer.AllocateChannel(type, track);
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
	private void PlayNote(SDATTrack track, byte note, byte velocity, int duration)
	{
		SDATChannel? channel = null;
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
			channel.SweepPitch += (short)((track.PortamentoNote - note) << 6); // "<< 6" is "* 0x40"
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

	internal void ExecuteNext(SDATTrack track)
	{
		bool resetOverride = true;
		bool resetCmdWork = true;
		byte cmd = _sseq.Data[track.DataOffset++];
		if (cmd < 0x80)
		{
			ExecuteNoteEvent(track, cmd);
		}
		else
		{
			switch (cmd & 0xF0)
			{
				case 0x80: ExecuteCmdGroup0x80(track, cmd); break;
				case 0x90: ExecuteCmdGroup0x90(track, cmd); break;
				case 0xA0: ExecuteCmdGroup0xA0(track, cmd, ref resetOverride, ref resetCmdWork); break;
				case 0xB0: ExecuteCmdGroup0xB0(track, cmd); break;
				case 0xC0: ExecuteCmdGroup0xC0(track, cmd); break;
				case 0xD0: ExecuteCmdGroup0xD0(track, cmd); break;
				case 0xE0: ExecuteCmdGroup0xE0(track, cmd); break;
				default: ExecuteCmdGroup0xF0(track, cmd); break;
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

	private void ExecuteNoteEvent(SDATTrack track, byte cmd)
	{
		byte velocity = _sseq.Data[track.DataOffset++];
		int duration = ReadArg(track, ArgType.VarLen);
		if (!track.DoCommandWork)
		{
			return;
		}

		int n = cmd + track.Transpose;
		if (n < 0)
		{
			n = 0;
		}
		else if (n > 0x7F)
		{
			n = 0x7F;
		}
		byte note = (byte)n;
		PlayNote(track, note, velocity, duration);
		track.PortamentoNote = note;
		if (track.Mono)
		{
			track.Rest = duration;
			if (duration == 0)
			{
				track.WaitingForNoteToFinishBeforeContinuingXD = true;
			}
		}
	}
	private void ExecuteCmdGroup0x80(SDATTrack track, byte cmd)
	{
		int arg = ReadArg(track, ArgType.VarLen);

		switch (cmd)
		{
			case 0x80: // Rest
			{
				if (track.DoCommandWork)
				{
					track.Rest = arg;
				}
				break;
			}
			case 0x81: // Program Change
			{
				if (track.DoCommandWork && arg <= byte.MaxValue)
				{
					track.Voice = (byte)arg;
				}
				break;
			}
			throw Invalid(track.Index, track.DataOffset - 1, cmd);
		}
	}
	private void ExecuteCmdGroup0x90(SDATTrack track, byte cmd)
	{
		switch (cmd)
		{
			case 0x93: // Open Track
			{
				int index = _sseq.Data[track.DataOffset++];
				int offset24bit = _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8) | (_sseq.Data[track.DataOffset++] << 16);
				if (track.DoCommandWork && track.Index == 0)
				{
					SDATTrack other = _player.Tracks[index];
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
			default: throw Invalid(track.Index, track.DataOffset - 1, cmd);
		}
	}
	private static void ExecuteCmdGroup0xA0(SDATTrack track, byte cmd, ref bool resetOverride, ref bool resetCmdWork)
	{
		switch (cmd)
		{
			case 0xA0: // Rand Mod
			{
				if (track.DoCommandWork)
				{
					track.ArgOverrideType = ArgType.Rand;
					resetOverride = false;
				}
				break;
			}
			case 0xA1: // Var Mod
			{
				if (track.DoCommandWork)
				{
					track.ArgOverrideType = ArgType.PlayerVar;
					resetOverride = false;
				}
				break;
			}
			case 0xA2: // If Mod
			{
				if (track.DoCommandWork)
				{
					track.DoCommandWork = track.VariableFlag;
					resetCmdWork = false;
				}
				break;
			}
			default: throw Invalid(track.Index, track.DataOffset - 1, cmd);
		}
	}
	private void ExecuteCmdGroup0xB0(SDATTrack track, byte cmd)
	{
		byte varIndex = _sseq.Data[track.DataOffset++];
		short mathArg = (short)ReadArg(track, ArgType.Short);
		switch (cmd)
		{
			case 0xB0: // VarSet
			{
				if (track.DoCommandWork)
				{
					_player.Vars[varIndex] = mathArg;
				}
				break;
			}
			case 0xB1: // VarAdd
			{
				if (track.DoCommandWork)
				{
					_player.Vars[varIndex] += mathArg;
				}
				break;
			}
			case 0xB2: // VarSub
			{
				if (track.DoCommandWork)
				{
					_player.Vars[varIndex] -= mathArg;
				}
				break;
			}
			case 0xB3: // VarMul
			{
				if (track.DoCommandWork)
				{
					_player.Vars[varIndex] *= mathArg;
				}
				break;
			}
			case 0xB4: // VarDiv
			{
				if (track.DoCommandWork && mathArg != 0)
				{
					_player.Vars[varIndex] /= mathArg;
				}
				break;
			}
			case 0xB5: // VarShift
			{
				if (track.DoCommandWork)
				{
					ref short v = ref _player.Vars[varIndex];
					v = mathArg < 0 ? (short)(v >> -mathArg) : (short)(v << mathArg);
				}
				break;
			}
			case 0xB6: // VarRand
			{
				if (track.DoCommandWork)
				{
					bool negate = false;
					if (mathArg < 0)
					{
						negate = true;
						mathArg = (short)-mathArg;
					}
					short val = (short)_rand!.Next(mathArg + 1);
					if (negate)
					{
						val = (short)-val;
					}
					_player.Vars[varIndex] = val;
				}
				break;
			}
			case 0xB8: // VarCmpEE
			{
				if (track.DoCommandWork)
				{
					track.VariableFlag = _player.Vars[varIndex] == mathArg;
				}
				break;
			}
			case 0xB9: // VarCmpGE
			{
				if (track.DoCommandWork)
				{
					track.VariableFlag = _player.Vars[varIndex] >= mathArg;
				}
				break;
			}
			case 0xBA: // VarCmpGG
			{
				if (track.DoCommandWork)
				{
					track.VariableFlag = _player.Vars[varIndex] > mathArg;
				}
				break;
			}
			case 0xBB: // VarCmpLE
			{
				if (track.DoCommandWork)
				{
					track.VariableFlag = _player.Vars[varIndex] <= mathArg;
				}
				break;
			}
			case 0xBC: // VarCmpLL
			{
				if (track.DoCommandWork)
				{
					track.VariableFlag = _player.Vars[varIndex] < mathArg;
				}
				break;
			}
			case 0xBD: // VarCmpNE
			{
				if (track.DoCommandWork)
				{
					track.VariableFlag = _player.Vars[varIndex] != mathArg;
				}
				break;
			}
			default: throw Invalid(track.Index, track.DataOffset - 1, cmd);
		}
	}
	private void ExecuteCmdGroup0xC0(SDATTrack track, byte cmd)
	{
		int cmdArg = ReadArg(track, ArgType.Byte);
		switch (cmd)
		{
			case 0xC0: // Panpot
			{
				if (track.DoCommandWork)
				{
					track.Panpot = (sbyte)(cmdArg - 0x40);
				}
				break;
			}
			case 0xC1: // Track Volume
			{
				if (track.DoCommandWork)
				{
					track.Volume = (byte)cmdArg;
				}
				break;
			}
			case 0xC2: // Player Volume
			{
				if (track.DoCommandWork)
				{
					_player.Volume = (byte)cmdArg;
				}
				break;
			}
			case 0xC3: // Transpose
			{
				if (track.DoCommandWork)
				{
					track.Transpose = (sbyte)cmdArg;
				}
				break;
			}
			case 0xC4: // Pitch Bend
			{
				if (track.DoCommandWork)
				{
					track.PitchBend = (sbyte)cmdArg;
				}
				break;
			}
			case 0xC5: // Pitch Bend Range
			{
				if (track.DoCommandWork)
				{
					track.PitchBendRange = (byte)cmdArg;
				}
				break;
			}
			case 0xC6: // Priority
			{
				if (track.DoCommandWork)
				{
					track.Priority = (byte)(_player.Priority + (byte)cmdArg);
				}
				break;
			}
			case 0xC7: // Mono
			{
				if (track.DoCommandWork)
				{
					track.Mono = cmdArg == 1;
				}
				break;
			}
			case 0xC8: // Tie
			{
				if (track.DoCommandWork)
				{
					track.Tie = cmdArg == 1;
					track.StopAllChannels();
				}
				break;
			}
			case 0xC9: // Portamento Control
			{
				if (track.DoCommandWork)
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
					track.PortamentoNote = (byte)k;
					track.Portamento = true;
				}
				break;
			}
			case 0xCA: // LFO Depth
			{
				if (track.DoCommandWork)
				{
					track.LFODepth = (byte)cmdArg;
				}
				break;
			}
			case 0xCB: // LFO Speed
			{
				if (track.DoCommandWork)
				{
					track.LFOSpeed = (byte)cmdArg;
				}
				break;
			}
			case 0xCC: // LFO Type
			{
				if (track.DoCommandWork)
				{
					track.LFOType = (LFOType)cmdArg;
				}
				break;
			}
			case 0xCD: // LFO Range
			{
				if (track.DoCommandWork)
				{
					track.LFORange = (byte)cmdArg;
				}
				break;
			}
			case 0xCE: // Portamento Toggle
			{
				if (track.DoCommandWork)
				{
					track.Portamento = cmdArg == 1;
				}
				break;
			}
			case 0xCF: // Portamento Time
			{
				if (track.DoCommandWork)
				{
					track.PortamentoTime = (byte)cmdArg;
				}
				break;
			}
		}
	}
	private void ExecuteCmdGroup0xD0(SDATTrack track, byte cmd)
	{
		int cmdArg = ReadArg(track, ArgType.Byte);
		switch (cmd)
		{
			case 0xD0: // Forced Attack
			{
				if (track.DoCommandWork)
				{
					track.Attack = (byte)cmdArg;
				}
				break;
			}
			case 0xD1: // Forced Decay
			{
				if (track.DoCommandWork)
				{
					track.Decay = (byte)cmdArg;
				}
				break;
			}
			case 0xD2: // Forced Sustain
			{
				if (track.DoCommandWork)
				{
					track.Sustain = (byte)cmdArg;
				}
				break;
			}
			case 0xD3: // Forced Release
			{
				if (track.DoCommandWork)
				{
					track.Release = (byte)cmdArg;
				}
				break;
			}
			case 0xD4: // Loop Start
			{
				if (track.DoCommandWork && track.CallStackDepth < 3)
				{
					track.CallStack[track.CallStackDepth] = track.DataOffset;
					track.CallStackLoops[track.CallStackDepth] = (byte)cmdArg;
					track.CallStackDepth++;
				}
				break;
			}
			case 0xD5: // Track Expression
			{
				if (track.DoCommandWork)
				{
					track.Expression = (byte)cmdArg;
				}
				break;
			}
			default: throw Invalid(track.Index, track.DataOffset - 1, cmd);
		}
	}
	private void ExecuteCmdGroup0xE0(SDATTrack track, byte cmd)
	{
		int cmdArg = ReadArg(track, ArgType.Short);
		switch (cmd)
		{
			case 0xE0: // LFO Delay
			{
				if (track.DoCommandWork)
				{
					track.LFODelay = (ushort)cmdArg;
				}
				break;
			}
			case 0xE1: // Tempo
			{
				if (track.DoCommandWork)
				{
					_player.Tempo = (ushort)cmdArg;
				}
				break;
			}
			case 0xE3: // Sweep Pitch
			{
				if (track.DoCommandWork)
				{
					track.SweepPitch = (short)cmdArg;
				}
				break;
			}
		}
	}
	private void ExecuteCmdGroup0xF0(SDATTrack track, byte cmd)
	{
		switch (cmd)
		{
			case 0xFC: // Loop End
			{
				if (track.DoCommandWork && track.CallStackDepth != 0)
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
				if (track.DoCommandWork && track.CallStackDepth != 0)
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
				if (track.DoCommandWork && track.Index == 0 && track.DataOffset == 1) // == 1 because we read cmd already
				{
					// Track 1 enabled = bit 1 set, Track 4 enabled = bit 4 set, etc
					int trackBits = _sseq.Data[track.DataOffset++] | (_sseq.Data[track.DataOffset++] << 8);
					for (int i = 0; i < 0x10; i++)
					{
						if ((trackBits & (1 << i)) != 0)
						{
							_player.Tracks[i].Allocated = true;
						}
					}
				}
				break;
			}
			case 0xFF: // Finish
			{
				if (track.DoCommandWork)
				{
					track.Stopped = true;
				}
				break;
			}
			default: throw Invalid(track.Index, track.DataOffset - 1, cmd);
		}
	}
}
