using System;
using System.Collections;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KNoiseChannel : MP2KPSGChannel
{
	private BitArray _pat;

	public MP2KNoiseChannel(MP2KMixer mixer)
		: base(mixer)
	{
		_pat = null!;
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