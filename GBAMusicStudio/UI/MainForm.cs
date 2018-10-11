using GBAMusicStudio.Core;
using GBAMusicStudio.MIDI;
using GBAMusicStudio.Properties;
using GBAMusicStudio.Util;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [DesignerCategory("")]
    class MainForm : Form
    {
        bool stopUI = false, drag = false;
        List<sbyte> pianoNotes = new List<sbyte>();
        public readonly bool[] PianoTracks = new bool[16];

        APlaylist curPlaylist;
        int curSong = 0;
        List<int> playedSongs = new List<int>(), remainingSongs = new List<int>();

        readonly int iWidth = 528, iHeight = 800 + 25; // +25 for menustrip (24) and splitcontainer separator (1)
        readonly float sfWidth = 2.35f; // Song combobox and volumebar width
        readonly float spfHeight = 5.5f; // Split panel 1 height

        AssemblerDialog assemblerDialog; MIDIConverterDialog midiConverterDialog;
        TrackEditor trackEditor; VoiceTableEditor voiceTableEditor;

        #region Controls

        IContainer components;
        MenuStrip mainMenu;
        ToolStripMenuItem fileToolStripMenuItem, openROMToolStripMenuItem, openMIDIToolStripMenuItem, openASMToolStripMenuItem, configToolStripMenuItem,
            dataToolStripMenuItem, teToolStripMenuItem, vteToolStripMenuItem, eSf2ToolStripMenuItem, eASMToolStripMenuItem, eMIDIToolStripMenuItem;
        Timer timer;
        readonly object timerLock = new object();
        ThemedNumeric songNumerical, tableNumerical;
        ThemedButton playButton, stopButton, pauseButton;
        ThemedLabel creatorLabel, gameLabel, codeLabel;
        SplitContainer splitContainer;
        PianoControl piano;
        ColorSlider positionBar, volumeBar;
        TrackInfoControl trackInfo;
        ImageComboBox songsComboBox;

        ThumbnailToolBarButton prevTButton, toggleTButton, nextTButton;

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        public MainForm()
        {
            components = new Container();

            // Main Menu
            openROMToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuOpenROM, ShortcutKeys = Keys.Control | Keys.O };
            openROMToolStripMenuItem.Click += OpenROM;

            openMIDIToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuOpenMIDI, Enabled = false, ShortcutKeys = Keys.Control | Keys.M };
            openMIDIToolStripMenuItem.Click += OpenMIDIConverter;

            openASMToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuOpenASM, Enabled = false, ShortcutKeys = Keys.Control | Keys.Shift | Keys.M };
            openASMToolStripMenuItem.Click += OpenAssembler;

            configToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuRefreshConfig, ShortcutKeys = Keys.Control | Keys.R };
            configToolStripMenuItem.Click += ReloadConfig;

            fileToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuFile };
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openROMToolStripMenuItem, openMIDIToolStripMenuItem, openASMToolStripMenuItem, configToolStripMenuItem });


            teToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuTrackEditor, Enabled = false, ShortcutKeys = Keys.Control | Keys.T };
            teToolStripMenuItem.Click += OpenTrackEditor;

            vteToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuVoiceTableEditor, Enabled = false, ShortcutKeys = Keys.Control | Keys.V };
            vteToolStripMenuItem.Click += OpenVoiceTableEditor;

            eSf2ToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuSaveSF2, Enabled = false };
            eSf2ToolStripMenuItem.Click += ExportSF2;

            eASMToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuSaveASM, Enabled = false };
            eASMToolStripMenuItem.Click += ExportASM;

            eMIDIToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuSaveMIDI, Enabled = false };
            eMIDIToolStripMenuItem.Click += ExportMIDI;

            dataToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuData };
            dataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { teToolStripMenuItem, vteToolStripMenuItem, eSf2ToolStripMenuItem, eASMToolStripMenuItem, eMIDIToolStripMenuItem });


            mainMenu = new MenuStrip { Size = new Size(iWidth, 24) };
            mainMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, dataToolStripMenuItem });

            // Buttons
            playButton = new ThemedButton { ForeColor = Color.MediumSpringGreen, Location = new Point(5, 3), Text = Strings.PlayerPlay };
            playButton.Click += (o, e) => Play();
            pauseButton = new ThemedButton { ForeColor = Color.DeepSkyBlue, Location = new Point(85, 3), Text = Strings.PlayerPause };
            pauseButton.Click += (o, e) => Pause();
            stopButton = new ThemedButton { ForeColor = Color.MediumVioletRed, Location = new Point(166, 3), Text = Strings.PlayerStop };
            stopButton.Click += (o, e) => Stop();

            playButton.Enabled = pauseButton.Enabled = stopButton.Enabled = false;
            playButton.Size = stopButton.Size = new Size(75, 23);
            pauseButton.Size = new Size(76, 23);

            // Numericals
            songNumerical = new ThemedNumeric { Enabled = false, Location = new Point(246, 4) };
            tableNumerical = new ThemedNumeric { Location = new Point(246, 35), Maximum = 0, Visible = false };

            songNumerical.Size = tableNumerical.Size = new Size(45, 23);
            songNumerical.ValueChanged += (o, e) => LoadSong();
            tableNumerical.ValueChanged += TableIndexChanged;

            // Labels
            creatorLabel = new ThemedLabel { Location = new Point(3, 43), Size = new Size(72, 13) };
            gameLabel = new ThemedLabel { Location = new Point(3, 30), Size = new Size(66, 13) };
            codeLabel = new ThemedLabel { Location = new Point(3, 56), Size = new Size(63, 13) };

            creatorLabel.AutoSize = gameLabel.AutoSize = codeLabel.AutoSize = true;
            creatorLabel.TextAlign = gameLabel.TextAlign = codeLabel.TextAlign = ContentAlignment.MiddleCenter;

            // Timer
            timer = new Timer(components);
            timer.Tick += UpdateUI;

            // Piano
            piano = new PianoControl { Anchor = AnchorStyles.Bottom, Location = new Point(0, 125 - 50 - 1), Size = new Size(iWidth, 50) };

            // Volume bar & Position bar
            int sWidth = (int)(iWidth / sfWidth);
            int sX = iWidth - sWidth - 4;
            positionBar = new ColorSlider()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false,
                Location = new Point(sX, 45),
                Maximum = 0,
                Size = new Size(sWidth, 27)
            };
            positionBar.MouseUp += SetSongPosition;
            positionBar.MouseDown += (o, e) => drag = true;
            volumeBar = new ColorSlider()
            {
                LargeChange = 20,
                Location = new Point(83, 45),
                Maximum = 100,
                Size = new Size(155, 27),
                SmallChange = 5
            };
            volumeBar.ValueChanged += (o, e) => SoundMixer.Instance.MasterVolume = (volumeBar.Value / (float)volumeBar.Maximum);
            volumeBar.Value = Config.Instance.Volume; // Update MusicPlayer volume

            // Playlist box
            songsComboBox = new ImageComboBox()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false,
                Location = new Point(sX, 4),
                Size = new Size(sWidth, 23)
            };
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;

            // Track info
            trackInfo = new TrackInfoControl()
            {
                Dock = DockStyle.Fill,
                Size = new Size(iWidth, 690)
            };

            // Split container
            splitContainer = new SplitContainer()
            {
                BackColor = Theme.TitleBar,
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
                Orientation = Orientation.Horizontal,
                Size = new Size(iWidth, iHeight),
                SplitterDistance = 125,
                SplitterWidth = 1
            };
            splitContainer.Panel1.Controls.AddRange(new Control[] { playButton, creatorLabel, gameLabel, codeLabel, pauseButton, stopButton, songNumerical, tableNumerical, songsComboBox, piano, positionBar, volumeBar });
            splitContainer.Panel2.Controls.Add(trackInfo);

            // MainForm
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(iWidth, iHeight);
            Controls.AddRange(new Control[] { splitContainer, mainMenu });
            Icon = Resources.Icon;
            MainMenuStrip = mainMenu;
            MinimumSize = new Size(8 + iWidth + 8, 30 + iHeight + 8); // Borders
            SongPlayer.Instance.SongEnded += SongEnded;
            Resize += OnResize;
            Text = "GBA Music Studio";

            // Taskbar Buttons
            if (TaskbarManager.IsPlatformSupported)
            {
                prevTButton = new ThumbnailToolBarButton(Resources.IconPrevious, Strings.PlayerPreviousSong);
                prevTButton.Click += (o, e) => PlayPreviousSong();
                toggleTButton = new ThumbnailToolBarButton(Resources.IconPlay, Strings.PlayerPlay);
                toggleTButton.Click += (o, e) => TogglePlayback();
                nextTButton = new ThumbnailToolBarButton(Resources.IconNext, Strings.PlayerNextSong);
                nextTButton.Click += (o, e) => PlayNextSong();
                prevTButton.Enabled = toggleTButton.Enabled = nextTButton.Enabled = false;
                TaskbarManager.Instance.ThumbnailToolBars.AddButtons(Handle, prevTButton, toggleTButton, nextTButton);
            }
        }

        void SetSongPosition(object sender, EventArgs e)
        {
            SongPlayer.Instance.SetSongPosition(positionBar.Value);
            drag = false;
        }
        void SetSongMaximum()
        {
            playedSongs.Clear();
            songNumerical.Maximum = ROM.Instance.Game.SongTableSizes[(int)tableNumerical.Value] - 1;
        }
        void TableIndexChanged(object sender, EventArgs e)
        {
            SetSongMaximum();
            LoadSong();
        }

        void SetAndLoadSong(int song)
        {
            curSong = song;
            songNumerical.Value = song; // Calls LoadSong because of the numerical's event
        }
        void LoadSong()
        {
            songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;

            APlaylist mainPlaylist = ROM.Instance.Game.Playlists[0];
            List<ASong> songs = mainPlaylist.Songs.ToList();
            ASong song = songs.SingleOrDefault(s => s.Index == songNumerical.Value);
            if (song != null)
            {
                Text = "GBA Music Studio - " + song.Name;
                songsComboBox.SelectedIndex = songs.IndexOf(song) + 1; // + 1 for the Playlist index
            }
            else
            {
                Text = "GBA Music Studio";
                songsComboBox.SelectedIndex = 0;
            }
            bool playing = SongPlayer.Instance.State == PlayerState.Playing; // Play new song if one is already playing
            bool paused = SongPlayer.Instance.State == PlayerState.Paused;
            Stop();
            try
            {
                // Pause which stops the thread inside from processing during loading, which would increase the stream latency
                if (!paused)
                    SongPlayer.Instance.Pause();
                var loadedSong = ROM.Instance.SongTables[(int)tableNumerical.Value][(int)songNumerical.Value];
                SongPlayer.Instance.SetSong(loadedSong);
                // Then "un pause" it, setting it to the stopped state
                if (!paused)
                    SongPlayer.Instance.Stop();
                UpdateTrackInfo(playing);
                MIDIKeyboard.Instance.Start();
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, string.Format(Strings.ErrorLoadSong, songNumerical.Value));
            }

            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
        }
        void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = ((ImageComboBoxItem)songsComboBox.SelectedItem).Item;
            if (item is APlaylist playlist)
            {
                if (playlist.Songs.Length > 0
                    && FlexibleMessageBox.Show(string.Format(Strings.PlayPlaylistBody, Environment.NewLine + playlist), Strings.PlayPlaylistTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    curPlaylist = playlist;
                    remainingSongs.Clear();
                    SongPlayer.Instance.PlaylistPlaying = true;
                    PlayNextSong();
                }
            }
            else // Song was selected
            {
                if (SongPlayer.Instance.PlaylistPlaying)
                    playedSongs.Add(curSong);
                SetAndLoadSong(((ASong)item).Index);
            }
        }
        void SongEnded() => stopUI = true; // Allow MainForm's thread to do the next work in UpdateUI()
        public void PreviewSong(Song song, string caption)
        {
            Text = "GBA Music Studio - " + caption;
            bool playing = SongPlayer.Instance.State == PlayerState.Playing; // Play new song if one is already playing
            Stop();
            SongPlayer.Instance.SetSong(song);
            UpdateTrackInfo(playing);
        }
        void PopulatePlaylists(List<APlaylist> playlists)
        {
            songsComboBox.Items.Clear();
            foreach (var playlist in playlists)
            {
                // Add playlist to combobox
                songsComboBox.Items.Add(new ImageComboBoxItem(playlist, Resources.IconPlaylist, 0));
                // Add songs to combobox
                songsComboBox.Items.AddRange(playlist.Songs.Select(s => new ImageComboBoxItem(s, Resources.IconSong, 1)).ToArray());
            }
            // Load first song of complete playlist if there is one, otherwise load song 0
            if (playlists[0].Songs.Length == 0)
                SetAndLoadSong(0);
            else
                SetAndLoadSong(playlists[0].Songs[0].Index);
        }
        void EndCurrentPlaylist()
        {
            curPlaylist = null;
            playedSongs.Clear();
            remainingSongs.Clear();
            SongPlayer.Instance.PlaylistPlaying = false;
        }

        void OpenROM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = Strings.TitleOpenGBA, Filter = $"{Strings.FilterOpenGBA}|*.gba" };
            if (d.ShowDialog() != DialogResult.OK) return;

            Stop();

            try
            {
                new ROM(d.FileName);
                EndCurrentPlaylist();
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, Strings.ErrorOpenGBA);
                return;
            }

            UpdateMenuInfo();
            SongPlayer.Instance.Reset();
            LoadSong();
        }
        void OpenMIDIConverter(object sender, EventArgs e)
        {
            if (midiConverterDialog != null)
            {
                midiConverterDialog.Focus();
                return;
            }
            midiConverterDialog = new MIDIConverterDialog { Owner = this };
            midiConverterDialog.FormClosed += (o, s) => midiConverterDialog = null;
            midiConverterDialog.Show();
        }
        void OpenAssembler(object sender, EventArgs e)
        {
            if (assemblerDialog != null)
            {
                assemblerDialog.Focus();
                return;
            }
            assemblerDialog = new AssemblerDialog { Owner = this };
            assemblerDialog.FormClosed += (o, s) => assemblerDialog = null;
            assemblerDialog.Show();
        }
        void OpenTrackEditor(object sender, EventArgs e)
        {
            if (trackEditor != null)
            {
                trackEditor.Focus();
                return;
            }
            trackEditor = new TrackEditor { Owner = this };
            trackEditor.FormClosed += (o, s) => trackEditor = null;
            trackEditor.Show();
        }
        void OpenVoiceTableEditor(object sender, EventArgs e)
        {
            if (voiceTableEditor != null)
            {
                voiceTableEditor.Focus();
                return;
            }
            voiceTableEditor = new VoiceTableEditor { Owner = this };
            voiceTableEditor.FormClosed += (o, s) => voiceTableEditor = null;
            voiceTableEditor.Show();
        }
        void ReloadConfig(object sender, EventArgs e)
        {
            Config.Instance.Load();
            if (ROM.Instance != null)
            {
                ROM.Instance.HandleConfigLoaded();
                UpdateMenuInfo();
            }
        }
        void ExportSF2(object sender, EventArgs e)
        {
            var d = new SaveFileDialog { Title = Strings.TitleSaveSF2, Filter = $"{Strings.FilterSaveSF2}|*.sf2" };
            if (d.ShowDialog() != DialogResult.OK) return;

            try
            {
                VoiceTableSaver.Save(d.FileName);
                FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveSF2, d.FileName), Text);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, Strings.ErrorSaveSF2);
            }
        }
        void ExportASM(object sender, EventArgs e)
        {
            var d = new SaveFileDialog { Title = Strings.TitleSaveASM, Filter = $"{Strings.FilterSaveASM}|*.s" };
            if (d.ShowDialog() != DialogResult.OK) return;

            try
            {
                SongPlayer.Instance.Song.SaveAsASM(d.FileName);
                FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveASM, d.FileName), Text);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, Strings.ErrorSaveASM);
            }
        }
        void ExportMIDI(object sender, EventArgs e)
        {
            var d = new SaveFileDialog { Title = Strings.TitleSaveMIDI, Filter = $"{Strings.FilterSaveMIDI}|*.mid" };
            if (d.ShowDialog() != DialogResult.OK) return;

            try
            {
                var args = new MIDISaveArgs
                {
                    SaveBeforeKeysh = false,
                    ReverseVolume = false,
                    TimeSignatures = new List<Pair<int, Pair<byte, byte>>>
                    {
                        new Pair<int, Pair<byte, byte>>(0, new Pair<byte, byte>(4, 4)),
                        //new Pair<int, Pair<byte, byte>>(12, new Pair<byte, byte>(4, 4)),
                        //new Pair<int, Pair<byte, byte>>(84, new Pair<byte, byte>(4, 4)),
                    }
                };
                //Test.MIDIThing(@"D:\Development\GitHub\pokeemerald\sound\songs\midi\mus_kachi2.mid", @"D:\Development\GitHub\pokeemerald\sound\songs\midi\mus_rg_win_yasei.mid");
                SongPlayer.Instance.Song.SaveAsMIDI(d.FileName, args);
                FlexibleMessageBox.Show(string.Format(Strings.SuccessSaveMIDI, d.FileName), Text);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, Strings.ErrorSaveMIDI);
            }
        }

        void UpdateMenuInfo()
        {
            AGame game = ROM.Instance.Game;
            codeLabel.Text = game.Code;
            gameLabel.Text = game.Name;
            creatorLabel.Text = game.Creator;

            tableNumerical.Maximum = game.SongTables.Length - 1;
            tableNumerical.Value = 0;
            tableNumerical.Visible = game.SongTables.Length > 1;
            SetSongMaximum();
            PopulatePlaylists(game.Playlists);

            openMIDIToolStripMenuItem.Enabled =
                teToolStripMenuItem.Enabled = vteToolStripMenuItem.Enabled = eSf2ToolStripMenuItem.Enabled = eASMToolStripMenuItem.Enabled = eMIDIToolStripMenuItem.Enabled =
                songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = true;

            openASMToolStripMenuItem.Enabled = ROM.Instance.Game.Engine.Type == EngineType.M4A;

            UpdateTaskbarButtons();

            assemblerDialog?.Close(); midiConverterDialog?.Close();
        }
        void UpdateTrackInfo(bool play)
        {
            trackInfo.DeleteData(); // Refresh track count
            UpdateSongPosition(0);
            UpdateTaskbarState();
            positionBar.Maximum = SongPlayer.Instance.Song.NumTicks;
            positionBar.LargeChange = positionBar.Maximum / 10;
            positionBar.SmallChange = positionBar.LargeChange / 4;
            trackEditor?.UpdateTracks();
            voiceTableEditor?.UpdateTable();
            if (play)
                Play();
            else
                pauseButton.Text = Strings.PlayerPause;
            teToolStripMenuItem.Enabled = true;
        }

        void Play()
        {
            SongPlayer.Instance.Play();
            positionBar.Enabled = pauseButton.Enabled = stopButton.Enabled = true;
            pauseButton.Text = Strings.PlayerPause;
            timer.Interval = (int)(1000f / Config.Instance.RefreshRate);
            timer.Start();
            UpdateTaskbarState();
            UpdateTaskbarButtons();
        }
        void Pause()
        {
            SongPlayer.Instance.Pause(); // Change state
            if (SongPlayer.Instance.State != PlayerState.Paused)
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
                System.Threading.Monitor.Enter(timerLock);
                ClearPianoNotes();
            }
            UpdateTaskbarState();
            UpdateTaskbarButtons();
        }
        void Stop()
        {
            SongPlayer.Instance.Stop();
            positionBar.Enabled = pauseButton.Enabled = stopButton.Enabled = false;
            timer.Stop();
            System.Threading.Monitor.Enter(timerLock);
            ClearPianoNotes();
            trackInfo.DeleteData();
            UpdateSongPosition(0);
            UpdateTaskbarState();
            UpdateTaskbarButtons();
        }
        void TogglePlayback()
        {
            if (SongPlayer.Instance.State == PlayerState.Stopped)
                Play();
            else if (SongPlayer.Instance.State == PlayerState.Paused || SongPlayer.Instance.State == PlayerState.Playing)
                Pause();
        }
        void PlayPreviousSong()
        {
            int prevSong = -1;
            if (SongPlayer.Instance.PlaylistPlaying)
            {
                if (playedSongs.Count > 0)
                {
                    prevSong = playedSongs.Last();
                    playedSongs.Remove(prevSong);
                    remainingSongs.Insert(0, curSong);
                }
            }
            else if (songNumerical.Value > 0)
            {
                prevSong = (int)songNumerical.Value - 1;
            }

            if (prevSong > -1)
            {
                SetAndLoadSong(prevSong);
                Play();
            }
        }
        void PlayNextSong()
        {
            int nextSong = -1;
            if (SongPlayer.Instance.PlaylistPlaying)
            {
                // If the playlist has played all songs or is just starting, queue all songs
                if (remainingSongs.Count == 0)
                {
                    remainingSongs = curPlaylist.Songs.Select(s => s.Index).ToList();
                    if (Config.Instance.PlaylistMode == PlaylistMode.Random)
                        remainingSongs.Shuffle();
                }

                nextSong = remainingSongs[0];
                playedSongs.Add(curSong);
                remainingSongs.RemoveAt(0);
            }
            else if (songNumerical.Value < songNumerical.Maximum)
            {
                nextSong = (int)songNumerical.Value + 1;
            }

            if (nextSong > -1)
            {
                SetAndLoadSong(nextSong);
                Play();
            }
        }

        void ClearPianoNotes()
        {
            foreach (byte n in pianoNotes)
                if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                {
                    piano[n - piano.LowNoteID].NoteOnColor = Color.DeepSkyBlue;
                    piano.ReleasePianoKey(n);
                }
            pianoNotes.Clear();
        }
        void UpdateUI(object sender, EventArgs e)
        {
            if (!System.Threading.Monitor.TryEnter(timerLock)) return;
            try
            {
                // Song ended in SongPlayer
                if (stopUI)
                {
                    stopUI = false;
                    // Stop if no playlist is playing
                    if (!SongPlayer.Instance.PlaylistPlaying)
                        Stop();
                    else
                        PlayNextSong();
                }
                // Draw
                else
                {
                    // Draw piano notes
                    ClearPianoNotes();
                    var info = trackInfo.Info;
                    SongPlayer.Instance.GetSongState(info);
                    for (int i = SongPlayer.Instance.NumTracks - 1; i >= 0; i--)
                    {
                        if (!PianoTracks[i])
                            continue;

                        var notes = info.Notes[i];
                        pianoNotes.AddRange(notes);
                        foreach (var n in notes)
                        {
                            if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                            {
                                piano[n - piano.LowNoteID].NoteOnColor = Config.Instance.GetColor(info.Voices[i], ROM.Instance.Game.Remap, true);
                                piano.PressPianoKey(n);
                            }
                        }
                    }
                    // If the user is not dragging the position bar
                    if (!drag)
                        UpdateSongPosition(info.Position);
                    // Draw trackinfo
                    trackInfo.Invalidate();
                }
            }
            finally
            {
                System.Threading.Monitor.Exit(timerLock);
            }
        }
        void UpdateSongPosition(int position)
        {
            positionBar.Value = position.Clamp(0, positionBar.Maximum);
            if (Config.Instance.TaskbarProgress && TaskbarManager.IsPlatformSupported)
                TaskbarManager.Instance.SetProgressValue(positionBar.Value, positionBar.Maximum, Handle);
        }
        void UpdateTaskbarState()
        {
            if (Config.Instance.TaskbarProgress && TaskbarManager.IsPlatformSupported)
            {
                TaskbarProgressBarState state = TaskbarProgressBarState.NoProgress;
                if (SongPlayer.Instance.State == PlayerState.Playing) state = TaskbarProgressBarState.Normal;
                else if (SongPlayer.Instance.State == PlayerState.Paused) state = TaskbarProgressBarState.Paused;

                TaskbarManager.Instance.SetProgressState(state, Handle);
            }
        }
        void UpdateTaskbarButtons()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                if (SongPlayer.Instance.PlaylistPlaying)
                {
                    prevTButton.Enabled = playedSongs.Count > 0;
                    nextTButton.Enabled = true;
                }
                else
                {
                    prevTButton.Enabled = curSong > 0;
                    nextTButton.Enabled = curSong < songNumerical.Maximum;
                }

                switch (SongPlayer.Instance.State)
                {
                    case PlayerState.Stopped: toggleTButton.Icon = Resources.IconPlay; toggleTButton.Tooltip = Strings.PlayerPlay; break;
                    case PlayerState.Playing: toggleTButton.Icon = Resources.IconPause; toggleTButton.Tooltip = Strings.PlayerPause; break;
                    case PlayerState.Paused: toggleTButton.Icon = Resources.IconPlay; toggleTButton.Tooltip = Strings.PlayerUnpause; break;
                }
                toggleTButton.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Stop();
            SongPlayer.Instance.ShutDown();
            MIDIKeyboard.Instance.Stop();
            base.OnFormClosing(e);
        }
        void OnResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;

            // Position bar & song combobox
            int sWidth = (int)(splitContainer.Width / sfWidth);
            int sX = splitContainer.Width - sWidth - 4;
            songsComboBox.Location = new Point(sX, 4);
            songsComboBox.Size = new Size(sWidth, 23);
            positionBar.Location = new Point(sX, 45);
            positionBar.Size = new Size(sWidth, 27);

            splitContainer.SplitterDistance = (int)((Height - 38) / spfHeight) - 24 - 1;

            // Piano
            piano.Size = new Size(splitContainer.Width, (int)(splitContainer.Panel1.Height / 2.5f)); // Force it to initialize piano keys again
            int targetWhites = piano.Width / 10; // Minimum width of a white key is 10 pixels
            int targetAmount = (targetWhites / 7 * 12).Clamp(1, 128); // 7 white keys per octave
            int offset = targetAmount / 2 - ((targetWhites / 7) % 2);
            piano.LowNoteID = Math.Max(0, 60 - offset);
            piano.HighNoteID = (60 + offset - 1) >= 120 ? 127 : (60 + offset - 1);

            int wWidth = piano[0].Width; // White key width
            int dif = splitContainer.Width - wWidth * piano.WhiteKeyCount;
            piano.Location = new Point(dif / 2, splitContainer.Panel1.Height - piano.Height - 1);
            piano.Invalidate(true);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (playButton.Enabled && !songsComboBox.Focused && keyData == Keys.Space)
            {
                if (SongPlayer.Instance.State == PlayerState.Stopped)
                    Play();
                else
                    Pause();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
