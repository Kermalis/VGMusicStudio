#region License

/* Copyright (c) 2015 Andreas Grimme
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Sanford.Multimedia.Timers
{
    /// <summary>
    /// Queues and executes timer events in an internal worker thread.
    /// </summary>
    class ThreadTimerQueue
    {
        Stopwatch watch = Stopwatch.StartNew();
        Thread loop;
        List<Tick> tickQueue = new List<Tick>();

        public static ThreadTimerQueue Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ThreadTimerQueue();
                }
                return instance;

            }
        }
        static ThreadTimerQueue instance;

        private ThreadTimerQueue()
        {
        }

        public void Add(ThreadTimer timer)
        {
            lock (this)
            {
                var tick = new Tick
                {
                    Timer = timer,
                    Time = watch.Elapsed
                };
                tickQueue.Add(tick);
                tickQueue.Sort();

                if (loop == null)
                {
                    loop = new Thread(TimerLoop);
                    loop.Start();
                }
                Monitor.PulseAll(this);
            }
        }

        public void Remove(ThreadTimer timer)
        {
            lock (this)
            {
                int i = 0;
                for (; i < tickQueue.Count; ++i)
                {
                    if (tickQueue[i].Timer == timer)
                    {
                        break;
                    }
                }
                if (i < tickQueue.Count)
                {
                    tickQueue.RemoveAt(i);
                }
                Monitor.PulseAll(this);
            }
        }

        class Tick : IComparable
        {
            public ThreadTimer Timer;
            public TimeSpan Time;

            public int CompareTo(object obj)
            {
                var r = obj as Tick;
                if (r == null)
                {
                    return -1;
                }
                return Time.CompareTo(r.Time);
            }
        }

        static TimeSpan Min(TimeSpan x0, TimeSpan x1)
        {
            if (x0 > x1)
            {
                return x1;
            }
            else
            {
                return x0;
            }
        }

        /// <summary>
        /// The thread to execute the timer events
        /// </summary>
        private void TimerLoop()
        {
            lock (this)
            {
                TimeSpan maxTimeout = TimeSpan.FromMilliseconds(500);

                for (int queueEmptyCount = 0; queueEmptyCount < 3; ++queueEmptyCount)
                {
                    var waitTime = maxTimeout;
                    if (tickQueue.Count > 0)
                    {
                        waitTime = Min(tickQueue[0].Time - watch.Elapsed, waitTime);
                        queueEmptyCount = 0;
                    }

                    if (waitTime > TimeSpan.Zero)
                    {
                        Monitor.Wait(this, waitTime);
                    }

                    if (tickQueue.Count > 0)
                    {
                        var tick = tickQueue[0];
                        var mode = tick.Timer.Mode;
                        Monitor.Exit(this);
                        tick.Timer.DoTick();
                        Monitor.Enter(this);
                        if (mode == TimerMode.Periodic)
                        {
                            tick.Time += tick.Timer.PeriodTimeSpan;
                            tickQueue.Sort();
                        }
                        else
                        {
                            tickQueue.RemoveAt(0);
                        }
                    }
                }
                loop = null;
            }
        }
    }
}