using Kermalis.GBAMusicStudio.Core;
using Kermalis.GBAMusicStudio.Properties;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.GBAMusicStudio.UI
{
    class TrackInfo
    {
        public short Tempo; public int Position;
        public int[] Positions = new int[16];
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
            {
                Notes[i] = new sbyte[0];
            }
        }
    }

    [DesignerCategory("")]
    class TrackInfoControl : UserControl
    {
        const int checkboxSize = 15;

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
                pianos[i] = new CheckBox
                {
                    BackColor = Color.Transparent,
                    Size = new Size(checkboxSize, checkboxSize),
                    Checked = true
                };
                pianos[i].CheckStateChanged += TogglePiano;
                pianos[i].VisibleChanged += ToggleVisibility;
                mutes[i] = new CheckBox
                {
                    BackColor = Color.Transparent,
                    Size = new Size(checkboxSize, checkboxSize),
                    Checked = true
                };
                mutes[i].CheckStateChanged += ToggleMute;
                mutes[i].VisibleChanged += ToggleVisibility;
            }
            Controls.AddRange(pianos);
            Controls.AddRange(mutes);

            Resize += (o, s) => Invalidate();
            DeleteData();
        }

        void ToggleVisibility(object sender, EventArgs e) => ((CheckBox)sender).Checked = ((CheckBox)sender).Visible;
        void TogglePiano(object sender, EventArgs e)
        {
            if (ParentForm == null)
            {
                return;
            }
            var check = (CheckBox)sender;
            if (check == pianos[16])
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongPlayer.Instance.NumTracks; i++)
                {
                    pianos[i].Checked = b;
                }
            }
            else
            {
                int on = 0;
                for (int i = 0; i < SongPlayer.Instance.NumTracks; i++)
                {
                    if (pianos[i] == check)
                    {
                        MainForm.Instance.PianoTracks[i] = pianos[i].Checked && pianos[i].Visible;
                    }
                    if (pianos[i].Checked)
                    {
                        on++;
                    }
                }
                pianos[16].CheckStateChanged -= TogglePiano;
                pianos[16].CheckState = on == SongPlayer.Instance.NumTracks ? CheckState.Checked : (on == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                pianos[16].CheckStateChanged += TogglePiano;
            }
        }
        void ToggleMute(object sender, EventArgs e)
        {
            var check = (CheckBox)sender;
            if (check == mutes[16])
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongPlayer.Instance.NumTracks; i++)
                {
                    mutes[i].Checked = b;
                }
            }
            else
            {
                int on = 0;
                for (int i = 0; i < SongPlayer.Instance.NumTracks; i++)
                {
                    if (mutes[i] == check)
                    {
                        SoundMixer.Instance.Mutes[i] = !check.Checked;
                    }
                    if (mutes[i].Checked)
                    {
                        on++;
                    }
                }
                mutes[16].CheckStateChanged -= ToggleMute;
                mutes[16].CheckState = on == SongPlayer.Instance.NumTracks ? CheckState.Checked : (on == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                mutes[16].CheckStateChanged += ToggleMute;
            }
        }

        public void DeleteData()
        {
            Info = new TrackInfo();
            previousNotes = new Tuple<int[], string[]>(new int[16], new string[16]);
            for (int i = 0; i < 16; i++)
            {
                previousNotes.Item2[i] = string.Empty;
            }
            for (int i = SongPlayer.Instance.NumTracks; i < 16; i++)
            {
                pianos[i].Visible = mutes[i].Visible = false;
            }
            Invalidate();
        }

        float infoHeight, infoY, positionX, notesX, delayX, typeEndX, typeX, voicesX, row2ElementAdditionX, yMargin, trackHeight, row2Offset, tempoX;
        int checkboxOffset, barHeight, barStartX, barWidth, bwd, barRightBoundX, barCenterX;
        protected override void OnResize(EventArgs e)
        {
            infoHeight = Height / 25.125f;
            infoY = infoHeight - (TextRenderer.MeasureText("A", Font).Height * 1.125f);
            checkboxOffset = (checkboxSize - 13) / 2;
            positionX = (checkboxSize * 2) + (checkboxOffset * 2);
            int FWidth = Width - (int)positionX; // Width between checkboxes' edges and the window edge
            notesX = positionX + (FWidth / 4f);
            delayX = positionX + (FWidth / 6f);
            typeEndX = positionX + FWidth - (FWidth / 100f);
            typeX = typeEndX - TextRenderer.MeasureText("Type", Font).Width;
            voicesX = positionX + (FWidth / 25f);
            row2ElementAdditionX = FWidth / 15f;

            yMargin = Height / 200f;
            trackHeight = (Height - yMargin) / 16.5f;
            row2Offset = trackHeight / 2.5f;
            barHeight = (int)(Height / 30.3f);
            barStartX = (int)(positionX + (FWidth / 2.35f));
            barWidth = (int)(FWidth / 2.95f);
            bwd = barWidth % 2; // Add/Subtract by 1 if the bar width is odd
            barRightBoundX = barStartX + barWidth - bwd;
            barCenterX = barStartX + (barWidth / 2);

            tempoX = barCenterX - (TextRenderer.MeasureText("Tempo - 999", Font).Width / 2);

            base.OnResize(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var bg = new SolidBrush(Theme.PlayerColor);
            e.Graphics.FillRectangle(bg, e.ClipRectangle);

            mutes[16].Location = new Point(checkboxOffset, (int)infoY + checkboxOffset);
            pianos[16].Location = new Point(checkboxSize + checkboxOffset * 2, (int)infoY + checkboxOffset);
            e.Graphics.DrawString(Strings.PlayerPosition, Font, Brushes.Lime, positionX, infoY);
            e.Graphics.DrawString(Strings.PlayerDelay, Font, Brushes.Crimson, delayX, infoY);
            e.Graphics.DrawString(Strings.PlayerNotes, Font, Brushes.Turquoise, notesX, infoY);
            e.Graphics.DrawString("L", Font, Brushes.GreenYellow, barStartX - 5, infoY);
            e.Graphics.DrawString(Strings.PlayerTempo + " - " + Info.Tempo.ToString(), Font, Brushes.Cyan, tempoX, infoY);
            e.Graphics.DrawString("R", Font, Brushes.GreenYellow, barRightBoundX - 5, infoY);
            e.Graphics.DrawString(Strings.PlayerType, Font, Brushes.DeepPink, typeX, infoY);
            e.Graphics.DrawLine(Pens.Gold, 0, infoHeight, Width, infoHeight);

            for (int i = 0; i < SongPlayer.Instance.NumTracks; i++)
            {
                float r1y = infoHeight + yMargin + (i * trackHeight); // Row 1 y
                float r2y = r1y + row2Offset; // Row 2 y
                int by = (int)(r1y + yMargin); // Bar y
                int pax = (int)(barStartX + (barWidth / 2) + (barWidth / 2 * (Info.Pans[i] / (float)Engine.GetPanpotRange()))); // Pan line x

                Color color = Config.Instance.GetColor(Info.Voices[i], ROM.Instance.Game.Remap, true);
                var pen = new Pen(color);
                var brush = new SolidBrush(color);
                byte velocity = (byte)((Info.Lefts[i] + Info.Rights[i]) * byte.MaxValue);
                var lBrush = new LinearGradientBrush(new Point(barStartX, by), new Point(barStartX + barWidth, by + barHeight), Color.FromArgb(velocity, color), Color.FromArgb(Math.Min(velocity * 3, 0xFF), color));

                mutes[i].Location = new Point(checkboxOffset, (int)r1y + checkboxOffset);
                pianos[i].Visible = mutes[i].Visible = true;
                pianos[i].Location = new Point(checkboxSize + (checkboxOffset * 2), (int)r1y + checkboxOffset);

                e.Graphics.DrawString(string.Format("0x{0:X7}", Info.Positions[i]), Font, Brushes.Lime, positionX, r1y);
                e.Graphics.DrawString(Info.Delays[i].ToString(), Font, Brushes.Crimson, delayX, r1y);

                e.Graphics.DrawString(Info.Voices[i].ToString(), Font, brush, voicesX, r2y);
                e.Graphics.DrawString(Info.Pans[i].ToString(), Font, Brushes.OrangeRed, voicesX + row2ElementAdditionX, r2y);
                e.Graphics.DrawString(Info.Volumes[i].ToString(), Font, Brushes.LightSeaGreen, voicesX + (row2ElementAdditionX * 2), r2y);
                e.Graphics.DrawString(Info.Mods[i].ToString(), Font, Brushes.SkyBlue, voicesX + (row2ElementAdditionX * 3), r2y);
                e.Graphics.DrawString(Info.Pitches[i].ToString(), Font, Brushes.Purple, voicesX + (row2ElementAdditionX * 4), r2y);

                e.Graphics.DrawLine(Pens.GreenYellow, barStartX, by, barStartX, by + barHeight); // Left bar bound line
                if (Config.Instance.CenterIndicators)
                {
                    e.Graphics.DrawLine(pen, barCenterX, by, barCenterX, by + barHeight); // Center line
                }
                if (Config.Instance.PanpotIndicators)
                {
                    e.Graphics.DrawLine(Pens.OrangeRed, pax, by, pax, by + barHeight); // Pan line
                }
                e.Graphics.DrawLine(Pens.GreenYellow, barRightBoundX, by, barRightBoundX, by + barHeight); // Right bar bound line

                var rect = new Rectangle((int)(barStartX + (barWidth / 2) - (Info.Lefts[i] * barWidth / 2)) + bwd,
                    by,
                    (int)((Info.Lefts[i] + Info.Rights[i]) * barWidth / 2),
                    barHeight);
                e.Graphics.FillRectangle(lBrush, rect);
                e.Graphics.DrawRectangle(pen, rect);

                string theseNotes = string.Join(" ", Info.Notes[i].Select(n => SongEvent.NoteName(n)));
                bool empty = string.IsNullOrEmpty(theseNotes);
                theseNotes = empty ? string.Empty : theseNotes;
                if (empty && previousNotes.Item1[i]++ < Config.Instance.RefreshRate * 10)
                {
                    theseNotes = previousNotes.Item2[i];
                }
                else if (!empty || previousNotes.Item2[i] != theseNotes)
                {
                    previousNotes.Item1[i] = 0;
                    previousNotes.Item2[i] = theseNotes;
                }
                e.Graphics.DrawString(theseNotes, Font, Brushes.Turquoise, notesX, r1y);

                e.Graphics.DrawString(Info.Types[i], Font, Brushes.DeepPink, typeEndX - e.Graphics.MeasureString(Info.Types[i], Font).Width, by + (row2Offset / (Font.Size / 2.5f)));

                bg.Dispose();
                pen.Dispose();
                brush.Dispose();
                lBrush.Dispose();
            }
            base.OnPaint(e);
        }
    }
}
