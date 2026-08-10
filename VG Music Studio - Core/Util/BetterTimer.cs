using System.Diagnostics;

namespace Kermalis.VGMusicStudio.Core.Util;

internal sealed class BetterTimer(double emulatedTicksPerSecond)
{
    private readonly Stopwatch _sw = new();
    private static readonly double _swInterval = 1d / Stopwatch.Frequency;
    private static readonly long _swFrequency = Stopwatch.Frequency;
    private readonly double _emulatedTicksPerSecond = emulatedTicksPerSecond;
    private readonly double _tickDelta = 1d / emulatedTicksPerSecond;
    private long _swPrevTimerFrame;
    private long _swCurTimerFrame;

    public double GetTicksPerSecond()
    {
        return _emulatedTicksPerSecond;
    }
    public double GetDeltaTick()
    {
        return _tickDelta;
    }
    public double GetDeltaTime()
    {
        _swCurTimerFrame = _sw.ElapsedTicks;
        var delta = (_swCurTimerFrame - _swPrevTimerFrame) * _swInterval;

        _swPrevTimerFrame = _swCurTimerFrame;
        return delta;
    }
    public static long GetFrequency()
    {
        return _swFrequency;
    }

    public void Start()
    {
        _sw.Start();
    }
    public void Stop()
    {
        _sw.Stop();
    }
}
