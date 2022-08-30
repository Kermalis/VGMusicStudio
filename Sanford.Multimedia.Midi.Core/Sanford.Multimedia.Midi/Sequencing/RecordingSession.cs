using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// This class initializes the recording sessions.
    /// </summary>
    public class RecordingSession
    {
        private IClock clock;

        private List<TimestampedMessage> buffer = new List<TimestampedMessage>();

        private Track result = new Track();

        /// <summary>
		/// Main function for the recording sessions.
		/// </summary>
        public RecordingSession(IClock clock)
        {
            this.clock = clock;
        }

        /// <summary>
		/// Builds the tracks, sorts and compares between a buffer and a timestamp, then creates a timestamped message with the amount of ticks.
		/// </summary>
        public void Build()
        {
            result = new Track();

            buffer.Sort(new TimestampComparer());

            foreach(TimestampedMessage tm in buffer)
            {
                result.Insert(tm.ticks, tm.message);
            }
        }

        /// <summary>
		/// Removes all elements from the list.
		/// </summary>
        public void Clear()
        {
            buffer.Clear();
        }

        /// <summary>
		/// Gets and returns the track result for the recording session.
		/// </summary>
        public Track Result
        {
            get
            {
                return result;
            }
        }

        /// <summary>
		/// Records a channel message if the clock is running.
		/// </summary>
        public void Record(ChannelMessage message)
        {
            if(clock.IsRunning)
            {
                buffer.Add(new TimestampedMessage(clock.Ticks, message));
            }
        }

        /// <summary>
		/// Records an external system message if the clock is running.
		/// </summary>
        public void Record(SysExMessage message)
        {
            if(clock.IsRunning)
            {
                buffer.Add(new TimestampedMessage(clock.Ticks, message));
            }
        }

        private struct TimestampedMessage
        {
            public int ticks;

            public IMidiMessage message;

            public TimestampedMessage(int ticks, IMidiMessage message)
            {
                this.ticks = ticks;
                this.message = message;
            }
        }

        private class TimestampComparer : IComparer<TimestampedMessage>
        {
            #region IComparer<TimestampedMessage> Members

            public int Compare(TimestampedMessage x, TimestampedMessage y)
            {
                if(x.ticks > y.ticks)
                {
                    return 1;
                }
                else if(x.ticks < y.ticks)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            #endregion
        }
    }
}
