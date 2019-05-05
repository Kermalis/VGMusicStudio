namespace Kermalis.VGMusicStudio.Core.GBA
{
    internal static class Utils
    {
        public const double AGB_FPS = 59.7275;
        public const int SystemClock = 16777216; // 16.777216 MHz (16*1024*1024 Hz)

        public const int CartridgeOffset = 0x08000000;
        public const int CartridgeCapacity = 0x02000000;
    }
}
