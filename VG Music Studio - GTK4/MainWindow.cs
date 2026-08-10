using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA.AlphaDream;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.GTK4.Util;
using Adw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using GObject;

namespace Kermalis.VGMusicStudio.GTK4;

[Subclass<Window>]
internal sealed partial class MainWindow
{
    private PlayingPlaylist? _playlist;
    private int _curSong = -1;
    private int _backupSong = -1;

    private bool _songEnded = false;
    private bool _playlistChanged = false;
    private bool _autoplay = false;
    private bool _preventAutoplay = false;

    public static MainWindow? Instance { get; private set; }

    private Gtk.WindowGroup _windowGroup;

    #region Widgets

    // The Windows
    private Preferences? _preferences;
    private MIDIConverterDialog? _midiConverterDialog;
    private TrackEditor? _trackEditor;
    private SoundBankEditor? _soundBankEditor;
    private WidgetWindow? _playlistWindow, _seqAudioPianoWindow, _sequencedAudioTrackInfoWindow, _sequencedAudioListWindow;

    // Buttons
    private Gtk.Button _buttonPlay, _buttonStop, _buttonRecord;
    private Gtk.ToggleButton _buttonPause;

    // Spin Button for the numbered tracks
    private Gtk.SpinButton _sequenceNumberSpinButton;

    // Timer
    private GLib.Timer _timer;

    // Popover Menu Bar
    private Gtk.PopoverMenuBar _popoverMenuBar;

    // LibAdwaita Header Bar
    private HeaderBar _headerBar;

    // LibAdwaita Application
    private Application _app;

    // Menus
    private Gio.Menu _mainMenu, _fileMenu, _editMenu, _dataMenu, _playlistMenu, _widgetMenu, _playlistWidgetMenu,
        _seqAudioPianoWidgetMenu, _seqAudioTrackInfoWidgetMenu, _seqAudioListWidgetMenu;

    // Menu Labels
    private Gtk.Label _fileLabel, _editLabel, _dataLabel, _playlistLabel, _widgetLabel;

    // Menu Items
    private Gio.MenuItem
        _fileItem,
        _openDSEItem, _openAlphaDreamItem, _openMP2KItem, _openSDATItem,
        _editItem,
        _preferencesItem,
        _dataItem,
        _midiConverterDialogItem, _trackEditorItem, _soundBankEditorItem, _exportASMItem, _exportMIDIItem, _exportDLSItem, _exportSF2Item, _exportWAVItem,
        _playlistItem,
        _playPlaylistItem, _endPlaylistItem,
        _widgetItem,
        _playlistWidgetTiledItem, _playlistWidgetWindowedItem, _playlistWidgetHideItem,
        _seqAudioPianoWidgetTiledItem, _seqAudioPianoWidgetWindowedItem, _seqAudioPianoWidgetHideItem,
        _seqAudioTrackInfoWidgetTiledItem, _seqAudioTrackInfoWidgetWindowedItem, _seqAudioTrackInfoWidgetHideItem,
        _seqAudioListWidgetTiledItem, _seqAudioListWidgetWindowedItem, _seqAudioListWidgetHideItem;

    // Menu Actions
    private Gio.SimpleAction
        _openDSEAction, _openAlphaDreamAction, _openMP2KAction, _openSDATAction,
        _preferencesAction,
        _midiConverterDialogAction, _trackEditorAction, _soundBankEditorAction, _exportASMAction, _exportMIDIAction, _exportDLSAction, _exportSF2Action, _exportWAVAction,
        _playPlaylistAction, _endPlaylistAction,
        _playlistWidgetTiledAction, _playlistWidgetWindowedAction, _playlistWidgetHideAction,
        _seqAudioPianoWidgetTiledAction, _seqAudioPianoWidgetWindowedAction, _seqAudioPianoWidgetHideAction,
        _seqAudioTrackInfoWidgetTiledAction, _seqAudioTrackInfoWidgetWindowedAction, _seqAudioTrackInfoWidgetHideAction,
        _seqAudioListWidgetTiledAction, _seqAudioListWidgetWindowedAction, _seqAudioListWidgetHideAction;

    // Boxes
    private Gtk.Box _mainBox, _configButtonBox, _configPlayerButtonBox, _configSpinButtonBox, _configBarBox,
        _playlistBox, _pianoBox, _sequencedAudioTrackInfoBox, _sequencedAudioListBox;

    // One Scale controling volume and one Scale for the sequenced track
    private Gtk.Scale _volumeBar, _positionBar;

    // Mouse Click and Drag Gestures
    private Gtk.GestureClick _positionGestureClick, _sequenceNumberSpinButtonGestureClick;
    private Gtk.GestureDrag _positionGestureDrag;

    // Playlist
    private PlaylistSelector _playlistSelector;

    // Sequenced Audio Piano
    private SequencedAudio_Piano _piano;

    // Sequenced Audio List
    private SequencedAudio_List _sequencedAudioList;

    // Sequenced Audio Track Info
    private SequencedAudio_TrackInfo _sequencedAudioTrackInfo;

    #endregion

    partial void Initialize()
    {
        // Main Window
        SetDefaultSize(100, 100); // Sets the default size of the Window
        Title = GetProgramName(); // Sets the title to the name of the program, which is "VG Music Studio"
    }

