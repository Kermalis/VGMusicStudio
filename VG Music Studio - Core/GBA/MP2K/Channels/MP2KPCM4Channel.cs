using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

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