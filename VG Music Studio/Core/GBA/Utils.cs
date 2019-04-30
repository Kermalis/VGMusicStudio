namespace Kermalis.VGMusicStudio.Core.GBA
{
    internal static class Utils
    {
        // All research says different values (60, 59.7, 59.73, 59.7275, 59.59), so I am unsure of the definitive fps. Check M4AUtils.FrequencyTable for a hint towards the actual value.
        //public const int AGB_FPS = 60;
        public const double AGB_FPS = 59.7275;
        public const int SystemClock = 16777216; // 16.777216 MHz (16*1024*1024 Hz) (Reading http://www.akkit.org/info/gbatek.htm "GBA Sound Control Registers")

        public const int CartridgeOffset = 0x08000000;
        public const int CartridgeCapacity = 0x02000000;
    }
}
