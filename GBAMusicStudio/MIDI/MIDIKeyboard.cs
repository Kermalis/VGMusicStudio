using GBAMusicStudio.Core;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System;

namespace GBAMusicStudio.MIDI
{
    static class MIDIKeyboard
    {
        const byte vNum = 1; // Voice number in the voice table
        static readonly bool bGood = false;
        static InputDevice inDevice;

        static Core.Track track;

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

            bGood = true;
        }

        public static void Start()
        {
            if (!bGood) return;
            try
            {
                inDevice.StartRecording();
                track = new Core.Track(16);
                track.Init();
                track.Voice = vNum;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void Stop()
        {
            if (!bGood) return;
            try
            {
                inDevice.StopRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            Console.WriteLine("{0}\t\t{1}\t{2}\t{3}", e.Message.Command, e.Message.MidiChannel, e.Message.Data1, e.Message.Data2);

            var note = (sbyte)e.Message.Data1;
            byte volumeOrVelocity = (byte)((e.Message.Data2 / 127f) * Engine.GetMaxVolume());

            if (e.Message.Command == ChannelCommand.Controller && e.Message.Data1 == 7) // Volume
            {
                track.Volume = volumeOrVelocity;
            }
            else if ((e.Message.Command == ChannelCommand.NoteOn && e.Message.Data2 == 0) || e.Message.Command == ChannelCommand.NoteOff) // Note off
            {
                SoundMixer.Instance.ReleaseChannels(16, note);
            }
            else if (e.Message.Command == ChannelCommand.NoteOn) // Note on
            {
                // Has some input lag
                SongPlayer.Instance.PlayNote(track, note, Config.Instance.MIDIKeyboardFixedVelocity ? Engine.GetMaxVolume() : volumeOrVelocity, -1);
            }
        }
        static void LogError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
        }
    }
}
