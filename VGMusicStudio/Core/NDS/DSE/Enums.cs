namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    enum EnvelopeState
    {
        Attack,
        Decay,
        Sustain,
        Release
    }

    public enum SampleFormat : ushort
    {
        PCM8 = 0x000,
        PCM16 = 0x100,
        ADPCM = 0x200
    }
}
