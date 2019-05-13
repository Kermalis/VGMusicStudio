namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal enum EnvelopeState
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Hold = 3,
        Decay = 4,
        Decay2 = 5,
        Six = 6,
        Seven = 7,
        Eight = 8
    }

    internal enum SampleFormat : ushort
    {
        PCM8 = 0x000,
        PCM16 = 0x100,
        ADPCM = 0x200
    }
}
