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

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Dispatches IMidiMessages to their corresponding sink.
    /// </summary>
    public class MessageDispatcher
    {
        #region MessageDispatcher Members

        #region Events

        public event EventHandler<ChannelMessageEventArgs> ChannelMessageDispatched;

        public event EventHandler<SysExMessageEventArgs> SysExMessageDispatched;

        public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageDispatched;

        public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageDispatched;

        public event EventHandler<MetaMessageEventArgs> MetaMessageDispatched;

        #endregion

        /// <summary>
        /// Dispatches IMidiMessages to their corresponding sink.
        /// </summary>
        /// <param name="message">
        /// The IMidiMessage to dispatch.
        /// </param>
        public void Dispatch(MidiEvent evt, Track track)
        {
            #region Require

            var message = evt.MidiMessage;

            if(message == null)
            {
                throw new ArgumentNullException("message");
            }

            #endregion

            switch(message.MessageType)
            {
                case MessageType.Channel:
                    OnChannelMessageDispatched(new ChannelMessageEventArgs((ChannelMessage)message, evt.AbsoluteTicks), track);
                    break;

                case MessageType.SystemExclusive:
                    OnSysExMessageDispatched(new SysExMessageEventArgs((SysExMessage)message, evt.AbsoluteTicks), track);
                    break;

                case MessageType.Meta:
                    OnMetaMessageDispatched(new MetaMessageEventArgs((MetaMessage)message, evt.AbsoluteTicks), track);
                    break;

                case MessageType.SystemCommon:
                    OnSysCommonMessageDispatched(new SysCommonMessageEventArgs((SysCommonMessage)message, evt.AbsoluteTicks), track);
                    break;

                case MessageType.SystemRealtime:
                    switch(((SysRealtimeMessage)message).SysRealtimeType)
                    {
                        case SysRealtimeType.ActiveSense:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.ActiveSense, track);
                            break;

                        case SysRealtimeType.Clock:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.Clock, track);
                            break;

                        case SysRealtimeType.Continue:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.Continue, track);
                            break;

                        case SysRealtimeType.Reset:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.Reset, track);
                            break;

                        case SysRealtimeType.Start:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.Start, track);
                            break;

                        case SysRealtimeType.Stop:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.Stop, track);
                            break;

                        case SysRealtimeType.Tick:
                            OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs.Tick, track);
                            break;
                    }

                    break;
            }
        }

        protected virtual void OnChannelMessageDispatched(ChannelMessageEventArgs e, Track track)
        {
            EventHandler<ChannelMessageEventArgs> handler = ChannelMessageDispatched;

            if(handler != null)
            {
                handler(track, e);
            }
        }

        protected virtual void OnSysExMessageDispatched(SysExMessageEventArgs e, Track track)
        {
            EventHandler<SysExMessageEventArgs> handler = SysExMessageDispatched;

            if(handler != null)
            {
                handler(track, e);
            }
        }

        protected virtual void OnSysCommonMessageDispatched(SysCommonMessageEventArgs e, Track track)
        {
            EventHandler<SysCommonMessageEventArgs> handler = SysCommonMessageDispatched;

            if(handler != null)
            {
                handler(track, e);
            }
        }

        protected virtual void OnSysRealtimeMessageDispatched(SysRealtimeMessageEventArgs e, Track track)
        {
            EventHandler<SysRealtimeMessageEventArgs> handler = SysRealtimeMessageDispatched;

            if(handler != null)
            {
                handler(track, e);
            }
        }

        protected virtual void OnMetaMessageDispatched(MetaMessageEventArgs e, Track track)
        {
            EventHandler<MetaMessageEventArgs> handler = MetaMessageDispatched;

            if(handler != null)
            {
                handler(track, e);
            }
        }

        #endregion
    }
}
