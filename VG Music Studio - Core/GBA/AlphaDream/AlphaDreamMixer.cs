using Kermalis.VGMusicStudio.Core.Util;
using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamMixer : Mixer
{
	public readonly float SampleRateReciprocal;
	private readonly float _samplesReciprocal;
	public readonly int SamplesPerBuffer;
	private bool _isFading;
	private long _fadeMicroFramesLeft;
	private float _fadePos;
	private float _fadeStepPerMicroframe;

	public readonly AlphaDreamConfig Config;
	private readonly WaveBuffer _audio;
	private readonly float[][] _trackBuffers = new float[AlphaDreamPlayer.NUM_TRACKS][];
	private readonly BufferedWaveProvider _buffer;

	internal AlphaDreamMixer(AlphaDreamConfig config)
	{
		Config = config;
		const int sampleRate = 13_379; // TODO: Actual value unknown
		SamplesPerBuffer = 224; // TODO
		SampleRateReciprocal = 1f / sampleRate;
		_samplesReciprocal = 1f / SamplesPerBuffer;

		int amt = SamplesPerBuffer * 2;
		_audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
		for (int i = 0; i < AlphaDreamPlayer.NUM_TRACKS; i++)
		{
			_trackBuffers[i] = new float[amt];
		}
		_buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2)) // TODO
		{
			DiscardOnBufferOverflow = true,
			BufferLength = SamplesPerBuffer * 64
		};
		Init(_buffer);
	}

	internal void BeginFadeIn()
	{
		_fadePos = 0f;
		_fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * GBAUtils.AGB_FPS);
		_fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
		_isFading = true;
	}
	internal void BeginFadeOut()
	{
		_fadePos = 1f;
		_fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * GBAUtils.AGB_FPS);
		_fadeStepPerMicroframe = -1f / _fadeMicroFramesLeft;
		_isFading = true;
	}
	internal bool IsFading()
	{
		return _isFading;
	}
	internal bool IsFadeDone()
	{
		return _isFading && _fadeMicroFramesLeft == 0;
	}
	internal void ResetFade()
	{
		_isFading = false;
		_fadeMicroFramesLeft = 0;
	}

	private WaveFileWriter? _waveWriter;
	public void CreateWaveWriter(string fileName)
	{
		_waveWriter = new WaveFileWriter(fileName, _buffer.WaveFormat);
	}
	public void CloseWaveWriter()
	{
		_waveWriter!.Dispose();
		_waveWriter = null;
	}
	internal void Process(Track[] tracks, bool output, bool recording)
	{
		_audio.Clear();
		float masterStep;
		float masterLevel;
		if (_isFading && _fadeMicroFramesLeft == 0)
		{
			masterStep = 0;
			masterLevel = 0;
		}
		else
		{
			float fromMaster = 1f;
			float toMaster = 1f;
			if (_fadeMicroFramesLeft > 0)
			{
				const float scale = 10f / 6f;
				fromMaster *= (_fadePos < 0f) ? 0f : MathF.Pow(_fadePos, scale);
				_fadePos += _fadeStepPerMicroframe;
				toMaster *= (_fadePos < 0f) ? 0f : MathF.Pow(_fadePos, scale);
				_fadeMicroFramesLeft--;
			}
			masterStep = (toMaster - fromMaster) * _samplesReciprocal;
			masterLevel = fromMaster;
		}
		for (int i = 0; i < AlphaDreamPlayer.NUM_TRACKS; i++)
		{
			Track track = tracks[i];
			if (!track.Enabled || track.NoteDuration == 0 || track.Channel.Stopped || Mutes[i])
			{
				continue;
			}

			float level = masterLevel;
			float[] buf = _trackBuffers[i];
			Array.Clear(buf, 0, buf.Length);
			track.Channel.Process(buf);
			for (int j = 0; j < SamplesPerBuffer; j++)
			{
				_audio.FloatBuffer[j * 2] += buf[j * 2] * level;
				_audio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1] * level;
				level += masterStep;
			}
		}
		if (output)
		{
			_buffer.AddSamples(_audio.ByteBuffer, 0, _audio.ByteBufferCount);
		}
		if (recording)
		{
			_waveWriter!.Write(_audio.ByteBuffer, 0, _audio.ByteBufferCount);
		}
	}
}
