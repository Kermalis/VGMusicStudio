using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kermalis.VGMusicStudio.Core;

public abstract class LoadedSong : IDisposable, ILoadedSong
{
    public abstract List<SongEvent>[] Events { get; }
    public abstract long MaxTicks { get; protected set; }
    public virtual int HeaderOffset { get; protected set; }
    public abstract SoundBank Bank { get; protected set; }

    public virtual void InsertEvent(SongEvent e, int trackIndex, int insertIndex) { }
    public virtual void ChangeEvent(SongEvent ev, decimal vArgsVal1, byte vArgsVal2, bool changed) { }
    public virtual void RemoveEvent(int trackIndex, int eventIndex) { }
    public virtual CommandArg[] GetCommandMembers(int trackIndex, int eventIndex) { return null!; }
    public virtual bool CallOrJumpCommand(SongEvent e) { return false; }
    public virtual void OpenASM(Assembler assembler, string headerLabel) { }
    public virtual void SaveAsASM(string fileName, ASMSaveArgs args) { }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
