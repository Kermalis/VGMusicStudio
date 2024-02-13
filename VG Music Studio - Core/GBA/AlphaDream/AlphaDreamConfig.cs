using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

public sealed class AlphaDreamConfig : Config
{
	private const string CONFIG_FILE = "AlphaDream.yaml";

	internal readonly byte[] ROM;
	internal readonly EndianBinaryReader Reader; // TODO: Need?
	internal readonly string GameCode;
	internal readonly byte Version;

	internal readonly string Name;
	internal readonly AudioEngineVersion AudioEngineVersion;
	internal readonly int[] SongTableOffsets;
	public readonly long[] SongTableSizes;
	internal readonly int VoiceTableOffset;
	internal readonly int SampleTableOffset;
	internal readonly long SampleTableSize;

	internal AlphaDreamConfig(byte[] rom)
	{
		using (StreamReader fileStream = File.OpenText(ConfigUtils.CombineWithBaseDirectory(CONFIG_FILE)))
		{
			string gcv = string.Empty;
			try
			{
				ROM = rom;
				Reader = new EndianBinaryReader(new MemoryStream(rom), ascii: true);
				Reader.Stream.Position = 0xAC;
				GameCode = Reader.ReadString_Count(4);
				Reader.Stream.Position = 0xBC;
				Version = Reader.ReadByte();
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
					throw new Exception(string.Format(Strings.ErrorParseConfig, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KMissingGameCode, gcv)));
				}

				YamlNode? nameNode = null,
					audioEngineVersionNode = null,
					songTableOffsetsNode = null,
					voiceTableOffsetNode = null,
					sampleTableOffsetNode = null,
					songTableSizesNode = null,
					sampleTableSizeNode = null;
				void Load(YamlMappingNode gameToLoad)
				{
					if (gameToLoad.Children.TryGetValue("Copy", out YamlNode? node))
					{
						YamlMappingNode copyGame;
						try
						{
							copyGame = (YamlMappingNode)mapping.Children.GetValue(node);
						}
						catch (BetterKeyNotFoundException ex)
						{
							throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KCopyInvalidGameCode, ex.Key)));
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
								int songIndex = (int)ConfigUtils.ParseValue(string.Format(Strings.ConfigKeySubkey, nameof(Playlists)), song.Key.ToString(), 0, int.MaxValue);
								if (songs.Any(s => s.Index == songIndex))
								{
									throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KSongRepeated, name, songIndex)));
								}
								songs.Add(new Song(songIndex, song.Value.ToString()));
							}
							Playlists.Add(new Playlist(name, songs));
						}
					}
				}

				Load(game);

				if (nameNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(Name), null);
				}
				Name = nameNode.ToString();

				if (audioEngineVersionNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(AudioEngineVersion), null);
				}
				AudioEngineVersion = ConfigUtils.ParseEnum<AudioEngineVersion>(nameof(AudioEngineVersion), audioEngineVersionNode.ToString());

				if (songTableOffsetsNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(SongTableOffsets), null);
				}
				string[] songTables = songTableOffsetsNode.ToString().Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
				int numSongTables = songTables.Length;
				if (numSongTables == 0)
				{
					throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorConfigKeyNoEntries, nameof(SongTableOffsets))));
				}

				if (songTableSizesNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(SongTableSizes), null);
				}
				string[] songTableSizes = songTableSizesNode.ToString().Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
				if (songTableSizes.Length != numSongTables)
				{
					throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KSongTableCounts, nameof(SongTableSizes), nameof(SongTableOffsets))));
				}
				SongTableOffsets = new int[numSongTables];
				SongTableSizes = new long[numSongTables];
				int maxOffset = rom.Length - 1;
				for (int i = 0; i < numSongTables; i++)
				{
					SongTableOffsets[i] = (int)ConfigUtils.ParseValue(nameof(SongTableOffsets), songTables[i], 0, maxOffset);
					SongTableSizes[i] = ConfigUtils.ParseValue(nameof(SongTableSizes), songTableSizes[i], 1, maxOffset);
				}

				if (voiceTableOffsetNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(VoiceTableOffset), null);
				}
				VoiceTableOffset = (int)ConfigUtils.ParseValue(nameof(VoiceTableOffset), voiceTableOffsetNode.ToString(), 0, maxOffset);

				if (sampleTableOffsetNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(SampleTableOffset), null);
				}
				SampleTableOffset = (int)ConfigUtils.ParseValue(nameof(SampleTableOffset), sampleTableOffsetNode.ToString(), 0, maxOffset);

				if (sampleTableSizeNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(SampleTableSize), null);
				}
				SampleTableSize = ConfigUtils.ParseValue(nameof(SampleTableSize), sampleTableSizeNode.ToString(), 0, maxOffset);

				// The complete playlist
				if (!Playlists.Any(p => p.Name == "Music"))
				{
					Playlists.Insert(0, new Playlist(Strings.PlaylistMusic, Playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => s.Index).ToList()));
				}
			}
			catch (BetterKeyNotFoundException ex)
			{
				throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorConfigKeyMissing, ex.Key)));
			}
			catch (InvalidValueException ex)
			{
				throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + ex.Message));
			}
			catch (YamlException ex)
			{
				throw new Exception(string.Format(Strings.ErrorParseConfig, CONFIG_FILE, Environment.NewLine + ex.Message));
			}
		}
	}

	public override string GetGameName()
	{
		return Name;
	}
	public override string GetSongName(int index)
	{
		if (TryGetFirstSong(index, out Song s))
		{
			return s.Name;
		}
		return index.ToString();
	}

	public override void Dispose()
	{
		Reader.Stream.Dispose();
	}
}
