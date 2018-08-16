using GBAMusicStudio.Core;
using System.Diagnostics;
using System.Threading;

namespace GBAMusicStudio.Util
{
    // Credit to ipatix
    class TimeBarrier
    {
        readonly Stopwatch sw;
        readonly double timerInterval;
        readonly double waitInterval;
        double lastTimeStamp;
        bool started;

        public TimeBarrier()
        {
            waitInterval = 1d / (Engine.AGB_FPS * Engine.INTERFRAMES);
            started = false;
            sw = new Stopwatch();
            timerInterval = 1d / Stopwatch.Frequency;
        }

        public void Wait()
        {
            if (!started) return;
            double totalElapsed = sw.ElapsedTicks * timerInterval;
            double desiredTimeStamp = lastTimeStamp + waitInterval;
            double timeToWait = desiredTimeStamp - totalElapsed;
            if (timeToWait < 0)
                timeToWait = 0;
            int millisToWait = (int)(timeToWait * 1000d);
            Thread.Sleep(millisToWait);
            lastTimeStamp = desiredTimeStamp;
        }

        public void Start()
        {
            if (started) return;
            started = true;
            lastTimeStamp = 0;
            sw.Restart();
        }

        public void Stop()
        {
            if (!started) return;
            started = false;
            sw.Stop();
        }
    }
}
