using System.IO;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamEngine : Engine
{
	public static AlphaDreamEngine? AlphaDreamInstance { get; private set; }

	public override AlphaDreamConfig Config { get; }
	public override AlphaDreamMixer Mixer { get; }
	public override AlphaDreamPlayer Player { get; }

	public AlphaDreamEngine(byte[] rom)
	{
		if (rom.Length > GBAUtils.CARTRIDGE_CAPACITY)
		{
			throw new InvalidDataException($"The ROM is too large. Maximum size is 0x{GBAUtils.CARTRIDGE_CAPACITY:X7} bytes.");
		}

		Config = new AlphaDreamConfig(rom);
		Mixer = new AlphaDreamMixer(Config);
		Player = new AlphaDreamPlayer(Config, Mixer);

		AlphaDreamInstance = this;
		Instance = this;
	}

	public override void Dispose()
	{
		base.Dispose();
		AlphaDreamInstance = null;
	}
}
