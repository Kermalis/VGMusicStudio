using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal abstract class MP2KChannel
{
	internal MP2KTrack Track;

	internal MP2KTrack TrackOrg;

	internal MP2KResampler? Rs;

	internal NoteInfo Note;
	internal ADSR Env;
	internal EnvelopeState State = EnvelopeState.Initializing;

	internal long Pos = 0;
	internal float InterPos = 0.0f;
	internal float Freq = 0.0f;

	internal bool Stop = false;

	internal abstract void Process(Span<float> buffer, MixingArgs args);
	internal abstract void SetVolume(byte volume, sbyte panpot);
	internal abstract ChannelVolume GetVolume();
	internal abstract void SetPitch(short pitch);

	internal virtual void Release() { }
	internal virtual bool TickNote()
	{
		return false;
	}
	internal virtual VoiceConfigType GetVoiceType()
	{
		return VoiceConfigType.None;
	}

	internal MP2KChannel(MP2KTrack track, NoteInfo note, ADSR env)
	{
		Note = note;
		Env = env;
		Track = track;
		TrackOrg = track;

		track.Channels.Add(this);
	}

	~MP2KChannel()
	{
		RemoveFromTrack();
	}

	internal void RemoveFromTrack()
	{
		if (Track is null || Track.Channels is null)
		{
			return;
		}

		Track.Channels.Remove(this);
	}

	internal virtual bool IsReleasing()
	{
		return Stop;
	}

	internal void Kill()
	{
		State = EnvelopeState.Dead;
		RemoveFromTrack();
	}
}
