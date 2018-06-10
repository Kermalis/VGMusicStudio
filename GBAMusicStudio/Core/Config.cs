using GBAMusicStudio.Util;
using Humanizer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace GBAMusicStudio.Core
{
    public class Song
    {
        public readonly ushort Index;
        public readonly string Name;

        public Song(ushort index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString() => Name;
    }
    public class Playlist
    {
        public readonly string Name;
        public readonly Song[] Songs;

        public Playlist(string name, Song[] songs)
        {
            Name = name.Humanize();
            Songs = songs;
        }

        public override string ToString() => string.Format("{0} - ({1})", Name, "Song".ToQuantity(Songs.Where(s => s.Name != "Playlist is empty.").Count()));
    }
    public class Game
    {
        public readonly string Code, Name, Creator;
        public readonly uint[] SongTables;
        public readonly List<Playlist> Playlists;

        public Game(string code, string name, uint[] tables, string creator, List<Playlist> playlists)
        {
            Code = code;
            Name = name;
            SongTables = tables;
            Creator = creator;
            Playlists = playlists;
        }
    }

    public static class Config
    {
        public static byte DirectCount { get; private set; }
        public static byte PSGVolume { get; private set; }
        public static bool MIDIKeyboardFixedVelocity { get; private set; }
        public static byte RefreshRate { get; private set; }
        public static bool CenterIndicators { get; private set; }
        public static bool PanpotIndicators { get; private set; }
        public static byte Volume { get; private set; }
        public static HSLColor[] Colors { get; private set; }

        public static Dictionary<string, Game> Games { get; private set; }

        static Config() => Load();
        public static void Load() { LoadConfig(); LoadGames(); }
        static void LoadConfig()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Config.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            DirectCount = (byte)Utils.ParseUInt(mapping.Children[new YamlScalarNode("DirectCount")].ToString());
            PSGVolume = (byte)Utils.ParseUInt(mapping.Children[new YamlScalarNode("PSGVolume")].ToString());
            MIDIKeyboardFixedVelocity = bool.Parse(mapping.Children[new YamlScalarNode("MIDIKeyboardFixedVelocity")].ToString());
            RefreshRate = (byte)Utils.ParseUInt(mapping.Children[new YamlScalarNode("RefreshRate")].ToString());
            CenterIndicators = bool.Parse(mapping.Children[new YamlScalarNode("CenterIndicators")].ToString());
            PanpotIndicators = bool.Parse(mapping.Children[new YamlScalarNode("PanpotIndicators")].ToString());
            Volume = (byte)Utils.ParseUInt(mapping.Children[new YamlScalarNode("Volume")].ToString());

            var cmap = (YamlMappingNode)mapping.Children[new YamlScalarNode("Colors")];
            Colors = new HSLColor[256];
            foreach (var c in cmap)
            {
                uint i = Utils.ParseUInt(c.Key.ToString());
                var children = ((YamlMappingNode)c.Value).Children;
                double h = 0, s = 0, l = 0;
                foreach (var v in children)
                {
                    if (v.Key.ToString() == "H")
                        h = Utils.ParseUInt(v.Value.ToString());
                    else if (v.Key.ToString() == "S")
                        s = Utils.ParseUInt(v.Value.ToString());
                    else if (v.Key.ToString() == "L")
                        l = Utils.ParseUInt(v.Value.ToString());
                }
                HSLColor color = new HSLColor(h, s, l);
                Colors[i] = Colors[i + 0x80] = color;
            }
        }
        static void LoadGames()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Games.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            Games = new Dictionary<string, Game>();
            foreach (var g in mapping)
            {
                string code, name, creator;
                uint[] tables;
                List<Playlist> playlists;

                code = g.Key.ToString();
                var game = (YamlMappingNode)g.Value;

                // Basic info
                name = game.Children[new YamlScalarNode("Name")].ToString();
                var songTables = game.Children[new YamlScalarNode("SongTable")].ToString().Split(' ');
                tables = new uint[songTables.Length];
                for (int i = 0; i < songTables.Length; i++)
                    tables[i] = Utils.ParseUInt(songTables[i]);

                // If we are to copy another game's config
                if (game.Children.ContainsKey(new YamlScalarNode("Copy")))
                    game = (YamlMappingNode)mapping.Children[new YamlScalarNode(game.Children[new YamlScalarNode("Copy")].ToString())];

                // Creator name
                creator = game.Children[new YamlScalarNode("Creator")].ToString();

                // Load playlists
                playlists = new List<Playlist>();
                if (game.Children.ContainsKey(new YamlScalarNode("Music")))
                {
                    var music = (YamlMappingNode)game.Children[new YamlScalarNode("Music")];
                    foreach (var kvp in music)
                    {
                        var songs = new List<Song>();
                        foreach (var song in (YamlMappingNode)kvp.Value)
                            songs.Add(new Song(ushort.Parse(song.Key.ToString()), song.Value.ToString())); // No hex values. It prevents putting in duplicates by having one hex and one dec of the same song index
                        playlists.Add(new Playlist(kvp.Key.ToString(), songs.ToArray()));
                    }
                }

                // Full playlist
                if (!playlists.Any(p => p.Name == "Music"))
                    playlists.Insert(0, new Playlist("Music", playlists.Select(p => p.Songs).UniteAll().OrderBy(s => s.Index).ToArray()));

                // If playlist is empty, add an empty entry
                for (int i = 0; i < playlists.Count; i++)
                    if (playlists[i].Songs.Length == 0)
                        playlists[i] = new Playlist(playlists[i].Name, new Song[] { new Song(0, "Playlist is empty.") });

                Games.Add(code, new Game(code, name, tables, creator, playlists));
            }
        }
    }
}
