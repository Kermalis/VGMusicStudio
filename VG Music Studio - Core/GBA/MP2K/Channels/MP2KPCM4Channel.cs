using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KPCM4Channel : MP2KPSGChannel
{
	private float _dcCorrection100;
	private float _dcCorrection75;
	private float _dcCorrection50;
	private float _dcCorrection25;
	private byte[] _pcm4Buf = new byte[16];
	private readonly float[] _samples = new float[0x20];

	internal MP2KPCM4Channel(MP2KContext context, MP2KMixer mixer, MP2KTrack track, MP2KSample pcm4, ADSR env, NoteInfo note, bool useStairstep)
		: base(context, mixer, track, env, note, useStairstep)
	{
		// byte[] dummyPCM4 = new byte[16];
		// if (instrPCM4 < GBAUtils.CARTRIDGE_OFFSET)
		// {
		// 	_pcm4Buf = dummyPCM4;
		// }
		// else
		// {
		// 	instrPCM4 -= GBAUtils.CARTRIDGE_OFFSET;
		// 	if (instrPCM4 + 16 < MP2KEngine.MP2KInstance!.Config.ROM.Length)
		// 		_pcm4Buf = MP2KEngine.MP2KInstance!.Config.ROM.AsSpan()[instrPCM4..].ToArray();
		// 	else
		// 		_pcm4Buf = dummyPCM4;
		// }
		for (int i = 0; i < 16; i++)
		{
			_pcm4Buf[i] = (byte)pcm4.PCMData[i];
		}

		// MP2KUtils.PCM4ToFloat(_pcm4Buf, _sample);

		Rs = MP2KResampler.MakeResampler(ResamplerType.Nearest);

		float sum = 0.0f;
		for (int i = 0; i < 16; i++)
		{
			byte twoNibbles = _pcm4Buf[i];
			int nibbleA = twoNibbles >> 4;
			int nibbleB = twoNibbles & 0xF;
			sum += nibbleA / 16.0f;
			sum += nibbleB / 16.0f;
		}
		_dcCorrection100 = -sum * (1.0f / 32.0f);

		if (context.PlayerSoundMode.AccurateCh3Quantization)
		{
			sum = 0.0f;
			for (int i = 0; i < 16; i++)
			{
				byte twoNibbles = _pcm4Buf[i];
				int nibbleA = twoNibbles >> 4;
				int nibbleB = twoNibbles & 0xF;
				sum += ((nibbleA >> 2) + (nibbleA >> 1)) / 16.0f;
				sum += ((nibbleB >> 2) + (nibbleB >> 1)) / 16.0f;
			}
			_dcCorrection75 = -sum * (1.0f / 32.0f);

			sum = 0.0f;
			for (int i = 0; i < 16; i++)
			{
				byte twoNibbles = _pcm4Buf[i];
				int nibbleA = twoNibbles >> 4;
				int nibbleB = twoNibbles & 0xF;
				sum += (nibbleA >> 1) / 16.0f;
				sum += (nibbleB >> 1) / 16.0f;
			}
			_dcCorrection50 = -sum * (1.0f / 32.0f);

			sum = 0.0f;
			for (int i = 0; i < 16; i++)
			{
				byte twoNibbles = _pcm4Buf[i];
				int nibbleA = twoNibbles >> 4;
				int nibbleB = twoNibbles & 0xF;
				sum += (nibbleA >> 2) / 16.0f;
				sum += (nibbleB >> 2) / 16.0f;
			}
			_dcCorrection25 = -sum * (1.0f / 32.0f);
		}
	}

	internal override void SetPitch(short pitch)
	{
		Freq = 440.0f * 16.0f * MathF.Pow(2.0f, (Note.OriginalNote - 69) * (1.0f / 12.0f) + pitch * (1.0f / 768.0f));
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
			return;
		ChannelVolume vol = GetVolume();

		float lVolStep = (vol.ToVolLeft - vol.FromVolLeft) * args.SamplesPerBufferInv;
		float rVolStep = (vol.ToVolRight - vol.FromVolRight) * args.SamplesPerBufferInv;
		float lVol = vol.FromVolLeft;
		float rVol = vol.FromVolRight;
		float interStep = Freq * args.SampleRateInv;

		int bufPos = 0;

		// MP2KResampler.FetchCallback cb = new(SampleFetchCallback);
		// Rs!.Process(_mixer.TrackBuffers[Track.Index], interStep, cb);

		int samplesPerBuffer = _mixer.SamplesPerBuffer;
		MP2KUtils.PCM4ToFloat(_pcm4Buf, _samples);
		ReadOnlySpan<float> samples = ProcessSamples(interStep);

		for (int i = 0; i < samplesPerBuffer; i++)
		{
			InterPos += interStep;
			int posDelta = (int)InterPos;
			InterPos -= posDelta;
			Pos = (Pos + posDelta) & 0x1F;

			float samp = _samples[(int)Pos];
			buffer[bufPos++] += samp * lVol;
			buffer[bufPos++] += samp * rVol;
			lVol += lVolStep;
			rVol += rVolStep;
		}
	}

	internal override VoiceConfigType GetVoiceType()
	{
		return VoiceConfigType.PSGPCM4;
	}

	protected override bool IsChn3()
	{
		return true;
	}

	internal override ChannelVolume GetVolume()
	{
		var retval = base.GetVolume();

		if (!_context.PlayerSoundMode.AccurateCh3Volume)
		{
			return retval;
		}

		static float SnapFunc(float x)
		{
			if (x < 1.5f / 32.0f)
				return 0.0f / 32.0f;

			else if (x < 5.5f / 32.0f)
				return 4.0f / 32.0f;
			else if (x < 9.5f / 32.0f)
				return 8.0f / 32.0f;
			else if (x < 13.5f / 32.0f)
				return 12.0f / 32.0f;
			else
				return 16.0f / 32.0f;
		}

		retval.FromVolLeft = SnapFunc(retval.FromVolLeft);
		retval.FromVolRight = SnapFunc(retval.FromVolRight);
		retval.ToVolLeft = SnapFunc(retval.ToVolLeft);
		retval.ToVolRight = SnapFunc(retval.ToVolRight);
		return retval;
	}

	private ReadOnlySpan<float> ProcessSamples(float interStep)
	{
		Span<float> buffer = stackalloc float[32];
		int samplesToFetch = 32;
		int i = 0;

		if (_context.PlayerSoundMode.AccurateCh3Quantization)
		{
			ChannelVolume fade = GetVolume();
			float fromVol = Math.Max(fade.FromVolLeft, fade.FromVolRight);
			float toVol = Math.Max(fade.ToVolLeft, fade.ToVolRight);
			void MapFunc(float x, out float compensationScale, out float dcCorrection, out uint shiftA, out uint shiftB)
			{
				if (x < (6.0f / 32.0f))
				{
					shiftA = 2;
					shiftB = 4;
					compensationScale = 4.0f;
					dcCorrection = _dcCorrection25;
				}
				else if (x < (10.0f / 32.0f))
				{
					shiftA = 1;
					shiftB = 4;
					compensationScale = 2.0f;
					dcCorrection = _dcCorrection50;
				}
				else if (x < (14.0f / 32.0f))
				{
					shiftA = 1;
					shiftB = 2;
					compensationScale = 4.0f / 3.0f;
					dcCorrection = _dcCorrection75;
				}
				else
				{
					shiftA = 0;
					shiftB = 4;
					compensationScale = 1.0f;
					dcCorrection = _dcCorrection100;
				}
			}
			MapFunc(fromVol, out float compensationScaleFrom, out float dcCorrectionFrom, out uint shiftAFrom, out uint shiftBFrom);
			MapFunc(toVol, out float compensationScaleTo, out float dcCorrectionTo, out uint shiftATo, out uint shiftBTo);
			dcCorrectionFrom *= 16.0f;
			dcCorrectionTo *= 16.0f;

			float t = 0.0f;
			float t_inc = 1.0f / samplesToFetch;
			while (samplesToFetch-- > 0)
			{
				long pos = Pos++ % 32;
				byte nibble;
				if (pos % 2 == 0)
					nibble = (byte)(_pcm4Buf[pos / 2] >> (int)4u);
				else
					nibble = (byte)(_pcm4Buf[pos / 2] & 0xF);
				float sampleFrom = ((nibble >> (int)shiftAFrom) + (nibble >> (int)shiftBFrom) + dcCorrectionFrom)
								   * compensationScaleFrom;
				float sampleTo = ((nibble >> (int)shiftATo) + (nibble >> (int)shiftBTo) + dcCorrectionTo)
								 * compensationScaleTo;
				float sample = (sampleFrom + t * (sampleTo - sampleFrom)) * (1.0f / 16.0f);
				t += t_inc;
				buffer[i++] = sample * 1.6f;
			}
		}
		else
		{
			while (samplesToFetch-- > 0)
			{
				long pos = Pos++ % 32;
				byte nibble;
				if (pos % 2 == 0)
					nibble = (byte)(_pcm4Buf[pos / 2] >> (int)4u);
				else
					nibble = (byte)(_pcm4Buf[pos / 2] & 0xF);
				float sample = nibble * (1.0f / 16.0f) + _dcCorrection100;
				buffer[i++] = sample;
			}
		}

		return buffer.ToArray();
	}
	private bool SampleFetchCallback(ref List<float> fetchBuffer, int samplesRequired)
	{
		if (fetchBuffer.Count >= samplesRequired)
			return true;
		int samplesToFetch = samplesRequired - fetchBuffer.Count;
		int i = fetchBuffer.Count;

		if (_context.PlayerSoundMode.AccurateCh3Quantization)
		{
			ChannelVolume fade = GetVolume();
			float fromVol = Math.Max(fade.FromVolLeft, fade.FromVolRight);
			float toVol = Math.Max(fade.ToVolLeft, fade.ToVolRight);
			void mapFunc(float x, out float compensationScale, out float dcCorrection, out uint shiftA, out uint shiftB)
			{
				if (x < 6.0f / 32.0f)
				{
					shiftA = 2;
					shiftB = 4;
					compensationScale = 4.0f;
					dcCorrection = _dcCorrection25;
				}
				else if (x < 10.0f / 32.0f)
				{
					shiftA = 1;
					shiftB = 4;
					compensationScale = 2.0f;
					dcCorrection = _dcCorrection50;
				}
				else if (x < 14.0f / 32.0f)
				{
					shiftA = 1;
					shiftB = 2;
					compensationScale = 4.0f / 3.0f;
					dcCorrection = _dcCorrection75;
				}
				else
				{
					shiftA = 0;
					shiftB = 4;
					compensationScale = 1.0f;
					dcCorrection = _dcCorrection100;
				}
			}
			mapFunc(fromVol, out float compensationScaleFrom, out float dcCorrectionFrom, out uint shiftAFrom, out uint shiftBFrom);
			mapFunc(toVol, out float compensationScaleTo, out float dcCorrectionTo, out uint shiftATo, out uint shiftBTo);
			dcCorrectionFrom *= 16.0f;
			dcCorrectionTo *= 16.0f;

			float t = 0.0f;
			float t_inc = 1.0f / samplesToFetch;
			while (samplesToFetch-- > 0)
			{
				long pos = Pos++ % 32;
				byte nibble;
				if (pos % 2 == 0)
					nibble = (byte)(_pcm4Buf[pos / 2] >> (int)4u);
				else
					nibble = (byte)(_pcm4Buf[pos / 2] & 0xF);
				float sampleFrom = ((nibble >> (int)shiftAFrom) + (nibble >> (int)shiftBFrom) + dcCorrectionFrom)
								   * compensationScaleFrom;
				float sampleTo = ((nibble >> (int)shiftATo) + (nibble >> (int)shiftBTo) + dcCorrectionTo)
								 * compensationScaleTo;
				float sample = (sampleFrom + t * (sampleTo - sampleFrom)) * (1.0f / 16.0f);
				t += t_inc;
				fetchBuffer.Add(sample);
			}
		}
		else
		{
			while (samplesToFetch-- > 0)
			{
				long pos = Pos++ % 32;
				byte nibble;
				if (pos % 2 == 0)
					nibble = (byte)(_pcm4Buf[pos / 2] >> (int)4u);
				else
					nibble = (byte)(_pcm4Buf[pos / 2] & 0xF);
				float sample = nibble * (1.0f / 16.0f) + _dcCorrection100;
				fetchBuffer.Add(sample);
			}
		}

		return true;
	}
}
