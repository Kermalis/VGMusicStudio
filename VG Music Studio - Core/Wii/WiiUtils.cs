namespace Kermalis.VGMusicStudio.Core.Wii
{
    internal static class WiiUtils
    {
        // (48 * 48 / 9) = 256, (256 * 65536) = 16,777,216 Hz = 16.777216 MHz, (16777216 * 7.24) = 121,467,043.84 Hz = 121.46704384 MHz
        // It's close enough to the 121.5MHz mentioned in the RVL DSP spec sheet
        public const int Macronix_DSP_Clock = 16_777_216;
    }
}
