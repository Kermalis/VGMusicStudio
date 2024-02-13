using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using System;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed class AlphaDreamSquareChannel : AlphaDreamChannel
{
	private float[] _pat;

	public AlphaDreamSquareChannel(AlphaDreamMixer mixer)
		: base(mixer)
	{
		_pat = null!;
	}
	public void Init(byte key, ADSR env, byte vol, sbyte pan, int pitch)
	{
		_pat = MP2KUtils.SquareD50; // TODO: Which square pattern?
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