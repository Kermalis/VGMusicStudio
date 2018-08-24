using GBAMusicStudio.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    class TrackInfo
    {
        public ushort Tempo; public uint Position;
        public uint[] Positions = new uint[16];
        public byte[] Voices = new byte[16], Volumes = new byte[16],
            Delays = new byte[16], Mods = new byte[16];
        public sbyte[] Pans = new sbyte[16];
        public float[] Lefts = new float[16], Rights = new float[16];
        public int[] Pitches = new int[16];
        public string[] Types = new string[16];
        public sbyte[][] Notes = new sbyte[16][];

        public TrackInfo()
        {
            for (int i = 0; i < 16; i++)
                Notes[i] = new sbyte[0];
        }
    }

    [DesignerCategory("")]
    class TrackInfoControl : UserControl
    {
        readonly string noNotes = ""; //"…";
        readonly int checkboxSize = 15;

        readonly CheckBox[] mutes;
        readonly CheckBox[] pianos;

        public TrackInfo Info = new TrackInfo();
        Tuple<int[], string[]> previousNotes;

        public TrackInfoControl()
        {
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(525, 675);

            pianos = new CheckBox[17]; // Index 16 is master
            mutes = new CheckBox[17];
            for (int i = 0; i < 17; i++)
            {
                pianos[i] = new CheckBox()
                {
                    BackColor = Color.Transparent,
                    Checked = i == 0,
                    Size = new Size(checkboxSize, checkboxSize)
                };
                pianos[i].CheckStateChanged += TogglePiano;
                pianos[i].VisibleChanged += ToggleVisibility;
                mutes[i] = new CheckBox()
                {
                    BackColor = Color.Transparent,
                    Size = new Size(checkboxSize, checkboxSize)
                };
                mutes[i].CheckStateChanged += ToggleMute;
                mutes[i].VisibleChanged += ToggleVisibility;
            }
            Controls.AddRange(mutes);
            Controls.AddRange(pianos);

            Resize += (o, s) => Invalidate();
            DeleteData();
        }

        void ToggleVisibility(object sender, EventArgs e) => ((CheckBox)sender).Checked = ((CheckBox)sender).Visible;
        void TogglePiano(object sender, EventArgs e)
        {
            if (ParentForm == null) return;
            var check = (CheckBox)sender;
            if (check == pianos[16])
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongPlayer.NumTracks; i++)
                    pianos[i].Checked = b;
            }
            else
            {
                int on = 0;
                for (int i = 0; i < SongPlayer.NumTracks; i++)
                {
                    if (pianos[i] == check)
                        ((MainForm)ParentForm).PianoTracks[i] = pianos[i].Checked && pianos[i].Visible;
                    if (pianos[i].Checked)
                        on++;
                }
                pianos[16].CheckStateChanged -= TogglePiano;
                pianos[16].CheckState = on == SongPlayer.NumTracks ? CheckState.Checked : (on == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                pianos[16].CheckStateChanged += TogglePiano;
            }
        }
        void ToggleMute(object sender, EventArgs e)
        {
            var check = (CheckBox)sender;
            if (check == mutes[16])
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongPlayer.NumTracks; i++)
                    mutes[i].Checked = b;
            }
            else
            {
                int on = 0;
                for (int i = 0; i < SongPlayer.NumTracks; i++)
                {
                    if (mutes[i] == check)
                        SoundMixer.SetMute(i, !check.Checked);
                    if (mutes[i].Checked)
                        on++;
                }
                mutes[16].CheckStateChanged -= ToggleMute;
                mutes[16].CheckState = on == SongPlayer.NumTracks ? CheckState.Checked : (on == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                mutes[16].CheckStateChanged += ToggleMute;
            }
        }

        public void DeleteData()
        {
            Info = new TrackInfo();
            previousNotes = new Tuple<int[], string[]>(new int[16], new string[16]);
            for (int i = 0; i < 16; i++)
                previousNotes.Item2[i] = noNotes;
            for (int i = SongPlayer.NumTracks; i < 16; i++)
                pianos[i].Visible = mutes[i].Visible = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var bg = new SolidBrush(Theme.PlayerColor);
            e.Graphics.FillRectangle(bg, e.ClipRectangle);

            float ih = Height / 25.125f; // Info height
            float iy = ih - (e.Graphics.MeasureString("A", Font).Height * 1.125f); // Info y
            int co = (checkboxSize - 13) / 2; // Checkbox offset
            float px = checkboxSize * 2 + co * 2; // Position x
            int FWidth = Width - (int)px; // Fake width
            float dex = px + FWidth / 5.75f; // Del x
            float dx = dex - e.Graphics.MeasureString("Delay", Font).Width + e.Graphics.MeasureString("99", Font).Width; // "Delay" x
            float nx = px + FWidth / 4.4f; // Notes x
            float td = FWidth / 100f; // Voice type difference
            float ix = px + FWidth - td;
            float itx = ix - e.Graphics.MeasureString("Type", Font).Width; // "Type" x
            float vox = px + FWidth / 25f; // Voices x
            float r2d = FWidth / 15f; // Row 2's addition per element

            float ym = Height / 200f; // y margin
            float th = (Height - ym) / 16.5f; // Track height
            float r2o = th / 2.5f;
            int bh = (int)(Height / 30.3f); // Bar height
            int bx = (int)(px + FWidth / 2.35f); // Bar start x
            int bw = (int)(FWidth / 2.95f); // Bar width
            int bwd = bw % 2; // Add/Subtract by 1 if the bar width is odd
            int brx = bx + bw - bwd; // Bar right bound x
            int cx = bx + (bw / 2); // Bar center x

            string tempoStr = "Tempo - " + Info.Tempo.ToString();
            float tx = cx - (e.Graphics.MeasureString(tempoStr, Font).Width / 2); // "Tempo - 120" x

            mutes[16].Location = new Point(co, (int)iy + co);
            pianos[16].Location = new Point(checkboxSize + co * 2, (int)iy + co);
            e.Graphics.DrawString("Position", Font, Brushes.Lime, px, iy);
            e.Graphics.DrawString("Delay", Font, Brushes.Crimson, dx, iy);
            e.Graphics.DrawString("Notes", Font, Brushes.Turquoise, nx, iy);
            e.Graphics.DrawString("L", Font, Brushes.GreenYellow, bx - 5, iy);
            e.Graphics.DrawString(tempoStr, Font, Brushes.Cyan, tx, iy);
            e.Graphics.DrawString("R", Font, Brushes.GreenYellow, brx - 5, iy);
            e.Graphics.DrawString("Type", Font, Brushes.DeepPink, itx, iy);
            e.Graphics.DrawLine(Pens.Gold, 0, ih, Width, ih);

            for (int i = 0; i < SongPlayer.NumTracks; i++)
            {
                float r1y = ih + ym + (i * th); // Row 1 y
                float r2y = r1y + r2o; // Row 2 y
                int by = (int)(r1y + ym); // Bar y
                int pax = (int)(bx + (bw / 2) + (bw / 2 * (Info.Pans[i] / (float)Engine.GetPanpotRange()))); // Pan line x

                Color color = Config.Colors[Info.Voices[i]];
                Pen pen = new Pen(color);
                var brush = new SolidBrush(color);
                byte velocity = (byte)((Info.Lefts[i] + Info.Rights[i]) * 0xFF);
                var lBrush = new LinearGradientBrush(new Point(bx, by), new Point(bx + bw, by + bh), Color.FromArgb(velocity, color), Color.FromArgb(Math.Min(velocity * 4, 0xFF), color));

                mutes[i].Location = new Point(co, (int)r1y + co); // Checkboxes
                pianos[i].Visible = mutes[i].Visible = true;
                pianos[i].Location = new Point(checkboxSize + co * 2, (int)r1y + co);

                e.Graphics.DrawString(string.Format("0x{0:X7}", Info.Positions[i]), Font, Brushes.Lime, px, r1y);
                e.Graphics.DrawString(Info.Delays[i].ToString(), Font, Brushes.Crimson, dex, r1y);

                e.Graphics.DrawString(Info.Voices[i].ToString(), Font, brush, vox, r2y);
                e.Graphics.DrawString(Info.Pans[i].ToString(), Font, Brushes.OrangeRed, vox + r2d, r2y);
                e.Graphics.DrawString(Info.Volumes[i].ToString(), Font, Brushes.LightSeaGreen, vox + (r2d * 2), r2y);
                e.Graphics.DrawString(Info.Mods[i].ToString(), Font, Brushes.SkyBlue, vox + (r2d * 3), r2y);
                e.Graphics.DrawString(Info.Pitches[i].ToString(), Font, Brushes.Purple, vox + (r2d * 4), r2y);

                e.Graphics.DrawLine(Pens.GreenYellow, bx, by, bx, by + bh); // Left bar bound line
                if (Config.CenterIndicators) e.Graphics.DrawLine(pen, cx, by, cx, by + bh); // Center line
                if (Config.PanpotIndicators) e.Graphics.DrawLine(Pens.OrangeRed, pax, by, pax, by + bh); // Pan line
                e.Graphics.DrawLine(Pens.GreenYellow, brx, by, brx, by + bh); // Right bar bound line

                var rect = new Rectangle((int)(bx + (bw / 2) - (Info.Lefts[i] * bw / 2)) + bwd,
                    by,
                    (int)((Info.Lefts[i] + Info.Rights[i]) * bw / 2),
                    bh);
                e.Graphics.FillRectangle(lBrush, rect);
                e.Graphics.DrawRectangle(pen, rect);

                string theseNotes = string.Join(" ", Info.Notes[i].Select(n => SongEvent.NoteName(n)));
                bool empty = string.IsNullOrEmpty(theseNotes);
                theseNotes = empty ? noNotes : theseNotes;
                if (empty && previousNotes.Item1[i]++ < Config.RefreshRate * 10) theseNotes = previousNotes.Item2[i];
                else if (!empty || previousNotes.Item2[i] != theseNotes) { previousNotes.Item1[i] = 0; previousNotes.Item2[i] = theseNotes; }
                e.Graphics.DrawString(theseNotes, Font, Brushes.Turquoise, nx, r1y);

                var strSize = e.Graphics.MeasureString(Info.Types[i], Font);
                e.Graphics.DrawString(Info.Types[i], Font, Brushes.DeepPink, ix - strSize.Width, by + (r2o / (Font.Size / 2.5f)));

                bg.Dispose();
                pen.Dispose();
                brush.Dispose();
                lBrush.Dispose();
            }
            base.OnPaint(e);
        }
    }
}
