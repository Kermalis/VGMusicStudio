namespace Kermalis.MusicStudio.Core.NDS.SDAT
{
    enum EnvelopeState
    {
        Attack,
        Decay,
        Sustain,
        Release
    }

    enum LFOType : byte
    {
        Pitch,
        Volume,
        Panpot
    }
    enum InstrumentType : byte
    {
        PCM = 0x1,
        PSG = 0x2,
        Noise = 0x3,
        Drum = 0x10,
        KeySplit = 0x11
    }
    enum SWAVFormat : byte
    {
        PCM8,
        PCM16,
        ADPCM
    }
}
