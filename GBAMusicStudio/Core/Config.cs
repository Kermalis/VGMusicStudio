using GBAMusicStudio.Util;
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
            Name = name.Replace('_', ' ');
            Songs = songs;
        }

        public override string ToString() => string.Format("{0} - ({1} Songs)", Name, Songs.Length);
    }

    public class Config
    {
        public readonly uint SongTable;
        public readonly string GameName, CreatorName;

        public readonly List<Playlist> Playlists;

        YamlStream yaml;
        public Config()
        {
            yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Games.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
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
            var music = (YamlMappingNode)game.Children[new YamlScalarNode("Music")];
            foreach (var kvp in music)
            {
                var songs = new List<Song>();
                foreach (var song in (YamlMappingNode)kvp.Value)
                    songs.Add(new Song((ushort)Utils.ParseInt(song.Key.ToString()), song.Value.ToString()));
                Playlists.Add(new Playlist(kvp.Key.ToString(), songs.ToArray()));
            }

            // Full playlist
            if (!Playlists.Any(p => p.Name == "Music"))
                Playlists.Insert(0, new Playlist("Music", Playlists.Select(p => p.Songs).UniteAll().OrderBy(s => s.Index).ToArray()));
        }
    }
}
