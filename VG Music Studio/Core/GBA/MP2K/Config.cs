using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class Config : Core.Config
    {
        public readonly byte[] ROM;
        public readonly EndianBinaryReader Reader;
        public string GameCode;
        public byte Version;
        public string Name;
        public int[] SongTableOffsets;
        public long[] SongTableSizes;
        public string Remap;
        public ReverbType ReverbType;
        public byte Reverb;
        public byte Volume;
        public int SampleRate;
        public bool HasGoldenSunSynths;
        public bool HasPokemonCompression;

        public Config(byte[] rom)
        {
            const string configFile = "MP2K.yaml";
            using (StreamReader fileStream = File.OpenText(Util.Utils.CombineWithBaseDirectory(configFile)))
            {
                string gcv = string.Empty;
                try
                {
                    ROM = rom;
                    Reader = new EndianBinaryReader(new MemoryStream(rom));
                    GameCode = Reader.ReadString(4, 0xAC);
                    Version = Reader.ReadByte(0xBC);
                    gcv = $"{GameCode}_{Version:X2}";
                    var yaml = new YamlStream();
                    yaml.Load(fileStream);

                    var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                    YamlMappingNode game;
                    try
                    {
                        game = (YamlMappingNode)mapping.Children.GetValue(gcv);
                    }
                    catch (BetterKeyNotFoundException)
                    {
                        throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorMLSSMP2KMissingGameCode, gcv)));
                    }

                    Name = game.Children.GetValue(nameof(Name)).ToString();

                    string[] songTables = game.Children.GetValue(nameof(SongTableOffsets)).ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (songTables.Length == 0)
                    {
                        throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyNoEntries, nameof(SongTableOffsets))));
                    }

                    if (game.Children.TryGetValue("Copy", out YamlNode copy))
                    {
                        try
                        {
                            game = (YamlMappingNode)mapping.Children.GetValue(copy);
                        }
                        catch (BetterKeyNotFoundException ex)
                        {
                            throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorMLSSMP2KCopyInvalidGameCode, ex.Key)));
                        }
                    }

                    string[] sizes = game.Children.GetValue(nameof(SongTableSizes)).ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sizes.Length != songTables.Length)
                    {
                        throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorMLSSMP2KSongTableCounts, nameof(SongTableSizes), nameof(SongTableOffsets))));
                    }
                    SongTableOffsets = new int[songTables.Length];
                    SongTableSizes = new long[songTables.Length];
                    for (int i = 0; i < songTables.Length; i++)
                    {
                        SongTableSizes[i] = Util.Utils.ParseValue(nameof(SongTableSizes), sizes[i], 1, rom.Length - 1);
                        SongTableOffsets[i] = (int)Util.Utils.ParseValue(nameof(SongTableOffsets), songTables[i], 0, rom.Length - 1);
                    }

                    SampleRate = (int)game.GetValidValue(nameof(SampleRate), 0, Utils.FrequencyTable.Length - 1);
                    try
                    {
                        ReverbType = (ReverbType)Enum.Parse(typeof(ReverbType), game.Children.GetValue(nameof(ReverbType)).ToString());
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
                    {
                        throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyInvalid, nameof(ReverbType))));
                    }
                    Reverb = (byte)game.GetValidValue(nameof(Reverb), byte.MinValue, byte.MaxValue);
                    Volume = (byte)game.GetValidValue(nameof(Volume), 0, 15);
                    HasGoldenSunSynths = game.GetValidBoolean(nameof(HasGoldenSunSynths));
                    HasPokemonCompression = game.GetValidBoolean(nameof(HasPokemonCompression));
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
                                long songIndex = Util.Utils.ParseValue(string.Format(Strings.ConfigKeySubkey, nameof(Playlists)), song.Key.ToString(), 0, long.MaxValue);
                                if (songs.Any(s => s.Index == songIndex))
                                {
                                    throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorMLSSMP2KSongRepeated, name, songIndex)));
                                }
                                songs.Add(new Song(songIndex, song.Value.ToString()));
                            }
                            Playlists.Add(new Playlist(name, songs));
                        }
                    }

                    // The complete playlist
                    if (!Playlists.Any(p => p.Name == "Music"))
                    {
                        Playlists.Insert(0, new Playlist(Strings.PlaylistMusic, Playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => s.Index)));
                    }
                }
                catch (BetterKeyNotFoundException ex)
                {
                    throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyMissing, ex.Key)));
                }
                catch (InvalidValueException ex)
                {
                    throw new Exception(string.Format(Strings.ErrorMLSSMP2KParseGameCode, gcv, configFile, Environment.NewLine + ex.Message));
                }
                catch (YamlDotNet.Core.YamlException ex)
                {
                    throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + ex.Message));
                }
            }
        }

        public override string GetSongName(long index)
        {
            return index.ToString();
        }

        public override void Dispose()
        {
            Reader.Dispose();
        }
    }
}
