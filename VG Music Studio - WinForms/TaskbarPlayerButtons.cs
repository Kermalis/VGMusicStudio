using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Properties;
using Kermalis.VGMusicStudio.WinForms.API.Taskbar;
using System;

namespace Kermalis.VGMusicStudio.WinForms;

internal sealed class TaskbarPlayerButtons
{
	private readonly ThumbnailToolBarButton _prevTButton, _toggleTButton, _nextTButton;

	public TaskbarPlayerButtons(IntPtr handle)
	{
		_prevTButton = new ThumbnailToolBarButton(Resources.IconPrevious, Strings.PlayerPreviousSong);
		_prevTButton.Click += PrevTButton_Click;
		_toggleTButton = new ThumbnailToolBarButton(Resources.IconPlay, Strings.PlayerPlay);
		_toggleTButton.Click += ToggleTButton_Click;
		_nextTButton = new ThumbnailToolBarButton(Resources.IconNext, Strings.PlayerNextSong);
		_nextTButton.Click += NextTButton_Click;
		_prevTButton.Enabled = _toggleTButton.Enabled = _nextTButton.Enabled = false;
		TaskbarManager.Instance.ThumbnailToolBars.AddButtons(handle, _prevTButton, _toggleTButton, _nextTButton);
	}

	private void PrevTButton_Click(object? sender, ThumbnailButtonClickedEventArgs e)
	{
		MainForm.Instance.PlayPreviousSong();
	}
	private void ToggleTButton_Click(object? sender, ThumbnailButtonClickedEventArgs e)
	{
		MainForm.Instance.TogglePlayback();
	}
	private void NextTButton_Click(object? sender, ThumbnailButtonClickedEventArgs e)
	{
		MainForm.Instance.PlayNextSong();
	}

	public void DisableAll()
	{
		_prevTButton.Enabled = false;
		_toggleTButton.Enabled = false;
		_nextTButton.Enabled = false;
	}
	public void UpdateButtons(PlayingPlaylist? playlist, int curSong, int maxSong)
	{
		if (playlist is not null)
		{
			_prevTButton.Enabled = playlist._playedSongs.Count > 0;
			_nextTButton.Enabled = true;
		}
		else
		{
			_prevTButton.Enabled = curSong > 0;
			_nextTButton.Enabled = curSong < maxSong;
		}
		switch (Engine.Instance!.Player.State)
		{
			case PlayerState.Stopped: _toggleTButton.Icon = Resources.IconPlay; _toggleTButton.Tooltip = Strings.PlayerPlay; break;
			case PlayerState.Playing: _toggleTButton.Icon = Resources.IconPause; _toggleTButton.Tooltip = Strings.PlayerPause; break;
			case PlayerState.Paused: _toggleTButton.Icon = Resources.IconPlay; _toggleTButton.Tooltip = Strings.PlayerUnpause; break;
		}
		_toggleTButton.Enabled = true;
	}
	public static void UpdateState()
	{
		if (!GlobalConfig.Instance.TaskbarProgress || !TaskbarManager.IsPlatformSupported)
		{
			return;
		}

		TaskbarProgressBarState state;
		switch (Engine.Instance?.Player.State)
		{
			case PlayerState.Playing: state = TaskbarProgressBarState.Normal; break;
			case PlayerState.Paused: state = TaskbarProgressBarState.Paused; break;
			default: state = TaskbarProgressBarState.NoProgress; break;
		}
		TaskbarManager.Instance.SetProgressState(state);
	}
}
