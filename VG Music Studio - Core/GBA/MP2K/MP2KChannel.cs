using System;
using System.Collections;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal abstract class MP2KChannel
{
	public EnvelopeState State = EnvelopeState.Dead;
	public MP2KTrack? Owner;
	protected readonly MP2KMixer _mixer;

	public NoteInfo Note;
	protected ADSR _adsr;
	protected int _instPan;

	protected byte _velocity;
	protected int _pos;
	protected float _interPos;
	protected float _frequency;

	protected MP2KChannel(MP2KMixer mixer)
	{
		_mixer = mixer;
	}

	public abstract ChannelVolume GetVolume();
	public abstract void SetVolume(byte vol, sbyte pan);
	public abstract void SetPitch(int pitch);
	public virtual void Release()
	{
		if (State < EnvelopeState.Releasing)
		{
			State = EnvelopeState.Releasing;
		}
	}

	public abstract void Process(float[] buffer);

	// Returns whether the note is active or not
	public virtual bool TickNote()
	{
		if (State >= EnvelopeState.Releasing)
		{
			return false;
		}

		if (Note.Duration > 0)
		{
			Note.Duration--;
			if (Note.Duration == 0)
			{
				State = EnvelopeState.Releasing;
				return false;
			}
		}
		return true;
	}
	public void Stop()
	{
		State = EnvelopeState.Dead;
		if (Owner is not null)
		{
			Owner.Channels.Remove(this);
		}
		Owner = null;
	}
}
internal sealed class MP2KPCM8Channel : MP2KChannel
{
	private SampleHeader _sampleHeader;
	private int _sampleOffset;
	private GoldenSunPSG _gsPSG;
	private bool _bFixed;
	private bool _bGoldenSun;
	private bool _bCompressed;
	private byte _leftVol;
	private byte _rightVol;
	private sbyte[]? _decompressedSample;

	public MP2KPCM8Channel(MP2KMixer mixer)
		: base(mixer)
	{
		//
	}
	public void Init(MP2KTrack owner, NoteInfo note, ADSR adsr, int sampleOffset, byte vol, sbyte pan, int instPan, int pitch, bool bFixed, bool bCompressed)
	{
		State = EnvelopeState.Initializing;
		_pos = 0;
		_interPos = 0;
		if (Owner is not null)
		{
			Owner.Channels.Remove(this);
		}
		Owner = owner;
		Owner.Channels.Add(this);
		Note = note;
		_adsr = adsr;
		_instPan = instPan;
		byte[] rom = _mixer.Config.ROM;
		_sampleHeader = SampleHeader.Get(rom, sampleOffset, out _sampleOffset);
		_bFixed = bFixed;
		_bCompressed = bCompressed;
		_decompressedSample = bCompressed ? MP2KUtils.Decompress(rom.AsSpan(_sampleOffset), _sampleHeader.Length) : null;
		_bGoldenSun = _mixer.Config.HasGoldenSunSynths && _sampleHeader.Length == 0 && _sampleHeader.DoesLoop == SampleHeader.LOOP_TRUE && _sampleHeader.LoopOffset == 0;
		if (_bGoldenSun)
		{
			_gsPSG = GoldenSunPSG.Get(rom.AsSpan(_sampleOffset));
		}
		SetVolume(vol, pan);
		SetPitch(pitch);
	}

	public override ChannelVolume GetVolume()
	{
		const float MAX = 0x10_000;
		return new ChannelVolume
		{
			LeftVol = _leftVol * _velocity / MAX * _mixer.PCM8MasterVolume,
			RightVol = _rightVol * _velocity / MAX * _mixer.PCM8MasterVolume
		};
	}
	public override void SetVolume(byte vol, sbyte pan)
	{
		int combinedPan = pan + _instPan;
		if (combinedPan > 63)
		{
			combinedPan = 63;
		}
		else if (combinedPan < -64)
		{
			combinedPan = -64;
		}
		const int fix = 0x2000;
		if (State < EnvelopeState.Releasing)
		{
			int a = Note.Velocity * vol;
			_leftVol = (byte)(a * (-combinedPan + 0x40) / fix);
			_rightVol = (byte)(a * (combinedPan + 0x40) / fix);
		}
	}
	public override void SetPitch(int pitch)
	{
		_frequency = (_sampleHeader.SampleRate >> 10) * MathF.Pow(2, ((Note.Note - 60) / 12f) + (pitch / 768f));
	}

