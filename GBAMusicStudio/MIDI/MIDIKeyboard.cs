using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System;

namespace GBAMusicStudio.MIDI
{
    internal static class MIDIKeyboard
    {
        static readonly bool bGood = false;
        static InputDevice inDevice;
        
        static MIDIKeyboard()
        {
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

        internal static void AddHandler(EventHandler<ChannelMessageEventArgs> handler)
        {
            if (!bGood) return;
            inDevice.ChannelMessageReceived += handler;
        }
        internal static void Start()
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

        static void LogChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            Console.WriteLine("{0}\t\t{1}\t{2}\t{3}", e.Message.Command, e.Message.MidiChannel, e.Message.Data1, e.Message.Data2);
        }
        static void LogError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
        }
    }
}
