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

using Kermalis.MusicStudio.Util;
using Sanford.Multimedia.Midi;
using System;
using System.Collections;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Kermalis.MusicStudio.UI
{
    public partial class PianoControl : Control
    {
        public class PianoKey : Control
        {
            PianoControl owner;
            public bool Pressed { get; private set; } = false;

            SolidBrush onBrush = new SolidBrush(Color.SkyBlue);
            SolidBrush offBrush;
            public Color NoteOnColor
            {
                get
                {
                    return onBrush.Color;
                }
                set
                {
                    onBrush.Color = value;
                    if (Pressed)
                    {
                        Invalidate();
                    }
                }
            }
            public Color NoteOffColor
            {
                get
                {
                    return offBrush.Color;
                }
                set
                {
                    offBrush.Color = value;
                    if (!Pressed)
                    {
                        Invalidate();
                    }
                }
            }

            int noteID = 60;
            public int NoteID
            {
                get
                {
                    return noteID;
                }
                set
                {
                    if (value < 0 || value > ShortMessage.DataMaxValue)
                    {
                        throw new ArgumentOutOfRangeException("NoteID", noteID, "Note ID out of range.");
                    }
                    noteID = value;
                }
            }

            public PianoKey(PianoControl piano, int noteId)
            {
                owner = piano;
                TabStop = false;
                NoteID = noteId;
                offBrush = new SolidBrush(new HSLColor(160.0, 0.0, KeyTypeTable[noteID % 12] == KeyType.White ? noteID / 12 % 2 == 0 ? 240.0 : 120.0 : 0.0));
            }

            public void PressPianoKey()
            {
                if (Pressed)
                {
                    return;
                }

                Pressed = true;
                Invalidate();
                owner.OnPianoKeyDown(new PianoKeyEventArgs(noteID));
            }
            public void ReleasePianoKey()
            {
                if (!Pressed)
                {
                    return;
                }

                Pressed = false;
                Invalidate();
                owner.OnPianoKeyUp(new PianoKeyEventArgs(noteID));
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                if (MouseButtons == MouseButtons.Left)
                {
                    PressPianoKey();
                }
                base.OnMouseEnter(e);
            }
            protected override void OnMouseLeave(EventArgs e)
            {
                if (Pressed)
                {
                    ReleasePianoKey();
                }
                base.OnMouseLeave(e);
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                PressPianoKey();
                if (!owner.Focused)
                {
                    owner.Focus();
                }
                base.OnMouseDown(e);
            }
            protected override void OnMouseUp(MouseEventArgs e)
            {
                ReleasePianoKey();
                base.OnMouseUp(e);
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (e.X < 0 || e.X > Width || e.Y < 0 || e.Y > Height)
                {
                    Capture = false;
                }
                base.OnMouseMove(e);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    onBrush.Dispose();
                    offBrush.Dispose();
                }
                base.Dispose(disposing);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.FillRectangle(Pressed ? onBrush : offBrush, 0, 0, Size.Width, Size.Height);
                e.Graphics.DrawRectangle(Pens.Black, 0, 0, Size.Width - 1, Size.Height - 1);
                base.OnPaint(e);
            }
        }

        enum KeyType
        {
            White,
            Black
        }
        static readonly Hashtable keyTable = new Hashtable();
        static readonly KeyType[] KeyTypeTable = { KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White, KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White };
        const double BlackKeyScale = 2 / 3D;
        PianoKey[] keys = null;

        delegate void NoteMessageCallback(ChannelMessage message);

        int lowNoteID = 19;
        int highNoteID = 100;
        public int LowNoteID
        {
            get
            {
                return lowNoteID;
            }
            set
            {
                if (value < 0 || value > ShortMessage.DataMaxValue)
                {
                    throw new ArgumentOutOfRangeException("LowNoteID", value, "Low note ID out of range.");
                }
                if (value == lowNoteID)
                {
                    return;
                }

                lowNoteID = value;
                if (lowNoteID > highNoteID)
                {
                    highNoteID = lowNoteID;
                }

                CreatePianoKeys();
                InitializePianoKeys();
            }
        }
        public int HighNoteID
        {
            get
            {
                return highNoteID;
            }
            set
            {
                if (value < 0 || value > ShortMessage.DataMaxValue)
                {
                    throw new ArgumentOutOfRangeException("HighNoteID", value, "High note ID out of range.");
                }
                if (value == highNoteID)
                {
                    return;
                }

                highNoteID = value;
                if (highNoteID < lowNoteID)
                {
                    lowNoteID = highNoteID;
                }

                CreatePianoKeys();
                InitializePianoKeys();
            }
        }

        public int WhiteKeyCount { get; private set; }
        int octaveOffset = 5;

        Color noteOnColor = Color.DeepSkyBlue;
        public Color NoteOnColor
        {
            get
            {
                return noteOnColor;
            }
            set
            {
                if (value == noteOnColor)
                {
                    return;
                }

                noteOnColor = value;

                foreach (PianoKey key in keys)
                {
                    key.NoteOnColor = noteOnColor;
                }
            }
        }

        NoteMessageCallback noteOnCallback;
        NoteMessageCallback noteOffCallback;

        public event EventHandler<PianoKeyEventArgs> PianoKeyDown;
        public event EventHandler<PianoKeyEventArgs> PianoKeyUp;

        SynchronizationContext context;

        static PianoControl()
        {
            keyTable.Add(Keys.A, 0);
            keyTable.Add(Keys.W, 1);
            keyTable.Add(Keys.S, 2);
            keyTable.Add(Keys.E, 3);
            keyTable.Add(Keys.D, 4);
            keyTable.Add(Keys.F, 5);
            keyTable.Add(Keys.T, 6);
            keyTable.Add(Keys.G, 7);
            keyTable.Add(Keys.Y, 8);
            keyTable.Add(Keys.H, 9);
            keyTable.Add(Keys.U, 10);
            keyTable.Add(Keys.J, 11);
            keyTable.Add(Keys.K, 12);
            keyTable.Add(Keys.O, 13);
            keyTable.Add(Keys.L, 14);
            keyTable.Add(Keys.P, 15);
        }
        public PianoControl()
        {
            CreatePianoKeys();
            InitializePianoKeys();

            context = SynchronizationContext.Current;

            noteOnCallback = delegate (ChannelMessage message)
            {
                if (message.Data2 > 0)
                {
                    keys[message.Data1 - lowNoteID].PressPianoKey();
                }
                else
                {
                    keys[message.Data1 - lowNoteID].ReleasePianoKey();
                }
            };
            noteOffCallback = delegate (ChannelMessage message)
            {
                keys[message.Data1 - lowNoteID].ReleasePianoKey();
            };
        }

        void CreatePianoKeys()
        {
            // If piano keys have already been created.
            if (keys != null)
            {
                // Remove and dispose of current piano keys.
                foreach (PianoKey key in keys)
                {
                    Controls.Remove(key);
                    key.Dispose();
                }
            }

            keys = new PianoKey[HighNoteID - LowNoteID + 1];

            WhiteKeyCount = 0;

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new PianoKey(this, i + LowNoteID);

                if (KeyTypeTable[keys[i].NoteID % 12] == KeyType.White)
                {
                    WhiteKeyCount++;
                }
                else
                {
                    keys[i].BringToFront();
                }

                keys[i].NoteOnColor = NoteOnColor;
                Controls.Add(keys[i]);
            }
        }
        void InitializePianoKeys()
        {
            if (keys.Length == 0)
            {
                return;
            }

            int whiteKeyWidth = Width / WhiteKeyCount;
            int blackKeyWidth = (int)(whiteKeyWidth * BlackKeyScale);
            int blackKeyHeight = (int)(Height * BlackKeyScale);
            int offset = whiteKeyWidth - blackKeyWidth / 2;
            int n = 0;
            int w = 0;

            while (n < keys.Length)
            {
                if (KeyTypeTable[keys[n].NoteID % 12] == KeyType.White)
                {
                    keys[n].Height = Height;
                    keys[n].Width = whiteKeyWidth;
                    keys[n].Location = new Point(w * whiteKeyWidth, 0);
                    w++;
                    n++;
                }
                else
                {
                    keys[n].Height = blackKeyHeight;
                    keys[n].Width = blackKeyWidth;
                    keys[n].Location = new Point(offset + (w - 1) * whiteKeyWidth);
                    keys[n].BringToFront();
                    n++;
                }
            }
        }

        public PianoKey this[int i]
        {
            get => keys[i];
        }

        public void Send(ChannelMessage message)
        {
            if (message.Command == ChannelCommand.NoteOn &&
                message.Data1 >= LowNoteID && message.Data1 <= HighNoteID)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(noteOnCallback, message);
                }
                else
                {
                    noteOnCallback(message);
                }
            }
            else if (message.Command == ChannelCommand.NoteOff &&
                message.Data1 >= LowNoteID && message.Data1 <= HighNoteID)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(noteOffCallback, message);
                }
                else
                {
                    noteOffCallback(message);
                }
            }
        }
        public void PressPianoKey(int noteID)
        {
            if (noteID < lowNoteID || noteID > highNoteID)
            {
                throw new ArgumentOutOfRangeException();
            }
            keys[noteID - lowNoteID].PressPianoKey();
        }
        public void ReleasePianoKey(int noteID)
        {
            if (noteID < lowNoteID || noteID > highNoteID)
            {
                throw new ArgumentOutOfRangeException();
            }
            keys[noteID - lowNoteID].ReleasePianoKey();
        }
        public void PressPianoKey(Keys k)
        {
            if (!Focused)
            {
                return;
            }

            if (keyTable.Contains(k))
            {
                int noteID = (int)keyTable[k] + 12 * octaveOffset;
                if (noteID >= LowNoteID && noteID <= HighNoteID)
                {
                    if (!keys[noteID - lowNoteID].Pressed)
                    {
                        keys[noteID - lowNoteID].PressPianoKey();
                    }
                }
            }
            else
            {
                switch (k)
                {
                    case Keys.D0: octaveOffset = 0; break;
                    case Keys.D1: octaveOffset = 1; break;
                    case Keys.D2: octaveOffset = 2; break;
                    case Keys.D3: octaveOffset = 3; break;
                    case Keys.D4: octaveOffset = 4; break;
                    case Keys.D5: octaveOffset = 5; break;
                    case Keys.D6: octaveOffset = 6; break;
                    case Keys.D7: octaveOffset = 7; break;
                    case Keys.D8: octaveOffset = 8; break;
                    case Keys.D9: octaveOffset = 9; break;
                }
            }
        }
        public void ReleasePianoKey(Keys k)
        {
            if (!keyTable.Contains(k))
            {
                return;
            }

            int noteID = (int)keyTable[k] + 12 * octaveOffset;
            if (noteID >= LowNoteID && noteID <= HighNoteID)
            {
                keys[noteID - lowNoteID].ReleasePianoKey();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            InitializePianoKeys();
            base.OnResize(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (PianoKey key in keys)
                {
                    key.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        protected virtual void OnPianoKeyDown(PianoKeyEventArgs e)
        {
            PianoKeyDown?.Invoke(this, e);
        }
        protected virtual void OnPianoKeyUp(PianoKeyEventArgs e)
        {
            PianoKeyUp?.Invoke(this, e);
        }
    }

    public class PianoKeyEventArgs : EventArgs
    {
        public int NoteID { get; }

        public PianoKeyEventArgs(int noteID)
        {
            NoteID = noteID;
        }
    }
}