	private void StepEnvelope()
	{
		switch (State)
		{
			case EnvelopeState.Initializing:
			{
				_velocity = _adsr.A;
				State = EnvelopeState.Rising;
				break;
			}
			case EnvelopeState.Rising:
			{
				int nextVel = _velocity + _adsr.A;
				if (nextVel >= 0xFF)
				{
					State = EnvelopeState.Decaying;
					_velocity = 0xFF;
				}
				else
				{
					_velocity = (byte)nextVel;
				}
				break;
			}
			case EnvelopeState.Decaying:
			{
				int nextVel = (_velocity * _adsr.D) >> 8;
				if (nextVel <= _adsr.S)
				{
					State = EnvelopeState.Playing;
					_velocity = _adsr.S;
				}
				else
				{
					_velocity = (byte)nextVel;
				}
				break;
			}
			case EnvelopeState.Playing:
			{
				break;
			}
			case EnvelopeState.Releasing:
			{
				int nextVel = (_velocity * _adsr.R) >> 8;
				if (nextVel <= 0)
				{
					State = EnvelopeState.Dying;
					_velocity = 0;
				}
				else
				{
					_velocity = (byte)nextVel;
				}
				break;
			}
			case EnvelopeState.Dying:
			{
				Stop();
				break;
			}
		}
	}

	public override void Process(float[] buffer)
	{
		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		ChannelVolume vol = GetVolume();
		float interStep = _bFixed && !_bGoldenSun ? _mixer.SampleRate * _mixer.SampleRateReciprocal : _frequency * _mixer.SampleRateReciprocal;
		if (_bGoldenSun) // Most Golden Sun processing is thanks to ipatix
		{
			Process_GS(buffer, vol, interStep);
		}
		else if (_bCompressed)
		{
			Process_Compressed(buffer, vol, interStep);
		}
		else
		{
			Process_Standard(buffer, vol, interStep, _mixer.Config.ROM);
		}
	}
	private void Process_GS(float[] buffer, ChannelVolume vol, float interStep)
	{
		interStep /= 0x40;
		switch (_gsPSG.Type)
		{
			case GoldenSunPSGType.Square:
			{
				_pos += _gsPSG.CycleSpeed << 24;
				int iThreshold = (_gsPSG.MinimumCycle << 24) + _pos;
				iThreshold = (iThreshold < 0 ? ~iThreshold : iThreshold) >> 8;
				iThreshold = (iThreshold * _gsPSG.CycleAmplitude) + (_gsPSG.InitialCycle << 24);
				float threshold = iThreshold / (float)0x100_000_000;

				int bufPos = 0;
				int samplesPerBuffer = _mixer.SamplesPerBuffer;
				do
				{
					float samp = _interPos < threshold ? 0.5f : -0.5f;
					samp += 0.5f - threshold;
					buffer[bufPos++] += samp * vol.LeftVol;
					buffer[bufPos++] += samp * vol.RightVol;

					_interPos += interStep;
					if (_interPos >= 1)
					{
						_interPos--;
					}
				} while (--samplesPerBuffer > 0);
				break;
			}
			case GoldenSunPSGType.Saw:
			{
				const int FIX = 0x70;

				int bufPos = 0;
				int samplesPerBuffer = _mixer.SamplesPerBuffer;
				do
				{
					_interPos += interStep;
					if (_interPos >= 1)
					{
						_interPos--;
					}
					int var1 = (int)(_interPos * 0x100) - FIX;
					int var2 = (int)(_interPos * 0x10000) << 17;
					int var3 = var1 - (var2 >> 27);
					_pos = var3 + (_pos >> 1);

					float samp = _pos / (float)0x100;

					buffer[bufPos++] += samp * vol.LeftVol;
					buffer[bufPos++] += samp * vol.RightVol;
				} while (--samplesPerBuffer > 0);
				break;
			}
			case GoldenSunPSGType.Triangle:
			{
				int bufPos = 0;
				int samplesPerBuffer = _mixer.SamplesPerBuffer;
				do
				{
					_interPos += interStep;
					if (_interPos >= 1)
					{
						_interPos--;
					}
					float samp = _interPos < 0.5f ? (_interPos * 4) - 1 : 3 - (_interPos * 4);

					buffer[bufPos++] += samp * vol.LeftVol;
					buffer[bufPos++] += samp * vol.RightVol;
				} while (--samplesPerBuffer > 0);
				break;
			}
		}
	}
	private void Process_Compressed(float[] buffer, ChannelVolume vol, float interStep)
	{
		int bufPos = 0;
		int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = _decompressedSample![_pos] / (float)0x80;

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos += posDelta;
			if (_pos >= _decompressedSample.Length)
			{
				Stop();
				break;
			}
		} while (--samplesPerBuffer > 0);
	}
	private void Process_Standard(float[] buffer, ChannelVolume vol, float interStep, byte[] rom)
	{
		int bufPos = 0;
		int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = (sbyte)rom[_pos + _sampleOffset] / (float)0x80;

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos += posDelta;
			if (_pos >= _sampleHeader.Length)
			{
				if (_sampleHeader.DoesLoop != SampleHeader.LOOP_TRUE)
				{
					Stop();
					return;
				}

				_pos = _sampleHeader.LoopOffset;
			}
		} while (--samplesPerBuffer > 0);
	}
}
internal abstract class MP2KPSGChannel : MP2KChannel
{
	protected enum GBPan : byte
	{
		Left,
		Center,
		Right,
	}

