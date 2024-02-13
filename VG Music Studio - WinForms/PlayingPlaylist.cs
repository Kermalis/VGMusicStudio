using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Util;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.WinForms;

internal sealed class PlayingPlaylist
{
	public readonly List<int> _playedSongs;
	public readonly List<int> _remainingSongs;
	public readonly Config.Playlist _curPlaylist;

	public PlayingPlaylist(Config.Playlist play)
	{
		_playedSongs = new List<int>();
		_remainingSongs = new List<int>();
		_curPlaylist = play;
	}

	public void AdvanceThenSetAndLoadNextSong(int curSong)
	{
		_playedSongs.Add(curSong);
		SetAndLoadNextSong();
	}
	public void UndoThenSetAndLoadPrevSong(int curSong)
	{
		int prevIndex = _playedSongs.Count - 1;
		int prevSong = _playedSongs[prevIndex];
		_playedSongs.RemoveAt(prevIndex);
		_remainingSongs.Insert(0, curSong);
		MainForm.Instance.SetAndLoadSong(prevSong);
	}
	public void SetAndLoadNextSong()
	{
		if (_remainingSongs.Count == 0)
		{
			_remainingSongs.AddRange(_curPlaylist.Songs.Select(s => s.Index));
			if (GlobalConfig.Instance.PlaylistMode == PlaylistMode.Random)
			{
				_remainingSongs.Shuffle();
			}
		}
		int nextSong = _remainingSongs[0];
		_remainingSongs.RemoveAt(0);
		MainForm.Instance.SetAndLoadSong(nextSong);
	}
}
