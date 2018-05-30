using GBAMusicStudio.Core;
using GBAMusicStudio.Core.M4A;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System;
using System.Linq;
using System.Timers;
using ThreadSafeList;

namespace GBAMusicStudio.MIDI
{
    internal static class MIDIKeyboard
    {
        static readonly bool bGood = false;
        static InputDevice inDevice;
        
        static byte volume = 127;
        static readonly Timer timer;
        static readonly ThreadSafeList<Instrument> instruments;

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
                    inDevice.ChannelMessageReceived += HandleChannelMessageReceived;
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

            instruments = new ThreadSafeList<Instrument>();
            timer = new Timer() { Interval = 1000 / 60 };
            timer.Elapsed += Tick;

            bGood = true;
        }

        internal static void Start()
        {
            if (!bGood) return;
            try
            {
                timer.Start();
                inDevice.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        internal static void Stop()
        {
            if (!bGood) return;
            try
            {
                timer.Stop();
                foreach (Instrument i in instruments)
                    i.Stop();
                inDevice.StopRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Tick(object sender, ElapsedEventArgs e)
        {
            foreach (Instrument i in instruments)
            {
                i.Tick();
                if (i.State == ADSRState.Dead)
                    instruments.Remove(i);
            }
            MusicPlayer.System.update();
        }

        static void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            Console.WriteLine("{0}\t\t{1}\t{2}\t{3}", e.Message.Command, e.Message.MidiChannel, e.Message.Data1, e.Message.Data2);

            byte vNum = 48; // Voice number from the voice table

            var note = (byte)e.Message.Data1;

            if (e.Message.Command == ChannelCommand.Controller && e.Message.Data1 == 7) // Volume
            {
                volume = (byte)e.Message.Data2;
            }
            else if ((e.Message.Command == ChannelCommand.NoteOn && e.Message.Data2 == 0) || e.Message.Command == ChannelCommand.NoteOff) // Note off
            {
                foreach (var i in instruments.Where(ins => ins.DisplayNote == note))
                    i.State = ADSRState.Releasing;
            }
            else if (e.Message.Command == ChannelCommand.NoteOn) // Note on
            {
                var i = new Instrument();
                i.Play(new Core.M4A.Track(16) { Voice = vNum, PrevVelocity = (byte)(volume * (Config.MIDIKeyboardFixedVelocity ? 127 : e.Message.Data2) / 127) }, note, 0xFF);
                instruments.Add(i);
            }
        }
        static void LogError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
        }
    }
}
