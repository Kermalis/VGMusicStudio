using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed partial class MP2KPlayer : Player
{
    protected override string Name => "MP2K Player";

    private readonly string?[] _voiceTypeCache;
    internal readonly MP2KConfig Config;
    internal MP2KContext MContext;
    internal readonly MP2KMixer MMixer;
    private MP2KLoadedSong? _loadedSong;

    public override ushort Tempo { get; set; }
    internal int TempoStack;
    private long _elapsedLoops;

    private int? _prevVoiceTableOffset;
    private int _longestTrack;

    public override ILoadedSong? LoadedSong => _loadedSong;
    protected override Mixer Mixer => MMixer;

    internal MP2KPlayer(MP2KConfig config, MP2KContext context, MP2KMixer mixer)
        : base(GBAUtils.AGB_FPS)
    {
        Config = config;
        MContext = context;
        MMixer = mixer;

        _voiceTypeCache = new string[256];
    }

    public override void LoadSong(int index)
    {
        if (_loadedSong is not null)
        {
            _loadedSong = null;
        }

        // If there's an exception, this will remain null
        _loadedSong = new MP2KLoadedSong(this, index);
        if (_loadedSong.Events.Length == 0)
        {
            _loadedSong = null;
            return;
        }

        _loadedSong.CheckVoiceTypeCache(ref _prevVoiceTableOffset, _voiceTypeCache);
        _loadedSong.SetTicks();

        MContext.PCM8Channels.Clear();
        MContext.Square1Channels.Clear();
        MContext.Square2Channels.Clear();
        MContext.PCM4Channels.Clear();
        MContext.NoiseChannels.Clear();
    }
    public override void LoadSong(LoadedSong song)
    {
        // If there's an exception, this will remain null
        _loadedSong = (MP2KLoadedSong)song;
        if (_loadedSong.Events.Length == 0)
        {
            _loadedSong = null;
            return;
        }

        _loadedSong.CheckVoiceTypeCache(ref _prevVoiceTableOffset, _voiceTypeCache);
        _loadedSong.SetTicks();
    }
    public override void RefreshSong()
    {
        if (State == PlayerState.Stopped || State == PlayerState.ShutDown)
        {
            return;
        }
        DetermineLongestTrack();
        SetSongPosition(ElapsedTicks);
    }
    private void DetermineLongestTrack()
    {
        for (int i = 0; i < _loadedSong!.Tracks.Length; i++)
        {
            if (LoadedSong!.Events[i]!.Last().Ticks[0] == _loadedSong.MaxTicks - 1)
            {
                _longestTrack = i;
                break;
            }
        }
    }
    public override void UpdateSongState(SongState info)
    {
        info.Tempo = Tempo;
        _loadedSong!.UpdateSongState(info, _voiceTypeCache);
    }
    internal override void InitEmulation()
    {
        Tempo = 150;
        TempoStack = 0;
        _elapsedLoops = 0;
        ElapsedTicks = 0;
        MMixer.ResetFade();
        MP2KTrack[] tracks = _loadedSong!.Tracks;
        for (int i = 0; i < tracks.Length; i++)
        {
            tracks[i].Init();
        }

        MContext.PCM8Channels.Clear();
        MContext.Square1Channels.Clear();
        MContext.Square2Channels.Clear();
        MContext.PCM4Channels.Clear();
        MContext.NoiseChannels.Clear();
    }
    protected override void SetCurTick(long ticks)
    {
        _loadedSong!.SetCurTick(ticks);
    }
    protected override void OnStopped()
    {
        MP2KTrack[] tracks = _loadedSong!.Tracks;
        for (int i = 0; i < tracks.Length; i++)
        {
            tracks[i].StopAllChannels();
        }
    }

    protected override bool Tick(bool playing, bool recording)
    {
        MP2KLoadedSong s = _loadedSong!;

        bool allDone = false;
        while (!allDone && TempoStack >= 150)
        {
            TempoStack -= 150;
            allDone = true;
            for (int i = 0; i < s.Tracks.Length; i++)
            {
                TickTrack(s, s.Tracks[i], ref allDone);
            }
            if (MMixer.IsFadeDone())
            {
                allDone = true;
            }
        }
        if (!allDone)
        {
            TempoStack += Tempo;
        }
        // for (int i = 0; i < s.Tracks.Length; i++)
        // {
        //     MMixer.SetReverb(s.Tracks[i]);
        // }
        MMixer.Process(this, s.Tracks, playing, recording);
        return allDone;
    }
    private void TickTrack(MP2KLoadedSong s, MP2KTrack track, ref bool allDone)
    {
        track.Tick();
        bool update = false;
        while (track.Rest == 0 && !track.Stopped)
        {
            s.ExecuteNext(track, ref update);
        }
        if (track.Index == s.LongestTrack)
        {
            HandleTicksAndLoop(s, track);
        }
        if (!track.Stopped)
        {
            allDone = false;
        }
        if (track.Channels.Count > 0)
        {
            allDone = false;
            if (update || track.LFODepth > 0)
            {
                track.UpdateChannels();
            }
        }
        if (track.Stopped is true)
        {
            track.Channels.Clear();
        }
    }
    private void HandleTicksAndLoop(MP2KLoadedSong s, MP2KTrack track)
    {
        if (ElapsedTicks != s.MaxTicks)
        {
            ElapsedTicks++;
            return;
        }

        // Track reached the detected end, update loops/ticks accordingly
        if (track.Stopped)
        {
            return;
        }

        _elapsedLoops++;
        UpdateElapsedTicksAfterLoop(s.Events[track.Index], track.ROMOffset, track.Rest);
        if (ShouldFadeOut && _elapsedLoops > NumLoops && !MMixer.IsFading())
        {
            MMixer.BeginFadeOut();
        }
    }

    public void SaveAsMIDI(string fileName, MIDISaveArgs args)
    {
        _loadedSong!.SaveAsMIDI(fileName, args);
    }

    public void SaveAsASM(string fileName, ASMSaveArgs args)
    {
        _loadedSong!.SaveAsASM(fileName, args);
    }
}
