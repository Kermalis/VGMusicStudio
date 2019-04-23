namespace Kermalis.VGMusicStudio.Core.GBA
{
    static class GBAUtils
    {
        // All research says different values (60, 59.7, 59.73, 59.7275, 59.59), so I am unsure of the definitive fps. Below are the buffer sizes for the M4A frequencies which gives a hint towards the actual value.
        //public const int AGB_FPS = 60;
        public const double AGB_FPS = 59.7275;
        // 5734 / 96 = 59.72916666666667
        // 7884 / 132 = 59.72727272727273
        // 10512 / 176 = 59.72727272727273
        // 13379 / 224 = 59.72767857142857
        // 15768 / 264 = 59.72727272727273
        // 18157 / 304 = 59.72697368421053
        // 21024 / 352 = 59.72727272727273
        // 26758 / 448 = 59.72767857142857
        // 31536 / 528 = 59.72727272727273
        // 36314 / 608 = 59.72697368421053
        // 40137 / 672 = 59.72767857142857
        // 42048 / 704 = 59.72727272727273
        public const int SystemClock = 16777216; // 16.777216 MHz (16*1024*1024 Hz) (Reading http://www.akkit.org/info/gbatek.htm "GBA Sound Control Registers")

        public const int CartridgeOffset = 0x08000000;
        public const int CartridgeCapacity = 0x02000000;
    }
}
