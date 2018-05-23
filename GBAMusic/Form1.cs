using GBAMusic.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GBAMusic
{
    public partial class Form1 : Form
    {
        byte refreshRate = 60;
        MusicPlayer player;

        Label[] positionLabels = new Label[16],
            volumeLabels = new Label[16],
            delayLabels = new Label[16],
            noteLabels = new Label[16];

        bool stopUI = false;

        public Form1()
        {
            InitializeComponent();
            // Set song numerical max from config?
            for (int i = 0; i < 16; i++)
            {
                int y = i * 24;
                positionLabels[i] = new Label()
                {
                    Location = new Point(0, y),
                    AutoSize = true,
                    Text = "0x0",
                };
                panel1.Controls.Add(positionLabels[i]);
                volumeLabels[i] = new Label()
                {
                    Location = new Point(65, y),
                    AutoSize = true,
                    Text = "0",
                };
                panel1.Controls.Add(volumeLabels[i]);
                delayLabels[i] = new Label()
                {
                    Location = new Point(85, y),
                    AutoSize = true,
                    Text = "0",
                };
                panel1.Controls.Add(delayLabels[i]);
                noteLabels[i] = new Label()
                {
                    Location = new Point(105, y),
                    AutoSize = true,
                    Text = "0",
                };
                panel1.Controls.Add(noteLabels[i]);
            }
            timer1.Tick += Timer1_Tick;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (stopUI)
            {
                Stop(null, null);
                return;
            }
            var (tempo, positions, volumes, delays, notes) = player.GetTrackStates();
            tempoLabel.Text = string.Format("Tempo - {0}", tempo);
            for (int i = 0; i < positions.Length; i++)
            {
                positionLabels[i].Text = string.Format("0x{0:X}", positions[i]);
                volumeLabels[i].Text = volumes[i].ToString();
                delayLabels[i].Text = delays[i].ToString();
                noteLabels[i].Text = notes[i].ToString();
            }
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
            player = new MusicPlayer();
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
