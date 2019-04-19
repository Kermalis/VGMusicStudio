﻿using System.Diagnostics;
using System.Threading;

namespace Kermalis.MusicStudio.Util
{
    // Credit to ipatix
    class TimeBarrier
    {
        readonly Stopwatch sw;
        readonly double timerInterval;
        readonly double waitInterval;
        double lastTimeStamp;
        bool started;

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
