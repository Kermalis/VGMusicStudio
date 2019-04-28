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
            public long Index;
            public string Name;

            public Song(long index, string name)
            {
                Index = index; Name = name;
            }

            public override bool Equals(object obj)
            {
                return !(obj is Song other) ? false : other.Index == Index;
            }
            public override int GetHashCode()
            {
                return Index.GetHashCode();
            }
            public override string ToString()
            {
                return Name;
            }
        }
        public class Playlist
        {
            public string Name;
            public List<Song> Songs;

            public Playlist(string name, List<Song> songs)
            {
                Name = name; Songs = songs;
            }

            public override string ToString()
            {
                int songCount = Songs.Count;
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
        public int[] SongTableOffsets;
        public long[] SongTableSizes;
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
            using (StreamReader fileStream = File.OpenText(configFile))
            {
                try
                {
                    ROM = rom;
                    Reader = new EndianBinaryReader(new MemoryStream(rom));
                    GameCode = Reader.ReadString(4, 0xAC);
                    var yaml = new YamlStream();
                    yaml.Load(fileStream);

                    var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                    YamlMappingNode game;
                    try
                    {
                        game = (YamlMappingNode)mapping.Children.GetValue(GameCode);
                    }
                    catch (BetterKeyNotFoundException)
                    {
                        throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}Game code \"{GameCode}\" is missing.");
                    }

                    Name = game.Children.GetValue(nameof(Name)).ToString();

                    string[] songTables = game.Children.GetValue(nameof(SongTableOffsets)).ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (songTables.Length == 0)
                    {
                        throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}\"{nameof(SongTableOffsets)}\" must have at least one entry.");
                    }

                    if (game.Children.TryGetValue("Copy", out YamlNode copy))
                    {
                        try
                        {
                            game = (YamlMappingNode)mapping.Children.GetValue(copy);
                        }
                        catch (BetterKeyNotFoundException ex)
                        {
                            throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}Cannot copy invalid game code \"{ex.Key}\"");
                        }
                    }

                    string[] sizes = game.Children.GetValue(nameof(SongTableSizes)).ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sizes.Length != songTables.Length)
                    {
                        throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}\"{nameof(SongTableSizes)}\" count must be the same as \"{nameof(SongTableOffsets)}\" count.");
                    }
                    SongTableOffsets = new int[songTables.Length];
                    SongTableSizes = new long[songTables.Length];
                    for (int i = 0; i < songTables.Length; i++)
                    {
                        SongTableSizes[i] = Utils.ParseValue(nameof(SongTableSizes), sizes[i], 1, rom.Length / 8); // A song entry is 8 bytes
                        SongTableOffsets[i] = (int)Utils.ParseValue(nameof(SongTableOffsets), songTables[i], 0, rom.Length - (SongTableSizes[i] * 8));
                    }

                    SampleRate = (int)game.GetValidValue(nameof(SampleRate), 0, M4AUtils.FrequencyTable.Length - 1);
                    try
                    {
                        ReverbType = (ReverbType)Enum.Parse(typeof(ReverbType), game.Children.GetValue(nameof(ReverbType)).ToString());
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
                    {
                        throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}\"{nameof(ReverbType)}\" was invalid.");
                    }
                    Reverb = (byte)game.GetValidValue(nameof(Reverb), byte.MinValue, byte.MaxValue);
                    Volume = (byte)game.GetValidValue(nameof(Volume), 0, 15);
                    HasGoldenSunSynths = game.GetValidBoolean(nameof(HasGoldenSunSynths));
                    HasPokemonCompression = game.GetValidBoolean(nameof(HasPokemonCompression));
                    if (game.Children.TryGetValue(nameof(Remap), out YamlNode remap))
                    {
                        Remap = remap.ToString();
                    }

                    Playlists = new List<Playlist>();
                    if (game.Children.TryGetValue(nameof(Playlists), out YamlNode _playlists))
                    {
                        var playlists = (YamlMappingNode)_playlists;
                        foreach (KeyValuePair<YamlNode, YamlNode> kvp in playlists)
                        {
                            string name = kvp.Key.ToString();
                            var songs = new List<Song>();
                            foreach (KeyValuePair<YamlNode, YamlNode> song in (YamlMappingNode)kvp.Value)
                            {
                                long songIndex = Utils.ParseValue($"{nameof(Playlists)} key", song.Key.ToString(), 0, long.MaxValue);
                                if (songs.Any(s => s.Index == songIndex))
                                {
                                    throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}Playlist \"{name}\" has song {songIndex} defined more than once between decimal and hexadecimal.");
                                }
                                songs.Add(new Song(songIndex, song.Value.ToString()));
                            }
                            Playlists.Add(new Playlist(name, songs));
                        }
                    }

                    // The complete playlist
                    if (!Playlists.Any(p => p.Name == "Music"))
                    {
                        Playlists.Insert(0, new Playlist("Music", Playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => s.Index).ToList()));
                    }
                }
                catch (BetterKeyNotFoundException ex)
                {
                    throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}\"{ex.Key}\" is missing.");
                }
                catch (InvalidValueException ex)
                {
                    throw new Exception($"Error parsing game code \"{GameCode}\" in \"{configFile}\"{Environment.NewLine}{ex.Message}");
                }
                catch (YamlDotNet.Core.SyntaxErrorException ex)
                {
                    throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}{ex.Message}");
                }
            }
        }

        public override void Dispose()
        {
            Reader.Dispose();
        }
    }
}
