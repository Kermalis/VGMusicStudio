using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream
{
    internal class Config : Core.Config
    {
        public readonly byte[] ROM;
        public readonly EndianBinaryReader Reader;
        public string GameCode;
        public byte Version;

        public string Name;
        public AudioEngineVersion AudioEngineVersion;
        public int[] SongTableOffsets;
        public long[] SongTableSizes;
        public int VoiceTableOffset;
        public int SampleTableOffset;
        public long SampleTableSize;

        public Config(byte[] rom)
        {
            const string configFile = "AlphaDream.yaml";
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
                        throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KMissingGameCode, gcv)));
                    }

                    YamlNode nameNode = null,
                        audioEngineVersionNode = null,
                        songTableOffsetsNode = null,
                        voiceTableOffsetNode = null,
                        sampleTableOffsetNode = null,
                        songTableSizesNode = null,
                        sampleTableSizeNode = null;
                    void Load(YamlMappingNode gameToLoad)
                    {
                        if (gameToLoad.Children.TryGetValue("Copy", out YamlNode node))
                        {
                            YamlMappingNode copyGame;
                            try
                            {
                                copyGame = (YamlMappingNode)mapping.Children.GetValue(node);
                            }
                            catch (BetterKeyNotFoundException ex)
                            {
                                throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KCopyInvalidGameCode, ex.Key)));
                            }
                            Load(copyGame);
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(Name), out node))
                        {
                            nameNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(AudioEngineVersion), out node))
                        {
                            audioEngineVersionNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(SongTableOffsets), out node))
                        {
                            songTableOffsetsNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(SongTableSizes), out node))
                        {
                            songTableSizesNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(VoiceTableOffset), out node))
                        {
                            voiceTableOffsetNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(SampleTableOffset), out node))
                        {
                            sampleTableOffsetNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(SampleTableSize), out node))
                        {
                            sampleTableSizeNode = node;
                        }
                        if (gameToLoad.Children.TryGetValue(nameof(Playlists), out node))
                        {
                            var playlists = (YamlMappingNode)node;
                            foreach (KeyValuePair<YamlNode, YamlNode> kvp in playlists)
                            {
                                string name = kvp.Key.ToString();
                                var songs = new List<Song>();
                                foreach (KeyValuePair<YamlNode, YamlNode> song in (YamlMappingNode)kvp.Value)
                                {
                                    long songIndex = Util.Utils.ParseValue(string.Format(Strings.ConfigKeySubkey, nameof(Playlists)), song.Key.ToString(), 0, long.MaxValue);
                                    if (songs.Any(s => s.Index == songIndex))
                                    {
                                        throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KSongRepeated, name, songIndex)));
                                    }
                                    songs.Add(new Song(songIndex, song.Value.ToString()));
                                }
                                Playlists.Add(new Playlist(name, songs));
                            }
                        }
                    }

                    Load(game);

                    if (nameNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(Name), null);
                    }
                    Name = nameNode.ToString();
                    if (audioEngineVersionNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(AudioEngineVersion), null);
                    }
                    AudioEngineVersion = Util.Utils.ParseEnum<AudioEngineVersion>(nameof(AudioEngineVersion), audioEngineVersionNode.ToString());
                    if (songTableOffsetsNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(SongTableOffsets), null);
                    }
                    string[] songTables = songTableOffsetsNode.ToString().SplitSpace(StringSplitOptions.RemoveEmptyEntries);
                    int numSongTables = songTables.Length;
                    if (numSongTables == 0)
                    {
                        throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyNoEntries, nameof(SongTableOffsets))));
                    }
                    if (songTableSizesNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(SongTableSizes), null);
                    }
                    string[] songTableSizes = songTableSizesNode.ToString().SplitSpace(StringSplitOptions.RemoveEmptyEntries);
                    if (songTableSizes.Length != numSongTables)
                    {
                        throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KSongTableCounts, nameof(SongTableSizes), nameof(SongTableOffsets))));
                    }
                    SongTableOffsets = new int[numSongTables];
                    SongTableSizes = new long[numSongTables];
                    int maxOffset = rom.Length - 1;
                    for (int i = 0; i < numSongTables; i++)
                    {
                        SongTableOffsets[i] = (int)Util.Utils.ParseValue(nameof(SongTableOffsets), songTables[i], 0, maxOffset);
                        SongTableSizes[i] = Util.Utils.ParseValue(nameof(SongTableSizes), songTableSizes[i], 1, maxOffset);
                    }
                    if (voiceTableOffsetNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(VoiceTableOffset), null);
                    }
                    VoiceTableOffset = (int)Util.Utils.ParseValue(nameof(VoiceTableOffset), voiceTableOffsetNode.ToString(), 0, maxOffset);
                    if (sampleTableOffsetNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(SampleTableOffset), null);
                    }
                    SampleTableOffset = (int)Util.Utils.ParseValue(nameof(SampleTableOffset), sampleTableOffsetNode.ToString(), 0, maxOffset);
                    if (sampleTableSizeNode == null)
                    {
                        throw new BetterKeyNotFoundException(nameof(SampleTableSize), null);
                    }
                    SampleTableSize = Util.Utils.ParseValue(nameof(SampleTableSize), sampleTableSizeNode.ToString(), 0, maxOffset);

                    // The complete playlist
                    if (!Playlists.Any(p => p.Name == "Music"))
                    {
                        Playlists.Insert(0, new Playlist(Strings.PlaylistMusic, Playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => s.Index)));
                    }
                }
                catch (BetterKeyNotFoundException ex)
                {
                    throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyMissing, ex.Key)));
                }
                catch (InvalidValueException ex)
                {
                    throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, configFile, Environment.NewLine + ex.Message));
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