    internal void CreateWindow(Application app)
    {
        _app = app;
        _windowGroup = Gtk.WindowGroup.New();

        // LibAdwaita Header Bar
        _headerBar = Adw.HeaderBar.New();
        _headerBar.SetShowEndTitleButtons(true);

        // Main Menu
        _mainMenu = Gio.Menu.New();

        // Popover Menu Bar
        _popoverMenuBar = Gtk.PopoverMenuBar.NewFromModel(_mainMenu); // This will ensure that the menu model is used inside of the PopoverMenuBar widget
        _popoverMenuBar.MenuModel = _mainMenu;
        _popoverMenuBar.MnemonicActivate(true);

        // File Menu
        _fileMenu = Gio.Menu.New();

        _fileLabel = Gtk.Label.NewWithMnemonic(Strings.MenuFile);
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

        // Edit Menu
        _editMenu = Gio.Menu.New();

        _editLabel = Gtk.Label.NewWithMnemonic(Strings.MenuEdit);
        _editLabel.GetMnemonicKeyval();
        _editLabel.SetUseUnderline(true);
        _editItem = Gio.MenuItem.New(_editLabel.GetLabel(), null);
        _popoverMenuBar.AddMnemonicLabel(_editLabel);
        _editItem.SetSubmenu(_editMenu);

        _preferencesItem = Gio.MenuItem.New(Strings.MenuPreferences, "app.preferences");
        _preferencesAction = Gio.SimpleAction.New("preferences", null);
        _app.AddAction(_preferencesAction);
        _preferencesAction.OnActivate += OpenPreferences;
        _editMenu.AppendItem(_preferencesItem);
        _preferencesItem.Unref();

        _mainMenu.AppendItem(_editItem);
        _editItem.Unref();

        // Data Menu
        _dataMenu = Gio.Menu.New();

        _dataLabel = Gtk.Label.NewWithMnemonic(Strings.MenuData);
        _dataLabel.GetMnemonicKeyval();
        _dataLabel.SetUseUnderline(true);
        _dataItem = Gio.MenuItem.New(_dataLabel.GetLabel(), null);
        _popoverMenuBar.AddMnemonicLabel(_dataLabel);
        _dataItem.SetSubmenu(_dataMenu);

        _midiConverterDialogItem = Gio.MenuItem.New(Strings.MIDIConverterTitle, "app.midiConverterDialog");
        _midiConverterDialogAction = Gio.SimpleAction.New("midiConverterDialog", null);
        _app.AddAction(_midiConverterDialogAction);
        _midiConverterDialogAction.Enabled = true;
        _midiConverterDialogAction.OnActivate += OpenMIDIConverterDialog;
        _dataMenu.AppendItem(_midiConverterDialogItem);
        _midiConverterDialogItem.Unref();

        _trackEditorItem = Gio.MenuItem.New(Strings.TrackEditorTitle, "app.trackEditor");
        _trackEditorAction = Gio.SimpleAction.New("trackEditor", null);
        _app.AddAction(_trackEditorAction);
        _trackEditorAction.Enabled = false;
        _trackEditorAction.OnActivate += OpenTrackEditor;
        _dataMenu.AppendItem(_trackEditorItem);
        _trackEditorItem.Unref();

        _soundBankEditorItem = Gio.MenuItem.New(Strings.MenuVoiceGroupSoundBankEditor, "app.soundBankEditor");
        _soundBankEditorAction = Gio.SimpleAction.New("soundBankEditor", null);
        _app.AddAction(_soundBankEditorAction);
        _soundBankEditorAction.Enabled = false;
        _soundBankEditorAction.OnActivate += OpenSoundBankEditor;
        _dataMenu.AppendItem(_soundBankEditorItem);
        _soundBankEditorItem.Unref();

        _exportASMItem = Gio.MenuItem.New(Strings.MenuSaveASM, "app.exportASM");
        _exportASMAction = Gio.SimpleAction.New("exportASM", null);
        _app.AddAction(_exportASMAction);
        _exportASMAction.Enabled = false;
        _exportASMAction.OnActivate += ExportASM;
        _dataMenu.AppendItem(_exportASMItem);
        _exportASMItem.Unref();

        _exportMIDIItem = Gio.MenuItem.New(Strings.MenuSaveMIDI, "app.exportMIDI");
        _exportMIDIAction = Gio.SimpleAction.New("exportMIDI", null);
        _app.AddAction(_exportMIDIAction);
        _exportMIDIAction.Enabled = false;
        _exportMIDIAction.OnActivate += ExportMIDI;
        _dataMenu.AppendItem(_exportMIDIItem);
        _exportMIDIItem.Unref();

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

        _playlistLabel = Gtk.Label.NewWithMnemonic(Strings.MenuPlaylist);
        _playlistLabel.GetMnemonicKeyval();
        _playlistLabel.SetUseUnderline(true);
        _playlistItem = Gio.MenuItem.New(_playlistLabel.GetLabel(), null);
        _popoverMenuBar.AddMnemonicLabel(_playlistLabel);
        _playlistItem.SetSubmenu(_playlistMenu);

        _playPlaylistItem = Gio.MenuItem.New(Strings.MenuPlayPlaylist, "app.playPlaylist");
        _playPlaylistAction = Gio.SimpleAction.New("playPlaylist", null);
        _app.AddAction(_playPlaylistAction);
        _playPlaylistAction.Enabled = false;
        _playPlaylistAction.OnActivate += PlayCurrentPlaylist;
        _playlistMenu.AppendItem(_playPlaylistItem);
        _playPlaylistItem.Unref();

        _endPlaylistItem = Gio.MenuItem.New(Strings.MenuEndPlaylist, "app.endPlaylist");
        _endPlaylistAction = Gio.SimpleAction.New("endPlaylist", null);
        _app.AddAction(_endPlaylistAction);
        _endPlaylistAction.Enabled = false;
        _endPlaylistAction.OnActivate += EndCurrentPlaylist;
        _playlistMenu.AppendItem(_endPlaylistItem);
        _endPlaylistItem.Unref();

        _mainMenu.AppendItem(_playlistItem);
        _playlistItem.Unref();

        // Widget Menu
        _widgetMenu = Gio.Menu.New();

        _widgetLabel = Gtk.Label.NewWithMnemonic("Widgets");
        _widgetLabel.GetMnemonicKeyval();
        _widgetLabel.SetUseUnderline(true);
        _widgetItem = Gio.MenuItem.New(_widgetLabel.GetLabel(), null);
        _popoverMenuBar.AddMnemonicLabel(_widgetLabel);
        _widgetItem.SetSubmenu(_widgetMenu);

        _playlistWidgetMenu = Gio.Menu.New();

        _playlistWidgetTiledItem = Gio.MenuItem.New("Tiled", "app.playlistWidgetTiled");
        _playlistWidgetTiledAction = Gio.SimpleAction.NewStateful("playlistWidgetTiled", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_playlistWidgetTiledAction);
        _playlistWidgetTiledAction.Enabled = false;
        _playlistWidgetTiledAction.OnActivate += Playlist_GetTiled;
        _playlistWidgetMenu.AppendItem(_playlistWidgetTiledItem);
        _playlistWidgetTiledItem.Unref();

        _playlistWidgetWindowedItem = Gio.MenuItem.New("Windowed", "app.playlistWidgetWindowed");
        _playlistWidgetWindowedAction = Gio.SimpleAction.NewStateful("playlistWidgetWindowed", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_playlistWidgetWindowedAction);
        _playlistWidgetWindowedAction.Enabled = false;
        _playlistWidgetWindowedAction.OnActivate += Playlist_GetWindowed;
        _playlistWidgetMenu.AppendItem(_playlistWidgetWindowedItem);
        _playlistWidgetWindowedItem.Unref();

        _playlistWidgetHideItem = Gio.MenuItem.New("Hide", "app.playlistWidgetHide");
        _playlistWidgetHideAction = Gio.SimpleAction.NewStateful("playlistWidgetHide", null, GLib.Variant.NewBoolean(true));
        _app.AddAction(_playlistWidgetHideAction);
        _playlistWidgetHideAction.Enabled = false;
        _playlistWidgetHideAction.OnActivate += Playlist_Hide;
        _playlistWidgetMenu.AppendItem(_playlistWidgetHideItem);
        _playlistWidgetHideItem.Unref();

        _widgetMenu.AppendSubmenu("Playlist", _playlistWidgetMenu);

        _seqAudioPianoWidgetMenu = Gio.Menu.New();

        _seqAudioPianoWidgetTiledItem = Gio.MenuItem.New("Tiled", "app.seqAudioPianoWidgetTiled");
        _seqAudioPianoWidgetTiledAction = Gio.SimpleAction.NewStateful("seqAudioPianoWidgetTiled", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_seqAudioPianoWidgetTiledAction);
        _seqAudioPianoWidgetTiledAction.Enabled = false;
        _seqAudioPianoWidgetTiledAction.OnActivate += SeqAudioPiano_GetTiled;
        _seqAudioPianoWidgetMenu.AppendItem(_seqAudioPianoWidgetTiledItem);
        _seqAudioPianoWidgetTiledItem.Unref();

        _seqAudioPianoWidgetWindowedItem = Gio.MenuItem.New("Windowed", "app.seqAudioPianoWidgetWindowed");
        _seqAudioPianoWidgetWindowedAction = Gio.SimpleAction.NewStateful("seqAudioPianoWidgetWindowed", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_seqAudioPianoWidgetWindowedAction);
        _seqAudioPianoWidgetWindowedAction.Enabled = false;
        _seqAudioPianoWidgetWindowedAction.OnActivate += SeqAudioPiano_GetWindowed;
        _seqAudioPianoWidgetMenu.AppendItem(_seqAudioPianoWidgetWindowedItem);
        _seqAudioPianoWidgetWindowedItem.Unref();

        _seqAudioPianoWidgetHideItem = Gio.MenuItem.New("Hide", "app.seqAudioPianoWidgetHide");
        _seqAudioPianoWidgetHideAction = Gio.SimpleAction.NewStateful("seqAudioPianoWidgetHide", null, GLib.Variant.NewBoolean(true));
        _app.AddAction(_seqAudioPianoWidgetHideAction);
        _seqAudioPianoWidgetHideAction.Enabled = false;
        _seqAudioPianoWidgetHideAction.OnActivate += SeqAudioPiano_Hide;
        _seqAudioPianoWidgetMenu.AppendItem(_seqAudioPianoWidgetHideItem);
        _seqAudioPianoWidgetHideItem.Unref();

        _widgetMenu.AppendSubmenu("Piano", _seqAudioPianoWidgetMenu);

        _seqAudioTrackInfoWidgetMenu = Gio.Menu.New();

        _seqAudioTrackInfoWidgetTiledItem = Gio.MenuItem.New("Tiled", "app.seqAudioTrackInfoWidgetTiled");
        _seqAudioTrackInfoWidgetTiledAction = Gio.SimpleAction.NewStateful("seqAudioTrackInfoWidgetTiled", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_seqAudioTrackInfoWidgetTiledAction);
        _seqAudioTrackInfoWidgetTiledAction.Enabled = false;
        _seqAudioTrackInfoWidgetTiledAction.OnActivate += SeqAudioTrackInfo_GetTiled;
        _seqAudioTrackInfoWidgetMenu.AppendItem(_seqAudioTrackInfoWidgetTiledItem);
        _seqAudioTrackInfoWidgetTiledItem.Unref();

        _seqAudioTrackInfoWidgetWindowedItem = Gio.MenuItem.New("Windowed", "app.seqAudioTrackInfoWidgetWindowed");
        _seqAudioTrackInfoWidgetWindowedAction = Gio.SimpleAction.NewStateful("seqAudioTrackInfoWidgetWindowed", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_seqAudioTrackInfoWidgetWindowedAction);
        _seqAudioTrackInfoWidgetWindowedAction.Enabled = false;
        _seqAudioTrackInfoWidgetWindowedAction.OnActivate += SeqAudioTrackInfo_GetWindowed;
        _seqAudioTrackInfoWidgetMenu.AppendItem(_seqAudioTrackInfoWidgetWindowedItem);
        _seqAudioTrackInfoWidgetWindowedItem.Unref();

        _seqAudioTrackInfoWidgetHideItem = Gio.MenuItem.New("Hide", "app.seqAudioTrackInfoWidgetHide");
        _seqAudioTrackInfoWidgetHideAction = Gio.SimpleAction.NewStateful("seqAudioTrackInfoWidgetHide", null, GLib.Variant.NewBoolean(true));
        _app.AddAction(_seqAudioTrackInfoWidgetHideAction);
        _seqAudioTrackInfoWidgetHideAction.Enabled = false;
        _seqAudioTrackInfoWidgetHideAction.OnActivate += SeqAudioTrackInfo_Hide;
        _seqAudioTrackInfoWidgetMenu.AppendItem(_seqAudioTrackInfoWidgetHideItem);
        _seqAudioTrackInfoWidgetHideItem.Unref();

        _widgetMenu.AppendSubmenu("Sequenced Audio Track Info", _seqAudioTrackInfoWidgetMenu);

        _seqAudioListWidgetMenu = Gio.Menu.New();

        _seqAudioListWidgetTiledItem = Gio.MenuItem.New("Tiled", "app.seqAudioListWidgetTiled");
        _seqAudioListWidgetTiledAction = Gio.SimpleAction.NewStateful("seqAudioListWidgetTiled", null, GLib.Variant.NewBoolean(true));
        _app.AddAction(_seqAudioListWidgetTiledAction);
        _seqAudioListWidgetTiledAction.OnActivate += SeqAudioList_GetTiled;
        _seqAudioListWidgetMenu.AppendItem(_seqAudioListWidgetTiledItem);
        _seqAudioListWidgetTiledItem.Unref();

        _seqAudioListWidgetWindowedItem = Gio.MenuItem.New("Windowed", "app.seqAudioListWidgetWindowed");
        _seqAudioListWidgetWindowedAction = Gio.SimpleAction.NewStateful("seqAudioListWidgetWindowed", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_seqAudioListWidgetWindowedAction);
        _seqAudioListWidgetWindowedAction.OnActivate += SeqAudioList_GetWindowed;
        _seqAudioListWidgetMenu.AppendItem(_seqAudioListWidgetWindowedItem);
        _seqAudioListWidgetWindowedItem.Unref();

        _seqAudioListWidgetHideItem = Gio.MenuItem.New("Hide", "app.seqAudioListWidgetHide");
        _seqAudioListWidgetHideAction = Gio.SimpleAction.NewStateful("seqAudioListWidgetHide", null, GLib.Variant.NewBoolean(false));
        _app.AddAction(_seqAudioListWidgetHideAction);
        _seqAudioListWidgetHideAction.OnActivate += SeqAudioList_Hide;
        _seqAudioListWidgetMenu.AppendItem(_seqAudioListWidgetHideItem);
        _seqAudioListWidgetHideItem.Unref();

        _widgetMenu.AppendSubmenu("Sequenced Audio List", _seqAudioListWidgetMenu);

        _mainMenu.AppendItem(_widgetItem);
        _widgetItem.Unref();

        // Buttons
        static ConstructArgument[] AddArgs(bool sensitive, string tooltipText, string iconName)
        {
            ConstructArgument[] args =
            [
                new ConstructArgument("sensitive", new Value(sensitive)),
                new ConstructArgument("tooltip_text", new Value(tooltipText)),
                new ConstructArgument("icon_name", new Value(iconName)),
            ];
            return args;
        }
        _buttonPlay = Gtk.Button.NewWithProperties(AddArgs(false, Strings.PlayerPlay, "media-playback-start-symbolic"));
        _buttonPlay.OnClicked += ButtonPlay_Clicked;
        _buttonPause = Gtk.ToggleButton.NewWithProperties(AddArgs(false, Strings.PlayerPause, "media-playback-pause-symbolic"));
        _buttonPause.OnClicked += ButtonPause_Clicked;
        _buttonStop = Gtk.Button.NewWithProperties(AddArgs(false, Strings.PlayerStop, "media-playback-stop-symbolic"));
        _buttonStop.OnClicked += ButtonStop_Clicked;

        _buttonRecord = Gtk.Button.NewWithProperties(AddArgs(false, Strings.PlayerRecord, "media-record-symbolic"));
        _buttonRecord.Sensitive = false; _buttonRecord.TooltipText = Strings.PlayerRecord; _buttonRecord.IconName = "media-record-symbolic";
        _buttonRecord.OnClicked += ExportWAV;

        // Spin Button
        _sequenceNumberSpinButton = Gtk.SpinButton.New(Gtk.Adjustment.New(0, 0, 10000, 1, 10, 0), 0, 0);
        _sequenceNumberSpinButtonGestureClick = Gtk.GestureClick.New();
        _sequenceNumberSpinButton.AddController(_sequenceNumberSpinButtonGestureClick);
        _sequenceNumberSpinButton.Sensitive = false;
        _sequenceNumberSpinButton.SetNumeric(true);
        _sequenceNumberSpinButton.Value = 0;
        _sequenceNumberSpinButton.OnValueChanged += SequenceNumberSpinButton_ValueChanged;
        _sequenceNumberSpinButton.OnChangeValue += SequenceNumberSpinButton_ChangeValue;

        // // Timer
        _timer = GLib.Timer.New();

        // Volume Bar
        _volumeBar = Gtk.Scale.New(Gtk.Orientation.Horizontal, Gtk.Adjustment.New(0, 0, 100, 1, 10, 0));
        _volumeBar.OnValueChanged += VolumeBar_ValueChanged;
        _volumeBar.Sensitive = false;
        _volumeBar.ShowFillLevel = true;
        _volumeBar.DrawValue = false;
        _volumeBar.WidthRequest = 250;

        // Position Bar
        _positionBar = Gtk.Scale.New(Gtk.Orientation.Horizontal, Gtk.Adjustment.New(0, 0, 100, 1, 10, 0)); // The Upper value property must contain a value of 1 or higher for the widget to show upon startup
        _positionGestureClick = Gtk.GestureClick.New();
        _positionGestureDrag = Gtk.GestureDrag.New();
        _positionBar.AddController(_positionGestureClick);
        _positionBar.AddController(_positionGestureDrag);
        _positionBar.Sensitive = false;
        _positionBar.Focusable = true;
        _positionBar.ShowFillLevel = true;
        _positionBar.DrawValue = false;
        _positionBar.WidthRequest = 250;
        _positionBar.RestrictToFillLevel = false;
        _positionBar.OnChangeValue += PositionBar_ChangeValue;
        _positionBar.OnMoveSlider += PositionBar_MoveSlider;
        _positionBar.OnValueChanged += PositionBar_ValueChanged;
        _positionGestureClick.OnStopped += PositionBar_MouseButtonRelease;
        _positionGestureClick.OnCancel += PositionBar_MouseButtonRelease;
        _positionGestureClick.OnPressed += PositionBar_MouseButtonPress;
        _positionGestureClick.OnReleased += PositionBar_MouseButtonRelease;
        _positionGestureClick.OnUnpairedRelease += PositionBar_MouseButtonRelease;
        _positionGestureClick.OnBegin += PositionBar_MouseButtonOnBegin;
        _positionGestureClick.OnEnd += PositionBar_MouseButtonOnEnd;
        // _positionGestureDrag.OnDragBegin += PositionBar_MouseButtonOnBegin;
        // _positionGestureDrag.OnDragEnd += PositionBar_MouseButtonOnEnd;

        // Playlist
        _playlistSelector = PlaylistSelector.NewWithProperties([]);
        _playlistSelector.PlaylistClick = Gtk.GestureClick.New();
        _playlistSelector.PlaylistClick.SetButton(1);
        // _playlistSelector.PlaylistClick.OnPressed += PlaylistClick_LeftClick;
        _playlistSelector.PlaylistSongClick = Gtk.GestureClick.New();
        _playlistSelector.PlaylistSongClick.SetButton(1);
        // _playlistSelector.PlaylistSongClick.OnPressed += PlaylistSongClick_LeftClick;
        _playlistSelector.PlaylistSelectDropDown!.AddController(_playlistSelector.PlaylistClick);
        _playlistSelector.PlaylistSelectSongDropDown!.AddController(_playlistSelector.PlaylistSongClick);
        _playlistSelector.ButtonPlayPlist!.OnClicked += ButtonPlayPlist_Clicked;
        _playlistSelector.ButtonPlistStyle!.OnClicked += ButtonPlistStyle_Clicked;
        _playlistSelector.ButtonPrevPlistSong!.OnClicked += PlayPreviousSong;
        _playlistSelector.ButtonNextPlistSong!.OnClicked += PlayNextSong;
        _playlistBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        _playlistBox.SetVexpand(true);

        // Sequenced Audio Piano
        _piano = SequencedAudio_Piano.NewWithProperties([]);
        _pianoBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        _pianoBox.SetVexpand(false);

        // Sequenced Audio Track Info
        _sequencedAudioTrackInfo = SequencedAudio_TrackInfo.NewWithProperties([]);
        _sequencedAudioTrackInfoBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        _sequencedAudioTrackInfoBox.SetVexpand(true);

        // Sequenced Audio List
        _sequencedAudioList = SequencedAudio_List.NewWithProperties([]);
        _sequencedAudioList.Init();
        _sequencedAudioListBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        _sequencedAudioListBox.SetVexpand(true);
        _sequencedAudioListBox.Append(_sequencedAudioList);

        // Main display
        _mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 4);

