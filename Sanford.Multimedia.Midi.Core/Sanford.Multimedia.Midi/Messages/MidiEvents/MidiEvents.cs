
using System;

namespace Sanford.Multimedia.Midi
{
    
    /// <summary>
    /// An event source that combines all possible midi events
    /// </summary>
    public interface MidiEvents : IDisposable
    {
        /// <summary>
        /// Gets the device identifier of the input devive.
        /// Set it to any negative value for custom event sources.
        /// </summary>
        int DeviceID { get; }

        /// <summary>
        /// Occurs when any message was received. The underlying type of the message should be as specific as possible.
        /// Channel, Common, Realtime or SysEx.
        /// </summary>
        event MidiMessageEventHandler MessageReceived;

        /// <summary>
        /// All incoming midi short messages 
        /// </summary>
        event EventHandler<ShortMessageEventArgs> ShortMessageReceived;
        
        /// <summary>
        /// Channel messages like, note, controller, program, ...
        /// </summary>
        event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived;
       
        /// <summary>
        /// SysEx messages
        /// </summary>
        event EventHandler<SysExMessageEventArgs> SysExMessageReceived;
        
        /// <summary>
        /// Midi timecode, song position, song select, tune request
        /// </summary>
        event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived;

        /// <summary>
        /// Timing events, midi clock, start, stop, reset, active sense, tick
        /// </summary>
        event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived;
    }
    
    
    
    
}
