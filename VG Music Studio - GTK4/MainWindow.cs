using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA.AlphaDream;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Adw;
using Gtk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;

using Application = Adw.Application;
using Window = Adw.Window;
using GObject;
using Kermalis.VGMusicStudio.GTK4.Util;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Kermalis.VGMusicStudio.GTK4
{
    internal sealed class MainWindow : Window
    {
        [DllImport("libgobject-2.0.so.0", EntryPoint = "g_object_unref")]
        private static extern void LinuxUnref(nint obj);

        [DllImport("libgobject-2.0.0.dylib", EntryPoint = "g_object_unref")]
        private static extern void MacOSUnref(nint obj);

        [DllImport("libgobject-2.0-0.dll", EntryPoint = "g_object_unref")]
        private static extern void WindowsUnref(nint obj);

        [DllImport("libgio-2.0.so.0", EntryPoint = "g_file_get_path")]
        private static extern string LinuxGetPath(nint file);

        [DllImport("libgio-2.0.0.dylib", EntryPoint = "g_file_get_path")]
        private static extern string MacOSGetPath(nint file);

        [DllImport("libgio-2.0-0.dll", EntryPoint = "g_file_get_path")]
        private static extern string WindowsGetPath(nint file);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_css_provider_load_from_data")]
        private static extern void LinuxLoadFromData(nint provider, string data, int length);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_css_provider_load_from_data")]
        private static extern void MacOSLoadFromData(nint provider, string data, int length);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_css_provider_load_from_data")]
        private static extern void WindowsLoadFromData(nint provider, string data, int length);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_new")]
        private static extern nint linux_gtk_file_dialog_new();

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_new")]
        private static extern nint macos_gtk_file_dialog_new();

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_new")]
        private static extern nint windows_gtk_file_dialog_new();

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_get_initial_file")]
        private static extern nint LinuxGetInitialFile(nint dialog, nint file);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_get_initial_file")]
        private static extern nint MacOSGetInitialFile(nint dialog, nint file);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_get_initial_file")]
        private static extern nint WindowsGetInitialFile(nint dialog, nint file);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_get_initial_folder")]
        private static extern nint LinuxGetInitialFolder(nint dialog, nint file);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_get_initial_folder")]
        private static extern nint MacOSGetInitialFolder(nint dialog, nint file);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_get_initial_folder")]
        private static extern nint WindowsGetInitialFolder(nint dialog, nint file);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_get_initial_name")]
        private static extern nint LinuxGetInitialName(nint dialog, nint file);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_get_initial_name")]
        private static extern nint MacOSGetInitialName(nint dialog, nint file);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_get_initial_name")]
        private static extern nint WindowsGetInitialName(nint dialog, nint file);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_set_title")]
        private static extern void LinuxSetTitle(nint dialog, string title);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_set_title")]
        private static extern void MacOSSetTitle(nint dialog, string title);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_set_title")]
        private static extern void WindowsSetTitle(nint dialog, string title);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_set_filters")]
        private static extern void LinuxSetFilters(nint dialog, nint filters);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_set_filters")]
        private static extern void MacOSSetFilters(nint dialog, nint filters);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_set_filters")]
        private static extern void WindowsSetFilters(nint dialog, nint filters);

        private delegate void GAsyncReadyCallback(nint source, nint res, nint user_data);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_open")]
        private static extern void LinuxOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_open")]
        private static extern void MacOSOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_open")]
        private static extern void WindowsOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_open_finish")]
        private static extern nint LinuxOpenFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_open_finish")]
        private static extern nint MacOSOpenFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_open_finish")]
        private static extern nint WindowsOpenFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_save")]
        private static extern void LinuxSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_save")]
        private static extern void MacOSSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_save")]
        private static extern void WindowsSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_save_finish")]
        private static extern nint LinuxSaveFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_save_finish")]
        private static extern nint MacOSSaveFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_save_finish")]
        private static extern nint WindowsSaveFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_select_folder")]
        private static extern void LinuxSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_select_folder")]
        private static extern void MacOSSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_select_folder")]
        private static extern void WindowsSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

        [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_select_folder_finish")]
        private static extern nint LinuxSelectFolderFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_select_folder_finish")]
        private static extern nint MacOSSelectFolderFinish(nint dialog, nint result, nint error);

        [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_select_folder_finish")]
        private static extern nint WindowsSelectFolderFinish(nint dialog, nint result, nint error);


        private bool _playlistPlaying;
        private Config.Playlist _curPlaylist;
        private long _curSong = -1;
        private readonly List<long> _playedSequences;
        private readonly List<long> _remainingSequences;

        private bool _stopUI = false;

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
            _dataAction, _trackViewerAction, _exportDLSAction, _exportSF2Action, _exportMIDIAction, _exportWAVAction, _playlistAction, _endPlaylistAction;

        private Signal<Gio.SimpleAction> _openDSESignal;

        private SignalHandler<Gio.SimpleAction> _openDSEHandler;

        // Menu Widgets
        private Widget _exportDLSWidget, _exportSF2Widget, _exportMIDIWidget, _exportWAVWidget, _endPlaylistWidget,
            _sequencesScrListView, _sequencesListView;

        // Main Box
        private Box _mainBox, _configButtonBox, _configPlayerButtonBox, _configSpinButtonBox, _configScaleBox;

        // Volume Button to indicate volume status
        private readonly VolumeButton _volumeButton;

        // One Scale controling volume and one Scale for the sequenced track
        private readonly Scale _volumeScale, _positionScale;

        // Mouse Click Gesture
        private GestureClick _positionGestureClick, _sequencesGestureClick;

        // Event Controller
        private EventArgs _openDSEEvent, _openAlphaDreamEvent, _openMP2KEvent, _openSDATEvent,
            _dataEvent, _trackViewerEvent, _exportDLSEvent, _exportSF2Event, _exportMIDIEvent, _exportWAVEvent, _playlistEvent, _endPlaylistEvent,
            _sequencesEventController;

        // Adjustments are for indicating the numbers and the position of the scale
        private Adjustment _volumeAdjustment, _positionAdjustment, _sequenceNumberAdjustment;

        // List Item Factory
        private readonly ListItemFactory _sequencesListFactory;

        // String List
        private string[] _sequencesListRowLabel;
        private StringList _sequencesStringList;

        // Single Selection
        private readonly SingleSelection _sequencesSingleSelection;

        // Column View
        private readonly ColumnView _sequencesColumnView;
        private readonly ColumnViewColumn _sequencesColumn;

        // List Item
        private readonly ListItem _sequencesListItem;

        // Tree View
        private readonly TreeView _sequencesTreeView;
        //private readonly TreeViewColumn _sequencesColumn;

        // List Store
        private ListStore _sequencesListStore;

        // Signal
        private Signal<ListItemFactory> _signal;

        // Callback
        private Gtk.FileDialog.GAsyncReadyCallback _saveCallback { get; set; }
        private Gtk.FileDialog.GAsyncReadyCallback _openCallback { get; set; }
        private Gtk.FileDialog.GAsyncReadyCallback _selectFolderCallback { get; set; }
        private Gio.AsyncReadyCallback _openCB { get; set; }
        private Gio.AsyncReadyCallback _saveCB { get; set; }
        private Gio.AsyncReadyCallback _selectFolderCB { get; set; }
        private Gio.AsyncReadyCallback _exceptionCallback { get; set;}

        #endregion

        public MainWindow(Application app)
        {
            // Main Window
            SetDefaultSize(500, 300); // Sets the default size of the Window
            Title = ConfigUtils.PROGRAM_NAME; // Sets the title to the name of the program, which is "VG Music Studio"
            _app = app;

            // Sets the _playedSequences and _remainingSequences with a List<long>() function to be ready for use
            _playedSequences = new List<long>();
            _remainingSequences = new List<long>();

            // Configures SetVolumeScale method with the MixerVolumeChanged Event action
            Mixer.MixerVolumeChanged += SetVolumeScale;

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

            _mainMenu.AppendItem(_fileItem); // Note: It must append the menu item, not the file menu itself
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
            _exportDLSAction.OnActivate += ExportDLS;
            _dataMenu.AppendItem(_exportDLSItem);
            _exportDLSItem.Unref();

            _exportSF2Item = Gio.MenuItem.New(Strings.MenuSaveSF2, "app.exportSF2");
            _exportSF2Action = Gio.SimpleAction.New("exportSF2", null);
            _app.AddAction(_exportSF2Action);
            _exportSF2Action.OnActivate += ExportSF2;
            _dataMenu.AppendItem(_exportSF2Item);
            _exportSF2Item.Unref();

            _exportMIDIItem = Gio.MenuItem.New(Strings.MenuSaveMIDI, "app.exportMIDI");
            _exportMIDIAction = Gio.SimpleAction.New("exportMIDI", null);
            _app.AddAction(_exportMIDIAction);
            _exportMIDIAction.OnActivate += ExportMIDI;
            _dataMenu.AppendItem(_exportMIDIItem);
            _exportMIDIItem.Unref();

            _exportWAVItem = Gio.MenuItem.New(Strings.MenuSaveWAV, "app.exportWAV");
            _exportWAVAction = Gio.SimpleAction.New("exportWAV", null);
            _app.AddAction(_exportWAVAction);
            _exportWAVAction.OnActivate += ExportWAV;
            _dataMenu.AppendItem(_exportWAVItem);
            _exportWAVItem.Unref();

            //_mainMenu.PrependItem(_dataItem); // Data menu item needs to be reserved, but remain invisible until a sound engine is initialized.
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
            _endPlaylistAction.OnActivate += EndCurrentPlaylist;
            _playlistMenu.AppendItem(_endPlaylistItem);
            _endPlaylistItem.Unref();

            //_mainMenu.PrependItem(_playlistItem); // Same thing as Data menu item.
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
            _sequenceNumberSpinButton.Visible = false;
            _sequenceNumberSpinButton.OnValueChanged += SequenceNumberSpinButton_ValueChanged;

            // Timer
            _timer = new Timer();
            _timer.Elapsed += UpdateUI;

            // Volume Scale
            _volumeAdjustment = Adjustment.New(0, 0, 100, 1, 1, 1);
            _volumeScale = Scale.New(Orientation.Horizontal, _volumeAdjustment);
            _volumeScale.Sensitive = false;
            _volumeScale.ShowFillLevel = true;
            _volumeScale.DrawValue = false;
            _volumeScale.WidthRequest = 250;
            _volumeScale.OnValueChanged += VolumeScale_ValueChanged;

            // Position Scale
            _positionAdjustment = Adjustment.New(0, 0, -1, 1, 1, 1);
            _positionScale = Scale.New(Orientation.Horizontal, _positionAdjustment);
            _positionScale.Sensitive = false;
            _positionScale.ShowFillLevel = true;
            _positionScale.DrawValue = false;
            _positionScale.WidthRequest = 250;
            _positionGestureClick = GestureClick.New();
            //_positionGestureClick.GetWidget().SetParent(_positionScale);
            _positionGestureClick.OnReleased += PositionScale_MouseButtonRelease; // ButtonRelease must go first, otherwise the scale it will follow the mouse cursor upon loading
            _positionGestureClick.OnPressed += PositionScale_MouseButtonPress;

            // Sequences List View
            _sequencesListRowLabel = new string[3] { "#", "Internal Name", null };
            _sequencesStringList = StringList.New(_sequencesListRowLabel);
            _sequencesScrListView = ScrolledWindow.New();
            _sequencesSingleSelection = SingleSelection.New(_sequencesStringList);
            _sequencesColumnView = ColumnView.New(_sequencesSingleSelection);
            _sequencesColumn = ColumnViewColumn.New("Name", _sequencesListFactory);
            //_sequencesColumn.GetColumnView().SetParent(_sequencesColumnView);
            _sequencesListFactory = SignalListItemFactory.New();
            _sequencesGestureClick = GestureClick.New();
            _sequencesListView = ListView.New(_sequencesSingleSelection, _sequencesListFactory);
            _sequencesListView.SetParent(_sequencesScrListView);
            //_sequencesGestureClick.GetWidget().SetParent(_sequencesListView);
            //_sequencesListView = new TreeView();
            //_sequencesListStore = new ListStore(typeof(string), typeof(string));
            //_sequencesColumn = new TreeViewColumn("Name", new CellRendererText(), "text", 1);
            //_sequencesListView.AppendColumn("#", new CellRendererText(), "text", 0);
            //_sequencesListView.AppendColumn(_sequencesColumn);
            //_sequencesListView.Model = _sequencesListStore;

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

            _mainBox.Append(_headerBar);
            _mainBox.Append(_popoverMenuBar);
            _mainBox.Append(_configButtonBox);
            _mainBox.Append(_configScaleBox);
            _mainBox.Append(_sequencesScrListView);

            _configPlayerButtonBox.MarginStart = 40;
            _configPlayerButtonBox.MarginEnd = 40;
            _configButtonBox.Append(_configPlayerButtonBox);
            _configSpinButtonBox.MarginStart = 100;
            _configSpinButtonBox.MarginEnd = 100;
            _configButtonBox.Append(_configSpinButtonBox);

            _configPlayerButtonBox.Append(_buttonPlay);
            _configPlayerButtonBox.Append(_buttonPause);
            _configPlayerButtonBox.Append(_buttonStop);

            _configSpinButtonBox.Append(_sequenceNumberSpinButton);

            _volumeScale.MarginStart = 20;
            _volumeScale.MarginEnd = 20;
            _configScaleBox.Append(_volumeScale);
            _positionScale.MarginStart = 20;
            _positionScale.MarginEnd = 20;
            _configScaleBox.Append(_positionScale);

            SetContent(_mainBox);

            Show();

            // Ensures the entire application closes when the window is closed
            //OnCloseRequest += delegate { Application.Quit(); };
        }

        // When the value is changed on the volume scale
        private void VolumeScale_ValueChanged(object sender, EventArgs e)
        {
            Engine.Instance.Mixer.SetVolume((float)(_volumeScale.Adjustment.Value / _volumeAdjustment.Value));
        }

        // Sets the volume scale to the specified position
        public void SetVolumeScale(float volume)
        {
            _volumeScale.OnValueChanged -= VolumeScale_ValueChanged;
            _volumeScale.Adjustment.Value = (int)(volume * _volumeAdjustment.Upper);
            _volumeScale.OnValueChanged += VolumeScale_ValueChanged;
        }

        private bool _positionScaleFree = true;
        private void PositionScale_MouseButtonRelease(object sender, GestureClick.ReleasedSignalArgs args)
        {
            if (args.NPress == 1) // Number 1 is Left Mouse Button
            {
                Engine.Instance.Player.SetCurrentPosition((long)_positionScale.Adjustment.Value); // Sets the value based on the position when mouse button is released
                _positionScaleFree = true; // Sets _positionScaleFree to true when mouse button is released
                LetUIKnowPlayerIsPlaying(); // This method will run the void that tells the UI that the player is playing a track
            }
        }
        private void PositionScale_MouseButtonPress(object sender, GestureClick.PressedSignalArgs args)
        {
            if (args.NPress == 1) // Number 1 is Left Mouse Button
            {
                _positionScaleFree = false;
            }
        }

        private bool _autoplay = false;
        private void SequenceNumberSpinButton_ValueChanged(object sender, EventArgs e)
        {
            //_sequencesGestureClick.OnBegin -= SequencesListView_SelectionGet;
            _signal.Connect(_sequencesListFactory, SequencesListView_SelectionGet, false, null);

            long index = (long)_sequenceNumberAdjustment.Value;
            Stop();
            this.Title = ConfigUtils.PROGRAM_NAME;
            //_sequencesListView.Margin = 0;
            //_songInfo.Reset();
            bool success;
            try
            {
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
                Config.Song? song = songs.SingleOrDefault(s => s.Index == index);
                if (song is not null)
                {
                    this.Title = $"{ConfigUtils.PROGRAM_NAME} - {song.Name}"; // TODO: Make this a func
                    //_sequencesColumnView.SortColumnId = songs.IndexOf(song) + 1; // + 1 because the "Music" playlist is first in the combobox
                }
                _positionAdjustment.Upper = Engine.Instance!.Player.LoadedSong!.MaxTicks;
                _positionAdjustment.Value = _positionAdjustment.Upper / 10;
                _positionAdjustment.Value = _positionAdjustment.Value / 4;
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
            _positionScale.Sensitive = _exportWAVWidget.Sensitive = success;
            _exportMIDIWidget.Sensitive = success && MP2KEngine.MP2KInstance is not null;
            _exportDLSWidget.Sensitive = _exportSF2Widget.Sensitive = success && AlphaDreamEngine.AlphaDreamInstance is not null;

            _autoplay = true;
            //_sequencesGestureClick.OnEnd += SequencesListView_SelectionGet;
            _signal.Connect(_sequencesListFactory, SequencesListView_SelectionGet, true, null);
        }
        private void SequencesListView_SelectionGet(object sender, EventArgs e)
        {
            var item = new object();
            item = _sequencesSingleSelection.SelectedItem;
            if (item is Config.Song song)
            {
                SetAndLoadSequence(song.Index);
            }
            else if (item is Config.Playlist playlist)
            {
                if (playlist.Songs.Count > 0
                && FlexibleMessageBox.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), Strings.MenuPlaylist, ButtonsType.YesNo) == ResponseType.Yes)
                {
                    ResetPlaylistStuff(false);
                    _curPlaylist = playlist;
                    Engine.Instance.Player.ShouldFadeOut = _playlistPlaying = true;
                    Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
                    _endPlaylistWidget.Sensitive = true;
                    SetAndLoadNextPlaylistSong();
                }
            }
        }
        private void SetAndLoadSequence(long index)
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

        private void SetAndLoadNextPlaylistSong()
        {
            if (_remainingSequences.Count == 0)
            {
                _remainingSequences.AddRange(_curPlaylist.Songs.Select(s => s.Index));
                if (GlobalConfig.Instance.PlaylistMode == PlaylistMode.Random)
                {
                    _remainingSequences.Any();
                }
            }
            long nextSequence = _remainingSequences[0];
            _remainingSequences.RemoveAt(0);
            SetAndLoadSequence(nextSequence);
        }
        private void ResetPlaylistStuff(bool enableds)
        {
            if (Engine.Instance != null)
            {
                Engine.Instance.Player.ShouldFadeOut = false;
            }
            _playlistPlaying = false;
            _curPlaylist = null;
            _curSong = -1;
            _remainingSequences.Clear();
            _playedSequences.Clear();
            //_endPlaylistWidget.Sensitive = false;
            _sequenceNumberSpinButton.Sensitive = _sequencesListView.Sensitive = enableds;
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
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuOpenDSE);

                _selectFolderCallback = (source, res, data) =>
                {
                    var folderHandle = d.SelectFolderFinish(res, IntPtr.Zero);
                    if (folderHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(folderHandle);
                        OpenDSEFinish(path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.SelectFolder(Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero);
                // if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                // {
                //     var d = linux_gtk_file_dialog_new();
                //     LinuxSetTitle(d, Strings.MenuOpenDSE);

                //     _selectFolderCallback = (source, res, data) =>
                //     {
                //         var folderHandle = LinuxSelectFolderFinish(d, res, IntPtr.Zero);
                //         if (folderHandle != IntPtr.Zero)
                //         {
                //             var path = LinuxGetPath(folderHandle);
                //             OpenDSEFinish(path); // GtkFileDialog also doesn't have blocking APIs.
                //         }
                //     };
                //     LinuxSelectFolder(d, Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero);
                // }
                // else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                // {
                //     var d = macos_gtk_file_dialog_new();
                //     MacOSSetTitle(d, Strings.MenuOpenDSE);

                //     _selectFolderCallback = (source, res, data) =>
                //     {
                //         var folderHandle = MacOSSelectFolderFinish(d, res, IntPtr.Zero);
                //         if (folderHandle != IntPtr.Zero)
                //         {
                //             var path = MacOSGetPath(folderHandle);
                //             OpenDSEFinish(path);
                //         }
                //     };
                //     MacOSSelectFolder(d, Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero);
                // }
                // else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                // {
                //     var d = windows_gtk_file_dialog_new();
                //     WindowsSetTitle(d, Strings.MenuOpenDSE);

                //     _selectFolderCallback = (source, res, data) =>
                //     {
                //         var folderHandle = WindowsSelectFolderFinish(d, res, IntPtr.Zero);
                //         if (folderHandle != IntPtr.Zero)
                //         {
                //             var path = WindowsGetPath(folderHandle);
                //             OpenDSEFinish(path);
                //         }
                //     };
                //     WindowsSelectFolder(d, Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero);
                // }
                // else { return; }
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
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuOpenSDAT);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(filterSDAT);
                filters.Append(allFiles);
                d.SetFilters(filters);
                _openCallback = (source, res, data) =>
                {
                    var fileHandle = d.OpenFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        d.GetData(path);
                        OpenSDATFinish(path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
            }
        }
        private void OpenSDATFinish(string path)
        {
            DisposeEngine();
            try
            {
                _ = new SDATEngine(new SDAT(File.ReadAllBytes(path)));
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
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuOpenAlphaDream);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(filterGBA);
                filters.Append(allFiles);
                d.SetFilters(filters);
                _openCallback = (source, res, data) =>
                {
                    var fileHandle = d.OpenFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        d.GetData(path);
                        OpenAlphaDreamFinish(path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
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
                    d.GetData(path);
                    OpenMP2KFinish(path);
                    d.Unref();
                };
                d.Show();
            }
            else
            {
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuOpenMP2K);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(filterGBA);
                filters.Append(allFiles);
                d.SetFilters(filters);
                _openCallback = (source, res, data) =>
                {
                    var fileHandle = d.OpenFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        d.GetData(path);
                        OpenMP2KFinish(path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
            }
        }
        private void OpenMP2KFinish(string path)
        {
            DisposeEngine();
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

            MP2KConfig config = MP2KEngine.MP2KInstance!.Config;
            FinishLoading(config.SongTableSizes[0]);
            _sequenceNumberSpinButton.Visible = true;
            _sequenceNumberSpinButton.Show();
            _mainMenu.AppendItem(_dataItem);
            _mainMenu.AppendItem(_playlistItem);
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
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuSaveDLS);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(ff);
                d.SetFilters(filters);
                _saveCallback = (source, res, data) =>
                {
                    var fileHandle = d.SaveFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        ExportDLSFinish(cfg, path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
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
                d.SetCurrentName(Engine.Instance!.Config.GetSongName((long)_sequenceNumberSpinButton.Value));
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
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuSaveMIDI);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(ff);
                d.SetFilters(filters);
                _saveCallback = (source, res, data) =>
                {
                    var fileHandle = d.SaveFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        ExportMIDIFinish(path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
            }
        }
        private void ExportMIDIFinish(string path)
        {
            MP2KPlayer p = MP2KEngine.MP2KInstance!.Player;
            var args = new MIDISaveArgs
            {
                SaveCommandsBeforeTranspose = true,
                ReverseVolume = false,
                TimeSignatures = new List<(int AbsoluteTick, (byte Numerator, byte Denominator))>
                    {
                    (0, (4, 4)),
                    },
            };

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
                    ExportSF2Finish(cfg, path);
                    d.Unref();
                };
                d.Show();
            }
            else
            {
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuSaveSF2);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(ff);
                d.SetFilters(filters);
                _saveCallback = (source, res, data) =>
                {
                    var fileHandle = d.SaveFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        ExportSF2Finish(cfg, path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
            }
        }
        private void ExportSF2Finish(AlphaDreamConfig config, string path)
        {
            try
            {
                AlphaDreamSoundFontSaver_SF2.Save(config, path);
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

                d.SetCurrentName(Engine.Instance!.Config.GetSongName((long)_sequenceNumberSpinButton.Value));
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
                var d = Gtk.FileDialog.New();
                d.SetTitle(Strings.MenuSaveWAV);
                var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
                filters.Append(ff);
                d.SetFilters(filters);
                _saveCallback = (source, res, data) =>
                {
                    var fileHandle = d.SaveFinish(res, IntPtr.Zero);
                    if (fileHandle != IntPtr.Zero)
                    {
                        var path = d.GetPath(fileHandle);
                        ExportWAVFinish(path);
                        d.Unref();
                    }
                    d.Unref();
                };
                d.Save(Handle, IntPtr.Zero, _saveCallback, IntPtr.Zero);
            }
        }
        private void ExportWAVFinish(string path)
        {
            Stop();

            IPlayer player = Engine.Instance.Player;
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
            _exceptionCallback = (source, res) =>
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

            bool timerValue; // Used for updating _positionAdjustment to be in sync with _timer

            // Configures the buttons when player is playing a sequenced track
            _buttonPause.FocusOnClick = _buttonStop.FocusOnClick = true;
            _buttonPause.Label = Strings.PlayerPause;
            _timer.Interval = (int)(1_000.0 / GlobalConfig.Instance.RefreshRate);

            // Experimental attempt for _positionAdjustment to be synchronized with _timer
            timerValue = _timer.Equals(_positionAdjustment);
            timerValue.CompareTo(_timer);

            _timer.Start();
        }

        private void Play()
        {
            Engine.Instance!.Player.Play();
            LetUIKnowPlayerIsPlaying();
        }
        private void Pause()
        {
            Engine.Instance!.Player.Pause();
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
            Engine.Instance!.Player.Stop();
            _buttonPause.Sensitive = _buttonStop.Sensitive = false;
            _buttonPause.Label = Strings.PlayerPause;
            _timer.Stop();
            UpdatePositionIndicators(0L);
        }
        private void TogglePlayback(object? sender, EventArgs? e)
        {
            switch (Engine.Instance!.Player.State)
            {
                case PlayerState.Stopped: Play(); break;
                case PlayerState.Paused:
                case PlayerState.Playing: Pause(); break;
            }
        }
        private void PlayPreviousSequence(object? sender, EventArgs? e)
        {
            long prevSequence;
            if (_playlistPlaying)
            {
                int index = _playedSequences.Count - 1;
                prevSequence = _playedSequences[index];
                _playedSequences.RemoveAt(index);
                _playedSequences.Insert(0, _curSong);
            }
            else
            {
                prevSequence = (long)_sequenceNumberSpinButton.Value - 1;
            }
            SetAndLoadSequence(prevSequence);
        }
        private void PlayNextSong(object? sender, EventArgs? e)
        {
            if (_playlistPlaying)
            {
                _playedSequences.Add(_curSong);
                SetAndLoadNextPlaylistSong();
            }
            else
            {
                SetAndLoadSequence((long)_sequenceNumberSpinButton.Value + 1);
            }
        }

        private void FinishLoading(long numSongs)
        {
            Engine.Instance!.Player.SongEnded += SongEnded;
            foreach (Config.Playlist playlist in Engine.Instance.Config.Playlists)
            {
                int i = 0;
                Value v = new Value(i++);
                _sequencesListStore.SetValue(null, playlist.GetHashCode(), v);
                playlist.Songs.Select(s => ColumnView.New((SelectionModel)_sequencesListStore)).ToArray();
            }
            _sequenceNumberAdjustment.Upper = numSongs - 1;
#if DEBUG
            // [Debug methods specific to this UI will go in here]
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

        private void UpdateUI(object? sender, EventArgs? e)
        {
            if (_stopUI)
            {
                _stopUI = false;
                if (_playlistPlaying)
                {
                    _playedSequences.Add(_curSong);
                }
                else
                {
                    Stop();
                }
            }
            else
            {
                UpdatePositionIndicators(Engine.Instance!.Player.LoadedSong!.ElapsedTicks);
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
                _positionAdjustment.Value = ticks; // A Gtk.Adjustment field must be used here to avoid issues
            }
        }
    }
}
