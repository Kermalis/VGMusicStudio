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
using System.Collections;

namespace Sanford.Multimedia.Midi
{
    public class ChannelChaser
    {
        private ChannelMessage[,] controllerMessages;

        private ChannelMessage[] programChangeMessages;

        private ChannelMessage[] pitchBendMessages;

        private ChannelMessage[] channelPressureMessages;

        private ChannelMessage[] polyPressureMessages;

        public event EventHandler<ChasedEventArgs> Chased;

        public ChannelChaser()
        {
            int c = ChannelMessage.MidiChannelMaxValue + 1;
            int d = ShortMessage.DataMaxValue + 1;

            controllerMessages = new ChannelMessage[c, d];

            programChangeMessages = new ChannelMessage[c];
            pitchBendMessages = new ChannelMessage[c];
            channelPressureMessages = new ChannelMessage[c];
            polyPressureMessages = new ChannelMessage[c];
        }

        public void Process(ChannelMessage message)
        {
            switch(message.Command)
            {
                case ChannelCommand.Controller:
                    controllerMessages[message.MidiChannel, message.Data1] = message;
                    break;

                case ChannelCommand.ChannelPressure:
                    channelPressureMessages[message.MidiChannel] = message;
                    break;

                case ChannelCommand.PitchWheel:
                    pitchBendMessages[message.MidiChannel] = message;
                    break;

                case ChannelCommand.PolyPressure:
                    polyPressureMessages[message.MidiChannel] = message;
                    break;

                case ChannelCommand.ProgramChange:
                    programChangeMessages[message.MidiChannel] = message;
                    break;
            }
        }

        public void Chase()
        {
            ArrayList chasedMessages = new ArrayList();

            for(int c = 0; c <= ChannelMessage.MidiChannelMaxValue; c++)
            {
                for(int n = 0; n <= ShortMessage.DataMaxValue; n++)
                {
                    if(controllerMessages[c, n] != null)
                    {
                        chasedMessages.Add(controllerMessages[c, n]);

                        controllerMessages[c, n] = null;
                    }
                }

                if(programChangeMessages[c] != null)
                {
                    chasedMessages.Add(programChangeMessages[c]);

                    programChangeMessages[c] = null;
                }

                if(pitchBendMessages[c] != null)
                {
                    chasedMessages.Add(pitchBendMessages[c]);

                    pitchBendMessages[c] = null;
                }

                if(channelPressureMessages[c] != null)
                {
                    chasedMessages.Add(channelPressureMessages[c]);

                    channelPressureMessages[c] = null;
                }

                if(polyPressureMessages[c] != null)
                {
                    chasedMessages.Add(polyPressureMessages[c]);

                    polyPressureMessages[c] = null;
                }
            }

            OnChased(new ChasedEventArgs(chasedMessages));
        }

        public void Reset()
        {
            for(int c = 0; c <= ChannelMessage.MidiChannelMaxValue; c++)
            {
                for(int n = 0; n <= ShortMessage.DataMaxValue; n++)
                {
                    controllerMessages[c, n] = null;
                }

                programChangeMessages[c] = null;
                pitchBendMessages[c] = null;
                channelPressureMessages[c] = null;
                polyPressureMessages[c] = null;
            }
        }

        protected virtual void OnChased(ChasedEventArgs e)
        {
            EventHandler<ChasedEventArgs> handler = Chased;

            if(handler != null)
            {
                handler(this, e);
            }
        }
    }
}
