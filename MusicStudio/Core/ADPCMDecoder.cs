using Kermalis.MusicStudio.Util;

namespace Kermalis.MusicStudio.Core
{
    class ADPCMDecoder
    {
        static readonly short[] indexTable = new short[] { -1, -1, -1, -1, 2, 4, 6, 8 };
        static readonly short[] stepTable = new short[] // 89 entries
        {
            7, 8, 9, 10, 11, 12, 13, 14,
            16, 17, 19, 21, 23, 25, 28,
            31, 34, 37, 41, 45, 50, 55,
            60, 66, 73, 80, 88, 97, 107,
            118, 130, 143, 157, 173, 190, 209,
            230, 253, 279, 307, 337, 371, 408,
            449, 494, 544, 598, 658, 724, 796,
            876, 963, 1060, 1166, 1282, 1411, 1552,
            1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327,
            3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132,
            7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289,
            16818, 18500, 20350, 22385, 24623, 27086, 29794, short.MaxValue
        };

        readonly byte[] data;
        public short LastSample;
        public short StepIndex;
        public int DataOffset;
        public bool OnSecondNibble;

        public ADPCMDecoder(byte[] data)
        {
            LastSample = (short)(data[0] | (data[1] << 8));
            StepIndex = (short)((data[2] | (data[3] << 8)) & 0x7F);
            DataOffset = 4;
            this.data = data;
        }

        public static short[] ADPCMToPCM16(byte[] data)
        {
            var decoder = new ADPCMDecoder(data);
            var buffer = new short[(data.Length - 4) * 2];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = decoder.GetSample();
            }
            return buffer;
        }

        public short GetSample()
        {
            int val = (data[DataOffset] >> (OnSecondNibble ? 4 : 0)) & 0xF;
            int diff =
                (stepTable[StepIndex] / 8) +
                (stepTable[StepIndex] / 4 * (val & 1)) +
                (stepTable[StepIndex] / 2 * ((val >> 1) & 1)) +
                (stepTable[StepIndex] * ((val >> 2) & 1));

            LastSample = (short)Utils.Clamp((diff * ((((val >> 3) & 1) == 1) ? -1 : 1)) + LastSample, short.MinValue, short.MaxValue);
            StepIndex = (short)Utils.Clamp(StepIndex + indexTable[val & 7], 0, 88);
            if (OnSecondNibble)
            {
                DataOffset++;
            }
            OnSecondNibble = !OnSecondNibble;
            return LastSample;
        }
    }
}
