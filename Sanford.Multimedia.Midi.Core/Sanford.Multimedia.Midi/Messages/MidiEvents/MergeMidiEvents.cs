using System;
using System.Collections.Generic;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Takes a number of MidiEvents and merges them into a new single MidiEvent source
    /// </summary>
    public class MergeMidiEvents : MidiEvents
    {
        /// <summary>
		/// Gets the device ID and returns with a value of -3.
		/// </summary>
        public int DeviceID
        {
            get
            {
                return -3;
            }
        }

        readonly List<MidiEvents> FMidiEventsList = new List<MidiEvents>();

        /// <summary>
		/// Merges the MIDI events.
		/// </summary>
        public MergeMidiEvents(IEnumerable<MidiEvents> midiEvents)
        {
            foreach (var elem in midiEvents)
            {
                if (elem != null)
                    FMidiEventsList.Add(elem);
            }
        }

        /// <summary>
		/// Gets and returns the MIDI event sources from the events list.
		/// </summary>
        public IEnumerable<MidiEvents> EventSources
        {
            get
            {
                return FMidiEventsList;
            }
        }

        /// <summary>
		/// Disposes of the MergeMidiEvents when closed.
		/// </summary>
        public void Dispose()
        {
        }

        /// <summary>
		/// Handles the event for when a MIDI message is received.
		/// </summary>
        public event MidiMessageEventHandler MessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList)
                {
                    elem.MessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList)
                {
                    elem.MessageReceived -= value;
                }
            }
        }

        /// <summary>
		/// Handles the event for when a short message is received.
		/// </summary>
        public event EventHandler<ShortMessageEventArgs> ShortMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.ShortMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.ShortMessageReceived -= value;
                }
            }
        }

        /// <summary>
		/// Handles the event for when a channel message is received.
		/// </summary>
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList)
                {
                    elem.ChannelMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList)
                {
                    elem.ChannelMessageReceived -= value;
                }
            }
        }

        /// <summary>
		/// Handles the event for when an exclusive system message is received.
		/// </summary>
        public event EventHandler<SysExMessageEventArgs> SysExMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysExMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysExMessageReceived -= value;
                }
            }
        }

        /// <summary>
		/// Handles the event for when a common system message is received.
		/// </summary>
        public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysCommonMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysCommonMessageReceived -= value;
                }
            }
        }

        /// <summary>
		/// Handles the event for when a realtime system message is received.
		/// </summary>
        public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysRealtimeMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysRealtimeMessageReceived -= value;
                }
            }
        }
        
    }
}
