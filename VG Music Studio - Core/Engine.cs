using System;

namespace Kermalis.VGMusicStudio.Core;

public abstract class Engine : IDisposable
{
    public static Engine? Instance { get; protected set; }

    public abstract Config Config { get; }
    public abstract Mixer Mixer { get; }
    public abstract Player Player { get; }

    public abstract bool IsFileSystemFormat { get; }

    public virtual ICommand[] GetCommands() { return new ICommand[1]; }

    public abstract void Reload();
    public virtual void Dispose()
    {
        Config.Dispose();
        Mixer.Dispose();
        Player.Dispose();
        Instance = null;
        GC.SuppressFinalize(this);
    }
}
