#region License

/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    [DesignerCategory("")]
    internal class PianoControl : Control
    {
        private enum KeyType : byte
        {
            White, Black
        }
        private static readonly KeyType[] KeyTypeTable = new KeyType[12]
        {
            KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White, KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White
        };
        private const double blackKeyScale = 2.0 / 3.0;

        public class PianoKey : Control
        {
            public bool Dirty, Pressed;

            public readonly SolidBrush OnBrush = new SolidBrush(Color.Transparent);
            private readonly SolidBrush offBrush;

            public PianoKey(byte k)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.Selectable, false);
                offBrush = new SolidBrush(new HSLColor(160.0, 0.0, KeyTypeTable[k % 12] == KeyType.White ? k / 12 % 2 == 0 ? 240.0 : 120.0 : 0.0));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    OnBrush.Dispose();
                    offBrush.Dispose();
                }
                base.Dispose(disposing);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.FillRectangle(Pressed ? OnBrush : offBrush, 1, 1, Width - 2, Height - 2);
                e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
                base.OnPaint(e);
            }
        }

        private readonly PianoKey[] Keys = new PianoKey[0x80];
        public const int WhiteKeyCount = 75;
        public int WhiteKeyWidth;

        public PianoControl()
        {
            SetStyle(ControlStyles.Selectable, false);
            for (byte k = 0; k <= 0x7F; k++)
            {
                var key = new PianoKey(k);
                Keys[k] = key;
                if (KeyTypeTable[k % 12] == KeyType.Black)
                {
                    key.BringToFront();
                }
                Controls.Add(key);
            }
            SetKeySizes();
        }
        private void SetKeySizes()
        {
            WhiteKeyWidth = Width / WhiteKeyCount;
            int blackKeyWidth = (int)(WhiteKeyWidth * blackKeyScale);
            int blackKeyHeight = (int)(Height * blackKeyScale);
            int offset = WhiteKeyWidth - (blackKeyWidth / 2);
            int w = 0;
            for (int k = 0; k <= 0x7F; k++)
            {
                PianoKey key = Keys[k];
                if (KeyTypeTable[k % 12] == KeyType.White)
                {
                    key.Height = Height;
                    key.Width = WhiteKeyWidth;
                    key.Location = new Point(w * WhiteKeyWidth, 0);
                    w++;
                }
                else
                {
                    key.Height = blackKeyHeight;
                    key.Width = blackKeyWidth;
                    key.Location = new Point(offset + ((w - 1) * WhiteKeyWidth));
                    key.BringToFront();
                }
            }
        }

        public void UpdateKeys(SongInfoControl.SongInfo info, bool[] enabledTracks)
        {
            for (int k = 0; k <= 0x7F; k++)
            {
                PianoKey key = Keys[k];
                key.Dirty = key.Pressed;
                key.Pressed = false;
            }
            for (int i = SongInfoControl.SongInfo.MaxTracks - 1; i >= 0; i--)
            {
                if (enabledTracks[i])
                {
                    SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                    for (int nk = 0; nk < SongInfoControl.SongInfo.MaxKeys; nk++)
                    {
                        byte k = tin.Keys[nk];
                        if (k == byte.MaxValue)
                        {
                            break;
                        }
                        else
                        {
                            PianoKey key = Keys[k];
                            key.OnBrush.Color = GlobalConfig.Instance.Colors[tin.Voice];
                            key.Pressed = key.Dirty = true;
                        }
                    }
                }
            }
            for (int k = 0; k <= 0x7F; k++)
            {
                PianoKey key = Keys[k];
                if (key.Dirty)
                {
                    key.Invalidate();
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            SetKeySizes();
            base.OnResize(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int k = 0; k < 0x80; k++)
                {
                    Keys[k].Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
