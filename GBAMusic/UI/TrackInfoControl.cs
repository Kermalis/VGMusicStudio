using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GBAMusic.UI
{
    internal class TrackInfoControl : UserControl
    {
        IContainer components = null;
        Core.MusicPlayer player;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            DoubleBuffered = true;
            Name = "TrackInfoControl";
            Size = new Size(350, 36 * 16);
            Paint += TrackInfoControl_Paint;
            ResumeLayout(false);
        }

        internal TrackInfoControl() => InitializeComponent();
        internal void SetPlayer(Core.MusicPlayer p)
        {
            player = p;
            Invalidate();
        }

        private readonly string[] simplenotenumber =
        {
            "Cn", "Cs", "Dn", "Ds", "En", "Fn", "Fs", "Gn", "Gs", "An", "As", "Bn"
        };
        void TrackInfoControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
            if (player == null) return;

            var (_, positions, volumes, delays, notes, velocities, voices, modulations, bends) = player.GetTrackStates();
            for (int i = 0; i < positions.Length; i++)
            {
                int y1 = i * 36;
                int y2 = y1 + 12;
                e.Graphics.DrawString(string.Format("0x{0:X}", positions[i]), Font, Brushes.Lime, 0, y1);
                e.Graphics.DrawString(delays[i].ToString(), Font, Brushes.Crimson, 65, y1);
                e.Graphics.DrawString(string.Join(", ", notes[i].Select(n => string.Format("{0}{1}", simplenotenumber[n % 12], (n / 12) - 2))), Font, Brushes.Turquoise, 85, y1);

                e.Graphics.DrawString(voices[i].ToString(), Font, Brushes.OrangeRed, 15, y2);
                e.Graphics.DrawString(((int)(velocities[i] * 255)).ToString(), Font, Brushes.PeachPuff, 35, y2);
                e.Graphics.DrawString(volumes[i].ToString(), Font, Brushes.LightSeaGreen, 55, y2);
                e.Graphics.DrawString(modulations[i].ToString(), Font, Brushes.SkyBlue, 75, y2);
                e.Graphics.DrawString(bends[i].ToString(), Font, Brushes.Purple, 95, y2);

                var rect = new Rectangle(200, y2, (int)(100 * velocities[i]), 18);
                e.Graphics.FillRectangle(Brushes.Plum, rect);
                e.Graphics.DrawRectangle(Pens.Purple, rect);
            }
        }
    }
}
