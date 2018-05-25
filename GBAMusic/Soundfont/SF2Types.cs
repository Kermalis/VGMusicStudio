using System.Runtime.InteropServices;

namespace SoundFont
{
    public class SF2Types
    {
        // From the SF2 spec v2.1 page 19
        // Two bytes that can handle either two 8-bit values or a single 16-bit value
        [StructLayout(LayoutKind.Explicit)]
        public class GenAmountType
        {
            [FieldOffset(0)] byte byLo;
            [FieldOffset(1)] byte byHi;
            [FieldOffset(0)] short shAmount;
            [FieldOffset(0)] ushort wAmount;

            public short Value { get => shAmount; }

            public GenAmountType(ushort value = 0)
            {
                wAmount = value;
            }
            public GenAmountType(byte lo, byte hi)
            {
                byLo = lo;
                byHi = hi;
            }
        }

        // SF2 v2.1 spec page 20
        public enum SF2SampleLink : ushort
        {
            monoSample = 1,
            rightSample = 2,
            leftSample = 4,
            linkedSample = 8,
            RomMonoSample = 0x8001,
            RomRightSample = 0x8002,
            RomLeftSample = 0x8004,
            RomLinkedSample = 0x8008
        }

        // SF2 v2.1 spec page 38
        public enum SF2Generator : ushort
        {
            startAddrsOffset = 0,
            endAddrsOffset = 1,
            startloopAddrsOffset = 2,
            endloopAddrsOffset = 3,
            startAddrsCoarseOffset = 4,
            modLfoToPitch = 5,
            vibLfoToPitch = 6,
            modEnvToPitch = 7,
            initialFilterFc = 8,
            initialFilterQ = 9,
            modLfoToFilterFc = 10,
            modEnvToFilterFc = 11,
            endAddrsCoarseOffset = 12,
            modLfoToVolume = 13,
            chorusEffectsSend = 15,
            reverbEffectsSend = 16,
            pan = 17,
            delayModLFO = 21,
            freqModLFO = 22,
            delayVibLFO = 23,
            freqVibLFO = 24,
            delayModEnv = 25,
            attackModEnv = 26,
            holdModEnv = 27,
            decayModEnv = 28,
            sustainModEnv = 29,
            releaseModEnv = 30,
            keynumToModEnvHold = 31,
            keynumToModEnvDecay = 32,
            delayVolEnv = 33,
            attackVolEnv = 34,
            holdVolEnv = 35,
            decayVolEnv = 36,
            sustainVolEnv = 37,
            releaseVolEnv = 38,
            keynumToVolEnvHold = 39,
            keynumToVolEnvDecay = 40,
            instrument = 41,
            keyRange = 43,
            velRange = 44,
            startloopAddrsCoarseOffset = 45,
            keynum = 46,
            velocity = 47,
            initialAttenuation = 48,
            endloopAddrsCoarseOffset = 50,
            coarseTune = 51,
            fineTune = 52,
            sampleID = 53,
            sampleModes = 54,
            scaleTuning = 56,
            exclusiveClass = 57,
            overridingRootKey = 58,
            endOper = 60
        }

        // Modulator's public enumeration class
        // SF2 v2.1 spec page 50
        public enum SF2Modulator : ushort
        {
            none = 0,
            noteOnVelocity = 1,
            noteOnKey = 2,
            polyPressure = 10,
            chnPressure = 13,
            pitchWheel = 14,
            ptchWeelSensivity = 16
        }

        // SF2 v2.1 spec page 52
        public enum SF2Transform : ushort
        {
            linear = 0,
            concave = 1,
            convex = 2
        }
    }
}
