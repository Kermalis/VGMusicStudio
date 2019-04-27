using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal class M4AConfig : Config
    {
        public class Song
        {
            public int Index;
            public string Name;

            public Song(int index, string name)
            {
                Index = index; Name = name;
            }

            public override bool Equals(object obj)
            {
                return !(obj is Song other) ? false : other.Index == Index && other.Name == Name;
            }
            public override int GetHashCode()
            {
                return unchecked(Index.GetHashCode() ^ Name.GetHashCode());
            }
            public override string ToString()
            {
                return Name;
            }
        }
        public class Playlist
        {
            public string Name;
            public Song[] Songs;

            public Playlist(string name, Song[] songs)
            {
                Name = name; Songs = songs;
            }

            public override string ToString()
            {
                int songCount = Songs.Length;
                CultureInfo cul = System.Threading.Thread.CurrentThread.CurrentUICulture;

                if (cul.Equals(CultureInfo.CreateSpecificCulture("it")) // Italian
                    || cul.Equals(CultureInfo.CreateSpecificCulture("it-it"))) // Italian (Italy)
                {
                    // PlaylistName - (1 Canzoni)
                    // PlaylistName - (2 Canzoni)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Canzone" : "Canzoni")})";
                }
                else // Fallback to en-US
                {
                    // PlaylistName - (1 Song)
                    // PlaylistName - (2 Songs)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Song" : "Songs")})";
                }
            }
        }

        public readonly byte[] ROM;
        public readonly EndianBinaryReader Reader;
        public string GameCode;
        public string Name;
        public string Creator;
        public int[] SongTableOffsets;
        public int[] SongTableSizes;
        public List<Playlist> Playlists;
        public string Remap;
        public ReverbType ReverbType;
        public byte Reverb;
        public byte Volume;
        public int SampleRate;
        public bool HasGoldenSunSynths;
        public bool HasPokemonCompression;

        public M4AConfig(byte[] rom)
        {
            const string configFile = "M4A.yaml";
            ROM = rom;
            Reader = new EndianBinaryReader(new MemoryStream(rom));
            GameCode = Reader.ReadString(4, 0xAC);
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText(configFile)));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            foreach (KeyValuePair<YamlNode, YamlNode> g in mapping)
            {
                if (g.Key.ToString() == GameCode)
                {
                    var game = (YamlMappingNode)g.Value;

                    Name = game.Children[nameof(Name)].ToString();

                    string[] songTables = game.Children[nameof(SongTableOffsets)].ToString().Split(' ');
                    SongTableOffsets = new int[songTables.Length];
                    SongTableSizes = new int[songTables.Length];
                    for (int i = 0; i < songTables.Length; i++)
                    {
                        SongTableOffsets[i] = (int)Utils.ParseValue(songTables[i]);
                    }

                    if (game.Children.TryGetValue("Copy", out YamlNode copy))
                    {
                        game = (YamlMappingNode)mapping.Children[copy];
                    }

                    string[] sizes = game.Children[nameof(SongTableSizes)].ToString().Split(' ');
                    for (int i = 0; i < songTables.Length; i++)
                    {
                        if (i < sizes.Length)
                        {
                            SongTableSizes[i] = (int)Utils.ParseValue(sizes[i]);
                        }
                    }

                    Creator = game.Children[nameof(Creator)].ToString();

                    SampleRate = (int)Utils.ParseValue(game.Children[nameof(SampleRate)].ToString());
                    ReverbType = (ReverbType)Enum.Parse(typeof(ReverbType), game.Children[nameof(ReverbType)].ToString());
                    Reverb = (byte)Utils.ParseValue(game.Children[nameof(Reverb)].ToString());
                    Volume = (byte)Utils.ParseValue(game.Children[nameof(Volume)].ToString());
                    HasGoldenSunSynths = bool.Parse(game.Children[nameof(HasGoldenSunSynths)].ToString());
                    HasPokemonCompression = bool.Parse(game.Children[nameof(HasPokemonCompression)].ToString());
                    if (game.Children.TryGetValue(nameof(Remap), out YamlNode rmap))
                    {
                        Remap = rmap.ToString();
                    }

                    Playlists = new List<Playlist>();
                    if (game.Children.TryGetValue(nameof(Playlists), out YamlNode ymusic))
                    {
                        var music = (YamlMappingNode)ymusic;
                        foreach (KeyValuePair<YamlNode, YamlNode> kvp in music)
                        {
                            var songs = new List<Song>();
                            foreach (KeyValuePair<YamlNode, YamlNode> song in (YamlMappingNode)kvp.Value) // No hex values. It prevents putting in duplicates by having one hex and one dec of the same song index
                            {
                                songs.Add(new Song(int.Parse(song.Key.ToString()), song.Value.ToString()));
                            }
                            Playlists.Add(new Playlist(kvp.Key.ToString(), songs.ToArray()));
                        }
                    }

                    // The complete playlist
                    if (!Playlists.Any(p => p.Name == "Music"))
                    {
                        Playlists.Insert(0, new Playlist("Music", Playlists.Select(p => p.Songs).UniteAll().OrderBy(s => s.Index).ToArray()));
                    }
                    return;
                }
            }
            throw new Exception($"Game code \"{GameCode}\" was not found in {configFile}");
        }
    }
}
