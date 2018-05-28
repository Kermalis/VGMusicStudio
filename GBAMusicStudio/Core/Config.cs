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

        public override string ToString() => string.Format("{0} - ({1})", Name, "Song".ToQuantity(Songs.Length));
    }
    public class Game
    {
        public readonly string Code, Name, Creator;
        public readonly uint SongTable;
        public readonly List<Playlist> Playlists;

        public Game(string code, string name, uint table, string creator, List<Playlist> playlists)
        {
            Code = code;
            Name = name;
            SongTable = table;
            Creator = creator;
            Playlists = playlists;
        }
    }

    public static class Config
    {
        public static byte DirectCount { get; private set; }
        public static byte Volume { get; private set; }

        public static Dictionary<string, Game> Games { get; private set; }

        static string fileName = "Games.yaml";

        static Config() => Load();
        public static void Load()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText(fileName)));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            DirectCount = (byte)Utils.ParseUInt(mapping.Children[new YamlScalarNode("DirectCount")].ToString());
            Volume = (byte)Utils.ParseUInt(mapping.Children[new YamlScalarNode("Volume")].ToString());

            Games = new Dictionary<string, Game>();

            var gmap = (YamlMappingNode)mapping.Children[new YamlScalarNode("Games")];
            foreach (var g in gmap)
            {
                string code, name, creator;
                uint table;
                List<Playlist> playlists;

                code = g.Key.ToString();
                var game = (YamlMappingNode)g.Value;

                // Basic info
                name = game.Children[new YamlScalarNode("Name")].ToString();
                table = Utils.ParseUInt(game.Children[new YamlScalarNode("SongTable")].ToString());

                // If we are to copy another game's config
                if (game.Children.ContainsKey(new YamlScalarNode("Copy")))
                    game = (YamlMappingNode)gmap.Children[new YamlScalarNode(game.Children[new YamlScalarNode("Copy")].ToString())];

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

                Games.Add(code, new Game(code, name, table, creator, playlists));
            }
        }
    }
}
