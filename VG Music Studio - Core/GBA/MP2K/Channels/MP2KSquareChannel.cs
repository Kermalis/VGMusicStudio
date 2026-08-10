using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KSquareChannel : MP2KPSGChannel
{
	private readonly int _instrDuty;
	private readonly float[] _pat;
	private short SweepStartCount = -1;
	private readonly byte _sweep;
	private float _sweepTimer = 1.0f;
	private readonly bool _sweepEnabled;
	private readonly float _sweepConvergence;
	private readonly float _sweepCoefficent;

	public MP2KSquareChannel(MP2KContext context, MP2KMixer mixer, MP2KTrack track, int instrDuty, ADSR env, NoteInfo note, byte sweep)
		: base(context, mixer, track, env, note)
	{
		_instrDuty = instrDuty;
		_sweep = sweep;
		_sweepEnabled = IsSweepEnabled(sweep);
		_sweepConvergence = SweepToConvergence(sweep);
		_sweepCoefficent = SweepToCoefficent(sweep);

		List<float[]> patterns =
		[
			MP2KUtils.SquareD12,
			MP2KUtils.SquareD25,
			MP2KUtils.SquareD50,
			MP2KUtils.SquareD75,
		];

		_pat = patterns[instrDuty % 4];
		Rs = MP2KResampler.MakeResampler(ResamplerType.Nearest);
	}

	internal override void SetPitch(short pitch)
	{
		if (!Stop || Freq <= 0.0f)
		{
			Freq = 3_520 * MathF.Pow(2, ((Note.OriginalNote - 69) / 12f) + (pitch / 768f));
		}

		if (_sweepEnabled && (SweepStartCount < 0))
		{
			_sweepTimer = FrequencyToTimer(Freq / 8.0f);
			byte time = SweepTime(_sweep);
			SweepStartCount = (short)(time * GBAUtils.AGB_APPROX_FPS * GBAUtils.INTERFRAMES);
		}
	}

	internal override void Process(Span<float> buffer, MixingArgs args)
	{
		if (State == EnvelopeState.Dead)
		{
			return;
		}
		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		UpdateVolumeFade();

		if (buffer.Length == 0)
		{
			return;
		}

		ChannelVolume vol = GetVolume();
		float lVolStep = (vol.ToVolLeft - vol.FromVolLeft) * args.SamplesPerBufferInv;
		float rVolStep = (vol.ToVolRight - vol.FromVolRight) * args.SamplesPerBufferInv;
		float lVol = vol.FromVolLeft;
		float rVol = vol.FromVolRight;
		float interStep;

		int bufPos = 0;
		if (_sweepEnabled)
		{
			interStep = 8.0f * TimerToFrequency(_sweepTimer) * args.SampleRateInv;
		}
		else
		{
			interStep = Freq * args.SampleRateInv;
		}

		// MP2KResampler.FetchCallback cb = new(SampleFetchCallback);
		// Rs!.Process(_mixer.TrackBuffers[Track.Index], interStep, cb);

		int samplesPerBuffer = _mixer!.SamplesPerBuffer;
		ReadOnlySpan<float> samples = ProcessSamples(samplesPerBuffer, interStep);
		for (int i = 0; i < samplesPerBuffer; i++)
		{
			float samp = samples[i];
			buffer[bufPos++] += samp * lVol;
			buffer[bufPos++] += samp * rVol;
			lVol += lVolStep;
			rVol += rVolStep;
		}

		if (_sweepEnabled)
		{
			if (SweepStartCount == 0)
			{
				_sweepTimer *= _sweepCoefficent;
				if (IsSweepAscending(_sweep))
					_sweepTimer = MathF.Min(_sweepTimer, _sweepConvergence);
				else
					_sweepTimer = MathF.Max(_sweepTimer, _sweepConvergence);
			}
			else
			{
				SweepStartCount -= 128;
				if (SweepStartCount < 0)
					SweepStartCount = 0;
			}
		}
	}

	internal override VoiceConfigType GetVoiceType()
	{
		if (_sweep != 0)
		{
			return _instrDuty switch
			{
				0 => VoiceConfigType.PSGSquare12Sweep,
				1 => VoiceConfigType.PSGSquare25Sweep,
				2 => VoiceConfigType.PSGSquare50Sweep,
				_ => VoiceConfigType.PSGSquare75Sweep,
			};
		}
		else
		{
			return _instrDuty switch
			{
				0 => VoiceConfigType.PSGSquare12,
				1 => VoiceConfigType.PSGSquare25,
				2 => VoiceConfigType.PSGSquare50,
				_ => VoiceConfigType.PSGSquare75,
			};
		}
	}

	private ReadOnlySpan<float> ProcessSamples(int samplesRequired, float interStep)
	{
		Span<float> buffer = stackalloc float[samplesRequired];
		int samplesToFetch = samplesRequired;
		int i = 0;

		do
		{
			buffer[i++] = _pat[Pos] * 1.6f;
			InterPos += interStep;
			int posDelta = (int)InterPos;
			InterPos -= posDelta;
			Pos = (Pos + posDelta) & 0x7;
			// buffer[i++] = _pat[Pos++];
			// Pos %= 8;
		} while (--samplesToFetch > 0);
		return buffer.ToArray();
	}
	private bool SampleFetchCallback(ref List<float> fetchBuffer, int samplesRequired)
	{
		if (fetchBuffer.Count >= samplesRequired)
		{
			return true;
		}
		int samplesToFetch = samplesRequired - fetchBuffer.Count;

		do
		{
			fetchBuffer.Add(_pat[Pos++]);
			Pos %= 8;
		} while (--samplesToFetch > 0);
		return true;
	}

	internal static bool IsSweepEnabled(byte sweep)
	{
		return (sweep < 0x80) && (sweep & 0x7) != 0;
	}

	internal static bool IsSweepAscending(byte sweep)
	{
		return (sweep & 8) == 0;
	}

	internal static float SweepToCoefficent(byte sweep)
	{
		/* if sweep time is zero, don't change pitch -ipatix */
		int sweepTime = SweepTime(sweep);
		if (sweepTime == 0)
			return 1.0f;

		int shifts = sweep & 7;
		float stepCoeff;

		if (IsSweepAscending(sweep))
		{
			/* if ascending */
			stepCoeff = (128 + (128 >> shifts)) / 128.0f;
		}
		else
		{
			/* if descending */
			stepCoeff = (128 - (128 >> shifts)) / 128.0f;
		}

		/* convert the sweep pitch timer coefficient to the rate that agbplay runs at */
		float hardwareSweepRate = 128 / (float)sweepTime;
		float softwareSweepRate = GBAUtils.AGB_APPROX_FPS;
		float coeff = MathF.Pow(stepCoeff, hardwareSweepRate / softwareSweepRate);

		return coeff;
	}

	internal static float SweepToConvergence(byte sweep)
	{
		if (IsSweepAscending(sweep))
		{
			/* if ascending:
			 *
			 * Convergance is always at the maximum timer value to prevent hardware overflow -ipatix */
			return 2047.0f;
		}
		else
		{
			/* if descending:
			 *
			 * Because hardware calculates sweep with:
			 * timer -= timer >> shift
			 * the timer converges to the value which timer >> shift always results zero. -ipatix */
			int shifts = sweep & 7;
			return (1 << shifts) - 1;
		}
	}

	internal static byte SweepTime(byte sweep)
	{
		return (byte)((sweep & 0x70) >> 4);
	}
}
