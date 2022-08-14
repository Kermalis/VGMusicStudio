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
using System.Collections.Generic;
using System.Threading;

namespace Sanford.Multimedia.Midi
{
    public sealed partial class Track
    {
        #region Iterators

        public IEnumerable<MidiEvent> Iterator()
        {
            MidiEvent current = head;

            while(current != null)
            {
                yield return current;

                current = current.Next;
            }

            current = endOfTrackMidiEvent;

            yield return current;
        }
        
        public IEnumerable<int> DispatcherIterator(MessageDispatcher dispatcher)
        {
            IEnumerator<MidiEvent> enumerator = Iterator().GetEnumerator();

            while(enumerator.MoveNext())
            {
                yield return enumerator.Current.AbsoluteTicks;

                dispatcher.Dispatch(enumerator.Current, this);
            }
        }

        public IEnumerable<int> TickIterator(int startPosition, 
            ChannelChaser chaser, MessageDispatcher dispatcher)
        {
            #region Require

            if(startPosition < 0)
            {
                throw new ArgumentOutOfRangeException("startPosition", startPosition,
                    "Start position out of range.");
            }

            #endregion

            IEnumerator<MidiEvent> enumerator = Iterator().GetEnumerator();

            bool notFinished = enumerator.MoveNext();
            IMidiMessage message;

            while(notFinished && enumerator.Current.AbsoluteTicks < startPosition)
            {
                message = enumerator.Current.MidiMessage;

                if(message.MessageType == MessageType.Channel)
                {
                    chaser.Process((ChannelMessage)message);
                }
                else if(message.MessageType == MessageType.Meta)
                {
                    dispatcher.Dispatch(enumerator.Current, this);
                }

                notFinished = enumerator.MoveNext();
            }

            chaser.Chase();

            int ticks = startPosition;

            while(notFinished)
            {
                while(ticks < enumerator.Current.AbsoluteTicks)
                {
                    yield return ticks;

                    ticks++;
                }

                yield return ticks;

                while(notFinished && enumerator.Current.AbsoluteTicks == ticks)
                {
                    dispatcher.Dispatch(enumerator.Current, this);

                    notFinished = enumerator.MoveNext();    
                }

                ticks++;
            }
        }

        #endregion
    }
}
