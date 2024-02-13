using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal sealed class SDATTrack
{
	public readonly byte Index;
	private readonly SDATPlayer _player;

	public bool Allocated;
	public bool Enabled;
	public bool Stopped;
	public bool Tie;
	public bool Mono;
	public bool Portamento;
	public bool WaitingForNoteToFinishBeforeContinuingXD; // TODO: Is this necessary?
	public byte Voice;
	public byte Priority;
	public byte Volume;
	public byte Expression;
	public byte PitchBendRange;
	public byte LFORange;
	public byte LFOSpeed;
	public byte LFODepth;
	public ushort LFODelay;
	public ushort LFOPhase;
	public int LFOParam;
	public ushort LFODelayCount;
	public LFOType LFOType;
	public sbyte PitchBend;
	public sbyte Panpot;
	public sbyte Transpose;
	public byte Attack;
	public byte Decay;
	public byte Sustain;
	public byte Release;
	public byte PortamentoNote;
	public byte PortamentoTime;
	public short SweepPitch;
	public int Rest;
	public readonly int[] CallStack;
	public readonly byte[] CallStackLoops;
	public byte CallStackDepth;
	public int DataOffset;
	public bool VariableFlag; // Set by variable commands (0xB0 - 0xBD)
	public bool DoCommandWork;
	public ArgType ArgOverrideType;

	public readonly List<SDATChannel> Channels = new(0x10);

	public SDATTrack(byte i, SDATPlayer player)
	{
		Index = i;
		_player = player;

		CallStack = new int[3];
		CallStackLoops = new byte[3];
	}
	public void Init()
	{
		Stopped = Tie = WaitingForNoteToFinishBeforeContinuingXD = Portamento = false;
		Allocated = Enabled = Index == 0;
		DataOffset = 0;
		ArgOverrideType = ArgType.None;
		Mono = VariableFlag = DoCommandWork = true;
		CallStackDepth = 0;
		Voice = LFODepth = 0;
		PitchBend = Panpot = Transpose = 0;
		LFOPhase = LFODelay = LFODelayCount = 0;
		LFORange = 1;
		LFOSpeed = 0x10;
		Priority = (byte)(_player.Priority + 0x40);
		Volume = Expression = 0x7F;
		Attack = Decay = Sustain = Release = 0xFF;
		PitchBendRange = 2;
		PortamentoNote = 60;
		PortamentoTime = 0;
		SweepPitch = 0;
		LFOType = LFOType.Pitch;
		Rest = 0;
		StopAllChannels();
	}
	public void LFOTick()
	{
		if (Channels.Count == 0)
		{
			LFOPhase = 0;
			LFOParam = 0;
			LFODelayCount = LFODelay;
		}
		else
		{
			if (LFODelayCount > 0)
			{
				LFODelayCount--;
				LFOPhase = 0;
			}
			else
			{
				int param = LFORange * SDATUtils.Sin(LFOPhase >> 8) * LFODepth;
				if (LFOType == LFOType.Volume)
				{
					param = (int)(((long)param * 60) >> 14);
				}
				else
				{
					param >>= 8;
				}
				LFOParam = param;
				int counter = LFOPhase + (LFOSpeed << 6); // "<< 6" is "* 0x40"
				while (counter >= 0x8000)
				{
					counter -= 0x8000;
				}
				LFOPhase = (ushort)counter;
			}
		}
	}
	public void Tick()
	{
		if (Rest > 0)
		{
			Rest--;
		}
		if (Channels.Count != 0)
		{
			// TickNotes:
			for (int i = 0; i < Channels.Count; i++)
			{
				SDATChannel c = Channels[i];
				if (c.NoteDuration > 0)
				{
					c.NoteDuration--;
				}
				if (!c.AutoSweep && c.SweepCounter < c.SweepLength)
				{
					c.SweepCounter++;
				}
			}
		}
		else
		{
			WaitingForNoteToFinishBeforeContinuingXD = false;
		}
	}
	public void UpdateChannels()
	{
		for (int i = 0; i < Channels.Count; i++)
		{
			SDATChannel c = Channels[i];
			c.LFOType = LFOType;
			c.LFOSpeed = LFOSpeed;
			c.LFODepth = LFODepth;
			c.LFORange = LFORange;
			c.LFODelay = LFODelay;
		}
	}

	public void StopAllChannels()
	{
		SDATChannel[] chans = Channels.ToArray();
		for (int i = 0; i < chans.Length; i++)
		{
			chans[i].Stop();
		}
	}

	public int GetPitch()
	{
		//int lfo = LFOType == LFOType.Pitch ? LFOParam : 0;
		int lfo = 0;
		return (PitchBend * PitchBendRange / 2) + lfo;
	}
	public int GetVolume()
	{
		//int lfo = LFOType == LFOType.Volume ? LFOParam : 0;
		int lfo = 0;
		return SDATUtils.SustainTable[_player.Volume] + SDATUtils.SustainTable[Volume] + SDATUtils.SustainTable[Expression] + lfo;
	}
	public sbyte GetPan()
	{
		//int lfo = LFOType == LFOType.Panpot ? LFOParam : 0;
		int lfo = 0;
		int p = Panpot + lfo;
		if (p < -0x40)
		{
			p = -0x40;
		}
		else if (p > 0x3F)
		{
			p = 0x3F;
		}
		return (sbyte)p;
	}

	public void UpdateSongState(SongState.Track tin, SDATLoadedSong loadedSong, string?[] voiceTypeCache)
	{
		tin.Position = DataOffset;
		tin.Rest = Rest;
		tin.Voice = Voice;
		tin.LFO = LFODepth * LFORange;
		ref string? cache = ref voiceTypeCache[Voice];
		if (cache is null)
		{
			loadedSong.UpdateInstrumentCache(Voice, out cache);
		}
		tin.Type = cache;
		tin.Volume = Volume;
		tin.PitchBend = GetPitch();
		tin.Extra = Portamento ? PortamentoTime : (byte)0;
		tin.Panpot = GetPan();

		SDATChannel[] channels = Channels.ToArray();
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
				SDATChannel c = channels[j];
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
