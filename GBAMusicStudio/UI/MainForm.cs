using GBAMusicStudio.Core;
using GBAMusicStudio.MIDI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    public partial class MainForm : Form
    {
        bool stopUI = false;
        List<byte> pianoNotes = new List<byte>();
        public readonly bool[] PianoTracks = new bool[16];

        public MainForm()
        {
            InitializeComponent();
            timer.Tick += UpdateUI;
            MusicPlayer.SongEnded += () => stopUI = true;
            codeLabel.Text = gameLabel.Text = creatorLabel.Text = "";
            volumeBar.Value = Config.Volume;
        }

        void ChangeVolume(object sender, EventArgs e)
        {
            MusicPlayer.SetVolume(volumeBar.Value / 100f);
        }
        void SongNumerical_ValueChanged(object sender, EventArgs e)
        {
            Playlist mainPlaylist = ROM.Instance.Game.Playlists[0];
            List<Song> songs = mainPlaylist.Songs.ToList();
            Song song = songs.SingleOrDefault(s => s.Index == songNumerical.Value);
            if (song != null)
            {
                Text = "GBA Music Studio - " + song.Name;
                songsComboBox.SelectedIndex = songs.IndexOf(song) + 1; // + 1 for the Playlist index
            }
            bool playing = MusicPlayer.State == State.Playing;
            MusicPlayer.LoadSong((ushort)songNumerical.Value);
            trackInfoControl.DeleteData();
            trackInfoControl.Invalidate();
            if (playing) // Play new song if one is already playing
                Play(null, null);
        }
        void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!((songsComboBox.SelectedItem as ImageComboBox.ImageComboBoxItem).Item is Song song)) return; // A playlist was selected
            songsComboBox.SelectedIndexChanged -= SongsComboBox_SelectedIndexChanged;
            songNumerical.Value = song.Index;
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
        }

        void UpdateUI(object sender, EventArgs e)
        {
            if (stopUI)
            {
                Stop(null, null);
                return;
            }
            foreach (byte n in pianoNotes)
                if (n >= pianoControl.LowNoteID && n <= pianoControl.HighNoteID)
                    pianoControl.ReleasePianoKey(n);
            pianoNotes.Clear();
            var tup = MusicPlayer.GetSongState();
            for (int i = 0; i < 16; i++)
                if (PianoTracks[i])
                    pianoNotes.AddRange(tup.Item5[i]);
            foreach (byte n in pianoNotes)
                if (n >= pianoControl.LowNoteID && n <= pianoControl.HighNoteID)
                    pianoControl.PressPianoKey(n);
            trackInfoControl.ReceiveData(tup);
        }

        void OpenROM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog
            {
                Title = "Open GBA ROM",
                Filter = "GBA files|*.gba",
            };
            if (d.ShowDialog() != DialogResult.OK) return;

            Stop(null, null);

            new ROM(d.FileName);
            RefreshConfig();
            SongNumerical_ValueChanged(null, null);

            songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = true;
        }
        void ReloadConfig(object sender, EventArgs e)
        {
            Config.Load();
            if (ROM.Instance != null)
            {
                ROM.Instance.ReloadGameConfig();
                RefreshConfig();
            }
        }
        void RefreshConfig()
        {
            PopulatePlaylists(ROM.Instance.Game.Playlists);
            codeLabel.Text = ROM.Instance.Game.Code;
            gameLabel.Text = ROM.Instance.Game.Name;
            creatorLabel.Text = ROM.Instance.Game.Creator;
        }

        void PopulatePlaylists(List<Playlist> playlists)
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
            MusicPlayer.Play();
            timer.Interval = (int)(1000f / Config.RefreshRate);
            timer.Start();
        }
        void Pause(object sender, EventArgs e)
        {
            stopButton.Enabled = MusicPlayer.State != State.Playing;
            pauseButton.Text = MusicPlayer.State != State.Playing ? "Pause" : "Unpause";
            MusicPlayer.Pause();
        }
        void Stop(object sender, EventArgs e)
        {
            stopUI = pauseButton.Enabled = stopButton.Enabled = false;
            timer.Stop();
            foreach (byte n in pianoNotes)
                pianoControl.ReleasePianoKey(n);
            trackInfoControl.DeleteData();
            MusicPlayer.Stop();
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop(null, null);
            MIDIKeyboard.Stop();
        }
    }
}
