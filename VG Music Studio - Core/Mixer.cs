using PortAudio;
using System;
using System.Runtime.InteropServices;
using System.IO;
using Kermalis.VGMusicStudio.Core.Formats;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Interfaces;
using NAudio.Wave.Alsa;

namespace Kermalis.VGMusicStudio.Core;

public abstract class Mixer : IAudioSessionEventsHandler, IDisposable
{
    public readonly bool[] Mutes;

    #region MiniAudio Fields
    // MiniAudio Fields
    private AudioEngine? _soundFlowEngine;
    private AudioPlaybackDevice? _playbackDevice;
    internal SoundPlayer? MiniAudioPlayer;
    internal QueueDataProvider? DataProvider;
    private System.IO.Stream? _soundFlowFileStream;
    protected ISoundEncoder? _soundFlowEncoder;
    protected abstract AudioFormat SoundFlowFormat { get; } // New virtual field! Thank you, LSXPrime!
    #endregion

    #region PortAudio Fields
    // PortAudio Fields
    public Wave? WaveData;
    internal abstract int SamplesPerBuffer { get; }

    public readonly object CountLock = new();

    protected Wave? _waveWriterPortAudio;

    internal PortAudioPlayer? PortAudioPlayer;
    #endregion

    #region NAudio Fields
    // NAudio Fields
    public static event Action<float>? VolumeChanged;
    private WasapiOut? _wasapiOut;
    private AlsaOut? _alsaOut;
    private AudioSessionControl? _appVolume;

    private bool _shouldSendVolUpdateEvent = true;

    protected WaveFileWriter? _waveWriterNAudio;
    protected abstract WaveFormat? WaveFormat { get; }
    #endregion

    // Audio Backend
    public static AudioBackend PlaybackBackend { get; set; }

    public enum AudioBackend
    {
        MiniAudio,
        PortAudio,
        NAudio
    }

    protected Mixer()
    {
        Mutes = new bool[SongState.MAX_TRACKS];
        if (PlaybackBackend is AudioBackend.NAudio)
        {
            _wasapiOut = null!;
            _appVolume = null!;
        }
    }

