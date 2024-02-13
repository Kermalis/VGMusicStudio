using System;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed class AlphaDreamPCMChannel : AlphaDreamChannel
{
	private SampleHeader _sampleHeader;
	private int _sampleOffset;
	private bool _bFixed;

	public AlphaDreamPCMChannel(AlphaDreamMixer mixer) : base(mixer)
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

		_sampleHeader = new SampleHeader(_mixer.Config.ROM, sampleOffset, out _sampleOffset);
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