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

    public class Config
    {
        public readonly uint SongTable;
        public readonly string GameName, CreatorName;
        public readonly byte DirectCount;

        public readonly List<Playlist> Playlists;

        YamlStream yaml;
        public Config()
        {
            yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Games.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            DirectCount = (byte)Utils.ParseInt(mapping.Children[new YamlScalarNode("DirectCount")].ToString());
            var game = (YamlMappingNode)mapping.Children[new YamlScalarNode(ROM.Instance.GameCode)];

            // Basic info
            SongTable = (uint)Utils.ParseInt(game.Children[new YamlScalarNode("SongTable")].ToString());
            GameName = game.Children[new YamlScalarNode("Name")].ToString();

            // If we are to copy another game's config
            if (game.Children.ContainsKey(new YamlScalarNode("Copy")))
                game = (YamlMappingNode)mapping.Children[new YamlScalarNode(game.Children[new YamlScalarNode("Copy")].ToString())];

            // Creator name
            CreatorName = game.Children[new YamlScalarNode("Creator")].ToString();

            // Load playlists
            Playlists = new List<Playlist>();
            if (game.Children.ContainsKey(new YamlScalarNode("Music")))
            {
                var music = (YamlMappingNode)game.Children[new YamlScalarNode("Music")];
                foreach (var kvp in music)
                {
                    var songs = new List<Song>();
                    foreach (var song in (YamlMappingNode)kvp.Value)
                        songs.Add(new Song(ushort.Parse(song.Key.ToString()), song.Value.ToString())); // No hex values. It prevents putting in duplicates by having one hex and one dec of the same song index
                    Playlists.Add(new Playlist(kvp.Key.ToString(), songs.ToArray()));
                }
            }

            // Full playlist
            if (!Playlists.Any(p => p.Name == "Music"))
                Playlists.Insert(0, new Playlist("Music", Playlists.Select(p => p.Songs).UniteAll().OrderBy(s => s.Index).ToArray()));

            // If playlist is empty, add an empty entry
            for (int i = 0; i < Playlists.Count; i++)
            {
                if (Playlists[i].Songs.Length == 0)
                    Playlists[i] = new Playlist(Playlists[i].Name, new Song[] { new Song(0, "Playlist is empty.") });
            }
        }
    }
}
