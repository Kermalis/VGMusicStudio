using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Properties;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEConfig : Config
{
	public readonly string SMDPath;
	public readonly string[] SMDFiles;

	internal DSEConfig(string smdPath)
	{
		SMDPath = smdPath;
		SMDFiles = Directory.GetFiles(smdPath, "*.smd", SearchOption.TopDirectoryOnly);
		if (SMDFiles.Length == 0)
		{
			throw new DSENoSequencesException(smdPath);
		}

		// TODO: Big endian files
		var songs = new List<Song>(SMDFiles.Length);
		for (int i = 0; i < SMDFiles.Length; i++)
		{
			using (FileStream stream = File.OpenRead(SMDFiles[i]))
			{
				var r = new EndianBinaryReader(stream, ascii: true);
				SMD.Header header = new SMD.Header(r);
				if(header.Type == "smdl")
				{
					char[] chars = header.Label.ToCharArray();
					EndianBinaryPrimitives.TrimNullTerminators(ref chars);
					songs.Add(new Song(i, $"{Path.GetFileNameWithoutExtension(SMDFiles[i])} - {new string(chars)}"));
				}
				else if(header.Type == "smdb")
				{
					r.Endianness = Endianness.BigEndian;
					char[] chars = header.Label.ToCharArray();
					EndianBinaryPrimitives.TrimNullTerminators(ref chars);
					songs.Add(new Song(i, $"{Path.GetFileNameWithoutExtension(SMDFiles[i])} - {new string(chars)}"));
				}
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
		return index < 0 || index >= SMDFiles.Length
			? index.ToString()
			: '\"' + SMDFiles[index] + '\"';
	}
}
