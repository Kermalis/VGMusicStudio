﻿using Kermalis.MusicStudio.UI;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;

namespace Kermalis.MusicStudio.Core
{
    abstract class Mixer : IAudioSessionEventsHandler
    {
        public bool[] Mutes { get; protected set; }
        IWavePlayer @out;
        AudioSessionControl appVolume;

        protected void Init(IWaveProvider waveProvider)
        {
            @out = new WasapiOut();
            @out.Init(waveProvider);
            SessionCollection sessions = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).AudioSessionManager.Sessions;
            int id = System.Diagnostics.Process.GetCurrentProcess().Id;
            for (int i = 0; i < sessions.Count; i++)
            {
                AudioSessionControl session = sessions[i];
                if (session.GetProcessID == id)
                {
                    appVolume = session;
                    appVolume.RegisterEventClient(this);
                    break;
                }
            }
            @out.Play();
        }

        bool ignoreVolChangeFromUI = false;
        public void OnVolumeChanged(float volume, bool isMuted)
        {
            if (!ignoreVolChangeFromUI)
            {
                MainForm.Instance.SetVolumeBarValue(volume);
            }
            ignoreVolChangeFromUI = false;
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
                OnVolumeChanged(appVolume.SimpleAudioVolume.Volume, appVolume.SimpleAudioVolume.Mute);
            }
        }
        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            throw new NotImplementedException();
        }
        public void SetVolume(float volume)
        {
            ignoreVolChangeFromUI = true;
            appVolume.SimpleAudioVolume.Volume = volume;
        }
    }
}
