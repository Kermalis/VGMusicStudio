namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal abstract class MP2KChannel
{
	public EnvelopeState State;
	public MP2KTrack? Owner;
	protected readonly MP2KMixer _mixer;

	public NoteInfo Note;
	protected ADSR _adsr;
	protected int _instPan;

	protected byte _velocity;
	protected int _pos;
	protected float _interPos;
	protected float _frequency;

	protected MP2KChannel(MP2KMixer mixer)
	{
		_mixer = mixer;
		State = EnvelopeState.Dead;
	}

	public abstract ChannelVolume GetVolume();
	public abstract void SetVolume(byte vol, sbyte pan);
	public abstract void SetPitch(int pitch);
	public virtual void Release()
	{
		if (State < EnvelopeState.Releasing)
		{
			State = EnvelopeState.Releasing;
		}
	}

	public abstract void Process(float[] buffer);

	/// <summary>Returns whether the note is active or not</summary>
	public virtual bool TickNote()
	{
		if (State >= EnvelopeState.Releasing)
		{
			return false;
		}

		if (Note.Duration > 0)
		{
			Note.Duration--;
			if (Note.Duration == 0)
			{
				State = EnvelopeState.Releasing;
				return false;
			}
		}
		return true;
	}
	public void Stop()
	{
		State = EnvelopeState.Dead;
		Owner?.Channels.Remove(this);
		Owner = null;
	}
}