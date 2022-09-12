using System;

namespace Kermalis.VGMusicStudio.Core;

public abstract class Engine : IDisposable
{
	public static Engine? Instance { get; protected set; }

	public abstract Config Config { get; }
	public abstract Mixer Mixer { get; }
	public abstract Player Player { get; }

	public virtual void Dispose()
	{
		Config.Dispose();
		Mixer.Dispose();
		Player.Dispose();
		Instance = null;
	}
}
