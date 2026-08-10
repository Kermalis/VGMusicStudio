using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace Kermalis.VGMusicStudio.Core;

// Used everywhere
public interface IOffset
{
    int Offset { get; set; }
}

// Used for song events
public interface ICommand
{
    Color Color { get; }
    string Label { get; }
    string Arguments { get; }
}

// Used for loading songs
public interface ILoadedSong
{
    List<SongEvent>?[] Events { get; }
    long MaxTicks { get; }
    SoundBank Bank { get; }

    virtual CommandArg[] GetCommandMembers(int trackIndex, int eventIndex) { return null!; }
    virtual bool CallOrJumpCommand(SongEvent se) { return false; }
    virtual void ChangeEvent(SongEvent ev, decimal vArgsVal1, byte vArgsVal2, bool changed) { }
    virtual void InsertEvent(SongEvent e, int trackIndex, int insertIndex) { }
    virtual void RemoveEvent(int trackIndex, int eventIndex) { }
}

// Used in the SoundBankEditor. GetName() is also used for the UI
public interface IVoiceInfo : IOffset
{
    string? Name { get; }
    IEnumerable<IVoiceInfo> GetSubVoices() => Enumerable.Empty<IVoiceInfo>();
}