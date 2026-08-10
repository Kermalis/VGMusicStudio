using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal abstract class MP2KPSGChannel : MP2KChannel
{
	protected enum PSGPan : byte
	{
		Left,
		Center,
		Right,
	}

	protected MP2KContext _context;
	protected MP2KMixer _mixer;
	private byte _envInterStep;
	private byte _envFrameCount = 0;
	private float _envFadeLevel = 0.0f;
	protected byte _envLevelCur;
	private byte _envPeak;
	private byte _envSustain;
	protected PSGPan _panpotCurrent = PSGPan.Center;
	protected PSGPan _panpotPrev = PSGPan.Center;
	protected ChannelVolume _channelVolume = new();

	protected bool _useStairstep;
	protected bool _fastRelease;
	protected ushort _volume = 0;
	protected short _panpot = 0;
	protected bool _playingVolBugUpdate = false;

	protected ushort _psgLengthCount = 0;
	protected bool _psgLengthActive = false;

	public MP2KPSGChannel(MP2KContext context, MP2KMixer mixer, MP2KTrack track, ADSR env, NoteInfo note, bool useStairstep = false)
		: base(track, note, env)
	{
		_context = context;
		_mixer = mixer;
		_useStairstep = useStairstep;
		Env.A = (byte)(env.A & 0x7);
		Env.D = (byte)(env.D & 0x7);
		Env.S = (byte)(env.S & 0xF);
		Env.R = (byte)(env.R & 0x7);

		if (note.PSGLength > 0)
		{
			byte invertedLength = (byte)(64 - (note.PSGLength & 0x3F));
			_psgLengthCount = (ushort)((invertedLength * GBAUtils.AGB_APPROX_FPS * GBAUtils.INTERFRAMES + 128) / 256);
			_psgLengthActive = true;
		}
	}

	internal override void SetVolume(byte vol, sbyte pan)
	{
		if (Stop)
		{
			return;
		}

		_volume = vol;
		_panpot = Math.Clamp(_panpot, (short)-128, (short)127);
		_playingVolBugUpdate = true;
	}
	internal override ChannelVolume GetVolume()
	{
		return _channelVolume;
	}

	public byte GetPseudoEchoLevel()
	{
		return (byte)(((_envPeak * Note.PseudoEchoVolume) + 0xFF) >> 8);
	}

	internal static float TimerToFrequency(float timer)
	{
		return 131072.0f / (float)(2048.0f - timer);
	}

	internal static float FrequencyToTimer(float freq)
	{
		return 2048.0f - MathF.Min(131072.0f / freq, 2047.0f);
	}

	internal override void Release()
	{
		Release(false);
	}
	public void Release(bool fastRelease)
	{
		Stop = true;
		_fastRelease = fastRelease;
	}
	internal override bool TickNote()
	{
		if (State < EnvelopeState.Releasing)
		{
			if (Note.Duration > 0)
			{
				Note.Duration--;
				if (Note.Duration == 0)
				{
					Release(false);
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public bool IsFastReleasing()
	{
		return _fastRelease;
	}

	protected virtual bool IsChn3()
	{
		return false;
	}

	protected void StepEnvelope()
	{
		// void BeginDecay()
		// {
		// 	State = EnvelopeState.Decaying;
		// 	_envFrameCount = Env.D;
		// 	if (_envPeak == 0 || _envFrameCount == 0 || _envPeak == _envSustain)
		// 	{
		// 		BeginSustain();
		// 	}
		// 	else
		// 	{
		// 		_envLevelCur = _envPeak;
		// 	}
		// }
		// void BeginSustain()
		// {
		// 	if (Env.S == 0)
		// 	{
		// 		State = EnvelopeState.Releasing;
		// 		BeginPseudoEcho();
		// 	}
		// 	else
		// 	{
		// 		State = EnvelopeState.Playing;
		// 		_envLevelCur = _envSustain;
		// 		SustainState();
		// 	}
		// }
		// void BeginPseudoEcho()
		// {
		// 	_envFrameCount = 1;
		// 	_envLevelCur = GetPseudoEchoLevel();
		// 	if (_envLevelCur != 0 && Note.PseudoEchoLength != 0)
		// 	{
		// 		State = EnvelopeState.PseudoEcho;
		// 	}
		// 	else
		// 	{
		// 		State = EnvelopeState.Dying;
		// 		_envInterStep = GBAUtils.INTERFRAMES - 1;
		// 		return;
		// 	}
		// }
		void SustainState()
		{
			_envFrameCount = 7;
			if (IsChn3())
			{
				_envLevelCur = _envSustain;
			}
		}

		if (State == EnvelopeState.Initializing)
		{
			if (Stop)
			{
				State = EnvelopeState.Dead;
				return;
			}

			ApplyVolume();
			_panpotPrev = _panpotCurrent;

			_envInterStep = 0;

			_envLevelCur = 0;
			_envFrameCount = Env.A;
			State = EnvelopeState.Rising;

			if (_envFrameCount > 0)
			{
				_envFadeLevel = 0.0f;
				return;
			}
			else
			{
				if (Env.D > 0)
				{
					_envFadeLevel = _envPeak;
				}
				else if (_envSustain > 0)
				{
					_envFadeLevel = _envSustain;
				}
				else if (GetPseudoEchoLevel() > 0)
				{
					_envFadeLevel = GetPseudoEchoLevel();
				}
				State = EnvelopeState.Decaying;
				_envFrameCount = Env.D;
				if (_envPeak == 0 || _envFrameCount == 0 || _envPeak == _envSustain)
				{
					if (Env.S == 0)
					{
						State = EnvelopeState.Releasing;
						_envFrameCount = 1;
						_envLevelCur = GetPseudoEchoLevel();
						if (_envLevelCur != 0 && Note.PseudoEchoLength != 0)
						{
							State = EnvelopeState.PseudoEcho;
						}
						else
						{
							State = EnvelopeState.Dying;
							_envInterStep = GBAUtils.INTERFRAMES - 1;
							return;
						}
					}
					else
					{
						State = EnvelopeState.Playing;
						_envLevelCur = _envSustain;
						SustainState();
					}
					_envFrameCount = Env.D;
				}
				else
				{
					_envLevelCur = _envPeak;
				}
			}
		}
		else
		{
			if (_psgLengthActive && _psgLengthCount > 0)
			{
				_psgLengthCount--;
				if (_psgLengthCount is 0)
				{
					Release(true);
				}
			}
			if (_fastRelease && State != EnvelopeState.Dying)
			{
				if (Env.R == 0 || State == EnvelopeState.PseudoEcho)
				{
					_envInterStep = GBAUtils.INTERFRAMES - 1;
				}
				else
				{
					_envInterStep = 0;
				}

				State = EnvelopeState.Dying;
				_envFrameCount = 1;
				return;
			}

			if (++_envInterStep < GBAUtils.INTERFRAMES)
			{
				return;
			}

			_envInterStep = 0;

			_envFrameCount--;
		}

		if (State == EnvelopeState.PseudoEcho)
		{
			_envFrameCount = 1;
			if (--Note.PseudoEchoLength == 0)
			{
				State = EnvelopeState.Dying;
				_envInterStep = GBAUtils.INTERFRAMES - 1;
			}
		}
		else if (Stop && State < EnvelopeState.Releasing)
		{
			State = EnvelopeState.Releasing;
			_envFrameCount = Env.R;
			if (_envLevelCur == 0 || _envFrameCount == 0)
			{
				_envFrameCount = 1;
				_envLevelCur = GetPseudoEchoLevel();
				if (_envLevelCur != 0 && Note.PseudoEchoLength != 0)
				{
					State = EnvelopeState.PseudoEcho;
				}
				else
				{
					State = EnvelopeState.Dying;
					_envInterStep = GBAUtils.INTERFRAMES - 1;
					return;
				}
			}
			else
			{
				return;
			}
		}
		else if (_envFrameCount == 0)
		{
			ApplyVolume();

			if (State == EnvelopeState.Releasing)
			{
				_envLevelCur--;

				if (_envLevelCur == 0)
				{
					_envFrameCount = 1;
					_envLevelCur = GetPseudoEchoLevel();
					if (_envLevelCur != 0 && Note.PseudoEchoLength != 0)
					{
						State = EnvelopeState.PseudoEcho;
					}
					else
					{
						State = EnvelopeState.Dying;
						_envInterStep = GBAUtils.INTERFRAMES - 1;
						return;
					}
				}
				else
				{
					_envFrameCount = Env.R;
				}
			}
			else if (State == EnvelopeState.Playing)
			{
				SustainState();
			}
			else if (State == EnvelopeState.Decaying)
			{
				_envLevelCur--;

				if (_envLevelCur <= _envSustain)
				{
					if (Env.S == 0)
					{
						State = EnvelopeState.Releasing;
						_envFrameCount = 1;
						_envLevelCur = GetPseudoEchoLevel();
						if (_envLevelCur != 0 && Note.PseudoEchoLength != 0)
						{
							State = EnvelopeState.PseudoEcho;
						}
						else
						{
							State = EnvelopeState.Dying;
							_envInterStep = GBAUtils.INTERFRAMES - 1;
							return;
						}
					}
					else
					{
						State = EnvelopeState.Playing;
						_envLevelCur = _envSustain;
						SustainState();
					}
				}
				_envFrameCount = Env.D;
			}
			else if (State == EnvelopeState.Rising)
			{
				_envLevelCur++;

				if (_envLevelCur >= _envPeak)
				{
					State = EnvelopeState.Decaying;
					_envFrameCount = Env.D;
					if (_envPeak == 0 || _envFrameCount == 0 || _envPeak == _envSustain)
					{
						if (Env.S == 0)
						{
							State = EnvelopeState.Releasing;
							_envFrameCount = 1;
							_envLevelCur = GetPseudoEchoLevel();
							if (_envLevelCur != 0 && Note.PseudoEchoLength != 0)
							{
								State = EnvelopeState.PseudoEcho;
							}
							else
							{
								State = EnvelopeState.Dying;
								_envInterStep = GBAUtils.INTERFRAMES - 1;
								return;
							}
						}
						else
						{
							State = EnvelopeState.Playing;
							_envLevelCur = _envSustain;
							SustainState();
						}
						_envFrameCount = Env.D;
					}
					else
					{
						_envLevelCur = _envPeak;
					}
				}
				else
				{
					_envFrameCount = Env.A;
				}
			}
			else if (State == EnvelopeState.Dying)
			{
				State = EnvelopeState.Dead;
				return;
			}
		}
	}

	protected void UpdateVolumeFade()
	{
		int fadeInterframesCount = _envFrameCount * GBAUtils.INTERFRAMES - _envInterStep;

		byte _fadeVelocityTo = 0xFF;
		switch (State)
		{
			case EnvelopeState.Initializing:
				break;
			case EnvelopeState.Rising:
				_fadeVelocityTo = (byte)(_envLevelCur + 1);
				break;
			case EnvelopeState.Decaying:
			case EnvelopeState.Releasing:
				_fadeVelocityTo = (byte)(_envLevelCur - 1);
				break;
			case EnvelopeState.Playing:
			case EnvelopeState.PseudoEcho:
				_fadeVelocityTo = _envLevelCur;
				fadeInterframesCount = 1;
				break;
			case EnvelopeState.Dying:
				_fadeVelocityTo = 0;
				break;
			case EnvelopeState.Dead:
				break;
		}

		float fadeVelocityNew;
		if (_useStairstep)
		{
			if (fadeInterframesCount == 1)
			{
				fadeVelocityNew = _fadeVelocityTo;
			}
			else
			{
				fadeVelocityNew = _envFadeLevel;
			}
		}
		else
		{
			fadeVelocityNew = _envFadeLevel + (_fadeVelocityTo - _envFadeLevel) / fadeInterframesCount;
		}

		_channelVolume.FromVolLeft = (_panpotPrev == PSGPan.Right) ? 0.0f : _envFadeLevel * (1.0f / 32.0f);
		_channelVolume.FromVolRight = (_panpotPrev == PSGPan.Left) ? 0.0f : _envFadeLevel * (1.0f / 32.0f);
		_channelVolume.ToVolLeft = (_panpotCurrent == PSGPan.Right) ? 0.0f : fadeVelocityNew * (1.0f / 32.0f);
		_channelVolume.ToVolRight = (_panpotCurrent == PSGPan.Left) ? 0.0f : fadeVelocityNew * (1.0f / 32.0f);

		_panpotPrev = _panpotCurrent;
		_envFadeLevel = fadeVelocityNew;
	}

	protected void ApplyVolume()
	{
		int trkVolML = ((127 - _panpot) * _volume) >> 8;
		int trkVolMR = ((_panpot + 128) * _volume) >> 8;
		int chnVolL = ((127 - Note.RhythmPan) * Note.Velocity * trkVolML) >> 14;
		int chnVolR = ((Note.RhythmPan + 128) * Note.Velocity * trkVolMR) >> 14;

		if ((chnVolR / 2) >= chnVolL)
		{
			_panpotCurrent = PSGPan.Right;
		}
		else if ((chnVolL / 2) >= chnVolR)
		{
			_panpotCurrent = PSGPan.Left;
		}
		else
		{
			_panpotCurrent = PSGPan.Center;
		}

		if (!IsChn3() && _playingVolBugUpdate && State == EnvelopeState.Playing)
		{
			_envLevelCur = _envSustain;
			_playingVolBugUpdate = false;
		}

		_envPeak = (byte)Math.Clamp((chnVolL + chnVolR) >> 4, 0, 15);
		_envSustain = (byte)Math.Clamp((_envPeak * Env.S + 15) >> 4, 0, 15);
	}
}