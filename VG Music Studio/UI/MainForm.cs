using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    [DesignerCategory("")]
    internal class MainForm : ThemedForm
    {
        private const int intendedWidth = 675,
            intendedHeight = 675 + 1 + 125 + 24;

        public static MainForm Instance { get; } = new MainForm();

        private readonly List<byte> pianoNotes = new List<byte>();
        public readonly bool[] PianoTracks = new bool[0x10];

        public bool PlaylistPlaying;
        private Config.Playlist curPlaylist;
        private long curSong = -1;
        private readonly List<long> playedSongs = new List<long>(),
            remainingSongs = new List<long>();

        private TrackViewer trackViewer;

        #region Controls

        private readonly IContainer components;
        private readonly MenuStrip mainMenu;
        private readonly ToolStripMenuItem fileItem, openDSEItem, openMLSSItem, openMP2KItem, openSDATItem,
            dataItem, trackViewerItem, exportMIDIItem,
            playlistItem, endPlaylistItem;
        private readonly Timer timer;
        private readonly ThemedNumeric songNumerical;
        private readonly ThemedButton playButton, pauseButton, stopButton;
        private readonly SplitContainer splitContainer;
        private readonly PianoControl piano;
        private readonly ColorSlider volumeBar, positionBar;
        private readonly TrackInfoControl trackInfo;
        private readonly ImageComboBox songsComboBox;
        private readonly ThumbnailToolBarButton prevTButton, toggleTButton, nextTButton;

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        private MainForm()
        {
            for (int i = 0; i < PianoTracks.Length; i++)
            {
                PianoTracks[i] = true;
            }

            components = new Container();

            // File Menu
            openDSEItem = new ToolStripMenuItem { Text = "Open DSE Folder" };
            openDSEItem.Click += OpenDSE;
            openMLSSItem = new ToolStripMenuItem { Text = "Open GBA ROM (MLSS)" };
            openMLSSItem.Click += OpenMLSS;
            openMP2KItem = new ToolStripMenuItem { Text = "Open GBA ROM (MP2K)" };
            openMP2KItem.Click += OpenMP2K;
            openSDATItem = new ToolStripMenuItem { Text = "Open SDAT File" };
            openSDATItem.Click += OpenSDAT;
            fileItem = new ToolStripMenuItem { Text = Strings.MenuFile };
            fileItem.DropDownItems.AddRange(new ToolStripItem[] { openDSEItem, openMLSSItem, openMP2KItem, openSDATItem });

            // Data Menu
            trackViewerItem = new ToolStripMenuItem { ShortcutKeys = Keys.Control | Keys.T, Text = "Track Viewer" };
            trackViewerItem.Click += OpenTrackViewer;
            exportMIDIItem = new ToolStripMenuItem { Enabled = false, Text = "Export Song as MIDI" };
            exportMIDIItem.Click += ExportMIDI;
            dataItem = new ToolStripMenuItem { Text = Strings.MenuData };
            dataItem.DropDownItems.AddRange(new ToolStripItem[] { trackViewerItem, exportMIDIItem });

            // Playlist Menu
            endPlaylistItem = new ToolStripMenuItem { Enabled = false, Text = "End Current Playlist" };
            endPlaylistItem.Click += EndCurrentPlaylist;
            playlistItem = new ToolStripMenuItem { Text = "Playlist" };
            playlistItem.DropDownItems.AddRange(new ToolStripItem[] { endPlaylistItem });

            // Main Menu
            mainMenu = new MenuStrip { Size = new Size(intendedWidth, 24) };
            mainMenu.Items.AddRange(new ToolStripItem[] { fileItem, dataItem, playlistItem });

            // Buttons
            playButton = new ThemedButton { Enabled = false, ForeColor = Color.MediumSpringGreen, Text = Strings.PlayerPlay };
            playButton.Click += (o, e) => Play();
            pauseButton = new ThemedButton { Enabled = false, ForeColor = Color.DeepSkyBlue, Text = Strings.PlayerPause };
            pauseButton.Click += (o, e) => Pause();
            stopButton = new ThemedButton { Enabled = false, ForeColor = Color.MediumVioletRed, Text = Strings.PlayerStop };
            stopButton.Click += (o, e) => Stop();

            // Numerical
            songNumerical = new ThemedNumeric { Enabled = false, Minimum = 0, Visible = false };
            songNumerical.ValueChanged += SongNumerical_ValueChanged;

            // Timer
            timer = new Timer(components);
            timer.Tick += UpdateUI;

            // Piano
            piano = new PianoControl { HighNoteID = 127, LowNoteID = 0 };

            // Volume bar
            volumeBar = new ColorSlider { Enabled = false, LargeChange = 20, Maximum = 100, SmallChange = 5 };
            volumeBar.ValueChanged += VolumeBar_ValueChanged;

            // Position bar
            positionBar = new ColorSlider { Enabled = false, Maximum = 0 };
            positionBar.MouseUp += PositionBar_MouseUp;
            positionBar.MouseDown += PositionBar_MouseDown;

            // Playlist box
            songsComboBox = new ImageComboBox { Enabled = false };
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;

            // Track info
            trackInfo = new TrackInfoControl { Dock = DockStyle.Fill };

            // Split container
            splitContainer = new SplitContainer { BackColor = Theme.TitleBar, Dock = DockStyle.Fill, IsSplitterFixed = true, Orientation = Orientation.Horizontal, SplitterWidth = 1 };
            splitContainer.Panel1.Controls.AddRange(new Control[] { playButton, pauseButton, stopButton, songNumerical, songsComboBox, piano, volumeBar, positionBar });
            splitContainer.Panel2.Controls.Add(trackInfo);

            // MainForm
            ClientSize = new Size(intendedWidth, intendedHeight);
            Controls.AddRange(new Control[] { splitContainer, mainMenu });
            MainMenuStrip = mainMenu;
            MinimumSize = new Size(intendedWidth + (Width - intendedWidth), intendedHeight + (Height - intendedHeight)); // Borders
            Resize += OnResize;
            Text = Utils.ProgramName;

            // Taskbar Buttons
            if (TaskbarManager.IsPlatformSupported)
            {
                prevTButton = new ThumbnailToolBarButton(Resources.IconPrevious, Strings.PlayerPreviousSong);
                prevTButton.Click += PlayPreviousSong;
                toggleTButton = new ThumbnailToolBarButton(Resources.IconPlay, Strings.PlayerPlay);
                toggleTButton.Click += TogglePlayback;
                nextTButton = new ThumbnailToolBarButton(Resources.IconNext, Strings.PlayerNextSong);
                nextTButton.Click += PlayNextSong;
                prevTButton.Enabled = toggleTButton.Enabled = nextTButton.Enabled = false;
                TaskbarManager.Instance.ThumbnailToolBars.AddButtons(Handle, prevTButton, toggleTButton, nextTButton);
            }

            OnResize(null, null);
        }

        private void VolumeBar_ValueChanged(object sender, EventArgs e)
        {
            Engine.Instance.Mixer.SetVolume(volumeBar.Value / (float)volumeBar.Maximum);
        }
        public void SetVolumeBarValue(float volume)
        {
            volumeBar.ValueChanged -= VolumeBar_ValueChanged;
            volumeBar.Value = (int)(volume * volumeBar.Maximum);
            volumeBar.ValueChanged += VolumeBar_ValueChanged;
        }
        private bool positionBarFree = true;
        private void PositionBar_MouseUp(object sender, EventArgs e)
        {
            Engine.Instance.Player.SetCurrentPosition(positionBar.Value);
            positionBarFree = true;
            LetUIKnowPlayerIsPlaying();
        }
        private void PositionBar_MouseDown(object sender, EventArgs e)
        {
            positionBarFree = false;
        }

        private bool autoplay = false;
        private void SongNumerical_ValueChanged(object sender, EventArgs e)
        {
            songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;

            long index = (long)songNumerical.Value;
            Stop();
            Text = Utils.ProgramName;
            songsComboBox.SelectedIndex = 0;
            trackInfo.DeleteData();
            bool success;
            try
            {
                Engine.Instance.Player.LoadSong(index);
                success = true;
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, string.Format(Strings.ErrorLoadSong, songNumerical.Value));
                success = false;
            }

            trackViewer?.UpdateTracks();
            if (success)
            {
                Config config = Engine.Instance.Config;
                List<Config.Song> songs = config.Playlists[0].Songs; // Complete "Music" playlist is present in all configs at index 0
                Config.Song song = songs.SingleOrDefault(s => s.Index == index);
                if (song != null)
                {
                    Text = $"{Utils.ProgramName} - {song.Name}";
                    songsComboBox.SelectedIndex = songs.IndexOf(song) + 1; // + 1 because the "Music" playlist is first in the combobox
                }
                positionBar.Maximum = (int)Engine.Instance.Player.MaxTicks;
                positionBar.LargeChange = positionBar.Maximum / 10;
                positionBar.SmallChange = positionBar.LargeChange / 4;
                if (autoplay)
                {
                    Play();
                }
            }
            positionBar.Enabled = success;
            exportMIDIItem.Enabled = success && Engine.Instance.Type == Engine.EngineType.GBA_MP2K && Engine.Instance.Player.Events.Length > 0;

            autoplay = true;
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
        }
        private void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = (ImageComboBoxItem)songsComboBox.SelectedItem;
            if (item.Item is Config.Song song)
            {
                SetAndLoadSong(song.Index);
            }
            else if (item.Item is Config.Playlist playlist)
            {
                if (playlist.Songs.Count > 0
                    && FlexibleMessageBox.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), "Playlist", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ResetPlaylistStuff(false);
                    curPlaylist = playlist;
                    PlaylistPlaying = true;
                    endPlaylistItem.Enabled = true;
                    SetAndLoadNextPlaylistSong();
                }
            }
        }
        private void SetAndLoadSong(long index)
        {
            curSong = index;
            if (songNumerical.Value == index)
            {
                SongNumerical_ValueChanged(null, null);
            }
            else
            {
                songNumerical.Value = index;
            }
        }
        private void SetAndLoadNextPlaylistSong()
        {
            if (remainingSongs.Count == 0)
            {
                remainingSongs.AddRange(curPlaylist.Songs.Select(s => s.Index));
                if (GlobalConfig.Instance.PlaylistMode == PlaylistMode.Random)
                {
                    remainingSongs.Shuffle();
                }
            }
            long nextSong = remainingSongs[0];
            remainingSongs.RemoveAt(0);
            SetAndLoadSong(nextSong);
        }
        private void ResetPlaylistStuff(bool enableds)
        {
            PlaylistPlaying = false;
            curPlaylist = null;
            curSong = -1;
            remainingSongs.Clear();
            playedSongs.Clear();
            endPlaylistItem.Enabled = false;
            songNumerical.Enabled = songsComboBox.Enabled = enableds;
        }
        private void EndCurrentPlaylist(object sender, EventArgs e)
        {
            if (FlexibleMessageBox.Show("Would you like to stop playing the current playlist?", "Playlist", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ResetPlaylistStuff(true);
            }
        }

        private void OpenDSE(object sender, EventArgs e)
        {
            var d = new CommonOpenFileDialog
            {
                Title = "Open DSE Folder",
                IsFolderPicker = true
            };
            if (d.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DisposeEngine();
                bool success;
                try
                {
                    new Engine(Engine.EngineType.NDS_DSE, d.FileName);
                    success = true;
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex.Message, "Error Loading DSE");
                    success = false;
                }
                if (success)
                {
                    var config = (Core.NDS.DSE.Config)Engine.Instance.Config;
                    FinishLoading(false, config.BGMFiles.Length);
                }
            }
        }
        private void OpenMLSS(object sender, EventArgs e)
        {
            var d = new CommonOpenFileDialog
            {
                Title = "Open GBA ROM (MLSS)",
                Filters = { new CommonFileDialogFilter("GBA Files", ".gba") }
            };
            if (d.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DisposeEngine();
                bool success;
                try
                {
                    new Engine(Engine.EngineType.GBA_MLSS, File.ReadAllBytes(d.FileName));
                    success = true;
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(ex.Message, "Error Loading GBA ROM (MLSS)");
                    success = false;
                }
                if (success)
                {
                    var config = (Core.GBA.MLSS.Config)Engine.Instance.Config;
                    FinishLoading(true, config.SongTableSizes[0]);
                }
            }
        }
        private void OpenMP2K(object sender, EventArgs e)
        {
            var d = new CommonOpenFileDialog
            {
                Title = "Open GBA ROM (MP2K)",
                Filters = { new CommonFileDialogFilter("GBA Files", ".gba") }
            };
            if (d.ShowDialog() == CommonFileDialogResult.Ok)
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
                    FlexibleMessageBox.Show(ex.Message, "Error Loading GBA ROM (MP2K)");
                    success = false;
                }
                if (success)
                {
                    var config = (Core.GBA.MP2K.Config)Engine.Instance.Config;
                    FinishLoading(true, config.SongTableSizes[0]);
                }
            }
        }
        private void OpenSDAT(object sender, EventArgs e)
        {
            var d = new CommonOpenFileDialog
            {
                Title = "Open SDAT File",
                Filters = { new CommonFileDialogFilter("SDAT Files", ".sdat") }
            };
            if (d.ShowDialog() == CommonFileDialogResult.Ok)
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
                    FlexibleMessageBox.Show(ex.Message, "Error Loading SDAT File");
                    success = false;
                }
                if (success)
                {
                    var config = (Core.NDS.SDAT.Config)Engine.Instance.Config;
                    FinishLoading(true, config.SDAT.INFOBlock.SequenceInfos.NumEntries);
                }
            }
        }

        private void ExportMIDI(object sender, EventArgs e)
        {
            var d = new CommonSaveFileDialog
            {
                Title = "Export Song as MIDI File",
                Filters = { new CommonFileDialogFilter("MIDI Files", ".mid;.midi") }
            };
            if (d.ShowDialog() == CommonFileDialogResult.Ok)
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
                    FlexibleMessageBox.Show(ex.Message, Strings.ErrorSaveMIDI);
                }
            }
        }

        public void LetUIKnowPlayerIsPlaying()
        {
            if (!timer.Enabled)
            {
                pauseButton.Enabled = stopButton.Enabled = true;
                pauseButton.Text = Strings.PlayerPause;
                timer.Interval = (int)(1000.0 / GlobalConfig.Instance.RefreshRate);
                timer.Start();
                UpdateTaskbarState();
                UpdateTaskbarButtons();
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
            if (Engine.Instance.Player.State != PlayerState.Paused)
            {
                stopButton.Enabled = true;
                pauseButton.Text = Strings.PlayerPause;
                timer.Start();
            }
            else
            {
                stopButton.Enabled = false;
                pauseButton.Text = Strings.PlayerUnpause;
                timer.Stop();
                System.Threading.Monitor.Enter(timer);
                ClearPianoNotes();
            }
            UpdateTaskbarState();
            UpdateTaskbarButtons();
        }
        private void Stop()
        {
            Engine.Instance.Player.Stop();
            pauseButton.Enabled = stopButton.Enabled = false;
            timer.Stop();
            System.Threading.Monitor.Enter(timer);
            ClearPianoNotes();
            trackInfo.DeleteData();
            UpdatePositionIndicators(0);
            UpdateTaskbarState();
            UpdateTaskbarButtons();
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
            if (PlaylistPlaying)
            {
                int index = playedSongs.Count - 1;
                prevSong = playedSongs[index];
                playedSongs.RemoveAt(index);
                remainingSongs.Insert(0, curSong);
            }
            else
            {
                prevSong = (long)songNumerical.Value - 1;
            }
            SetAndLoadSong(prevSong);
        }
        private void PlayNextSong(object sender, EventArgs e)
        {
            if (PlaylistPlaying)
            {
                playedSongs.Add(curSong);
                SetAndLoadNextPlaylistSong();
            }
            else
            {
                SetAndLoadSong((long)songNumerical.Value + 1);
            }
        }

        private void FinishLoading(bool numericalVisible, long numSongs)
        {
            Engine.Instance.Player.SongEnded += SongEnded;
            foreach (Config.Playlist playlist in Engine.Instance.Config.Playlists)
            {
                songsComboBox.Items.Add(new ImageComboBoxItem(playlist, Resources.IconPlaylist, 0));
                songsComboBox.Items.AddRange(playlist.Songs.Select(s => new ImageComboBoxItem(s, Resources.IconSong, 1)).ToArray());
            }
            songNumerical.Maximum = numSongs - 1;
#if DEBUG
            //Debug.EventScan(numSongs);
#endif
            autoplay = false;
            SetAndLoadSong(Engine.Instance.Config.Playlists[0].Songs.Count == 0 ? 0 : Engine.Instance.Config.Playlists[0].Songs[0].Index);
            songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = volumeBar.Enabled = true;
            songNumerical.Visible = numericalVisible;
            UpdateTaskbarButtons();
        }
        private void DisposeEngine()
        {
            if (Engine.Instance != null)
            {
                Stop();
                Engine.Instance.Dispose();
            }
            trackViewer?.UpdateTracks();
            prevTButton.Enabled = toggleTButton.Enabled = nextTButton.Enabled = songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = volumeBar.Enabled = positionBar.Enabled = false;
            Text = Utils.ProgramName;
            ResetPlaylistStuff(false);
            UpdatePositionIndicators(0);
            UpdateTaskbarState();
            songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;
            songNumerical.ValueChanged -= SongNumerical_ValueChanged;
            songNumerical.Visible = false;
            songNumerical.Value = songNumerical.Maximum = 0;
            songsComboBox.SelectedItem = null;
            songsComboBox.Items.Clear();
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
            songNumerical.ValueChanged += SongNumerical_ValueChanged;
        }
        private void ClearPianoNotes()
        {
            foreach (byte n in pianoNotes)
            {
                if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                {
                    piano[n - piano.LowNoteID].NoteOnColor = Color.DeepSkyBlue;
                    piano.ReleasePianoKey(n);
                }
            }
            pianoNotes.Clear();
        }
        private bool stopUI = false;
        private void UpdateUI(object sender, EventArgs e)
        {
            if (System.Threading.Monitor.TryEnter(timer))
            {
                if (stopUI)
                {
                    stopUI = false;
                    if (PlaylistPlaying)
                    {
                        playedSongs.Add(curSong);
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
                        TrackInfoControl.TrackInfo info = trackInfo.Info;
                        Engine.Instance.Player.GetSongState(info);
                        ClearPianoNotes();
                        for (int i = PianoTracks.Length - 1; i >= 0; i--)
                        {
                            if (PianoTracks[i])
                            {
                                byte[] notes = info.Notes[i];
                                pianoNotes.AddRange(notes);
                                for (int j = 0; j < notes.Length; j++)
                                {
                                    byte n = notes[j];
                                    if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                                    {
                                        piano[n - piano.LowNoteID].NoteOnColor = GlobalConfig.Instance.Colors[info.Voices[i]];
                                        piano.PressPianoKey(n);
                                    }
                                }
                            }
                        }
                        trackInfo.Invalidate();
                    }
                    UpdatePositionIndicators((int)Engine.Instance.Player.ElapsedTicks);
                }
                System.Threading.Monitor.Exit(timer);
            }
        }
        private void SongEnded()
        {
            stopUI = true;
        }
        private void UpdatePositionIndicators(int ticks)
        {
            if (positionBarFree)
            {
                positionBar.Value = ticks;
            }
            if (GlobalConfig.Instance.TaskbarProgress && TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(ticks, positionBar.Maximum);
            }
        }
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
                if (PlaylistPlaying)
                {
                    prevTButton.Enabled = playedSongs.Count > 0;
                    nextTButton.Enabled = true;
                }
                else
                {
                    prevTButton.Enabled = curSong > 0;
                    nextTButton.Enabled = curSong < songNumerical.Maximum;
                }
                switch (Engine.Instance.Player.State)
                {
                    case PlayerState.Stopped: toggleTButton.Icon = Resources.IconPlay; toggleTButton.Tooltip = Strings.PlayerPlay; break;
                    case PlayerState.Playing: toggleTButton.Icon = Resources.IconPause; toggleTButton.Tooltip = Strings.PlayerPause; break;
                    case PlayerState.Paused: toggleTButton.Icon = Resources.IconPlay; toggleTButton.Tooltip = Strings.PlayerUnpause; break;
                }
                toggleTButton.Enabled = true;
            }
        }

        private void OpenTrackViewer(object sender, EventArgs e)
        {
            if (trackViewer != null)
            {
                trackViewer.Focus();
                return;
            }
            trackViewer = new TrackViewer { Owner = this };
            trackViewer.FormClosed += (o, s) => trackViewer = null;
            trackViewer.Show();
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
                splitContainer.SplitterDistance = (int)(ClientSize.Height / 5.5) - 25; // -25 for menustrip (24) and itself (1)

                int w1 = (int)(splitContainer.Panel1.Width / 2.35);
                int h1 = (int)(splitContainer.Panel1.Height / 5.0);

                int xoff = splitContainer.Panel1.Width / 83;
                int yoff = splitContainer.Panel1.Height / 25;
                int a, b, c, d;

                // Buttons
                a = (w1 / 3) - xoff;
                b = (xoff / 2) + 1;
                playButton.Location = new Point(xoff + b, yoff);
                pauseButton.Location = new Point((xoff * 2) + a + b, yoff);
                stopButton.Location = new Point((xoff * 3) + (a * 2) + b, yoff);
                playButton.Size = pauseButton.Size = stopButton.Size = new Size(a, h1);
                c = yoff + ((h1 - 21) / 2);
                songNumerical.Location = new Point((xoff * 4) + (a * 3) + b, c);
                songNumerical.Size = new Size((int)(a / 1.175), 21);
                // Song combobox
                d = splitContainer.Panel1.Width - w1 - xoff;
                songsComboBox.Location = new Point(d, c);
                songsComboBox.Size = new Size(w1, 21);

                // Volume bar
                c = (int)(splitContainer.Panel1.Height / 3.5);
                volumeBar.Location = new Point(xoff, c);
                volumeBar.Size = new Size(w1, h1);
                // Position bar
                positionBar.Location = new Point(d, c);
                positionBar.Size = new Size(w1, h1);

                // Piano
                piano.Size = new Size(splitContainer.Panel1.Width, (int)(splitContainer.Panel1.Height / 2.5)); // Force it to initialize piano keys again
                piano.Location = new Point((splitContainer.Panel1.Width - (piano[0].Width * piano.WhiteKeyCount)) / 2, splitContainer.Panel1.Height - piano.Height - 1);
                piano.Invalidate(true);
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space && playButton.Enabled && !songsComboBox.Focused)
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
