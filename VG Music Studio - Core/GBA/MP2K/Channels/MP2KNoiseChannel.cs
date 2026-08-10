using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KNoiseChannel : MP2KPSGChannel
{
	private BitArray _pat;
	// private readonly MP2KResampler _srs;
	private readonly int _instrNp;
	private ushort _noiseState;
	private readonly ushort _noiseLfsrMask;

	internal MP2KNoiseChannel(MP2KContext context, MP2KMixer mixer, MP2KTrack track, int instrNp, ADSR env, NoteInfo note)
		: base(context, mixer, track, env, note)
	{
		_instrNp = instrNp;
		_pat = (NoisePattern)instrNp == NoisePattern.Fine ? MP2KUtils.NoiseFine : MP2KUtils.NoiseRough;
		Rs = MP2KResampler.MakeResampler(ResamplerType.Nearest);
		// _srs = MP2KResampler.MakeResampler(ResamplerType.Sinc);
		if ((instrNp & 0x1) == 0)
		{
			_noiseState = 0x4000;
			_noiseLfsrMask = 0x6000;
		}
		else
		{
			_noiseState = 0x40;
			_noiseLfsrMask = 0x60;
		}
	}

	internal override void SetPitch(short pitch)
	{
		// int key = Note.Note + (int)MathF.Round(pitch / 64f);
		// if (key <= 20)
		// {
		// 	key = 0;
		// }
		// else
		// {
		// 	key -= 21;
		// 	if (key > 59)
		// 	{
		// 		key = 59;
		// 	}
		// }
		// byte v = MP2KUtils.NoiseFrequencyTable[key];
		// // The following emulates 0x0400007C - SOUND4CNT_H
		// int r = v & 7; // Bits 0-2
		// int s = v >> 4; // Bits 4-7
		// Freq = 524_288f / (r == 0 ? 0.5f : r) / MathF.Pow(2, s + 1);

		float fkey = Note.OriginalNote + pitch * (1.0f / 64.0f);
		float noisefreq;
		if (fkey < 76.0f)
			noisefreq = 4096.0f * MathF.Pow(8.0f, (fkey - 60.0f) * (1.0f / 12.0f));
		else if (fkey < 78.0f)
			noisefreq = 65536.0f * MathF.Pow(2.0f, (fkey - 76.0f) * (1.0f / 2.0f));
		else if (fkey < 80.0f)
			noisefreq = 131072.0f * MathF.Pow(2.0f, fkey - 78.0f);
		else
			noisefreq = 524288.0f;

		Freq = Math.Max(4.5714f, noisefreq);
	}

	internal override void Process(Span<float> buffer, MixingArgs args)
	{
		if (State == EnvelopeState.Dead)
			return;
		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		UpdateVolumeFade();

		if (buffer.Length == 0)
			return;

		float[] noiseFreqs = [32768.0f, 65536.0f, 131072.0f, 262144.0f];
		float noiseFreq = noiseFreqs[_context.MP2KSoundMode.DACConfig % noiseFreqs.Length];

		ChannelVolume vol = GetVolume();
		float lVolStep = (vol.ToVolLeft - vol.FromVolLeft) * args.SamplesPerBufferInv;
		float rVolStep = (vol.ToVolRight - vol.FromVolRight) * args.SamplesPerBufferInv;
		float lVol = vol.FromVolLeft;
		float rVol = vol.FromVolRight;
		float interStep = Freq / noiseFreq;

		bool CBNearest(ref List<float> fetchBuffer, int samplesRequired)
		{
			if (fetchBuffer.Count >= samplesRequired)
				return true;
			int samplesToFetch = samplesRequired - fetchBuffer.Count;
			int i = fetchBuffer.Count;
			MP2KResampler.FetchCallback cbNearest = new(SampleFetchCallback);
			return Rs!.Process(fetchBuffer[i..samplesToFetch].ToArray(), interStep, cbNearest);
		}

		// Rs!.Process(_mixer.TrackBuffers[Track.Index], noiseFreq / _context.SampleRate, CBNearest);

		int bufPos = 0;
		interStep = Freq * _mixer!.SampleRateReciprocal;
		int samplesPerBuffer = _mixer!.SamplesPerBuffer;
		ReadOnlySpan<float> samples = ProcessSamples(samplesPerBuffer);
		for (int i = 0; i < samplesPerBuffer; i++)
		{
			float samp = samples[i];
			buffer[bufPos++] += samp * lVol;
			buffer[bufPos++] += samp * rVol;
			lVol += lVolStep;
			rVol += rVolStep;
		}
	}

	internal override VoiceConfigType GetVoiceType()
	{
		if (_instrNp == 0x0)
			return VoiceConfigType.PSGNoise15;
		else
			return VoiceConfigType.PSGNoise7;
	}

	private ReadOnlySpan<float> ProcessSamples(int samplesRequired)
	{
		Span<float> buffer = stackalloc float[samplesRequired];
		int samplesToFetch = samplesRequired;
		int i = 0;

		do
		{
			float sample;
			if ((_noiseState & 1) != 0)
			{
				sample = 0.5f;
				_noiseState >>= 1;
				_noiseState ^= _noiseLfsrMask;
			}
			else
			{
				sample = -0.5f;
				_noiseState >>= 1;
			}
			buffer[i++] = sample;
		} while (--samplesToFetch > 0);

		return buffer.ToArray();
	}
	private bool SampleFetchCallback(ref List<float> fetchBuffer, int samplesRequired)
	{
		if (fetchBuffer.Count >= samplesRequired)
			return true;
		int samplesToFetch = samplesRequired - fetchBuffer.Count;
		int i = fetchBuffer.Count;

		do
		{
			float sample;
			if ((_noiseState & 1) != 0)
			{
				sample = 0.5f;
				_noiseState >>= 1;
				_noiseState ^= _noiseLfsrMask;
			}
			else
			{
				sample = -0.5f;
				_noiseState >>= 1;
			}
			fetchBuffer.Add(sample);
		} while (--samplesToFetch > 0);

		return true;
	}
}