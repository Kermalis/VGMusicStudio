using Gtk;
using Kermalis.VGMusicStudio.Core.Util;
using Cairo;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System;
using Pango;
using Window = Gtk.Window;
using Context = Cairo.Context;

namespace Kermalis.VGMusicStudio.GTK4;

/// <summary>
/// LibAdwaita theme selection enumerations.
/// </summary>
public enum ThemeType
{
    Light = 0,  // Light Theme
    Dark,       // Dark Theme
    System      // System Default Theme
}

internal class Theme
{

    public Theme ThemeType { get; set; }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct Color
    //{
    //    public float Red;
    //    public float Green;
    //    public float Blue;
    //    public float Alpha;
    //}

    //[DllImport("libadwaita-1.so.0")]
    //[return: MarshalAs(UnmanagedType.I1)]
    //private static extern bool gdk_rgba_parse(ref Color rgba, string spec);

    //[DllImport("libadwaita-1.so.0")]
    //private static extern string gdk_rgba_to_string(ref Color rgba);

    //[DllImport("libadwaita-1.so.0")]
    //private static extern void gtk_color_chooser_get_rgba(nint chooser, ref Color rgba);

    //[DllImport("libadwaita-1.so.0")]
    //private static extern void gtk_color_chooser_set_rgba(nint chooser, ref Color rgba);

    //public static Color FromArgb(int r, int g, int b)
    //{
    //    Color color = new Color();
    //    r = (int)color.Red;
    //    g = (int)color.Green;
    //    b = (int)color.Blue;

    //    return color;
    //}

    //public static readonly Font Font = new("Segoe UI", 8f, FontStyle.Bold);
    //public static readonly Color
    //    BackColor = Color.FromArgb(33, 33, 39),
    //    BackColorDisabled = Color.FromArgb(35, 42, 47),
    //    BackColorMouseOver = Color.FromArgb(32, 37, 47),
    //    BorderColor = Color.FromArgb(25, 120, 186),
    //    BorderColorDisabled = Color.FromArgb(47, 55, 60),
    //    ForeColor = Color.FromArgb(94, 159, 230),
    //    PlayerColor = Color.FromArgb(8, 8, 8),
    //    SelectionColor = Color.FromArgb(7, 51, 141),
    //    TitleBar = Color.FromArgb(16, 40, 63);



    //public static Color DrainColor(Color c)
    //{
    //    var hsl = new HSLColor(c);
    //    return HSLColor.ToColor(hsl.H, (byte)(hsl.S / 2.5), hsl.L);
    //}
}

internal sealed class ThemedButton : Button
{
    public ResponseType ResponseType;
    public ThemedButton()
    {
        //FlatAppearance.MouseOverBackColor = Theme.BackColorMouseOver;
        //FlatStyle = FlatStyle.Flat;
        //Font = Theme.FontType;
        //ForeColor = Theme.ForeColor;
    }
    protected void OnEnabledChanged(EventArgs e)
    {
        //base.OnEnabledChanged(e);
        //BackColor = Enabled ? Theme.BackColor : Theme.BackColorDisabled;
        //FlatAppearance.BorderColor = Enabled ? Theme.BorderColor : Theme.BorderColorDisabled;
    }
    protected void OnDraw(Context c)
    {
        //base.OnPaint(e);
        //if (!Enabled)
        //{
        //    TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Theme.DrainColor(ForeColor), BackColor);
        //}
    }
    //protected override bool ShowFocusCues => false;
}
internal sealed class ThemedLabel : Label
{
    public ThemedLabel()
    {
        //Font = Theme.Font;
        //ForeColor = Theme.ForeColor;
    }
}
internal class ThemedWindow : Window
{
    public ThemedWindow()
    {
        //BackColor = Theme.BackColor;
        //Icon = Resources.Icon;
    }
}
internal class ThemedBox : Box
{
    public ThemedBox()
    {
        //SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }
    protected void OnDraw(Context c)
    {
        //base.OnPaint(e);
        //using (var b = new SolidBrush(BackColor))
        //{
        //    e.Graphics.FillRectangle(b, e.ClipRectangle);
        //}
        //using (var b = new SolidBrush(Theme.BorderColor))
        //using (var p = new Pen(b, 2))
        //{
        //    e.Graphics.DrawRectangle(p, e.ClipRectangle);
        //}
    }
    private const int WM_PAINT = 0xF;
    //protected void WndProc(ref Message m)
    //{
    //    if (m.Msg == WM_PAINT)
    //    {
    //        Invalidate();
    //    }
    //    base.WndProc(ref m);
    //}
}
internal class ThemedTextBox : Adw.Window
{
    public Box Box;
    public Text Text;
    public ThemedTextBox()
    {
        //BackColor = Theme.BackColor;
        //Font = Theme.Font;
        //ForeColor = Theme.ForeColor;
        Box = Box.New(Orientation.Horizontal, 0);
        Text = Text.New();
        Box.Append(Text);
    }
    //[DllImport("user32.dll")]
    //private static extern IntPtr GetWindowDC(IntPtr hWnd);
    //[DllImport("user32.dll")]
    //private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    //[DllImport("user32.dll")]
    //private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);
    //private const int WM_NCPAINT = 0x85;
    //private const uint RDW_INVALIDATE = 0x1;
    //private const uint RDW_IUPDATENOW = 0x100;
    //private const uint RDW_FRAME = 0x400;
    //protected override void WndProc(ref Message m)
    //{
    //    base.WndProc(ref m);
    //    if (m.Msg == WM_NCPAINT && BorderStyle == BorderStyle.Fixed3D)
    //    {
    //        IntPtr hdc = GetWindowDC(Handle);
    //        using (var g = Graphics.FromHdcInternal(hdc))
    //        using (var p = new Pen(Theme.BorderColor))
    //        {
    //            g.DrawRectangle(p, new Rectangle(0, 0, Width - 1, Height - 1));
    //        }
    //        ReleaseDC(Handle, hdc);
    //    }
    //}
    protected void OnSizeChanged(EventArgs e)
    {
        //base.OnSizeChanged(e);
        //RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
    }
}
internal sealed class ThemedRichTextBox : Adw.Window
{
    public Box Box;
    public Text Text;
    public ThemedRichTextBox()
    {
        //BackColor = Theme.BackColor;
        //Font = Theme.Font;
        //ForeColor = Theme.ForeColor;
        //SelectionColor = Theme.SelectionColor;
        Box = Box.New(Orientation.Horizontal, 0);
        Text = Text.New();
        Box.Append(Text);
    }
}
internal sealed class ThemedNumeric : SpinButton
{
    public ThemedNumeric()
    {
        //BackColor = Theme.BackColor;
        //Font = new Font(Theme.Font.FontFamily, 7.5f, Theme.Font.Style);
        //ForeColor = Theme.ForeColor;
        //TextAlign = HorizontalAlignment.Center;
    }
    protected void OnDraw(Context c)
    {
        //base.OnPaint(e);
        //ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Enabled ? Theme.BorderColor : Theme.BorderColorDisabled, ButtonBorderStyle.Solid);
    }
}