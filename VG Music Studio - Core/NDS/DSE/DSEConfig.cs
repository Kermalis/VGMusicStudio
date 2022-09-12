using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Properties;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEConfig : Config
{
	public readonly string BGMPath;
	public readonly string[] BGMFiles;

	internal DSEConfig(string bgmPath)
	{
		BGMPath = bgmPath;
		BGMFiles = Directory.GetFiles(bgmPath, "bgm*.smd", SearchOption.TopDirectoryOnly);
		if (BGMFiles.Length == 0)
		{
			throw new DSENoSequencesException(bgmPath);
		}

		// TODO: Big endian files
		var songs = new List<Song>(BGMFiles.Length);
		for (int i = 0; i < BGMFiles.Length; i++)
		{
			using (FileStream stream = File.OpenRead(BGMFiles[i]))
			{
				var r = new EndianBinaryReader(stream, ascii: true);
				SMD.Header header = r.ReadObject<SMD.Header>();
				char[] chars = header.Label.ToCharArray();
				EndianBinaryPrimitives.TrimNullTerminators(ref chars);
				songs.Add(new Song(i, $"{Path.GetFileNameWithoutExtension(BGMFiles[i])} - {new string(chars)}"));
			}
		}
		Playlists.Add(new Playlist(Strings.PlaylistMusic, songs));
	}

	public override string GetGameName()
	{
		return "DSE";
	}
	public override string GetSongName(int index)
	{
		return index < 0 || index >= BGMFiles.Length
			? index.ToString()
			: '\"' + BGMFiles[index] + '\"';
	}
}
