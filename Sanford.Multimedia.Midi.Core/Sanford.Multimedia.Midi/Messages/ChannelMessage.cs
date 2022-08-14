#region License

/* Copyright (c) 2005 Leslie Sanford
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
using System.ComponentModel;
using System.Diagnostics;

namespace Sanford.Multimedia.Midi
{
    #region Channel Command Types

    /// <summary>
    /// Defines constants for ChannelMessage types.
    /// </summary>
    public enum ChannelCommand 
    {
        /// <summary>
        /// Represents the note-off command type.
        /// </summary>
        NoteOff = 0x80,

        /// <summary>
        /// Represents the note-on command type.
        /// </summary>
        NoteOn = 0x90,

        /// <summary>
        /// Represents the poly pressure (aftertouch) command type.
        /// </summary>
        PolyPressure = 0xA0,

        /// <summary>
        /// Represents the controller command type.
        /// </summary>
        Controller = 0xB0,  
  
        /// <summary>
        /// Represents the program change command type.
        /// </summary>
        ProgramChange = 0xC0,

        /// <summary>
        /// Represents the channel pressure (aftertouch) command 
        /// type.
        /// </summary>
        ChannelPressure = 0xD0,   
     
        /// <summary>
        /// Represents the pitch wheel command type.
        /// </summary>
        PitchWheel = 0xE0
    }

    #endregion

    #region Controller Types

    /// <summary>
    /// Defines constants for controller types.
    /// </summary>
    public enum ControllerType
    {
        /// <summary>
        /// The Bank Select coarse.
        /// </summary>
        BankSelect,

        /// <summary>
        /// The Modulation Wheel coarse.
        /// </summary>
        ModulationWheel,

        /// <summary>
        /// The Breath Control coarse.
        /// </summary>
        BreathControl,

        /// <summary>
        /// The Foot Pedal coarse.
        /// </summary>
        FootPedal = 4,

        /// <summary>
        /// The Portamento Time coarse.
        /// </summary>
        PortamentoTime,

        /// <summary>
        /// The Data Entry Slider coarse.
        /// </summary>
        DataEntrySlider,

        /// <summary>
        /// The Volume coarse.
        /// </summary>
        Volume,

        /// <summary>
        /// The Balance coarse.
        /// </summary>
        Balance,

        /// <summary>
        /// The Pan position coarse.
        /// </summary>
        Pan = 10,

        /// <summary>
        /// The Expression coarse.
        /// </summary>
        Expression,

        /// <summary>
        /// The Effect Control 1 coarse.
        /// </summary>
        EffectControl1,

        /// <summary>
        /// The Effect Control 2 coarse.
        /// </summary>
        EffectControl2,

        /// <summary>
        /// The General Puprose Slider 1
        /// </summary>
        GeneralPurposeSlider1 = 16,

        /// <summary>
        /// The General Puprose Slider 2
        /// </summary>
        GeneralPurposeSlider2,

        /// <summary>
        /// The General Puprose Slider 3
        /// </summary>
        GeneralPurposeSlider3,

        /// <summary>
        /// The General Puprose Slider 4
        /// </summary>
        GeneralPurposeSlider4,

        /// <summary>
        /// The Bank Select fine.
        /// </summary>
        BankSelectFine = 32,

        /// <summary>
        /// The Modulation Wheel fine.
        /// </summary>
        ModulationWheelFine,

        /// <summary>
        /// The Breath Control fine.
        /// </summary>
        BreathControlFine,

        /// <summary>
        /// The Foot Pedal fine.
        /// </summary>
        FootPedalFine = 36,

        /// <summary>
        /// The Portamento Time fine.
        /// </summary>
        PortamentoTimeFine,

        /// <summary>
        /// The Data Entry Slider fine.
        /// </summary>
        DataEntrySliderFine,

        /// <summary>
        /// The Volume fine.
        /// </summary>
        VolumeFine,

        /// <summary>
        /// The Balance fine.
        /// </summary>
        BalanceFine,

        /// <summary>
        /// The Pan position fine.
        /// </summary>
        PanFine = 42,

        /// <summary>
        /// The Expression fine.
        /// </summary>
        ExpressionFine,

        /// <summary>
        /// The Effect Control 1 fine.
        /// </summary>
        EffectControl1Fine,

        /// <summary>
        /// The Effect Control 2 fine.
        /// </summary>
        EffectControl2Fine,

        /// <summary>
        /// The Hold Pedal 1.
        /// </summary>
        HoldPedal1 = 64,

        /// <summary>
        /// The Portamento.
        /// </summary>
        Portamento,

        /// <summary>
        /// The Sustenuto Pedal.
        /// </summary>
        SustenutoPedal,

        /// <summary>
        /// The Soft Pedal.
        /// </summary>
        SoftPedal,

        /// <summary>
        /// The Legato Pedal.
        /// </summary>
        LegatoPedal,

        /// <summary>
        /// The Hold Pedal 2.
        /// </summary>
        HoldPedal2,

        /// <summary>
        /// The Sound Variation.
        /// </summary>
        SoundVariation,

        /// <summary>
        /// The Sound Timbre.
        /// </summary>
        SoundTimbre,

        /// <summary>
        /// The Sound Release Time.
        /// </summary>
        SoundReleaseTime,

        /// <summary>
        /// The Sound Attack Time.
        /// </summary>
        SoundAttackTime,

        /// <summary>
        /// The Sound Brightness.
        /// </summary>
        SoundBrightness,

        /// <summary>
        /// The Sound Control 6.
        /// </summary>
        SoundControl6,

        /// <summary>
        /// The Sound Control 7.
        /// </summary>
        SoundControl7,

        /// <summary>
        /// The Sound Control 8.
        /// </summary>
        SoundControl8,

        /// <summary>
        /// The Sound Control 9.
        /// </summary>
        SoundControl9,

        /// <summary>
        /// The Sound Control 10.
        /// </summary>
        SoundControl10,

        /// <summary>
        /// The General Purpose Button 1.
        /// </summary>
        GeneralPurposeButton1,

        /// <summary>
        /// The General Purpose Button 2.
        /// </summary>
        GeneralPurposeButton2,

        /// <summary>
        /// The General Purpose Button 3.
        /// </summary>
        GeneralPurposeButton3,

        /// <summary>
        /// The General Purpose Button 4.
        /// </summary>
        GeneralPurposeButton4,

        /// <summary>
        /// The Effects Level.
        /// </summary>
        EffectsLevel = 91,

        /// <summary>
        /// The Tremolo Level.
        /// </summary>
        TremoloLevel,
        
        /// <summary>
        /// The Chorus Level.
        /// </summary>
        ChorusLevel,

        /// <summary>
        /// The Celeste Level.
        /// </summary>
        CelesteLevel,

        /// <summary>
        /// The Phaser Level.
        /// </summary>
        PhaserLevel,

        /// <summary>
        /// The Data Button Increment.
        /// </summary>
        DataButtonIncrement,

        /// <summary>
        /// The Data Button Decrement.
        /// </summary>
        DataButtonDecrement,

        /// <summary>
        /// The NonRegistered Parameter Fine.
        /// </summary>
        NonRegisteredParameterFine,

        /// <summary>
        /// The NonRegistered Parameter Coarse.
        /// </summary>
        NonRegisteredParameterCoarse,

        /// <summary>
        /// The Registered Parameter Fine.
        /// </summary>
        RegisteredParameterFine,

        /// <summary>
        /// The Registered Parameter Coarse.
        /// </summary>
        RegisteredParameterCoarse,

        /// <summary>
        /// The All Sound Off.
        /// </summary>
        AllSoundOff = 120,

        /// <summary>
        /// The All Controllers Off.
        /// </summary>
        AllControllersOff,

        /// <summary>
        /// The Local Keyboard.
        /// </summary>
        LocalKeyboard,
        
        /// <summary>
        /// The All Notes Off.
        /// </summary>
        AllNotesOff,

        /// <summary>
        /// The Omni Mode Off.
        /// </summary>
        OmniModeOff,

        /// <summary>
        /// The Omni Mode On.
        /// </summary>
        OmniModeOn,

        /// <summary>
        /// The Mono Operation.
        /// </summary>
        MonoOperation,

        /// <summary>
        /// The Poly Operation.
        /// </summary>
        PolyOperation
    }

    #endregion

	/// <summary>
	/// Represents MIDI channel messages.
	/// </summary>
	[ImmutableObject(true)]
	public sealed class ChannelMessage : ShortMessage
	{
        #region ChannelEventArgs Members

        #region Constants

        //
        // Bit manipulation constants.
        //

        private const int MidiChannelMask = ~15;
        private const int CommandMask = ~240;

        /// <summary>
        /// Maximum value allowed for MIDI channels.
        /// </summary> 
        public const int MidiChannelMaxValue = 15;

        #endregion

        #region Construction
        
        /// <summary>
        /// Initializes a new instance of the ChannelEventArgs class with the
        /// specified command, MIDI channel, and data 1 values.
        /// </summary>
        /// <param name="command">
        /// The command value.
        /// </param>
        /// <param name="midiChannel">
        /// The MIDI channel.
        /// </param>
        /// <param name="data1">
        /// The data 1 value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If midiChannel is less than zero or greater than 15. Or if 
        /// data1 is less than zero or greater than 127.
        /// </exception>
        public ChannelMessage(ChannelCommand command, int midiChannel, int data1)
        { 
            msg = 0;

            msg = PackCommand(msg, command);
            msg = PackMidiChannel(msg, midiChannel);
            msg = PackData1(msg, data1);

            #region Ensure

            Debug.Assert(Command == command);
            Debug.Assert(MidiChannel == midiChannel);
            Debug.Assert(Data1 == data1);

            #endregion
        }        

        /// <summary>
        /// Initializes a new instance of the ChannelEventArgs class with the 
        /// specified command, MIDI channel, data 1, and data 2 values.
        /// </summary>
        /// <param name="command">
        /// The command value.
        /// </param>
        /// <param name="midiChannel">
        /// The MIDI channel.
        /// </param>
        /// <param name="data1">
        /// The data 1 value.
        /// </param>
        /// <param name="data2">
        /// The data 2 value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If midiChannel is less than zero or greater than 15. Or if 
        /// data1 or data 2 is less than zero or greater than 127. 
        /// </exception>
        public ChannelMessage(ChannelCommand command, int midiChannel, 
            int data1, int data2)
        {
            msg = 0;

            msg = PackCommand(msg, command);
            msg = PackMidiChannel(msg, midiChannel);
            msg = PackData1(msg, data1);
            msg = PackData2(msg, data2);

            #region Ensure

            Debug.Assert(Command == command);
            Debug.Assert(MidiChannel == midiChannel);
            Debug.Assert(Data1 == data1);
            Debug.Assert(Data2 == data2);

            #endregion
        }

        internal ChannelMessage(int message)
        {
            this.msg = message;            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a value for the current ChannelEventArgs suitable for use in 
        /// hashing algorithms.
        /// </summary>
        /// <returns>
        /// A hash code for the current ChannelEventArgs.
        /// </returns>
        public override int GetHashCode()
        {
            return msg;
        }

        /// <summary>
        /// Determines whether two ChannelEventArgs instances are equal.
        /// </summary>
        /// <param name="obj">
        /// The ChannelMessageEventArgs to compare with the current ChannelEventArgs.
        /// </param>
        /// <returns>
        /// <b>true</b> if the specified object is equal to the current 
        /// ChannelMessageEventArgs; otherwise, <b>false</b>.
        /// </returns>
        public override bool Equals(object obj)
        {
            #region Guard

            if(!(obj is ChannelMessage))
            {
                return false;
            }

            #endregion
            
            ChannelMessage e = (ChannelMessage)obj;            

            return this.msg == e.msg;
        }

        /// <summary>
        /// Returns a value indicating how many bytes are used for the 
        /// specified ChannelCommand.
        /// </summary>
        /// <param name="command">
        /// The ChannelCommand value to test.
        /// </param>
        /// <returns>
        /// The number of bytes used for the specified ChannelCommand.
        /// </returns>
        internal static int DataBytesPerType(ChannelCommand command)
        {
            int result;

            if(command == ChannelCommand.ChannelPressure ||
                command == ChannelCommand.ProgramChange)
            {
                result = 1;
            }
            else
            {
                result = 2;
            }

            return result;
        }
   
        /// <summary>
        /// Unpacks the command value from the specified integer channel 
        /// message.
        /// </summary>
        /// <param name="message">
        /// The message to unpack.
        /// </param>
        /// <returns>
        /// The command value for the packed message.
        /// </returns>
        internal static ChannelCommand UnpackCommand(int message)
        {
            return (ChannelCommand)(message & DataMask & MidiChannelMask);
        }
     
        /// <summary>
        /// Unpacks the MIDI channel from the specified integer channel 
        /// message.
        /// </summary>
        /// <param name="message">
        /// The message to unpack.
        /// </param>
        /// <returns>
        /// The MIDI channel for the pack message.
        /// </returns>
        internal static int UnpackMidiChannel(int message)
        {
            return message & DataMask & CommandMask;
        }

        /// <summary>
        /// Packs the MIDI channel into the specified integer message.
        /// </summary>
        /// <param name="message">
        /// The message into which the MIDI channel is packed.
        /// </param>
        /// <param name="midiChannel">
        /// The MIDI channel to pack into the message.
        /// </param>
        /// <returns>
        /// An integer message.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If midiChannel is less than zero or greater than 15.
        /// </exception>
        internal static int PackMidiChannel(int message, int midiChannel)
        {
            #region Preconditons

            if(midiChannel < 0 || midiChannel > MidiChannelMaxValue)
            {
                throw new ArgumentOutOfRangeException("midiChannel", midiChannel,
                    "MIDI channel out of range.");
            }

            #endregion

            return (message & MidiChannelMask) | midiChannel;
        }

        /// <summary>
        /// Packs the command value into an integer message.
        /// </summary>
        /// <param name="message">
        /// The message into which the command is packed.
        /// </param>
        /// <param name="command">
        /// The command value to pack into the message.
        /// </param>
        /// <returns>
        /// An integer message.
        /// </returns>
        internal static int PackCommand(int message, ChannelCommand command)
        {
            return (message & CommandMask) | (int)command;
        }        

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets the channel command value.
        /// </summary>
        public ChannelCommand Command
        {
            get
            {
                return UnpackCommand(msg);
            }
        }
        
        /// <summary>
        /// Gets the MIDI channel.
        /// </summary>
        public int MidiChannel
        {
            get
            {
                return UnpackMidiChannel(msg);
            }
        }

        /// <summary>
        /// Gets the first data value.
        /// </summary>
        public int Data1
        {
            get
            {
                return UnpackData1(msg);
            }                
        }
        
        /// <summary>
        /// Gets the second data value.
        /// </summary>
        public int Data2
        {
            get
            {
                return UnpackData2(msg);
            }
        }

        /// <summary>
        /// Gets the EventType.
        /// </summary>
        public override MessageType MessageType
        {
            get
            {
                return MessageType.Channel;
            }
        }

        #endregion

        #endregion        
    }
}