        _configButtonBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 2);
        _configButtonBox.Halign = Gtk.Align.Center;
        _configPlayerButtonBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 3);
        _configPlayerButtonBox.Halign = Gtk.Align.Center;
        _configSpinButtonBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 1);
        _configSpinButtonBox.Halign = Gtk.Align.Center;
        _configSpinButtonBox.WidthRequest = 100;
        _configBarBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 2);
        _configBarBox.Halign = Gtk.Align.Center;

        _configPlayerButtonBox.MarginStart = 40;
        _configPlayerButtonBox.MarginEnd = 40;
        _configButtonBox.Append(_configPlayerButtonBox);
        _configSpinButtonBox.MarginStart = 100;
        _configSpinButtonBox.MarginEnd = 100;
        _configButtonBox.Append(_configSpinButtonBox);

        _configPlayerButtonBox.Append(_buttonPlay);
        _configPlayerButtonBox.Append(_buttonPause);
        _configPlayerButtonBox.Append(_buttonStop);
        _configPlayerButtonBox.Append(_buttonRecord);

        if (_configSpinButtonBox.GetFirstChild() == null)
        {
            _sequenceNumberSpinButton.Hide();
            _configSpinButtonBox.Append(_sequenceNumberSpinButton);
        }

        _volumeBar.MarginStart = 20;
        _volumeBar.MarginEnd = 20;
        _configBarBox.Append(_volumeBar);
        _positionBar.MarginStart = 20;
        _positionBar.MarginEnd = 20;
        _configBarBox.Append(_positionBar);

        _mainBox.Append(_headerBar);
        _mainBox.Append(_popoverMenuBar);
        _mainBox.Append(_configButtonBox);
        _mainBox.Append(_configBarBox);
        _mainBox.Append(_playlistBox);
        _mainBox.Append(_pianoBox);
        _mainBox.Append(_sequencedAudioTrackInfoBox);
        _mainBox.Append(_sequencedAudioListBox);

        SetContent(_mainBox);

        Instance = this;

        _windowGroup.AddWindow(this);

        // Ensures the entire application gets closed when the main window is closed
        OnCloseRequest += MainWindow_CloseRequest;
    }

    // Closes the entire application
    private bool MainWindow_CloseRequest(Gtk.Window sender, EventArgs args)
    {
        DisposeEngine(); // Engine must be disposed first, otherwise the window will softlock when closing
        Dispose();
        return false;
    }

    #region Widget Display Toggle Methods
    private void Playlist_GetTiled(object sender, EventArgs args)
    {
        if (_playlistWidgetTiledAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_playlistWindow is not null)
        {
            if (_playlistWindow.WidgetBox.GetFirstChild() is not null)
            {
                _playlistWindow.WidgetBox.Remove(_playlistSelector);
            }
            _playlistWindow.OnCloseRequest -= PlaylistWindow_CloseRequest;
            _playlistWindow.Dispose();
            _playlistWindow.Close();
            if (_playlistWindow is not null)
            {
                _playlistWindow = null!;
            }
        }

        _playlistBox.Append(_playlistSelector);

        _playlistWidgetTiledAction.SetState(GLib.Variant.NewBoolean(true));
        _playlistWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _playlistWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void Playlist_GetWindowed(object sender, EventArgs args)
    {
        if (_playlistWidgetWindowedAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_playlistBox.GetFirstChild() is not null)
        {
            _playlistBox.Remove(_playlistSelector);
        }
        _playlistWindow ??= WidgetWindow.CreateWindow(_playlistSelector);
        _playlistWindow.OnCloseRequest += PlaylistWindow_CloseRequest;
        _playlistWindow.Present();

        _playlistWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _playlistWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(true));
        _playlistWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void Playlist_Hide(object sender, EventArgs args)
    {
        if (_playlistWidgetHideAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_playlistWindow is not null)
        {
            if (_playlistWindow.WidgetBox.GetFirstChild() is not null)
            {
                _playlistWindow.WidgetBox.Remove(_playlistSelector);
            }
            _playlistWindow.OnCloseRequest -= PlaylistWindow_CloseRequest;
            _playlistWindow.Dispose();
            _playlistWindow.Close();
            if (_playlistWindow is not null)
            {
                _playlistWindow = null!;
            }
        }
        if (_playlistBox.GetFirstChild() is not null)
        {
            _playlistBox.Remove(_playlistSelector);
        }

        _playlistWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _playlistWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _playlistWidgetHideAction.SetState(GLib.Variant.NewBoolean(true));
    }

    private void SeqAudioPiano_GetTiled(object sender, EventArgs args)
    {
        if (_seqAudioPianoWidgetTiledAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_seqAudioPianoWindow is not null)
        {
            if (_seqAudioPianoWindow.WidgetBox.GetFirstChild() is not null)
            {
                _seqAudioPianoWindow.WidgetBox.Remove(_piano);
            }
            _seqAudioPianoWindow.OnCloseRequest -= SeqAudioPianoWindow_CloseRequest;
            _seqAudioPianoWindow.Dispose();
            _seqAudioPianoWindow.Close();
            if (_seqAudioPianoWindow is not null)
            {
                _seqAudioPianoWindow = null!;
            }
        }

        _pianoBox.Append(_piano);

        _seqAudioPianoWidgetTiledAction.SetState(GLib.Variant.NewBoolean(true));
        _seqAudioPianoWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioPianoWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void SeqAudioPiano_GetWindowed(object sender, EventArgs args)
    {
        if (_seqAudioPianoWidgetWindowedAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_pianoBox.GetFirstChild() is not null)
        {
            _pianoBox.Remove(_piano);
        }
        _seqAudioPianoWindow ??= WidgetWindow.CreateWindow(_piano);
        _seqAudioPianoWindow.HeightRequest = 60;
        _seqAudioPianoWindow.OnCloseRequest += SeqAudioPianoWindow_CloseRequest;
        _seqAudioPianoWindow.Present();

        _seqAudioPianoWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioPianoWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(true));
        _seqAudioPianoWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void SeqAudioPiano_Hide(object sender, EventArgs args)
    {
        if (_seqAudioPianoWidgetHideAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_seqAudioPianoWindow is not null)
        {
            if (_seqAudioPianoWindow.WidgetBox.GetFirstChild() is not null)
            {
                _seqAudioPianoWindow.WidgetBox.Remove(_piano);
            }
            _seqAudioPianoWindow.OnCloseRequest -= SeqAudioPianoWindow_CloseRequest;
            _seqAudioPianoWindow.Dispose();
            _seqAudioPianoWindow.Close();
            if (_seqAudioPianoWindow is not null)
            {
                _seqAudioPianoWindow = null!;
            }
        }
        if (_pianoBox.GetFirstChild() is not null)
        {
            _pianoBox.Remove(_piano);
        }

        _seqAudioPianoWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioPianoWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioPianoWidgetHideAction.SetState(GLib.Variant.NewBoolean(true));
    }

    private void SeqAudioTrackInfo_GetTiled(object sender, EventArgs args)
    {
        if (_seqAudioTrackInfoWidgetTiledAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_sequencedAudioTrackInfoWindow is not null)
        {
            if (_sequencedAudioTrackInfoWindow.WidgetBox.GetFirstChild() is not null)
            {
                _sequencedAudioTrackInfoWindow.WidgetBox.Remove(_sequencedAudioTrackInfo);
            }
            _sequencedAudioTrackInfoWindow.OnCloseRequest -= SeqAudioTrackInfoWindow_CloseRequest;
            _sequencedAudioTrackInfoWindow.Dispose();
            _sequencedAudioTrackInfoWindow.Close();
            if (_sequencedAudioTrackInfoWindow is not null)
            {
                _sequencedAudioTrackInfoWindow = null!;
            }
        }

        _sequencedAudioTrackInfoBox.Append(_sequencedAudioTrackInfo);

        _seqAudioTrackInfoWidgetTiledAction.SetState(GLib.Variant.NewBoolean(true));
        _seqAudioTrackInfoWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioTrackInfoWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void SeqAudioTrackInfo_GetWindowed(object sender, EventArgs args)
    {
        if (_seqAudioTrackInfoWidgetWindowedAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_sequencedAudioTrackInfoBox.GetFirstChild() is not null)
        {
            _sequencedAudioTrackInfoBox.Remove(_sequencedAudioTrackInfo);
        }
        _sequencedAudioTrackInfoWindow ??= WidgetWindow.CreateWindow(_sequencedAudioTrackInfo);
        _sequencedAudioTrackInfoWindow.OnCloseRequest += SeqAudioTrackInfoWindow_CloseRequest;
        _sequencedAudioTrackInfoWindow.Present();

        _seqAudioTrackInfoWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioTrackInfoWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(true));
        _seqAudioTrackInfoWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void SeqAudioTrackInfo_Hide(object sender, EventArgs args)
    {
        if (_seqAudioTrackInfoWidgetHideAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_sequencedAudioTrackInfoWindow is not null)
        {
            if (_sequencedAudioTrackInfoWindow.WidgetBox.GetFirstChild() is not null)
            {
                _sequencedAudioTrackInfoWindow.WidgetBox.Remove(_sequencedAudioTrackInfo);
            }
            _sequencedAudioTrackInfoWindow.OnCloseRequest -= SeqAudioTrackInfoWindow_CloseRequest;
            _sequencedAudioTrackInfoWindow.Dispose();
            _sequencedAudioTrackInfoWindow.Close();
            if (_sequencedAudioTrackInfoWindow is not null)
            {
                _sequencedAudioTrackInfoWindow = null!;
            }
        }
        if (_sequencedAudioTrackInfoBox.GetFirstChild() is not null)
        {
            _sequencedAudioTrackInfoBox.Remove(_sequencedAudioTrackInfo);
        }

        _seqAudioTrackInfoWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioTrackInfoWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioTrackInfoWidgetHideAction.SetState(GLib.Variant.NewBoolean(true));
    }

    private void SeqAudioList_GetTiled(object sender, EventArgs args)
    {
        if (_seqAudioListWidgetTiledAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_sequencedAudioListWindow is not null)
        {
            if (_sequencedAudioListWindow.WidgetBox.GetFirstChild() is not null)
            {
                _sequencedAudioListWindow.WidgetBox.Remove(_sequencedAudioList);
            }
            _sequencedAudioListWindow.OnCloseRequest -= SeqAudioListWindow_CloseRequest;
            _sequencedAudioListWindow.Dispose();
            _sequencedAudioListWindow.Close();
            if (_sequencedAudioListWindow is not null)
            {
                _sequencedAudioListWindow = null!;
            }
        }

        _sequencedAudioListBox.Append(_sequencedAudioList);

        _seqAudioListWidgetTiledAction.SetState(GLib.Variant.NewBoolean(true));
        _seqAudioListWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioListWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void SeqAudioList_GetWindowed(object sender, EventArgs args)
    {
        if (_seqAudioListWidgetWindowedAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_sequencedAudioListBox.GetFirstChild() is not null)
        {
            _sequencedAudioListBox.Remove(_sequencedAudioList);
        }
        _sequencedAudioListWindow ??= WidgetWindow.CreateWindow(_sequencedAudioList);
        _sequencedAudioListWindow.OnCloseRequest += SeqAudioListWindow_CloseRequest;
        _sequencedAudioListWindow.Present();

        _seqAudioListWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioListWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(true));
        _seqAudioListWidgetHideAction.SetState(GLib.Variant.NewBoolean(false));
    }

    private void SeqAudioList_Hide(object sender, EventArgs args)
    {
        if (_seqAudioListWidgetHideAction.GetState()!.GetBoolean() == true)
        {
            return;
        }

        if (_sequencedAudioListWindow is not null)
        {
            if (_sequencedAudioListWindow.WidgetBox.GetFirstChild() is not null)
            {
                _sequencedAudioListWindow.WidgetBox.Remove(_sequencedAudioList);
            }
            _sequencedAudioListWindow.OnCloseRequest -= SeqAudioListWindow_CloseRequest;
            _sequencedAudioListWindow.Dispose();
            _sequencedAudioListWindow.Close();
            if (_sequencedAudioListWindow is not null)
            {
                _sequencedAudioListWindow = null!;
            }
        }
        if (_sequencedAudioListBox.GetFirstChild() is not null)
        {
            _sequencedAudioListBox.Remove(_sequencedAudioList);
        }

        _seqAudioListWidgetTiledAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioListWidgetWindowedAction.SetState(GLib.Variant.NewBoolean(false));
        _seqAudioListWidgetHideAction.SetState(GLib.Variant.NewBoolean(true));
    }

    #region Widget Window Closure Methods

    private bool PlaylistWindow_CloseRequest(Gtk.Window sender, EventArgs args)
    {
        Playlist_GetTiled(sender, args);
        return false;
    }

    private bool SeqAudioPianoWindow_CloseRequest(Gtk.Window sender, EventArgs args)
    {
        SeqAudioPiano_GetTiled(sender, args);
        return false;
    }

    private bool SeqAudioTrackInfoWindow_CloseRequest(Gtk.Window sender, EventArgs args)
    {
        SeqAudioTrackInfo_GetTiled(sender, args);
        return false;
    }

    private bool SeqAudioListWindow_CloseRequest(Gtk.Window sender, EventArgs args)
    {
        SeqAudioList_GetTiled(sender, args);
        return false;
    }
    #endregion

    #region Widget Menu Enable and Disable Methods
    private void PlaylistWidgetAction_IsEnabled(bool enabled)
    {
        _playlistWidgetTiledAction.Enabled = enabled;
        _playlistWidgetWindowedAction.Enabled = enabled;
        _playlistWidgetHideAction.Enabled = enabled;
    }

    private void PianoWidgetAction_IsEnabled(bool enabled)
    {
        _seqAudioPianoWidgetTiledAction.Enabled = enabled;
        _seqAudioPianoWidgetWindowedAction.Enabled = enabled;
        _seqAudioPianoWidgetHideAction.Enabled = enabled;
    }

    private void SeqAudioTrackInfoWidgetAction_IsEnabled(bool enabled)
    {
        _seqAudioTrackInfoWidgetTiledAction.Enabled = enabled;
        _seqAudioTrackInfoWidgetWindowedAction.Enabled = enabled;
        _seqAudioTrackInfoWidgetHideAction.Enabled = enabled;
    }

    private void SeqAudioListWidgetAction_IsEnabled(bool enabled)
    {
        _seqAudioListWidgetTiledAction.Enabled = enabled;
        _seqAudioListWidgetWindowedAction.Enabled = enabled;
        _seqAudioListWidgetHideAction.Enabled = enabled;
    }
    #endregion

    #region Widget Auto-Close when Unsupported, Re-Open when Supported and Already Selected
    private void CheckWidgetPlaylist()
    {
        if ((_playlistWidgetTiledAction.Enabled =
            _playlistWidgetWindowedAction.Enabled =
            _playlistWidgetHideAction.Enabled) == true
        )
        {
            if (_playlistWidgetTiledAction.GetState()!.GetBoolean() == true)
            {

                if (_playlistWindow is not null)
                {
                    if (_playlistWindow.WidgetBox.GetFirstChild() is not null)
                    {
                        _playlistWindow.WidgetBox.Remove(_playlistSelector);
                    }
                    _playlistWindow.OnCloseRequest -= PlaylistWindow_CloseRequest;
                    _playlistWindow.Dispose();
                    _playlistWindow.Close();
                    if (_playlistWindow is not null)
                    {
                        _playlistWindow = null!;
                    }
                }

                _playlistBox.Append(_playlistSelector);
            }
            else if (_playlistWidgetWindowedAction.GetState()!.GetBoolean() == true)
            {
                if (_playlistBox.GetFirstChild() is not null)
                {
                    _playlistBox.Remove(_playlistSelector);
                }
                _playlistWindow ??= WidgetWindow.CreateWindow(_playlistSelector);
                _playlistWindow.OnCloseRequest += PlaylistWindow_CloseRequest;
                _playlistWindow.Present();

            }
        }
        else
        {
            if (_playlistWindow is not null)
            {
                if (_playlistWindow.WidgetBox.GetFirstChild() is not null)
                {
                    _playlistWindow.WidgetBox.Remove(_playlistSelector);
                }
                _playlistWindow.OnCloseRequest -= PlaylistWindow_CloseRequest;
                _playlistWindow.Dispose();
                _playlistWindow.Close();
                if (_playlistWindow is not null)
                {
                    _playlistWindow = null!;
                }
            }
            if (_playlistBox.GetFirstChild() is not null)
            {
                _playlistBox.Remove(_playlistSelector);
            }
        }
    }
    #endregion
    #endregion

    private void OpenPreferences(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        _preferences = null;
        _preferences = Preferences.NewWithProperties([]);
        SetSensitive(false);
        SetModal(false);
        SetFocus(_preferences);
        _preferences.SetModal(true);
        _preferences.SetSensitive(true);

        _preferences.Present();
    }

    // When the value is changed on the volume scale
    private void VolumeBar_ValueChanged(object sender, EventArgs e)
    {
        Engine.Instance!.Mixer!.SetVolume((float)(_volumeBar.Adjustment!.Value / _volumeBar.Adjustment.Upper));
    }

    // Sets the volume scale to the specified position
    public void SetVolumeBar(float volume)
    {
        _volumeBar.Adjustment!.Value = (int)(volume * _volumeBar.Adjustment.Upper);
        _volumeBar.OnValueChanged += VolumeBar_ValueChanged;
    }

    private bool _positionBarFree = true;
    private bool _positionBarDebug = false;

    private void PositionBar_MouseButtonPress(object sender, EventArgs args)
    {
        if (_positionBarDebug)
        {
            Debug.WriteLine(sender.ToString() + " | " + args.ToString() + " | _positionBarFree: " + _positionBarFree.ToString());
        }
        _positionBarFree = false;
    }
    private void PositionBar_MouseButtonRelease(object sender, EventArgs args)
    {
        // if (args == EventArgs.Empty)
        // {
        //     return;
        // }

        if (_positionBarDebug)
        {
            Debug.WriteLine(sender.ToString() + " | " + args.ToString() + " | _positionBarFree: " + _positionBarFree.ToString());
        }

        if (!_positionBarFree)
        {
            Engine.Instance!.Player.SetSongPosition((long)_positionBar.Adjustment!.Value); // Sets the value based on the position when mouse button is released
            _positionBarFree = true; // Sets _positionBarFree to true when mouse button is released
            if (Engine.Instance!.Player.State is PlayerState.Playing)
            {
                LetUIKnowPlayerIsPlaying(); // This method will run the void that tells the UI that the player is playing a track
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

    }
    private void PositionBar_MouseButtonOnBegin(object sender, EventArgs args)
    {
        if (_positionBarDebug)
        {
            Debug.WriteLine(sender.ToString() + " | " + args.ToString() + " | _positionBarFree: " + _positionBarFree.ToString());
        }
        _positionBarFree = false;
    }
    private void PositionBar_MouseButtonOnEnd(object sender, EventArgs args)
    {
        if (_positionBarDebug)
        {
            Debug.WriteLine(sender.ToString() + " | " + args.ToString() + " | _positionBarFree: " + _positionBarFree.ToString());
        }
        _positionBarFree = true;
    }
    private bool PositionBar_ChangeValue(object sender, EventArgs args)
    {
        if (_positionBarDebug)
        {
            Debug.WriteLine(sender.ToString() + " | " + args.ToString() + " | _positionBarFree: " + _positionBarFree.ToString());
        }
        return _positionBarFree = false;
    }
    private void PositionBar_MoveSlider(object sender, EventArgs args)
    {
        if (_positionBarDebug)
        {
            Debug.WriteLine(sender.ToString() + " | " + args.ToString() + " | _positionBarFree: " + _positionBarFree.ToString());
        }
        UpdatePositionIndicators(Engine.Instance!.Player.ElapsedTicks);
        _positionBarFree = false;
    }
    private void PositionBar_ValueChanged(object sender, EventArgs args)
    {
        if (Engine.Instance is not null)
        {
            UpdatePositionIndicators(Engine.Instance!.Player.ElapsedTicks); // Sets the value based on the position when mouse button is released
        }
    }

    private void SequenceNumberSpinButton_ValueChanged(object sender, EventArgs e)
    {
        _sequenceNumberSpinButton.OnValueChanged -= SequenceNumberSpinButton_ValueChanged;
        _sequenceNumberSpinButton.OnChangeValue -= SequenceNumberSpinButton_ChangeValue;
        int index = (int)_sequenceNumberSpinButton.Adjustment!.Value;
        if (Engine.Instance is not null)
        {
            _sequencedAudioList.SelectRow(index);
            // _sequencedAudioList.ColumnView!.ScrollTo((uint)index, null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
            if (Engine.Instance!.Config.Playlists is not null)
            {
                PlaylistSongStringChanged(index);
            }
            if (!_preventAutoplay)
            {
                _autoplay = true;
            }
            else
            {
                _preventAutoplay = false;
            }
            LoadSong(index);
            _autoplay = false;
        }
        _sequenceNumberSpinButton.OnChangeValue += SequenceNumberSpinButton_ChangeValue;
        _sequenceNumberSpinButton.OnValueChanged += SequenceNumberSpinButton_ValueChanged;
    }

    private void SequenceNumberSpinButton_ChangeValue(Gtk.SpinButton sender, Gtk.SpinButton.ChangeValueSignalArgs args)
    {
        _sequenceNumberSpinButton.OnValueChanged -= SequenceNumberSpinButton_ValueChanged;
        _sequenceNumberSpinButton.OnChangeValue -= SequenceNumberSpinButton_ChangeValue;
        int index = (int)_sequenceNumberSpinButton.Adjustment!.Value;
        if (Engine.Instance is not null)
        {
            _sequencedAudioList.SelectRow(index);
            // _sequencedAudioList.ColumnView!.ScrollTo((uint)index, null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
            if (Engine.Instance!.Config.Playlists is not null)
            {
                PlaylistSongStringChanged(index);
            }
            _autoplay = true;
            LoadSong(index);
            _autoplay = false;
        }
        _sequenceNumberSpinButton.OnChangeValue += SequenceNumberSpinButton_ChangeValue;
        _sequenceNumberSpinButton.OnValueChanged += SequenceNumberSpinButton_ValueChanged;
    }

    internal static string GetProgramName()
    {
        return ConfigUtils.PROGRAM_NAME;
    }

    private void SetSongToProgramTitle(List<Config.Song> songs, int songIndex)
    {
        Title = $"{GetProgramName()} - {songs[songIndex].Name}";
    }

    // For SequencedAudio_List
    internal void ChangeIndex(int index)
    {
        // First, check if the index is identical to current song index
        // to prevent it from unexpectedly stopping
        if (index != _curSong)
        {
            _sequencedAudioList.HasSelectedRow = true;
            _preventAutoplay = true;
            _sequenceNumberSpinButton.Value = index;
            if (Engine.Instance!.Config.Playlists is not null)
            {
                PlaylistSongStringChanged(index);
            }
            _sequencedAudioList.HasSelectedRow = false;
        }
    }

    // For SequencedAudio_List
    internal void CheckIndex(int index)
    {
        // First, check if the index is identical to current song index
        // to prevent it from unexpectedly stopping
        if (index != _curSong)
        {
            _preventAutoplay = true;
            _sequenceNumberSpinButton.Value = index;
            if (Engine.Instance is not null)
            {
                if (Engine.Instance!.Config.Playlists is not null)
                {
                    PlaylistSongStringChanged(index);
                }
            }
        }
    }

    private void ResetPlaylistStuff(bool spinButtonAndPlaylistSelectEnabled)
    {
        if (Engine.Instance != null)
        {
            Engine.Instance.Player.ShouldFadeOut = false;
        }
        _curSong = -1;
        _playlist = null;
        _endPlaylistAction.Enabled = false;
        _sequenceNumberSpinButton.Sensitive =
            _playlistSelector.PlaylistSelectDropDown!.Sensitive =
            _playlistSelector.PlaylistSongBox!.Sensitive =
            spinButtonAndPlaylistSelectEnabled;
    }
    private void ButtonPlayPlist_Clicked(object sender, EventArgs e)
    {
        _playlistSelector.ButtonPlayPlist!.OnClicked -= ButtonPlayPlist_Clicked;
        if (_playlistSelector.ButtonPlayPlist!.Active)
        {
            PlayCurrentPlaylist(sender, e);
        }
        else
        {
            EndCurrentPlaylist(sender, e);
        }
        _playlistSelector.ButtonPlayPlist!.OnClicked += ButtonPlayPlist_Clicked;
    }
    private void ButtonPlistStyle_Clicked(object sender, EventArgs e)
    {
        if (GlobalConfig.Instance.PlaylistMode is PlaylistMode.Sequential)
        {
            GlobalConfig.Instance.PlaylistMode = PlaylistMode.Random;
            _playlistSelector.ButtonPlistStyle!.SetIconName("media-playlist-shuffle-symbolic");
        }
        else if (GlobalConfig.Instance.PlaylistMode is PlaylistMode.Random)
        {
            GlobalConfig.Instance.PlaylistMode = PlaylistMode.Sequential;
            _playlistSelector.ButtonPlistStyle!.SetIconName("media-playlist-consecutive-symbolic");
        }
    }
    private void CheckPlaylistMode()
    {
        if (GlobalConfig.Instance.PlaylistMode is PlaylistMode.Sequential)
        {
            GlobalConfig.Instance.PlaylistMode = PlaylistMode.Sequential;
            _playlistSelector.ButtonPlistStyle!.SetIconName("media-playlist-consecutive-symbolic");
        }
        else if (GlobalConfig.Instance.PlaylistMode is PlaylistMode.Random)
        {
            GlobalConfig.Instance.PlaylistMode = PlaylistMode.Random;
            _playlistSelector.ButtonPlistStyle!.SetIconName("media-playlist-shuffle-symbolic");
        }
    }
    private void PlayCurrentPlaylist(object sender, EventArgs e)
    {
        Config.Playlist playlist = _playlistSelector.GetPlaylist();
        FlexibleDialog.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), Strings.MenuPlaylist, FlexibleDialog.ButtonsType.YesNo);
        FlexibleDialog.OnResponse += ResponseSelected;
        void ResponseSelected(FlexibleDialog.ResponseSelected response)
        {
            FlexibleDialog.OnResponse -= ResponseSelected;
            if (response == FlexibleDialog.ResponseSelected.Yes)
            {
                if (playlist.Songs.Count > 0)
                {
                    _playPlaylistAction.OnActivate -= PlayCurrentPlaylist;
                    _backupSong = _curSong;
                    ResetPlaylistStuff(false);
                    Engine.Instance!.Player.ShouldFadeOut = true;
                    Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
                    _playPlaylistAction.Enabled = false;
                    _endPlaylistAction.Enabled = true;
                    _preventAutoplay = false;
                    _autoplay = true;
                    _playlist = new PlayingPlaylist(playlist);
                    _playlist.SetAndLoadNextSong();
                    _endPlaylistAction.OnActivate += EndCurrentPlaylist;
                }
                else
                {
                    _playlistSelector.ButtonPlayPlist!.Active = false;
                }
            }
            else
            {
                _playlistSelector.ButtonPlayPlist!.Active = false;
            }
        }
    }
    private void EndCurrentPlaylist(object sender, EventArgs e)
    {
        FlexibleDialog.Show(Strings.EndPlaylistBody, Strings.MenuPlaylist, FlexibleDialog.ButtonsType.YesNo);
        FlexibleDialog.OnResponse += ResponseSelected;
        void ResponseSelected(FlexibleDialog.ResponseSelected response)
        {
            FlexibleDialog.OnResponse -= ResponseSelected;
            if (response == FlexibleDialog.ResponseSelected.Yes)
            {
                _endPlaylistAction.OnActivate -= EndCurrentPlaylist;
                _autoplay = false;
                _preventAutoplay = true;
                ResetPlaylistStuff(true);
                Stop();
                if (_sequenceNumberSpinButton.Value != _backupSong)
                {
                    _sequenceNumberSpinButton.Value = _backupSong;
                    _backupSong = -1;
                }
                _playPlaylistAction.Enabled = true;
                _playPlaylistAction.OnActivate += PlayCurrentPlaylist;
            }
            else
            {
                _playlistSelector.ButtonPlayPlist!.Active = true;
            }
        }
    }

    private void OnPlaylistStringSelected(GObject.Object sender, NotifySignalArgs args)
    {
        var name = args.Pspec.GetName();
        if (_playlistSelector.PlaylistSelectDropDown!.SelectedItem is not null)
        {
            if (args.Pspec.GetName() == "selected" || args.Pspec.GetName() == "root")
            {
                _playlistSelector.PlaylistSelectDropDown.OnNotify -= OnPlaylistStringSelected;
                _playlistSelector.PlaylistSelectSongDropDown!.OnNotify -= OnPlaylistSongStringSelected;
                _autoplay = false;  // Must be set to false first
                CheckPlaylistItem();  // Check the playlist item, to set the dropdown to it's first song in the playlist
                _playlistSelector.PlaylistStringSelect();  // Selects the playlist item
                _playlistChanged = true;  // We set this, so that the autoplay doesn't get set while changing playlists
                _sequencedAudioList.SelectRow(_playlistSelector.GetSongIndex(_playlistSelector.PlaylistSelectDropDown.Selected));
                if (!_playlistChanged)
                {
                    // _sequencedAudioList.ColumnView!.ScrollTo((uint)_playlistSelector.GetSongIndex(_playlistSelector.PlaylistDropDown.Selected), null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
                }
                if (_sequenceNumberSpinButton.Value != _playlistSelector.GetSongIndex(_playlistSelector.PlaylistSelectDropDown.Selected))
                {
                    _preventAutoplay = true;
                    _sequenceNumberSpinButton.Value = _playlistSelector.GetSongIndex(_playlistSelector.PlaylistSelectDropDown.Selected);
                }
                _playlistChanged = false;  // Now we can set it back to false
                _playlistSelector.PlaylistSelectDropDown.OnNotify += OnPlaylistStringSelected;
                _playlistSelector.PlaylistSelectSongDropDown.OnNotify += OnPlaylistSongStringSelected;
            }
        }
    }

    private void OnPlaylistSongStringSelected(GObject.Object sender, NotifySignalArgs args)
    {
        // Debug.WriteLine(args.Pspec.GetName());
        if (_playlistSelector.PlaylistSelectSongDropDown!.SelectedItem is not null)
        {
            if (args.Pspec.GetName() == "selected")
            {
                _playlistSelector.PlaylistSelectDropDown!.OnNotify -= OnPlaylistStringSelected;
                _playlistSelector.PlaylistSelectSongDropDown.OnNotify -= OnPlaylistSongStringSelected;
                if (_playlistSelector.PlaylistSelectDropDown.Selected != _playlistSelector.SelectedPlaylistIndex)
                {
                    Stop();
                }

                if (_playlistSelector.PlaylistSelectSongDropDown.Selected != _playlistSelector.SelectedSongIndex)
                {
                    CheckPlaylistItem();
                    var selectedItem = (PlaylistSelector.PlaylistData)_playlistSelector.PlaylistSelectSongDropDown.SelectedItem;
                    var selectedItemName = selectedItem.GetTitle();
                    foreach (var song in _playlistSelector.Songs!)
                    {
                        if (song.Name.Equals(selectedItemName))
                        {
                            _playlistSelector.SelectedSongIndex = _playlistSelector.PlaylistSelectSongDropDown.Selected;
                            if (!_playlistChanged)
                            {
                                _autoplay = true;
                            }

                            _sequencedAudioList.SelectRow(_playlistSelector.GetSongIndex(_playlistSelector.PlaylistSelectDropDown.Selected));
                            if (!_playlistChanged)
                            {
                                // _sequencedAudioList.ColumnView!.ScrollTo((uint)_playlistSelector.GetSongIndex(_playlistSelector.PlaylistDropDown.Selected), null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
                            }
                            _sequenceNumberSpinButton.Value = _playlistSelector.GetSongIndex(_playlistSelector.PlaylistSelectDropDown.Selected);
                            _autoplay = false;
                        }
                    }
                }
                _playlistSelector.PlaylistSelectDropDown.OnNotify += OnPlaylistStringSelected;
                _playlistSelector.PlaylistSelectSongDropDown.OnNotify += OnPlaylistSongStringSelected;
            }
        }
    }
    private void PlaylistSongStringChanged(int index)
    {
        if (_playlistSelector.PlaylistSelectSongDropDown!.SelectedItem is not null)
        {
            foreach (var song in _playlistSelector.Songs!)
            {
                if (song.Index.Equals(index))
                {
                    _playlistSelector.PlaylistSelectSongDropDown.SetSelected(_playlistSelector.GetPlaylistSongIndex(index));
                }
            }
        }
    }

    public void SetSong(int index)
    {
        if (_sequenceNumberSpinButton.Value != index)
        {
            _sequenceNumberSpinButton.Value = index;
        }
        else
        {
            if (!_preventAutoplay)
            {
                _autoplay = true;
            }
            else
            {
                _preventAutoplay = false;
            }
            LoadSong(index);
        }
    }

    public void LoadSong(int index)
    {
        _curSong = index;

        Stop();
        Title = GetProgramName();
        bool success;
        if (Engine.Instance == null)
        {
            return; // Prevents referencing a null Engine.Instance when the engine is being disposed, especially while main window is being closed
        }
        Player player = Engine.Instance!.Player;
        Config cfg = Engine.Instance.Config;
        try
        {
            if (Engine.Instance.Config.Playlists is not null && Engine.Instance.Config.Playlists.Count != 0)
            {
                List<Config.Song> songs = cfg.Playlists![^1].Songs; // Complete "All Songs" playlist is present in all configs at the last index value
                int songIndex = songs.FindIndex(s => s.Index == index);
                if (songIndex != -1)
                {
                    SetSongToProgramTitle(songs, songIndex); // Done! It's now a func
                    PlaylistSongStringChanged(index);
                    CheckPlaylistItem();
                }
            }
            else
            {
                List<Config.Song> songs = cfg.InternalSongNames![0].Songs;
                int songIndex = songs.FindIndex(s => s.Index == index);
                if (songIndex != -1)
                {
                    SetSongToProgramTitle(songs, songIndex);
                }
            }
            player.LoadSong(index);
            success = Engine.Instance.Player.LoadedSong is not null; // Done! Every Engine now disables playback and remains null when there's no tracks in the sequence
        }
        catch (Exception ex)
        {
            if (ex is IndexOutOfRangeException && Engine.Instance is DSEEngine)
            {
                FlexibleDialog.Show(ex, string.Format(Strings.ErrorLoadSong, Engine.Instance!.Config.GetSongName(index)));
            }
            else if (ex is DSEInvalidNoteException)
            {
                var dseEx = ex as DSEInvalidNoteException;
                FlexibleDialog.Show($"Attempted to read a note that was out of range.\n\nTrack Index: {dseEx!.TrackIndex}\nCommand Offset: {string.Format("0x{0:X}", dseEx.Offset)}\nAttempted note value: {dseEx.Note} ({string.Format("0x{0:X}", dseEx.Note)})", "Unable to load song.");
            }
            else
            {
                FlexibleDialog.Show(ex, string.Format(Strings.ErrorLoadSong, Engine.Instance!.Config.GetSongName(index)));
            }
            success = false;
        }

        ILoadedSong? loadedSong = player.LoadedSong; // LoadedSong is still null when there are no tracks
        if (success)
        {
            if (_buttonPlay.Sensitive is false)
            {
                _buttonPlay.Sensitive = true;
            }
            _positionBar.Adjustment!.Upper = loadedSong!.MaxTicks;
            _positionBar.SetRange(0, loadedSong.MaxTicks);
            SequencedAudio_TrackInfo.SetNumTracks(loadedSong.Events.Length);
            _sequencedAudioTrackInfo.AddTrackInfo();
            if (_autoplay)
            {
                Play();
            }
        }
        else
        {
            _buttonPlay.Sensitive = false;
            SequencedAudio_TrackInfo.SetNumTracks(0);
        }
        _trackEditor?.UpdateTracks();
        _positionBar.Sensitive = _exportWAVAction.Enabled = success;
        _exportMIDIAction.Enabled = success && MP2KEngine.MP2KInstance is not null;
        _exportDLSAction.Enabled = _exportSF2Action.Enabled = success && AlphaDreamEngine.AlphaDreamInstance is not null;
    }

    private void OpenDSE(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.OnPathChanged += LoadSWD;
        GTK4Utils.CreateLoadDialog(["*.swd"], Strings.MenuOpenSWD, Strings.FilterOpenSWD);

        void LoadSWD(string swdPath)
        {
            GTK4Utils.OnPathChanged -= LoadSWD;
            if (swdPath is null)
            {
                return;
            }

            GTK4Utils.OnPathChanged += LoadFiles;
            GTK4Utils.CreateLoadDialog(Strings.MenuOpenSMD);

            void LoadFiles(string smdPath)
            {
                GTK4Utils.OnPathChanged -= LoadFiles;
                if (smdPath is null)
                {
                    return;
                }
                if (Engine.Instance is not null)
                {
                    DisposeEngine();
                }
                try
                {
                    _ = new DSEEngine(swdPath, smdPath, true);
                }
                catch (Exception ex)
                {
                    FlexibleDialog.Show(ex, Strings.ErrorOpenDSE);
                    return;
                }
                DSEConfig config = DSEEngine.DSEInstance!.Config;
                _sequencedAudioList.ChangeColumns();
                FinishLoading(config.SMDFiles.Length);
                _sequenceNumberSpinButton.Visible = false;
                _sequenceNumberSpinButton.Hide();
                _buttonRecord.Sensitive = true;
                _playlistSelector.PlaylistSelectDropDown!.Sensitive =
                    _playlistSelector.ButtonPlayPlist!.Sensitive =
                    _playlistSelector.ButtonPlistStyle!.Sensitive =
                    _playlistSelector.ButtonPrevPlistSong!.Sensitive =
                    _playlistSelector.PlaylistSelectSongDropDown!.Sensitive =
                    _playlistSelector.ButtonNextPlistSong!.Sensitive = false;
                _midiConverterDialogAction.Enabled = false;
                _trackEditorAction.Enabled = true;
                _exportASMAction.Enabled = false;
                _exportMIDIAction.Enabled = false;
                _exportDLSAction.Enabled = false;
                _exportSF2Action.Enabled = false;
                _playPlaylistAction.Enabled =
                    _endPlaylistAction.Enabled = false;
                PlaylistWidgetAction_IsEnabled(false);
                PianoWidgetAction_IsEnabled(true);
                SeqAudioTrackInfoWidgetAction_IsEnabled(true);
                SeqAudioListWidgetAction_IsEnabled(true);
                CheckWidgetPlaylist();
            }
        }
    }
    private void OpenSDAT(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.OnPathChanged += LoadFile;
        GTK4Utils.CreateLoadDialog(["*.sdat"], Strings.MenuOpenSDAT, Strings.FilterOpenSDAT);

        void LoadFile(string path)
        {
            GTK4Utils.OnPathChanged -= LoadFile;
            if (path is null)
            {
                return;
            }
            if (Engine.Instance is not null)
            {
                DisposeEngine();
            }
            try
            {
                using FileStream stream = File.OpenRead(path);
                _ = new SDATEngine(new SDAT(stream));
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, Strings.ErrorOpenSDAT);
                return;
            }

            SDATConfig config = SDATEngine.SDATInstance!.Config;
            _sequencedAudioList.ChangeColumns();
            FinishLoading(config.SDAT.INFOBlock.SequenceInfos.NumEntries);
            _sequenceNumberSpinButton.Visible = false;
            _sequenceNumberSpinButton.Hide();
            _buttonRecord.Sensitive = true;
            _playlistSelector.PlaylistSelectDropDown!.Sensitive =
                _playlistSelector.ButtonPlayPlist!.Sensitive =
                _playlistSelector.ButtonPlistStyle!.Sensitive =
                _playlistSelector.ButtonPrevPlistSong!.Sensitive =
                _playlistSelector.PlaylistSelectSongDropDown!.Sensitive =
                _playlistSelector.ButtonNextPlistSong!.Sensitive = false;
            _midiConverterDialogAction.Enabled = false;
            _trackEditorAction.Enabled = true;
            _exportASMAction.Enabled = false;
            _exportMIDIAction.Enabled = false;
            _exportDLSAction.Enabled = false;
            _exportSF2Action.Enabled = false;
            _playPlaylistAction.Enabled =
                _endPlaylistAction.Enabled = false;
            PlaylistWidgetAction_IsEnabled(false);
            PianoWidgetAction_IsEnabled(true);
            SeqAudioTrackInfoWidgetAction_IsEnabled(true);
            SeqAudioListWidgetAction_IsEnabled(true);
            CheckWidgetPlaylist();
        }
    }
    private void OpenAlphaDream(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.OnPathChanged += LoadFile;
        GTK4Utils.CreateLoadDialog(["*.gba", "*.srl"], Strings.MenuOpenAlphaDream, Strings.FilterOpenGBA);

        void LoadFile(string path)
        {
            GTK4Utils.OnPathChanged -= LoadFile;
            if (path is null)
            {
                return;
            }
            if (Engine.Instance is not null)
            {
                DisposeEngine();
            }
            try
            {
                _ = new AlphaDreamEngine(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, Strings.ErrorOpenAlphaDream);
                return;
            }

            AlphaDreamConfig config = AlphaDreamEngine.AlphaDreamInstance!.Config;
            _sequencedAudioList.ChangeColumns(true);
            FinishLoading(config.SongTableSizes[0]);
            _sequenceNumberSpinButton.Visible = true;
            _sequenceNumberSpinButton.Show();
            _buttonRecord.Sensitive = true;
            _playlistSelector.PlaylistSelectDropDown!.Sensitive =
                _playlistSelector.ButtonPlayPlist!.Sensitive =
                _playlistSelector.ButtonPlistStyle!.Sensitive =
                _playlistSelector.ButtonPrevPlistSong!.Sensitive =
                _playlistSelector.PlaylistSelectSongDropDown!.Sensitive =
                _playlistSelector.ButtonNextPlistSong!.Sensitive = true;
            _midiConverterDialogAction.Enabled = false;
            _trackEditorAction.Enabled = true;
            _exportASMAction.Enabled = false;
            _exportMIDIAction.Enabled = false;
            _exportDLSAction.Enabled = true;
            _exportSF2Action.Enabled = true;
            _playPlaylistAction.Enabled = true;
            _endPlaylistAction.Enabled = false;
            PlaylistWidgetAction_IsEnabled(true);
            PianoWidgetAction_IsEnabled(true);
            SeqAudioTrackInfoWidgetAction_IsEnabled(true);
            SeqAudioListWidgetAction_IsEnabled(true);
            CheckWidgetPlaylist();
        }
    }

    private void OpenMP2K(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.OnPathChanged += LoadFile;
        GTK4Utils.CreateLoadDialog(["*.gba", "*.srl"], Strings.MenuOpenMP2K, Strings.FilterOpenGBA);

        void LoadFile(string path)
        {
            GTK4Utils.OnPathChanged -= LoadFile;
            if (path is null)
            {
                return;
            }
            if (Engine.Instance is not null)
            {
                DisposeEngine();
            }

            try
            {
                _ = new MP2KEngine(File.ReadAllBytes(path), false);
            }
            catch (Exception ex)
            {
                DisposeEngine();
                FlexibleDialog.Show(ex, Strings.ErrorOpenMP2K);
                return;
            }

            MP2KConfig config = MP2KEngine.MP2KInstance!.Config;
            _sequencedAudioList.ChangeColumns(true);
            FinishLoading(config.SongTableSizes[0]);
            _sequenceNumberSpinButton.Visible = true;
            _sequenceNumberSpinButton.Show();
            _buttonRecord.Sensitive = true;
            _playlistSelector.PlaylistSelectDropDown!.Sensitive =
                _playlistSelector.ButtonPlayPlist!.Sensitive =
                _playlistSelector.ButtonPlistStyle!.Sensitive =
                _playlistSelector.ButtonPrevPlistSong!.Sensitive =
                _playlistSelector.PlaylistSelectSongDropDown!.Sensitive =
                _playlistSelector.ButtonNextPlistSong!.Sensitive = true;
            _midiConverterDialogAction.Enabled = true;
            _trackEditorAction.Enabled = true;
            _soundBankEditorAction.Enabled = true;
            _exportASMAction.Enabled = true;
            _exportMIDIAction.Enabled = true;
            _exportDLSAction.Enabled = false;
            _exportSF2Action.Enabled = false;
            _playPlaylistAction.Enabled = true;
            _endPlaylistAction.Enabled = false;
            PlaylistWidgetAction_IsEnabled(true);
            PianoWidgetAction_IsEnabled(true);
            SeqAudioTrackInfoWidgetAction_IsEnabled(true);
            SeqAudioListWidgetAction_IsEnabled(true);
            CheckWidgetPlaylist();
        }
    }
    private void ExportASM(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.CreateSaveDialog(Engine.Instance!.Config.GetSongName((int)_sequenceNumberSpinButton.Value), ["*.s"], Strings.MenuSaveASM, Strings.FilterSaveASM);
        GTK4Utils.OnPathChanged += SaveFile;

        static void SaveFile(string path)
        {
            GTK4Utils.OnPathChanged -= SaveFile;
            if (path is null)
            {
                return;
            }

            MP2KPlayer p = MP2KEngine.MP2KInstance!.Player;
            var args = new ASMSaveArgs(true);
            try
            {
                p.SaveAsASM(path, args);
                FlexibleDialog.Show(string.Format(Strings.SuccessSaveASM, path), Strings.SuccessSaveASM);
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex.Message, Strings.ErrorSaveASM);
            }
        }
    }
    private void ExportMIDI(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.CreateSaveDialog(Engine.Instance!.Config.GetSongName((int)_sequenceNumberSpinButton.Value), ["*.mid", "*.midi"], Strings.MenuSaveMIDI, Strings.FilterSaveMIDI);
        GTK4Utils.OnPathChanged += SaveFile;

        static void SaveFile(string path)
        {
            GTK4Utils.OnPathChanged -= SaveFile;
            if (path is null)
            {
                return;
            }

            MP2KPlayer p = MP2KEngine.MP2KInstance!.Player;
            var args = new MIDISaveArgs(true, false, [(0, (4, 4))]); // timeSignatures collection contains: (int AbsoluteTick, (byte Numerator, byte Denominator))

            try
            {
                p.SaveAsMIDI(path, args);
                FlexibleDialog.Show(string.Format(Strings.SuccessSaveMIDI, path), Strings.SuccessSaveMIDI);
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, Strings.ErrorSaveMIDI);
            }
        }
    }
    private void ExportDLS(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.CreateSaveDialog(Engine.Instance!.Config.GetGameName(), ["*.dls"], Strings.MenuSaveDLS, Strings.FilterSaveDLS);
        GTK4Utils.OnPathChanged += SaveFile;

        static void SaveFile(string path)
        {
            GTK4Utils.OnPathChanged -= SaveFile;
            if (path is null)
            {
                return;
            }

            AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;

            try
            {
                AlphaDreamSoundFontSaver_DLS.Save(cfg, path);
                FlexibleDialog.Show(string.Format(Strings.SuccessSaveDLS, path), Strings.SuccessSaveDLS);
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, Strings.ErrorSaveDLS);
            }
        }
    }
    private void ExportSF2(Gio.SimpleAction sender, EventArgs e)
    {
        GTK4Utils.CreateSaveDialog(Engine.Instance!.Config.GetGameName(), ["*.sf2"], Strings.MenuSaveSF2, Strings.FilterSaveSF2);
        GTK4Utils.OnPathChanged += SaveFile;

        static void SaveFile(string path)
        {
            GTK4Utils.OnPathChanged -= SaveFile;
            if (path is null)
            {
                return;
            }

            AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;

            try
            {
                AlphaDreamSoundFontSaver_SF2.Save(path, cfg);
                FlexibleDialog.Show(string.Format(Strings.SuccessSaveSF2, path), Strings.SuccessSaveSF2);
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, Strings.ErrorSaveSF2);
            }
        }
    }
    private void ExportWAV(object sender, EventArgs e)
    {
        GTK4Utils.CreateSaveDialog(Engine.Instance!.Config.GetSongName((int)_sequenceNumberSpinButton.Value), ["*.wav"], Strings.MenuSaveWAV, Strings.FilterSaveWAV);
        GTK4Utils.OnPathChanged += SaveFile;

        void SaveFile(string path)
        {
            GTK4Utils.OnPathChanged -= SaveFile;
            if (path is null)
            {
                return;
            }

            Stop();

            Player player = Engine.Instance!.Player;
            bool oldFade = player.ShouldFadeOut;
            long oldLoops = player.NumLoops;
            player.ShouldFadeOut = true;
            player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;

            try
            {
                player.Record(path);
                FlexibleDialog.Show(string.Format(Strings.SuccessSaveWAV, path), Strings.SuccessSaveWAV);
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, Strings.ErrorSaveWAV);
            }

            player.ShouldFadeOut = oldFade;
            player.NumLoops = oldLoops;
        }
    }

    public void LetUIKnowPlayerIsPlaying()
    {
        // Prevents method from being used if timer is already active
        if (_timer.IsActive())
        {
            return;
        }

        // _sequencedAudioTrackInfo.AddEntries();

        // Configures the buttons when player is playing a sequenced track
        _buttonPause.Sensitive = _buttonStop.Sensitive = true; // Setting the 'Sensitive' property to 'true' enables the buttons, allowing you to click on them
        _buttonPause.TooltipText = Strings.PlayerPause;

        ConfigureTimer();
    }

    // Configures the timer, which triggers the CheckPlayback method at every interval depending on the GlobalConfig RefreshRate
    private void ConfigureTimer()
    {
        var context = GLib.MainContext.GetThreadDefault(); // Grabs the default GLib MainContext thread
        var source = GLib.Functions.TimeoutSourceNew(50); // Creates and configures the timeout interval
        source.SetCallback(TimerCallback); // Sets the callback for the timer interval to be used on
        var microsec = new CULong(source.Attach(context)); // Configures the microseconds based on attaching the GLib MainContext thread
        // _timer.Elapsed(ref microsec); // Adds the pointer to the configured microseconds source
        GLib.Internal.Timer.Elapsed(_timer.Handle, ref microsec); // GLib.Timer.Elapsed was removed in GirCore 0.6.3, so we're using this workaround instead
        _timer.Start(); // Starts the timer
    }

    private void Play()
    {
        _buttonPlay.OnClicked -= ButtonPlay_Clicked;
        Engine.Instance!.Player.IsPauseToggled = _buttonPause.Active = false;
        _sequencedAudioTrackInfo.ResetTempo();
        Engine.Instance.Player.Play();
        LetUIKnowPlayerIsPlaying();
        _buttonPlay.OnClicked += ButtonPlay_Clicked;
    }
    private void Pause()
    {
        _buttonPause.OnClicked -= ButtonPause_Clicked;
        Engine.Instance!.Player.TogglePlaying();
        if (Engine.Instance.Player.State == PlayerState.Paused)
        {
            _buttonPause.Active = true;
            _buttonPause.TooltipText = Strings.PlayerUnpause;
            Engine.Instance.Player.IsPauseToggled = true;
            _timer.Stop();
        }
        else
        {
            _buttonPause.Active = false;
            _buttonPause.TooltipText = Strings.PlayerPause;
            Engine.Instance.Player.IsPauseToggled = false;
            _timer.Start();
        }
        _buttonPause.OnClicked += ButtonPause_Clicked;
    }
    private void Stop()
    {
        _buttonStop.OnClicked -= ButtonStop_Clicked;
        if (Engine.Instance == null)
        {
            return; // This is here to ensure that it returns if the Engine.Instance is null while closing the main window
        }
        _timer.Stop();
        Engine.Instance!.Player.Stop();
        _sequencedAudioTrackInfo.Info!.Reset();
        _sequencedAudioTrackInfo.ResetTempo();
        Engine.Instance.Player.IsPauseToggled = _buttonPause.Active = false;
        _buttonPause.Sensitive = _buttonStop.Sensitive = false;
        _buttonPause.TooltipText = Strings.PlayerPause;
        UpdatePositionIndicators(0L);
        _buttonStop.OnClicked += ButtonStop_Clicked;
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
    private void ButtonPlay_Clicked(Gtk.Button sender, EventArgs args)
    {
        Play();
    }

    private void ButtonPause_Clicked(Gtk.Button sender, EventArgs args)
    {
        Pause();
    }

    private void ButtonStop_Clicked(Gtk.Button sender, EventArgs args)
    {
        Stop();
    }

    private void PlayPreviousSong(object? sender, EventArgs? e)
    {
        _playlistSelector.ButtonPrevPlistSong!.OnClicked -= PlayPreviousSong;
        _playlistSelector.ButtonNextPlistSong!.OnClicked -= PlayNextSong;
        if (_playlist is not null)
        {
            _playlist.UndoThenSetAndLoadPrevSong(_curSong);
        }
        else
        {
            _playlistSelector.PlaylistSelectSongDropDown!.Selected -= 1;
            _autoplay = true;
            int index = _playlistSelector.Songs![(int)_playlistSelector.PlaylistSelectSongDropDown.Selected].Index;
            _sequencedAudioList.SelectRow(index);
            if (!_playlistChanged)
            {
                // _sequencedAudioList.ColumnView!.ScrollTo((uint)index, null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
            }
            _sequenceNumberSpinButton.Value = index;
            _autoplay = false;
            CheckPlaylistItem();
        }
        _playlistSelector.ButtonPrevPlistSong.OnClicked += PlayPreviousSong;
        _playlistSelector.ButtonNextPlistSong.OnClicked += PlayNextSong;
    }
    private void PlayNextSong(object? sender, EventArgs? e)
    {
        _playlistSelector.ButtonPrevPlistSong!.OnClicked -= PlayPreviousSong;
        _playlistSelector.ButtonNextPlistSong!.OnClicked -= PlayNextSong;
        if (_playlist is not null)
        {
            _playlist.AdvanceThenSetAndLoadNextSong(_curSong);
        }
        else
        {
            _playlistSelector.PlaylistSelectSongDropDown!.Selected += 1;
            _autoplay = true;
            int index = _playlistSelector.Songs![(int)_playlistSelector.PlaylistSelectSongDropDown.Selected].Index;
            _sequencedAudioList.SelectRow(index);
            if (!_playlistChanged)
            {
                // _sequencedAudioList.ColumnView!.ScrollTo((uint)index, null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
            }
            _sequenceNumberSpinButton.Value = index;
            _autoplay = false;
            CheckPlaylistItem();
        }
        _playlistSelector.ButtonPrevPlistSong.OnClicked += PlayPreviousSong;
        _playlistSelector.ButtonNextPlistSong.OnClicked += PlayNextSong;
    }

    private void CheckPlaylistItem()
    {
        // For the Previous Song button
        if (_playlistSelector.PlaylistSelectSongDropDown!.Selected is 0)
        {
            _playlistSelector.ButtonPrevPlistSong!.Sensitive = false;
        }
        else
        {
            _playlistSelector.ButtonPrevPlistSong!.Sensitive = true;
        }

        // For the Next Song button
        if (_playlistSelector.PlaylistSelectSongDropDown.Selected == PlaylistSelector.GetNumSongs() - 1)
        {
            _playlistSelector.ButtonNextPlistSong!.Sensitive = false;
        }
        else
        {
            _playlistSelector.ButtonNextPlistSong!.Sensitive = true;
        }
    }

    private void FinishLoading(long numSongs)
    {
        Engine.Instance!.Player.SongEnded += SongEnded;
        _sequencedAudioList.Show();
        var config = Engine.Instance.Config;

        // Ensures a GlobalConfig Instance is created if one doesn't exist
        if (GlobalConfig.Instance == null)
        {
            GlobalConfig.Init(); // A new instance needs to be initialized before it can do anything
        }
        _sequencedAudioList.AddEntries(numSongs, config);
        if (config.Playlists is not null)
        {
            _playlistSelector.AddPlaylistEntries(config.Playlists);
            CheckPlaylistMode();
        }
        _sequenceNumberSpinButton.Adjustment!.Upper = numSongs - 1;
#if DEBUG
        // [Debug methods specific to this GUI will go in here]
#endif
        _autoplay = false;
        _sequenceNumberSpinButton.Sensitive = _buttonPlay.Sensitive = _volumeBar.Sensitive = true;
        int index = 0;
        if (Engine.Instance!.Config.InternalSongNames is not null && Engine.Instance!.Config.InternalSongNames.Capacity > 0)
        {
            index = Engine.Instance.Config.InternalSongNames[0].Songs.Count == 0 ? 0 : Engine.Instance.Config.InternalSongNames[0].Songs[0].Index;
        }
        if (Engine.Instance!.Config.Playlists is not null && Engine.Instance!.Config.Playlists.Capacity > 0)
        {
            index = Engine.Instance.Config.Playlists[^1].Songs.Count == 0 ? 0 : Engine.Instance.Config.Playlists[^1].Songs[0].Index;
            PlaylistSongStringChanged(index);
        }
        _sequencedAudioList.SelectRow(index);
        // _sequencedAudioList.ColumnView!.ScrollTo((uint)index, null, Gtk.ListScrollFlags.Select, Gtk.ScrollInfo.New());
        _preventAutoplay = true;
        if (_sequenceNumberSpinButton.Value != index)
        {
            _sequenceNumberSpinButton.Value = index;
        }
        else
        {
            SequenceNumberSpinButton_ValueChanged(null!, null!);
        }
        if (config.Playlists is not null)
        {
            _playlistSelector.PlaylistSelectDropDown!.OnNotify += OnPlaylistStringSelected;
            _playlistSelector.PlaylistSelectSongDropDown!.OnNotify += OnPlaylistSongStringSelected;
        }
        _volumeBar.SetValue(100);
    }
    public void ReloadEngine()
    {
        if (Engine.Instance is not null)
        {
            Stop();
            Engine.Instance.Reload();
            ResetPlaylistStuff(true);
            UpdatePositionIndicators(0L);
        }
    }
    private void DisposeEngine()
    {
        if (Engine.Instance is not null)
        {
            Stop();
            Engine.Instance.Dispose();
        }

        //_trackEditor?.UpdateTracks();
        Name = GetProgramName();
        SequencedAudio_TrackInfo.SetNumTracks(0);
        _sequencedAudioTrackInfo.ResetMutes();
        ResetPlaylistStuff(false);
        UpdatePositionIndicators(0L);
        _sequenceNumberSpinButton.Visible = false;
        _sequenceNumberSpinButton.Value = _sequenceNumberSpinButton.Adjustment!.Upper = 0;
    }

    private bool TimerCallback()
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
        if (Engine.Instance is not null)
        {
            if (_positionBarFree)
            {
                Player player = Engine.Instance!.Player;
                player.Info = _sequencedAudioTrackInfo.Info!;
                if (player.ErrorDetails is not null)
                {
                    FlexibleDialog.Show(player.ErrorDetails, player.ErrorDetails.Message);
                    Stop();
                    player.ErrorDetails = null;
                }
                _piano.UpdateKeys(player.Info.Tracks, _sequencedAudioTrackInfo.NumTracks!);
                if (player.State is PlayerState.Stopped)
                {
                    UpdatePositionIndicators(0L);
                }
                else
                {
                    UpdatePositionIndicators(player.ElapsedTicks);
                }
            }
        }
        return true;
    }

    private void SongEnded()
    {
        _songEnded = true;
    }

    // This updates _positionBar to the value specified
    private void UpdatePositionIndicators(long ticks)
    {
        if (_positionBarFree)
        {
            _positionBar.Adjustment!.SetValue(ticks);
        }
    }

    private void OpenMIDIConverterDialog(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        _midiConverterDialog = MIDIConverterDialog.NewWithProperties([]);
        _midiConverterDialog.Present(this);

        _midiConverterDialog.OnClosed += WindowClosed;

        void WindowClosed(Dialog sender, EventArgs args)
        {
            _midiConverterDialog!.Dispose();
            _midiConverterDialog = null!;
        }
    }

    private void OpenTrackEditor(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        if (_trackEditor is not null)
        {
            _trackEditor.FocusVisible = true;
        }

        _trackEditor = TrackEditor.NewWithProperties([]);
        _windowGroup.AddWindow(_trackEditor);
        if (Engine.Instance is not null)
        {
            _trackEditor.Init();
            _trackEditor.ReloadDropDownEntries();
            // _trackEditor.ReloadColumnEntries();
        }
        _trackEditor.Present();

        _trackEditor.OnCloseRequest += WindowClosed;

        bool WindowClosed(Gtk.Window sender, EventArgs args)
        {
            _windowGroup.RemoveWindow(_trackEditor);
            _trackEditor!.Dispose();
            _trackEditor = null!;
            return false;
        }
    }

    private void OpenSoundBankEditor(Gio.SimpleAction sender, Gio.SimpleAction.ActivateSignalArgs args)
    {
        if (_soundBankEditor is not null)
        {
            _soundBankEditor.FocusVisible = true;
        }

        _soundBankEditor = SoundBankEditor.NewWithProperties([]);
        _windowGroup.AddWindow(_soundBankEditor);
        if (Engine.Instance is not null)
        {
            _soundBankEditor.Init();
            _soundBankEditor.LoadVoices(Engine.Instance.Player.LoadedSong!.Bank);
        }
        _soundBankEditor.Present();

        _soundBankEditor.OnCloseRequest += WindowClosed;

        bool WindowClosed(Gtk.Window sender, EventArgs args)
        {
            _windowGroup.RemoveWindow(_soundBankEditor);
            _soundBankEditor!.Dispose();
            _soundBankEditor = null!;
            return false;
        }
    }
}
