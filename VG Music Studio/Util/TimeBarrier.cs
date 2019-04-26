using System.Diagnostics;
using System.Threading;

namespace Kermalis.VGMusicStudio.Util
{
    // Credit to ipatix
    internal class TimeBarrier
    {
        private readonly Stopwatch sw;
        private readonly double timerInterval;
        private readonly double waitInterval;
        private double lastTimeStamp;
        private bool started;

        public TimeBarrier(double framesPerSecond)
        {
            waitInterval = 1.0 / framesPerSecond;
            started = false;
            sw = new Stopwatch();
            timerInterval = 1.0 / Stopwatch.Frequency;
        }

        public void Wait()
        {
            if (!started)
            {
                return;
            }
            double totalElapsed = sw.ElapsedTicks * timerInterval;
            double desiredTimeStamp = lastTimeStamp + waitInterval;
            double timeToWait = desiredTimeStamp - totalElapsed;
            if (timeToWait < 0)
            {
                timeToWait = 0;
            }
            Thread.Sleep((int)(timeToWait * 1000));
            lastTimeStamp = desiredTimeStamp;
        }

        public void Start()
        {
            if (started)
            {
                return;
            }
            started = true;
            lastTimeStamp = 0;
            sw.Restart();
        }

        public void Stop()
        {
            if (!started)
            {
                return;
            }
            started = false;
            sw.Stop();
        }
    }
}
