using Kermalis.EndianBinaryIO;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core
{
#if DEBUG
    internal static class VGMSDebug
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

        public static void EventScan(List<Config.Song> songs, bool showIndexes)
        {
            Console.WriteLine($"{nameof(EventScan)} started.");
            var scans = new Dictionary<string, List<Config.Song>>();
            foreach (Config.Song song in songs)
            {
                try
                {
                    Engine.Instance.Player.LoadSong(song.Index);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception loading {0} - {1}", showIndexes ? $"song {song.Index}" : $"\"{song.Name}\"", ex.Message);
                    continue;
                }
                if (Engine.Instance.Player.Events != null)
                {
                    foreach (string cmd in Engine.Instance.Player.Events.Where(ev => ev != null).SelectMany(ev => ev).Select(ev => ev.Command.Label).Distinct())
                    {
                        if (scans.ContainsKey(cmd))
                        {
                            scans[cmd].Add(song);
                        }
                        else
                        {
                            scans.Add(cmd, new List<Config.Song>() { song });
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, List<Config.Song>> kvp in scans.OrderBy(k => k.Key))
            {
                Console.WriteLine("{0} ({1})", kvp.Key, showIndexes ? string.Join(", ", kvp.Value.Select(s => s.Index)) : string.Join(", ", kvp.Value.Select(s => s.Name)));
            }
            Console.WriteLine($"{nameof(EventScan)} ended.");
        }

        public static void GBAGameCodeScan(string path)
        {
            Console.WriteLine($"{nameof(GBAGameCodeScan)} started.");
            var scans = new List<string>();
            foreach (string file in Directory.GetFiles(path, "*.gba", SearchOption.AllDirectories))
            {
                try
                {
                    using (var reader = new EndianBinaryReader(File.OpenRead(file)))
                    {
                        string gameCode = reader.ReadString(3, false, 0xAC);
                        char regionCode = reader.ReadChar(0xAF);
                        byte version = reader.ReadByte(0xBC);
                        scans.Add(string.Format("Code: {0}\tRegion: {1}\tVersion: {2}\tFile: {3}", gameCode, regionCode, version, file));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception loading \"{0}\" - {1}", file, ex.Message);
                }
            }
            foreach (string s in scans.OrderBy(s => s))
            {
                Console.WriteLine(s);
            }
            Console.WriteLine($"{nameof(GBAGameCodeScan)} ended.");
        }
    }
#endif
}
