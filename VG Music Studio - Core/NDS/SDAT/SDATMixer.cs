using Kermalis.VGMusicStudio.Core.Formats;
using Kermalis.VGMusicStudio.Core.Util;
using NAudio.Wave;
using SoundFlow.Structs;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDATMixer : Mixer
{
	private readonly float _samplesReciprocal;
	internal override int SamplesPerBuffer { get; }
	private bool _isFading;
	private long _fadeMicroFramesLeft;
	private float _fadePos;
	private float _fadeStepPerMicroframe;

	internal SDATChannel[] Channels;
	private readonly AudioBackend SDATPlaybackBackend;

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
	private readonly BufferedWaveProvider? _bufferNAudio;
	protected override WaveFormat? WaveFormat => _bufferNAudio!.WaveFormat;
	#endregion

	internal SDATMixer()
	{
		// The sampling frequency of the mixer is 1.04876 MHz with an amplitude resolution of 24 bits, but the sampling frequency after mixing with PWM modulation is 32.768 kHz with an amplitude resolution of 10 bits.
		// - gbatek
		// I'm not using either of those because the samples per buffer leads to an overflow eventually
		const int sampleRate = 65456;
		SamplesPerBuffer = 341; // TODO
		_samplesReciprocal = 1f / SamplesPerBuffer;

		Channels = new SDATChannel[0x10];
		for (byte i = 0; i < 0x10; i++)
		{
			Channels[i] = new SDATChannel(i);
		}

		SDATPlaybackBackend = PlaybackBackend;
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
						BufferLength = SamplesPerBuffer * 64
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

	private static readonly int[] _pcmChanOrder = [4, 5, 6, 7, 2, 0, 3, 1, 8, 9, 10, 11, 14, 12, 15, 13];
	private static readonly int[] _psgChanOrder = [8, 9, 10, 11, 12, 13];
	private static readonly int[] _noiseChanOrder = [14, 15];
	internal SDATChannel? AllocateChannel(InstrumentType type, SDATTrack track)
	{
		Span<int> allowedChannels;
		switch (type)
		{
			case InstrumentType.PCM: allowedChannels = _pcmChanOrder; break;
			case InstrumentType.PSG: allowedChannels = _psgChanOrder; break;
			case InstrumentType.Noise: allowedChannels = _noiseChanOrder; break;
			default: return null;
		}
		SDATChannel? nChan = null;
		for (int i = 0; i < allowedChannels.Length; i++)
		{
			SDATChannel c = Channels[allowedChannels[i]];
			if (nChan is not null && c.Priority >= nChan.Priority && (c.Priority != nChan.Priority || nChan.Volume <= c.Volume))
			{
				continue;
			}
			nChan = c;
		}
		if (nChan is null || track.Priority < nChan.Priority)
		{
			return null;
		}
		return nChan;
	}

	internal void ChannelTick()
	{
		for (int i = 0; i < 0x10; i++)
		{
			SDATChannel chan = Channels[i];
			if (chan.Owner is null)
			{
				continue;
			}

			chan.StepEnvelope();
			if (chan.NoteDuration == 0 && !chan.Owner.WaitingForNoteToFinishBeforeContinuingXD)
			{
				chan.Priority = 1;
				chan.State = EnvelopeState.Release;
			}
			int vol = SDATUtils.SustainTable[chan.NoteVelocity] + chan.Velocity + chan.Owner.GetVolume();
			int pitch = ((chan.Note - chan.BaseNote) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
			int pan = 0;
			chan.LFOTick();
			switch (chan.LFOType)
			{
				case LFOType.Pitch: pitch += chan.LFOParam; break;
				case LFOType.Volume: vol += chan.LFOParam; break;
				case LFOType.Panpot: pan += chan.LFOParam; break;
			}
			if (chan.State == EnvelopeState.Release && vol <= -92544)
			{
				chan.Stop();
			}
			else
			{
				chan.Volume = SDATUtils.GetChannelVolume(vol);
				chan.Timer = SDATUtils.GetChannelTimer(chan.BaseTimer, pitch);
				int p = chan.StartingPan + chan.Owner.GetPan() + pan;
				if (p < -0x40)
				{
					p = -0x40;
				}
				else if (p > 0x3F)
				{
					p = 0x3F;
				}
				chan.Pan = (sbyte)p;
			}
		}
	}

	internal void BeginFadeIn()
	{
		_fadePos = 0f;
		_fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1_000.0 * 192);
		_fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
		_isFading = true;
	}
	internal void BeginFadeOut()
	{
		_fadePos = 1f;
		_fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1_000.0 * 192);
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

	internal void EmulateProcess()
	{
		for (int i = 0; i < SamplesPerBuffer; i++)
		{
			for (int j = 0; j < 0x10; j++)
			{
				SDATChannel chan = Channels[j];
				if (chan.Owner is not null)
				{
					chan.EmulateProcess();
				}
			}
		}
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
			for (int j = 0; j < 0x10; j++)
			{
				SDATChannel chan = Channels[j];
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
				switch (SDATPlaybackBackend)
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
				switch (SDATPlaybackBackend)
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
