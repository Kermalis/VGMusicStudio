using System;
using System.IO;
using System.Linq;
using Kermalis.VGMusicStudio.Core.Formats;
using Kermalis.VGMusicStudio.Core.Util;
using NAudio.Wave;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed class MP2KMixer : Mixer
{
    internal readonly int SampleRate;
    internal override int SamplesPerBuffer { get; }
    internal readonly float SampleRateReciprocal;
    private readonly float _samplesReciprocal;
    private int _fixedModeRate = 13379;
    // internal MP2KReverb[] Reverbs;
    // internal readonly float PCM8MasterVolume;
    private bool _isFading;
    private long _fadeMicroFramesLeft;
    private float _fadePos;
    private float _fadeStepPerMicroframe;

    internal readonly MP2KConfig Config;
    private readonly AudioBackend MP2KPlaybackBackend;

    #region MiniAudio Fields
    // MiniAudio Fields
    private readonly float[]? _bufferMiniAudio;
    private readonly AudioFormat _formatSoundFlow;
    protected override AudioFormat SoundFlowFormat => _formatSoundFlow;
    #endregion

    #region PortAudio Fields
    // PortAudio Fields
    private readonly Audio? _audioPortAudio;
    private readonly Wave? _bufferPortAudio;
    #endregion

    #region NAudio Fields
    // NAudio Fields
    private readonly WaveBuffer? _audioNAudio;
    private readonly BufferedWaveProvider? _bufferNAudio;

    protected override WaveFormat WaveFormat => _bufferNAudio!.WaveFormat;
    #endregion

    // internal float[][] TrackBuffers;

    internal MP2KMixer(MP2KConfig config)
    {
        Config = config;
        (SampleRate, SamplesPerBuffer) = MP2KUtils.FrequencyTable[config.FrequencyIndex];
        SampleRateReciprocal = 1f / SampleRate;
        _samplesReciprocal = 1f / SamplesPerBuffer;
        // PCM8MasterVolume = config.Volume / 15f;

        int amt = SamplesPerBuffer * 2;
        // TrackBuffers = new float[0x10][];
        // Reverbs = new MP2KReverb[0x10];
        // for (int i = 0; i < TrackBuffers.Length; i++)
        // {
        //     TrackBuffers[i] = new float[amt];
        // }
        MP2KPlaybackBackend = PlaybackBackend;
        switch (PlaybackBackend)
        {
            case AudioBackend.MiniAudio:
                {
                    _bufferMiniAudio = new float[amt];
                    _formatSoundFlow = new AudioFormat
                    {
                        Channels = 2,
                        SampleRate = SampleRate,
                        Format = SampleFormat.F32
                    };
                    Init();
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    _audioPortAudio = new Audio(amt * sizeof(float)) { Float32BufferCount = amt };
                    _bufferPortAudio = new Wave()
                    {
                        DiscardOnBufferOverflow = true,
                        BufferLength = SamplesPerBuffer * 64,
                    };
                    _bufferPortAudio.CreateIeeeFloatWave((uint)SampleRate, 2);

                    Init(waveData: _bufferPortAudio);
                    break;
                }
            case AudioBackend.NAudio:
                {
                    _audioNAudio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
                    _bufferNAudio = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2))
                    {
                        DiscardOnBufferOverflow = true
                    };
                    Init(waveProvider: _bufferNAudio);
                    break;
                }
        }
    }

    // internal MP2KPCM8Channel? AllocPCM8Channel(MP2KTrack owner, ADSR env, NoteInfo note, byte vol, sbyte pan, int panSweep, int pitch, bool bFixed, bool bCompressed, int sampleOffset)
    // {
    //     MP2KPCM8Channel? nChn = null;
    //     int instPan = (panSweep & 0x80) != 0 ? panSweep - 0xC0 : 0;
    //     IOrderedEnumerable<MP2KPCM8Channel> byOwner = _pcm8Channels.OrderByDescending(c => c.Track is null ? 0xFF : c.Track.Index);
    //     foreach (MP2KPCM8Channel i in byOwner) // Find free
    //     {
    //         if (i.State == EnvelopeState.Dead || i.Owner is null)
    //         {
    //             nChn = i;
    //             break;
    //         }
    //     }
    //     if (nChn is null) // Find releasing
    //     {
    //         foreach (MP2KPCM8Channel i in byOwner)
    //         {
    //             if (i.State == EnvelopeState.Releasing)
    //             {
    //                 nChn = i;
    //                 break;
    //             }
    //         }
    //     }
    //     if (nChn is null) // Find prioritized
    //     {
    //         foreach (MP2KPCM8Channel i in byOwner)
    //         {
    //             if (owner.Priority > i.Owner!.Priority)
    //             {
    //                 nChn = i;
    //                 break;
    //             }
    //         }
    //     }
    //     if (nChn is null) // None available
    //     {
    //         MP2KPCM8Channel lowest = byOwner.First(); // Kill lowest track's instrument if the track is lower than this one
    //         if (lowest.Owner!.Index >= owner.Index)
    //         {
    //             nChn = lowest;
    //         }
    //     }
    //     if (nChn is not null) // Could still be null from the above if
    //     {
    //         nChn.Init(owner, note, env, sampleOffset, vol, pan, instPan, pitch, bFixed, bCompressed);
    //     }
    //     return nChn;
    // }
    // internal MP2KPSGChannel? AllocPSGChannel(MP2KTrack owner, ADSR env, NoteInfo note, byte vol, sbyte pan, int panSweep, int pitch, VoiceType type, object arg)
    // {
    //     MP2KPSGChannel nChn;
    //     int instPan = (panSweep & 0x80) != 0 ? panSweep - 0xC0 : 0;
    //     switch (type)
    //     {
    //         case VoiceType.Square1:
    //             {
    //                 nChn = _sq1;
    //                 if (nChn.State < EnvelopeState.Releasing && nChn.Owner!.Index < owner.Index)
    //                 {
    //                     return null;
    //                 }
    //                 _sq1.Init(owner, note, env, (byte)panSweep, (SquarePattern)arg);
    //                 break;
    //             }
    //         case VoiceType.Square2:
    //             {
    //                 nChn = _sq2;
    //                 if (nChn.State < EnvelopeState.Releasing && nChn.Owner!.Index < owner.Index)
    //                 {
    //                     return null;
    //                 }
    //                 _sq2.Init(owner, note, env, (byte)panSweep, (SquarePattern)arg);
    //                 break;
    //             }
    //         case VoiceType.PCM4:
    //             {
    //                 nChn = _pcm4;
    //                 if (nChn.State < EnvelopeState.Releasing && nChn.Owner!.Index < owner.Index)
    //                 {
    //                     return null;
    //                 }
    //                 _pcm4.Init(owner, note, env, instPan, (int)arg);
    //                 break;
    //             }
    //         case VoiceType.Noise:
    //             {
    //                 nChn = _noise;
    //                 if (nChn.State < EnvelopeState.Releasing && nChn.Owner!.Index < owner.Index)
    //                 {
    //                     return null;
    //                 }
    //                 _noise.Init(owner, note, env, instPan, (NoisePattern)arg);
    //                 break;
    //             }
    //         default: return null;
    //     }
    //     nChn.SetVolume(vol, pan);
    //     nChn.SetPitch(pitch);
    //     return nChn;
    // }

    internal static void UpdateReverb(Span<MP2KTrack> tracks)
    {
        foreach (MP2KTrack trk in tracks)
        {
            trk.Reverb.SetLevel(GetReverbLevel());
        }
    }

    internal void UpdateFixedModeRate(Span<MP2KTrack> tracks)
    {
        int[] rateTable = [
            0, 5734, 7884, 10512, 13379, 15768, 18157, 21024, 26758, 31536, 36314, 40137, 42048, 0, 0, 0
        ];

        MP2KContext ctx = MP2KEngine.MP2KInstance!.Context;

        _fixedModeRate = rateTable[ctx.MP2KSoundMode.FrequencyIndex % rateTable.Length];

        byte numDmaBuffers = Math.Max(
            (byte)2, (byte)(ctx.PlayerSoundMode.DMABufferLength / (float)(_fixedModeRate / GBAUtils.AGB_APPROX_FPS))
        );

        foreach (MP2KTrack trk in tracks)
        {
            trk.Reverb = MP2KReverb.MakeReverb(
                ctx.PlayerSoundMode.ReverbType, GetReverbLevel(), SampleRate, numDmaBuffers
            );
        }
    }

    internal static byte GetReverbLevel()
    {
        MP2KContext ctx = MP2KEngine.MP2KInstance!.Context;
        if ((ctx.PlayerSoundMode.ReverbForce & MP2KSoundMode.REV_MASK_SET) != 0)
            return (byte)(ctx.PlayerSoundMode.ReverbForce & MP2KSoundMode.REV_MASK_VAL);
        else
            return (byte)(ctx.MP2KSoundMode.Reverb & MP2KSoundMode.REV_MASK_VAL);
    }

    // internal void SetReverb(MP2KTrack track)
    // {
    //     byte reverb = (byte)(Config.Reverb >= 0x80 ? Config.Reverb & 0x7F : 0 & 0x7F);
    //     if (track.Reverb >> 7 is 1)
    //     {
    //         reverb = (byte)(track.Reverb >> 1);
    //     }
    //     float engineFrequency = SampleRate / SamplesPerBuffer;
    //     for (int i = 0; i < Reverbs.Length; i++)
    //     {
    //         byte numBuffers = (byte)(0x630 / (engineFrequency / GBAUtils.AGB_FPS));
    //         switch (Config.ReverbType)
    //         {
    //             default: Reverbs[i] = new MP2KReverb(this, reverb, numBuffers); break;
    //             case ReverbType.Camelot1: Reverbs[i] = new MP2KReverbCamelot1(this, reverb, numBuffers); break;
    //             case ReverbType.Camelot2: Reverbs[i] = new MP2KReverbCamelot2(this, reverb, numBuffers, 53 / 128f, -8 / 128f); break;
    //             case ReverbType.MGAT: Reverbs[i] = new MP2KReverbCamelot2(this, reverb, numBuffers, 32 / 128f, -6 / 128f); break;
    //             case ReverbType.None: Reverbs[i] = null!; break;
    //         }
    //     }
    // }

    internal void BeginFadeIn()
    {
        _fadePos = 0f;
        _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1_000.0 * GBAUtils.AGB_FPS);
        _fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
        _isFading = true;
    }
    internal void BeginFadeOut()
    {
        _fadePos = 1f;
        _fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1_000.0 * GBAUtils.AGB_FPS);
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

    internal void Process(MP2KPlayer player, MP2KTrack[] tracks, bool output, bool recording)
    {
        for (int i = 0; i < tracks.Length; i++)
        {
            Span<float> buf = tracks[i].Buffer;
            buf.Clear();
        }

        MixingArgs margs = new()
        {
            Volume = (MP2KEngine.MP2KInstance!.Context.MP2KSoundMode.Volume + 1) / 16.0f,
            FixedModeRate = SampleRate,
            SampleRateInv = 1.0f / SampleRate,
            SamplesPerBufferInv = 1.0f / SamplesPerBuffer
        };

        switch (MP2KPlaybackBackend)
        {
            case AudioBackend.MiniAudio:
                {
                    Array.Clear(_bufferMiniAudio!);
                    break;
                }
            case AudioBackend.PortAudio:
                {
                    _audioPortAudio!.Clear();
                    break;
                }
            case AudioBackend.NAudio:
                {
                    _audioNAudio!.Clear();
                    break;
                }
        }

        foreach (MP2KPCM8Channel c in player.MContext.PCM8Channels)
        {
            if (c.Track is not null)
            {
                var index = c.Track.Index;
                c.Process(tracks[index].Buffer, margs);
                // Reverbs[index].Process(TrackBuffers[index], SamplesPerBuffer);
            }
        }

        foreach (MP2KTrack trk in tracks)
        {
            if (trk is not null)
            {
                var index = trk.Index;
                trk.Reverb.Process(tracks[index].Buffer, SamplesPerBuffer);
            }
        }

        foreach (MP2KSquareChannel c in player.MContext.Square1Channels)
        {
            if (c.Track is not null)
            {
                c.Process(tracks[c.Track.Index].Buffer, margs);
            }
        }

        foreach (MP2KSquareChannel c in player.MContext.Square2Channels)
        {
            if (c.Track is not null)
            {
                c.Process(tracks[c.Track.Index].Buffer, margs);
            }
        }

        foreach (MP2KPCM4Channel c in player.MContext.PCM4Channels)
        {
            if (c.Track is not null)
            {
                c.Process(tracks[c.Track.Index].Buffer, margs);
            }
        }

        foreach (MP2KNoiseChannel c in player.MContext.NoiseChannels)
        {
            if (c.Track is not null)
            {
                c.Process(tracks[c.Track.Index].Buffer, margs);
            }
        }

        static bool RemoveFunc(MP2KChannel? channel)
        {
            return channel?.State == EnvelopeState.Dead;
        }

        for (int i = 0; i < player.MContext.PCM8Channels.Count; i++)
        {
            if (RemoveFunc(player.MContext.PCM8Channels[i]))
            {
                player.MContext.PCM8Channels.RemoveAt(i);
            }
        }

        for (int i = 0; i < player.MContext.Square1Channels.Count; i++)
        {
            if (RemoveFunc(player.MContext.Square1Channels[i]))
            {
                player.MContext.Square1Channels.RemoveAt(i);
            }
        }

        for (int i = 0; i < player.MContext.Square2Channels.Count; i++)
        {
            if (RemoveFunc(player.MContext.Square2Channels[i]))
            {
                player.MContext.Square2Channels.RemoveAt(i);
            }
        }

        for (int i = 0; i < player.MContext.PCM4Channels.Count; i++)
        {
            if (RemoveFunc(player.MContext.PCM4Channels[i]))
            {
                player.MContext.PCM4Channels.RemoveAt(i);
            }
        }

        for (int i = 0; i < player.MContext.NoiseChannels.Count; i++)
        {
            if (RemoveFunc(player.MContext.NoiseChannels[i]))
            {
                player.MContext.NoiseChannels.RemoveAt(i);
            }
        }

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
        for (int i = 0; i < tracks.Length; i++)
        {
            if (Mutes[i])
            {
                continue;
            }

            float level = masterLevel;
            Span<float> buf = tracks[i].Buffer;
            for (int j = 0; j < SamplesPerBuffer; j++)
            {
                switch (MP2KPlaybackBackend)
                {
                    case AudioBackend.MiniAudio:
                        {
                            _bufferMiniAudio![j * 2] += buf[j * 2] * level;
                            _bufferMiniAudio[(j * 2) + 1] += buf[(j * 2) + 1] * level;
                            break;
                        }
                    case AudioBackend.PortAudio:
                        {
                            _audioPortAudio!.Float32Buffer![j * 2] += buf[j * 2] * level;
                            _audioPortAudio.Float32Buffer[(j * 2) + 1] += buf[(j * 2) + 1] * level;
                            break;
                        }
                    case AudioBackend.NAudio:
                        {
                            _audioNAudio!.FloatBuffer![j * 2] += buf[j * 2] * level;
                            _audioNAudio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1] * level;
                            break;
                        }
                }
                level += masterStep;
            }
        }
        if (output)
        {
            switch (MP2KPlaybackBackend)
            {
                case AudioBackend.MiniAudio:
                    {
                        DataProvider!.AddSamples(_bufferMiniAudio); // Thank you LSXPrime for pointing out that it just needs AddSamples and nothing else in here
                        break;
                    }
                case AudioBackend.PortAudio:
                    {
                        _bufferPortAudio!.AddSamples(_audioPortAudio!.ByteBuffer, 0, _audioPortAudio.ByteBufferCount);
                        break;
                    }
                case AudioBackend.NAudio:
                    {
                        _bufferNAudio!.AddSamples(_audioNAudio!.ByteBuffer, 0, _audioNAudio.ByteBufferCount);
                        break;
                    }
            }
        }
        if (recording)
        {
            switch (MP2KPlaybackBackend)
            {
                case AudioBackend.MiniAudio:
                    {
                        _soundFlowEncoder!.Encode(_bufferMiniAudio); // Again, thank you LSXPrime for showing how to encode
                        break;
                    }
                case AudioBackend.PortAudio:
                    {
                        _waveWriterPortAudio!.Write(_audioPortAudio!.ByteBuffer, 0, _audioPortAudio.ByteBufferCount);
                        break;
                    }
                case AudioBackend.NAudio:
                    {
                        _waveWriterNAudio!.Write(_audioNAudio!.ByteBuffer, 0, _audioNAudio.ByteBufferCount);
                        break;
                    }
            }
        }
    }
}
