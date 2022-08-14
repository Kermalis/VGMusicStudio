#region License

/* Copyright (c) 2017 Fabrice Lacharme
 * This code is inspired from Michal Brylka 
 * https://www.codeproject.com/Articles/17395/Owner-drawn-trackbar-slider
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


using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    [DesignerCategory(""), ToolboxBitmap(typeof(TrackBar))]
    internal class ColorSlider : Control
    {
        private const int thumbSize = 14;
        private Rectangle thumbRect;

        private long _value = 0L;
        public long Value
        {
            get => _value;
            set
            {
                if (value >= _minimum && value <= _maximum)
                {
                    _value = value;
                    ValueChanged?.Invoke(this, new EventArgs());
                    Invalidate();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(Value), $"{nameof(Value)} must be between {nameof(Minimum)} and {nameof(Maximum)}.");
                }
            }
        }
        private long _minimum = 0L;
        public long Minimum
        {
            get => _minimum;
            set
            {
                if (value <= _maximum)
                {
                    _minimum = value;
                    if (_value < _minimum)
                    {
                        _value = _minimum;
                        ValueChanged?.Invoke(this, new EventArgs());
                    }
                    Invalidate();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(Minimum), $"{nameof(Minimum)} cannot be higher than {nameof(Maximum)}.");
                }
            }
        }
        private long _maximum = 10L;
        public long Maximum
        {
            get => _maximum;
            set
            {
                if (value >= _minimum)
                {
                    _maximum = value;
                    if (_value > _maximum)
                    {
                        _value = _maximum;
                        ValueChanged?.Invoke(this, new EventArgs());
                    }
                    Invalidate();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(Maximum), $"{nameof(Maximum)} cannot be lower than {nameof(Minimum)}.");
                }
            }
        }
        private long _smallChange = 1L;
        public long SmallChange
        {
            get => _smallChange;
            set
            {
                if (value >= 0)
                {
                    _smallChange = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(SmallChange), $"{nameof(SmallChange)} must be greater than or equal to 0.");
                }
            }
        }
        private long _largeChange = 5L;
        public long LargeChange
        {
            get => _largeChange;
            set
            {
                if (value >= 0)
                {
                    _largeChange = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(LargeChange), $"{nameof(LargeChange)} must be greater than or equal to 0.");
                }
            }
        }
        private bool _acceptKeys = true;
        public bool AcceptKeys
        {
            get => _acceptKeys;
            set
            {
                _acceptKeys = value;
                SetStyle(ControlStyles.Selectable, value);
            }
        }

        public event EventHandler ValueChanged;

        private readonly Color _thumbOuterColor = Color.White;
        private readonly Color _thumbInnerColor = Color.White;
        private readonly Color _thumbPenColor = Color.FromArgb(125, 125, 125);
        private readonly Color _barInnerColor = Theme.BackColorMouseOver;
        private readonly Color _elapsedPenColorTop = Theme.ForeColor;
        private readonly Color _elapsedPenColorBottom = Theme.ForeColor;
        private readonly Color _barPenColorTop = Color.FromArgb(85, 90, 104);
        private readonly Color _barPenColorBottom = Color.FromArgb(117, 124, 140);
        private readonly Color _elapsedInnerColor = Theme.BorderColor;
        private readonly Color _tickColor = Color.White;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pen.Dispose();
            }
            base.Dispose(disposing);
        }
        public ColorSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.UserMouse |
                     ControlStyles.UserPaint, true);
            Size = new Size(200, 48);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Enabled)
            {
                Color[] c = DesaturateColors(_thumbOuterColor, _thumbInnerColor, _thumbPenColor,
                    _barInnerColor,
                    _elapsedPenColorTop, _elapsedPenColorBottom,
                    _barPenColorTop, _barPenColorBottom,
                    _elapsedInnerColor);
                Draw(e,
                    c[0], c[1], c[2],
                    c[3],
                    c[4], c[5],
                    c[6], c[7],
                    c[8]);
            }
            else
            {
                if (mouseInRegion)
                {
                    Color[] c = LightenColors(_thumbOuterColor, _thumbInnerColor, _thumbPenColor,
                        _barInnerColor,
                        _elapsedPenColorTop, _elapsedPenColorBottom,
                        _barPenColorTop, _barPenColorBottom,
                        _elapsedInnerColor);
                    Draw(e,
                        c[0], c[1], c[2],
                        c[3],
                        c[4], c[5],
                        c[6], c[7],
                        c[8]);
                }
                else
                {
                    Draw(e,
                        _thumbOuterColor, _thumbInnerColor, _thumbPenColor,
                        _barInnerColor,
                        _elapsedPenColorTop, _elapsedPenColorBottom,
                        _barPenColorTop, _barPenColorBottom,
                        _elapsedInnerColor);
                }
            }
        }
        private readonly Pen pen = new Pen(Color.Transparent);
        private void Draw(PaintEventArgs e,
            Color thumbOuterColorPaint, Color thumbInnerColorPaint, Color thumbPenColorPaint,
            Color barInnerColorPaint,
            Color elapsedTopPenColorPaint, Color elapsedBottomPenColorPaint,
            Color barTopPenColorPaint, Color barBottomPenColorPaint,
            Color elapsedInnerColorPaint)
        {
            if (Focused)
            {
                ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.FromArgb(50, elapsedTopPenColorPaint), ButtonBorderStyle.Dashed);
            }

            long a = _maximum - _minimum;
            long x = a == 0 ? 0 : (_value - _minimum) * (ClientRectangle.Width - thumbSize) / a;
            thumbRect = new Rectangle((int)x, ClientRectangle.Y + (ClientRectangle.Height / 2) - (thumbSize / 2), thumbSize, thumbSize);
            Rectangle barRect = ClientRectangle;
            barRect.Inflate(-1, -barRect.Height / 3);
            Rectangle elapsedRect = barRect;
            elapsedRect.Width = thumbRect.Left + (thumbSize / 2);

            pen.Color = barInnerColorPaint;
            e.Graphics.DrawLine(pen, barRect.X, barRect.Y + (barRect.Height / 2), barRect.X + barRect.Width, barRect.Y + (barRect.Height / 2));
            pen.Color = elapsedInnerColorPaint;
            e.Graphics.DrawLine(pen, barRect.X, barRect.Y + (barRect.Height / 2), barRect.X + elapsedRect.Width, barRect.Y + (barRect.Height / 2));
            pen.Color = elapsedTopPenColorPaint;
            e.Graphics.DrawLine(pen, barRect.X, barRect.Y - 1 + (barRect.Height / 2), barRect.X + elapsedRect.Width, barRect.Y - 1 + (barRect.Height / 2));
            pen.Color = elapsedBottomPenColorPaint;
            e.Graphics.DrawLine(pen, barRect.X, barRect.Y + 1 + (barRect.Height / 2), barRect.X + elapsedRect.Width, barRect.Y + 1 + (barRect.Height / 2));
            pen.Color = barTopPenColorPaint;
            e.Graphics.DrawLine(pen, barRect.X + elapsedRect.Width, barRect.Y - 1 + (barRect.Height / 2), barRect.X + barRect.Width, barRect.Y - 1 + (barRect.Height / 2));
            pen.Color = barBottomPenColorPaint;
            e.Graphics.DrawLine(pen, barRect.X + elapsedRect.Width, barRect.Y + 1 + (barRect.Height / 2), barRect.X + barRect.Width, barRect.Y + 1 + (barRect.Height / 2));
            pen.Color = barTopPenColorPaint;
            e.Graphics.DrawLine(pen, barRect.X, barRect.Y - 1 + (barRect.Height / 2), barRect.X, barRect.Y + (barRect.Height / 2) + 1);
            pen.Color = barBottomPenColorPaint;
            e.Graphics.DrawLine(pen, barRect.X + barRect.Width, barRect.Y - 1 + (barRect.Height / 2), barRect.X + barRect.Width, barRect.Y + 1 + (barRect.Height / 2));

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color newthumbOuterColorPaint = thumbOuterColorPaint,
                    newthumbInnerColorPaint = thumbInnerColorPaint;
            if (busyMouse)
            {
                newthumbOuterColorPaint = Color.FromArgb(175, thumbOuterColorPaint);
                newthumbInnerColorPaint = Color.FromArgb(175, thumbInnerColorPaint);
            }
            using (GraphicsPath thumbPath = CreateRoundRectPath(thumbRect, thumbSize))
            {
                using (var lgbThumb = new LinearGradientBrush(thumbRect, newthumbOuterColorPaint, newthumbInnerColorPaint, LinearGradientMode.Vertical) { WrapMode = WrapMode.TileFlipXY })
                {
                    e.Graphics.FillPath(lgbThumb, thumbPath);
                }
                Color newThumbPenColor = thumbPenColorPaint;
                if (busyMouse || mouseInThumbRegion)
                {
                    newThumbPenColor = ControlPaint.Dark(newThumbPenColor);
                }
                pen.Color = newThumbPenColor;
                e.Graphics.DrawPath(pen, thumbPath);
            }

            const int numTicks = 1 + (10 * (5 + 1));
            int interval = 0;
            int start = thumbRect.Width / 2;
            int w = barRect.Width - thumbRect.Width;
            int idx = 0;
            pen.Color = _tickColor;
            for (int i = 0; i <= 10; i++)
            {
                e.Graphics.DrawLine(pen, start + barRect.X + interval, ClientRectangle.Y + ClientRectangle.Height, start + barRect.X + interval, ClientRectangle.Y + ClientRectangle.Height - 5);
                if (i < 10)
                {
                    for (int j = 0; j <= 5; j++)
                    {
                        idx++;
                        interval = idx * w / (numTicks - 1);
                    }
                }
            }
        }

        private bool mouseInRegion = false;
        private bool mouseInThumbRegion = false;
        private bool busyMouse = false;
        private void SetValueFromPoint(Point p)
        {
            int x = p.X;
            int margin = thumbSize / 2;
            x -= margin;
            _value = (long)((x * ((_maximum - _minimum) / (ClientSize.Width - (2f * margin)))) + _minimum);
            if (_value < _minimum)
            {
                _value = _minimum;
            }
            else if (_value > _maximum)
            {
                _value = _maximum;
            }
            ValueChanged?.Invoke(this, new EventArgs());
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            mouseInRegion = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseInRegion = false;
            mouseInThumbRegion = false;
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mouseInThumbRegion = IsPointInRect(e.Location, thumbRect);
            busyMouse = (MouseButtons & MouseButtons.Left) != MouseButtons.None;
            if (busyMouse)
            {
                SetValueFromPoint(e.Location);
            }
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseInThumbRegion = IsPointInRect(e.Location, thumbRect);
            if (busyMouse)
            {
                SetValueFromPoint(e.Location);
            }
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            mouseInThumbRegion = IsPointInRect(e.Location, thumbRect);
            bool old = busyMouse;
            busyMouse = old && e.Button == MouseButtons.Left ? false : old;
            Invalidate();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_acceptKeys && !busyMouse)
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                    case Keys.Left:
                    {
                        long newVal = _value - _smallChange;
                        if (newVal < _minimum)
                        {
                            newVal = _minimum;
                        }
                        Value = newVal;
                        break;
                    }
                    case Keys.Up:
                    case Keys.Right:
                    {
                        long newVal = _value + _smallChange;
                        if (newVal > _maximum)
                        {
                            newVal = _maximum;
                        }
                        Value = newVal;
                        break;
                    }
                    case Keys.Home:
                    {
                        Value = _minimum;
                        break;
                    }
                    case Keys.End:
                    {
                        Value = _maximum;
                        break;
                    }
                    case Keys.PageDown:
                    {
                        long newVal = _value - _largeChange;
                        if (newVal < _minimum)
                        {
                            newVal = _minimum;
                        }
                        Value = newVal;
                        break;
                    }
                    case Keys.PageUp:
                    {
                        long newVal = _value + _largeChange;
                        if (newVal > _maximum)
                        {
                            newVal = _maximum;
                        }
                        Value = newVal;
                        break;
                    }
                }
            }
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            return !_acceptKeys || keyData == Keys.Tab || ModifierKeys == Keys.Shift ? base.ProcessDialogKey(keyData) : false;
        }

        private static GraphicsPath CreateRoundRectPath(Rectangle rect, int size)
        {
            var gp = new GraphicsPath();
            gp.AddLine(rect.Left + (size / 2), rect.Top, rect.Right - (size / 2), rect.Top);
            gp.AddArc(rect.Right - size, rect.Top, size, size, 270, 90);

            gp.AddLine(rect.Right, rect.Top + (size / 2), rect.Right, rect.Bottom - (size / 2));
            gp.AddArc(rect.Right - size, rect.Bottom - size, size, size, 0, 90);

            gp.AddLine(rect.Right - (size / 2), rect.Bottom, rect.Left + (size / 2), rect.Bottom);
            gp.AddArc(rect.Left, rect.Bottom - size, size, size, 90, 90);

            gp.AddLine(rect.Left, rect.Bottom - (size / 2), rect.Left, rect.Top + (size / 2));
            gp.AddArc(rect.Left, rect.Top, size, size, 180, 90);
            return gp;
        }
        private static Color[] DesaturateColors(params Color[] colors)
        {
            var ret = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                int gray = (int)((colors[i].R * 0.3) + (colors[i].G * 0.6) + (colors[i].B * 0.1));
                ret[i] = Color.FromArgb((-0x010101 * (255 - gray)) - 1);
            }
            return ret;
        }
        private static Color[] LightenColors(params Color[] colors)
        {
            var ret = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                ret[i] = ControlPaint.Light(colors[i]);
            }
            return ret;
        }
        private static bool IsPointInRect(Point p, Rectangle rect)
        {
            return p.X > rect.Left & p.X < rect.Right & p.Y > rect.Top & p.Y < rect.Bottom;
        }
    }
}
