using System.Diagnostics;
using System.Threading;

namespace Kermalis.VGMusicStudio.Util
{
    // Credit to ipatix
    internal class TimeBarrier
    {
        private readonly Stopwatch _sw;
        private readonly double _timerInterval;
        private readonly double _waitInterval;
        private double _lastTimeStamp;
        private bool _started;

        public TimeBarrier(double framesPerSecond)
        {
            _waitInterval = 1.0 / framesPerSecond;
            _started = false;
            _sw = new Stopwatch();
            _timerInterval = 1.0 / Stopwatch.Frequency;
        }

        public void Wait()
        {
            if (!_started)
            {
                return;
            }
            double totalElapsed = _sw.ElapsedTicks * _timerInterval;
            double desiredTimeStamp = _lastTimeStamp + _waitInterval;
            double timeToWait = desiredTimeStamp - totalElapsed;
            if (timeToWait > 0)
            {
                Thread.Sleep((int)(timeToWait * 1000));
            }
            _lastTimeStamp = desiredTimeStamp;
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }
            _started = true;
            _lastTimeStamp = 0;
            _sw.Restart();
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }
            _started = false;
            _sw.Stop();
        }
    }
}