    protected void Init(Wave waveData = null!, PortAudio.SampleFormat sampleFormat = PortAudio.SampleFormat.Float32,
    IWaveProvider waveProvider = null!)
    {
        switch (PlaybackBackend)
        {
            case AudioBackend.MiniAudio:
                {
                    _soundFlowEngine = new MiniAudioEngine();
                    // LSXPrime's notes: Let SoundFlow pick the default device by passing null
                    _playbackDevice = _soundFlowEngine.InitializePlaybackDevice(null, SoundFlowFormat); // I have to pass the deviceInfo as null, thank you LSXPrime for showing me
                    // LSXPrime's notes: Provide a capacity to the queue to prevent unbound memory growth
                    DataProvider = new QueueDataProvider(SoundFlowFormat, SamplesPerBuffer * 64, QueueFullBehavior.Block); // Apparently I needed to add buffer length as well. Thank you, LSXPrime for pointing that out
                    // _soundFlowDecoder = _soundFlowEngine.CreateDecoder(new MemoryStream(), SoundFlowFormat);
                    MiniAudioPlayer = new SoundPlayer(_soundFlowEngine, SoundFlowFormat, DataProvider);
                    _playbackDevice.MasterMixer.AddComponent(MiniAudioPlayer);
                    _playbackDevice.Start();
                    // LSXPrime's notes: Start from the player once; it will pull from the queue automatically
                    MiniAudioPlayer.Play(); // LSXPrime said that I only need to start the player once and it will pull from the queue automatically
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    // First, check if the instance contains something
                    if (WaveData is null || PortAudioPlayer is null)
                    {
                        WaveData = waveData;
                        PortAudioPlayer = new PortAudioPlayer(sampleFormat, SamplesPerBuffer, WaveData);
                    }
                    PortAudioPlayer.Play();
                    break;
                }
            case AudioBackend.NAudio:
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        _wasapiOut = new WasapiOut();
                        _wasapiOut.Init(waveProvider);
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
                        _wasapiOut.Play();
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        _alsaOut = new AlsaOut();
                        _alsaOut.Init(waveProvider);
                        _alsaOut.Play();
                    }
                    break;
                }
        }
    }

    public float Volume
    {
        get => GetVolume();
        set => SetVolume(value);
    }

    public float GetVolume()
    {
        return PlaybackBackend switch
        {
            AudioBackend.MiniAudio => _playbackDevice!.MasterMixer.Volume,
            AudioBackend.PortAudio => PortAudioPlayer!.Volume,
            AudioBackend.NAudio => _appVolume!.SimpleAudioVolume.Volume,
            _ => float.NaN,
        };
    }

    public void SetVolume(float volume)
    {
        switch (PlaybackBackend)
        {
            case AudioBackend.MiniAudio:
                {
                    _playbackDevice!.MasterMixer.Volume = volume;
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    PortAudioPlayer!.Volume = Math.Clamp(volume, 0, 1);
                    break;
                }
            case AudioBackend.NAudio:
                {
                    _shouldSendVolUpdateEvent = false;
                    _appVolume!.SimpleAudioVolume.Volume = volume;
                    break;
                }
        }
    }

    #region NAudio Functions
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
            OnVolumeChanged(_appVolume!.SimpleAudioVolume.Volume, _appVolume.SimpleAudioVolume.Mute);
        }
    }
    public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
    {
        throw new NotImplementedException();
    }
    #endregion

    public void CreateWaveWriter(string fileName)
    {
        switch (PlaybackBackend)
        {
            case AudioBackend.MiniAudio:
                {
                    if (_soundFlowEngine is null)
                    {
                        throw new InvalidOperationException("SoundFlow engine or format is not initialized for recording.");
                    }

                    _soundFlowFileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

                    _soundFlowEncoder = _soundFlowEngine.CreateEncoder(_soundFlowFileStream, "wav", SoundFlowFormat);
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    _waveWriterPortAudio = WaveData;
                    _waveWriterPortAudio!.CreateFileStream(fileName);
                    break;
                }
            case AudioBackend.NAudio:
                {
                    _waveWriterNAudio = new WaveFileWriter(fileName, WaveFormat);
                    break;
                }
        }
    }
    public void CloseWaveWriter()
    {
        switch (PlaybackBackend)
        {
            case AudioBackend.MiniAudio: // Thank you LSXPrime for adding this in
                {
                    _soundFlowEncoder?.Dispose();
                    _soundFlowFileStream?.Dispose();
                    _soundFlowEncoder = null;
                    _soundFlowFileStream = null;
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    _waveWriterPortAudio!.Dispose(true);
                    _waveWriterPortAudio = null;
                    break;
                }
            case AudioBackend.NAudio:
                {
                    _waveWriterNAudio!.Dispose();
                    _waveWriterNAudio = null;
                    break;
                }
        }
    }

    public virtual void Dispose()
    {
        switch (PlaybackBackend)
        {
            case AudioBackend.MiniAudio:
                {
                    if (MiniAudioPlayer is not null && _playbackDevice is not null)
                    {
                        MiniAudioPlayer.Stop();
                        _playbackDevice.Stop();
                        _playbackDevice.Dispose();
                        _soundFlowEngine?.Dispose();
                        DataProvider?.Dispose();
                    }
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    if (PortAudioPlayer is not null)
                    {
                        PortAudioPlayer.Stop();
                        PortAudioPlayer.Dispose();
                    }
                    break;
                }
            case AudioBackend.NAudio:
                {
                    if (_wasapiOut is not null && _appVolume is not null)
                    {
                        _wasapiOut.Stop();
                        _wasapiOut.Dispose();
                        _appVolume.Dispose();
                    }
                    break;
                }
        }
        GC.SuppressFinalize(this);
    }

    public interface IAudio
    {
        Span<byte> ByteBuffer { get; }
        Span<short> Int16Buffer { get; }
        Span<int> Int32Buffer { get; }
        Span<long> Int64Buffer { get; }
        Span<Int128> Int128Buffer { get; }
        Span<Half> Float16Buffer { get; }
        Span<float> Float32Buffer { get; }
        Span<double> Float64Buffer { get; }
        Span<decimal> Float128Buffer { get; }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 2)]
    public class Audio : IAudio
    {
        [FieldOffset(0)]
        public int NumberOfBytes;
        [FieldOffset(8)]
        public byte[]? ByteBuffer;
        [FieldOffset(8)]
        public short[]? Int16Buffer;
        [FieldOffset(8)]
        public int[]? Int32Buffer;
        [FieldOffset(8)]
        public long[]? Int64Buffer;
        [FieldOffset(8)]
        public Int128[]? Int128Buffer;
        [FieldOffset(8)]
        public Half[]? Float16Buffer;
        [FieldOffset(8)]
        public float[]? Float32Buffer;
        [FieldOffset(8)]
        public double[]? Float64Buffer;
        [FieldOffset(8)]
        public decimal[]? Float128Buffer;

        Span<byte> IAudio.ByteBuffer => ByteBuffer!;
        Span<short> IAudio.Int16Buffer => Int16Buffer!;
        Span<int> IAudio.Int32Buffer => Int32Buffer!;
        Span<long> IAudio.Int64Buffer => Int64Buffer!;
        Span<Int128> IAudio.Int128Buffer => Int128Buffer!;
        Span<Half> IAudio.Float16Buffer => Float16Buffer!;
        Span<float> IAudio.Float32Buffer => Float32Buffer!;
        Span<double> IAudio.Float64Buffer => Float64Buffer!;
        Span<decimal> IAudio.Float128Buffer => Float128Buffer!;

        public int ByteBufferCount
        {
            get
            {
                return NumberOfBytes;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("ByteBufferCount", value, 1);
            }
        }

        public int Int16BufferCount
        {
            get
            {
                return NumberOfBytes / 2;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Int16BufferCount", value, 2);
            }
        }

        public int Int32BufferCount
        {
            get
            {
                return NumberOfBytes / 4;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Int32BufferCount", value, 4);
            }
        }

        public int Int64BufferCount
        {
            get
            {
                return NumberOfBytes / 8;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Int64BufferCount", value, 8);
            }
        }

        public int Int128BufferCount
        {
            get
            {
                return NumberOfBytes / 16;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Int128BufferCount", value, 16);
            }
        }

        public int Float16BufferCount
        {
            get
            {
                return NumberOfBytes / 2;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Float16BufferCount", value, 2);
            }
        }

        public int Float32BufferCount
        {
            get
            {
                return NumberOfBytes / 4;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Float32BufferCount", value, 4);
            }
        }

        public int Float64BufferCount
        {
            get
            {
                return NumberOfBytes / 8;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Float64BufferCount", value, 8);
            }
        }

        public int Float128BufferCount
        {
            get
            {
                return NumberOfBytes / 16;
            }
            set
            {
                NumberOfBytes = CheckValidityCount("Float128BufferCount", value, 16);
            }
        }

        public Audio(int sizeToAllocateInBytes)
        {
            var sizeInBytes = sizeToAllocateInBytes;
            int aligned32Bits = sizeInBytes % 4;
            sizeToAllocateInBytes = (aligned32Bits == 0) ? sizeInBytes : (sizeInBytes + 4 - aligned32Bits);
            ByteBuffer = new byte[sizeToAllocateInBytes];
            NumberOfBytes = 0;
        }

        public static implicit operator byte[](Audio waveBuffer)
        {
            return waveBuffer.ByteBuffer!;
        }

        private int CheckValidityCount(string argName, int value, int sizeOfValue)
        {
            int num = value * sizeOfValue;
            if (num % 4 != 0)
            {
                throw new ArgumentOutOfRangeException(argName, $"{argName} cannot set a count ({num}) that is not 4 bytes aligned ");
            }

            if (value < 0 || value > ByteBuffer!.Length / sizeOfValue)
            {
                throw new ArgumentOutOfRangeException(argName, $"{argName} cannot set a count that exceeds max count of {ByteBuffer!.Length / sizeOfValue}.");
            }

            return num;
        }

        public void Clear()
        {
            Array.Clear(ByteBuffer!, 0, ByteBuffer!.Length);
        }

        public void Copy(Array destinationArray)
        {
            Array.Copy(ByteBuffer!, destinationArray, NumberOfBytes);
        }
    }
}
