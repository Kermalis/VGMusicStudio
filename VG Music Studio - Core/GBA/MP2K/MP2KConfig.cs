using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed class MP2KConfig : Config
{
	private const string CONFIG_FILE = "MP2K.yaml";

	internal readonly byte[] ROM;
	internal readonly EndianBinaryReader Reader; // TODO: Need?
	internal readonly string GameCode;
	internal readonly byte Version;

	internal readonly string Name;
	internal readonly int[] SongTableOffsets;
	public readonly long[] SongTableSizes;
	internal readonly int SampleRate;
	internal readonly ReverbType ReverbType;
	internal readonly byte Reverb;
	internal readonly byte Volume;
	internal readonly bool HasGoldenSunSynths;
	internal readonly bool HasPokemonCompression;

	internal MP2KConfig(byte[] rom)
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
					songTableOffsetsNode = null,
					songTableSizesNode = null,
					sampleRateNode = null,
					reverbTypeNode = null,
					reverbNode = null,
					volumeNode = null,
					hasGoldenSunSynthsNode = null,
					hasPokemonCompression = null;
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
					if (gameToLoad.Children.TryGetValue(nameof(SongTableOffsets), out node))
					{
						songTableOffsetsNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(SongTableSizes), out node))
					{
						songTableSizesNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(SampleRate), out node))
					{
						sampleRateNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(ReverbType), out node))
					{
						reverbTypeNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(Reverb), out node))
					{
						reverbNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(Volume), out node))
					{
						volumeNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(HasGoldenSunSynths), out node))
					{
						hasGoldenSunSynthsNode = node;
					}
					if (gameToLoad.Children.TryGetValue(nameof(HasPokemonCompression), out node))
					{
						hasPokemonCompression = node;
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
								long songIndex = ConfigUtils.ParseValue(string.Format(Strings.ConfigKeySubkey, nameof(Playlists)), song.Key.ToString(), 0, long.MaxValue);
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
				string[] sizes = songTableSizesNode.ToString().Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
				if (sizes.Length != numSongTables)
				{
					throw new Exception(string.Format(Strings.ErrorAlphaDreamMP2KParseGameCode, gcv, CONFIG_FILE, Environment.NewLine + string.Format(Strings.ErrorAlphaDreamMP2KSongTableCounts, nameof(SongTableSizes), nameof(SongTableOffsets))));
				}
				SongTableOffsets = new int[numSongTables];
				SongTableSizes = new long[numSongTables];
				int maxOffset = rom.Length - 1;
				for (int i = 0; i < numSongTables; i++)
				{
					SongTableSizes[i] = ConfigUtils.ParseValue(nameof(SongTableSizes), sizes[i], 1, maxOffset);
					SongTableOffsets[i] = (int)ConfigUtils.ParseValue(nameof(SongTableOffsets), songTables[i], 0, maxOffset);
				}

				if (sampleRateNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(SampleRate), null);
				}
				SampleRate = (int)ConfigUtils.ParseValue(nameof(SampleRate), sampleRateNode.ToString(), 0, Utils.FrequencyTable.Length - 1);

				if (reverbTypeNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(ReverbType), null);
				}
				ReverbType = ConfigUtils.ParseEnum<ReverbType>(nameof(ReverbType), reverbTypeNode.ToString());

				if (reverbNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(Reverb), null);
				}
				Reverb = (byte)ConfigUtils.ParseValue(nameof(Reverb), reverbNode.ToString(), byte.MinValue, byte.MaxValue);

				if (volumeNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(Volume), null);
				}
				Volume = (byte)ConfigUtils.ParseValue(nameof(Volume), volumeNode.ToString(), 0, 15);

				if (hasGoldenSunSynthsNode is null)
				{
					throw new BetterKeyNotFoundException(nameof(HasGoldenSunSynths), null);
				}
				HasGoldenSunSynths = ConfigUtils.ParseBoolean(nameof(HasGoldenSunSynths), hasGoldenSunSynthsNode.ToString());

				if (hasPokemonCompression is null)
				{
					throw new BetterKeyNotFoundException(nameof(HasPokemonCompression), null);
				}
				HasPokemonCompression = ConfigUtils.ParseBoolean(nameof(HasPokemonCompression), hasPokemonCompression.ToString());

				// The complete playlist
				if (!Playlists.Any(p => p.Name == "Music"))
				{
					Playlists.Insert(0, new Playlist(Strings.PlaylistMusic, Playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => s.Index)));
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
			catch (YamlDotNet.Core.YamlException ex)
			{
				throw new Exception(string.Format(Strings.ErrorParseConfig, CONFIG_FILE, Environment.NewLine + ex.Message));
			}
		}
	}

	public override string GetGameName()
	{
		return Name;
	}
	public override string GetSongName(long index)
	{
		Song? s = GetFirstSong(index);
		return s is not null ? s.Name : index.ToString();
	}

	public override void Dispose()
	{
		Reader.Stream.Dispose();
	}
}
