using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA.AlphaDream;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Properties;
using Kermalis.VGMusicStudio.WinForms.Util;
using Kermalis.VGMusicStudio.WinForms.API.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
internal sealed class MainForm : ThemedForm
{
	private const int TARGET_WIDTH = 675;
	private const int TARGET_HEIGHT = 675 + 1 + 125 + 24;

	public static MainForm Instance { get; } = new MainForm();

	public readonly bool[] PianoTracks;

	private PlayingPlaylist? _playlist;
	private int _curSong = -1;

	private TrackViewer? _trackViewer;

	private bool _songEnded = false;
	private bool _positionBarFree = true;
	private bool _autoplay = false;

	#region Controls

	private readonly MenuStrip _mainMenu;
	private readonly ToolStripMenuItem _fileItem, _openDSEItem, _openAlphaDreamItem, _openMP2KItem, _openSDATItem,
		_dataItem, _trackViewerItem, _exportDLSItem, _exportMIDIItem, _exportSF2Item, _exportWAVItem,
		_playlistItem, _endPlaylistItem;
	private readonly Timer _timer;
	private readonly ThemedNumeric _songNumerical;
	private readonly ThemedButton _playButton, _pauseButton, _stopButton;
	private readonly SplitContainer _splitContainer;
	private readonly PianoControl _piano;
	private readonly ColorSlider _volumeBar, _positionBar;
	private readonly SongInfoControl _songInfo;
	private readonly ImageComboBox _songsComboBox;
	private readonly TaskbarPlayerButtons? _taskbar;

	#endregion

