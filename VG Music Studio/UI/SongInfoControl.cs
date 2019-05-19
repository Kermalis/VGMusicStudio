using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    [DesignerCategory("")]
    internal class SongInfoControl : Control
    {
        public class SongInfo
        {
            public class Track
            {
                public long Position;
                public byte Voice;
                public byte Volume;
                public int LFO;
                public long Rest;
                public sbyte Panpot;
                public float LeftVolume;
                public float RightVolume;
                public int PitchBend;
                public byte Extra;
                public string Type;
                public byte[] Keys = new byte[MaxKeys];

                public int PreviousKeysTime;
                public string PreviousKeys;

                public Track()
                {
                    for (int i = 0; i < MaxKeys; i++)
                    {
                        Keys[i] = byte.MaxValue;
                    }
                }
            }
            public const int MaxKeys = 33; // DSE is currently set to use 32 channels
            public const int MaxTracks = 18; // PMD2 has a few songs with 18 tracks

            public ushort Tempo;
            public Track[] Tracks = new Track[MaxTracks];

            public SongInfo()
            {
                for (int i = 0; i < MaxTracks; i++)
                {
                    Tracks[i] = new Track();
                }
            }
        }

        private const int checkboxSize = 15;

        private readonly CheckBox[] mutes;
        private readonly CheckBox[] pianos;
        private readonly SolidBrush solidBrush = new SolidBrush(Theme.PlayerColor);
        private readonly Pen pen = new Pen(Color.Transparent);

        public SongInfo Info;
        private int numTracksToDraw;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                solidBrush.Dispose();
                pen.Dispose();
            }
            base.Dispose(disposing);
        }
        public SongInfoControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(675, 675);

            pianos = new CheckBox[SongInfo.MaxTracks + 1];
            mutes = new CheckBox[SongInfo.MaxTracks + 1];
            for (int i = 0; i < SongInfo.MaxTracks + 1; i++)
            {
                pianos[i] = new CheckBox
                {
                    BackColor = Color.Transparent,
                    Checked = true,
                    Size = new Size(checkboxSize, checkboxSize),
                    TabStop = false
                };
                pianos[i].CheckStateChanged += TogglePiano;
                mutes[i] = new CheckBox
                {
                    BackColor = Color.Transparent,
                    Checked = true,
                    Size = new Size(checkboxSize, checkboxSize),
                    TabStop = false
                };
                mutes[i].CheckStateChanged += ToggleMute;
            }
            Controls.AddRange(pianos);
            Controls.AddRange(mutes);

            SetNumTracks(0);
            DeleteData();
        }

        private void TogglePiano(object sender, EventArgs e)
        {
            var check = (CheckBox)sender;
            CheckBox master = pianos[SongInfo.MaxTracks];
            if (check == master)
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    pianos[i].Checked = b;
                }
            }
            else
            {
                int numChecked = 0;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    if (pianos[i] == check)
                    {
                        MainForm.Instance.PianoTracks[i] = pianos[i].Checked;
                    }
                    if (pianos[i].Checked)
                    {
                        numChecked++;
                    }
                }
                master.CheckStateChanged -= TogglePiano;
                master.CheckState = numChecked == SongInfo.MaxTracks ? CheckState.Checked : (numChecked == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                master.CheckStateChanged += TogglePiano;
            }
        }
        private void ToggleMute(object sender, EventArgs e)
        {
            var check = (CheckBox)sender;
            CheckBox master = mutes[SongInfo.MaxTracks];
            if (check == master)
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    mutes[i].Checked = b;
                }
            }
            else
            {
                int numChecked = 0;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    if (mutes[i] == check)
                    {
                        Engine.Instance.Mixer.Mutes[i] = !check.Checked;
                    }
                    if (mutes[i].Checked)
                    {
                        numChecked++;
                    }
                }
                master.CheckStateChanged -= ToggleMute;
                master.CheckState = numChecked == SongInfo.MaxTracks ? CheckState.Checked : (numChecked == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                master.CheckStateChanged += ToggleMute;
            }
        }

        public void DeleteData()
        {
            Info = new SongInfo();
            Invalidate();
        }
        public void SetNumTracks(int num)
        {
            numTracksToDraw = num;
            pianos[SongInfo.MaxTracks].Enabled = mutes[SongInfo.MaxTracks].Enabled = num > 0;
            for (int i = 0; i < SongInfo.MaxTracks; i++)
            {
                pianos[i].Visible = mutes[i].Visible = i < num;
            }
            OnResize(EventArgs.Empty);
        }
        public void ResetMutes()
        {
            for (int i = 0; i < SongInfo.MaxTracks + 1; i++)
            {
                CheckBox mute = mutes[i];
                mute.CheckStateChanged -= ToggleMute;
                mute.CheckState = CheckState.Checked;
                mute.CheckStateChanged += ToggleMute;
            }
        }

        private float infoHeight, infoY, positionX, keysX, delayX, typeEndX, typeX, voicesX, row2ElementAdditionX, yMargin, trackHeight, row2Offset, tempoX;
        private int barHeight, barStartX, barWidth, bwd, barRightBoundX, barCenterX;
        protected override void OnResize(EventArgs e)
        {
            infoHeight = Height / 30f;
            infoY = infoHeight - (TextRenderer.MeasureText("A", Font).Height * 1.125f);
            positionX = (checkboxSize * 2) + 2;
            int FWidth = Width - (int)positionX; // Width between checkboxes' edges and the window edge
            keysX = positionX + (FWidth / 4.4f);
            delayX = positionX + (FWidth / 7.5f);
            typeEndX = positionX + FWidth - (FWidth / 100f);
            typeX = typeEndX - TextRenderer.MeasureText(Strings.PlayerType, Font).Width;
            voicesX = positionX + (FWidth / 25f);
            row2ElementAdditionX = FWidth / 15f;

            yMargin = Height / 200f;
            trackHeight = (Height - yMargin) / (Math.Max(16, numTracksToDraw) * 1.04f);
            row2Offset = trackHeight / 2.5f;
            barHeight = (int)(Height / 30.3f);
            barStartX = (int)(positionX + (FWidth / 2.35f));
            barWidth = (int)(FWidth / 2.95f);
            bwd = barWidth % 2; // Add/Subtract by 1 if the bar width is odd
            barRightBoundX = barStartX + barWidth - bwd;
            barCenterX = barStartX + (barWidth / 2);

            tempoX = barCenterX - (TextRenderer.MeasureText(string.Format("{0} - 999", Strings.PlayerTempo), Font).Width / 2);

            if (mutes != null)
            {
                int x1 = 3;
                int x2 = checkboxSize + 4;
                int y = (int)infoY + 3;
                mutes[SongInfo.MaxTracks].Location = new Point(x1, y);
                pianos[SongInfo.MaxTracks].Location = new Point(x2, y);
                for (int i = 0; i < numTracksToDraw; i++)
                {
                    float r1y = infoHeight + yMargin + (i * trackHeight);
                    y = (int)r1y + 4;
                    mutes[i].Location = new Point(x1, y);
                    pianos[i].Location = new Point(x2, y);
                }
            }

            base.OnResize(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            solidBrush.Color = Theme.PlayerColor;
            e.Graphics.FillRectangle(solidBrush, e.ClipRectangle);

            e.Graphics.DrawString(Strings.PlayerPosition, Font, Brushes.Lime, positionX, infoY);
            e.Graphics.DrawString(Strings.PlayerRest, Font, Brushes.Crimson, delayX, infoY);
            e.Graphics.DrawString(Strings.PlayerNotes, Font, Brushes.Turquoise, keysX, infoY);
            e.Graphics.DrawString("L", Font, Brushes.GreenYellow, barStartX - 5, infoY);
            e.Graphics.DrawString(string.Format("{0} - ", Strings.PlayerTempo) + Info.Tempo, Font, Brushes.Cyan, tempoX, infoY);
            e.Graphics.DrawString("R", Font, Brushes.GreenYellow, barRightBoundX - 5, infoY);
            e.Graphics.DrawString(Strings.PlayerType, Font, Brushes.DeepPink, typeX, infoY);
            e.Graphics.DrawLine(Pens.Gold, 0, infoHeight, Width, infoHeight);

            for (int i = 0; i < numTracksToDraw; i++)
            {
                SongInfo.Track track = Info.Tracks[i];
                float r1y = infoHeight + yMargin + (i * trackHeight); // Row 1 y
                e.Graphics.DrawString(string.Format("0x{0:X}", track.Position), Font, Brushes.Lime, positionX, r1y);
                e.Graphics.DrawString(track.Rest.ToString(), Font, Brushes.Crimson, delayX, r1y);

                float r2y = r1y + row2Offset; // Row 2 y
                e.Graphics.DrawString(track.Panpot.ToString(), Font, Brushes.OrangeRed, voicesX + row2ElementAdditionX, r2y);
                e.Graphics.DrawString(track.Volume.ToString(), Font, Brushes.LightSeaGreen, voicesX + (row2ElementAdditionX * 2), r2y);
                e.Graphics.DrawString(track.LFO.ToString(), Font, Brushes.SkyBlue, voicesX + (row2ElementAdditionX * 3), r2y);
                e.Graphics.DrawString(track.PitchBend.ToString(), Font, Brushes.Purple, voicesX + (row2ElementAdditionX * 4), r2y);
                e.Graphics.DrawString(track.Extra.ToString(), Font, Brushes.HotPink, voicesX + (row2ElementAdditionX * 5), r2y);

                int by = (int)(r1y + yMargin); // Bar y
                int byh = by + barHeight;
                e.Graphics.DrawString(track.Type, Font, Brushes.DeepPink, typeEndX - e.Graphics.MeasureString(track.Type, Font).Width, by + (row2Offset / (Font.Size / 2.5f)));
                e.Graphics.DrawLine(Pens.GreenYellow, barStartX, by, barStartX, byh); // Left bar bound line
                e.Graphics.DrawLine(Pens.GreenYellow, barRightBoundX, by, barRightBoundX, byh); // Right bar bound line
                if (GlobalConfig.Instance.PanpotIndicators)
                {
                    int pax = (int)(barStartX + (barWidth / 2) + (barWidth / 2 * (track.Panpot / (float)0x40))); // Pan line x
                    e.Graphics.DrawLine(Pens.OrangeRed, pax, by, pax, byh); // Pan line
                }

                {
                    Color color = GlobalConfig.Instance.Colors[track.Voice];
                    solidBrush.Color = color;
                    pen.Color = color;
                    e.Graphics.DrawString(track.Voice.ToString(), Font, solidBrush, voicesX, r2y);
                    var rect = new Rectangle((int)(barStartX + (barWidth / 2) - (track.LeftVolume * barWidth / 2)) + bwd,
                            by,
                            (int)((track.LeftVolume + track.RightVolume) * barWidth / 2),
                            barHeight);
                    if (!rect.IsEmpty)
                    {
                        byte velocity = (byte)(Math.Min(1f, (track.LeftVolume + track.RightVolume) * 2) * byte.MaxValue);
                        solidBrush.Color = Color.FromArgb(velocity, color);
                        e.Graphics.FillRectangle(solidBrush, rect);
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                    if (GlobalConfig.Instance.CenterIndicators)
                    {
                        e.Graphics.DrawLine(pen, barCenterX, by, barCenterX, byh); // Center line
                    }
                }
                {
                    string keysString;
                    if (track.Keys[0] == byte.MaxValue)
                    {
                        if (track.PreviousKeysTime != 0)
                        {
                            track.PreviousKeysTime--;
                            keysString = track.PreviousKeys;
                        }
                        else
                        {
                            keysString = string.Empty;
                        }
                    }
                    else
                    {
                        keysString = string.Empty;
                        for (int nk = 0; nk < SongInfo.MaxKeys; nk++)
                        {
                            byte k = track.Keys[nk];
                            if (k == byte.MaxValue)
                            {
                                break;
                            }
                            else
                            {
                                if (nk != 0)
                                {
                                    keysString += ' ';
                                }
                                keysString += Utils.GetNoteName(k);
                            }
                        }
                        track.PreviousKeysTime = GlobalConfig.Instance.RefreshRate << 2;
                        track.PreviousKeys = keysString;
                    }
                    if (keysString != string.Empty)
                    {
                        e.Graphics.DrawString(keysString, Font, Brushes.Turquoise, keysX, r1y);
                    }
                }
            }
            base.OnPaint(e);
        }
    }
}
