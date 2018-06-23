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
            Index = index;
            Name = name;
        }

        public override string ToString() => Name;
    }
    internal class APlaylist
    {
        internal readonly string Name;
        internal readonly ASong[] Songs;

        internal APlaylist(string name, ASong[] songs)
        {
            Name = name.Humanize();
            Songs = songs;
        }

        public override string ToString() => string.Format("{0} - ({1})", Name, "Song".ToQuantity(Songs.Where(s => s.Name != "Playlist is empty.").Count()));
    }
    internal class AGame
    {
        internal readonly string Code, Name, Creator;
        internal readonly uint[] SongTables;
        internal readonly List<APlaylist> Playlists;

        internal AGame(string code, string name, uint[] tables, string creator, List<APlaylist> playlists)
        {
            Code = code;
            Name = name;
            SongTables = tables;
            Creator = creator;
            Playlists = playlists;
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
        internal static byte DirectCount { get; private set; }
        internal static byte PSGVolume { get; private set; }
        internal static bool MIDIKeyboardFixedVelocity { get; private set; }
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
            DirectCount = (byte)Utils.ParseValue(mapping.Children[new YamlScalarNode("DirectCount")].ToString());
            PSGVolume = (byte)Utils.ParseValue(mapping.Children[new YamlScalarNode("PSGVolume")].ToString());
            MIDIKeyboardFixedVelocity = bool.Parse(mapping.Children[new YamlScalarNode("MIDIKeyboardFixedVelocity")].ToString());
            RefreshRate = (byte)Utils.ParseValue(mapping.Children[new YamlScalarNode("RefreshRate")].ToString());
            CenterIndicators = bool.Parse(mapping.Children[new YamlScalarNode("CenterIndicators")].ToString());
            PanpotIndicators = bool.Parse(mapping.Children[new YamlScalarNode("PanpotIndicators")].ToString());
            Volume = (byte)Utils.ParseValue(mapping.Children[new YamlScalarNode("Volume")].ToString());

            var cmap = (YamlMappingNode)mapping.Children[new YamlScalarNode("Colors")];
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

            var rmap = (YamlMappingNode)mapping.Children[new YamlScalarNode("InstrumentRemaps")];
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
                uint[] tables;
                List<APlaylist> playlists;

                code = g.Key.ToString();
                var game = (YamlMappingNode)g.Value;

                // Basic info
                name = game.Children[new YamlScalarNode("Name")].ToString();
                var songTables = game.Children[new YamlScalarNode("SongTable")].ToString().Split(' ');
                tables = new uint[songTables.Length];
                for (int i = 0; i < songTables.Length; i++)
                    tables[i] = (uint)Utils.ParseValue(songTables[i]);

                // If we are to copy another game's config
                if (game.Children.ContainsKey(new YamlScalarNode("Copy")))
                    game = (YamlMappingNode)mapping.Children[new YamlScalarNode(game.Children[new YamlScalarNode("Copy")].ToString())];

                // Creator name
                creator = game.Children[new YamlScalarNode("Creator")].ToString();

                // Load playlists
                playlists = new List<APlaylist>();
                if (game.Children.ContainsKey(new YamlScalarNode("Music")))
                {
                    var music = (YamlMappingNode)game.Children[new YamlScalarNode("Music")];
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

                Games.Add(code, new AGame(code, name, tables, creator, playlists));
            }
        }

        internal static byte GetRemap(byte voice, string key = null)
        {
            if (key == null)
                key = "HGSS";
            if (InstrumentRemaps.TryGetValue(key, out ARemap remap))
            {
                var r = remap.Remaps.FirstOrDefault(t => t.Item1 == voice);
                if (r == null)
                    return voice;
                return r.Item2;
            }
            return voice;
        }
    }
}
