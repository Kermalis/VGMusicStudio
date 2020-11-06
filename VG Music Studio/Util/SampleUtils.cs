namespace Kermalis.VGMusicStudio.Util
{
    internal static class SampleUtils
    {
        public static short[] PCMU8ToPCM16(byte[] data, int index, int length)
        {
            short[] ret = new short[length];
            for (int i = 0; i < length; i++)
            {
                byte b = data[index + i];
                ret[i] = (short)((b - 0x80) << 8);
            }
            return ret;
        }
    }
}
