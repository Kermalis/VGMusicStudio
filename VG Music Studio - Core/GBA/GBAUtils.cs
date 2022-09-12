namespace Kermalis.VGMusicStudio.Core.GBA;

internal static class GBAUtils
{
	public const double AGB_FPS = 59.7275;
	public const int SYSTEM_CLOCK = 16_777_216; // 16.777216 MHz (16*1024*1024 Hz)

	public const int CARTRIDGE_OFFSET = 0x08_000_000;
	public const int CARTRIDGE_CAPACITY = 0x02_000_000;

	public static readonly string[] PSGTypes = new string[4] { "Square 1", "Square 2", "PCM4", "Noise" };
}