	private MainForm()
	{
		PianoTracks = new bool[SongState.MAX_TRACKS];
		for (int i = 0; i < SongState.MAX_TRACKS; i++)
		{
			PianoTracks[i] = true;
		}

		Mixer.VolumeChanged += Mixer_VolumeChanged;

		// File Menu
		_openDSEItem = new ToolStripMenuItem { Text = Strings.MenuOpenDSE };
		_openDSEItem.Click += OpenDSE;
		_openAlphaDreamItem = new ToolStripMenuItem { Text = Strings.MenuOpenAlphaDream };
		_openAlphaDreamItem.Click += OpenAlphaDream;
		_openMP2KItem = new ToolStripMenuItem { Text = Strings.MenuOpenMP2K };
		_openMP2KItem.Click += OpenMP2K;
		_openSDATItem = new ToolStripMenuItem { Text = Strings.MenuOpenSDAT };
		_openSDATItem.Click += OpenSDAT;
		_fileItem = new ToolStripMenuItem { Text = Strings.MenuFile };
		_fileItem.DropDownItems.AddRange(new ToolStripItem[] { _openDSEItem, _openAlphaDreamItem, _openMP2KItem, _openSDATItem });

		// Data Menu
		_trackViewerItem = new ToolStripMenuItem { ShortcutKeys = Keys.Control | Keys.T, Text = Strings.TrackViewerTitle };
		_trackViewerItem.Click += OpenTrackViewer;
		_exportDLSItem = new ToolStripMenuItem { Enabled = false, Text = Strings.MenuSaveDLS };
		_exportDLSItem.Click += ExportDLS;
		_exportMIDIItem = new ToolStripMenuItem { Enabled = false, Text = Strings.MenuSaveMIDI };
		_exportMIDIItem.Click += ExportMIDI;
		_exportSF2Item = new ToolStripMenuItem { Enabled = false, Text = Strings.MenuSaveSF2 };
		_exportSF2Item.Click += ExportSF2;
		_exportWAVItem = new ToolStripMenuItem { Enabled = false, Text = Strings.MenuSaveWAV };
		_exportWAVItem.Click += ExportWAV;
		_dataItem = new ToolStripMenuItem { Text = Strings.MenuData };
		_dataItem.DropDownItems.AddRange(new ToolStripItem[] { _trackViewerItem, _exportDLSItem, _exportMIDIItem, _exportSF2Item, _exportWAVItem });

		// Playlist Menu
		_endPlaylistItem = new ToolStripMenuItem { Enabled = false, Text = Strings.MenuEndPlaylist };
		_endPlaylistItem.Click += EndCurrentPlaylist;
		_playlistItem = new ToolStripMenuItem { Text = Strings.MenuPlaylist };
		_playlistItem.DropDownItems.AddRange(new ToolStripItem[] { _endPlaylistItem });

		// Main Menu
		_mainMenu = new MenuStrip { Size = new Size(TARGET_WIDTH, 24) };
		_mainMenu.Items.AddRange(new ToolStripItem[] { _fileItem, _dataItem, _playlistItem });

		// Buttons
		_playButton = new ThemedButton { Enabled = false, ForeColor = Color.MediumSpringGreen, Text = Strings.PlayerPlay };
		_playButton.Click += PlayButton_Click;
		_pauseButton = new ThemedButton { Enabled = false, ForeColor = Color.DeepSkyBlue, Text = Strings.PlayerPause };
		_pauseButton.Click += PauseButton_Click;
		_stopButton = new ThemedButton { Enabled = false, ForeColor = Color.MediumVioletRed, Text = Strings.PlayerStop };
		_stopButton.Click += StopButton_Click;

		// Numerical
		_songNumerical = new ThemedNumeric { Enabled = false, Minimum = 0, Visible = false };
		_songNumerical.ValueChanged += SongNumerical_ValueChanged;

		// Timer
		_timer = new Timer();
		_timer.Tick += Timer_Tick;

		// Piano
		_piano = new PianoControl();

		// Volume bar
		_volumeBar = new ColorSlider { Enabled = false, LargeChange = 20, Maximum = 100, SmallChange = 5 };
		_volumeBar.ValueChanged += VolumeBar_ValueChanged;

		// Position bar
		_positionBar = new ColorSlider { AcceptKeys = false, Enabled = false, Maximum = 0 };
		_positionBar.MouseUp += PositionBar_MouseUp;
		_positionBar.MouseDown += PositionBar_MouseDown;

		// Playlist box
		_songsComboBox = new ImageComboBox { Enabled = false };
		_songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;

		// Track info
		_songInfo = new SongInfoControl { Dock = DockStyle.Fill };

		// Split container
		_splitContainer = new SplitContainer { BackColor = Theme.TitleBar, Dock = DockStyle.Fill, IsSplitterFixed = true, Orientation = Orientation.Horizontal, SplitterWidth = 1 };
		_splitContainer.Panel1.Controls.AddRange(new Control[] { _playButton, _pauseButton, _stopButton, _songNumerical, _songsComboBox, _piano, _volumeBar, _positionBar });
		_splitContainer.Panel2.Controls.Add(_songInfo);

		// MainForm
		ClientSize = new Size(TARGET_WIDTH, TARGET_HEIGHT);
		Controls.AddRange(new Control[] { _splitContainer, _mainMenu });
		MainMenuStrip = _mainMenu;
		MinimumSize = new Size(TARGET_WIDTH + (Width - TARGET_WIDTH), TARGET_HEIGHT + (Height - TARGET_HEIGHT)); // Borders
		Resize += OnResize;
		Text = ConfigUtils.PROGRAM_NAME;

		// Taskbar Buttons
		if (TaskbarManager.IsPlatformSupported)
		{
			_taskbar = new TaskbarPlayerButtons(Handle);
		}

		OnResize(null, EventArgs.Empty);
	}

	private void SongNumerical_ValueChanged(object? sender, EventArgs e)
	{
		_songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;

		int index = (int)_songNumerical.Value;
		Stop();
		Text = ConfigUtils.PROGRAM_NAME;
		_songsComboBox.SelectedIndex = 0;
		_songInfo.Reset();

		Player player = Engine.Instance!.Player;
		Config cfg = Engine.Instance.Config;
		try
		{
			player.LoadSong(index);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, string.Format(Strings.ErrorLoadSong, cfg.GetSongName(index)));
		}

