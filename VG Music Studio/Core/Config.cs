using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core
{
    internal abstract class Config : IDisposable
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
        public class Playlist
        {
            public string Name;
            public List<Song> Songs;

            public Playlist(string name, IEnumerable<Song> songs)
            {
                Name = name; Songs = songs.ToList();
            }

            public override string ToString()
            {
                int songCount = Songs.Count;
                CultureInfo cul = System.Threading.Thread.CurrentThread.CurrentUICulture;
                if (cul.TwoLetterISOLanguageName == "it") // Italian
                {
                    // PlaylistName - (1 Canzone)
                    // PlaylistName - (2 Canzoni)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Canzone" : "Canzoni")})";
                }
                else if (cul.TwoLetterISOLanguageName == "es") // Spanish
                {
                    // PlaylistName - (1 Canción)
                    // PlaylistName - (2 Canciones)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Canción" : "Canciones")})";
                }
                else // Fallback to en-US
                {
                    // PlaylistName - (1 Song)
                    // PlaylistName - (2 Songs)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Song" : "Songs")})";
                }
            }
        }

        public List<Playlist> Playlists = new List<Playlist>();

        public abstract string GetSongName(long index);

        public virtual void Dispose() { }
    }
}
