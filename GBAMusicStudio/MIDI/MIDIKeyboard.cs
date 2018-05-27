using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System;

namespace GBAMusicStudio.MIDI
{
    internal class MIDIKeyboard
    {
        bool bGood = false;
        InputDevice inDevice;

        internal static MIDIKeyboard Instance { get; private set; }
        internal MIDIKeyboard()
        {
            if (Instance != null) return;
            Instance = this;

            if (InputDevice.DeviceCount == 0)
            {
                Console.WriteLine("No MIDI input devices available.");
                return;
            }
            else
            {
                try
                {
                    inDevice = new InputDevice(1);
                    inDevice.ChannelMessageReceived += LogChannelMessageReceived;
                    //inDevice.SysCommonMessageReceived += LogSysCommonMessageReceived;
                    //inDevice.SysExMessageReceived += LogSysExMessageReceived;
                    //inDevice.SysRealtimeMessageReceived += LogSysRealtimeMessageReceived;
                    inDevice.Error += new EventHandler<ErrorEventArgs>(LogError);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }
            bGood = true;
        }

        internal void AddHandler(EventHandler<ChannelMessageEventArgs> handler)
        {
            if (!bGood) return;
            inDevice.ChannelMessageReceived += handler;
        }
        internal void Start()
        {
            if (!bGood) return;
            try
            {
                inDevice.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void LogChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            Console.WriteLine("{0}\t\t{1}\t{2}\t{3}", e.Message.Command, e.Message.MidiChannel, e.Message.Data1, e.Message.Data2);
        }
        void LogError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
        }
    }
}
