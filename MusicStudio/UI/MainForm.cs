using Kermalis.MusicStudio.Core;
using Kermalis.MusicStudio.Core.NDS.SDAT;
using Kermalis.MusicStudio.Properties;
using Kermalis.MusicStudio.Util;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.MusicStudio.UI
{
    [DesignerCategory("")]
    class MainForm : ThemedForm
    {
        public static MainForm Instance { get; } = new MainForm();

        bool stopUI = false;
        readonly List<byte> pianoNotes = new List<byte>();
        public readonly bool[] PianoTracks = new bool[0x10];

        const int iWidth = 528, iHeight = 800 + 25; // +25 for menustrip (24) and splitcontainer separator (1)
        const float sfWidth = 2.35f; // Song combobox and volumebar width
        const float spfHeight = 5.5f; // Split panel 1 height

        #region Controls

        IContainer components;
        MenuStrip mainMenu;
        ToolStripMenuItem fileToolStripMenuItem, openDSEToolStripMenuItem, openSDATToolStripMenuItem, configToolStripMenuItem;
        Timer timer;
        ThemedNumeric songNumerical;
        ThemedButton playButton, pauseButton, stopButton;
        SplitContainer splitContainer;
        PianoControl piano;
        ColorSlider volumeBar;
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
        private MainForm()
        {
            for (int i = 0; i < PianoTracks.Length; i++)
            {
                PianoTracks[i] = true;
            }

            components = new Container();

            // Main Menu
            openDSEToolStripMenuItem = new ToolStripMenuItem { Text = "Open DSE", ShortcutKeys = Keys.Control | Keys.D };
            openDSEToolStripMenuItem.Click += OpenDSE;

            openSDATToolStripMenuItem = new ToolStripMenuItem { Text = "Open SDAT", ShortcutKeys = Keys.Control | Keys.S };
            openSDATToolStripMenuItem.Click += OpenSDAT;

            configToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuRefreshConfig, ShortcutKeys = Keys.Control | Keys.R };
            configToolStripMenuItem.Click += ReloadConfig;

            fileToolStripMenuItem = new ToolStripMenuItem { Text = Strings.MenuFile };
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openDSEToolStripMenuItem, openSDATToolStripMenuItem, configToolStripMenuItem });


            mainMenu = new MenuStrip { Size = new Size(iWidth, 24) };
            mainMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });

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
            songNumerical = new ThemedNumeric { Enabled = false, Location = new Point(246, 4), Minimum = ushort.MinValue };

            songNumerical.Size = new Size(45, 23);
            songNumerical.ValueChanged += (o, e) => LoadSong();

            // Timer
            timer = new Timer(components);
            timer.Tick += UpdateUI;

            // Piano
            piano = new PianoControl { Anchor = AnchorStyles.Bottom, Location = new Point(0, 125 - 50 - 1), Size = new Size(iWidth, 50) };

            // Volume bar
            int sWidth = (int)(iWidth / sfWidth);
            int sX = iWidth - sWidth - 4;
            volumeBar = new ColorSlider
            {
                Enabled = false,
                LargeChange = 20,
                Location = new Point(83, 45),
                Maximum = 100,
                Size = new Size(155, 27),
                SmallChange = 5
            };
            volumeBar.ValueChanged += VolumeBar_ValueChanged;

            // Playlist box
            songsComboBox = new ImageComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false,
                Location = new Point(sX, 4),
                Size = new Size(sWidth, 23)
            };
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;

            // Track info
            trackInfo = new TrackInfoControl
            {
                Dock = DockStyle.Fill,
                Size = new Size(iWidth, 690)
            };

            // Split container
            splitContainer = new SplitContainer
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
            splitContainer.Panel1.Controls.AddRange(new Control[] { playButton, pauseButton, stopButton, songNumerical, songsComboBox, piano, volumeBar });
            splitContainer.Panel2.Controls.Add(trackInfo);

            // MainForm
            AutoScaleDimensions = new SizeF(6, 13);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(iWidth, iHeight);
            Controls.AddRange(new Control[] { splitContainer, mainMenu });
            MainMenuStrip = mainMenu;
            MinimumSize = new Size(8 + iWidth + 8, 30 + iHeight + 8); // Borders
            Resize += OnResize;
            Text = Utils.ProgramName;

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

        void VolumeBar_ValueChanged(object sender, EventArgs e)
        {
            Engine.Instance.Mixer.SetVolume(volumeBar.Value / (float)volumeBar.Maximum);
        }
        public void SetVolumeBarValue(float volume)
        {
            volumeBar.ValueChanged -= VolumeBar_ValueChanged;
            volumeBar.Value = (int)(volume * volumeBar.Maximum);
            volumeBar.ValueChanged += VolumeBar_ValueChanged;
        }

        void LoadSong()
        {
            songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;

            var index = (int)songNumerical.Value;
            bool playing = Engine.Instance.Player.State == PlayerState.Playing; // Play new song if one is already playing
            bool paused = Engine.Instance.Player.State == PlayerState.Paused;
            Stop();
            try
            {
                if (!paused)
                {
                    Engine.Instance.Player.Pause();
                }
                string label = Engine.Instance.Player.LoadSong(index);
                if (label == null)
                {
                    Text = Utils.ProgramName;
                    songsComboBox.SelectedIndex = -1;
                }
                else
                {
                    Text = $"{Utils.ProgramName} - {label}";
                    songsComboBox.SelectedIndex = songsComboBox.Items.IndexOf(songsComboBox.Items.Cast<ImageComboBoxItem>().Single(i => (string)i.Item == label));
                }

                if (!paused)
                {
                    Engine.Instance.Player.Stop();
                }
                trackInfo.DeleteData();
                if (playing)
                {
                    Play();
                }
                else
                {
                    pauseButton.Text = Strings.PlayerPause;
                }
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, string.Format(Strings.ErrorLoadSong, songNumerical.Value));
            }

            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
        }
        void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Fix
            /*var item = (string)songsComboBox.SelectedItem;
            if (sdat.SYMBBlock == null)
            {
                songNumerical.Value = int.Parse(item);
            }
            else
            {
                songNumerical.Value = Array.IndexOf(sdat.SYMBBlock.SequenceSymbols.Entries, item);
            }*/
        }
        // Allow MainForm's thread to do the next work in UpdateUI()
        void SongEnded()
        {
            stopUI = true;
        }

        void OpenDSE(object sender, EventArgs e)
        {
            if (Engine.Instance != null)
            {
                Stop();
                Engine.Instance.ShutDown();
            }

            try
            {
                new Engine(Engine.EngineType.NDS_DSE, @"D:\Emulation\NDS\Games\SDATs\PMD2\SOUND\BGM"); // TODO
                Engine.Instance.Player.SongEnded += SongEnded;
                songsComboBox.Items.Clear();
                const int numSequences = 202; // TODO
                songsComboBox.Items.AddRange(Enumerable.Range(0, numSequences).Select(i => new ImageComboBoxItem(i.ToString(), Resources.IconSong, 0)).ToArray()); // TODO

                songsComboBox.SelectedIndex = 0;
                SongsComboBox_SelectedIndexChanged(null, null); // Why doesn't it work on its own??
                LoadSong();
                songNumerical.Maximum = numSequences - 1;
                songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = volumeBar.Enabled = true;
                UpdateTaskbarButtons();
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, "Error Loading DSE");
            }
        }
        void OpenSDAT(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = "Open SDAT", Filter = "SDAT Files|*.sdat" };
            if (d.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (Engine.Instance != null)
            {
                Stop();
                Engine.Instance.ShutDown();
            }

            try
            {
                var sdat = new SDAT(File.ReadAllBytes(d.FileName));
                new Engine(Engine.EngineType.NDS_SDAT, sdat);
                Engine.Instance.Player.SongEnded += SongEnded;
                songsComboBox.Items.Clear();
                songsComboBox.Items.AddRange(Enumerable.Range(0, sdat.INFOBlock.SequenceInfos.NumEntries).Where(i => sdat.INFOBlock.SequenceInfos.Entries[i] != null).Select(i => new ImageComboBoxItem(sdat.GetLabelForSong(i), Resources.IconSong, 0)).ToArray());

                songsComboBox.SelectedIndex = 0;
                SongsComboBox_SelectedIndexChanged(null, null); // Why doesn't it work on its own??
                LoadSong();
                songNumerical.Maximum = sdat.INFOBlock.SequenceInfos.NumEntries - 1;
                songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = volumeBar.Enabled = true;
                UpdateTaskbarButtons();
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, "Error Loading SDAT");
            }
        }
        void ReloadConfig(object sender, EventArgs e)
        {
            Config.Instance.Load();
        }

        void Play()
        {
            Engine.Instance.Player.Play();
            pauseButton.Enabled = stopButton.Enabled = true;
            pauseButton.Text = Strings.PlayerPause;
            timer.Interval = (int)(1000.0 / Config.Instance.RefreshRate);
            timer.Start();
            UpdateTaskbarButtons();
        }
        void Pause()
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
            UpdateTaskbarButtons();
        }
        void Stop()
        {
            Engine.Instance.Player.Stop();
            pauseButton.Enabled = stopButton.Enabled = false;
            timer.Stop();
            System.Threading.Monitor.Enter(timer);
            ClearPianoNotes();
            trackInfo.DeleteData();
            UpdateTaskbarButtons();
        }
        void TogglePlayback()
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
        void PlayPreviousSong()
        {
            if (songNumerical.Value > 0)
            {
                songNumerical.Value--;
                Play();
            }
        }
        void PlayNextSong()
        {
            if (songNumerical.Value < songNumerical.Maximum)
            {
                songNumerical.Value++;
                Play();
            }
        }

        void ClearPianoNotes()
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
        void UpdateUI(object sender, EventArgs e)
        {
            if (!System.Threading.Monitor.TryEnter(timer))
            {
                return;
            }
            try
            {
                // Song ended
                if (stopUI)
                {
                    stopUI = false;
                    Stop();
                }
                // Draw
                else
                {
                    // Draw piano notes
                    ClearPianoNotes();
                    TrackInfo info = trackInfo.Info;
                    Engine.Instance.Player.GetSongState(info);
                    for (int i = PianoTracks.Length - 1; i >= 0; i--)
                    {
                        if (!PianoTracks[i])
                        {
                            continue;
                        }

                        byte[] notes = info.Notes[i];
                        pianoNotes.AddRange(notes);
                        foreach (byte n in notes)
                        {
                            if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                            {
                                piano[n - piano.LowNoteID].NoteOnColor = Config.Instance.Colors[info.Voices[i]];
                                piano.PressPianoKey(n);
                            }
                        }
                    }
                    // Draw trackinfo
                    trackInfo.Invalidate();
                }
            }
            finally
            {
                System.Threading.Monitor.Exit(timer);
            }
        }

        void UpdateTaskbarButtons()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                prevTButton.Enabled = songNumerical.Value > 0;
                nextTButton.Enabled = songNumerical.Value < songNumerical.Maximum;
                switch (Engine.Instance.Player.State)
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
            if (Engine.Instance != null)
            {
                Stop();
                Engine.Instance.ShutDown();
            }
            base.OnFormClosing(e);
        }
        void OnResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                return;
            }

            // Song combobox
            int sWidth = (int)(splitContainer.Width / sfWidth);
            int sX = splitContainer.Width - sWidth - 4;
            songsComboBox.Location = new Point(sX, 4);
            songsComboBox.Size = new Size(sWidth, 23);

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
                if (Engine.Instance.Player.State == PlayerState.Stopped)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
