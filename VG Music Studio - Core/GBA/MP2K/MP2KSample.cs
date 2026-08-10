using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal class MP2KSample
{
    public SampleHeader Header;
    public sbyte[] PCMData;

    public int Position;
    public float MidCFrequency;
    public bool LoopEnabled;

    public MP2KSample(ReadOnlySpan<byte> src, bool isPCM4 = false)
    {
        if (isPCM4)
        {
            PCMData = new sbyte[16];
            for (int i = 0; i < 16; i++)
            {
                PCMData[i] = (sbyte)src[i];
            }
        }
        else
        {
            Header = new SampleHeader(src);
            PCMData = new sbyte[Header.Length];
            ReadOnlySpan<byte> samples = src.Slice(16, Header.Length);
            for (int i = 0; i < Header.Length; i++)
            {
                PCMData[i] = (sbyte)samples[i];
            }
            Position = 0;
            MidCFrequency = Header.SampleRate / 1024.0f;
            LoopEnabled = Convert.ToBoolean(Header.DoesLoop);

            if (Header.LoopOffset > Header.Length)
            {
                Header.LoopOffset = 0;
            }
            if (Header.LoopOffset == Header.Length)
            {
                LoopEnabled = false;
            }
        }
    }
}
