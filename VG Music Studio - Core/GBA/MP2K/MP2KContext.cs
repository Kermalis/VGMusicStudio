using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KContext
{
    internal int SampleRate;
    internal int SamplesPerBuffer;
    internal float[] AudioBuffer;
    // internal MP2KLoadedSong LoadedSong;
    internal MP2KSoundMode MP2KSoundMode = new();
    internal PlayerSoundMode PlayerSoundMode = new();
    internal SongTableInfo SongTableInfo;
    internal List<PlayerInfo> PlayerTableInfo = [];

    internal Memory<byte> MemAccArea = new byte[256];

    // Channels
    internal List<MP2KPCM8Channel> PCM8Channels = [];
    internal List<MP2KSquareChannel> Square1Channels = [];
    internal List<MP2KSquareChannel> Square2Channels = [];
    internal List<MP2KPCM4Channel> PCM4Channels = [];
    internal List<MP2KNoiseChannel> NoiseChannels = [];

    internal static bool Playing = false;
    internal static bool Recording = false;

    internal MP2KContext(MP2KConfig config)
    {
        (SampleRate, SamplesPerBuffer) = MP2KUtils.FrequencyTable[config.Results[0].MP2KSoundMode.FrequencyIndex];
        MP2KSoundMode = config.Results[0].MP2KSoundMode;
        PlayerTableInfo = config.Results[0].PlayerTableInfo;
        SongTableInfo = config.Results[0].SongTableInfo;

        // LoadedSong = new(this);

        AudioBuffer = new float[SamplesPerBuffer];
    }

    // internal void InitSoundMode(uint mode)
    // {
    //     byte reverb = 0 & 0xFF;

    //     SoundModeReverb(reverb);

    //     byte masterVol = (byte)((mode >> 12) & 0xF);
    //     SoundModePCMVolume(masterVol);

    //     byte freq = (byte)((mode >> 16) & 0xF);
    //     SoundModePCMFrequency(freq);

    //     byte dac = (byte)((mode >> 20) & 0xF);
    //     SoundModeDACConfig(dac);
    // }

    // internal void SoundModeReverb(byte reverb)
    // {
    //     if ((reverb & 0x80) != 0)
    //     {
    //         MP2KSoundMode.Reverb = reverb;
    //         MP2KEngine.MP2KInstance!.Mixer.UpdateReverb();
    //     }
    // }

    // internal void SoundModePCMVolume(byte volume)
    // {
    //     if (volume != 0)
    //     {
    //         MP2KSoundMode.Volume = volume;
    //     }
    // }

    // internal void SoundModePCMFrequency(byte frequency)
    // {
    //     if (frequency != 0)
    //     {
    //         MP2KSoundMode.Frequency = frequency;
    //         MP2KEngine.MP2KInstance!.Mixer.UpdateFixedModeRate();
    //     }
    // }

    // internal void SoundModeDACConfig(byte dacConfig)
    // {
    //     if (dacConfig != 0)
    //     {
    //         MP2KSoundMode.DACConfig = dacConfig;
    //     }
    // }

    // internal void SoundClear()
    // {
    //     PCM8Channels.Clear();
    //     Square1 = null;
    //     Square2 = null;
    //     PCM4 = null;
    //     Noise = null;

    //     MP2KPlayer player = MP2KEngine.MP2KInstance!.Player;
    //     foreach (MP2KTrack track in ((MP2KLoadedSong)player.LoadedSong!).Tracks)
    //     {
    //         track.Reverb!.Reset();
    //     }
    // }

    // internal byte GetSongNumPlayer(byte songIndex)
    // {
    //     return MP2KEngine.MP2KInstance!.Config.ROM[SongTableInfo.Position + songIndex * 8 + 4];
    // }

    // internal bool SongEnded()
    // {
    //     return LoadedSong.EndReached() && MP2KEngine.MP2KInstance!.Mixer.IsFadeDone();
    // }
}