	private byte _processStep;
	private EnvelopeState _nextState;
	private byte _peakVelocity;
	private byte _sustainVelocity;
	protected GBPan _panpot = GBPan.Center;

	public MP2KPSGChannel(MP2KMixer mixer)
		: base(mixer)
	{
		//
	}
	protected void Init(MP2KTrack owner, NoteInfo note, ADSR env, int instPan)
	{
		State = EnvelopeState.Initializing;
		if (Owner is not null)
		{
			Owner.Channels.Remove(this);
		}
		Owner = owner;
		Owner.Channels.Add(this);
		Note = note;
		_adsr.A = (byte)(env.A & 0x7);
		_adsr.D = (byte)(env.D & 0x7);
		_adsr.S = (byte)(env.S & 0xF);
		_adsr.R = (byte)(env.R & 0x7);
		_instPan = instPan;
	}

	public override void Release()
	{
		if (State < EnvelopeState.Releasing)
		{
			if (_adsr.R == 0)
			{
				_velocity = 0;
				Stop();
			}
			else if (_velocity == 0)
			{
				Stop();
			}
			else
			{
				_nextState = EnvelopeState.Releasing;
			}
		}
	}
	public override bool TickNote()
	{
		if (State < EnvelopeState.Releasing)
		{
			if (Note.Duration > 0)
			{
				Note.Duration--;
				if (Note.Duration == 0)
				{
					if (_velocity == 0)
					{
						Stop();
					}
					else
					{
						State = EnvelopeState.Releasing;
					}
					return false;
				}
				return true;
			}
			else
			{
				return true;
			}
		}
		else
		{
			return false;
		}
	}

