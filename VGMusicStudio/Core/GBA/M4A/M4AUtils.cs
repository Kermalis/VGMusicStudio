namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    static class M4AUtils
    {
        public static byte[] ClockTable = new byte[49]
        {
            00, 01, 02, 03, 04, 05, 06, 07,
            08, 09, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 28, 30, 32, 36, 40, 42, 44,
            48, 52, 54, 56, 60, 64, 66, 68,
            72, 76, 78, 80, 84, 88, 90, 92,
            96
        };

        public static int Tri(int index)
        {
            index = (index - 64) & 0xFF;
            return (index < 128) ? index * 12 - 768 : 2304 - index * 12;
        }
    }
}
