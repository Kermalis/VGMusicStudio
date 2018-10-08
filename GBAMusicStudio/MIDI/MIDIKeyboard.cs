using GBAMusicStudio.Core;
using Sanford.Multimedia.Midi;
using System;

namespace GBAMusicStudio.MIDI
{
    class MIDIKeyboard
    {
        static MIDIKeyboard instance;
        public static MIDIKeyboard Instance
        {
            get
            {
                if (instance == null)
                    instance = new MIDIKeyboard();
                return instance;
            }
        }

        const byte vNum = 1; // Voice number in the voice table
        bool enabled = false;
        InputDevice inDevice;
        
        Core.Track track;

        private MIDIKeyboard()
        {
            try
            {
                inDevice = new InputDevice(1);
                inDevice.ChannelMessageReceived += HandleChannelMessageReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Start()
        {
            if (inDevice != null && !enabled)
            {
                try
                {
                    inDevice.StartRecording();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
                track = new Core.Track(16);
                track.Init();
                track.Voice = vNum;
                enabled = true;
            }
        }
        public void Stop()
        {
            if (inDevice != null)
            {
                try
                {
                    inDevice.StopRecording();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            enabled = false;
        }

        void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            Console.WriteLine("{0}\t\t{1}\t{2}\t{3}", e.Message.Command, e.Message.MidiChannel, e.Message.Data1, e.Message.Data2);

            var note = (sbyte)e.Message.Data1;
            byte volumeOrVelocity = (byte)((e.Message.Data2 / (float)0x7F) * Engine.GetMaxVolume());

            if (e.Message.Command == ChannelCommand.Controller && e.Message.Data1 == 7) // Volume
            {
                track.Volume = volumeOrVelocity;
            }
            else if ((e.Message.Command == ChannelCommand.NoteOn && e.Message.Data2 == 0) || e.Message.Command == ChannelCommand.NoteOff) // Note off
            {
                // Has some input lag
                SoundMixer.Instance.ReleaseChannels(16, note);
            }
            else if (e.Message.Command == ChannelCommand.NoteOn) // Note on
            {
                // Has some input lag
                SongPlayer.Instance.PlayNote(track, note, Config.Instance.MIDIKeyboardFixedVelocity ? Engine.GetMaxVolume() : volumeOrVelocity, -1);
            }
        }
    }
}