	public override ChannelVolume GetVolume()
	{
		const float max = 0x20;
		return new ChannelVolume
		{
			LeftVol = _panpot == GBPan.Right ? 0 : _velocity / max,
			RightVol = _panpot == GBPan.Left ? 0 : _velocity / max
		};
	}
	public override void SetVolume(byte vol, sbyte pan)
	{
		int combinedPan = pan + _instPan;
		if (combinedPan > 63)
		{
			combinedPan = 63;
		}
		else if (combinedPan < -64)
		{
			combinedPan = -64;
		}
		if (State < EnvelopeState.Releasing)
		{
			_panpot = combinedPan < -21 ? GBPan.Left : combinedPan > 20 ? GBPan.Right : GBPan.Center;
			_peakVelocity = (byte)((Note.Velocity * vol) >> 10);
			_sustainVelocity = (byte)(((_peakVelocity * _adsr.S) + 0xF) >> 4); // TODO
			if (State == EnvelopeState.Playing)
			{
				_velocity = _sustainVelocity;
			}
		}
	}

	protected void StepEnvelope()
	{
		void dec()
		{
			_processStep = 0;
			if (_velocity - 1 <= _sustainVelocity)
			{
				_velocity = _sustainVelocity;
				_nextState = EnvelopeState.Playing;
			}
			else if (_velocity != 0)
			{
				_velocity--;
			}
		}
		void sus()
		{
			_processStep = 0;
		}
		void rel()
		{
			if (_adsr.R == 0)
			{
				_velocity = 0;
				Stop();
			}
			else
			{
				_processStep = 0;
				if (_velocity - 1 <= 0)
				{
					_nextState = EnvelopeState.Dying;
					_velocity = 0;
				}
				else
				{
					_velocity--;
				}
			}
		}

		switch (State)
		{
			case EnvelopeState.Initializing:
			{
				_nextState = EnvelopeState.Rising;
				_processStep = 0;
				if ((_adsr.A | _adsr.D) == 0 || (_sustainVelocity == 0 && _peakVelocity == 0))
				{
					State = EnvelopeState.Playing;
					_velocity = _sustainVelocity;
					return;
				}
				else if (_adsr.A == 0 && _adsr.S < 0xF)
				{
					State = EnvelopeState.Decaying;
					int next = _peakVelocity - 1;
					if (next < 0)
					{
						next = 0;
					}
					_velocity = (byte)next;
					if (_velocity < _sustainVelocity)
					{
						_velocity = _sustainVelocity;
					}
					return;
				}
				else if (_adsr.A == 0)
				{
					State = EnvelopeState.Playing;
					_velocity = _sustainVelocity;
					return;
				}
				else
				{
					State = EnvelopeState.Rising;
					_velocity = 1;
					return;
				}
			}
			case EnvelopeState.Rising:
			{
				if (++_processStep >= _adsr.A)
				{
					if (_nextState == EnvelopeState.Decaying)
					{
						State = EnvelopeState.Decaying;
						dec(); return;
					}
					if (_nextState == EnvelopeState.Playing)
					{
						State = EnvelopeState.Playing;
						sus(); return;
					}
					if (_nextState == EnvelopeState.Releasing)
					{
						State = EnvelopeState.Releasing;
						rel(); return;
					}
					_processStep = 0;
					if (++_velocity >= _peakVelocity)
					{
						if (_adsr.D == 0)
						{
							_nextState = EnvelopeState.Playing;
						}
						else if (_peakVelocity == _sustainVelocity)
						{
							_nextState = EnvelopeState.Playing;
							_velocity = _peakVelocity;
						}
						else
						{
							_velocity = _peakVelocity;
							_nextState = EnvelopeState.Decaying;
						}
					}
				}
				break;
			}
			case EnvelopeState.Decaying:
			{
				if (++_processStep >= _adsr.D)
				{
					if (_nextState == EnvelopeState.Playing)
					{
						State = EnvelopeState.Playing;
						sus(); return;
					}
					if (_nextState == EnvelopeState.Releasing)
					{
						State = EnvelopeState.Releasing;
						rel(); return;
					}
					dec();
				}
				break;
			}
			case EnvelopeState.Playing:
			{
				if (++_processStep >= 1)
				{
					if (_nextState == EnvelopeState.Releasing)
					{
						State = EnvelopeState.Releasing;
						rel(); return;
					}
					sus();
				}
				break;
			}
			case EnvelopeState.Releasing:
			{
				if (++_processStep >= _adsr.R)
				{
					if (_nextState == EnvelopeState.Dying)
					{
						Stop();
						return;
					}
					rel();
				}
				break;
			}
		}
	}
}
internal sealed class MP2KSquareChannel : MP2KPSGChannel
{
	private float[]? _pat;

