#region License

/* Copyright (c) 2006 Leslie Sanford
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

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

using System;
using System.Diagnostics;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Represents a collection of MidiEvents and a MIDI track within a 
    /// Sequence.
    /// </summary>
    public sealed partial class Track
    {
        #region Track Members

        #region Fields

        // The number of MidiEvents in the Track. Will always be at least 1
        // because the Track will always have an end of track message.
        private int count = 1;

        // The number of ticks to offset the end of track message.
        private int endOfTrackOffset = 0;

        // The first MidiEvent in the Track.
        private MidiEvent head = null;

        // The last MidiEvent in the Track, not including the end of track
        // message.
        private MidiEvent tail = null;

        // The end of track MIDI event.
        private MidiEvent endOfTrackMidiEvent;

        #endregion

        #region Construction

        public Track()
        {
            endOfTrackMidiEvent = new MidiEvent(this, Length, MetaMessage.EndOfTrackMessage);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts an IMidiMessage at the specified position in absolute ticks.
        /// </summary>
        /// <param name="position">
        /// The position in the Track in absolute ticks in which to insert the
        /// IMidiMessage.
        /// </param>
        /// <param name="message">
        /// The IMidiMessage to insert.
        /// </param>
        public void Insert(int position, IMidiMessage message)
        {
            #region Require

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", position,
                    "IMidiMessage position out of range.");
            }
            else if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            #endregion            

            MidiEvent newMidiEvent = new MidiEvent(this, position, message);

            if (head == null)
            {
                head = newMidiEvent;
                tail = newMidiEvent;
            }
            else if (position < head.AbsoluteTicks)
            {
                newMidiEvent.Next = head;
                head.Previous = newMidiEvent;
                head = newMidiEvent;
            }
            else if (position >= tail.AbsoluteTicks)
            {
                newMidiEvent.Previous = tail;
                tail.Next = newMidiEvent;
                tail = newMidiEvent;
                endOfTrackMidiEvent.SetAbsoluteTicks(Length);
                endOfTrackMidiEvent.Previous = tail;
            }
            else
            {
                MidiEvent current = head;

                while (!(current.AbsoluteTicks > position))
                    current = current.Next;

                newMidiEvent.Next = current;
                newMidiEvent.Previous = current.Previous;
                current.Previous.Next = newMidiEvent;
                current.Previous = newMidiEvent;
            }

            count++;

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Clears all of the MidiEvents, with the exception of the end of track
        /// message, from the Track.
        /// </summary>
        public void Clear()
        {
            head = tail = null;

            count = 1;

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Merges the specified Track with the current Track.
        /// </summary>
        /// <param name="trk">
        /// The Track to merge with.
        /// </param>
        public void Merge(Track trk)
        {
            #region Require

            if (trk == null)
            {
                throw new ArgumentNullException("trk");
            }

            #endregion

            #region Guard

            if (trk == this)
            {
                return;
            }
            else if (trk.Count == 1)
            {
                return;
            }

            #endregion

#if(DEBUG)
            int oldCount = Count;
#endif

            count += trk.Count - 1;

            MidiEvent a = head;
            MidiEvent b = trk.head;
            MidiEvent current = null;

            Debug.Assert(b != null);

            if (a != null && a.AbsoluteTicks <= b.AbsoluteTicks)
            {
                current = new MidiEvent(this, a.AbsoluteTicks, a.MidiMessage);
                a = a.Next;
            }
            else
            {
                current = new MidiEvent(this, b.AbsoluteTicks, b.MidiMessage);
                b = b.Next;
            }

            head = current;

            while (a != null && b != null)
            {
                while (a != null && a.AbsoluteTicks <= b.AbsoluteTicks)
                {
                    current.Next = new MidiEvent(this, a.AbsoluteTicks, a.MidiMessage);
                    current.Next.Previous = current;
                    current = current.Next;
                    a = a.Next;
                }

                if (a != null)
                {
                    while (b != null && b.AbsoluteTicks <= a.AbsoluteTicks)
                    {
                        current.Next = new MidiEvent(this, b.AbsoluteTicks, b.MidiMessage);
                        current.Next.Previous = current;
                        current = current.Next;
                        b = b.Next;
                    }
                }
            }

            while (a != null)
            {
                current.Next = new MidiEvent(this, a.AbsoluteTicks, a.MidiMessage);
                current.Next.Previous = current;
                current = current.Next;
                a = a.Next;
            }

            while (b != null)
            {
                current.Next = new MidiEvent(this, b.AbsoluteTicks, b.MidiMessage);
                current.Next.Previous = current;
                current = current.Next;
                b = b.Next;
            }

            tail = current;

            endOfTrackMidiEvent.SetAbsoluteTicks(Length);
            endOfTrackMidiEvent.Previous = tail;

            #region Ensure
#if(DEBUG)
            Debug.Assert(count == oldCount + trk.Count - 1);
#endif
            #endregion

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Removes the MidiEvent at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index into the Track at which to remove the MidiEvent.
        /// </param>
        public void RemoveAt(int index)
        {
            #region Require

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", index, "Track index out of range.");
            }
            else if (index == Count - 1)
            {
                throw new ArgumentException("Cannot remove the end of track event.", "index");
            }

            #endregion

            MidiEvent current = GetMidiEvent(index);

            if (current.Previous != null)
            {
                current.Previous.Next = current.Next;
            }
            else
            {
                Debug.Assert(current == head);

                head = head.Next;
            }

            if (current.Next != null)
            {
                current.Next.Previous = current.Previous;
            }
            else
            {
                Debug.Assert(current == tail);

                tail = tail.Previous;

                endOfTrackMidiEvent.SetAbsoluteTicks(Length);
                endOfTrackMidiEvent.Previous = tail;
            }

            current.Next = current.Previous = null;

            count--;

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Gets the MidiEvent at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the MidiEvent to get.
        /// </param>
        /// <returns>
        /// The MidiEvent at the specified index.
        /// </returns>
        public MidiEvent GetMidiEvent(int index)
        {
            #region Require

            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    "Track index out of range.");
            }

            #endregion

            MidiEvent result;

            if (index == Count - 1)
            {
                result = endOfTrackMidiEvent;
            }
            else
            {
                if (index < Count / 2)
                {
                    result = head;

                    for (int i = 0; i < index; i++)
                    {
                        result = result.Next;
                    }
                }
                else
                {
                    result = tail;

                    for (int i = Count - 2; i > index; i--)
                    {
                        result = result.Previous;
                    }
                }
            }

            #region Ensure

#if(DEBUG)
            if (index == Count - 1)
            {
                Debug.Assert(result.AbsoluteTicks == Length);
                Debug.Assert(result.MidiMessage == MetaMessage.EndOfTrackMessage);
            }
            else
            {
                MidiEvent t = head;

                for (int i = 0; i < index; i++)
                {
                    t = t.Next;
                }

                Debug.Assert(t == result);
            }
#endif

            #endregion

            return result;
        }

        public void Move(MidiEvent e, int newPosition)
        {
            #region Require

            if (e.Owner != this)
            {
                throw new ArgumentException("MidiEvent does not belong to this Track.");
            }
            else if (newPosition < 0)
            {
                throw new ArgumentOutOfRangeException("newPosition");
            }
            else if (e == endOfTrackMidiEvent)
            {
                throw new InvalidOperationException(
                    "Cannot move end of track message. Use the EndOfTrackOffset property instead.");
            }

            #endregion

            MidiEvent previous = e.Previous;
            MidiEvent next = e.Next;

            if (e.Previous != null && e.Previous.AbsoluteTicks > newPosition)
            {
                e.Previous.Next = e.Next;

                if (e.Next != null)
                {
                    e.Next.Previous = e.Previous;
                }

                while (previous != null && previous.AbsoluteTicks > newPosition)
                {
                    next = previous;
                    previous = previous.Previous;
                }
            }
            else if (e.Next != null && e.Next.AbsoluteTicks < newPosition)
            {
                e.Next.Previous = e.Previous;

                if (e.Previous != null)
                {
                    e.Previous.Next = e.Next;
                }

                while (next != null && next.AbsoluteTicks < newPosition)
                {
                    previous = next;
                    next = next.Next;
                }
            }

            if (previous != null)
            {
                previous.Next = e;
            }

            if (next != null)
            {
                next.Previous = e;
            }

            e.Previous = previous;
            e.Next = next;
            e.SetAbsoluteTicks(newPosition);

            if (newPosition < head.AbsoluteTicks)
            {
                head = e;
            }

            if (newPosition > tail.AbsoluteTicks)
            {
                tail = e;
            }

            endOfTrackMidiEvent.SetAbsoluteTicks(Length);
            endOfTrackMidiEvent.Previous = tail;

            #region Invariant

            AssertValid();

            #endregion
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            int c = 1;
            MidiEvent current = head;
            int ticks = 1;

            while (current != null)
            {
                ticks += current.DeltaTicks;

                if (current.Previous != null)
                {
                    Debug.Assert(current.AbsoluteTicks >= current.Previous.AbsoluteTicks);
                    Debug.Assert(current.DeltaTicks == current.AbsoluteTicks - current.Previous.AbsoluteTicks);
                }

                if (current.Next == null)
                {
                    Debug.Assert(tail == current);
                }

                current = current.Next;

                c++;
            }

            ticks += EndOfTrackOffset;

            Debug.Assert(ticks == Length, "Length mismatch");
            Debug.Assert(c == Count, "Count mismatch");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of MidiEvents in the Track.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets the length of the Track in ticks.
        /// </summary>
        public int Length
        {
            get
            {
                int length = EndOfTrackOffset;

                if (tail != null)
                {
                    length += tail.AbsoluteTicks;
                }

                return length;
            }
        }

        /// <summary>
        /// Gets or sets the end of track meta message position offset.
        /// </summary>
        public int EndOfTrackOffset
        {
            get
            {
                return endOfTrackOffset;
            }
            set
            {
                #region Require

                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("EndOfTrackOffset", value,
                        "End of track offset out of range.");
                }

                #endregion

                endOfTrackOffset = value;

                endOfTrackMidiEvent.SetAbsoluteTicks(Length);
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the Track.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        #endregion

        #endregion
    }
}