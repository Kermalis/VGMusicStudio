namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal abstract class AlphaDreamChannel
{
	protected readonly AlphaDreamMixer _mixer;
	public EnvelopeState State;
	public byte Key;
	public bool Stopped;

	protected ADSR _adsr;

	protected byte _velocity;
	protected int _pos;
	protected float _interPos;
	protected float _frequency;
	protected byte _leftVol;
	protected byte _rightVol;

	protected AlphaDreamChannel(AlphaDreamMixer mixer)
	{
		_mixer = mixer;
	}

	public ChannelVolume GetVolume()
	{
		const float MAX = 1f / 0x10000;
		return new ChannelVolume
		{
			LeftVol = _leftVol * _velocity * MAX,
			RightVol = _rightVol * _velocity * MAX,
		};
	}
	public void SetVolume(byte vol, sbyte pan)
	{
		_leftVol = (byte)((vol * (-pan + 0x80)) >> 8);
		_rightVol = (byte)((vol * (pan + 0x80)) >> 8);
	}
	public abstract void SetPitch(int pitch);

	public abstract void Process(float[] buffer);
}