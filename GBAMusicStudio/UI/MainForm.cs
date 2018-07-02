using GBAMusicStudio.Core;
using GBAMusicStudio.MIDI;
using GBAMusicStudio.Properties;
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
    internal class MainForm : Form
    {
        bool stopUI = false, drag = false;
        TrackEditor trackEditor;
        List<sbyte> pianoNotes = new List<sbyte>();
        internal readonly bool[] PianoTracks = new bool[16];

        readonly int iWidth = 528, iHeight = 800 + 25; // +25 for menustrip (24) and splitcontainer separator (1)
        readonly float sfWidth = 2.35f; // Song combobox and volumebar width
        readonly float spfHeight = 5.5f; // Split panel 1 height

        IContainer components;
        MenuStrip mainMenu;
        ToolStripMenuItem fileToolStripMenuItem, openROMToolStripMenuItem, openMIDIToolStripMenuItem, openASMToolStripMenuItem, configToolStripMenuItem,
            dataToolStripMenuItem, teToolStripMenuItem, eSf2ToolStripMenuItem;
        Timer timer;
        readonly object timerLock = new object();
        ThemedNumeric songNumerical, tableNumerical;
        ThemedButton playButton, stopButton, pauseButton;
        ThemedLabel creatorLabel, gameLabel, codeLabel;
        SplitContainer splitContainer;
        PianoControl piano;
        ColorSlider positionBar, volumeBar;
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

            // Main Menu
            openROMToolStripMenuItem = new ToolStripMenuItem { Text = "Open ROM", ShortcutKeys = Keys.Control | Keys.O };
            openROMToolStripMenuItem.Click += OpenROM;

            openMIDIToolStripMenuItem = new ToolStripMenuItem { Text = "Open MIDI", Enabled = false, ShortcutKeys = Keys.Control | Keys.M };
            openMIDIToolStripMenuItem.Click += OpenMIDIConverter;

            openASMToolStripMenuItem = new ToolStripMenuItem { Text = "Open ASM", Enabled = false, ShortcutKeys = Keys.Control | Keys.Shift | Keys.M };
            openASMToolStripMenuItem.Click += OpenAssembler;

            configToolStripMenuItem = new ToolStripMenuItem { Text = "Refresh Config", ShortcutKeys = Keys.Control | Keys.R };
            configToolStripMenuItem.Click += ReloadConfig;

            fileToolStripMenuItem = new ToolStripMenuItem { Text = "File" };
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openROMToolStripMenuItem, openMIDIToolStripMenuItem, openASMToolStripMenuItem, configToolStripMenuItem });


            teToolStripMenuItem = new ToolStripMenuItem { Text = "Track Editor", Enabled = false, ShortcutKeys = Keys.Control | Keys.T };
            teToolStripMenuItem.Click += OpenTrackEditor;

            eSf2ToolStripMenuItem = new ToolStripMenuItem { Text = "Export Voicetable To SF2", Enabled = false };
            eSf2ToolStripMenuItem.Click += ExportSF2;


            dataToolStripMenuItem = new ToolStripMenuItem { Text = "Data" };
            dataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { teToolStripMenuItem, eSf2ToolStripMenuItem, saveASMToolStripMenuItem });


            mainMenu = new MenuStrip { Size = new Size(iWidth, 24) };
            mainMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, dataToolStripMenuItem });

            // Buttons
            playButton = new ThemedButton { ForeColor = Color.MediumSpringGreen, Location = new Point(5, 3), Text = "Play" };
            playButton.Click += Play;
            pauseButton = new ThemedButton { ForeColor = Color.DeepSkyBlue, Location = new Point(85, 3), Text = "Pause" };
            pauseButton.Click += Pause;
            stopButton = new ThemedButton { ForeColor = Color.MediumVioletRed, Location = new Point(166, 3), Text = "Stop" };
            stopButton.Click += Stop;

            playButton.Enabled = pauseButton.Enabled = stopButton.Enabled = false;
            playButton.Size = stopButton.Size = new Size(75, 23);
            pauseButton.Size = new Size(76, 23);

            // Numericals
            songNumerical = new ThemedNumeric { Enabled = false, Location = new Point(246, 4), Maximum = 1000 };
            tableNumerical = new ThemedNumeric { Location = new Point(246, 35), Maximum = 0, Visible = false };

            songNumerical.Size = tableNumerical.Size = new Size(45, 23);
            songNumerical.ValueChanged += LoadSong;
            tableNumerical.ValueChanged += LoadSong;

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
                Enabled = false,
                Location = new Point(sX, 45),
                Maximum = 0,
                Size = new Size(sWidth, 27)
            };
            positionBar.MouseUp += SetPosition;
            positionBar.MouseDown += (o, e) => drag = true;
            volumeBar = new ColorSlider()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                LargeChange = 20,
                Location = new Point(83, 45),
                Maximum = 100,
                Size = new Size(155, 27),
                SmallChange = 5
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
            il.Images.AddRange(new Image[] { Resources.PlaylistIcon, Resources.SongIcon });
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
            SongPlayer.SongEnded += () => stopUI = true;
            Resize += OnResize;
            Text = "GBA Music Studio";
        }

        void SetPosition(object sender, EventArgs e)
        {
            SongPlayer.SetPosition((uint)positionBar.Value);
            drag = false;
        }

        internal void PreviewASM(Assembler asm, string headerLabel, string caption)
        {
            Text = "GBA Music Studio - " + caption;
            bool playing = SongPlayer.State == State.Playing; // Play new song if one is already playing
            Stop(null, null);
            SongPlayer.LoadASMSong(asm, headerLabel);
            UpdateTrackInfo(playing);
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
            bool playing = SongPlayer.State == State.Playing; // Play new song if one is already playing
            Stop(null, null);
            SongPlayer.LoadROMSong((ushort)songNumerical.Value, (byte)tableNumerical.Value);
            UpdateTrackInfo(playing);
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
            UpdateMenuInfo();
            LoadSong(null, null);
        }
        void OpenMIDIConverter(object sender, EventArgs e)
        {
            new MIDIConverterDialog { Owner = this }.Show();
        }
        void OpenAssembler(object sender, EventArgs e)
        {
            new AssemblerDialog { Owner = this }.Show();
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
        void ReloadConfig(object sender, EventArgs e)
        {
            Config.Load();
            if (ROM.Instance != null)
            {
                ROM.Instance.ReloadGameConfig();
                UpdateMenuInfo();
            }
        }
        void ExportSF2(object sender, EventArgs e)
        {
            var d = new SaveFileDialog { Title = "Export SF2 File", Filter = "SF2 file|*.sf2" };
            if (d.ShowDialog() != DialogResult.OK) return;

            try
            {
                new VoiceTableSaver(SongPlayer.VoiceTable, d.FileName);
                FlexibleMessageBox.Show($"Voice table saved to {d.FileName}.", Text);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, "Error Exporting SF2 File");
            }
        }

        void UpdateMenuInfo()
        {
            AGame game = ROM.Instance.Game;
            PopulatePlaylists(game.Playlists);
            codeLabel.Text = game.Code;
            gameLabel.Text = game.Name;
            creatorLabel.Text = game.Creator;

            tableNumerical.Maximum = game.SongTables.Length - 1;
            tableNumerical.Value = 0;
            tableNumerical.Visible = game.SongTables.Length > 1;

            openMIDIToolStripMenuItem.Enabled = openASMToolStripMenuItem.Enabled =
                teToolStripMenuItem.Enabled = eSf2ToolStripMenuItem.Enabled =
                songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = true;
        }
        void UpdateTrackInfo(bool play)
        {
            trackInfo.DeleteData(); // Refresh track count
            positionBar.Maximum = SongPlayer.Song.NumTicks;
            positionBar.LargeChange = (uint)(positionBar.Maximum / 10);
            positionBar.SmallChange = positionBar.LargeChange / 4;
            if (trackEditor != null)
                trackEditor.UpdateTracks();
            if (play)
                Play(null, null);
            teToolStripMenuItem.Enabled = true;
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
            SongPlayer.Play();
            positionBar.Enabled = pauseButton.Enabled = stopButton.Enabled = true;
            pauseButton.Text = "Pause";
            timer.Interval = (int)(1000f / Config.RefreshRate);
            timer.Start();
        }
        void Pause(object sender, EventArgs e)
        {
            SongPlayer.Pause(); // Change state
            if (SongPlayer.State != State.Paused)
            {
                stopButton.Enabled = true;
                pauseButton.Text = "Pause";
                timer.Start();
            }
            else
            {
                stopButton.Enabled = false;
                pauseButton.Text = "Unpause";
                timer.Stop();
                System.Threading.Monitor.Enter(timerLock);
                ClearPianoNotes();
            }
        }
        void Stop(object sender, EventArgs e)
        {
            SongPlayer.Stop();
            positionBar.Enabled = pauseButton.Enabled = stopButton.Enabled = false;
            timer.Stop();
            System.Threading.Monitor.Enter(timerLock);
            ClearPianoNotes();
            trackInfo.DeleteData();
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
                if (stopUI)
                {
                    Stop(null, null);
                    stopUI = false;
                    return;
                }
                ClearPianoNotes();
                var tup = SongPlayer.GetSongState();
                for (int i = SongPlayer.NumTracks - 1; i >= 0; i--)
                {
                    if (!PianoTracks[i]) continue;

                    var notes = tup.Item6[i];
                    pianoNotes.AddRange(notes);
                    foreach (var n in notes)
                        if (n >= piano.LowNoteID && n <= piano.HighNoteID)
                        {
                            piano[n - piano.LowNoteID].NoteOnColor = Config.Colors[tup.Item8[i]];
                            piano.PressPianoKey(n);
                        }
                }
                if (!drag)
                    positionBar.Value = ((int)tup.Item2).Clamp(0, positionBar.Maximum);
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
    }
}
