using Sanford.Multimedia.Midi;
using System;

namespace Kermalis.VGMusicStudio.Core
{
    class Test
    {
        public static void MIDIThing(string f1, string f2)
        {
            var midi1 = new Sequence(f1);
            var midi2 = new Sequence(f2);
            var baby = new Sequence(midi1.Division);

            for (int i = 0; i < midi1.Count; i++)
            {
                Track midi1Track = midi1[i];
                Track midi2Track = midi2[i];
                var babyTrack = new Track();
                baby.Add(babyTrack);

                for (int j = 0; j < midi1Track.Count; j++)
                {
                    MidiEvent e1 = midi1Track.GetMidiEvent(j);
                    if (e1.MidiMessage is ChannelMessage cm1 && cm1.Command == ChannelCommand.Controller && cm1.Data1 == (int)ControllerType.Volume)
                    {
                        MidiEvent e2 = midi2Track.GetMidiEvent(j);
                        var cm2 = (ChannelMessage)e2.MidiMessage;
                        babyTrack.Insert(e1.AbsoluteTicks, new ChannelMessage(ChannelCommand.Controller, cm1.MidiChannel, (int)ControllerType.Volume, Math.Max(cm1.Data2, cm2.Data2)));
                    }
                    else
                    {
                        babyTrack.Insert(e1.AbsoluteTicks, e1.MidiMessage);
                    }
                }
            }

            baby.Save(f1);
            baby.Save(f2);
        }
    }
}
