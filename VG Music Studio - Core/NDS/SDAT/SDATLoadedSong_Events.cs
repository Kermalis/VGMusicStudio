using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal sealed partial class SDATLoadedSong
{
	private void AddEvent<T>(byte trackIndex, long cmdOffset, T command, ArgType argOverrideType)
		where T : SDATCommand, ICommand
	{
		command.RandMod = argOverrideType == ArgType.Rand;
		command.VarMod = argOverrideType == ArgType.PlayerVar;
		Events[trackIndex]!.Add(new SongEvent(cmdOffset, command));
	}
	private bool EventExists(byte trackIndex, long cmdOffset)
	{
		return Events[trackIndex]!.Exists(e => e.Offset == cmdOffset);
	}

	private int ReadArg(ref int dataOffset, ArgType type)
	{
		switch (type)
		{
			case ArgType.Byte:
			{
				return _sseq.Data[dataOffset++];
			}
			case ArgType.Short:
			{
				short s = ReadInt16LittleEndian(_sseq.Data.AsSpan(dataOffset));
				dataOffset += 2;
				return s;
			}
			case ArgType.VarLen:
			{
				int numRead = 0;
				int value = 0;
				byte b;
				do
				{
					b = _sseq.Data[dataOffset++];
					value = (value << 7) | (b & 0x7F);
					numRead++;
				}
				while (numRead < 4 && (b & 0x80) != 0);
				return value;
			}
			case ArgType.Rand:
			{
				// Combine min and max into one int
				int minMax = ReadInt32LittleEndian(_sseq.Data.AsSpan(dataOffset));
				dataOffset += 4;
				return minMax;
			}
			case ArgType.PlayerVar:
			{
				return _sseq.Data[dataOffset++]; // Return var index
			}
			default: throw new Exception();
		}
	}

	private void AddTrackEvents(byte trackIndex, int trackStartOffset)
	{
		ref List<SongEvent>? trackEvents = ref Events[trackIndex];
		trackEvents ??= new List<SongEvent>();

		int callStackDepth = 0;
		AddEvents(trackIndex, trackStartOffset, ref callStackDepth);
	}
	private void AddEvents(byte trackIndex, int startOffset, ref int callStackDepth)
	{
		int dataOffset = startOffset;
		bool cont = true;
		while (cont)
		{
			bool @if = false;
			int cmdOffset = dataOffset;
			ArgType argOverrideType = ArgType.None;
		again:
			byte cmd = _sseq.Data[dataOffset++];

			if (cmd <= 0x7F)
			{
				HandleNoteEvent(trackIndex, ref dataOffset, cmdOffset, cmd, argOverrideType);
			}
			else
			{
				switch (cmd & 0xF0)
				{
					case 0x80: HandleCmdGroup0x80(trackIndex, ref dataOffset, cmdOffset, cmd, argOverrideType); break;
					case 0x90: HandleCmdGroup0x90(trackIndex, ref dataOffset, ref callStackDepth, cmdOffset, cmd, argOverrideType, ref @if, ref cont); break;
					case 0xA0:
					{
						if (HandleCmdGroup0xA0(trackIndex, ref cmdOffset, cmd, ref argOverrideType, ref @if))
						{
							goto again;
						}
						break;
					}
					case 0xB0: HandleCmdGroup0xB0(trackIndex, ref dataOffset, cmdOffset, cmd, argOverrideType); break;
					case 0xC0: HandleCmdGroup0xC0(trackIndex, ref dataOffset, cmdOffset, cmd, argOverrideType); break;
					case 0xD0: HandleCmdGroup0xD0(trackIndex, ref dataOffset, cmdOffset, cmd, argOverrideType); break;
					case 0xE0: HandleCmdGroup0xE0(trackIndex, ref dataOffset, cmdOffset, cmd, argOverrideType); break;
					default: HandleCmdGroup0xF0(trackIndex, ref dataOffset, ref callStackDepth, cmdOffset, cmd, argOverrideType, ref @if, ref cont); break;
				}
			}
		}
	}

	private void HandleNoteEvent(byte trackIndex, ref int dataOffset, int cmdOffset, byte cmd, ArgType argOverrideType)
	{
		byte velocity = _sseq.Data[dataOffset++];
		int duration = ReadArg(ref dataOffset, argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
		if (!EventExists(trackIndex, cmdOffset))
		{
			AddEvent(trackIndex, cmdOffset, new NoteComand { Note = cmd, Velocity = velocity, Duration = duration }, argOverrideType);
		}
	}
	private void HandleCmdGroup0x80(byte trackIndex, ref int dataOffset, int cmdOffset, byte cmd, ArgType argOverrideType)
	{
		int arg = ReadArg(ref dataOffset, argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
		switch (cmd)
		{
			case 0x80:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = arg }, argOverrideType);
				}
				break;
			}
			case 0x81: // RAND PROGRAM: [BW2 (2249)]
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VoiceCommand { Voice = arg }, argOverrideType); // TODO: Bank change
				}
				break;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}
	private void HandleCmdGroup0x90(byte trackIndex, ref int dataOffset, ref int callStackDepth, int cmdOffset, byte cmd, ArgType argOverrideType, ref bool @if, ref bool cont)
	{
		switch (cmd)
		{
			case 0x93:
			{
				byte openTrackIndex = _sseq.Data[dataOffset++];
				int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new OpenTrackCommand { Track = openTrackIndex, Offset = offset24bit }, argOverrideType);
					AddTrackEvents(openTrackIndex, offset24bit);
				}
				break;
			}
			case 0x94:
			{
				int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new JumpCommand { Offset = offset24bit }, argOverrideType);
					if (!EventExists(trackIndex, offset24bit))
					{
						AddEvents(trackIndex, offset24bit, ref callStackDepth);
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
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new CallCommand { Offset = offset24bit }, argOverrideType);
				}
				if (callStackDepth < 3)
				{
					if (!EventExists(trackIndex, offset24bit))
					{
						callStackDepth++;
						AddEvents(trackIndex, offset24bit, ref callStackDepth);
					}
				}
				else
				{
					throw new SDATTooManyNestedCallsException(trackIndex);
				}
				break;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}
	private bool HandleCmdGroup0xA0(byte trackIndex, ref int cmdOffset, byte cmd, ref ArgType argOverrideType, ref bool @if)
	{
		switch (cmd)
		{
			case 0xA0: // [New Super Mario Bros (BGM_AMB_CHIKA)] [BW2 (1917, 1918)]
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ModRandCommand(), argOverrideType);
				}
				argOverrideType = ArgType.Rand;
				cmdOffset++;
				return true;
			}
			case 0xA1: // [New Super Mario Bros (BGM_AMB_SABAKU)]
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ModVarCommand(), argOverrideType);
				}
				argOverrideType = ArgType.PlayerVar;
				cmdOffset++;
				return true;
			}
			case 0xA2: // [Mario Kart DS (75)] [BW2 (1917, 1918)]
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ModIfCommand(), argOverrideType);
				}
				@if = true;
				cmdOffset++;
				return true;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}
	private void HandleCmdGroup0xB0(byte trackIndex, ref int dataOffset, int cmdOffset, byte cmd, ArgType argOverrideType)
	{
		byte varIndex = _sseq.Data[dataOffset++];
		int arg = ReadArg(ref dataOffset, argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
		switch (cmd)
		{
			case 0xB0:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarSetCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB1:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarAddCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB2:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarSubCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB3:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarMulCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB4:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarDivCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB5:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarShiftCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB6: // [Mario Kart DS (75)]
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarRandCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB8:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarCmpEECommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xB9:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarCmpGECommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xBA:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarCmpGGCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xBB:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarCmpLECommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xBC:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarCmpLLCommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			case 0xBD:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarCmpNECommand { Variable = varIndex, Argument = arg }, argOverrideType);
				}
				break;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}
	private void HandleCmdGroup0xC0(byte trackIndex, ref int dataOffset, int cmdOffset, byte cmd, ArgType argOverrideType)
	{
		int arg = ReadArg(ref dataOffset, argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType);
		switch (cmd)
		{
			case 0xC0:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PanpotCommand { Panpot = arg }, argOverrideType);
				}
				break;
			}
			case 0xC1:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new TrackVolumeCommand { Volume = arg }, argOverrideType);
				}
				break;
			}
			case 0xC2:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PlayerVolumeCommand { Volume = arg }, argOverrideType);
				}
				break;
			}
			case 0xC3:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new TransposeCommand { Transpose = arg }, argOverrideType);
				}
				break;
			}
			case 0xC4:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PitchBendCommand { Bend = arg }, argOverrideType);
				}
				break;
			}
			case 0xC5:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PitchBendRangeCommand { Range = arg }, argOverrideType);
				}
				break;
			}
			case 0xC6:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PriorityCommand { Priority = arg }, argOverrideType);
				}
				break;
			}
			case 0xC7:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new MonophonyCommand { Mono = arg }, argOverrideType);
				}
				break;
			}
			case 0xC8:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new TieCommand { Tie = arg }, argOverrideType);
				}
				break;
			}
			case 0xC9:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PortamentoControlCommand { Portamento = arg }, argOverrideType);
				}
				break;
			}
			case 0xCA:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LFODepthCommand { Depth = arg }, argOverrideType);
				}
				break;
			}
			case 0xCB:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LFOSpeedCommand { Speed = arg }, argOverrideType);
				}
				break;
			}
			case 0xCC:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LFOTypeCommand { Type = arg }, argOverrideType);
				}
				break;
			}
			case 0xCD:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LFORangeCommand { Range = arg }, argOverrideType);
				}
				break;
			}
			case 0xCE:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PortamentoToggleCommand { Portamento = arg }, argOverrideType);
				}
				break;
			}
			case 0xCF:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new PortamentoTimeCommand { Time = arg }, argOverrideType);
				}
				break;
			}
		}
	}
	private void HandleCmdGroup0xD0(byte trackIndex, ref int dataOffset, int cmdOffset, byte cmd, ArgType argOverrideType)
	{
		int arg = ReadArg(ref dataOffset, argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType);
		switch (cmd)
		{
			case 0xD0:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ForceAttackCommand { Attack = arg }, argOverrideType);
				}
				break;
			}
			case 0xD1:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ForceDecayCommand { Decay = arg }, argOverrideType);
				}
				break;
			}
			case 0xD2:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ForceSustainCommand { Sustain = arg }, argOverrideType);
				}
				break;
			}
			case 0xD3:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ForceReleaseCommand { Release = arg }, argOverrideType);
				}
				break;
			}
			case 0xD4:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LoopStartCommand { NumLoops = arg }, argOverrideType);
				}
				break;
			}
			case 0xD5:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new TrackExpressionCommand { Expression = arg }, argOverrideType);
				}
				break;
			}
			case 0xD6:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new VarPrintCommand { Variable = arg }, argOverrideType);
				}
				break;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}
	private void HandleCmdGroup0xE0(byte trackIndex, ref int dataOffset, int cmdOffset, byte cmd, ArgType argOverrideType)
	{
		int arg = ReadArg(ref dataOffset, argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
		switch (cmd)
		{
			case 0xE0:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LFODelayCommand { Delay = arg }, argOverrideType);
				}
				break;
			}
			case 0xE1:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new TempoCommand { Tempo = arg }, argOverrideType);
				}
				break;
			}
			case 0xE3:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new SweepPitchCommand { Pitch = arg }, argOverrideType);
				}
				break;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}
	private void HandleCmdGroup0xF0(byte trackIndex, ref int dataOffset, ref int callStackDepth, int cmdOffset, byte cmd, ArgType argOverrideType, ref bool @if, ref bool cont)
	{
		switch (cmd)
		{
			case 0xFC: // [HGSS(1353)]
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new LoopEndCommand(), argOverrideType);
				}
				break;
			}
			case 0xFD:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new ReturnCommand(), argOverrideType);
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
				ushort bits = (ushort)ReadArg(ref dataOffset, ArgType.Short);
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new AllocTracksCommand { Tracks = bits }, argOverrideType);
				}
				break;
			}
			case 0xFF:
			{
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new FinishCommand(), argOverrideType);
				}
				if (!@if)
				{
					cont = false;
				}
				break;
			}
			default: throw Invalid(trackIndex, cmdOffset, cmd);
		}
	}

	public void SetTicks()
	{
		// TODO: (NSMB 81) (Spirit Tracks 18) does not count all ticks because the songs keep jumping backwards while changing vars and then using ModIfCommand to change events
		// Should evaluate all branches if possible
		MaxTicks = 0;
		for (int i = 0; i < 0x10; i++)
		{
			ref List<SongEvent>? evs = ref Events[i];
			evs?.Sort((e1, e2) => e1.Offset.CompareTo(e2.Offset));
		}
		_player.InitEmulation();

		bool[] done = new bool[0x10]; // We use this instead of track.Stopped just to be certain that emulating Monophony works as intended
		while (Array.Exists(_player.Tracks, t => t.Allocated && t.Enabled && !done[t.Index]))
		{
			while (_player.TempoStack >= 240)
			{
				_player.TempoStack -= 240;
				for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
				{
					SDATTrack track = _player.Tracks[trackIndex];
					List<SongEvent> evs = Events[trackIndex]!;
					if (!track.Enabled || track.Stopped)
					{
						continue;
					}

					track.Tick();
					while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
					{
						SongEvent e = evs.Single(ev => ev.Offset == track.DataOffset);
						ExecuteNext(track);
						if (done[trackIndex])
						{
							continue;
						}

						e.Ticks.Add(_player.ElapsedTicks);
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
							if (_player.ElapsedTicks > MaxTicks)
							{
								LongestTrack = trackIndex;
								MaxTicks = _player.ElapsedTicks;
							}
						}
					}
				}
				_player.ElapsedTicks++;
			}
			_player.TempoStack += _player.Tempo;
			_player.SMixer.ChannelTick();
			_player.SMixer.EmulateProcess();
		}
		for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
		{
			_player.Tracks[trackIndex].StopAllChannels();
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
				for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
				{
					SDATTrack track = _player.Tracks[trackIndex];
					if (track.Enabled && !track.Stopped)
					{
						track.Tick();
						while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
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
			_player.SMixer.ChannelTick();
			_player.SMixer.EmulateProcess();
		}
	finish:
		for (int i = 0; i < 0x10; i++)
		{
			_player.Tracks[i].StopAllChannels();
		}
	}
}
