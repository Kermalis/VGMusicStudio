using Kermalis.VGMusicStudio.UI;
using Kermalis.VGMusicStudio.Properties;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using static Kermalis.VGMusicStudio.UI.SongInfoControl;

namespace Kermalis.VGMusicStudio.Core
{
    internal abstract class Mixer : IAudioSessionEventsHandler, IDisposable
    {
        public readonly bool[] Mutes = new bool[SongInfoControl.SongInfo.MaxTracks];
        private IWavePlayer _out;
        private AudioSessionControl _appVolume;
        private DeviceState _device = DeviceState.Unplugged;

        protected void Init(IWaveProvider waveProvider)
        {
            _out = new WasapiOut();
            _out.Init(waveProvider);
            using (var en = new MMDeviceEnumerator())
            {
                SessionCollection sessions = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).AudioSessionManager.Sessions;
                int id = System.Diagnostics.Process.GetCurrentProcess().Id;
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

        private bool _volChange = true;
        public void OnVolumeChanged(float volume, bool isMuted)
        {
            if (_volChange)
            {
                MainForm.Instance.SetVolumeBarValue(volume);
            }
            _volChange = true;
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
            if (disconnectReason == AudioSessionDisconnectReason.DisconnectReasonDeviceRemoval)
            {
                Exception ex = new Exception();
                FlexibleMessageBox.Show(ex, string.Format(Strings.AudioDeviceRemoved, _device));
            }
            if (disconnectReason == AudioSessionDisconnectReason.DisconnectReasonExclusiveModeOverride)
            {

            }
            if (disconnectReason == AudioSessionDisconnectReason.DisconnectReasonFormatChanged)
            {

            }
            if (disconnectReason == AudioSessionDisconnectReason.DisconnectReasonServerShutdown)
            {

            }
            if (disconnectReason == AudioSessionDisconnectReason.DisconnectReasonSessionDisconnected)
            {

            }
            if (disconnectReason == AudioSessionDisconnectReason.DisconnectReasonSessionLogoff)
            {

            }
        }
        public void SetVolume(float volume)
        {
            _volChange = false;
            _appVolume.SimpleAudioVolume.Volume = volume;
        }

        public virtual void Dispose()
        {
            _out.Stop();
            _out.Dispose();
            _appVolume.Dispose();
        }
    }
}
