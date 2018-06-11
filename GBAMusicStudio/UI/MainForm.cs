using GBAMusicStudio.Core;
using GBAMusicStudio.MIDI;
using GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [DesignerCategory("")]
    public class MainForm : Form
    {
        bool stopUI = false;
        List<byte> pianoNotes = new List<byte>();
        public readonly bool[] PianoTracks = new bool[16];

        readonly int iWidth = 528, iHeight = 800 + 25; // +25 for menustrip (24) and splitcontainer separator (1)
        readonly float sfWidth = 2.35f; // Song combobox and volumebar width
        readonly float spfHeight = 5.5f; // Split panel 1 height

        IContainer components;
        MenuStrip mainMenu;
        ToolStripMenuItem fileToolStripMenuItem, openToolStripMenuItem, configToolStripMenuItem,
            dataToolStripMenuItem, eSf2ToolStripMenuItem;
        Timer timer;
        readonly object timerLock = new object();
        NumericUpDown songNumerical, tableNumerical;
        Button playButton, stopButton, pauseButton;
        Label creatorLabel, gameLabel, codeLabel;
        SplitContainer splitContainer;
        PianoControl piano;
        TrackBar volumeBar;
        TrackInfoControl trackInfo;
        ImageComboBox.ImageComboBox songsComboBox;

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
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));

            // Main Menu
            openToolStripMenuItem = new ToolStripMenuItem() { Text = "Open", ShortcutKeys = Keys.Control | Keys.O };
            openToolStripMenuItem.Click += OpenROM;

            configToolStripMenuItem = new ToolStripMenuItem() { Text = "Refresh Config", ShortcutKeys = Keys.Control | Keys.R };
            configToolStripMenuItem.Click += ReloadConfig;

            fileToolStripMenuItem = new ToolStripMenuItem() { Text = "File" };
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, configToolStripMenuItem });


            eSf2ToolStripMenuItem = new ToolStripMenuItem() { Text = "Export to SF2", Enabled = false };
            eSf2ToolStripMenuItem.Click += ExportSF2;

            dataToolStripMenuItem = new ToolStripMenuItem() { Text = "Data" };
            dataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { eSf2ToolStripMenuItem });

            mainMenu = new MenuStrip() { Size = new Size(iWidth, 24) };
            mainMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, dataToolStripMenuItem });

            // Buttons
            playButton = new Button() { ForeColor = Color.DarkGreen, Location = new Point(3, 3), Text = "Play" };
            playButton.Click += Play;
            pauseButton = new Button() { ForeColor = Color.DarkSlateBlue, Location = new Point(84, 3), Text = "Pause" };
            pauseButton.Click += Pause;
            stopButton = new Button() { ForeColor = Color.MediumVioletRed, Location = new Point(164, 3), Text = "Stop" };
            stopButton.Click += Stop;

            playButton.Enabled = pauseButton.Enabled = stopButton.Enabled = false;
            playButton.Size = pauseButton.Size = stopButton.Size = new Size(75, 23);
            playButton.UseVisualStyleBackColor = pauseButton.UseVisualStyleBackColor = stopButton.UseVisualStyleBackColor = true;

            // Numericals
            songNumerical = new NumericUpDown() { Enabled = false, Location = new Point(246, 4), Maximum = 1000 };
            tableNumerical = new NumericUpDown() { Location = new Point(246, 35), Maximum = 0, Visible = false };

            songNumerical.Size = tableNumerical.Size = new Size(45, 23);
            songNumerical.TextAlign = tableNumerical.TextAlign = HorizontalAlignment.Center;
            songNumerical.ValueChanged += LoadSong;
            tableNumerical.ValueChanged += LoadSong;

            // Labels
            creatorLabel = new Label() { Location = new Point(3, 42), Size = new Size(72, 13) };
            gameLabel = new Label() { Location = new Point(3, 29), Size = new Size(66, 13) };
            codeLabel = new Label() { Location = new Point(3, 55), Size = new Size(63, 13) };

            creatorLabel.AutoSize = gameLabel.AutoSize = codeLabel.AutoSize = true;
            creatorLabel.TextAlign = gameLabel.TextAlign = codeLabel.TextAlign = ContentAlignment.MiddleCenter;

            // Timer
            timer = new Timer(components);
            timer.Tick += UpdateUI;

            // Piano
            piano = new PianoControl() { Anchor = AnchorStyles.Bottom, Location = new Point(0, 125 - 50 - 1), Size = new Size(iWidth, 50) };

            // Volume bar
            int sWidth = (int)(iWidth / sfWidth);
            int sX = iWidth - sWidth - 4;
            volumeBar = new TrackBar()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                LargeChange = 20,
                Location = new Point(sX, 35),
                Maximum = 100,
                Size = new Size(sWidth, 27),
                SmallChange = 5,
                TickFrequency = 10
            };
            volumeBar.ValueChanged += (o, e) => SongPlayer.SetVolume(volumeBar.Value / (float)volumeBar.Maximum);
            volumeBar.Value = Config.Volume; // Update MusicPlayer volume

            // Playlist box
            ImageList il = new ImageList(components)
            {
                ColorDepth = ColorDepth.Depth16Bit,
                ImageSize = new Size(64, 64),
                TransparentColor = Color.Transparent
            };
            il.Images.AddRange(new Image[] { (Image)(resources.GetObject("PlaylistIcon")), (Image)(resources.GetObject("SongIcon")) });
            songsComboBox = new ImageComboBox.ImageComboBox()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false,
                ImageList = il,
                Indent = 15,
                Location = new Point(sX, 4),
                Size = new Size(sWidth, 23)
            };
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;

            // Track info
            trackInfo = new TrackInfoControl()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft Tai Le", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Size = new Size(iWidth, 690)
            };

            // Split container
            splitContainer = new SplitContainer()
            {
                BackColor = Color.FromArgb(85, 50, 125),
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
                Orientation = Orientation.Horizontal,
                Size = new Size(iWidth, iHeight),
                SplitterDistance = 125,
                SplitterWidth = 1
            };
            splitContainer.Panel1.Controls.AddRange(new Control[] { playButton, creatorLabel, gameLabel, codeLabel, pauseButton, stopButton, songNumerical, tableNumerical, songsComboBox, piano, volumeBar });
            splitContainer.Panel2.Controls.Add(trackInfo);

            // MainForm
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(iWidth, iHeight);
            Controls.AddRange(new Control[] { splitContainer, mainMenu });
            Icon = (Icon)(resources.GetObject("Icon"));
            MainMenuStrip = mainMenu;
            MinimumSize = new Size(8 + iWidth + 8, 30 + iHeight + 8); // Borders
            SongPlayer.SongEnded += () => stopUI = true;
            Resize += OnResize;
            Text = "GBA Music Studio";
        }

        void LoadSong(object sender, EventArgs e)
        {
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
            bool playing = SongPlayer.State == State.Playing;
            Stop(null, null);
            SongPlayer.LoadROMSong((ushort)songNumerical.Value, (byte)tableNumerical.Value);
            trackInfo.DeleteData(); // Refresh track count
            if (playing) // Play new song if one is already playing
                Play(null, null);
        }
        void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!((songsComboBox.SelectedItem as ImageComboBox.ImageComboBoxItem).Item is ASong song)) return; // A playlist was selected
            songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;
            songNumerical.Value = song.Index;
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
        }

        void OpenROM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = "Open GBA ROM", Filter = "GBA files|*.gba" };
            if (d.ShowDialog() != DialogResult.OK) return;

            Stop(null, null);

            new ROM(d.FileName);
            UpdateInfo();
            LoadSong(null, null);
        }
        void ReloadConfig(object sender, EventArgs e)
        {
            Config.Load();
            if (ROM.Instance != null)
            {
                ROM.Instance.ReloadGameConfig();
                UpdateInfo();
            }
        }
        void ExportSF2(object sender, EventArgs e)
        {
            var d = new SaveFileDialog { Title = "Export SF2 File", Filter = "SF2 file|*.sf2" };
            if (d.ShowDialog() != DialogResult.OK) return;

            SongPlayer.ExportSF2(d.FileName);
            MessageBox.Show("SF2 saved.", Text);
        }

        void UpdateInfo()
        {
            AGame game = ROM.Instance.Game;
            PopulatePlaylists(game.Playlists);
            codeLabel.Text = game.Code;
            gameLabel.Text = game.Name;
            creatorLabel.Text = game.Creator;

            tableNumerical.Maximum = game.SongTables.Length - 1;
            tableNumerical.Value = 0;
            tableNumerical.Visible = game.SongTables.Length > 1;

            eSf2ToolStripMenuItem.Enabled = songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = true;
        }
        void PopulatePlaylists(List<APlaylist> playlists)
        {
            songsComboBox.ComboBoxClear();
            foreach (var playlist in playlists)
            {
                songsComboBox.ComboBoxAddItem(new ImageComboBox.ImageComboBoxItem(playlist) { ImageIndex = 0 });
                songsComboBox.Items.AddRange(playlist.Songs.Select(s => new ImageComboBox.ImageComboBoxItem(s) { ImageIndex = 1, IndentLevel = 1 }).ToArray());
            }
            songNumerical.Value = playlists[0].Songs[0].Index;
            songsComboBox.SelectedIndex = 0; // Select main playlist
        }

        void Play(object sender, EventArgs e)
        {
            pauseButton.Enabled = stopButton.Enabled = true;
            pauseButton.Text = "Pause";
            SongPlayer.Play();
            timer.Interval = (int)(1000f / Config.RefreshRate);
            timer.Start();
        }
        void Pause(object sender, EventArgs e)
        {
            stopButton.Enabled = SongPlayer.State != State.Playing;
            pauseButton.Text = SongPlayer.State != State.Playing ? "Pause" : "Unpause";
            SongPlayer.Pause();
        }
        void Stop(object sender, EventArgs e)
        {
            stopUI = pauseButton.Enabled = stopButton.Enabled = false;
            timer.Stop();
            System.Threading.Monitor.Enter(timerLock);
            foreach (byte n in pianoNotes)
                piano.ReleasePianoKey(n);
            trackInfo.DeleteData();
            SongPlayer.Stop();
        }

        void UpdateUI(object sender, EventArgs e)
        {
            if (!System.Threading.Monitor.TryEnter(timerLock)) return;
            try
            {
                if (stopUI)
                {
                    Stop(null, null);
                    return;
                }
                foreach (byte n in pianoNotes)
                    if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                    {
                        piano[n - piano.LowNoteID].NoteOnColor = Color.DeepSkyBlue;
                        piano.ReleasePianoKey(n);
                    }
                pianoNotes.Clear();
                var tup = SongPlayer.GetSongState();
                for (int i = SongPlayer.NumTracks - 1; i >= 0; i--)
                {
                    if (!PianoTracks[i]) continue;

                    var notes = tup.Item5[i];
                    pianoNotes.AddRange(notes);
                    foreach (var n in notes)
                        if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                        {
                            piano[n - piano.LowNoteID].NoteOnColor = Config.Colors[tup.Item7[i]];
                            piano.PressPianoKey(n);
                        }
                }
                trackInfo.ReceiveData(tup);
            }
            finally
            {
                System.Threading.Monitor.Exit(timerLock);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Stop(null, null);
            MIDIKeyboard.Stop();
            base.OnFormClosing(e);
        }
        void OnResize(object sender, EventArgs e)
        {
            // Volume bar & song combobox
            int sWidth = (int)(splitContainer.Width / sfWidth);
            int sX = splitContainer.Width - sWidth - 4;
            songsComboBox.Location = new Point(sX, 4);
            songsComboBox.Size = new Size(sWidth, 23);
            volumeBar.Location = new Point(sX, 35);
            volumeBar.Size = new Size(sWidth, 27);

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
    }
}
