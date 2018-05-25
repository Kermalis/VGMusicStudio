using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    internal class TrackInfoControl : UserControl
    {
        IContainer components = null;
        Color barColor = Color.FromArgb(0xa7, 0x44, 0xdd);
        readonly string noNotes = "…";

        readonly string[] simpleNotes = { "Cn", "Cs", "Dn", "Ds", "En", "Fn", "Fs", "Gn", "Gs", "An", "As", "Bn" };

        Tuple<int[], string[]> previousNotes = new Tuple<int[], string[]>(new int[16], new string[16]);
        uint[] positions = new uint[0];
        byte[] volumes = new byte[0];
        byte[] delays = new byte[0];
        byte[][] notes = new byte[0][];
        float[] velocities = new float[0];
        byte[] voices = new byte[0];
        byte[] mods = new byte[0];
        int[] bends = new int[0];
        float[] pans = new float[0];
        string[] types = new string[0];

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
            Font = new Font("Microsoft Tai Le", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Size = new Size(525, 675);
            Paint += TrackInfoControl_Paint;
            Resize += (o, s) => Invalidate();
            for (int i = 0; i < 16; i++)
                previousNotes.Item2[i] = noNotes;
            ResumeLayout(false);
        }

        internal TrackInfoControl() => InitializeComponent();

        internal void ReceiveData(uint[] pos, byte[] vol, byte[] del, byte[][] note, float[] vel, byte[] voice, byte[] mod, int[] bend, float[] pan, string[] type)
        {
            positions = pos; volumes = vol; delays = del; notes = note; velocities = vel; voices = voice; mods = mod; bends = bend; pans = pan; types = type;
            Invalidate();
        }

        void TrackInfoControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);

            float ih = Height / 25.125f; // Info height
            float dex = Width / 5.75f; // Del x
            float dx = dex - e.Graphics.MeasureString("Delay", Font).Width + e.Graphics.MeasureString("99", Font).Width; // "Delay" x
            float nx = Width / 4.4f; // Notes x
            float vox = Width / 25f; // Voices x
            float r2d = Width / 15f; // Row 2's addition per element

            float ym = Height / 201f; // y margin
            float xm = Width / 125f; // x margin
            float th = (Height - ym) / 16f; // Track height
            float r2o = th / 2.5f;
            int bh = (int)(Height / 30.3f); // Bar height
            int bx = (int)(Width / 2.35f); // Bar start x
            int bw = (int)(Width / 2.95f); // Bar width
            int cx = bx + (bw / 2); // Bar center x
            int bwd = bw % 2; // Add/Subtract by 1 if the bar width is odd
            float td = Width / 100f; // Voice type difference

            e.Graphics.DrawString("Position", Font, Brushes.Lime, 0, 5);
            e.Graphics.DrawString("Delay", Font, Brushes.Crimson, dx, 5);
            e.Graphics.DrawString("Notes", Font, Brushes.Turquoise, nx, 5);
            e.Graphics.DrawLine(Pens.Gold, 0, ih, Width, ih);

            for (int i = 0; i < positions.Length; i++)
            {
                float r1y = ih + ym + (i * th); // Row 1 y
                float r2y = r1y + r2o; // Row 2 y

                byte velocity = (byte)(velocities[i] * 0xFF);

                e.Graphics.DrawString(string.Format("0x{0:X6}", positions[i]), Font, Brushes.Lime, 0, r1y);
                e.Graphics.DrawString(delays[i].ToString(), Font, Brushes.Crimson, dex, r1y);

                e.Graphics.DrawString(voices[i].ToString(), Font, Brushes.OrangeRed, vox, r2y);
                e.Graphics.DrawString(velocity.ToString(), Font, Brushes.PeachPuff, vox + r2d, r2y);
                e.Graphics.DrawString(volumes[i].ToString(), Font, Brushes.LightSeaGreen, vox + (r2d * 2), r2y);
                e.Graphics.DrawString(mods[i].ToString(), Font, Brushes.SkyBlue, vox + (r2d * 3), r2y);
                e.Graphics.DrawString(bends[i].ToString(), Font, Brushes.Purple, vox + (r2d * 4), r2y);

                int by = (int)(r1y + ym); // Bar y
                int px = (int)(bx + (bw / 2) + (bw / 2 * pans[i])); // Pan line x

                e.Graphics.DrawLine(Pens.GreenYellow, bx, by, bx, by + bh); // Left bar bound line
                e.Graphics.DrawLine(Pens.Purple, cx, by, cx, by + bh); // Center line
                e.Graphics.DrawLine(Pens.OrangeRed, px, by, px, by + bh); // Pan line
                e.Graphics.DrawLine(Pens.GreenYellow, bx + bw - bwd, by, bx + bw - bwd, by + bh); // Right bar bound line

                float percentRight = (pans[i] + 1) * velocities[i],
                    percentLeft = (-pans[i] + 1) * velocities[i];

                var rect = new Rectangle((int)(bx + (bw / 2) - (percentLeft * bw / 2)) + bwd,
                    by,
                    (int)((percentLeft + percentRight) * bw / 2),
                    bh);
                e.Graphics.FillRectangle(
                    new LinearGradientBrush(new Point(bx, bh), new Point(bx + bw, bh), Color.FromArgb(velocity, barColor), Color.Purple),
                    rect);
                e.Graphics.DrawRectangle(Pens.Purple, rect);

                var strSize = e.Graphics.MeasureString(types[i], Font);
                e.Graphics.DrawString(types[i], Font, Brushes.Azure, Width - td - strSize.Width, by + (r2o / 2));

                string theseNotes = string.Join(" ", notes[i].Select(n => string.Format("{0}{1}", simpleNotes[n % 12], (n / 12) - 2)));
                bool empty = string.IsNullOrEmpty(theseNotes);
                theseNotes = empty ? noNotes : theseNotes;
                if (empty && previousNotes.Item1[i]++ < MainForm.RefreshRate) theseNotes = previousNotes.Item2[i];
                else if (previousNotes.Item2[i] != theseNotes) { previousNotes.Item1[i] = 0; previousNotes.Item2[i] = theseNotes; }
                e.Graphics.DrawString(theseNotes, Font, Brushes.Turquoise, nx, r1y);
            }
        }
    }
}
