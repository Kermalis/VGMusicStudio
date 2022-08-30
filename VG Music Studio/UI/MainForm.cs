using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
//using Microsoft.WindowsAPICodePack.Dialogs;
//using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Kermalis.VGMusicStudio.UI
{
    [DesignerCategory("")]
    internal class MainForm : ThemedForm
    {
        private const int _intendedWidth = 675;
        private const int _intendedHeight = 675 + 1 + 125 + 24;

        public static MainForm Instance { get; } = new MainForm();

        public readonly bool[] PianoTracks = new bool[SongInfoControl.SongInfo.MaxTracks];

        private bool _playlistPlaying;
        private Config.Playlist _curPlaylist;
        private long _curSong = -1;
        private readonly List<long> _playedSongs = new List<long>();
        private readonly List<long> _remainingSongs = new List<long>();

        private TrackViewer _trackViewer;

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
        //private readonly ThumbnailToolBarButton _prevTButton, _toggleTButton, _nextTButton;

        #endregion

        #region Folder Picker

        // Originally made by: Simon Mourier
        // Source: https://stackoverflow.com/questions/11624298/how-do-i-use-openfiledialog-to-select-a-folder/66187224#66187224

        public class FolderPicker
        {
            public virtual string ResultPath { get; protected set; }
            public virtual string ResultName { get; protected set; }
            public virtual string InputPath { get; set; }
            public virtual bool ForceFileSystem { get; set; }
            public virtual string Title { get; set; }
            public virtual string OkButtonLabel { get; set; }
            public virtual string FileNameLabel { get; set; }

            protected virtual int SetOptions(int options)
            {
                if (ForceFileSystem)
                {
                    options |= (int)FOS.FOS_FORCEFILESYSTEM;
                }
                return options;
            }

            // for all .NET
            public virtual bool? ShowDialog(IntPtr owner, bool throwOnError = false)
            {
                var dialog = (IFileOpenDialog)new FileOpenDialog();
                if (!string.IsNullOrEmpty(InputPath))
                {
                    if (CheckHr(SHCreateItemFromParsingName(InputPath, null, typeof(IShellItem).GUID, out var item), throwOnError) != 0)
                        return null;

                    dialog.SetFolder(item);
                }

                var options = FOS.FOS_PICKFOLDERS;
                options = (FOS)SetOptions((int)options);
                dialog.SetOptions(options);

                if (Title != null)
                {
                    dialog.SetTitle(Title);
                }

                if (OkButtonLabel != null)
                {
                    dialog.SetOkButtonLabel(OkButtonLabel);
                }

                if (FileNameLabel != null)
                {
                    dialog.SetFileName(FileNameLabel);
                }

                if (owner == IntPtr.Zero)
                {
                    owner = Process.GetCurrentProcess().MainWindowHandle;
                    if (owner == IntPtr.Zero)
                    {
                        owner = GetDesktopWindow();
                    }
                }

                var hr = dialog.Show(owner);
                if (hr == ERROR_CANCELLED)
                    return null;

                if (CheckHr(hr, throwOnError) != 0)
                    return null;

                if (CheckHr(dialog.GetResult(out var result), throwOnError) != 0)
                    return null;

                if (CheckHr(result.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var path), throwOnError) != 0)
                    return null;

                ResultPath = path;

                if (CheckHr(result.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING, out path), false) == 0)
                {
                    ResultName = path;
                }
                return true;
            }

            private static int CheckHr(int hr, bool throwOnError)
            {
                if (hr != 0)
                {
                    if (throwOnError)
                        Marshal.ThrowExceptionForHR(hr);
                }
                return hr;
            }

            [DllImport("shell32")]
            private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

            [DllImport("user32")]
            private static extern IntPtr GetDesktopWindow();

#pragma warning disable IDE1006 // Naming Styles
            private const int ERROR_CANCELLED = unchecked((int)0x800704C7);
#pragma warning restore IDE1006 // Naming Styles

            [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")] // CLSID_FileOpenDialog
            private class FileOpenDialog
            {
            }

            [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IFileOpenDialog
            {
                [PreserveSig] int Show(IntPtr parent); // IModalWindow
                [PreserveSig] int SetFileTypes();  // not fully defined
                [PreserveSig] int SetFileTypeIndex(int iFileType);
                [PreserveSig] int GetFileTypeIndex(out int piFileType);
                [PreserveSig] int Advise(); // not fully defined
                [PreserveSig] int Unadvise();
                [PreserveSig] int SetOptions(FOS fos);
                [PreserveSig] int GetOptions(out FOS pfos);
                [PreserveSig] int SetDefaultFolder(IShellItem psi);
                [PreserveSig] int SetFolder(IShellItem psi);
                [PreserveSig] int GetFolder(out IShellItem ppsi);
                [PreserveSig] int GetCurrentSelection(out IShellItem ppsi);
                [PreserveSig] int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
                [PreserveSig] int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
                [PreserveSig] int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
                [PreserveSig] int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
                [PreserveSig] int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
                [PreserveSig] int GetResult(out IShellItem ppsi);
                [PreserveSig] int AddPlace(IShellItem psi, int alignment);
                [PreserveSig] int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
                [PreserveSig] int Close(int hr);
                [PreserveSig] int SetClientGuid();  // not fully defined
                [PreserveSig] int ClearClientData();
                [PreserveSig] int SetFilter([MarshalAs(UnmanagedType.IUnknown)] object pFilter);
                [PreserveSig] int GetResults([MarshalAs(UnmanagedType.IUnknown)] out object ppenum);
                [PreserveSig] int GetSelectedItems([MarshalAs(UnmanagedType.IUnknown)] out object ppsai);
            }

            [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IShellItem
            {
                [PreserveSig] int BindToHandler(); // not fully defined
                [PreserveSig] int GetParent(); // not fully defined
                [PreserveSig] int GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
                [PreserveSig] int GetAttributes();  // not fully defined
                [PreserveSig] int Compare();  // not fully defined
            }

#pragma warning disable CA1712 // Do not prefix enum values with type name
            private enum SIGDN : uint
            {
                SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
                SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
                SIGDN_FILESYSPATH = 0x80058000,
                SIGDN_NORMALDISPLAY = 0,
                SIGDN_PARENTRELATIVE = 0x80080001,
                SIGDN_PARENTRELATIVEEDITING = 0x80031001,
                SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
                SIGDN_PARENTRELATIVEPARSING = 0x80018001,
                SIGDN_URL = 0x80068000
            }

            [Flags]
            private enum FOS
            {
                FOS_OVERWRITEPROMPT = 0x2,
                FOS_STRICTFILETYPES = 0x4,
                FOS_NOCHANGEDIR = 0x8,
                FOS_PICKFOLDERS = 0x20,
                FOS_FORCEFILESYSTEM = 0x40,
                FOS_ALLNONSTORAGEITEMS = 0x80,
                FOS_NOVALIDATE = 0x100,
                FOS_ALLOWMULTISELECT = 0x200,
                FOS_PATHMUSTEXIST = 0x800,
                FOS_FILEMUSTEXIST = 0x1000,
                FOS_CREATEPROMPT = 0x2000,
                FOS_SHAREAWARE = 0x4000,
                FOS_NOREADONLYRETURN = 0x8000,
                FOS_NOTESTFILECREATE = 0x10000,
                FOS_HIDEMRUPLACES = 0x20000,
                FOS_HIDEPINNEDPLACES = 0x40000,
                FOS_NODEREFERENCELINKS = 0x100000,
                FOS_OKBUTTONNEEDSINTERACTION = 0x200000,
                FOS_DONTADDTORECENT = 0x2000000,
                FOS_FORCESHOWHIDDEN = 0x10000000,
                FOS_DEFAULTNOMINIMODE = 0x20000000,
                FOS_FORCEPREVIEWPANEON = 0x40000000,
                FOS_SUPPORTSTREAMABLEITEMS = unchecked((int)0x80000000)
            }
#pragma warning restore CA1712 // Do not prefix enum values with type name
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
        private MainForm()
        {
            for (int i = 0; i < PianoTracks.Length; i++)
            {
                PianoTracks[i] = true;
            }

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
            _mainMenu = new MenuStrip { Size = new Size(_intendedWidth, 24) };
            _mainMenu.Items.AddRange(new ToolStripItem[] { _fileItem, _dataItem, _playlistItem });

            // Buttons
            _playButton = new ThemedButton { Enabled = false, ForeColor = Color.MediumSpringGreen, Text = Strings.PlayerPlay };
            _playButton.Click += (o, e) => Play();
            _pauseButton = new ThemedButton { Enabled = false, ForeColor = Color.DeepSkyBlue, Text = Strings.PlayerPause };
            _pauseButton.Click += (o, e) => Pause();
            _stopButton = new ThemedButton { Enabled = false, ForeColor = Color.MediumVioletRed, Text = Strings.PlayerStop };
            _stopButton.Click += (o, e) => Stop();

            // Numerical
            _songNumerical = new ThemedNumeric { Enabled = false, Minimum = 0, Visible = false };
            _songNumerical.ValueChanged += SongNumerical_ValueChanged;

            // Timer
            _timer = new Timer();
            _timer.Tick += UpdateUI;

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
            ClientSize = new Size(_intendedWidth, _intendedHeight);
            Controls.AddRange(new Control[] { _splitContainer, _mainMenu });
            MainMenuStrip = _mainMenu;
            MinimumSize = new Size(_intendedWidth + (Width - _intendedWidth), _intendedHeight + (Height - _intendedHeight)); // Borders
            Resize += OnResize;
            Text = Utils.ProgramName;

            // Taskbar Buttons
            /*if (TaskbarManager.IsPlatformSupported)
            {
                _prevTButton = new ToolStripButton(Resources.IconPrevious, Strings.PlayerPreviousSong);
                _prevTButton.Click += PlayPreviousSong;
                _toggleTButton = new ToolStripButton(Resources.IconPlay, Strings.PlayerPlay);
                _toggleTButton.Click += TogglePlayback;
                _nextTButton = new ToolStripButton(Resources.IconNext, Strings.PlayerNextSong);
                _nextTButton.Click += PlayNextSong;
                _prevTButton.Enabled = _toggleTButton.Enabled = _nextTButton.Enabled = false;
                TaskbarManager.Instance.ThumbnailToolBars.AddButtons(Handle, _prevTButton, _toggleTButton, _nextTButton);
            }*/

            OnResize(null, null);
        }

        private void VolumeBar_ValueChanged(object sender, EventArgs e)
        {
            Engine.Instance.Mixer.SetVolume(_volumeBar.Value / (float)_volumeBar.Maximum);
        }
        public void SetVolumeBarValue(float volume)
        {
            _volumeBar.ValueChanged -= VolumeBar_ValueChanged;
            _volumeBar.Value = (int)(volume * _volumeBar.Maximum);
            _volumeBar.ValueChanged += VolumeBar_ValueChanged;
        }
        private bool _positionBarFree = true;
        private void PositionBar_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Engine.Instance.Player.SetCurrentPosition(_positionBar.Value);
                _positionBarFree = true;
                LetUIKnowPlayerIsPlaying();
            }
        }
        private void PositionBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _positionBarFree = false;
            }
        }

        private bool _autoplay = false;
        private void SongNumerical_ValueChanged(object sender, EventArgs e)
        {
            _songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;

            long index = (long)_songNumerical.Value;
            Stop();
            Text = Utils.ProgramName;
            _songsComboBox.SelectedIndex = 0;
            _songInfo.DeleteData();
            bool success;
            try
            {
                Engine.Instance.Player.LoadSong(index);
                success = true;
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex, string.Format(Strings.ErrorLoadSong, Engine.Instance.Config.GetSongName(index)));
                success = false;
            }

            _trackViewer?.UpdateTracks();
            if (success)
            {
                Config config = Engine.Instance.Config;
                List<Config.Song> songs = config.Playlists[0].Songs; // Complete "Music" playlist is present in all configs at index 0
                Config.Song song = songs.SingleOrDefault(s => s.Index == index);

                // When the song isn't a null value and is played
                if (song != null)
                {
                    Text = $"{Utils.ProgramName} - {song.Name}"; // Reads the song name from the .yaml file
                    _songsComboBox.SelectedIndex = songs.IndexOf(song) + 1; // + 1 because the "Music" playlist is first in the combobox
                }

                // When the song is a null value and is played
                if (song == null)
                {
                    return; // Resets the music player and prevents the song from playing
                }
                _positionBar.Maximum = Engine.Instance.Player.MaxTicks;
                _positionBar.LargeChange = _positionBar.Maximum / 10;
                _positionBar.SmallChange = _positionBar.LargeChange / 4;
                _songInfo.SetNumTracks(Engine.Instance.Player.Events.Length);
                if (_autoplay)
                {
                    Play();
                }
            }
            else
            {
                _songInfo.SetNumTracks(0);
            }
            int numTracks = (Engine.Instance.Player.Events?.Length).GetValueOrDefault();
            _positionBar.Enabled = _exportWAVItem.Enabled = success && numTracks > 0;
            _exportMIDIItem.Enabled = success && Engine.Instance.Type == Engine.EngineType.GBA_MP2K && numTracks > 0;
            _exportDLSItem.Enabled = _exportSF2Item.Enabled = success && Engine.Instance.Type == Engine.EngineType.GBA_AlphaDream;

            _autoplay = true;
            _songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
        }
        private void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = (ImageComboBoxItem)_songsComboBox.SelectedItem;
            if (item.Item is Config.Song song)
            {
                SetAndLoadSong(song.Index);
            }
            else if (item.Item is Config.Playlist playlist)
            {
                if (playlist.Songs.Count > 0
                    && FlexibleMessageBox.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), Strings.MenuPlaylist, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ResetPlaylistStuff(false);
                    _curPlaylist = playlist;
                    Engine.Instance.Player.ShouldFadeOut = _playlistPlaying = true;
                    Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
                    _endPlaylistItem.Enabled = true;
                    SetAndLoadNextPlaylistSong();
                }
            }
        }
        private void SetAndLoadSong(long index)
        {
            _curSong = index;
            if (_songNumerical.Value == index)
            {
                SongNumerical_ValueChanged(null, null);
            }
            else
            {
                _songNumerical.Value = index;
            }
        }
        private void SetAndLoadNextPlaylistSong()
        {
            if (_remainingSongs.Count == 0)
            {
                _remainingSongs.AddRange(_curPlaylist.Songs.Select(s => s.Index));
                if (GlobalConfig.Instance.PlaylistMode == PlaylistMode.Random)
                {
                    _remainingSongs.Shuffle();
                }
            }
            long nextSong = _remainingSongs[0];
            _remainingSongs.RemoveAt(0);
            SetAndLoadSong(nextSong);
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
            _remainingSongs.Clear();
            _playedSongs.Clear();
            _endPlaylistItem.Enabled = false;
            _songNumerical.Enabled = _songsComboBox.Enabled = enableds;
        }
        private void EndCurrentPlaylist(object sender, EventArgs e)
        {
            if (FlexibleMessageBox.Show(Strings.EndPlaylistBody, Strings.MenuPlaylist, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ResetPlaylistStuff(true);
            }
        }

        private void OpenDSE(object sender, EventArgs e)
        {
            var d = new FolderPicker
            {
                Title = Strings.MenuOpenDSE,
            };
            //var f = File.OpenRead(d.SelectedPath);
            if (d.ShowDialog(Handle) == true)
            {
                DisposeEngine();
                bool success;

                new Engine(Engine.EngineType.NDS_DSE, d.ResultPath);
                success = true;
                if (success != true)
                {
                    Exception ex = new Exception();
                    FlexibleMessageBox.Show(ex, Strings.ErrorOpenDSE);
                    success = false;
                }
                if (success)
                {
                    var config = (Core.NDS.DSE.Config)Engine.Instance.Config;
                    FinishLoading(config.BGMFiles.Length);
                    _songNumerical.Visible = false;
                    _exportDLSItem.Visible = false;
                    _exportMIDIItem.Visible = false;
                    _exportSF2Item.Visible = false;
                }
            }
        }
        private void OpenAlphaDream(object sender, EventArgs e)
        {
            var d = new OpenFileDialog
            {
                Title = Strings.MenuOpenAlphaDream,
                Filter = "Game Boy Advance ROM (*.gba, *.srl)|*.gba;*.srl|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                DisposeEngine();
                bool success;
                try
                {
                    new Engine(Engine.EngineType.GBA_AlphaDream, File.ReadAllBytes(d.FileName));
                    success = true;
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorOpenAlphaDream);
                    success = false;
                }
                if (success)
                {
                    var config = (Core.GBA.AlphaDream.Config)Engine.Instance.Config;
                    FinishLoading(config.SongTableSizes[0]);
                    _songNumerical.Visible = true;
                    _exportDLSItem.Visible = true;
                    _exportMIDIItem.Visible = false;
                    _exportSF2Item.Visible = true;
                }
            }
        }
        private void OpenMP2K(object sender, EventArgs e)
        {
            var d = new OpenFileDialog
            {
                Title = Strings.MenuOpenMP2K,
                Filter = "Game Boy Advance ROM (*.gba, *.srl)|*.gba;*.srl|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                DisposeEngine();
                bool success;
                try
                {
                    new Engine(Engine.EngineType.GBA_MP2K, File.ReadAllBytes(d.FileName));
                    success = true;
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorOpenMP2K);
                    success = false;
                }
                if (success)
                {
                    var config = (Core.GBA.MP2K.Config)Engine.Instance.Config;
                    FinishLoading(config.SongTableSizes[0]);
                    _songNumerical.Visible = true;
                    _exportDLSItem.Visible = false;
                    _exportMIDIItem.Visible = true;
                    _exportSF2Item.Visible = false;
                }
            }
        }
        private void OpenSDAT(object sender, EventArgs e)
        {
            var d = new OpenFileDialog
            {
                Title = Strings.MenuOpenSDAT,
                Filter = "Nitro Soundmaker Sound Data Archive (*.sdat)|*.sdat|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                DisposeEngine();
                bool success;
                try
                {
                    new Engine(Engine.EngineType.NDS_SDAT, new Core.NDS.SDAT.SDAT(File.ReadAllBytes(d.FileName)));
                    success = true;
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorOpenSDAT);
                    success = false;
                }
                if (success)
                {
                    var config = (Core.NDS.SDAT.Config)Engine.Instance.Config;
                    FinishLoading(config.SDAT.INFOBlock.SequenceInfos.NumEntries);
                    _songNumerical.Visible = true;
                    _exportDLSItem.Visible = false;
                    _exportMIDIItem.Visible = false;
                    _exportSF2Item.Visible = false;
                }
            }
        }

        private void ExportDLS(object sender, EventArgs e)
        {
            var d = new SaveFileDialog
            {
                FileName = Engine.Instance.Config.GetGameName(),
                Filter = Strings.FilterSaveDLS,
                FilterIndex = 1,
                ValidateNames = true,
                Title = Strings.MenuSaveDLS
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Core.GBA.AlphaDream.SoundFontSaver_DLS.Save((Core.GBA.AlphaDream.Config)Engine.Instance.Config, d.FileName);
                    FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveDLS, d.FileName), Text);
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorSaveDLS);
                }
            }
        }
        private void ExportMIDI(object sender, EventArgs e)
        {
            var d = new SaveFileDialog
            {
                FileName = Engine.Instance.Config.GetSongName((long)_songNumerical.Value),
                DefaultExt = ".mid",
                ValidateNames = true,
                Title = Strings.MenuSaveMIDI,
                Filter = Strings.FilterSaveMIDI
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                var p = (Core.GBA.MP2K.Player)Engine.Instance.Player;
                var args = new Core.GBA.MP2K.Player.MIDISaveArgs
                {
                    SaveCommandsBeforeTranspose = true,
                    ReverseVolume = false,
                    TimeSignatures = new List<(int AbsoluteTick, (byte Numerator, byte Denominator))>
                    {
                        (0, (4, 4))
                    }
                };
                try
                {
                    p.SaveAsMIDI(d.FileName, args);
                    FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveMIDI, d.FileName), Text);
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorSaveMIDI);
                }
            }
        }
        private void ExportSF2(object sender, EventArgs e)
        {
            var d = new SaveFileDialog
            {
                FileName = Engine.Instance.Config.GetGameName(),
                DefaultExt = ".sf2",
                ValidateNames = true,
                Title = Strings.MenuSaveSF2,
                Filter = Strings.FilterSaveSF2
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Core.GBA.AlphaDream.SoundFontSaver_SF2.Save((Core.GBA.AlphaDream.Config)Engine.Instance.Config, d.FileName);
                    FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveSF2, d.FileName), Text);
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorSaveSF2);
                }
            }
        }
        private void ExportWAV(object sender, EventArgs e)
        {
            var d = new SaveFileDialog
            {
                FileName = Engine.Instance.Config.GetSongName((long)_songNumerical.Value),
                DefaultExt = ".wav",
                ValidateNames = true,
                Title = Strings.MenuSaveWAV,
                Filter = Strings.FilterSaveWAV
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                Stop();
                bool oldFade = Engine.Instance.Player.ShouldFadeOut;
                long oldLoops = Engine.Instance.Player.NumLoops;
                Engine.Instance.Player.ShouldFadeOut = true;
                Engine.Instance.Player.NumLoops = GlobalConfig.Instance.PlaylistSongLoops;
                try
                {
                    Engine.Instance.Player.Record(d.FileName);
                    FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveWAV, d.FileName), Text);
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex, Strings.ErrorSaveWAV);
                }
                Engine.Instance.Player.ShouldFadeOut = oldFade;
                Engine.Instance.Player.NumLoops = oldLoops;
                _stopUI = false;
            }
        }

        public void LetUIKnowPlayerIsPlaying()
        {
            if (!_timer.Enabled)
            {
                _pauseButton.Enabled = _stopButton.Enabled = true;
                _pauseButton.Text = Strings.PlayerPause;
                _timer.Interval = (int)(1_000d / GlobalConfig.Instance.RefreshRate);
                _timer.Start();
                //UpdateTaskbarState();
                //UpdateTaskbarButtons();
            }
        }
        private void Play()
        {
            Engine.Instance.Player.Play();
            LetUIKnowPlayerIsPlaying();
        }
        private void Pause()
        {
            Engine.Instance.Player.Pause();
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
            //UpdateTaskbarState();
            //UpdateTaskbarButtons();
        }
        private void Stop()
        {
            Engine.Instance.Player.Stop();
            _pauseButton.Enabled = _stopButton.Enabled = false;
            _pauseButton.Text = Strings.PlayerPause;
            _timer.Stop();
            _songInfo.DeleteData();
            _piano.UpdateKeys(_songInfo.Info, PianoTracks);
            UpdatePositionIndicators(0L);
            //UpdateTaskbarState();
            //UpdateTaskbarButtons();
        }
        private void TogglePlayback(object sender, EventArgs e)
        {
            if (Engine.Instance.Player.State == PlayerState.Stopped)
            {
                Play();
            }
            else if (Engine.Instance.Player.State == PlayerState.Paused || Engine.Instance.Player.State == PlayerState.Playing)
            {
                Pause();
            }
        }
        private void PlayPreviousSong(object sender, EventArgs e)
        {
            long prevSong;
            if (_playlistPlaying)
            {
                int index = _playedSongs.Count - 1;
                prevSong = _playedSongs[index];
                _playedSongs.RemoveAt(index);
                _remainingSongs.Insert(0, _curSong);
            }
            else
            {
                prevSong = (long)_songNumerical.Value - 1;
            }
            SetAndLoadSong(prevSong);
        }
        private void PlayNextSong(object sender, EventArgs e)
        {
            if (_playlistPlaying)
            {
                _playedSongs.Add(_curSong);
                SetAndLoadNextPlaylistSong();
            }
            else
            {
                SetAndLoadSong((long)_songNumerical.Value + 1);
            }
        }

        private void FinishLoading(long numSongs)
        {
            Engine.Instance.Player.SongEnded += SongEnded;
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
            //UpdateTaskbarButtons();
        }
        private void DisposeEngine()
        {
            if (Engine.Instance != null)
            {
                Stop();
                Engine.Instance.Dispose();
            }
            _trackViewer?.UpdateTracks();
            //_prevTButton.Enabled = _toggleTButton.Enabled = _nextTButton.Enabled = _songsComboBox.Enabled = _songNumerical.Enabled = _playButton.Enabled = _volumeBar.Enabled = _positionBar.Enabled = false;
            Text = Utils.ProgramName;
            _songInfo.SetNumTracks(0);
            _songInfo.ResetMutes();
            ResetPlaylistStuff(false);
            UpdatePositionIndicators(0L);
            //UpdateTaskbarState();
            _songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;
            _songNumerical.ValueChanged -= SongNumerical_ValueChanged;
            _songNumerical.Visible = false;
            _songNumerical.Value = _songNumerical.Maximum = 0;
            _songsComboBox.SelectedItem = null;
            _songsComboBox.Items.Clear();
            _songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
            _songNumerical.ValueChanged += SongNumerical_ValueChanged;
        }
        private bool _stopUI = false;
        private void UpdateUI(object sender, EventArgs e)
        {
            if (_stopUI)
            {
                _stopUI = false;
                if (_playlistPlaying)
                {
                    _playedSongs.Add(_curSong);
                    SetAndLoadNextPlaylistSong();
                }
                else
                {
                    Stop();
                }
            }
            else
            {
                if (WindowState != FormWindowState.Minimized)
                {
                    SongInfoControl.SongInfo info = _songInfo.Info;
                    Engine.Instance.Player.GetSongState(info);
                    _piano.UpdateKeys(info, PianoTracks);
                    _songInfo.Invalidate();
                }
                UpdatePositionIndicators(Engine.Instance.Player.ElapsedTicks);
            }
        }
        private void SongEnded()
        {
            _stopUI = true;
        }

        private void UpdatePositionIndicators(long ticks)
        {
            if (_positionBarFree)
            {
                _positionBar.Value = ticks;
            }
            /*
            if (GlobalConfig.Instance.TaskbarProgress && TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue((int)ticks, (int)_positionBar.Maximum);
            }
            */
        }

        /*
        private void UpdateTaskbarState()
        {
            if (GlobalConfig.Instance.TaskbarProgress && TaskbarManager.IsPlatformSupported)
            {
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
        private void UpdateTaskbarButtons()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                if (_playlistPlaying)
                {
                    _prevTButton.Enabled = _playedSongs.Count > 0;
                    _nextTButton.Enabled = true;
                }
                else
                {
                    _prevTButton.Enabled = _curSong > 0;
                    _nextTButton.Enabled = _curSong < _songNumerical.Maximum;
                }
                switch (Engine.Instance.Player.State)
                {
                    case PlayerState.Stopped: _toggleTButton.Icon = Resources.IconPlay; _toggleTButton.Tooltip = Strings.PlayerPlay; break;
                    case PlayerState.Playing: _toggleTButton.Icon = Resources.IconPause; _toggleTButton.Tooltip = Strings.PlayerPause; break;
                    case PlayerState.Paused: _toggleTButton.Icon = Resources.IconPlay; _toggleTButton.Tooltip = Strings.PlayerUnpause; break;
                }
                _toggleTButton.Enabled = true;
            }
        }*/

        private void OpenTrackViewer(object sender, EventArgs e)
        {
            if (_trackViewer != null)
            {
                _trackViewer.Focus();
                return;
            }
            _trackViewer = new TrackViewer { Owner = this };
            _trackViewer.FormClosed += (o, s) => _trackViewer = null;
            _trackViewer.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DisposeEngine();
            base.OnFormClosing(e);
        }
        private void OnResize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
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
                _piano.Location = new Point((_splitContainer.Panel1.Width - (_piano.WhiteKeyWidth * PianoControl.WhiteKeyCount)) / 2, _splitContainer.Panel1.Height - _piano.Height - 1);
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space && _playButton.Enabled && !_songsComboBox.Focused)
            {
                TogglePlayback(null, null);
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
