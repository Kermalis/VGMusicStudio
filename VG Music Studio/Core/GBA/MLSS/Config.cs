using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class Config : Core.Config
    {
        public readonly byte[] ROM;
        public readonly EndianBinaryReader Reader;
        public string GameCode;
        public string Name;
        public int[] SongTableOffsets;
        public long[] SongTableSizes;
        public int VoiceTableOffset;
        public int SampleTableOffset;
        public long SampleTableSize;
        public string Remap;

        public Config(byte[] rom)
        {
            const string configFile = "MLSS.yaml";
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
                        SongTableOffsets[i] = (int)Util.Utils.ParseValue(nameof(SongTableOffsets), songTables[i], 0, rom.Length - 1);
                        SongTableSizes[i] = Util.Utils.ParseValue(nameof(SongTableSizes), sizes[i], 1, rom.Length - 1);
                    }
                    VoiceTableOffset = (int)game.GetValidValue(nameof(VoiceTableOffset), 0, rom.Length - 1);
                    SampleTableOffset = (int)game.GetValidValue(nameof(SampleTableOffset), 0, rom.Length - 1);
                    SampleTableSize = game.GetValidValue(nameof(SampleTableSize), 0, rom.Length - 1);
                    if (game.Children.TryGetValue(nameof(Remap), out YamlNode remap))
                    {
                        Remap = remap.ToString();
                    }

                    if (game.Children.TryGetValue(nameof(Playlists), out YamlNode _playlists))
                    {
                        var playlists = (YamlMappingNode)_playlists;
                        foreach (KeyValuePair<YamlNode, YamlNode> kvp in playlists)
                        {
                            string name = kvp.Key.ToString();
                            var songs = new List<Song>();
                            foreach (KeyValuePair<YamlNode, YamlNode> song in (YamlMappingNode)kvp.Value)
                            {
                                long songIndex = Util.Utils.ParseValue($"{nameof(Playlists)} key", song.Key.ToString(), 0, long.MaxValue);
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
                        Playlists.Insert(0, new Playlist("Music", Playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => s.Index)));
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
