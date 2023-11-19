using System;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal abstract class AlphaDreamChannel
{
	protected readonly AlphaDreamMixer _mixer;
	public EnvelopeState State;
	public byte Key;
	public bool Stopped;

	protected ADSR _adsr;

	protected byte _velocity;
	protected int _pos;
	protected float _interPos;
	protected float _frequency;
	protected byte _leftVol;
	protected byte _rightVol;

	protected AlphaDreamChannel(AlphaDreamMixer mixer)
	{
		_mixer = mixer;
	}

	public ChannelVolume GetVolume()
	{
		const float MAX = 1f / 0x10000;
		return new ChannelVolume
		{
			LeftVol = _leftVol * _velocity * MAX,
			RightVol = _rightVol * _velocity * MAX,
		};
	}
	public void SetVolume(byte vol, sbyte pan)
	{
		_leftVol = (byte)((vol * (-pan + 0x80)) >> 8);
		_rightVol = (byte)((vol * (pan + 0x80)) >> 8);
	}
	public abstract void SetPitch(int pitch);

	public abstract void Process(float[] buffer);
}
internal class PCMChannel : AlphaDreamChannel
{
	private SampleHeader _sampleHeader;
	private int _sampleOffset;
	private bool _bFixed;

	public PCMChannel(AlphaDreamMixer mixer) : base(mixer)
	{
		//
	}
	public void Init(byte key, ADSR adsr, int sampleOffset, bool bFixed)
	{
		_velocity = adsr.A;
		State = EnvelopeState.Attack;
		_pos = 0; _interPos = 0;
		Key = key;
		_adsr = adsr;

		_sampleHeader = MemoryMarshal.Read<SampleHeader>(_mixer.Config.ROM.AsSpan(sampleOffset));
		_sampleOffset = sampleOffset + 0x10;
		_bFixed = bFixed;
		Stopped = false;
	}

	public override void SetPitch(int pitch)
	{
		_frequency = (_sampleHeader.SampleRate >> 10) * MathF.Pow(2, ((Key - 60) / 12f) + (pitch / 768f));
	}

	private void StepEnvelope()
	{
		switch (State)
		{
			case EnvelopeState.Attack:
			{
				int nextVel = _velocity + _adsr.A;
				if (nextVel >= 0xFF)
				{
					State = EnvelopeState.Decay;
					_velocity = 0xFF;
				}
				else
				{
					_velocity = (byte)nextVel;
				}
				break;
			}
			case EnvelopeState.Decay:
			{
				int nextVel = (_velocity * _adsr.D) >> 8;
				if (nextVel <= _adsr.S)
				{
					State = EnvelopeState.Sustain;
					_velocity = _adsr.S;
				}
				else
				{
					_velocity = (byte)nextVel;
				}
				break;
			}
			case EnvelopeState.Release:
			{
				int next = (_velocity * _adsr.R) >> 8;
				if (next < 0)
				{
					next = 0;
				}
				_velocity = (byte)next;
				break;
			}
		}
	}

	public override void Process(float[] buffer)
	{
		StepEnvelope();

		ChannelVolume vol = GetVolume();
		float interStep = (_bFixed ? _sampleHeader.SampleRate >> 10 : _frequency) * _mixer.SampleRateReciprocal;
		int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = (_mixer.Config.ROM[_pos + _sampleOffset] - 0x80) / (float)0x80;

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos += posDelta;
			if (_pos >= _sampleHeader.Length)
			{
				if (_sampleHeader.DoesLoop == 0x40000000)
				{
					_pos = _sampleHeader.LoopOffset;
				}
				else
				{
					Stopped = true;
					break;
				}
			}
		} while (--samplesPerBuffer > 0);
	}
}
internal class SquareChannel : AlphaDreamChannel
{
	private float[] _pat;

	public SquareChannel(AlphaDreamMixer mixer) : base(mixer)
	{
		//
	}
	public void Init(byte key, ADSR env, byte vol, sbyte pan, int pitch)
	{
		_pat = MP2K.Utils.SquareD50; // TODO: Which square pattern?
		Key = key;
		_adsr = env;
		SetVolume(vol, pan);
		SetPitch(pitch);
		State = EnvelopeState.Attack;
	}

	public override void SetPitch(int pitch)
	{
		_frequency = 3_520 * MathF.Pow(2, ((Key - 69) / 12f) + (pitch / 768f));
	}

	private void StepEnvelope()
	{
		switch (State)
		{
			case EnvelopeState.Attack:
			{
				int next = _velocity + _adsr.A;
				if (next >= 0xF)
				{
					State = EnvelopeState.Decay;
					_velocity = 0xF;
				}
				else
				{
					_velocity = (byte)next;
				}
				break;
			}
			case EnvelopeState.Decay:
			{
				int next = (_velocity * _adsr.D) >> 3;
				if (next <= _adsr.S)
				{
					State = EnvelopeState.Sustain;
					_velocity = _adsr.S;
				}
				else
				{
					_velocity = (byte)next;
				}
				break;
			}
			case EnvelopeState.Release:
			{
				int next = (_velocity * _adsr.R) >> 3;
				if (next < 0)
				{
					next = 0;
				}
				_velocity = (byte)next;
				break;
			}
		}
	}

	public override void Process(float[] buffer)
	{
		StepEnvelope();

		ChannelVolume vol = GetVolume();
		float interStep = _frequency * _mixer.SampleRateReciprocal;

		int bufPos = 0; int samplesPerBuffer = _mixer.SamplesPerBuffer;
		do
		{
			float samp = _pat[_pos];

			buffer[bufPos++] += samp * vol.LeftVol;
			buffer[bufPos++] += samp * vol.RightVol;

			_interPos += interStep;
			int posDelta = (int)_interPos;
			_interPos -= posDelta;
			_pos = (_pos + posDelta) & 0x7;
		} while (--samplesPerBuffer > 0);
	}
}
