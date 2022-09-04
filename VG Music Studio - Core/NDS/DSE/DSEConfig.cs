using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Properties;
using System.IO;
using System.Linq;

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

		var songs = new Song[BGMFiles.Length];
		for (int i = 0; i < BGMFiles.Length; i++)
		{
			using (FileStream stream = File.OpenRead(BGMFiles[i]))
			{
				var r = new EndianBinaryReader(stream, ascii: true);
				SMD.Header header = r.ReadObject<SMD.Header>();
				songs[i] = new Song(i, $"{Path.GetFileNameWithoutExtension(BGMFiles[i])} - {new string(header.Label.TakeWhile(c => c != '\0').ToArray())}");
			}
		}
		Playlists.Add(new Playlist(Strings.PlaylistMusic, songs));
	}

	public override string GetGameName()
	{
		return "DSE";
	}
	public override string GetSongName(long index)
	{
		return index < 0 || index >= BGMFiles.Length
			? index.ToString()
			: '\"' + BGMFiles[index] + '\"';
	}
}
