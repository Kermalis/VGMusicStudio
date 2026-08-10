using Kermalis.VGMusicStudio.Core.Codec;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Wii;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class DSEChannel
{
	public readonly byte Index;

	public DSETrack? Owner;
	public string? SWDType;
	public EnvelopeState State;
	public sbyte RootKey;
	public byte Key;
	public byte NoteVelocity;
	public sbyte Panpot; // Not necessary
	public ushort BaseTimer;
	public ushort Timer;
	public uint NoteLength;
	public byte Volume;
	internal int SweepCounter;
	public static readonly float Root12Of2 = MathF.Pow(2, 1f / 12);

	private int _pos;
	private short _prevLeft;
	private short _prevRight;

	private int _envelopeTimeLeft;
	private int _volumeIncrement;
	private int _velocity; // From 0-0x3FFFFFFF ((128 << 23) - 1)
	private byte _targetVolume;

	private byte _attackVolume;
	private byte _attackTime;
	private byte _decay;
	private byte _sustain;
	private byte _hold;
	private byte _fade;
	private byte _release;

	// PCM8, PCM16, IMA-ADPCM, DSP-ADPCM
	private SWD.SampleBlock? _sample;
	// PCM8, PCM16
	private int _dataOffset;
	// IMA-ADPCM
	private IMAADPCM _adpcmDecoder;
	private short _adpcmLoopLastSample;
	private short _adpcmLoopStepIndex;
	// DSP-ADPCM
	private DSPADPCM _dspADPCM;
	// PSG
	private byte _psgDuty;
	private int _psgCounter;

	public DSEChannel(byte i)
	{
		_sample = null!;
		Index = i;
	}

	public bool StartChannel(SWD localswd, SWD masterswd, DSETrack track, int key, uint noteLength)
	{
		Owner = track;
		if (localswd == null) { SWDType = masterswd.Type; }
		else { SWDType = localswd.Type; }

		SWD.IProgramInfo? programInfo = null; // Declaring Program Info Interface here, to ensure VGMS compiles
		if (localswd == null)
		{
			// Failsafe to check if SWD.ProgramBank contains an instance, if it doesn't, it will be skipped
			// This is especially important for initializing a main SWD before the local SWDs
			// accompaning the SMDs with the same names are loaded in.
			if (track.Voice > masterswd.Programs!.ProgramInfos!.Length)
			{
				throw new IndexOutOfRangeException(string.Format(Strings.ErrorDSEVoiceIndexOutOfRange, track.Voice, masterswd.Programs!.ProgramInfos!.Length));
			}
			if (masterswd.Programs != null)
			{
				programInfo = masterswd.Programs!.ProgramInfos![track.Voice];
			}
		}
		else if (track.Voice > localswd.Programs!.ProgramInfos!.Length)
		{
			programInfo = masterswd.Programs!.ProgramInfos![track.Voice];
		}
		else
		{
			programInfo = localswd.Programs!.ProgramInfos![track.Voice];
		}

		if (programInfo is null)
		{
			return false;
		}

		for (int i = 0; i < programInfo.SplitEntries.Length; i++)
		{
			SWD.ISplitEntry split = programInfo.SplitEntries[i];
			if (key < split.LowKey || key > split.HighKey)
			{
				continue;
			}

			_sample = masterswd.Samples![split.SampleId];
			Key = (byte)key;
			RootKey = split.SampleRootKey;
			if (_sample != null)
			{
				switch (SWDType) // Configures the base timer based on the specific console's sound framework and sample rate
				{
					case "wds ": throw new NotImplementedException("The base timer for the WDS type is not yet implemented."); // PlayStation
					case "swdm": throw new NotImplementedException("The base timer for the SWDM type is not yet implemented."); // PlayStation 2
					case "swdl": BaseTimer = (ushort)(NDSUtils.ARM7_CLOCK / _sample.WavInfo!.SampleRate); break; // Nintendo DS // Time Base algorithm is the ARM7 CPU clock rate divided by SampleRate
					case "swdb": BaseTimer = (ushort)(WiiUtils.Macronix_DSP_Clock / _sample.WavInfo!.SampleRate); break; // Wii // The AX Time Base algorithm is the DSP clock rate divided by SampleRate
				}
				if (_sample.WavInfo!.SampleFormat == SampleFormat.ADPCM)
				{
					_adpcmDecoder.Init(_sample.Data!);
				}
				if (masterswd.Type == "swdb")
				{
					_dspADPCM.Init(_sample.DSPADPCM.Data, _sample.DSPADPCM.Info);
				}
				//attackVolume = sample.WavInfo.AttackVolume == 0 ? split.AttackVolume : sample.WavInfo.AttackVolume;
				//attack = sample.WavInfo.Attack == 0 ? split.Attack : sample.WavInfo.Attack;
				//decay = sample.WavInfo.Decay == 0 ? split.Decay : sample.WavInfo.Decay;
				//sustain = sample.WavInfo.Sustain == 0 ? split.Sustain : sample.WavInfo.Sustain;
				//hold = sample.WavInfo.Hold == 0 ? split.Hold : sample.WavInfo.Hold;
				//decay2 = sample.WavInfo.Decay2 == 0 ? split.Decay2 : sample.WavInfo.Decay2;
				//release = sample.WavInfo.Release == 0 ? split.Release : sample.WavInfo.Release;
				//attackVolume = split.AttackVolume == 0 ? sample.WavInfo.AttackVolume : split.AttackVolume;
				//attack = split.Attack == 0 ? sample.WavInfo.Attack : split.Attack;
				//decay = split.Decay == 0 ? sample.WavInfo.Decay : split.Decay;
				//sustain = split.Sustain == 0 ? sample.WavInfo.Sustain : split.Sustain;
				//hold = split.Hold == 0 ? sample.WavInfo.Hold : split.Hold;
				//decay2 = split.Decay2 == 0 ? sample.WavInfo.Decay2 : split.Decay2;
				//release = split.Release == 0 ? sample.WavInfo.Release : split.Release;

				_attackVolume = split.AttackVolume == 0 ? _sample.WavInfo.AttackVolume == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.AttackVolume : split.AttackVolume;
				_attackTime = split.AttackTime == 0 ? _sample.WavInfo.AttackTime == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.AttackTime : split.AttackTime;
				_decay = split.Decay == 0 ? _sample.WavInfo.Decay == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.Decay : split.Decay;
				_sustain = split.Sustain == 0 ? _sample.WavInfo.Sustain == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.Sustain : split.Sustain;
				_hold = split.Hold == 0 ? _sample.WavInfo.Hold == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.Hold : split.Hold;
				_fade = split.Fade == 0 ? _sample.WavInfo.Fade == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.Fade : split.Fade;
				_release = split.Release == 0 ? _sample.WavInfo.Release == 0 ? _sample.WavInfo.Volume : _sample.WavInfo.Release : split.Release;

				DetermineEnvelopeStartingPoint();
				_pos = 0;
				_prevLeft = _prevRight = 0;
				NoteLength = noteLength;
				return true;
			}
		}
		return false;
	}

	public void Stop()
	{
		if (Owner is not null)
		{
			Owner.Channels.Remove(this);
		}
		Owner = null;
		Volume = 0;
	}

	public int SweepMain()
	{
		if (Owner!.SweepPitch == 0 || SweepCounter >= Owner.SweepRate)
		{
			return 0;
		}

		int sweep = (int)(Math.BigMul(Owner.SweepPitch, Owner.SweepRate - SweepCounter) / Owner.SweepRate);
		SweepCounter++;
		return sweep;
	}

	// CMDB1___sub_2074CA0
	private bool IsValidEnvelope()
	{
		bool b = true;
		bool ge = _sample!.WavInfo!.EnvMult >= _sample.WavInfo.Volume;
		bool ee = _sample.WavInfo.EnvMult == _sample.WavInfo.Volume;
		if (_sample.WavInfo.EnvMult > _sample.WavInfo.Volume)
		{
			ge = _attackVolume >= _sample.WavInfo.Volume;
			ee = _attackVolume == _sample.WavInfo.Volume;
		}
		if (!ee & ge
			&& _attackTime > _sample.WavInfo.Volume
			&& _decay > _sample.WavInfo.Volume
			&& _sustain > _sample.WavInfo.Volume
			&& _hold > _sample.WavInfo.Volume
			&& _fade > _sample.WavInfo.Volume
			&& _release > _sample.WavInfo.Volume)
		{
			b = false;
		}
		return b;
	}
	private void DetermineEnvelopeStartingPoint()
	{
		State = EnvelopeState.Attack; // This isn't actually placed in this func
		bool atLeastOneThingIsValid = IsValidEnvelope(); // Neither is this
		if (atLeastOneThingIsValid)
		{
			if (_fade != 0)
			{
				UpdateEnvelopePlan(0, _fade);
				State = EnvelopeState.Attack;
			}
			if (_attackTime != 0)
			{
				_velocity = _attackVolume << 23;
				State = EnvelopeState.Hold;
				UpdateEnvelopePlan(_sample!.WavInfo!.Volume, _attackTime);
			}
			else
			{
				_velocity = _sample!.WavInfo!.Volume << 23;
				if (_hold != 0)
				{
					UpdateEnvelopePlan(_sample.WavInfo.Volume, _hold);
					State = EnvelopeState.Decay;
				}
				else if (_decay != 0)
				{
					UpdateEnvelopePlan(_sustain, _decay);
					State = EnvelopeState.Fade;
				}
				else
				{
					UpdateEnvelopePlan(0, _release);
					State = EnvelopeState.Sustain;
				}
			}
			// Unk1E = 1
		}
		else if (State != EnvelopeState.PlayNote) // Need to Initialize before it starts the PlayNote state
		{
			State = EnvelopeState.Initialize;
			_velocity = _sample!.WavInfo!.Volume << 23;
		}
	}

	// SetEnvelopePhase7_2074ED8
	public void SetEnvelopeRelease()
	{
		if (State != EnvelopeState.Initialize)
		{
			UpdateEnvelopePlan(0, _release);
			State = EnvelopeState.End;
		}
	}
	public int StepEnvelope()
	{
		if (State > EnvelopeState.Attack)
		{
			if (_envelopeTimeLeft != 0)
			{
				_envelopeTimeLeft--;
				_velocity += _volumeIncrement;
				if (_velocity < 0)
				{
					_velocity = 0;
				}
				else if (_velocity > 0x3FFFFFFF)
				{
					_velocity = 0x3FFFFFFF;
				}
			}
			else
			{
				_velocity = _targetVolume << 23;
				switch (State)
				{
					default: return _velocity >> 23; // case 8
					case EnvelopeState.Hold:
						{
							if (_hold == 0)
							{
								goto LABEL_6;
							}
							else
							{
								UpdateEnvelopePlan(_sample!.WavInfo!.Volume, _hold);
								State = EnvelopeState.Decay;
							}
							break;
						}
					case EnvelopeState.Decay:
					LABEL_6:
						{
							if (_decay == 0)
							{
								_velocity = _sustain << 23;
								goto LABEL_9;
							}
							else
							{
								UpdateEnvelopePlan(_sustain, _decay);
								State = EnvelopeState.Fade;
							}
							break;
						}
					case EnvelopeState.Fade:
					LABEL_9:
						{
							if (_fade == 0)
							{
								goto LABEL_11;
							}
							else
							{
								UpdateEnvelopePlan(0, _fade);
								State = EnvelopeState.Sustain;
							}
							break;
						}
					case EnvelopeState.Sustain:
					LABEL_11:
						{
							UpdateEnvelopePlan(0, 0);
							State = EnvelopeState.Attack;
							break;
						}
					case EnvelopeState.End:
						{
							State = EnvelopeState.Release;
							_velocity = 0;
							_envelopeTimeLeft = 0;
							break;
						}
				}
			}
		}
		return _velocity >> 23;
	}
	private void UpdateEnvelopePlan(byte targetVolume, int envelopeParam)
	{
		if (envelopeParam == _sample!.WavInfo!.Volume)
		{
			_volumeIncrement = 0;
			_envelopeTimeLeft = int.MaxValue;
		}
		else
		{
			_targetVolume = targetVolume;
			_envelopeTimeLeft = (int)(_sample!.WavInfo!.EnvMult == 0
				? DSEUtils.Duration32[envelopeParam] * 1_000 / 10_000
				: DSEUtils.Duration16[envelopeParam] * (_sample.WavInfo.EnvMult * .2));
			_volumeIncrement = _envelopeTimeLeft == 0 ? 0 : ((targetVolume << 23) - _velocity) / _envelopeTimeLeft;
		}
	}

	public void Process(out short left, out short right)
	{
		if (Timer == 0)
		{
			left = _prevLeft;
			right = _prevRight;
			return;
		}

		int numSamples = (_pos + 0x100) / Timer;
		_pos = (_pos + 0x100) % Timer;
		// prevLeft and prevRight are stored because numSamples can be 0.
		for (int i = 0; i < numSamples; i++)
		{
			switch (SWDType)
			{
				case "wds ":
				case "swdm":
				case "swdl":
					{
						short samp;
						switch (_sample!.WavInfo!.SampleFormat)
						{
							case SampleFormat.PCM8:
								{
									// If hit end
									if (_dataOffset >= _sample.Data!.Length)
									{
										if (_sample.WavInfo.Loop)
										{
											_dataOffset = (int)(_sample.WavInfo.LoopStart * 4); // DS counts LoopStart 32-bits (4 bytes) at a time, so LoopStart needs to be bigger
										}
										else
										{
											left = right = _prevLeft = _prevRight = 0;
											Stop();
											return;
										}
									}
									samp = (short)((sbyte)_sample.Data[_dataOffset++] << 8);
									break;
								}
							case SampleFormat.PCM16:
								{
									// If hit end
									if (_dataOffset >= _sample.Data!.Length)
									{
										if (_sample.WavInfo.Loop)
										{
											_dataOffset = (int)(_sample.WavInfo.LoopStart * 4);
										}
										else
										{
											left = right = _prevLeft = _prevRight = 0;
											Stop();
											return;
										}
									}
									samp = (short)(_sample.Data[_dataOffset++] | (_sample.Data[_dataOffset++] << 8));
									break;
								}
							case SampleFormat.ADPCM:
								{
									// If just looped
									if (_adpcmDecoder!.DataOffset == _sample.WavInfo.LoopStart * 4 && !_adpcmDecoder.OnSecondNibble)
									{
										_adpcmLoopLastSample = _adpcmDecoder.LastSample;
										_adpcmLoopStepIndex = _adpcmDecoder.StepIndex;
									}
									// If hit end
									if (_adpcmDecoder.DataOffset >= _sample.Data!.Length && !_adpcmDecoder.OnSecondNibble)
									{
										if (_sample.WavInfo.Loop)
										{
											_adpcmDecoder.DataOffset = (int)(_sample.WavInfo.LoopStart * 4);
											_adpcmDecoder.StepIndex = _adpcmLoopStepIndex;
											_adpcmDecoder.LastSample = _adpcmLoopLastSample;
											_adpcmDecoder.OnSecondNibble = false;
										}
										else
										{
											left = right = _prevLeft = _prevRight = 0;
											Stop();
											return;
										}
									}
									samp = _adpcmDecoder.GetSample();
									break;
								}
							case SampleFormat.PSG:
								{
									samp = _psgCounter <= _psgDuty ? short.MinValue : short.MaxValue;
									_psgCounter++;
									if (_psgCounter >= 8)
									{
										_psgCounter = 0;
									}
									break;
								}
							default: samp = 0; break;
						}
						samp = (short)(samp * Volume / _sample.WavInfo.Volume);
						_prevLeft = (short)(samp * (-Panpot + 0x40) / 0x80);
						_prevRight = (short)(samp * (Panpot + 0x40) / 0x80);
						break;
					}
				case "swdb":
					{
						// If hit end
						if (_dataOffset >= DSPADPCM.NibblesToSamples((int)_sample!.WavInfo!.LoopEnd)) // Wii DSE always reads the LoopEnd address (in nibbles) when looping is enabled for a SWD entry, instead of reading until the end of the sample data
						{
							if (_sample.WavInfo!.Loop)
							{
								_dataOffset = DSPADPCM.NibblesToSamples((int)_sample.WavInfo.LoopStart); // Wii values for LoopStart offset are counted in nibbles (4-bits or half a byte) at a time, but because DataOutput is using a 16-bit array, LoopStart value needs to be converted to a 16-bit PCM sample offset
							}
							else
							{
								left = right = _prevLeft = _prevRight = 0;
								Stop();
								return;
							}
						}
						short samp = _sample.DSPADPCM!.DataOutput![_dataOffset++]; // Since DataOutput is already a 16-bit array, only one array entry is needed per loop, no bitshifting needed either
						samp = (short)(samp * Volume / _sample.WavInfo.Volume);
						_prevLeft = (short)(samp * (-Panpot + 0x40) / 0x80);
						_prevRight = (short)(samp * (Panpot + 0x40) / 0x80);
						break;
					}
			}
		}
		left = _prevLeft;
		right = _prevRight;
	}
}
