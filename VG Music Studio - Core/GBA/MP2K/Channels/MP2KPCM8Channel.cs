using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

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