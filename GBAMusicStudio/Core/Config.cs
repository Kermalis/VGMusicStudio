using GBAMusicStudio.Util;
using Humanizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace GBAMusicStudio.Core
{
    internal class ASong
    {
        internal readonly ushort Index;
        internal readonly string Name;

        internal ASong(ushort index, string name)
        {
            Index = index; Name = name;
        }

        public override string ToString() => Name;
    }
    internal class APlaylist
    {
        internal readonly string Name;
        internal readonly ASong[] Songs;

        internal APlaylist(string name, ASong[] songs)
        {
            Name = name.Humanize(); Songs = songs;
        }

        public override string ToString() => string.Format("{0} - ({1})", Name, "Song".ToQuantity(Songs.Where(s => s.Name != "Playlist is empty.").Count()));
    }
    internal class AnEngine
    {
        internal readonly EngineType Type;
        internal readonly ReverbType ReverbType;
        internal readonly byte Reverb;
        internal readonly byte Volume; // 0-F
        internal readonly uint Frequency;

        internal AnEngine(EngineType type, ReverbType reverbType, byte reverb, byte volume, uint frequency)
        {
            Type = type; ReverbType = reverbType; Reverb = reverb; Volume = volume; Frequency = frequency;
        }

        public override string ToString() => Type.ToString();
    }
    internal class AGame
    {
        internal readonly string Code, Name, Creator;
        internal readonly AnEngine Engine;
        internal readonly uint[] SongTables, SongTableSizes;
        internal readonly List<APlaylist> Playlists;

        // MLSS only
        internal readonly uint VoiceTable, SampleTable, SampleTableSize;

        internal AGame(string code, string name, string creator, AnEngine engine, uint[] tables, uint[] tableSizes, List<APlaylist> playlists,
            uint voiceTable, uint sampleTable, uint sampleTableSize)
        {
            Code = code; Name = name; Creator = creator; Engine = engine;
            SongTables = tables; SongTableSizes = tableSizes;
            Playlists = playlists;

            VoiceTable = voiceTable; SampleTable = sampleTable; SampleTableSize = sampleTableSize;
        }

        public override string ToString() => Name;
    }
    internal class ARemap
    {
        internal readonly List<Tuple<byte, byte>> Remaps;

        internal ARemap(List<Tuple<byte, byte>> remaps)
        {
            Remaps = remaps;
        }
    }

    internal static class Config
    {
        static readonly uint DefaultTableSize = 1000;

        internal static byte DirectCount { get; private set; }
        internal static uint SampleRate { get; private set; }
        internal static bool MIDIKeyboardFixedVelocity { get; private set; }
        internal static bool TaskbarProgress { get; private set; }
        internal static byte RefreshRate { get; private set; }
        internal static bool CenterIndicators { get; private set; }
        internal static bool PanpotIndicators { get; private set; }
        internal static byte Volume { get; private set; }
        internal static HSLColor[] Colors { get; private set; }
        internal static Dictionary<string, ARemap> InstrumentRemaps { get; private set; }

        internal static Dictionary<string, AGame> Games { get; private set; }

        static Config() => Load();
        internal static void Load() { LoadConfig(); LoadGames(); }
        static void LoadConfig()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Config.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            DirectCount = (byte)Utils.ParseValue(mapping.Children["DirectCount"].ToString());
            SampleRate = (uint)Utils.ParseValue(mapping.Children["SampleRate"].ToString());
            MIDIKeyboardFixedVelocity = bool.Parse(mapping.Children["MIDIKeyboardFixedVelocity"].ToString());
            TaskbarProgress = bool.Parse(mapping.Children["TaskbarProgress"].ToString());
            RefreshRate = (byte)Utils.ParseValue(mapping.Children["RefreshRate"].ToString());
            CenterIndicators = bool.Parse(mapping.Children["CenterIndicators"].ToString());
            PanpotIndicators = bool.Parse(mapping.Children["PanpotIndicators"].ToString());
            Volume = (byte)Utils.ParseValue(mapping.Children["Volume"].ToString());

            var cmap = (YamlMappingNode)mapping.Children["Colors"];
            Colors = new HSLColor[256];
            foreach (var c in cmap)
            {
                uint i = (uint)Utils.ParseValue(c.Key.ToString());
                var children = ((YamlMappingNode)c.Value).Children;
                double h = 0, s = 0, l = 0;
                foreach (var v in children)
                {
                    if (v.Key.ToString() == "H")
                        h = byte.Parse(v.Value.ToString());
                    else if (v.Key.ToString() == "S")
                        s = byte.Parse(v.Value.ToString());
                    else if (v.Key.ToString() == "L")
                        l = byte.Parse(v.Value.ToString());
                }
                HSLColor color = new HSLColor(h, s, l);
                Colors[i] = Colors[i + 0x80] = color;
            }

            var rmap = (YamlMappingNode)mapping.Children["InstrumentRemaps"];
            InstrumentRemaps = new Dictionary<string, ARemap>();
            foreach (var r in rmap)
            {
                var remaps = new List<Tuple<byte, byte>>();

                var children = ((YamlMappingNode)r.Value).Children;
                foreach (var v in children)
                    remaps.Add(new Tuple<byte, byte>(byte.Parse(v.Key.ToString()), byte.Parse(v.Value.ToString())));

                InstrumentRemaps.Add(r.Key.ToString(), new ARemap(remaps));
            }
        }
        static void LoadGames()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Games.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            Games = new Dictionary<string, AGame>();
            foreach (var g in mapping)
            {
                string code, name, creator;
                EngineType engineType = EngineType.M4A; ReverbType reverbType = ReverbType.Normal;
                byte engineReverb = 0, engineVolume = 0xF; uint engineFrequency = 13379;
                uint[] tables, tableSizes;
                List<APlaylist> playlists;
                uint voiceTable = 0, sampleTable = 0, sampleTableSize = 0;

                code = g.Key.ToString();
                var game = (YamlMappingNode)g.Value;

                // Basic info
                name = game.Children["Name"].ToString();

                // SongTables
                var songTables = game.Children["SongTable"].ToString().Split(' ');
                tables = new uint[songTables.Length]; tableSizes = new uint[songTables.Length];
                for (int i = 0; i < songTables.Length; i++)
                    tables[i] = (uint)Utils.ParseValue(songTables[i]);

                // MLSS info
                if (game.Children.TryGetValue("VoiceTable", out YamlNode vTable))
                    voiceTable = (uint)Utils.ParseValue(vTable.ToString());
                if (game.Children.TryGetValue("SampleTable", out YamlNode sTable))
                    sampleTable = (uint)Utils.ParseValue(sTable.ToString());
                if (game.Children.TryGetValue("SampleTableSize", out YamlNode saTableSize))
                    sampleTableSize = (uint)Utils.ParseValue(saTableSize.ToString());

                // If we are to copy another game's config
                if (game.Children.TryGetValue("Copy", out YamlNode copy))
                    game = (YamlMappingNode)mapping.Children[copy];

                // SongTable Sizes
                string[] sizes = { };
                if (game.Children.TryGetValue("SongTableSize", out YamlNode soTableSize))
                    sizes = soTableSize.ToString().Split(' ');
                for (int i = 0; i < songTables.Length; i++)
                {
                    tableSizes[i] = DefaultTableSize;
                    if (i < sizes.Length)
                        tableSizes[i] = (uint)Utils.ParseValue(sizes[i]);
                }

                // Creator name
                creator = game.Children["Creator"].ToString();

                // Engine
                if (game.Children.TryGetValue("Engine", out YamlNode yeng))
                {
                    var eng = (YamlMappingNode)yeng;
                    if(eng.Children.TryGetValue("Type", out YamlNode type))
                        engineType = (EngineType)Enum.Parse(typeof(EngineType), type.ToString());
                    if(eng.Children.TryGetValue("ReverbType", out YamlNode rType))
                        reverbType = (ReverbType)Enum.Parse(typeof(ReverbType), rType.ToString());
                    if(eng.Children.TryGetValue("Reverb", out YamlNode reverb))
                        engineReverb = (byte)Utils.ParseValue(reverb.ToString());
                    if(eng.Children.TryGetValue("Volume", out YamlNode volume))
                        engineVolume = (byte)Utils.ParseValue(volume.ToString());
                    if(eng.Children.TryGetValue("Frequency", out YamlNode frequency))
                        engineFrequency = (uint)Utils.ParseValue(frequency.ToString());
                    
                }
                var engine = new AnEngine(engineType, reverbType, engineReverb, engineVolume, engineFrequency);

                // Load playlists
                playlists = new List<APlaylist>();
                if (game.Children.TryGetValue("Music", out YamlNode ymusic))
                {
                    var music = (YamlMappingNode)ymusic;
                    foreach (var kvp in music)
                    {
                        var songs = new List<ASong>();
                        foreach (var song in (YamlMappingNode)kvp.Value)
                            songs.Add(new ASong(ushort.Parse(song.Key.ToString()), song.Value.ToString())); // No hex values. It prevents putting in duplicates by having one hex and one dec of the same song index
                        playlists.Add(new APlaylist(kvp.Key.ToString(), songs.ToArray()));
                    }
                }

                // Full playlist
                if (!playlists.Any(p => p.Name == "Music"))
                    playlists.Insert(0, new APlaylist("Music", playlists.Select(p => p.Songs).UniteAll().OrderBy(s => s.Index).ToArray()));

                // If playlist is empty, add an empty entry
                for (int i = 0; i < playlists.Count; i++)
                    if (playlists[i].Songs.Length == 0)
                        playlists[i] = new APlaylist(playlists[i].Name, new ASong[] { new ASong(0, "Playlist is empty.") });

                Games.Add(code, new AGame(code, name, creator, engine, tables, tableSizes, playlists,
                    voiceTable, sampleTable, sampleTableSize));
            }
        }

        internal static byte GetRemap(byte voice, string key, bool from)
        {
            if (InstrumentRemaps.TryGetValue(key, out ARemap remap))
            {
                var r = remap.Remaps.FirstOrDefault(t => (from ? t.Item1 : t.Item2) == voice);
                if (r == null)
                    return voice;
                return from ? r.Item2 : r.Item1;
            }
            return voice;
        }
    }
}
