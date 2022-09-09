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
        private readonly List<long> _playedTracks;
        private readonly List<long> _remainingTracks;

        private bool _stopUI = false;

        #region Widgets

        // For viewing sequenced tracks
        private Box _trackViewer;

        // Buttons
        private readonly Button _buttonPlay, _buttonPause, _buttonStop;

        // A Box specifically made to contain two contents inside
        private readonly Box _splitContainerBox;

        // Spin Button for the numbered tracks
        private readonly SpinButton _soundNumberSpinButton;

        // Timer
        private readonly Timer _timer;

        // Menu Bar
        private readonly MenuBar _mainMenu;

        // Menus
        private readonly Menu _fileMenu, _dataMenu;

        // Menu Items
        private readonly MenuItem _fileItem, _openDSEItem, _openAlphaDreamItem, _openMP2KItem, _openSDATItem,
            _dataItem, _trackViewerItem, _exportDLSItem, _exportSF2Item, _exportMIDIItem, _exportWAVItem;

        // Main Box
        private Box _mainBox;

        // Volume Button to indicate volume status
        private readonly VolumeButton _volumeButton;

        // One Scale controling volume and one Scale for the sequenced track
        private readonly Scale _volumeScale, _positionScale;

        // Adjustments are for indicating the numbers and the position of the scale
        private Adjustment _volumeAdjustment, _positionAdjustment, _soundNumberAdjustment;

        #endregion

        public MainWindow() : base("Main Window") 
        {
            // Main Window
            // Sets the default size of the Window
            SetDefaultSize(500, 300);


            // Sets the _playedTracks and _remainingTracks with a List<long>() function to be ready for use
            //_playedTracks = new List<long>();
            //_remainingTracks = new List<long>();

            // Configures SetVolumeScale method with the MixerVolumeChanged Event action
            //Mixer.MixerVolumeChanged += SetVolumeScale;

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

            _exportDLSItem = new MenuItem() { Sensitive = false, Label = Strings.MenuSaveDLS, UseUnderline = true };
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

            // Buttons

            // Spin Button

            // Timer

            // Volume Scale

            // Position Scale

            // Main display
            _mainBox = new Box(Orientation.Vertical, 2);
            _mainBox.PackStart(_mainMenu, false, false, 0);

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
        private void PositionScale_MouseButtonPress(object? sender, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 1) // Number 1 is Left Mouse Button
            {
                Engine.Instance.Player.SetCurrentPosition((long)_positionScale.Value);
                _positionScaleFree = true;
            }
        }
        private void PositionScale_MouseButtonRelease(object? sender, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 1) // Number 1 is Left Mouse Button
            {
                _positionScaleFree = false;
            }
        }

        private bool _autoplay = false;
        private void SoundNumberSpinButton_ValueChanged(object? sender, EventArgs? e)
        {
            //_songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;

            long index = (long)_soundNumberAdjustment.Value;
            Stop();
            Parent.Name = ConfigUtils.PROGRAM_NAME;
            //_songsComboBox.SelectedIndex = 0;
            //_songInfo.Reset();
            bool success;
            try
            {
                Engine.Instance!.Player.LoadSong(index);
                success = Engine.Instance.Player.LoadedSong is not null; // TODO: Make sure loadedsong is null when there are no tracks (for each engine, only mp2k guarantees it rn)
            }
            catch (Exception ex)
            {
                //FlexibleMessageBox.Show(ex, string.Format(Strings.ErrorLoadSong, Engine.Instance!.Config.GetSongName(index)));
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
                    Parent.Name = $"{ConfigUtils.PROGRAM_NAME} - {song.Name}"; // TODO: Make this a func
                    //_songsComboBox.SelectedIndex = songs.IndexOf(song) + 1; // + 1 because the "Music" playlist is first in the combobox
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
        }

        private void SetAndLoadTrack(long index)
        {
            _curSong = index;
            if (_soundNumberSpinButton.Value == index)
            {
                SoundNumberSpinButton_ValueChanged(null, null);
            }
            else
            {
                _soundNumberSpinButton.Value = index;
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
                //FlexibleMessageBox.Show(ex, Strings.ErrorOpenDSE);
                return;
            }

            DSEConfig config = DSEEngine.DSEInstance!.Config;
            FinishLoading(config.BGMFiles.Length);
            _soundNumberSpinButton.Visible = false;
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
                //FlexibleMessageBox.Show(ex, Strings.ErrorOpenAlphaDream);
                return;
            }

            AlphaDreamConfig config = AlphaDreamEngine.AlphaDreamInstance!.Config;
            FinishLoading(config.SongTableSizes[0]);
            _soundNumberSpinButton.Visible = true;
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
                //FlexibleMessageBox.Show(ex, Strings.ErrorOpenMP2K);
                return;
            }

            MP2KConfig config = MP2KEngine.MP2KInstance!.Config;
            FinishLoading(config.SongTableSizes[0]);
            _soundNumberSpinButton.Visible = true;
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
                //FlexibleMessageBox.Show(ex, Strings.ErrorOpenSDAT);
                return;
            }

            SDATConfig config = SDATEngine.SDATInstance!.Config;
            FinishLoading(config.SDAT.INFOBlock.SequenceInfos.NumEntries);
            _soundNumberSpinButton.Visible = true;
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
                //FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveDLS, d.FileName), Text);
            }
            catch (Exception ex)
            {
                //FlexibleMessageBox.Show(ex, Strings.ErrorSaveDLS);
            }

            d.Destroy();
        }
        private void ExportMIDI(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuSaveMIDI,
                this,
                FileChooserAction.Save, "Save", "Cancel");
            d.SetFilename(Engine.Instance!.Config.GetSongName((long)_soundNumberSpinButton.Value));

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
                //FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveMIDI, d.FileName), Text);
            }
            catch (Exception ex)
            {
                //FlexibleMessageBox.Show(ex, Strings.ErrorSaveMIDI);
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
                //FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveSF2, d.FileName), Text);
            }
            catch (Exception ex)
            {
                //FlexibleMessageBox.Show(ex, Strings.ErrorSaveSF2);
            }
        }
        private void ExportWAV(object? sender, EventArgs? e)
        {
            var d = new FileChooserNative(
                Strings.MenuSaveWAV,
                this,
                FileChooserAction.Save, "Save", "Cancel");

            d.SetFilename(Engine.Instance!.Config.GetSongName((long)_soundNumberSpinButton.Value));

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
                //FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveWAV, d.FileName), Text);
            }
            catch (Exception ex)
            {
                //FlexibleMessageBox.Show(ex, Strings.ErrorSaveWAV);
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

        private void FinishLoading(long numSongs)
        {
            Engine.Instance!.Player.SongEnded += SongEnded;
            foreach (Config.Playlist playlist in Engine.Instance.Config.Playlists)
            {

            }
            _soundNumberAdjustment.Upper = numSongs - 1;
#if DEBUG
            // [Debug methods specific to this UI will go in here]
#endif
            _autoplay = false;
            //SetAndLoadSong(Engine.Instance.Config.Playlists[0].Songs.Count == 0 ? 0 : Engine.Instance.Config.Playlists[0].Songs[0].Index);
            _soundNumberSpinButton.Sensitive = _buttonPlay.Sensitive = _volumeScale.Sensitive = true;
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
            //ResetPlaylistStuff(false);
            UpdatePositionIndicators(0L);
            //_songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;
            _soundNumberAdjustment.ValueChanged -= SoundNumberSpinButton_ValueChanged;
            _soundNumberSpinButton.Visible = false;
            _soundNumberSpinButton.Value = _soundNumberAdjustment.Upper = 0;
            //_songsComboBox.SelectedItem = null;
            //_songsComboBox.Items.Clear();
            //_songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
            _soundNumberSpinButton.ValueChanged += SoundNumberSpinButton_ValueChanged;
        }

        private void UpdateUI(object? sender, EventArgs? e)
        {
            if (_stopUI)
            {
                _stopUI = false;
                if (_playlistPlaying)
                {
                    _playedTracks.Add(_curSong);
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
