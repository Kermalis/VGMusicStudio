using Kermalis.VGMusicStudio.Core.Formats;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using Kermalis.VGMusicStudio.Core.Util;
using NAudio.Wave;
using SoundFlow.Structs;
using SoundFlow.Enums;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEMixer : Mixer
{
	private const int NUM_CHANNELS = 0x20; // Actual value unknown for now

	private readonly float _samplesReciprocal;
	internal override int SamplesPerBuffer { get; }
	private bool _isFading;
	private long _fadeMicroFramesLeft;
	private float _fadePos;
	private float _fadeStepPerMicroframe;

	private readonly DSEChannel[] _channels;
	private readonly AudioBackend DSEPlaybackBackend;

	#region MiniAudio Fields
	// MiniAudio Fields
	private readonly AudioFormat _formatSoundFlow;
	protected override AudioFormat SoundFlowFormat => _formatSoundFlow;
	#endregion

	#region PortAudio Fields
	// PortAudio Fields
	private readonly Wave? _bufferPortAudio;
	#endregion

	#region NAudio Fields
	// NAudio Fields
	private readonly BufferedWaveProvider? _bufferNAudio;
	protected override WaveFormat? WaveFormat => _bufferNAudio!.WaveFormat;
	#endregion

	public DSEMixer()
	{
		// The sampling frequency of the mixer is 1.04876 MHz with an amplitude resolution of 24 bits, but the sampling frequency after mixing with PWM modulation is 32.768 kHz with an amplitude resolution of 10 bits.
		// - gbatek
		// I'm not using either of those because the samples per buffer leads to an overflow eventually
		const int sampleRate = 65_456;
		SamplesPerBuffer = 341; // TODO
		_samplesReciprocal = 1f / SamplesPerBuffer;

		_channels = new DSEChannel[NUM_CHANNELS];
		for (byte i = 0; i < NUM_CHANNELS; i++)
		{
			_channels[i] = new DSEChannel(i);
		}

		DSEPlaybackBackend = PlaybackBackend;
		switch (PlaybackBackend)
		{
			case AudioBackend.MiniAudio:
				{
					_formatSoundFlow = new AudioFormat
					{
						Channels = 2,
						SampleRate = sampleRate,
						Format = SoundFlow.Enums.SampleFormat.F32
					};
					Init();
					break;
				}
			case AudioBackend.PortAudio:
				{
					_bufferPortAudio = new Wave()
					{
						DiscardOnBufferOverflow = true,
						BufferLength = SamplesPerBuffer * 64,
					};
					_bufferPortAudio.CreateIeeeFloatWave(sampleRate, 2, 16);
					Init(waveData: _bufferPortAudio, PortAudio.SampleFormat.Int16);
					break;
				}
			case AudioBackend.NAudio:
				{
					_bufferNAudio = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, 2))
					{
						DiscardOnBufferOverflow = true
					};
					Init(waveProvider: _bufferNAudio);
					break;
				}
		}
	}

	internal DSEChannel? AllocateChannel()
	{
		static int GetScore(DSEChannel c)
		{
			// Free channels should be used before releasing channels
			return c.Owner is null ? -2 : DSEUtils.IsStateRemovable(c.State) ? -1 : 0;
		}
		DSEChannel? nChan = null;
		for (int i = 0; i < NUM_CHANNELS; i++)
		{
			DSEChannel c = _channels[i];
			if (nChan is null)
			{
				nChan = c;
			}
			else
			{
				int nScore = GetScore(nChan);
				int cScore = GetScore(c);
				if (cScore <= nScore && (cScore < nScore || c.Volume <= nChan.Volume))
				{
					nChan = c;
				}
			}
		}
		return nChan is not null && 0 >= GetScore(nChan) ? nChan : null;
	}

	internal void ChannelTick()
	{
		for (int i = 0; i < NUM_CHANNELS; i++)
		{
			DSEChannel chan = _channels[i];
			if (chan.Owner is null)
			{
				continue;
			}

			chan.Volume = (byte)chan.StepEnvelope();
			if (chan.NoteLength == 0 && !DSEUtils.IsStateRemovable(chan.State))
			{
				chan.SetEnvelopeRelease();
			}
			int vol = DSEUtils.SustainTable[chan.NoteVelocity] + DSEUtils.SustainTable[chan.Volume] + DSEUtils.SustainTable[chan.Owner.Volume] + DSEUtils.SustainTable[chan.Owner.Expression];
			//int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
			int pitch = ((chan.Key - chan.RootKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
			// switch (chan.Owner.LFOType)
			// {
			// 	case LFOType.Pitch: pitch += chan.LFOParam; break;
			// 	case LFOType.Volume: vol += chan.LFOParam; break;
			// 	case LFOType.Panpot: pan += chan.LFOParam; break;
			// }
			if (DSEUtils.IsStateRemovable(chan.State) && vol <= -92544)
			{
				chan.Stop();
			}
			else
			{
				chan.Volume = DSEUtils.GetChannelVolume(vol);
				chan.Panpot = chan.Owner.Panpot;
				chan.Timer = DSEUtils.GetChannelTimer(chan.BaseTimer, pitch);
			}
		}
	}

	internal void BeginFadeIn()
	{
		_fadePos = 0f;
		_fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
		_fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
		_isFading = true;
	}
	internal void BeginFadeOut()
	{
		_fadePos = 1f;
		_fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
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

	private readonly byte[] _b = new byte[4];
	private readonly float[] _f = new float[2];
	internal void Process(bool output, bool recording)
	{
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
		for (int i = 0; i < SamplesPerBuffer; i++)
		{
			int left = 0,
				right = 0;
			for (int j = 0; j < NUM_CHANNELS; j++)
			{
				DSEChannel chan = _channels[j];
				if (chan.Owner is null)
				{
					continue;
				}

				bool muted = Mutes[chan.Owner.Index]; // Get mute first because chan.Process() can call chan.Stop() which sets chan.Owner to null
				chan.Process(out short channelLeft, out short channelRight);
				if (!muted)
				{
					left += channelLeft;
					right += channelRight;
				}
			}
			float f = left * masterLevel;
			if (f < short.MinValue)
			{
				f = short.MinValue;
			}
			else if (f > short.MaxValue)
			{
				f = short.MaxValue;
			}
			left = (int)f;
			_b[0] = (byte)left;
			_b[1] = (byte)(left >> 8);
			_f[0] = left / (float)short.MaxValue;
			f = right * masterLevel;
			if (f < short.MinValue)
			{
				f = short.MinValue;
			}
			else if (f > short.MaxValue)
			{
				f = short.MaxValue;
			}
			right = (int)f;
			_b[2] = (byte)right;
			_b[3] = (byte)(right >> 8);
			_f[1] = right / (float)short.MaxValue;
			masterLevel += masterStep;
			if (output)
			{
				switch (DSEPlaybackBackend)
				{
					case AudioBackend.MiniAudio:
						{
							DataProvider!.AddSamples(_f);
							break;
						}
					case AudioBackend.PortAudio:
						{
							_bufferPortAudio!.AddSamples(_b, 0, 4);
							break;
						}
					case AudioBackend.NAudio:
						{
							_bufferNAudio!.AddSamples(_b, 0, 4);
							break;
						}
				}
			}
			if (recording)
			{
				switch (DSEPlaybackBackend)
				{
					case AudioBackend.MiniAudio:
						{
							_soundFlowEncoder!.Encode(_f);
							break;
						}
					case AudioBackend.PortAudio:
						{
							_waveWriterPortAudio!.Write(_b, 0, 4);
							break;
						}
					case AudioBackend.NAudio:
						{
							_waveWriterNAudio!.Write(_b, 0, 4);
							break;
						}
				}
			}
		}
	}
}
