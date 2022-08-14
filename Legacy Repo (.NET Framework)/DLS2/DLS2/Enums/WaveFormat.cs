namespace Kermalis.DLS2
{
    public enum WaveFormat : ushort
    {
        Unknown = 0,
        PCM = 1,
        MSADPCM = 2,
        Float = 3,
        ALaw = 6,
        MuLaw = 7,
        DVIADPCM = 17,
        IMAADPCM = 17,
        Extensible = 0xFFFE
    }
}
