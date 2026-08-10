using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class DSETrack(byte i, int startOffset)
{
	public readonly byte Index = i;
	private readonly int _startOffset = startOffset;
	public byte Octave;
	public byte Voice;
	public byte Expression;
	public byte Volume;
	public sbyte Panpot;
	public uint Rest;
	public ushort PitchBend;
	public byte PitchBendRange;
	public byte FineTune;
	public byte FineTuneAdd;
	public byte CoarseTune;
	public ushort CoarseTuneAdd;
	public ushort SweepTuneRate;
	public byte SweepTuneTarget;
	public byte RandomNoteRangeMin;
	public byte RandomNoteRangeMax;
	public ushort DetuneRange;
	public byte NoteVolume;
	public byte BankHi;
	public byte BankLo;
	public bool FlagEnded;
	public byte FlagValue; // Unsure as to what value it's referring to
	public byte ChannelPanpot;
	public byte ChannelVolume;
	public ushort LFORate;
	public ushort LFODepth;
	public WaveformType LFOWaveformType;
	public ushort LFODelay;
	public ushort LFOFadeTime;
    internal ushort LFO1Rate;
    internal ushort LFO1Depth;
    internal WaveformType LFO1WaveformType;
    internal ushort LFO1Delay;
    internal ushort LFO1FadeTime;
    internal ushort LFO2Rate;
    internal ushort LFO2Depth;
    internal WaveformType LFO2WaveformType;
    internal ushort LFO2Delay;
    internal ushort LFO2FadeTime;
    internal ushort LFO3Rate;
    internal ushort LFO3Depth;
    internal WaveformType LFO3WaveformType;
    internal ushort LFO3Delay;
    internal ushort LFO3FadeTime;
	public ParameterType LFOParamType;
	public WaveformType LFOParamWaveformType;
	public byte LFOTarget;
	public bool LFOEnabled;
	public LFOType LFOType;
	public bool LFO1PitchEnabled;
	public bool LFO2VolumeEnabled;
	public bool LFO3PanpotEnabled;
	public byte VolumeAdd;
	public byte PanpotAdd;
	public int CurOffset;
	public int LoopOffset;
	public bool Stopped;
	public uint LastNoteDuration;
	public uint LastRest;
	public uint TickInterval;
	public ushort SweepRate;
	public byte SweepPitch;
	public byte SweepVolume;
	public byte PanpotTarget;
	public int WaveIDIndex;
	public int BankIDIndex;
	public List<SongEvent>? DalSegnoCommands;
	public SongEvent? ToCodaCommand;
	public byte AttackVolume;
	public byte AttackTime;
	public byte Hold;
	public byte Decay;
	public byte Sustain;
	public byte Fade;
	public byte Release;
    public readonly List<DSEChannel> Channels = new(0x10);

	public void Init()
	{
		Expression = 0;
		Voice = 0;
		Volume = 0;
		Octave = 4;
		Panpot = 0;
		Rest = 0;
		PitchBend = 0;
		NoteVolume = 0;
		FlagEnded = true;
		FlagValue = 0;
		ChannelPanpot = 0;
		ChannelVolume = 0;
		CurOffset = _startOffset;
		LoopOffset = -1;
		Stopped = false;
		LastNoteDuration = 0;
		LastRest = 0;
		TickInterval = 0;
		SweepRate = 0;
		SweepPitch = 0;
		SweepTuneRate = 0;
		WaveIDIndex = -1;
		BankIDIndex = -1;
		DalSegnoCommands = [];

		AttackVolume = 0;
		AttackTime = 0;
		Hold = 0;
		Decay = 0;
		Sustain = 0;
		Fade = 0;
		Release = 0;
		StopAllChannels();
	}
	public int GetPitch()
	{
		//int lfo = LFOType == LFOType.Pitch ? LFOParam : 0;
		int lfo = 0;
		return (PitchBend * PitchBendRange / 2) + lfo;
	}
	public void Tick()
	{
		if (Rest > 0)
		{
			Rest--;
		}
		for (int i = 0; i < Channels.Count; i++)
		{
			DSEChannel c = Channels[i];
			if (c.NoteLength > 0)
			{
				c.NoteLength--;
			}
		}
	}

	public void StopAllChannels()
	{
		DSEChannel[] chans = Channels.ToArray();
		for (int i = 0; i < chans.Length; i++)
		{
			chans[i].Stop();
		}
	}

	public void UpdateSongState(SongState.Track tin)
	{
		tin.Position = CurOffset;
		tin.Rest = Rest;
		tin.Voice = Voice;
		tin.Type = "PCM";
		tin.Volume = Volume;
		tin.PitchBend = GetPitch();
		tin.Extra = Octave;
		tin.Panpot = Panpot;

		DSEChannel[] channels = Channels.ToArray();
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
				DSEChannel c = channels[j];
				c ??= new DSEChannel((byte)j); // Failsafe in the rare event that the c variable becomes null
				if (!DSEUtils.IsStateRemovable(c.State))
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
