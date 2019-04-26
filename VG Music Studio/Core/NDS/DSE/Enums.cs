namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal enum EnvelopeState
    {
        Attack,
        Decay,
        Sustain,
        Release
    }

    internal enum SampleFormat : ushort
    {
        PCM8 = 0x000,
        PCM16 = 0x100,
        ADPCM = 0x200
    }
}
