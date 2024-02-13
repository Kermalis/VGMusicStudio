namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal abstract class MP2KPSGChannel : MP2KChannel
{
	protected enum GBPan : byte
	{
		Left,
		Center,
		Right,
	}

	private byte _processStep;
	private EnvelopeState _nextState;
	private byte _peakVelocity;
	private byte _sustainVelocity;
	protected GBPan _panpot = GBPan.Center;

	public MP2KPSGChannel(MP2KMixer mixer)
		: base(mixer)
	{
		//
	}
	protected void Init(MP2KTrack owner, NoteInfo note, ADSR env, int instPan)
	{
		State = EnvelopeState.Initializing;
		Owner?.Channels.Remove(this);
		Owner = owner;
		Owner.Channels.Add(this);
		Note = note;
		_adsr.A = (byte)(env.A & 0x7);
		_adsr.D = (byte)(env.D & 0x7);
		_adsr.S = (byte)(env.S & 0xF);
		_adsr.R = (byte)(env.R & 0x7);
		_instPan = instPan;
	}

	public override void Release()
	{
		if (State < EnvelopeState.Releasing)
		{
			if (_adsr.R == 0)
			{
				_velocity = 0;
				Stop();
			}
			else if (_velocity == 0)
			{
				Stop();
			}
			else
			{
				_nextState = EnvelopeState.Releasing;
			}
		}
	}
	public override bool TickNote()
	{
		if (State >= EnvelopeState.Releasing)
		{
			return false;
		}
		if (Note.Duration <= 0)
		{
			return true;
		}

		Note.Duration--;
		if (Note.Duration == 0)
		{
			if (_velocity == 0)
			{
				Stop();
			}
			else
			{
				State = EnvelopeState.Releasing;
			}
			return false;
		}
		return true;
	}

	public override ChannelVolume GetVolume()
	{
		const float MAX = 0x20;
		return new ChannelVolume
		{
			LeftVol = _panpot == GBPan.Right ? 0 : _velocity / MAX,
			RightVol = _panpot == GBPan.Left ? 0 : _velocity / MAX
		};
	}
	public override void SetVolume(byte vol, sbyte pan)
	{
		int combinedPan = pan + _instPan;
		if (combinedPan > 63)
		{
			combinedPan = 63;
		}
		else if (combinedPan < -64)
		{
			combinedPan = -64;
		}
		if (State < EnvelopeState.Releasing)
		{
			_panpot = combinedPan < -21 ? GBPan.Left : combinedPan > 20 ? GBPan.Right : GBPan.Center;
			_peakVelocity = (byte)((Note.Velocity * vol) >> 10);
			_sustainVelocity = (byte)(((_peakVelocity * _adsr.S) + 0xF) >> 4); // TODO
			if (State == EnvelopeState.Playing)
			{
				_velocity = _sustainVelocity;
			}
		}
	}

	protected void StepEnvelope()
	{
		void dec()
		{
			_processStep = 0;
			if (_velocity - 1 <= _sustainVelocity)
			{
				_velocity = _sustainVelocity;
				_nextState = EnvelopeState.Playing;
			}
			else if (_velocity != 0)
			{
				_velocity--;
			}
		}
		void sus()
		{
			_processStep = 0;
		}
		void rel()
		{
			if (_adsr.R == 0)
			{
				_velocity = 0;
				Stop();
			}
			else
			{
				_processStep = 0;
				if (_velocity - 1 <= 0)
				{
					_nextState = EnvelopeState.Dying;
					_velocity = 0;
				}
				else
				{
					_velocity--;
				}
			}
		}

		switch (State)
		{
			case EnvelopeState.Initializing:
			{
				_nextState = EnvelopeState.Rising;
				_processStep = 0;
				if ((_adsr.A | _adsr.D) == 0 || (_sustainVelocity == 0 && _peakVelocity == 0))
				{
					State = EnvelopeState.Playing;
					_velocity = _sustainVelocity;
					return;
				}
				else if (_adsr.A == 0 && _adsr.S < 0xF)
				{
					State = EnvelopeState.Decaying;
					int next = _peakVelocity - 1;
					if (next < 0)
					{
						next = 0;
					}
					_velocity = (byte)next;
					if (_velocity < _sustainVelocity)
					{
						_velocity = _sustainVelocity;
					}
					return;
				}
				else if (_adsr.A == 0)
				{
					State = EnvelopeState.Playing;
					_velocity = _sustainVelocity;
					return;
				}
				else
				{
					State = EnvelopeState.Rising;
					_velocity = 1;
					return;
				}
			}
			case EnvelopeState.Rising:
			{
				if (++_processStep >= _adsr.A)
				{
					if (_nextState == EnvelopeState.Decaying)
					{
						State = EnvelopeState.Decaying;
						dec(); return;
					}
					if (_nextState == EnvelopeState.Playing)
					{
						State = EnvelopeState.Playing;
						sus(); return;
					}
					if (_nextState == EnvelopeState.Releasing)
					{
						State = EnvelopeState.Releasing;
						rel(); return;
					}
					_processStep = 0;
					if (++_velocity >= _peakVelocity)
					{
						if (_adsr.D == 0)
						{
							_nextState = EnvelopeState.Playing;
						}
						else if (_peakVelocity == _sustainVelocity)
						{
							_nextState = EnvelopeState.Playing;
							_velocity = _peakVelocity;
						}
						else
						{
							_velocity = _peakVelocity;
							_nextState = EnvelopeState.Decaying;
						}
					}
				}
				break;
			}
			case EnvelopeState.Decaying:
			{
				if (++_processStep >= _adsr.D)
				{
					if (_nextState == EnvelopeState.Playing)
					{
						State = EnvelopeState.Playing;
						sus(); return;
					}
					if (_nextState == EnvelopeState.Releasing)
					{
						State = EnvelopeState.Releasing;
						rel(); return;
					}
					dec();
				}
				break;
			}
			case EnvelopeState.Playing:
			{
				if (++_processStep >= 1)
				{
					if (_nextState == EnvelopeState.Releasing)
					{
						State = EnvelopeState.Releasing;
						rel(); return;
					}
					sus();
				}
				break;
			}
			case EnvelopeState.Releasing:
			{
				if (++_processStep >= _adsr.R)
				{
					if (_nextState == EnvelopeState.Dying)
					{
						Stop();
						return;
					}
					rel();
				}
				break;
			}
		}
	}
}