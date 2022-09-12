using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA.AlphaDream;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Gtk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace Kermalis.VGMusicStudio.GTK3
{
    internal sealed class MainWindow : Window
    {
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

        // Menu Bar
        private readonly MenuBar _mainMenu;

        // Menus
        private readonly Menu _fileMenu, _dataMenu, _soundtableMenu;

        // Menu Items
        private readonly MenuItem _fileItem, _openDSEItem, _openAlphaDreamItem, _openMP2KItem, _openSDATItem,
            _dataItem, _trackViewerItem, _exportDLSItem, _exportSF2Item, _exportMIDIItem, _exportWAVItem, _soundtableItem, _endSoundtableItem; 

        // Main Box
        private Box _mainBox, _configButtonBox, _configPlayerButtonBox, _configSpinButtonBox, _configScaleBox;

        // Volume Button to indicate volume status
        private readonly VolumeButton _volumeButton;

        // One Scale controling volume and one Scale for the sequenced track
        private readonly Scale _volumeScale, _positionScale;

        // Adjustments are for indicating the numbers and the position of the scale
        private Adjustment _volumeAdjustment, _positionAdjustment, _sequenceNumberAdjustment;

        // Tree View
        private readonly TreeView _sequencesListView;
        private readonly TreeViewColumn _sequencesColumn;

        // List Store
        private ListStore _sequencesListStore;

        #endregion

        public MainWindow() : base(ConfigUtils.PROGRAM_NAME) 
        {
            // Main Window
            // Sets the default size of the Window
            SetDefaultSize(500, 300);


            // Sets the _playedSequences and _remainingSequences with a List<long>() function to be ready for use
            _playedSequences = new List<long>();
            _remainingSequences = new List<long>();

            // Configures SetVolumeScale method with the MixerVolumeChanged Event action
            Mixer.MixerVolumeChanged += SetVolumeScale;

            // Main Menu
            _mainMenu = new MenuBar();

            // File Menu
            _fileMenu = new Menu();

            _fileItem = new MenuItem() { Label = Strings.MenuFile, UseUnderline = true };
            _fileItem.Submenu = _fileMenu;

            _openDSEItem = new MenuItem() { Label = Strings.MenuOpenDSE, UseUnderline = true };
            _openDSEItem.Activated += OpenDSE;
            _fileMenu.Append(_openDSEItem);

            _openSDATItem = new MenuItem() { Label = Strings.MenuOpenSDAT, UseUnderline = true };
            _openSDATItem.Activated += OpenSDAT;
            _fileMenu.Append(_openSDATItem);

            _openAlphaDreamItem = new MenuItem() { Label = Strings.MenuOpenAlphaDream, UseUnderline = true };
            _openAlphaDreamItem.Activated += OpenAlphaDream;
            _fileMenu.Append(_openAlphaDreamItem);

            _openMP2KItem = new MenuItem() { Label = Strings.MenuOpenMP2K, UseUnderline = true };
            _openMP2KItem.Activated += OpenMP2K;
            _fileMenu.Append(_openMP2KItem);

            _mainMenu.Append(_fileItem); // Note: It must append the menu item, not the file menu itself

            // Data Menu
            _dataMenu = new Menu();

            _dataItem = new MenuItem() { Label = Strings.MenuData, UseUnderline = true };
            _dataItem.Submenu = _dataMenu;

            _exportDLSItem = new MenuItem() { Sensitive = false, Label = Strings.MenuSaveDLS, UseUnderline = true }; // Sensitive is identical to 'Enabled', so if you're disabling the control, Sensitive must be set to false
            _exportDLSItem.Activated += ExportDLS;
            _dataMenu.Append(_exportDLSItem);

            _exportSF2Item = new MenuItem() { Sensitive = false, Label = Strings.MenuSaveSF2, UseUnderline = true };
            _exportSF2Item.Activated += ExportSF2;
            _dataMenu.Append(_exportSF2Item);

            _exportMIDIItem = new MenuItem() { Sensitive = false, Label = Strings.MenuSaveMIDI, UseUnderline = true };
            _exportMIDIItem.Activated += ExportMIDI;
            _dataMenu.Append(_exportMIDIItem);

            _exportWAVItem = new MenuItem() { Sensitive = false, Label = Strings.MenuSaveWAV, UseUnderline = true };
            _exportWAVItem.Activated += ExportWAV;
            _dataMenu.Append(_exportWAVItem);

            _mainMenu.Append(_dataItem);

            // Soundtable Menu
            _soundtableMenu = new Menu();

            _soundtableItem = new MenuItem() { Label = Strings.MenuPlaylist, UseUnderline = true };
            _soundtableItem.Submenu = _soundtableMenu;

            _endSoundtableItem = new MenuItem() { Label = Strings.MenuEndPlaylist, UseUnderline = true };
            _endSoundtableItem.Activated += EndCurrentPlaylist;
            _soundtableMenu.Append(_endSoundtableItem);

            _mainMenu.Append(_soundtableItem);

            // Buttons
            _buttonPlay = new Button() { Sensitive = false, Label = Strings.PlayerPlay };
            _buttonPlay.Clicked += (o, e) => Play();
            _buttonPause = new Button() { Sensitive = false, Label = Strings.PlayerPause };
            _buttonPause.Clicked += (o, e) => Pause();
            _buttonStop = new Button() { Sensitive = false, Label = Strings.PlayerStop };
            _buttonStop.Clicked += (o, e) => Stop();

            // Spin Button
            _sequenceNumberAdjustment = new Adjustment(0, 0, -1, 1, 1, 1);
            _sequenceNumberSpinButton = new SpinButton(_sequenceNumberAdjustment, 1, 0) { Sensitive = false, Value = 0, NoShowAll = true, Visible = false };
            _sequenceNumberSpinButton.ValueChanged += SequenceNumberSpinButton_ValueChanged;

            // Timer
            _timer = new Timer();
            _timer.Elapsed += UpdateUI;

            // Volume Scale
            _volumeAdjustment = new Adjustment(0, 0, 100, 1, 1, 1);
            _volumeScale = new Scale(Orientation.Horizontal, _volumeAdjustment) { Sensitive = false, ShowFillLevel = true, DrawValue = false, WidthRequest = 250 };
            _volumeScale.ValueChanged += VolumeScale_ValueChanged;

            // Position Scale
            _positionAdjustment = new Adjustment(0, 0, -1, 1, 1, 1);
            _positionScale = new Scale(Orientation.Horizontal, _positionAdjustment) { Sensitive = false, ShowFillLevel = true, DrawValue = false, WidthRequest = 250 };
            _positionScale.ButtonReleaseEvent += PositionScale_MouseButtonRelease; // ButtonRelease must go first, otherwise the scale it will follow the mouse cursor upon loading
            _positionScale.ButtonPressEvent += PositionScale_MouseButtonPress;

            // Sequences List View
            _sequencesListView = new TreeView();
            _sequencesListStore = new ListStore(typeof(string), typeof(string));
            _sequencesColumn = new TreeViewColumn("Name", new CellRendererText(), "text", 1);
            _sequencesListView.AppendColumn("#", new CellRendererText(), "text", 0);
            _sequencesListView.AppendColumn(_sequencesColumn);
            _sequencesListView.Model = _sequencesListStore;

            // Main display
            _mainBox = new Box(Orientation.Vertical, 4);
            _configButtonBox = new Box(Orientation.Horizontal, 2) { Halign = Align.Center };
            _configPlayerButtonBox = new Box(Orientation.Horizontal, 3) { Halign = Align.Center };
            _configSpinButtonBox = new Box(Orientation.Horizontal, 1) { Halign = Align.Center, WidthRequest = 100 };
            _configScaleBox = new Box(Orientation.Horizontal, 2) { Halign = Align.Center };

            _mainBox.PackStart(_mainMenu, false, false, 0);
            _mainBox.PackStart(_configButtonBox, false, false, 0);
            _mainBox.PackStart(_configScaleBox, false, false, 0);
            _mainBox.PackStart(_sequencesListView, false, false, 0);

            _configButtonBox.PackStart(_configPlayerButtonBox, false, false, 40);
            _configButtonBox.PackStart(_configSpinButtonBox, false, false, 100);

            _configPlayerButtonBox.PackStart(_buttonPlay, false, false, 0);
            _configPlayerButtonBox.PackStart(_buttonPause, false, false, 0);
            _configPlayerButtonBox.PackStart(_buttonStop, false, false, 0);

            _configSpinButtonBox.PackStart(_sequenceNumberSpinButton, false, false, 0);

            _configScaleBox.PackStart(_volumeScale, false, false, 20);
            _configScaleBox.PackStart(_positionScale, false, false, 20);

            Add(_mainBox);

            ShowAll();

            // Ensures the entire application closes when the window is closed
            DeleteEvent += delegate { Application.Quit(); };
        }

        // When the value is changed on the volume scale
        private void VolumeScale_ValueChanged(object? sender, EventArgs? e)
        {
            Engine.Instance.Mixer.SetVolume((float)(_volumeScale.Value / _volumeAdjustment.Value));
        }

        // Sets the volume scale to the specified position
        public void SetVolumeScale(float volume)
        {
            _volumeScale.ValueChanged -= VolumeScale_ValueChanged;
            _volumeScale.Value = (int)(volume * _volumeAdjustment.Upper);
            _volumeScale.ValueChanged += VolumeScale_ValueChanged;
        }

        private bool _positionScaleFree = true;
        private void PositionScale_MouseButtonRelease(object? sender, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button == 1) // Number 1 is Left Mouse Button
            {
                Engine.Instance.Player.SetCurrentPosition((long)_positionScale.Value); // Sets the value based on the position when mouse button is released
                _positionScaleFree = true; // Sets _positionScaleFree to true when mouse button is released
                LetUIKnowPlayerIsPlaying(); // This method will run the void that tells the UI that the player is playing a track
            }
        }
        private void PositionScale_MouseButtonPress(object? sender, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 1) // Number 1 is Left Mouse Button
            {
                _positionScaleFree = false;
            }
        }

        private bool _autoplay = false;
        private void SequenceNumberSpinButton_ValueChanged(object? sender, EventArgs? e)
        {
            _sequencesListView.SelectionGet -= SequencesListView_SelectionGet;

            long index = (long)_sequenceNumberAdjustment.Value;
            Stop();
            this.Title = ConfigUtils.PROGRAM_NAME;
            _sequencesListView.Margin = 0;
            //_songInfo.Reset();
            bool success;
            try
            {
                Engine.Instance!.Player.LoadSong(index);
                success = Engine.Instance.Player.LoadedSong is not null; // TODO: Make sure loadedsong is null when there are no tracks (for each engine, only mp2k guarantees it rn)
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.YesNo, string.Format(Strings.ErrorLoadSong, Engine.Instance!.Config.GetSongName(index)), ex);
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
                    _sequencesColumn.SortColumnId = songs.IndexOf(song) + 1; // + 1 because the "Music" playlist is first in the combobox
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
            _positionScale.Sensitive = _exportWAVItem.Sensitive = success;
            _exportMIDIItem.Sensitive = success && MP2KEngine.MP2KInstance is not null;
            _exportDLSItem.Sensitive = _exportSF2Item.Sensitive = success && AlphaDreamEngine.AlphaDreamInstance is not null;

            _autoplay = true;
            _sequencesListView.SelectionGet += SequencesListView_SelectionGet;
        }
        private void SequencesListView_SelectionGet(object? sender, EventArgs? e)
        {
            var item = _sequencesListView.Selection;
            if (item.SelectFunction.Target is Config.Song song)
            {
                SetAndLoadSequence(song.Index);
            }
            else if (item.SelectFunction.Target is Config.Playlist playlist)
            {
                var md = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist, Strings.MenuPlaylist));
                if (playlist.Songs.Count > 0
                    && md.Run() == (int)ResponseType.Yes)
                {
                    ResetPlaylistStuff(false);
                    _curPlaylist = playlist;
                    Engine.Instance.Player.ShouldFadeOut = _playlistPlaying = true;
                    Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
                    _endSoundtableItem.Sensitive = true;
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
            _endSoundtableItem.Sensitive = false;
            _sequenceNumberSpinButton.Sensitive = _sequencesListView.Sensitive = enableds;
        }
        private void EndCurrentPlaylist(object? sender, EventArgs? e)
        {
            var md = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.YesNo, string.Format(Strings.EndPlaylistBody, Strings.MenuPlaylist));
            if (md.Run() == (int)ResponseType.Yes)
            {
                ResetPlaylistStuff(true);
            }
        }

        private void OpenDSE(object? sender, EventArgs? e)
        {
            // To allow the dialog to display in native windowing format, FileChooserNative is used instead of FileChooserDialog
            var d = new FileChooserNative(
                Strings.MenuOpenDSE, // The title shown in the folder select dialog window
                this, // The parent of the dialog window, is the MainWindow itself
                FileChooserAction.SelectFolder, "Open", "Cancel"); // To ensure it becomes a folder select dialog window, SelectFolder is used as the FileChooserAction, followed by the accept and cancel button names

            if (d.Run() != (int)ResponseType.Accept)
            {
                return;
            }

            DisposeEngine();
            try
            {
                _ = new DSEEngine(d.CurrentFolder);
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorOpenDSE, ex);
                return;
            }

            DSEConfig config = DSEEngine.DSEInstance!.Config;
            FinishLoading(config.BGMFiles.Length);
            _sequenceNumberSpinButton.Visible = false;
            _sequenceNumberSpinButton.NoShowAll = true;
            _exportDLSItem.Visible = false;
            _exportMIDIItem.Visible = false;
            _exportSF2Item.Visible = false;

            d.Destroy(); // Ensures disposal of the dialog when closed
        }
        private void OpenAlphaDream(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuOpenAlphaDream,
                this,
                FileChooserAction.Open, "Open", "Cancel");

            FileFilter filterGBA = new FileFilter()
            {
                Name = Strings.GTKFilterOpenGBA
            };
            filterGBA.AddPattern("*.gba");
            filterGBA.AddPattern("*.srl");
            FileFilter allFiles = new FileFilter()
            {
                Name = Strings.GTKAllFiles
            };
            allFiles.AddPattern("*.*");
            d.AddFilter(filterGBA);
            d.AddFilter(allFiles);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

            DisposeEngine();
            try
            {
                _ = new AlphaDreamEngine(File.ReadAllBytes(d.Filename));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorOpenAlphaDream, ex);
                return;
            }

            AlphaDreamConfig config = AlphaDreamEngine.AlphaDreamInstance!.Config;
            FinishLoading(config.SongTableSizes[0]);
            _sequenceNumberSpinButton.Visible = true;
            _sequenceNumberSpinButton.NoShowAll = false;
            _exportDLSItem.Visible = true;
            _exportMIDIItem.Visible = false;
            _exportSF2Item.Visible = true;

            d.Destroy();
        }
        private void OpenMP2K(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuOpenMP2K,
                this,
                FileChooserAction.Open, "Open", "Cancel");

            FileFilter filterGBA = new FileFilter()
            {
                Name = Strings.GTKFilterOpenGBA
            };
            filterGBA.AddPattern("*.gba");
            filterGBA.AddPattern("*.srl");
            FileFilter allFiles = new FileFilter()
            {
                Name = Strings.GTKAllFiles
            };
            allFiles.AddPattern("*.*");
            d.AddFilter(filterGBA);
            d.AddFilter(allFiles);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

            DisposeEngine();
            try
            {
                _ = new MP2KEngine(File.ReadAllBytes(d.Filename));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorOpenMP2K, ex);
                return;
            }

            MP2KConfig config = MP2KEngine.MP2KInstance!.Config;
            FinishLoading(config.SongTableSizes[0]);
            _sequenceNumberSpinButton.Visible = true;
            _sequenceNumberSpinButton.NoShowAll = false;
            _exportDLSItem.Visible = false;
            _exportMIDIItem.Visible = true;
            _exportSF2Item.Visible = false;

            d.Destroy();
        }
        private void OpenSDAT(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuOpenSDAT,
                this,
                FileChooserAction.Open, "Open", "Cancel");

            FileFilter filterSDAT = new FileFilter()
            {
                Name = Strings.GTKFilterOpenSDAT
            };
            filterSDAT.AddPattern("*.sdat");
            FileFilter allFiles = new FileFilter()
            {
                Name = Strings.GTKAllFiles
            };
            allFiles.AddPattern("*.*");
            d.AddFilter(filterSDAT);
            d.AddFilter(allFiles);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

            DisposeEngine();
            try
            {
                _ = new SDATEngine(new SDAT(File.ReadAllBytes(d.Filename)));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorOpenSDAT, ex);
                return;
            }

            SDATConfig config = SDATEngine.SDATInstance!.Config;
            FinishLoading(config.SDAT.INFOBlock.SequenceInfos.NumEntries);
            _sequenceNumberSpinButton.Visible = true;
            _sequenceNumberSpinButton.NoShowAll = false;
            _exportDLSItem.Visible = false;
            _exportMIDIItem.Visible = false;
            _exportSF2Item.Visible = false;

            d.Destroy();
        }

        private void ExportDLS(object? sender, EventArgs? e)
        {
            AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;

            var d = new FileChooserNative(
                Strings.MenuSaveDLS,
                this,
                FileChooserAction.Save, "Save", "Cancel");
            d.SetFilename(cfg.GetGameName());

            FileFilter ff = new FileFilter()
            {
                Name = Strings.GTKFilterSaveDLS
            };
            ff.AddPattern("*.dls");
            d.AddFilter(ff);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

            try
            {
                AlphaDreamSoundFontSaver_DLS.Save(cfg, d.Filename);
                new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, string.Format(Strings.SuccessSaveDLS, d.Filename));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorSaveDLS, ex);
            }

            d.Destroy();
        }
        private void ExportMIDI(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuSaveMIDI,
                this,
                FileChooserAction.Save, "Save", "Cancel");
            d.SetFilename(Engine.Instance!.Config.GetSongName((long)_sequenceNumberSpinButton.Value));

            FileFilter ff = new FileFilter()
            {
                Name = Strings.GTKFilterSaveMIDI
            };
            ff.AddPattern("*.mid");
            ff.AddPattern("*.midi");
            d.AddFilter(ff);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

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
                p.SaveAsMIDI(d.Filename, args);
                new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, string.Format(Strings.SuccessSaveMIDI, d.Filename));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorSaveMIDI, ex);
            }
        }
        private void ExportSF2(object? sender, EventArgs? e)
        {
            AlphaDreamConfig cfg = AlphaDreamEngine.AlphaDreamInstance!.Config;

            var d = new FileChooserNative(
                Strings.MenuSaveSF2,
                this,
                FileChooserAction.Save, "Save", "Cancel");

            d.SetFilename(cfg.GetGameName());

            FileFilter ff = new FileFilter()
            {
                Name = Strings.GTKFilterSaveSF2
            };
            ff.AddPattern("*.sf2");
            d.AddFilter(ff);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

            try
            {
                AlphaDreamSoundFontSaver_SF2.Save(cfg, d.Filename);
                new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, string.Format(Strings.SuccessSaveSF2, d.Filename));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorSaveSF2, ex);
            }
        }
        private void ExportWAV(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuSaveWAV,
                this,
                FileChooserAction.Save, "Save", "Cancel");

            d.SetFilename(Engine.Instance!.Config.GetSongName((long)_sequenceNumberSpinButton.Value));

            FileFilter ff = new FileFilter()
            {
                Name = Strings.GTKFilterSaveWAV
            };
            ff.AddPattern("*.wav");
            d.AddFilter(ff);

            if (d.Run() != (int)ResponseType.Accept)
            {
                d.Destroy();
                return;
            }

            Stop();

            IPlayer player = Engine.Instance.Player;
            bool oldFade = player.ShouldFadeOut;
            long oldLoops = player.NumLoops;
            player.ShouldFadeOut = true;
            player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;

            try
            {
                player.Record(d.Filename);
                new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, string.Format(Strings.SuccessSaveWAV, d.Filename));
            }
            catch (Exception ex)
            {
                new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Strings.ErrorSaveWAV, ex);
            }

            player.ShouldFadeOut = oldFade;
            player.NumLoops = oldLoops;
            _stopUI = false;
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
                _sequencesListStore.AppendValues(i++, playlist);
                playlist.Songs.Select(s => new TreeView(_sequencesListStore)).ToArray();
            }
            _sequenceNumberAdjustment.Upper = numSongs - 1;
#if DEBUG
            // [Debug methods specific to this UI will go in here]
#endif
            _autoplay = false;
            SetAndLoadSequence(Engine.Instance.Config.Playlists[0].Songs.Count == 0 ? 0 : Engine.Instance.Config.Playlists[0].Songs[0].Index);
            _sequenceNumberSpinButton.Sensitive = _buttonPlay.Sensitive = _volumeScale.Sensitive = true;
            ShowAll();
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
            _sequencesListView.SelectionGet -= SequencesListView_SelectionGet;
            _sequenceNumberAdjustment.ValueChanged -= SequenceNumberSpinButton_ValueChanged;
            _sequenceNumberSpinButton.Visible = false;
            _sequenceNumberSpinButton.Value = _sequenceNumberAdjustment.Upper = 0;
            _sequencesListView.Selection.SelectFunction = null;
            _sequencesListView.Data.Clear();
            _sequencesListView.SelectionGet += SequencesListView_SelectionGet;
            _sequenceNumberSpinButton.ValueChanged += SequenceNumberSpinButton_ValueChanged;
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
