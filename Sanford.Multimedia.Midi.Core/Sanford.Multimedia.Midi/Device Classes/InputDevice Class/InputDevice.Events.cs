using System;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Handles the MIDI Message Events.
    /// </summary>
    /// <param name="message">
    /// This provides the basic functionality for all MIDI messages.
    /// </param>
    public delegate void MidiMessageEventHandler(IMidiMessage message);

    public partial class InputDevice
    {
        /// <summary>
        /// Gets or sets a value indicating whether the midi events should be posted on the same synchronization context as the device constructor was called.
        /// Default is <c>true</c>. If set to <c>false</c> the events are fired on the driver callback or the thread of the driver callback delegate queue, depending on the PostDriverCallbackToDelegateQueue property.
        /// </summary>
        /// <value>
        ///   <c>true</c> if midi events should be posted on the same synchronization context as the device constructor was called; otherwise, <c>false</c>.
        /// </value>
        public bool PostEventsOnCreationContext
        {
            get;
            set;
        }

        /// <summary>
        /// Occurs when any message was received. The underlying type of the message is as specific as possible.
        /// Channel, Common, Realtime or SysEx.
        /// </summary>
        public event MidiMessageEventHandler MessageReceived;

        /// <summary>
        /// Occurs when a short message was received.
        /// </summary>
        public event EventHandler<ShortMessageEventArgs> ShortMessageReceived;

        /// <summary>
        /// Occurs when a channel message was received.
        /// </summary>
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived;

        /// <summary>
        /// Occurs when a system ex message was received.
        /// </summary>
        public event EventHandler<SysExMessageEventArgs> SysExMessageReceived;

        /// <summary>
        /// Occurs when a system common message was received.
        /// </summary>
        public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived;

        /// <summary>
        /// Occurs when a system realtime message was received.
        /// </summary>
        public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived;

        /// <summary>
        /// Occurs when a invalid short message was received.
        /// </summary>
        public event EventHandler<InvalidShortMessageEventArgs> InvalidShortMessageReceived;

        /// <summary>
        /// Occurs when a invalid system ex message message was received.
        /// </summary>
        public event EventHandler<InvalidSysExMessageEventArgs> InvalidSysExMessageReceived;

        /// <summary>
        /// Occurs when a short message was sent.
        /// </summary>
        protected virtual void OnShortMessage(ShortMessageEventArgs e)
        {
            EventHandler<ShortMessageEventArgs> handler = ShortMessageReceived;

            if (handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                            {
                                handler(this, e);
                            }, null); 
                }
                else
                {
                    handler(this, e);
                }
            }
        }

        /// <summary>
        /// Occurs when a message was received.
        /// </summary>
        protected void OnMessageReceived(IMidiMessage message)
        {
            MidiMessageEventHandler handler = MessageReceived;

            if (handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(message);
                    }, null);
                }
                else
                {
                    handler(message);
                }
            }
        }

        /// <summary>
        /// Occurs when a channel message is received.
        /// </summary>
        protected virtual void OnChannelMessageReceived(ChannelMessageEventArgs e)
        {
            EventHandler<ChannelMessageEventArgs> handler = ChannelMessageReceived;

            if(handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(this, e);
                    }, null);
                }
                else
                {
                    handler(this, e);
                }
            }
        }

        /// <summary>
        /// Occurs when a system ex message is received.
        /// </summary>
        protected virtual void OnSysExMessageReceived(SysExMessageEventArgs e)
        {
            EventHandler<SysExMessageEventArgs> handler = SysExMessageReceived;

            if(handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(this, e);
                    }, null);
                }
                else
                {
                    handler(this, e);
                }
            }
        }

        /// <summary>
        /// Occurs when a system common message is received.
        /// </summary>
        protected virtual void OnSysCommonMessageReceived(SysCommonMessageEventArgs e)
        {
            EventHandler<SysCommonMessageEventArgs> handler = SysCommonMessageReceived;

            if(handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(this, e);
                    }, null);
                }
                else
                {
                    handler(this, e);
                }
            }
        }

        /// <summary>
        /// Occurs when a system realtime message is received.
        /// </summary>
        protected virtual void OnSysRealtimeMessageReceived(SysRealtimeMessageEventArgs e)
        {
            EventHandler<SysRealtimeMessageEventArgs> handler = SysRealtimeMessageReceived;

            if(handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(this, e);
                    }, null);
                }
                else
                {
                    handler(this, e);
                }
            }
        }

        /// <summary>
        /// Occurs when an invalid short message is received.
        /// </summary>
        protected virtual void OnInvalidShortMessageReceived(InvalidShortMessageEventArgs e)
        {
            EventHandler<InvalidShortMessageEventArgs> handler = InvalidShortMessageReceived;

            if(handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(this, e);
                    }, null);
                }
                else
                {
                    handler(this, e);
                }
            }
        }

        /// <summary>
        /// Occurs when an invalid system ex message is received.
        /// </summary>
        protected virtual void OnInvalidSysExMessageReceived(InvalidSysExMessageEventArgs e)
        {
            EventHandler<InvalidSysExMessageEventArgs> handler = InvalidSysExMessageReceived;

            if(handler != null)
            {
                if (PostEventsOnCreationContext)
                {
                    context.Post(delegate (object dummy)
                    {
                        handler(this, e);
                    }, null);
                }
                else
                {
                    handler(this, e);
                }
            }
        }
    }
}