		_trackViewer?.UpdateTracks();
		ILoadedSong? loadedSong = player.LoadedSong; // LoadedSong is still null when there are no tracks
		if (loadedSong is not null)
		{
			List<Config.Song> songs = cfg.Playlists[0].Songs; // Complete "Music" playlist is present in all configs at index 0
			int songIndex = songs.FindIndex(s => s.Index == index);
			if (songIndex != -1)
			{
				Text = $"{ConfigUtils.PROGRAM_NAME} ― {songs[songIndex].Name}"; // TODO: Make this a func
				_songsComboBox.SelectedIndex = songIndex + 1; // + 1 because the "Music" playlist is first in the combobox
			}
			_positionBar.Maximum = loadedSong.MaxTicks;
			_positionBar.LargeChange = _positionBar.Maximum / 10;
			_positionBar.SmallChange = _positionBar.LargeChange / 4;
			_songInfo.SetNumTracks(loadedSong.Events.Length);
			if (_autoplay)
			{
				Play();
			}
			_positionBar.Enabled = true;
			_exportWAVItem.Enabled = true;
			_exportMIDIItem.Enabled = MP2KEngine.MP2KInstance is not null;
			_exportDLSItem.Enabled = _exportSF2Item.Enabled = AlphaDreamEngine.AlphaDreamInstance is not null;
		}
		else
		{
			_songInfo.SetNumTracks(0);
			_positionBar.Enabled = false;
			_exportWAVItem.Enabled = false;
			_exportMIDIItem.Enabled = false;
			_exportDLSItem.Enabled = false;
			_exportSF2Item.Enabled = false;
		}

