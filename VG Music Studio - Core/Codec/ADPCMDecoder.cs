using System;

namespace Kermalis.VGMusicStudio.Core.Codec;

internal struct ADPCMDecoder
{
    private static ReadOnlySpan<short> IndexTable => new short[8]
    {
        -1, -1, -1, -1, 2, 4, 6, 8,
    };
    private static ReadOnlySpan<short> StepTable => new short[89]
    {
        00007, 00008, 00009, 00010, 00011, 00012, 00013, 00014,
        00016, 00017, 00019, 00021, 00023, 00025, 00028, 00031,
        00034, 00037, 00041, 00045, 00050, 00055, 00060, 00066,
        00073, 00080, 00088, 00097, 00107, 00118, 00130, 00143,
        00157, 00173, 00190, 00209, 00230, 00253, 00279, 00307,
        00337, 00371, 00408, 00449, 00494, 00544, 00598, 00658,
        00724, 00796, 00876, 00963, 01060, 01166, 01282, 01411,
        01552, 01707, 01878, 02066, 02272, 02499, 02749, 03024,
        03327, 03660, 04026, 04428, 04871, 05358, 05894, 06484,
        07132, 07845, 08630, 09493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794,
        32767,
    };

    private byte[] _data;
    public short LastSample;
    public short StepIndex;
    public int DataOffset;
    public bool OnSecondNibble;

    public void Init(byte[] data)
    {
        _data = data;
        LastSample = (short)(data[0] | (data[1] << 8));
        StepIndex = (short)((data[2] | (data[3] << 8)) & 0x7F);
        DataOffset = 4;
        OnSecondNibble = false;
    }

    public static short[] ADPCMToPCM16(byte[] data)
    {
        var decoder = new ADPCMDecoder();
        decoder.Init(data);

        short[] buffer = new short[(data.Length - 4) * 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = decoder.GetSample();
        }
        return buffer;
    }

    public short GetSample()
    {
        int val = (_data[DataOffset] >> (OnSecondNibble ? 4 : 0)) & 0xF;
        short step = StepTable[StepIndex];
        int diff =
            (step / 8) +
            (step / 4 * (val & 1)) +
            (step / 2 * ((val >> 1) & 1)) +
            (step * ((val >> 2) & 1));

        int a = (diff * ((((val >> 3) & 1) == 1) ? -1 : 1)) + LastSample;
        if (a < short.MinValue)
        {
            a = short.MinValue;
        }
        else if (a > short.MaxValue)
        {
            a = short.MaxValue;
        }
        LastSample = (short)a;

        a = StepIndex + IndexTable[val & 7];
        if (a < 0)
        {
            a = 0;
        }
        else if (a > 88)
        {
            a = 88;
        }
        StepIndex = (short)a;

        if (OnSecondNibble)
        {
            DataOffset++;
        }
        OnSecondNibble = !OnSecondNibble;
        return LastSample;
    }
}
