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
            FormClosing += MainForm_FormClosing;
            timer1.Tick += Timer1_Tick;
            codeLabel.Text = gameLabel.Text = creatorLabel.Text = "";
        }

        void Timer1_Tick(object sender, EventArgs e)
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

        void OpenROM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog
            {
                Title = "Open GBA ROM",
                Filter = "GBA files|*.gba",
            };
            if (d.ShowDialog() != DialogResult.OK) return;

            Stop(null, null);
            ROM.LoadROM(d.FileName);
            // Set song numerical num
            codeLabel.Text = ROM.Instance.GameCode;
            gameLabel.Text = ROM.Instance.Config.GameName;
            creatorLabel.Text = ROM.Instance.Config.CreatorName;

            trackInfoControl1.SetPlayer(player = new MusicPlayer());
            player.SongEnded += () => stopUI = true;
            playButton.Enabled = true;
        }

        void Play(object sender, EventArgs e)
        {
            pauseButton.Enabled = stopButton.Enabled = true;
            player.Play((ushort)songNumerical.Value);
            timer1.Interval = (int)(1000f / refreshRate);
            timer1.Start();
        }
        void Pause(object sender, EventArgs e)
        {
            stopButton.Enabled = player.State != State.Playing;
            player.Pause();
        }
        void Stop(object sender, EventArgs e)
        {
            stopUI = pauseButton.Enabled = stopButton.Enabled = false;
            if (player != null) player.Stop();
            timer1.Stop();
            trackInfoControl1.Invalidate();
        }
        
        void MainForm_FormClosing(object sender, FormClosingEventArgs e) => Stop(null, null);
    }
}
