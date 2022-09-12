using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core;

public abstract class Config : IDisposable
{
	public readonly struct Song
	{
		public readonly int Index;
		public readonly string Name;

		public Song(int index, string name)
		{
			Index = index;
			Name = name;
		}

		public static bool operator ==(Song left, Song right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(Song left, Song right)
		{
			return !(left == right);
		}

		public override bool Equals(object? obj)
		{
			return obj is Song other && other.Index == Index;
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
	public sealed class Playlist
	{
		public string Name;
		public List<Song> Songs;

		public Playlist(string name, List<Song> songs)
		{
			Name = name;
			Songs = songs;
		}

		public override string ToString()
		{
			int num = Songs.Count;
			return string.Format("{0} - ({1:N0} {2})", Name, num, LanguageUtils.HandlePlural(num, Strings.Song_s_));
		}
	}

	public readonly List<Playlist> Playlists;

	protected Config()
	{
		Playlists = new List<Playlist>();
	}

	public bool TryGetFirstSong(int index, out Song song)
	{
		foreach (Playlist p in Playlists)
		{
			foreach (Song s in p.Songs)
			{
				if (s.Index == index)
				{
					song = s;
					return true;
				}
			}
		}
		song = default;
		return false;
	}

	public abstract string GetGameName();
	public abstract string GetSongName(int index);

	public virtual void Dispose()
	{
		//
	}
}
