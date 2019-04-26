namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal enum EnvelopeState
    {
        Attack,
        Decay,
        Sustain,
        Release
    }

    internal enum LFOType : byte
    {
        Pitch,
        Volume,
        Panpot
    }
    internal enum InstrumentType : byte
    {
        PCM = 0x1,
        PSG = 0x2,
        Noise = 0x3,
        Drum = 0x10,
        KeySplit = 0x11
    }
    internal enum SWAVFormat : byte
    {
        PCM8,
        PCM16,
        ADPCM
    }
}