		_autoplay = true;
		_songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
	}
	private void SongsComboBox_SelectedIndexChanged(object? sender, EventArgs e)
	{
		var item = (ImageComboBoxItem)_songsComboBox.SelectedItem;
		switch (item.Item)
		{
			case Config.Song song:
			{
				SetAndLoadSong(song.Index);
				break;
			}
			case Config.Playlist playlist:
			{
				if (playlist.Songs.Count > 0
					&& FlexibleMessageBox.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), Strings.MenuPlaylist, MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					ResetPlaylistStuff(false);
					Engine.Instance!.Player.ShouldFadeOut = true;
					Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
					_endPlaylistItem.Enabled = true;
					_playlist = new PlayingPlaylist(playlist);
					_playlist.SetAndLoadNextSong();
				}
				break;
			}
		}
	}
	private void ResetPlaylistStuff(bool numericalAndComboboxEnabled)
	{
		if (Engine.Instance is not null)
		{
			Engine.Instance.Player.ShouldFadeOut = false;
		}
		_curSong = -1;
		_playlist = null;
		_endPlaylistItem.Enabled = false;
		_songNumerical.Enabled = numericalAndComboboxEnabled;
		_songsComboBox.Enabled = numericalAndComboboxEnabled;
	}
	private void EndCurrentPlaylist(object? sender, EventArgs e)
	{
		if (FlexibleMessageBox.Show(Strings.EndPlaylistBody, Strings.MenuPlaylist, MessageBoxButtons.YesNo) == DialogResult.Yes)
		{
			ResetPlaylistStuff(true);
		}
	}

	private void OpenDSE(object? sender, EventArgs e)
	{
		var f = WinFormsUtils.CreateLoadDialog(".swd", Strings.MenuOpenSWD, Strings.FilterOpenSWD + " (*.swd)|*.swd");
        if (f is null)
        {
            return;
        }
        var d = new FolderBrowserDialog
		{
			Description = Strings.MenuOpenSMD,
			UseDescriptionForTitle = true,
		};
		if (d.ShowDialog() != DialogResult.OK)
		{
			return;
		}

		DisposeEngine();
		try
		{
			_ = new DSEEngine(f.ToString(), d.SelectedPath);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorOpenDSE);
			return;
		}

		DSEConfig config = DSEEngine.DSEInstance!.Config;
		FinishLoading(config.SMDFiles.Length);
		_songNumerical.Visible = false;
		_exportDLSItem.Visible = false;
		_exportMIDIItem.Visible = false;
		_exportSF2Item.Visible = false;
	}
	private void OpenAlphaDream(object? sender, EventArgs e)
	{
		string? inFile = WinFormsUtils.CreateLoadDialog(".gba", Strings.MenuOpenAlphaDream, Strings.FilterOpenGBA + " (*.gba)|*.gba");
		if (inFile is null)
		{
			return;
		}

		DisposeEngine();
		try
		{
			_ = new AlphaDreamEngine(File.ReadAllBytes(inFile));
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorOpenAlphaDream);
			return;
		}

		AlphaDreamConfig config = AlphaDreamEngine.AlphaDreamInstance!.Config;
		FinishLoading(config.SongTableSizes[0]);
		_songNumerical.Visible = true;
		_exportDLSItem.Visible = true;
		_exportMIDIItem.Visible = false;
		_exportSF2Item.Visible = true;
	}
	private void OpenMP2K(object? sender, EventArgs e)
	{
		string? inFile = WinFormsUtils.CreateLoadDialog(".gba", Strings.MenuOpenMP2K, Strings.FilterOpenGBA + " (*.gba)|*.gba");
		if (inFile is null)
		{
			return;
		}

		DisposeEngine();
		try
		{
			_ = new MP2KEngine(File.ReadAllBytes(inFile));
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorOpenMP2K);
			return;
		}

		MP2KConfig config = MP2KEngine.MP2KInstance!.Config;
		FinishLoading(config.SongTableSizes[0]);
		_songNumerical.Visible = true;
		_exportDLSItem.Visible = false;
		_exportMIDIItem.Visible = true;
		_exportSF2Item.Visible = false;
	}
	private void OpenSDAT(object? sender, EventArgs e)
	{
		string? inFile = WinFormsUtils.CreateLoadDialog(".sdat", Strings.MenuOpenSDAT, Strings.FilterOpenSDAT + " (*.sdat)|*.sdat");
		if (inFile is null)
		{
			return;
		}

		DisposeEngine();
		try
		{
			using (FileStream stream = File.OpenRead(inFile))
			{
				_ = new SDATEngine(new SDAT(stream));
			}
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorOpenSDAT);
			return;
		}

		SDATConfig config = SDATEngine.SDATInstance!.Config;
		FinishLoading(config.SDAT.INFOBlock.SequenceInfos.NumEntries);
		_songNumerical.Visible = true;
		_exportDLSItem.Visible = false;
		_exportMIDIItem.Visible = false;
		_exportSF2Item.Visible = false;
	}

	private void ExportDLS(object? sender, EventArgs e)
	{
		AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;
		string? outFile = WinFormsUtils.CreateSaveDialog(cfg.GetGameName(), ".dls", Strings.MenuSaveDLS, Strings.FilterSaveDLS + " (*.dls)|*.dls");
		if (outFile is null)
		{
			return;
		}

		try
		{
			AlphaDreamSoundFontSaver_DLS.Save(cfg, outFile);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveDLS, outFile), Text);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveDLS);
		}
	}
	private void ExportMIDI(object? sender, EventArgs e)
	{
		string songName = Engine.Instance!.Config.GetSongName((int)_songNumerical.Value);
		string? outFile = WinFormsUtils.CreateSaveDialog(songName, ".mid", Strings.MenuSaveMIDI, Strings.FilterSaveMIDI + " (*.mid;*.midi)|*.mid;*.midi");
		if (outFile is null)
		{
			return;
		}

		MP2KPlayer p = MP2KEngine.MP2KInstance!.Player;
		var args = new MIDISaveArgs(true, false, new (int AbsoluteTick, (byte Numerator, byte Denominator))[]
		{
			(0, (4, 4)),
		});

		try
		{
			p.SaveAsMIDI(outFile, args);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveMIDI, outFile), Text);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveMIDI);
		}
	}
	private void ExportSF2(object? sender, EventArgs e)
	{
		AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;
		string? outFile = WinFormsUtils.CreateSaveDialog(cfg.GetGameName(), ".sf2", Strings.MenuSaveSF2, Strings.FilterSaveSF2 + " (*.sf2)|*.sf2");
		if (outFile is null)
		{
			return;
		}

		try
		{
			AlphaDreamSoundFontSaver_SF2.Save(outFile, cfg);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveSF2, outFile), Text);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveSF2);
		}
	}
	private void ExportWAV(object? sender, EventArgs e)
	{
		string songName = Engine.Instance!.Config.GetSongName((int)_songNumerical.Value);
		string? outFile = WinFormsUtils.CreateSaveDialog(songName, ".wav", Strings.MenuSaveWAV, Strings.FilterSaveWAV + " (*.wav)|*.wav");
		if (outFile is null)
		{
			return;
		}

		Stop();

		Player player = Engine.Instance.Player;
		bool oldFade = player.ShouldFadeOut;
		long oldLoops = player.NumLoops;
		player.ShouldFadeOut = true;
		player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;

		try
		{
			player.Record(outFile);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveWAV, outFile), Text);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveWAV);
		}

		player.ShouldFadeOut = oldFade;
		player.NumLoops = oldLoops;
		_songEnded = false; // Don't make UI do anything about the song ended event
	}

	private void Play()
	{
		Engine.Instance!.Player.Play();
		LetUIKnowPlayerIsPlaying();
	}
	private void Pause()
	{
		Engine.Instance!.Player.TogglePlaying();
		if (Engine.Instance.Player.State == PlayerState.Paused)
		{
			_pauseButton.Text = Strings.PlayerUnpause;
			_timer.Stop();
		}
		else
		{
			_pauseButton.Text = Strings.PlayerPause;
			_timer.Start();
		}
		TaskbarPlayerButtons.UpdateState();
		UpdateTaskbarButtons();
	}
	private void Stop()
	{
		Engine.Instance!.Player.Stop();
		_pauseButton.Enabled = false;
		_stopButton.Enabled = false;
		_pauseButton.Text = Strings.PlayerPause;
		_timer.Stop();
		_songInfo.Reset();
		_piano.UpdateKeys(_songInfo.Info.Tracks, PianoTracks);
		UpdatePositionIndicators(0L);
		TaskbarPlayerButtons.UpdateState();
		UpdateTaskbarButtons();
	}

	private void FinishLoading(long numSongs)
	{
		Engine.Instance!.Player.SongEnded += Player_SongEnded;
		foreach (Config.Playlist playlist in Engine.Instance.Config.Playlists)
		{
			_songsComboBox.Items.Add(new ImageComboBoxItem(playlist, Resources.IconPlaylist, 0));
			_songsComboBox.Items.AddRange(playlist.Songs.Select(s => new ImageComboBoxItem(s, Resources.IconSong, 1)).ToArray());
		}
		_songNumerical.Maximum = numSongs - 1;
#if DEBUG
		//VGMSDebug.EventScan(Engine.Instance.Config.Playlists[0].Songs, numericalVisible);
#endif
		_autoplay = false;
		SetAndLoadSong(Engine.Instance.Config.Playlists[0].Songs.Count == 0 ? 0 : Engine.Instance.Config.Playlists[0].Songs[0].Index);
		_songsComboBox.Enabled = _songNumerical.Enabled = _playButton.Enabled = _volumeBar.Enabled = true;
		UpdateTaskbarButtons();
	}
	private void DisposeEngine()
	{
		if (Engine.Instance is not null)
		{
			Stop();
			Engine.Instance.Dispose();
		}

		Text = ConfigUtils.PROGRAM_NAME;
		_trackViewer?.UpdateTracks();
		_taskbar?.DisableAll();
		_songsComboBox.Enabled = false;
		_songNumerical.Enabled = false;
		_playButton.Enabled = false;
		_volumeBar.Enabled = false;
		_positionBar.Enabled = false;
		_songInfo.SetNumTracks(0);
		_songInfo.ResetMutes();
		ResetPlaylistStuff(false);
		UpdatePositionIndicators(0L);
		TaskbarPlayerButtons.UpdateState();

		_songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;
		_songNumerical.ValueChanged -= SongNumerical_ValueChanged;

		_songNumerical.Visible = false;
		_songsComboBox.SelectedItem = null;
		_songsComboBox.Items.Clear();

		_songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
		_songNumerical.ValueChanged += SongNumerical_ValueChanged;
	}
	private void UpdatePositionIndicators(long ticks)
	{
		if (_positionBarFree)
		{
			_positionBar.Value = ticks;
		}
		if (GlobalConfig.Instance.TaskbarProgress && TaskbarManager.IsPlatformSupported)
		{
			TaskbarManager.Instance.SetProgressValue((int)ticks, (int)_positionBar.Maximum);
		}
	}
	private void UpdateTaskbarButtons()
	{
		_taskbar?.UpdateButtons(_playlist, _curSong, (int)_songNumerical.Maximum);
	}

	private void OpenTrackViewer(object? sender, EventArgs e)
	{
		if (_trackViewer is not null)
		{
			_trackViewer.Focus();
			return;
		}

		_trackViewer = new TrackViewer { Owner = this };
		_trackViewer.FormClosed += TrackViewer_FormClosed;
		_trackViewer.Show();
	}

	public void TogglePlayback()
	{
		switch (Engine.Instance!.Player.State)
		{
			case PlayerState.Stopped: Play(); break;
			case PlayerState.Paused:
			case PlayerState.Playing: Pause(); break;
		}
	}
	public void PlayPreviousSong()
	{
		if (_playlist is not null)
		{
			_playlist.UndoThenSetAndLoadPrevSong(_curSong);
		}
		else
		{
			SetAndLoadSong((int)_songNumerical.Value - 1);
		}
	}
	public void PlayNextSong()
	{
		if (_playlist is not null)
		{
			_playlist.AdvanceThenSetAndLoadNextSong(_curSong);
		}
		else
		{
			SetAndLoadSong((int)_songNumerical.Value + 1);
		}
	}
	public void LetUIKnowPlayerIsPlaying()
	{
		if (_timer.Enabled)
		{
			return;
		}

		_pauseButton.Enabled = true;
		_stopButton.Enabled = true;
		_pauseButton.Text = Strings.PlayerPause;
		_timer.Interval = (int)(1_000.0 / GlobalConfig.Instance.RefreshRate);
		_timer.Start();
		TaskbarPlayerButtons.UpdateState();
		UpdateTaskbarButtons();
	}
	public void SetAndLoadSong(int index)
	{
		_curSong = index;
		if (_songNumerical.Value == index)
		{
			SongNumerical_ValueChanged(null, EventArgs.Empty);
		}
		else
		{
			_songNumerical.Value = index;
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		DisposeEngine();
		base.OnFormClosing(e);
	}
	private void OnResize(object? sender, EventArgs e)
	{
		if (WindowState == FormWindowState.Minimized)
		{
			return;
		}

		_splitContainer.SplitterDistance = (int)(ClientSize.Height / 5.5) - 25; // -25 for menustrip (24) and itself (1)

		int w1 = (int)(_splitContainer.Panel1.Width / 2.35);
		int h1 = (int)(_splitContainer.Panel1.Height / 5.0);

		int xoff = _splitContainer.Panel1.Width / 83;
		int yoff = _splitContainer.Panel1.Height / 25;
		int a, b, c, d;

		// Buttons
		a = (w1 / 3) - xoff;
		b = (xoff / 2) + 1;
		_playButton.Location = new Point(xoff + b, yoff);
		_pauseButton.Location = new Point((xoff * 2) + a + b, yoff);
		_stopButton.Location = new Point((xoff * 3) + (a * 2) + b, yoff);
		_playButton.Size = _pauseButton.Size = _stopButton.Size = new Size(a, h1);
		c = yoff + ((h1 - 21) / 2);
		_songNumerical.Location = new Point((xoff * 4) + (a * 3) + b, c);
		_songNumerical.Size = new Size((int)(a / 1.175), 21);
		// Song combobox
		d = _splitContainer.Panel1.Width - w1 - xoff;
		_songsComboBox.Location = new Point(d, c);
		_songsComboBox.Size = new Size(w1, 21);

		// Volume bar
		c = (int)(_splitContainer.Panel1.Height / 3.5);
		_volumeBar.Location = new Point(xoff, c);
		_volumeBar.Size = new Size(w1, h1);
		// Position bar
		_positionBar.Location = new Point(d, c);
		_positionBar.Size = new Size(w1, h1);

		// Piano
		_piano.Size = new Size(_splitContainer.Panel1.Width, (int)(_splitContainer.Panel1.Height / 2.5)); // Force it to initialize piano keys again
		_piano.Location = new Point((_splitContainer.Panel1.Width - (_piano.WhiteKeyWidth * PianoControl.WHITE_KEY_COUNT)) / 2, _splitContainer.Panel1.Height - _piano.Height - 1);
	}
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Space && _playButton.Enabled && !_songsComboBox.Focused)
		{
			TogglePlayback();
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer.Dispose();
		}
		base.Dispose(disposing);
	}

	private void Timer_Tick(object? sender, EventArgs e)
	{
		if (_songEnded)
		{
			_songEnded = false;
			if (_playlist is not null)
			{
				_playlist.AdvanceThenSetAndLoadNextSong(_curSong);
			}
			else
			{
				Stop();
			}
		}
		else
		{
			Player player = Engine.Instance!.Player;
			if (WindowState != FormWindowState.Minimized)
			{
				SongState info = _songInfo.Info;
				player.UpdateSongState(info);
				_piano.UpdateKeys(info.Tracks, PianoTracks);
				_songInfo.Invalidate();
			}
			UpdatePositionIndicators(player.ElapsedTicks);
		}
	}
	private void Mixer_VolumeChanged(float volume)
	{
		_volumeBar.ValueChanged -= VolumeBar_ValueChanged;
		_volumeBar.Value = (int)(volume * _volumeBar.Maximum);
		_volumeBar.ValueChanged += VolumeBar_ValueChanged;
	}
	private void Player_SongEnded()
	{
		_songEnded = true;
	}
	private void VolumeBar_ValueChanged(object? sender, EventArgs e)
	{
		Engine.Instance!.Mixer.SetVolume(_volumeBar.Value / (float)_volumeBar.Maximum);
	}
	private void PositionBar_MouseUp(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			Engine.Instance!.Player.SetSongPosition(_positionBar.Value);
			_positionBarFree = true;
			LetUIKnowPlayerIsPlaying();
		}
	}
	private void PositionBar_MouseDown(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_positionBarFree = false;
		}
	}
	private void PlayButton_Click(object? sender, EventArgs e)
	{
		Play();
	}
	private void PauseButton_Click(object? sender, EventArgs e)
	{
		Pause();
	}
	private void StopButton_Click(object? sender, EventArgs e)
	{
		Stop();
	}
	private void TrackViewer_FormClosed(object? sender, FormClosedEventArgs e)
	{
		_trackViewer = null;
	}
}