	public MP2KSquareChannel(MP2KMixer mixer)
		: base(mixer)
	{
		//
	}
	public void Init(MP2KTrack owner, NoteInfo note, ADSR env, int instPan, SquarePattern pattern)
	{
		Init(owner, note, env, instPan);
		_pat = pattern switch
		{
			SquarePattern.D12 => MP2KUtils.SquareD12,
			SquarePattern.D25 => MP2KUtils.SquareD25,
			SquarePattern.D50 => MP2KUtils.SquareD50,
			_ => MP2KUtils.SquareD75,
		};
	}

	public override void SetPitch(int pitch)
	{
		_frequency = 3_520 * MathF.Pow(2, ((Note.Note - 69) / 12f) + (pitch / 768f));
	}

	public override void Process(float[] buffer)
	{
		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		ChannelVolume vol = GetVolume();
		float interStep = _frequency * _mixer.SampleRateReciprocal;

		int bufPos = 0;
		int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = _pat![_pos];

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos = (_pos + posDelta) & 0x7;
		} while (--samplesPerBuffer > 0);
	}
}
internal sealed class MP2KPCM4Channel : MP2KPSGChannel
{
	private readonly float[] _sample;

	public MP2KPCM4Channel(MP2KMixer mixer)
		: base(mixer)
	{
		_sample = new float[0x20];
	}
	public void Init(MP2KTrack owner, NoteInfo note, ADSR env, int instPan, int sampleOffset)
	{
		Init(owner, note, env, instPan);
		MP2KUtils.PCM4ToFloat(_mixer.Config.ROM.AsSpan(sampleOffset), _sample);
	}

	public override void SetPitch(int pitch)
	{
		_frequency = 7_040 * MathF.Pow(2, ((Note.Note - 69) / 12f) + (pitch / 768f));
	}

	public override void Process(float[] buffer)
	{
		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		ChannelVolume vol = GetVolume();
		float interStep = _frequency * _mixer.SampleRateReciprocal;

		int bufPos = 0;
		int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = _sample[_pos];

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos = (_pos + posDelta) & 0x1F;
		} while (--samplesPerBuffer > 0);
	}
}
internal sealed class MP2KNoiseChannel : MP2KPSGChannel
{
	private BitArray _pat;

	public MP2KNoiseChannel(MP2KMixer mixer)
		: base(mixer)
	{
		//
	}
	public void Init(MP2KTrack owner, NoteInfo note, ADSR env, int instPan, NoisePattern pattern)
	{
		Init(owner, note, env, instPan);
		_pat = pattern == NoisePattern.Fine ? MP2KUtils.NoiseFine : MP2KUtils.NoiseRough;
	}

	public override void SetPitch(int pitch)
	{
		int key = Note.Note + (int)MathF.Round(pitch / 64f);
		if (key <= 20)
		{
			key = 0;
		}
		else
		{
			key -= 21;
			if (key > 59)
			{
				key = 59;
			}
		}
		byte v = MP2KUtils.NoiseFrequencyTable[key];
		// The following emulates 0x0400007C - SOUND4CNT_H
		int r = v & 7; // Bits 0-2
		int s = v >> 4; // Bits 4-7
		_frequency = 524_288f / (r == 0 ? 0.5f : r) / MathF.Pow(2, s + 1);
	}

	public override void Process(float[] buffer)
	{
		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		ChannelVolume vol = GetVolume();
		float interStep = _frequency * _mixer.SampleRateReciprocal;

		int bufPos = 0;
		int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = _pat[_pos & (_pat.Length - 1)] ? 0.5f : -0.5f;

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos = (_pos + posDelta) & (_pat.Length - 1);
		} while (--samplesPerBuffer > 0);
	}
}