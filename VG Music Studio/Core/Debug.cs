using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core
{
#if DEBUG
    internal class Debug
    {
        public static void MIDIVolumeMerger(string f1, string f2)
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

        public static void EventScan(long numSongs)
        {
            var errors = new List<Tuple<long, Exception>>();
            var scans = new Dictionary<Type, List<long>>();
            for (long i = 0; i < numSongs; i++)
            {
                try
                {
                    Engine.Instance.Player.LoadSong(i);
                }
                catch (Exception ex)
                {
                    errors.Add(Tuple.Create(i, ex));
                    continue;
                }
                if (Engine.Instance.Player.Events != null)
                {
                    foreach (Type type in Engine.Instance.Player.Events.Where(ev => ev != null).SelectMany(ev => ev).Select(ev => ev.Command.GetType()))
                    {
                        if (scans.ContainsKey(type))
                        {
                            List<long> list = scans[type];
                            if (!list.Contains(i))
                            {
                                list.Add(i);
                            }
                        }
                        else
                        {
                            scans.Add(type, new List<long>() { i });
                        }
                    }
                }
            }
            foreach (Tuple<long, Exception> tup in errors)
            {
                Console.WriteLine("Exception in song {0} - {1}", tup.Item1, tup.Item2.Message);
            }
            foreach (KeyValuePair<Type, List<long>> kvp in scans)
            {
                Console.WriteLine("{0} ({1})", kvp.Key.Name, string.Join(", ", kvp.Value));
            }
        }
    }
#endif
}
