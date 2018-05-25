using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusic.UI
{
    internal class TrackInfoControl : UserControl
    {
        IContainer components = null;
        Color barColor = Color.FromArgb(0xa7, 0x44, 0xdd);
        Core.MusicPlayer player;

        readonly string[] simpleNotes = { "Cn", "Cs", "Dn", "Ds", "En", "Fn", "Fs", "Gn", "Gs", "An", "As", "Bn" };

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

        void TrackInfoControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);

            int my = 24;
            e.Graphics.DrawString("Position", Font, Brushes.Lime, 0, 5);
            e.Graphics.DrawString("Delay", Font, Brushes.Crimson, 50, 5);
            e.Graphics.DrawString("Notes", Font, Brushes.Turquoise, 85, 5);
            e.Graphics.DrawLine(Pens.Gold, 0, my, Width, my);

            if (player == null) return;
            var (_, positions, volumes, delays, notes, velocities, voices, modulations, bends, pans) = player.GetTrackStates();
            for (int i = 0; i < positions.Length; i++)
            {
                int y1 = my + 3 + i * 36;
                int y2 = y1 + 12;

                byte velocity = (byte)(velocities[i] * 0xFF);

                e.Graphics.DrawString(string.Format("0x{0:X}", positions[i]), Font, Brushes.Lime, 0, y1);
                e.Graphics.DrawString(delays[i].ToString(), Font, Brushes.Crimson, 65, y1);

                e.Graphics.DrawString(voices[i].ToString(), Font, Brushes.OrangeRed, 15, y2);
                e.Graphics.DrawString(velocity.ToString(), Font, Brushes.PeachPuff, 40, y2);
                e.Graphics.DrawString(volumes[i].ToString(), Font, Brushes.LightSeaGreen, 65, y2);
                e.Graphics.DrawString(modulations[i].ToString(), Font, Brushes.SkyBlue, 90, y2);
                e.Graphics.DrawString(bends[i].ToString(), Font, Brushes.Purple, 115, y2);

                int w = 127, bx = 160;
                e.Graphics.DrawLine(Pens.Gold, bx, y2 + 3, bx, y2 + 10);
                e.Graphics.DrawLine(Pens.Gold, bx + w - 1, y2 + 3, bx + w - 1, y2 + 10);

                float px = bx + (w / 2) + (w / 2 * pans[i]);
                float cx = bx + (w / 2);
                int ly = y1 + 3, lh = 19;
                e.Graphics.DrawLine(Pens.BlueViolet, cx, ly, cx, ly + lh); // Center line

                e.Graphics.DrawLine(Pens.OrangeRed, px, ly, px, ly + lh); // Pan line

                float percentRight = (pans[i] + 1) * velocities[i],
                    percentLeft = (-pans[i] + 1) * velocities[i];

                var rect = new Rectangle((int)(bx + (w / 2) - (percentLeft * w / 2)) + 1,
                    ly,
                    (int)((percentLeft + percentRight) * w / 2),
                    lh);
                e.Graphics.FillRectangle(
                    new LinearGradientBrush(new Point(bx, 10), new Point(bx + w, 10), Color.FromArgb(velocity, barColor), Color.Purple),
                    rect);
                e.Graphics.DrawRectangle(Pens.Purple, rect);

                e.Graphics.DrawString(string.Join(" ", notes[i].Select(n => string.Format("{0}{1}", simpleNotes[n % 12], (n / 12) - 2))), Font, Brushes.Turquoise, 85, y1);
            }
        }
    }
}
