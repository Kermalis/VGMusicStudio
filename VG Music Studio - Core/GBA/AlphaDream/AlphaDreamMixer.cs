using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.Core.Formats;
using System;
using NAudio.Wave;
using SoundFlow.Structs;
using SoundFlow.Enums;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamMixer : Mixer
{
	public readonly float SampleRateReciprocal;
	private readonly float _samplesReciprocal;
	internal override int SamplesPerBuffer { get; }
	private bool _isFading;
	private long _fadeMicroFramesLeft;
	private float _fadePos;
	private float _fadeStepPerMicroframe;

	public readonly AlphaDreamConfig Config;
	private readonly float[][] _trackBuffers = new float[AlphaDreamPlayer.NUM_TRACKS][];
	private readonly AudioBackend AlphaDreamPlaybackBackend;

	#region MiniAudio Fields
	// MiniAudio Fields
	private readonly float[]? _bufferMiniAudio;
	private readonly AudioFormat _formatSoundFlow;
    protected override AudioFormat SoundFlowFormat => _formatSoundFlow;
	#endregion

	#region PortAudio Fields
	// PortAudio Fields
	private readonly Audio? _audioPortAudio;
	private readonly Wave? _bufferPortAudio;
	#endregion

	#region NAudio Fields
	// NAudio Fields
	private readonly WaveBuffer? _audioNAudio;
	private readonly BufferedWaveProvider? _bufferNAudio;
	#endregion

	protected override WaveFormat WaveFormat => _bufferNAudio!.WaveFormat;

	internal AlphaDreamMixer(AlphaDreamConfig config)
	{
		Config = config;
		const int sampleRate = 13_379; // TODO: Actual value unknown
		SamplesPerBuffer = 224; // TODO
		SampleRateReciprocal = 1f / sampleRate;
		_samplesReciprocal = 1f / SamplesPerBuffer;

		int amt = SamplesPerBuffer * 2;
		for (int i = 0; i < AlphaDreamPlayer.NUM_TRACKS; i++)
		{
			_trackBuffers[i] = new float[amt];
		}
		AlphaDreamPlaybackBackend = PlaybackBackend;
		switch (PlaybackBackend)
		{
			case AudioBackend.MiniAudio:
				{
					_bufferMiniAudio = new float[amt];
					_formatSoundFlow = new AudioFormat
					{
						Channels = 2,
						SampleRate = sampleRate,
						Format = SampleFormat.F32
					};
					Init();
					break;
				}
			case AudioBackend.PortAudio:
				{
					_audioPortAudio = new Audio(amt * sizeof(float)) { Float32BufferCount = amt };
					_bufferPortAudio = new Wave()
					{
						DiscardOnBufferOverflow = true,
						BufferLength = SamplesPerBuffer * 64
					};
					_bufferPortAudio.CreateIeeeFloatWave(sampleRate, 2); // TODO

					Init(waveData: _bufferPortAudio);
					break;
				}
			case AudioBackend.NAudio:
				{
					_audioNAudio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
					for (int i = 0; i < AlphaDreamPlayer.NUM_TRACKS; i++)
					{
						_trackBuffers[i] = new float[amt];
					}
					_bufferNAudio = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2)) // TODO
					{
						DiscardOnBufferOverflow = true
					};
					Init(waveProvider: _bufferNAudio);
					break;
				}
		}
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

	internal void Process(AlphaDreamTrack[] tracks, bool output, bool recording)
	{
		switch (AlphaDreamPlaybackBackend)
		{
			case AudioBackend.PortAudio:
				{
					_audioPortAudio!.Clear();
					break;
				}
			case AudioBackend.NAudio:
				{
					_audioNAudio!.Clear();
					break;
				}
		}
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
			AlphaDreamTrack track = tracks[i];
			if (!track.IsEnabled || track.NoteDuration == 0 || track.Channel.Stopped || Mutes[i])
			{
				continue;
			}

			float level = masterLevel;
			float[] buf = _trackBuffers[i];
			Array.Clear(buf, 0, buf.Length);
			track.Channel.Process(buf);
			for (int j = 0; j < SamplesPerBuffer; j++)
			{
				switch (AlphaDreamPlaybackBackend)
				{
					case AudioBackend.MiniAudio:
						{
							_bufferMiniAudio![j * 2] += buf[j * 2] * level;
							_bufferMiniAudio[(j * 2) + 1] += buf[(j * 2) + 1] * level;
							break;
						}
					case AudioBackend.PortAudio:
						{
							_audioPortAudio!.Float32Buffer![j * 2] += buf[j * 2] * level;
							_audioPortAudio.Float32Buffer[(j * 2) + 1] += buf[(j * 2) + 1] * level;
							break;
						}
					case AudioBackend.NAudio:
						{
							_audioNAudio!.FloatBuffer![j * 2] += buf[j * 2] * level;
							_audioNAudio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1] * level;
							break;
						}
				}
				level += masterStep;
			}
		}
		if (output)
		{
			switch (AlphaDreamPlaybackBackend)
			{
				case AudioBackend.MiniAudio:
					{
						DataProvider!.AddSamples(_bufferMiniAudio);
						break;
					}
				case AudioBackend.PortAudio:
					{
						_bufferPortAudio!.AddSamples(_audioPortAudio!.ByteBuffer, 0, _audioPortAudio.ByteBufferCount);
						break;
					}
				case AudioBackend.NAudio:
					{
						_bufferNAudio!.AddSamples(_audioNAudio!.ByteBuffer, 0, _audioNAudio.ByteBufferCount);
						break;
					}
			}
		}
		if (recording)
		{
			switch (AlphaDreamPlaybackBackend)
			{
				case AudioBackend.MiniAudio:
					{
						_soundFlowEncoder!.Encode(_bufferMiniAudio);
						break;
					}
				case AudioBackend.PortAudio:
					{
						_waveWriterPortAudio!.Write(_audioPortAudio!.ByteBuffer, 0, _audioPortAudio.ByteBufferCount);
						break;
					}
				case AudioBackend.NAudio:
					{
						_waveWriterNAudio!.Write(_audioNAudio!.ByteBuffer, 0, _audioNAudio.ByteBufferCount);
						break;
					}
			}
		}
	}
}
