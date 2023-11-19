using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA.AlphaDream;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.GTK4.Util;
using GObject;
using Adw;
using Gtk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Application = Adw.Application;
using Window = Adw.Window;

namespace Kermalis.VGMusicStudio.GTK4;

internal sealed class MainWindow : Window
{
	private int _duration = 0;
	private int _position = 0;

	private PlayingPlaylist? _playlist;
	private int _curSong = -1;

	private static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows); // Because WASAPI (via NAudio) is the only audio backend currently.

	private bool _songEnded = false;
	private bool _stopUI = false;
	private bool _autoplay = false;

	#region Widgets

	// Buttons
	private readonly Button _buttonPlay, _buttonPause, _buttonStop;

	// A Box specifically made to contain two contents inside
	private readonly Box _splitContainerBox;

	// Spin Button for the numbered tracks
	private readonly SpinButton _sequenceNumberSpinButton;

	// Timer
	private readonly Timer _timer;

	// Popover Menu Bar
	private readonly PopoverMenuBar _popoverMenuBar;

	// LibAdwaita Header Bar
	private readonly Adw.HeaderBar _headerBar;

	// LibAdwaita Application
	private readonly Adw.Application _app;

	// LibAdwaita Message Dialog
	private Adw.MessageDialog _dialog;

	// Menu Model
	//private readonly Gio.MenuModel _mainMenu;

	// Menus
	private readonly Gio.Menu _mainMenu, _fileMenu, _dataMenu, _playlistMenu;

	// Menu Labels
	private readonly Label _fileLabel, _dataLabel, _playlistLabel;

	// Menu Items
	private readonly Gio.MenuItem _fileItem, _openDSEItem, _openAlphaDreamItem, _openMP2KItem, _openSDATItem,
		_dataItem, _trackViewerItem, _exportDLSItem, _exportSF2Item, _exportMIDIItem, _exportWAVItem, _playlistItem, _endPlaylistItem; 

	// Menu Actions
	private Gio.SimpleAction _openDSEAction, _openAlphaDreamAction, _openMP2KAction, _openSDATAction,
		_dataAction, _trackViewerAction, _exportDLSAction, _exportSF2Action, _exportMIDIAction, _exportWAVAction, _playlistAction, _endPlaylistAction,
		_soundSequenceAction;

	private Signal<Gio.SimpleAction> _openDSESignal;

	private SignalHandler<Gio.SimpleAction> _openDSEHandler;

	// Main Box
	private Box _mainBox, _configButtonBox, _configPlayerButtonBox, _configSpinButtonBox, _configScaleBox;

	// Volume Button to indicate volume status
	private readonly VolumeButton _volumeButton;

	// One Scale controling volume and one Scale for the sequenced track
	private Scale _volumeScale, _positionScale;

	// Mouse Click Gesture
	private GestureClick _positionGestureClick, _sequencesGestureClick;

	// Event Controller
	private EventArgs _openDSEEvent, _openAlphaDreamEvent, _openMP2KEvent, _openSDATEvent,
		_dataEvent, _trackViewerEvent, _exportDLSEvent, _exportSF2Event, _exportMIDIEvent, _exportWAVEvent, _playlistEvent, _endPlaylistEvent,
		_sequencesEventController;

	// Adjustments are for indicating the numbers and the position of the scale
	private readonly Adjustment _volumeAdjustment, _sequenceNumberAdjustment;
	//private ScaleControl _positionAdjustment;

	// Sound Sequence List
	//private SignalListItemFactory _soundSequenceFactory;
	//private SoundSequenceList _soundSequenceList;
	//private SoundSequenceListItem _soundSequenceListItem;
	//private SortListModel _soundSequenceSortListModel;
	//private ListBox _soundSequenceListBox;
	//private DropDown _soundSequenceDropDown;

	// Error Handle
	private GLib.Internal.ErrorOwnedHandle ErrorHandle = new GLib.Internal.ErrorOwnedHandle(IntPtr.Zero);

	// Signal
	private Signal<ListItemFactory> _signal;

	// Callback
	private Gio.Internal.AsyncReadyCallback _saveCallback { get; set; }
	private Gio.Internal.AsyncReadyCallback _openCallback { get; set; }
	private Gio.Internal.AsyncReadyCallback _selectFolderCallback { get; set; }
	private Gio.Internal.AsyncReadyCallback _exceptionCallback { get; set; }

	#endregion

	public MainWindow(Application app)
	{
		// Main Window
		SetDefaultSize(500, 300); // Sets the default size of the Window
		Title = ConfigUtils.PROGRAM_NAME; // Sets the title to the name of the program, which is "VG Music Studio"
		_app = app;

		// Configures SetVolumeScale method with the MixerVolumeChanged Event action
		Mixer.VolumeChanged += SetVolumeScale;

		// LibAdwaita Header Bar
		_headerBar = Adw.HeaderBar.New();
		_headerBar.SetShowEndTitleButtons(true);

		// Main Menu
		_mainMenu = Gio.Menu.New();

		// Popover Menu Bar
		_popoverMenuBar = PopoverMenuBar.NewFromModel(_mainMenu); // This will ensure that the menu model is used inside of the PopoverMenuBar widget
		_popoverMenuBar.MenuModel = _mainMenu;
		_popoverMenuBar.MnemonicActivate(true);

		// File Menu
		_fileMenu = Gio.Menu.New();

		_fileLabel = Label.NewWithMnemonic(Strings.MenuFile);
		_fileLabel.GetMnemonicKeyval();
		_fileLabel.SetUseUnderline(true);
		_fileItem = Gio.MenuItem.New(_fileLabel.GetLabel(), null);
		_fileLabel.SetMnemonicWidget(_popoverMenuBar);
		_popoverMenuBar.AddMnemonicLabel(_fileLabel);
		_fileItem.SetSubmenu(_fileMenu);

		_openDSEItem = Gio.MenuItem.New(Strings.MenuOpenDSE, "app.openDSE");
		_openDSEAction = Gio.SimpleAction.New("openDSE", null);
		_openDSEItem.SetActionAndTargetValue("app.openDSE", null);
		_app.AddAction(_openDSEAction);
		_openDSEAction.OnActivate += OpenDSE;
		_fileMenu.AppendItem(_openDSEItem);
		_openDSEItem.Unref();

		_openSDATItem = Gio.MenuItem.New(Strings.MenuOpenSDAT, "app.openSDAT");
		_openSDATAction = Gio.SimpleAction.New("openSDAT", null);
		_openSDATItem.SetActionAndTargetValue("app.openSDAT", null);
		_app.AddAction(_openSDATAction);
		_openSDATAction.OnActivate += OpenSDAT;
		_fileMenu.AppendItem(_openSDATItem);
		_openSDATItem.Unref();

		_openAlphaDreamItem = Gio.MenuItem.New(Strings.MenuOpenAlphaDream, "app.openAlphaDream");
		_openAlphaDreamAction = Gio.SimpleAction.New("openAlphaDream", null);
		_app.AddAction(_openAlphaDreamAction);
		_openAlphaDreamAction.OnActivate += OpenAlphaDream;
		_fileMenu.AppendItem(_openAlphaDreamItem);
		_openAlphaDreamItem.Unref();

		_openMP2KItem = Gio.MenuItem.New(Strings.MenuOpenMP2K, "app.openMP2K");
		_openMP2KAction = Gio.SimpleAction.New("openMP2K", null);
		_app.AddAction(_openMP2KAction);
		_openMP2KAction.OnActivate += OpenMP2K;
		_fileMenu.AppendItem(_openMP2KItem);
		_openMP2KItem.Unref();

		_mainMenu.AppendItem(_fileItem); // Note: It must append the menu item variable (_fileItem), not the file menu variable (_fileMenu) itself
		_fileItem.Unref();

		// Data Menu
		_dataMenu = Gio.Menu.New();

		_dataLabel = Label.NewWithMnemonic(Strings.MenuData);
		_dataLabel.GetMnemonicKeyval();
		_dataLabel.SetUseUnderline(true);
		_dataItem = Gio.MenuItem.New(_dataLabel.GetLabel(), null);
		_popoverMenuBar.AddMnemonicLabel(_dataLabel);
		_dataItem.SetSubmenu(_dataMenu);

		_exportDLSItem = Gio.MenuItem.New(Strings.MenuSaveDLS, "app.exportDLS");
		_exportDLSAction = Gio.SimpleAction.New("exportDLS", null);
		_app.AddAction(_exportDLSAction);
		_exportDLSAction.Enabled = false;
		_exportDLSAction.OnActivate += ExportDLS;
		_dataMenu.AppendItem(_exportDLSItem);
		_exportDLSItem.Unref();

		_exportSF2Item = Gio.MenuItem.New(Strings.MenuSaveSF2, "app.exportSF2");
		_exportSF2Action = Gio.SimpleAction.New("exportSF2", null);
		_app.AddAction(_exportSF2Action);
		_exportSF2Action.Enabled = false;
		_exportSF2Action.OnActivate += ExportSF2;
		_dataMenu.AppendItem(_exportSF2Item);
		_exportSF2Item.Unref();

		_exportMIDIItem = Gio.MenuItem.New(Strings.MenuSaveMIDI, "app.exportMIDI");
		_exportMIDIAction = Gio.SimpleAction.New("exportMIDI", null);
		_app.AddAction(_exportMIDIAction);
		_exportMIDIAction.Enabled = false;
		_exportMIDIAction.OnActivate += ExportMIDI;
		_dataMenu.AppendItem(_exportMIDIItem);
		_exportMIDIItem.Unref();

		_exportWAVItem = Gio.MenuItem.New(Strings.MenuSaveWAV, "app.exportWAV");
		_exportWAVAction = Gio.SimpleAction.New("exportWAV", null);
		_app.AddAction(_exportWAVAction);
		_exportWAVAction.Enabled = false;
		_exportWAVAction.OnActivate += ExportWAV;
		_dataMenu.AppendItem(_exportWAVItem);
		_exportWAVItem.Unref();

		_mainMenu.AppendItem(_dataItem);
		_dataItem.Unref();

		// Playlist Menu
		_playlistMenu = Gio.Menu.New();

		_playlistLabel = Label.NewWithMnemonic(Strings.MenuPlaylist);
		_playlistLabel.GetMnemonicKeyval();
		_playlistLabel.SetUseUnderline(true);
		_playlistItem = Gio.MenuItem.New(_playlistLabel.GetLabel(), null);
		_popoverMenuBar.AddMnemonicLabel(_playlistLabel);
		_playlistItem.SetSubmenu(_playlistMenu);

		_endPlaylistItem = Gio.MenuItem.New(Strings.MenuEndPlaylist, "app.endPlaylist");
		_endPlaylistAction = Gio.SimpleAction.New("endPlaylist", null);
		_app.AddAction(_endPlaylistAction);
		_endPlaylistAction.Enabled = false;
		_endPlaylistAction.OnActivate += EndCurrentPlaylist;
		_playlistMenu.AppendItem(_endPlaylistItem);
		_endPlaylistItem.Unref();

		_mainMenu.AppendItem(_playlistItem);
		_playlistItem.Unref();

		// Buttons
		_buttonPlay = new Button() { Sensitive = false, Label = Strings.PlayerPlay };
		_buttonPlay.OnClicked += (o, e) => Play();
		_buttonPause = new Button() { Sensitive = false, Label = Strings.PlayerPause };
		_buttonPause.OnClicked += (o, e) => Pause();
		_buttonStop = new Button() { Sensitive = false, Label = Strings.PlayerStop };
		_buttonStop.OnClicked += (o, e) => Stop();

		// Spin Button
		_sequenceNumberAdjustment = Adjustment.New(0, 0, -1, 1, 1, 1);
		_sequenceNumberSpinButton = SpinButton.New(_sequenceNumberAdjustment, 1, 0);
		_sequenceNumberSpinButton.Sensitive = false;
		_sequenceNumberSpinButton.Value = 0;
		//_sequenceNumberSpinButton.Visible = false;
		_sequenceNumberSpinButton.OnValueChanged += SequenceNumberSpinButton_ValueChanged;

		// Timer
		_timer = new Timer();
		_timer.Elapsed += Timer_Tick;

		// Volume Scale
		_volumeAdjustment = Adjustment.New(0, 0, 100, 1, 1, 1);
		_volumeScale = Scale.New(Orientation.Horizontal, _volumeAdjustment);
		_volumeScale.Sensitive = false;
		_volumeScale.ShowFillLevel = true;
		_volumeScale.DrawValue = false;
		_volumeScale.WidthRequest = 250;
		//_volumeScale.OnValueChanged += VolumeScale_ValueChanged;

		// Position Scale
		_positionScale = Scale.NewWithRange(Orientation.Horizontal, 0, 1, 1); // The Upper value property must contain a value of 1 or higher for the widget to show upon startup
		_positionScale.Sensitive = false;
		_positionScale.ShowFillLevel = true;
		_positionScale.DrawValue = false;
		_positionScale.WidthRequest = 250;
		_positionScale.RestrictToFillLevel = false;
		//_positionScale.SetRange(0, double.MaxValue);
		_positionGestureClick = GestureClick.New();
		//if (_positionGestureClick.Button == 1)
  //      {
  //          _positionScale.OnValueChanged += PositionScale_MouseButtonRelease;
  //          _positionScale.OnValueChanged += PositionScale_MouseButtonPress;
  //      }
		//_positionScale.Focusable = true;
		//_positionScale.HasOrigin = true;
		//_positionScale.Visible = true;
		//_positionScale.FillLevel = _positionAdjustment.Upper;
		//_positionGestureClick.OnReleased += PositionScale_MouseButtonRelease; // ButtonRelease must go first, otherwise the scale it will follow the mouse cursor upon loading
		//_positionGestureClick.OnPressed += PositionScale_MouseButtonPress;

		// Sound Sequence List
		//_soundSequenceList = new SoundSequenceList { Sensitive = false };
		//_soundSequenceFactory = SignalListItemFactory.New();
		//_soundSequenceListBox = ListBox.New();
		//_soundSequenceDropDown = DropDown.New(Gio.ListStore.New(DropDown.GetGType()), new ConstantExpression(IntPtr.Zero));
		//_soundSequenceDropDown.OnActivate += SequencesListView_SelectionGet;
		//_soundSequenceDropDown.ListFactory = _soundSequenceFactory;
		//_soundSequenceAction = Gio.SimpleAction.New("soundSequenceList", null);
		//_soundSequenceAction.OnActivate += SequencesListView_SelectionGet;

		// Main display
		_mainBox = Box.New(Orientation.Vertical, 4);
		_configButtonBox = Box.New(Orientation.Horizontal, 2);
		_configButtonBox.Halign = Align.Center;
		_configPlayerButtonBox = Box.New(Orientation.Horizontal, 3);
		_configPlayerButtonBox.Halign = Align.Center;
		_configSpinButtonBox = Box.New(Orientation.Horizontal, 1);
		_configSpinButtonBox.Halign = Align.Center;
		_configSpinButtonBox.WidthRequest = 100;
		_configScaleBox = Box.New(Orientation.Horizontal, 2);
		_configScaleBox.Halign = Align.Center;

		_configPlayerButtonBox.MarginStart = 40;
		_configPlayerButtonBox.MarginEnd = 40;
		_configButtonBox.Append(_configPlayerButtonBox);
		_configSpinButtonBox.MarginStart = 100;
		_configSpinButtonBox.MarginEnd = 100;
		_configButtonBox.Append(_configSpinButtonBox);

		_configPlayerButtonBox.Append(_buttonPlay);
		_configPlayerButtonBox.Append(_buttonPause);
		_configPlayerButtonBox.Append(_buttonStop);

		if (_configSpinButtonBox.GetFirstChild() == null)
		{
			_sequenceNumberSpinButton.Hide();
			_configSpinButtonBox.Append(_sequenceNumberSpinButton);
		}
		
		_volumeScale.MarginStart = 20;
		_volumeScale.MarginEnd = 20;
		_configScaleBox.Append(_volumeScale);
		_positionScale.MarginStart = 20;
		_positionScale.MarginEnd = 20;
		_configScaleBox.Append(_positionScale);

		_mainBox.Append(_headerBar);
		_mainBox.Append(_popoverMenuBar);
		_mainBox.Append(_configButtonBox);
		_mainBox.Append(_configScaleBox);
		//_mainBox.Append(_soundSequenceListBox);

		SetContent(_mainBox);

		Show();

		// Ensures the entire application gets closed when the main window is closed
		OnCloseRequest += (sender, args) =>
		{
			DisposeEngine(); // Engine must be disposed first, otherwise the window will softlock when closing
			_app.Quit();
			return true;
		};
	}

	// When the value is changed on the volume scale
	private void VolumeScale_ValueChanged(object sender, EventArgs e)
	{
		Engine.Instance!.Mixer.SetVolume((float)(_volumeScale.Adjustment!.Value / _volumeAdjustment.Upper));
	}

	// Sets the volume scale to the specified position
	public void SetVolumeScale(float volume)
	{
		_volumeScale.OnValueChanged -= VolumeScale_ValueChanged;
		_volumeScale.Adjustment!.Value = (int)(volume * _volumeAdjustment.Upper);
		_volumeScale.OnValueChanged += VolumeScale_ValueChanged;
	}

	private bool _positionScaleFree = true;
	private void PositionScale_MouseButtonRelease(object sender, EventArgs args)
	{
		if (_positionGestureClick.Button == 1) // Number 1 is Left Mouse Button
		{
			Engine.Instance!.Player.SetSongPosition((long)_positionScale.GetValue()); // Sets the value based on the position when mouse button is released
			_positionScaleFree = true; // Sets _positionScaleFree to true when mouse button is released
			LetUIKnowPlayerIsPlaying(); // This method will run the void that tells the UI that the player is playing a track
		}
	}
	private void PositionScale_MouseButtonPress(object sender, EventArgs args)
	{
		if (_positionGestureClick.Button == 1) // Number 1 is Left Mouse Button
		{
			_positionScaleFree = false;
		}
	}

	private void SequenceNumberSpinButton_ValueChanged(object sender, EventArgs e)
	{
		//_sequencesGestureClick.OnBegin -= SequencesListView_SelectionGet;
		//_signal.Connect(_sequencesListFactory, SequencesListView_SelectionGet, false, null);

		int index = (int)_sequenceNumberAdjustment.Value;
		Stop();
		this.Title = ConfigUtils.PROGRAM_NAME;
		//_sequencesListView.Margin = 0;
		//_songInfo.Reset();
		bool success;
		try
		{
			if (Engine.Instance == null)
			{
				return; // Prevents referencing a null Engine.Instance when the engine is being disposed, especially while main window is being closed
			}
			Engine.Instance!.Player.LoadSong(index);
			success = Engine.Instance.Player.LoadedSong is not null; // TODO: Make sure loadedsong is null when there are no tracks (for each engine, only mp2k guarantees it rn)
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, string.Format(Strings.ErrorLoadSong, Engine.Instance!.Config.GetSongName(index)));
			success = false;
		}

		//_trackViewer?.UpdateTracks();
		if (success)
		{
			Config config = Engine.Instance.Config;
			List<Config.Song> songs = config.Playlists[0].Songs; // Complete "Music" playlist is present in all configs at index 0
			int songIndex = songs.FindIndex(s => s.Index == index);
			if (songIndex != -1)
			{
				this.Title = $"{ConfigUtils.PROGRAM_NAME} - {songs[songIndex].Name}"; // TODO: Make this a func
				//_sequencesColumnView.SortColumnId = songs.IndexOf(song) + 1; // + 1 because the "Music" playlist is first in the combobox
			}
			//_positionScale.Adjustment!.Upper = double.MaxValue;
			_duration = (int)(Engine.Instance!.Player.LoadedSong!.MaxTicks + 0.5);
			_positionScale.SetRange(0, _duration);
			//_positionAdjustment.LargeChange = (long)(_positionAdjustment.Upper / 10) >> 64;
			//_positionAdjustment.SmallChange = (long)(_positionAdjustment.LargeChange / 4) >> 64;
			_positionScale.Show();
			//_songInfo.SetNumTracks(Engine.Instance.Player.LoadedSong.Events.Length);
			if (_autoplay)
			{
				Play();
			}
		}
		else
		{
			//_songInfo.SetNumTracks(0);
		}
		_positionScale.Sensitive = _exportWAVAction.Enabled = success;
		_exportMIDIAction.Enabled = success && MP2KEngine.MP2KInstance is not null;
		_exportDLSAction.Enabled = _exportSF2Action.Enabled = success && AlphaDreamEngine.AlphaDreamInstance is not null;

		_autoplay = true;
		//_sequencesGestureClick.OnEnd += SequencesListView_SelectionGet;
		//_signal.Connect(_sequencesListFactory, SequencesListView_SelectionGet, true, null);
	}
	//private void SequencesListView_SelectionGet(object sender, EventArgs e)
	//{
	//	var item = _soundSequenceList.SelectedItem;
	//	if (item is Config.Song song)
	//	{
	//		SetAndLoadSequence(song.Index);
	//	}
	//	else if (item is Config.Playlist playlist)
	//	{
	//		if (playlist.Songs.Count > 0
	//		&& FlexibleMessageBox.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), Strings.MenuPlaylist, ButtonsType.YesNo) == ResponseType.Yes)
	//		{
	//			ResetPlaylistStuff(false);
	//			_curPlaylist = playlist;
	//			Engine.Instance.Player.ShouldFadeOut = _playlistPlaying = true;
	//			Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
	//			_endPlaylistAction.Enabled = true;
	//			SetAndLoadNextPlaylistSong();
	//		}
	//	}
	//}
	public void SetAndLoadSequence(int index)
	{
		_curSong = index;
		if (_sequenceNumberSpinButton.Value == index)
		{
			SequenceNumberSpinButton_ValueChanged(null, null);
		}
		else
		{
			_sequenceNumberSpinButton.Value = index;
		}
	}

	//private void SetAndLoadNextPlaylistSong()
	//{
	//	if (_remainingSequences.Count == 0)
	//	{
	//		_remainingSequences.AddRange(_curPlaylist.Songs.Select(s => s.Index));
	//		if (GlobalConfig.Instance.PlaylistMode == PlaylistMode.Random)
	//		{
	//			_remainingSequences.Any();
	//		}
	//	}
	//	long nextSequence = _remainingSequences[0];
	//	_remainingSequences.RemoveAt(0);
	//	SetAndLoadSequence(nextSequence);
	//}
	private void ResetPlaylistStuff(bool spinButtonAndListBoxEnabled)
	{
		if (Engine.Instance != null)
		{
			Engine.Instance.Player.ShouldFadeOut = false;
		}
		_curSong = -1;
		_endPlaylistAction.Enabled = false;
		_sequenceNumberSpinButton.Sensitive = /* _soundSequenceListBox.Sensitive = */ spinButtonAndListBoxEnabled;
	}
	private void EndCurrentPlaylist(object sender, EventArgs e)
	{
		if (FlexibleMessageBox.Show(Strings.EndPlaylistBody, Strings.MenuPlaylist, ButtonsType.YesNo) == ResponseType.Yes)
		{
			ResetPlaylistStuff(true);
		}
	}

	private void OpenDSE(Gio.SimpleAction sender, EventArgs e)
	{
		if (Gtk.Functions.GetMinorVersion() <= 8) // There's a bug in Gtk 4.09 and later that has broken FileChooserNative functionality, causing icons and thumbnails to appear broken
		{
			// To allow the dialog to display in native windowing format, FileChooserNative is used instead of FileChooserDialog
			var d = FileChooserNative.New(
				Strings.MenuOpenDSE, // The title shown in the folder select dialog window
				this, // The parent of the dialog window, is the MainWindow itself
				FileChooserAction.SelectFolder, // To ensure it becomes a folder select dialog window, SelectFolder is used as the FileChooserAction
				"Select Folder", // Followed by the accept
				"Cancel");       // and cancel button names.

			d.SetModal(true);

			// Note: Blocking APIs were removed in GTK4, which means the code will proceed to run and return to the main loop, even when a dialog is displayed.
			// Instead, it's handled by the OnResponse event function when it re-enters upon selection.
			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept) // In GTK4, the 'Gtk.FileChooserNative.Action' property is used for determining the button selection on the dialog. The 'Gtk.Dialog.Run' method was removed in GTK4, due to it being a non-GUI function and going against GTK's main objectives.
				{
					d.Unref();
					return;
				}
				var path = d.GetCurrentFolder()!.GetPath() ?? "";
				d.GetData(path);
				OpenDSEFinish(path);
				d.Unref(); // Ensures disposal of the dialog when closed
				return;
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuOpenDSE);

			_selectFolderCallback = (source, res, data) =>
			{
				var folderHandle = Gtk.Internal.FileDialog.SelectFolderFinish(d.Handle, res, out ErrorHandle);
				if (folderHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(folderHandle).DangerousGetHandle());
					OpenDSEFinish(path!);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.SelectFolder(d.Handle, Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero); // SelectFolder, Open and Save methods are currently missing from GirCore, but are available in the Gtk.Internal namespace, so we're using this until GirCore updates with the method bindings. See here: https://github.com/gircore/gir.core/issues/900
			//d.SelectFolder(Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero);
		}
	}
	private void OpenDSEFinish(string path)
	{
		DisposeEngine();
		try
		{
			_ = new DSEEngine(path);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorOpenDSE);
			return;
		}
		DSEConfig config = DSEEngine.DSEInstance!.Config;
		FinishLoading(config.BGMFiles.Length);
		_sequenceNumberSpinButton.Visible = false;
		_sequenceNumberSpinButton.Hide();
		_mainMenu.AppendItem(_playlistItem);
		_exportDLSAction.Enabled = false;
		_exportMIDIAction.Enabled = false;
		_exportSF2Action.Enabled = false;
	}
	private void OpenSDAT(Gio.SimpleAction sender, EventArgs e)
	{
		var filterSDAT = FileFilter.New();
		filterSDAT.SetName(Strings.GTKFilterOpenSDAT);
		filterSDAT.AddPattern("*.sdat");
		var allFiles = FileFilter.New();
		allFiles.SetName(Strings.GTKAllFiles);
		allFiles.AddPattern("*.*");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
				Strings.MenuOpenSDAT,
				this,
				FileChooserAction.Open,
				"Open",
				"Cancel");

			d.SetModal(true);

			d.AddFilter(filterSDAT);
			d.AddFilter(allFiles);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}

				var path = d.GetFile()!.GetPath() ?? "";
				d.GetData(path);
				OpenSDATFinish(path);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuOpenSDAT);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(filterSDAT);
			filters.Append(allFiles);
			d.SetFilters(filters);
			_openCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.OpenFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					OpenSDATFinish(path!);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Open(d.Handle, Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
			//d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		}
	}
	private void OpenSDATFinish(string path)
	{
		DisposeEngine();
		try
		{
			using (FileStream stream = File.OpenRead(path))
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
		_sequenceNumberSpinButton.Visible = true;
		_sequenceNumberSpinButton.Show();
		_exportDLSAction.Enabled = false;
		_exportMIDIAction.Enabled = false;
		_exportSF2Action.Enabled = false;
	}
	private void OpenAlphaDream(Gio.SimpleAction sender, EventArgs e)
	{
		var filterGBA = FileFilter.New();
		filterGBA.SetName(Strings.GTKFilterOpenGBA);
		filterGBA.AddPattern("*.gba");
		filterGBA.AddPattern("*.srl");
		var allFiles = FileFilter.New();
		allFiles.SetName(Name = Strings.GTKAllFiles);
		allFiles.AddPattern("*.*");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
				Strings.MenuOpenAlphaDream,
				this,
				FileChooserAction.Open,
				"Open",
				"Cancel");
			d.SetModal(true);

			d.AddFilter(filterGBA);
			d.AddFilter(allFiles);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}
				var path = d.GetFile()!.GetPath() ?? "";
				d.GetData(path);
				OpenAlphaDreamFinish(path);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuOpenAlphaDream);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(filterGBA);
			filters.Append(allFiles);
			d.SetFilters(filters);
			_openCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.OpenFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					OpenAlphaDreamFinish(path!);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Open(d.Handle, Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
			//d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		}
	}
	private void OpenAlphaDreamFinish(string path)
	{
		DisposeEngine();
		try
		{
			_ = new AlphaDreamEngine(File.ReadAllBytes(path));
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorOpenAlphaDream);
			return;
		}

		AlphaDreamConfig config = AlphaDreamEngine.AlphaDreamInstance!.Config;
		FinishLoading(config.SongTableSizes[0]);
		_sequenceNumberSpinButton.Visible = true;
		_sequenceNumberSpinButton.Show();
		_mainMenu.AppendItem(_dataItem);
		_mainMenu.AppendItem(_playlistItem);
		_exportDLSAction.Enabled = true;
		_exportMIDIAction.Enabled = false;
		_exportSF2Action.Enabled = true;
	}
	private void OpenMP2K(Gio.SimpleAction sender, EventArgs e)
	{
		FileFilter filterGBA = FileFilter.New();
		filterGBA.SetName(Strings.GTKFilterOpenGBA);
		filterGBA.AddPattern("*.gba");
		filterGBA.AddPattern("*.srl");
		FileFilter allFiles = FileFilter.New();
		allFiles.SetName(Strings.GTKAllFiles);
		allFiles.AddPattern("*.*");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
				Strings.MenuOpenMP2K,
				this,
				FileChooserAction.Open,
				"Open",
				"Cancel");


			d.AddFilter(filterGBA);
			d.AddFilter(allFiles);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}
				var path = d.GetFile()!.GetPath() ?? "";
				OpenMP2KFinish(path);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuOpenMP2K);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(filterGBA);
			filters.Append(allFiles);
			d.SetFilters(filters);
			_openCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.OpenFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					OpenMP2KFinish(path!);
					filterGBA.Unref();
					allFiles.Unref();
					filters.Unref();
					GObject.Internal.Object.Unref(fileHandle);
					d.Unref();
					return;
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Open(d.Handle, Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
			//d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		}
	}
	private void OpenMP2KFinish(string path)
	{
		if (Engine.Instance is not null)
		{
			DisposeEngine();
		}
		
		if (IsWindows())
		{
			try
			{
				_ = new MP2KEngine(File.ReadAllBytes(path));
			}
			catch (Exception ex)
			{
				//_dialog = Adw.MessageDialog.New(this, Strings.ErrorOpenMP2K, ex.ToString());
				//FlexibleMessageBox.Show(ex, Strings.ErrorOpenMP2K);
				DisposeEngine();
				ExceptionDialog(ex, Strings.ErrorOpenMP2K);
				return;
			}
		}
		else
		{
			var ex = new PlatformNotSupportedException();
			ExceptionDialog(ex, Strings.ErrorOpenMP2K);
			return;
		}

		MP2KConfig config = MP2KEngine.MP2KInstance!.Config;
		FinishLoading(config.SongTableSizes[0]);
		_sequenceNumberSpinButton.Visible = true;
		_sequenceNumberSpinButton.Show();
		//_mainMenu.AppendItem(_dataItem);
		//_mainMenu.AppendItem(_playlistItem);
		_exportDLSAction.Enabled = false;
		_exportMIDIAction.Enabled = true;
		_exportSF2Action.Enabled = false;
	}
	private void ExportDLS(Gio.SimpleAction sender, EventArgs e)
	{
		AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;

		FileFilter ff = FileFilter.New();
		ff.SetName(Strings.GTKFilterSaveDLS);
		ff.AddPattern("*.dls");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
				Strings.MenuSaveDLS,
				this,
				FileChooserAction.Save,
				"Save",
				"Cancel");
			d.SetCurrentName(cfg.GetGameName());
			d.AddFilter(ff);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}

				var path = d.GetFile()!.GetPath() ?? "";
				ExportDLSFinish(cfg, path);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuSaveDLS);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(ff);
			d.SetFilters(filters);
			_saveCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.SaveFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					ExportDLSFinish(cfg, path!);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Save(d.Handle, Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
			//d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
		}
	}
	private void ExportDLSFinish(AlphaDreamConfig config, string path)
	{
		try
		{
			AlphaDreamSoundFontSaver_DLS.Save(config, path);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveDLS, path), Strings.SuccessSaveDLS);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveDLS);
		}
	}
	private void ExportMIDI(Gio.SimpleAction sender, EventArgs e)
	{
		FileFilter ff = FileFilter.New();
		ff.SetName(Strings.GTKFilterSaveMIDI);
		ff.AddPattern("*.mid");
		ff.AddPattern("*.midi");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
				Strings.MenuSaveMIDI,
				this,
				FileChooserAction.Save,
				"Save",
				"Cancel");
			d.SetCurrentName(Engine.Instance!.Config.GetSongName((int)_sequenceNumberSpinButton.Value));
			d.AddFilter(ff);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}

				var path = d.GetFile()!.GetPath() ?? "";
				ExportMIDIFinish(path);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuSaveMIDI);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(ff);
			d.SetFilters(filters);
			_saveCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.SaveFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					ExportMIDIFinish(path!);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Save(d.Handle, Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
			//d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
		}
	}
	private void ExportMIDIFinish(string path)
	{
		MP2KPlayer p = MP2KEngine.MP2KInstance!.Player;
		var args = new MIDISaveArgs(true, false, new (int AbsoluteTick, (byte Numerator, byte Denominator))[]
		{
			(0, (4, 4)),
		});

		try
		{
			p.SaveAsMIDI(path, args);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveMIDI, path), Strings.SuccessSaveMIDI);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveMIDI);
		}
	}
	private void ExportSF2(Gio.SimpleAction sender, EventArgs e)
	{
		AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;
		
		FileFilter ff = FileFilter.New();
		ff.SetName(Strings.GTKFilterSaveSF2);
		ff.AddPattern("*.sf2");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
				Strings.MenuSaveSF2,
				this,
				FileChooserAction.Save,
				"Save",
				"Cancel");

			d.SetCurrentName(cfg.GetGameName());
			d.AddFilter(ff);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}

				var path = d.GetFile()!.GetPath() ?? "";
				ExportSF2Finish(path, cfg);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuSaveSF2);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(ff);
			d.SetFilters(filters);
			_saveCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.SaveFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					ExportSF2Finish(path!, cfg);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Save(d.Handle, Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
			//d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
		}
	}
	private void ExportSF2Finish(string path, AlphaDreamConfig config)
	{
		try
		{
			AlphaDreamSoundFontSaver_SF2.Save(path, config);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveSF2, path), Strings.SuccessSaveSF2);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveSF2);
		}
	}
	private void ExportWAV(Gio.SimpleAction sender, EventArgs e)
	{
		FileFilter ff = FileFilter.New();
		ff.SetName(Strings.GTKFilterSaveWAV);
		ff.AddPattern("*.wav");

		if (Gtk.Functions.GetMinorVersion() <= 8)
		{
			var d = FileChooserNative.New(
			Strings.MenuSaveWAV,
			this,
			FileChooserAction.Save,
			"Save",
			"Cancel");

			d.SetCurrentName(Engine.Instance!.Config.GetSongName((int)_sequenceNumberSpinButton.Value));
			d.AddFilter(ff);

			d.OnResponse += (sender, e) =>
			{
				if (e.ResponseId != (int)ResponseType.Accept)
				{
					d.Unref();
					return;
				}

				var path = d.GetFile()!.GetPath() ?? "";
				ExportWAVFinish(path);
				d.Unref();
			};
			d.Show();
		}
		else
		{
			var d = FileDialog.New();
			d.SetTitle(Strings.MenuSaveWAV);
			var filters = Gio.ListStore.New(FileFilter.GetGType());
			filters.Append(ff);
			d.SetFilters(filters);
			_saveCallback = (source, res, data) =>
			{
				var fileHandle = Gtk.Internal.FileDialog.SaveFinish(d.Handle, res, out ErrorHandle);
				if (fileHandle != IntPtr.Zero)
				{
					var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
					ExportWAVFinish(path!);
					d.Unref();
				}
				d.Unref();
			};
			Gtk.Internal.FileDialog.Save(d.Handle, Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
			//d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
		}
	}
	private void ExportWAVFinish(string path)
	{
		Stop();

		Player player = Engine.Instance.Player;
		bool oldFade = player.ShouldFadeOut;
		long oldLoops = player.NumLoops;
		player.ShouldFadeOut = true;
		player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;

		try
		{
			player.Record(path);
			FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveWAV, path), Strings.SuccessSaveWAV);
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorSaveWAV);
		}

		player.ShouldFadeOut = oldFade;
		player.NumLoops = oldLoops;
		_stopUI = false;
	}

	public void ExceptionDialog(Exception error, string heading)
	{
		Debug.WriteLine(error.Message);
		var md = Adw.MessageDialog.New(this, heading, error.Message);
		md.SetModal(true);
		md.AddResponse("ok", ("_OK"));
		md.SetResponseAppearance("ok", ResponseAppearance.Default);
		md.SetDefaultResponse("ok");
		md.SetCloseResponse("ok");
		_exceptionCallback = (source, res, data) =>
		{
			md.Destroy();
		};
		md.Activate();
		md.Show();
	}

	public void LetUIKnowPlayerIsPlaying()
	{
		// Prevents method from being used if timer is already active
		if (_timer.Enabled)
		{
			return;
		}

		// Ensures a GlobalConfig Instance is created if one doesn't exist
		if (GlobalConfig.Instance == null)
		{
			GlobalConfig.Init(); // A new instance needs to be initialized before it can do anything
		}

		// Configures the buttons when player is playing a sequenced track
		_buttonPause.Sensitive = _buttonStop.Sensitive = true; // Setting the 'Sensitive' property to 'true' enables the buttons, allowing you to click on them
		_buttonPause.Label = Strings.PlayerPause;
		_timer.Interval = (int)(1_000.0 / GlobalConfig.Instance!.RefreshRate);
		_timer.Start();
		Show();
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
			_buttonPause.Label = Strings.PlayerUnpause;
			_timer.Stop();
		}
		else
		{
			_buttonPause.Label = Strings.PlayerPause;
			_timer.Start();
		}
	}
	private void Stop()
	{
		if (Engine.Instance == null)
		{
			return; // This is here to ensure that it returns if the Engine.Instance is null while closing the main window
		}
		Engine.Instance!.Player.Stop();
		_buttonPause.Sensitive = _buttonStop.Sensitive = false;
		_buttonPause.Label = Strings.PlayerPause;
		_timer.Stop();
		UpdatePositionIndicators(0L);
		Show();
	}
	private void TogglePlayback()
	{
		switch (Engine.Instance!.Player.State)
		{
			case PlayerState.Stopped: Play(); break;
			case PlayerState.Paused:
			case PlayerState.Playing: Pause(); break;
		}
	}
	private void PlayPreviousSequence()
	{

		if (_playlist is not null)
		{
			_playlist.UndoThenSetAndLoadPrevSong(this, _curSong);
		}
		else
		{
			SetAndLoadSequence((int)_sequenceNumberSpinButton.Value - 1);
		}
	}
	private void PlayNextSong(object? sender, EventArgs? e)
	{
		if (_playlist is not null)
		{
			_playlist.AdvanceThenSetAndLoadNextSong(this, _curSong);
		}
		else
		{
			SetAndLoadSequence((int)_sequenceNumberSpinButton.Value + 1);
		}
	}

	private void FinishLoading(long numSongs)
	{
		Engine.Instance!.Player.SongEnded += SongEnded;
		//foreach (Config.Playlist playlist in Engine.Instance.Config.Playlists)
		//{
		//	_soundSequenceListBox.Insert(Label.New(playlist.Name), playlist.Songs.Count);
		//	_soundSequenceList.Add(new SoundSequenceListItem(playlist));
		//	_soundSequenceList.AddRange(playlist.Songs.Select(s => new SoundSequenceListItem(s)).ToArray());
		//}
		_sequenceNumberAdjustment.Upper = numSongs - 1;
#if DEBUG
		// [Debug methods specific to this GUI will go in here]
#endif
		_autoplay = false;
		SetAndLoadSequence(Engine.Instance.Config.Playlists[0].Songs.Count == 0 ? 0 : Engine.Instance.Config.Playlists[0].Songs[0].Index);
		_sequenceNumberSpinButton.Sensitive = _buttonPlay.Sensitive = _volumeScale.Sensitive = true;
		Show();
	}
	private void DisposeEngine()
	{
		if (Engine.Instance is not null)
		{
			Stop();
			Engine.Instance.Dispose();
		}

		//_trackViewer?.UpdateTracks();
		Name = ConfigUtils.PROGRAM_NAME;
		//_songInfo.SetNumTracks(0);
		//_songInfo.ResetMutes();
		ResetPlaylistStuff(false);
		UpdatePositionIndicators(0L);
		//_signal.Connect(_sequencesListFactory, SequencesListView_SelectionGet, false, null);
		_sequenceNumberAdjustment.OnValueChanged -= SequenceNumberSpinButton_ValueChanged;
		_sequenceNumberSpinButton.Visible = false;
		_sequenceNumberSpinButton.Value = _sequenceNumberAdjustment.Upper = 0;
		//_sequencesListView.Selection.SelectFunction = null;
		//_sequencesColumnView.Unref();
		//_signal.Connect(_sequencesListFactory, SequencesListView_SelectionGet, true, null);
		_sequenceNumberSpinButton.OnValueChanged += SequenceNumberSpinButton_ValueChanged;
	}

	private void Timer_Tick(object? sender, EventArgs e)
	{
		if (_songEnded)
		{
			_songEnded = false;
			if (_playlist is not null)
			{
				_playlist.AdvanceThenSetAndLoadNextSong(this, _curSong);
			}
			else
			{
				Stop();
			}
		}
		else
		{
			Player player = Engine.Instance!.Player;
			UpdatePositionIndicators(player.ElapsedTicks);
		}
	}
	private void SongEnded()
	{
		_stopUI = true;
	}

	// This updates _positionScale and _positionAdjustment to the value specified
	// Note: Gtk.Scale is dependent on Gtk.Adjustment, which is why _positionAdjustment is used instead
	private void UpdatePositionIndicators(long ticks)
	{
		if (_positionScaleFree)
		{
			if (ticks < _duration)
			{
				// TODO: Implement GStreamer functions to replace Gtk.Adjustment
				_positionScale.SetRange(0, _duration);
				//_positionScale.SetValue(ticks); // A Gtk.Adjustment field must be used here to avoid issues
			}
			else
			{
				return;
			}
		}
	}
}
