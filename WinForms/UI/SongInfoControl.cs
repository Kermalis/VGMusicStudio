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
            public const int MaxKeys = 32 + 1; // DSE is currently set to use 32 channels
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

        private const int _checkboxSize = 15;

        private readonly CheckBox[] _mutes;
        private readonly CheckBox[] _pianos;
        private readonly SolidBrush _solidBrush = new SolidBrush(Theme.PlayerColor);
        private readonly Pen _pen = new Pen(Color.Transparent);

        public SongInfo Info;
        private int _numTracksToDraw;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _solidBrush.Dispose();
                _pen.Dispose();
            }
            base.Dispose(disposing);
        }
        public SongInfoControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(675, 675);

            _pianos = new CheckBox[SongInfo.MaxTracks + 1];
            _mutes = new CheckBox[SongInfo.MaxTracks + 1];
            for (int i = 0; i < SongInfo.MaxTracks + 1; i++)
            {
                _pianos[i] = new CheckBox
                {
                    BackColor = Color.Transparent,
                    Checked = true,
                    Size = new Size(_checkboxSize, _checkboxSize),
                    TabStop = false
                };
                _pianos[i].CheckStateChanged += TogglePiano;
                _mutes[i] = new CheckBox
                {
                    BackColor = Color.Transparent,
                    Checked = true,
                    Size = new Size(_checkboxSize, _checkboxSize),
                    TabStop = false
                };
                _mutes[i].CheckStateChanged += ToggleMute;
            }
            Controls.AddRange(_pianos);
            Controls.AddRange(_mutes);

            SetNumTracks(0);
            DeleteData();
        }

        private void TogglePiano(object sender, EventArgs e)
        {
            var check = (CheckBox)sender;
            CheckBox master = _pianos[SongInfo.MaxTracks];
            if (check == master)
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    _pianos[i].Checked = b;
                }
            }
            else
            {
                int numChecked = 0;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    if (_pianos[i] == check)
                    {
                        MainForm.Instance.PianoTracks[i] = _pianos[i].Checked;
                    }
                    if (_pianos[i].Checked)
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
            CheckBox master = _mutes[SongInfo.MaxTracks];
            if (check == master)
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    _mutes[i].Checked = b;
                }
            }
            else
            {
                int numChecked = 0;
                for (int i = 0; i < SongInfo.MaxTracks; i++)
                {
                    if (_mutes[i] == check)
                    {
                        Engine.Instance.Mixer.Mutes[i] = !check.Checked;
                    }
                    if (_mutes[i].Checked)
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
            _numTracksToDraw = num;
            _pianos[SongInfo.MaxTracks].Enabled = _mutes[SongInfo.MaxTracks].Enabled = num > 0;
            for (int i = 0; i < SongInfo.MaxTracks; i++)
            {
                _pianos[i].Visible = _mutes[i].Visible = i < num;
            }
            OnResize(EventArgs.Empty);
        }
        public void ResetMutes()
        {
            for (int i = 0; i < SongInfo.MaxTracks + 1; i++)
            {
                CheckBox mute = _mutes[i];
                mute.CheckStateChanged -= ToggleMute;
                mute.CheckState = CheckState.Checked;
                mute.CheckStateChanged += ToggleMute;
            }
        }

        private float _infoHeight, _infoY, _positionX, _keysX, _delayX, _typeEndX, _typeX, _voicesX, _row2ElementAdditionX, _yMargin, _trackHeight, _row2Offset, _tempoX;
        private int _barHeight, _barStartX, _barWidth, _bwd, _barRightBoundX, _barCenterX;
        protected override void OnResize(EventArgs e)
        {
            _infoHeight = Height / 30f;
            _infoY = _infoHeight - (TextRenderer.MeasureText("A", Font).Height * 1.125f);
            _positionX = (_checkboxSize * 2) + 2;
            int fWidth = Width - (int)_positionX; // Width between checkboxes' edges and the window edge
            _keysX = _positionX + (fWidth / 4.4f);
            _delayX = _positionX + (fWidth / 7.5f);
            _typeEndX = _positionX + fWidth - (fWidth / 100f);
            _typeX = _typeEndX - TextRenderer.MeasureText(Strings.PlayerType, Font).Width;
            _voicesX = _positionX + (fWidth / 25f);
            _row2ElementAdditionX = fWidth / 15f;

            _yMargin = Height / 200f;
            _trackHeight = (Height - _yMargin) / ((_numTracksToDraw < 16 ? 16 : _numTracksToDraw) * 1.04f);
            _row2Offset = _trackHeight / 2.5f;
            _barHeight = (int)(Height / 30.3f);
            _barStartX = (int)(_positionX + (fWidth / 2.35f));
            _barWidth = (int)(fWidth / 2.95f);
            _bwd = _barWidth % 2; // Add/Subtract by 1 if the bar width is odd
            _barRightBoundX = _barStartX + _barWidth - _bwd;
            _barCenterX = _barStartX + (_barWidth / 2);

            _tempoX = _barCenterX - (TextRenderer.MeasureText(string.Format("{0} - 999", Strings.PlayerTempo), Font).Width / 2);

            if (_mutes != null)
            {
                int x1 = 3;
                int x2 = _checkboxSize + 4;
                int y = (int)_infoY + 3;
                _mutes[SongInfo.MaxTracks].Location = new Point(x1, y);
                _pianos[SongInfo.MaxTracks].Location = new Point(x2, y);
                for (int i = 0; i < _numTracksToDraw; i++)
                {
                    float r1y = _infoHeight + _yMargin + (i * _trackHeight);
                    y = (int)r1y + 4;
                    _mutes[i].Location = new Point(x1, y);
                    _pianos[i].Location = new Point(x2, y);
                }
            }

            base.OnResize(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            _solidBrush.Color = Theme.PlayerColor;
            e.Graphics.FillRectangle(_solidBrush, e.ClipRectangle);

            e.Graphics.DrawString(Strings.PlayerPosition, Font, Brushes.Lime, _positionX, _infoY);
            e.Graphics.DrawString(Strings.PlayerRest, Font, Brushes.Crimson, _delayX, _infoY);
            e.Graphics.DrawString(Strings.PlayerNotes, Font, Brushes.Turquoise, _keysX, _infoY);
            e.Graphics.DrawString("L", Font, Brushes.GreenYellow, _barStartX - 5, _infoY);
            e.Graphics.DrawString(string.Format("{0} - ", Strings.PlayerTempo) + Info.Tempo, Font, Brushes.Cyan, _tempoX, _infoY);
            e.Graphics.DrawString("R", Font, Brushes.GreenYellow, _barRightBoundX - 5, _infoY);
            e.Graphics.DrawString(Strings.PlayerType, Font, Brushes.DeepPink, _typeX, _infoY);
            e.Graphics.DrawLine(Pens.Gold, 0, _infoHeight, Width, _infoHeight);

            for (int i = 0; i < _numTracksToDraw; i++)
            {
                SongInfo.Track track = Info.Tracks[i];
                float r1y = _infoHeight + _yMargin + (i * _trackHeight); // Row 1 y
                e.Graphics.DrawString(string.Format("0x{0:X}", track.Position), Font, Brushes.Lime, _positionX, r1y);
                e.Graphics.DrawString(track.Rest.ToString(), Font, Brushes.Crimson, _delayX, r1y);

                float r2y = r1y + _row2Offset; // Row 2 y
                e.Graphics.DrawString(track.Panpot.ToString(), Font, Brushes.OrangeRed, _voicesX + _row2ElementAdditionX, r2y);
                e.Graphics.DrawString(track.Volume.ToString(), Font, Brushes.LightSeaGreen, _voicesX + (_row2ElementAdditionX * 2), r2y);
                e.Graphics.DrawString(track.LFO.ToString(), Font, Brushes.SkyBlue, _voicesX + (_row2ElementAdditionX * 3), r2y);
                e.Graphics.DrawString(track.PitchBend.ToString(), Font, Brushes.Purple, _voicesX + (_row2ElementAdditionX * 4), r2y);
                e.Graphics.DrawString(track.Extra.ToString(), Font, Brushes.HotPink, _voicesX + (_row2ElementAdditionX * 5), r2y);

                int by = (int)(r1y + _yMargin); // Bar y
                int byh = by + _barHeight;
                e.Graphics.DrawString(track.Type, Font, Brushes.DeepPink, _typeEndX - e.Graphics.MeasureString(track.Type, Font).Width, by + (_row2Offset / (Font.Size / 2.5f)));
                e.Graphics.DrawLine(Pens.GreenYellow, _barStartX, by, _barStartX, byh); // Left bar bound line
                e.Graphics.DrawLine(Pens.GreenYellow, _barRightBoundX, by, _barRightBoundX, byh); // Right bar bound line
                if (GlobalConfig.Instance.PanpotIndicators)
                {
                    int pax = (int)(_barStartX + (_barWidth / 2) + (_barWidth / 2 * (track.Panpot / (float)0x40))); // Pan line x
                    e.Graphics.DrawLine(Pens.OrangeRed, pax, by, pax, byh); // Pan line
                }

                {
                    Color color = GlobalConfig.Instance.Colors[track.Voice];
                    _solidBrush.Color = color;
                    _pen.Color = color;
                    e.Graphics.DrawString(track.Voice.ToString(), Font, _solidBrush, _voicesX, r2y);
                    var rect = new Rectangle((int)(_barStartX + (_barWidth / 2) - (track.LeftVolume * _barWidth / 2)) + _bwd,
                            by,
                            (int)((track.LeftVolume + track.RightVolume) * _barWidth / 2),
                            _barHeight);
                    if (!rect.IsEmpty)
                    {
                        float velocity = (track.LeftVolume + track.RightVolume) * 2f;
                        if (velocity > 1f)
                        {
                            velocity = 1f;
                        }
                        _solidBrush.Color = Color.FromArgb((byte)(velocity * byte.MaxValue), color);
                        e.Graphics.FillRectangle(_solidBrush, rect);
                        e.Graphics.DrawRectangle(_pen, rect);
                    }
                    if (GlobalConfig.Instance.CenterIndicators)
                    {
                        e.Graphics.DrawLine(_pen, _barCenterX, by, _barCenterX, byh); // Center line
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
                        e.Graphics.DrawString(keysString, Font, Brushes.Turquoise, _keysX, r1y);
                    }
                }
            }
            base.OnPaint(e);
        }
    }
}
