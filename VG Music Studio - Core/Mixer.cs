using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core;

public abstract class Mixer : IAudioSessionEventsHandler, IDisposable
{
	public static event Action<float>? VolumeChanged;

	public readonly bool[] Mutes;
	private IWavePlayer _out;
	private AudioSessionControl _appVolume;

	private bool _shouldSendVolUpdateEvent = true;

	protected WaveFileWriter? _waveWriter;
	protected abstract WaveFormat WaveFormat { get; }

	protected Mixer()
	{
		Mutes = new bool[SongState.MAX_TRACKS];
	}

	protected void Init(IWaveProvider waveProvider)
	{
		_out = new WasapiOut();
		_out.Init(waveProvider);
		using (var en = new MMDeviceEnumerator())
		{
			SessionCollection sessions = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).AudioSessionManager.Sessions;
			int id = Environment.ProcessId;
			for (int i = 0; i < sessions.Count; i++)
			{
				AudioSessionControl session = sessions[i];
				if (session.GetProcessID == id)
				{
					_appVolume = session;
					_appVolume.RegisterEventClient(this);
					break;
				}
			}
		}
		_out.Play();
	}

	public void CreateWaveWriter(string fileName)
	{
		_waveWriter = new WaveFileWriter(fileName, WaveFormat);
	}
	public void CloseWaveWriter()
	{
		_waveWriter!.Dispose();
		_waveWriter = null;
	}

	public void OnVolumeChanged(float volume, bool isMuted)
	{
		if (_shouldSendVolUpdateEvent)
		{
			VolumeChanged?.Invoke(volume);
		}
		_shouldSendVolUpdateEvent = true;
	}
	public void OnDisplayNameChanged(string displayName)
	{
		throw new NotImplementedException();
	}
	public void OnIconPathChanged(string iconPath)
	{
		throw new NotImplementedException();
	}
	public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex)
	{
		throw new NotImplementedException();
	}
	public void OnGroupingParamChanged(ref Guid groupingId)
	{
		throw new NotImplementedException();
	}
	// Fires on @out.Play() and @out.Stop()
	public void OnStateChanged(AudioSessionState state)
	{
		if (state == AudioSessionState.AudioSessionStateActive)
		{
			OnVolumeChanged(_appVolume.SimpleAudioVolume.Volume, _appVolume.SimpleAudioVolume.Mute);
		}
	}
	public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
	{
		throw new NotImplementedException();
	}
	public void SetVolume(float volume)
	{
		_shouldSendVolUpdateEvent = false;
		_appVolume.SimpleAudioVolume.Volume = volume;
	}

	public virtual void Dispose()
	{
		_out.Stop();
		_out.Dispose();
		_appVolume.Dispose();
	}
}
