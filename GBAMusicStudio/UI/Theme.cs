using BrightIdeasSoftware;
using GBAMusicStudio.Properties;
using GBAMusicStudio.Util;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    internal static class Theme
    {
        internal static readonly Font Font = new Font("Segoe UI", 8f, FontStyle.Bold);
        internal static readonly Color
            BackColor = Color.FromArgb(33, 33, 39),
            BackColorDisabled = Color.FromArgb(35, 42, 47),
            BackColorMouseOver = Color.FromArgb(32, 37, 47),
            BorderColor = Color.FromArgb(25, 120, 186),
            BorderColorDisabled = Color.FromArgb(47, 55, 60),
            ForeColor = Color.FromArgb(94, 159, 230),
            PlayerColor = Color.FromArgb(8, 8, 8),
            SelectionColor = Color.FromArgb(7, 51, 141),
            TitleBar = Color.FromArgb(16, 40, 63);

        internal static HSLColor DrainColor(Color c)
        {
            var drained = new HSLColor(c);
            drained.Saturation /= 2.5;
            return drained;
        }
    }

    internal class ThemedButton : Button
    {
        public ThemedButton() : base()
        {
            FlatAppearance.MouseOverBackColor = Theme.BackColorMouseOver;
            FlatStyle = FlatStyle.Flat;
            Font = Theme.Font;
            ForeColor = Theme.ForeColor;
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            BackColor = Enabled ? Theme.BackColor : Theme.BackColorDisabled;
            FlatAppearance.BorderColor = Enabled ? Theme.BorderColor : Theme.BorderColorDisabled;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Enabled) TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Theme.DrainColor(ForeColor), BackColor);
        }
        protected override bool ShowFocusCues => false;
    }
    internal class ThemedLabel : Label
    {
        public ThemedLabel() : base()
        {
            Font = Theme.Font;
            ForeColor = Theme.ForeColor;
        }
    }
    internal class ThemedForm : Form
    {
        public ThemedForm() : base()
        {
            BackColor = Theme.BackColor;
            Icon = Resources.Icon;
        }
    }
    internal class ThemedPanel : Panel
    {
        public ThemedPanel() : base()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var b = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(b, e.ClipRectangle);
            using (var b = new SolidBrush(Theme.BorderColor))
            using (var p = new Pen(b, 2))
                e.Graphics.DrawRectangle(p, e.ClipRectangle);
        }
    }
    internal class ThemedTextBox : TextBox
    {
        public ThemedTextBox() : base()
        {
            BackColor = Theme.BackColor;
            Font = Theme.Font;
            ForeColor = Theme.ForeColor;
        }
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);
        const int WM_NCPAINT = 0x85;
        const uint RDW_INVALIDATE = 0x1;
        const uint RDW_IUPDATENOW = 0x100;
        const uint RDW_FRAME = 0x400;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCPAINT && BorderStyle == BorderStyle.Fixed3D)
            {
                var hdc = GetWindowDC(Handle);
                using (var g = Graphics.FromHdcInternal(hdc))
                using (var p = new Pen(Theme.BorderColor))
                    g.DrawRectangle(p, new Rectangle(0, 0, Width - 1, Height - 1));
                ReleaseDC(Handle, hdc);
            }
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
        }
    }
    internal class ThemedRichTextBox : RichTextBox
    {
        public ThemedRichTextBox() : base()
        {
            BackColor = Theme.BackColor;
            Font = Theme.Font;
            ForeColor = Theme.ForeColor;
            SelectionColor = Theme.SelectionColor;
        }
    }
    internal class ThemedNumeric : NumericUpDown
    {
        public ThemedNumeric() : base()
        {
            BackColor = Theme.BackColor;
            Font = new Font(Theme.Font.FontFamily, 7.5f, Theme.Font.Style);
            ForeColor = Theme.ForeColor;
            TextAlign = HorizontalAlignment.Center;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Enabled ? Theme.BorderColor : Theme.BorderColorDisabled, ButtonBorderStyle.Solid);
        }
    }
    /*internal class ThemedComboBox : ComboBox
    {
        public ThemedComboBox() : base()
        {
            BackColor = Theme.BackColor;
            Font = Theme.Font;
            DrawMode = DrawMode.OwnerDrawFixed;
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(SystemColors.Highlight), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(Theme.BackColor), e.Bounds);
            }

            e.Graphics.DrawString(Items[e.Index].ToString(),
                                          e.Font,
                                          new SolidBrush(Color.Black),
                                          new Point(e.Bounds.X, e.Bounds.Y));
        }
    }*/
}
