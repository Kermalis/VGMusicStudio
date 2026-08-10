using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong : LoadedSong
{
    public override List<SongEvent>[] Events { get; }
    public override long MaxTicks { get; protected set; }
    public override int HeaderOffset { get; protected set; }
    public override SoundBank Bank { get; protected set; }
    public MP2KSoundBank Voicegroup => (MP2KSoundBank)Bank;
    public int LongestTrack;

    private readonly MP2KPlayer _player;
    public readonly SongHeader Header;
    private readonly int _voicegroupOffset;
    public readonly MP2KTrack[] Tracks;

    public MP2KLoadedSong(MP2KPlayer player, int index)
    {
        _player = player;

        MP2KConfig cfg = player.Config;
        var entry = SongEntry.Get(cfg.ROM, cfg.SongTableOffsets[0], index);
        HeaderOffset = entry.HeaderOffset - GBAUtils.CARTRIDGE_OFFSET;

        Header = SongHeader.Get(cfg.ROM, HeaderOffset, out int tracksOffset);
        _voicegroupOffset = Header.SoundBankOffset - GBAUtils.CARTRIDGE_OFFSET;
        Bank = MP2KSoundBank.LoadTable<MP2KSoundBank>(_voicegroupOffset);

        Tracks = new MP2KTrack[Header.NumTracks];
        Events = new List<SongEvent>[Header.NumTracks];
        for (byte trackIndex = 0; trackIndex < Header.NumTracks; trackIndex++)
        {
            int trackStart = SongHeader.GetTrackOffset(cfg.ROM, tracksOffset, trackIndex) - GBAUtils.CARTRIDGE_OFFSET;
            Tracks[trackIndex] = new MP2KTrack(trackIndex, trackStart, _player.MContext.SamplesPerBuffer);

            AddTrackEvents(trackIndex, trackStart);
        }

        player.MMixer.UpdateFixedModeRate(Tracks);
        MP2KMixer.UpdateReverb(Tracks);

        _player = player;
    }

    public void CheckVoiceTypeCache(ref int? old, string?[] voiceTypeCache)
    {
        if (old != _voicegroupOffset)
        {
            old = _voicegroupOffset;
            Array.Clear(voiceTypeCache);
        }
    }
}
