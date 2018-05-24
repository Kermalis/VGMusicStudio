using GBAMusic.Core;
using System;
using System.Windows.Forms;

namespace GBAMusic.UI
{
    public partial class MainForm : Form
    {
        byte refreshRate = 60;
        MusicPlayer player;

        bool stopUI = false;

        public MainForm()
        {
            InitializeComponent();
            timer1.Tick += Timer1_Tick;
            codeLabel.Text = gameLabel.Text = creatorLabel.Text = "";
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (stopUI)
            {
                Stop(null, null);
                return;
            }
            trackInfoControl1.Invalidate();
            var (tempo, _, _, _, _, _, _, _, _, _) = player.GetTrackStates();
            tempoLabel.Text = string.Format("Tempo - {0}", tempo);
        }

        private void OpenROM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog
            {
                Title = "Open GBA ROM",
                Filter = "GBA files|*.gba",
            };
            if (d.ShowDialog() != DialogResult.OK) return;

            ROM.LoadROM(d.FileName);
            // Set song numerical num
            codeLabel.Text = ROM.Instance.GameCode;
            gameLabel.Text = ROM.Instance.Config.GameName;
            creatorLabel.Text = ROM.Instance.Config.CreatorName;

            trackInfoControl1.SetPlayer(player = new MusicPlayer());
            player.SongEnded += () => stopUI = true;
            playButton.Enabled = true;
        }

        private void Play(object sender, EventArgs e)
        {
            timer1.Interval = (int)(1000f / refreshRate);
            timer1.Start();
            player.Play((ushort)songNumerical.Value);
            pauseButton.Enabled = stopButton.Enabled = true;
        }
        private void Pause(object sender, EventArgs e)
        {
            stopButton.Enabled = player.State != State.Playing;
            player.Pause();
        }
        private void Stop(object sender, EventArgs e)
        {
            stopUI = pauseButton.Enabled = stopButton.Enabled = false;
            player.Stop();
        }
    }
}
