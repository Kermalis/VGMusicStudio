using GBAMusicStudio.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    public partial class MainForm : Form
    {
        internal static readonly byte RefreshRate = 60;

        bool stopUI = false;

        public MainForm()
        {
            InitializeComponent();
            FormClosing += MainForm_FormClosing;
            timer1.Tick += UpdateUI;
            MusicPlayer.Instance.SongEnded += () => stopUI = true;
            songNumerical.ValueChanged += SongNumerical_ValueChanged;
            songsComboBox.SelectedIndexChanged += SongsComboBox_SelectedIndexChanged;
            codeLabel.Text = gameLabel.Text = creatorLabel.Text = "";
        }

        private void SongNumerical_ValueChanged(object sender, EventArgs e)
        {
            if (MusicPlayer.Instance.State == State.Playing) // Play new song if one is already playing
                Play(null, null);
            Playlist mainPlaylist = ROM.Instance.Config.Playlists[0];
            List<Song> songs = mainPlaylist.Songs.ToList();
            Song song = songs.SingleOrDefault(s => s.Index == songNumerical.Value);
            if (song != null)
            {
                Text = "GBA Music Studio - " + song.Name;
                songsComboBox.SelectedIndex = songs.IndexOf(song) + 1; // + 1 for the Playlist index
            }
        }
        private void SongsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Song song = (songsComboBox.SelectedItem as ImageComboBox.ImageComboBoxItem).Item as Song;
            if (song == null) return; // A playlist was selected
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
            trackInfoControl1.ReceiveData();
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

            PopulatePlaylists(ROM.Instance.Config.Playlists);
            codeLabel.Text = ROM.Instance.GameCode;
            gameLabel.Text = ROM.Instance.Config.GameName;
            creatorLabel.Text = ROM.Instance.Config.CreatorName;
            songsComboBox.Enabled = songNumerical.Enabled = playButton.Enabled = true;
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
            MusicPlayer.Instance.Play((ushort)songNumerical.Value);
            timer1.Interval = (int)(1000f / RefreshRate);
            timer1.Start();
        }
        void Pause(object sender, EventArgs e)
        {
            stopButton.Enabled = MusicPlayer.Instance.State != State.Playing;
            MusicPlayer.Instance.Pause();
        }
        void Stop(object sender, EventArgs e)
        {
            stopUI = pauseButton.Enabled = stopButton.Enabled = false;
            timer1.Stop();
            trackInfoControl1.DeleteData();
            MusicPlayer.Instance.Stop();
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e) => Stop(null, null);
    }
}
