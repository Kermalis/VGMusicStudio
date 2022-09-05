using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Properties;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

internal static class Theme
{
	public static readonly Font Font = new("Segoe UI", 8f, FontStyle.Bold);
	public static readonly Color
		BackColor = Color.FromArgb(33, 33, 39),
		BackColorDisabled = Color.FromArgb(35, 42, 47),
		BackColorMouseOver = Color.FromArgb(32, 37, 47),
		BorderColor = Color.FromArgb(25, 120, 186),
		BorderColorDisabled = Color.FromArgb(47, 55, 60),
		ForeColor = Color.FromArgb(94, 159, 230),
		PlayerColor = Color.FromArgb(8, 8, 8),
		SelectionColor = Color.FromArgb(7, 51, 141),
		TitleBar = Color.FromArgb(16, 40, 63);

	public static Color DrainColor(Color c)
	{
		var hsl = new HSLColor(c);
		return HSLColor.ToColor(hsl.Hue, hsl.Saturation / 2.5, hsl.Lightness);
	}
}

internal sealed class ThemedButton : Button
{
	public ThemedButton()
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
		if (!Enabled)
		{
			TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Theme.DrainColor(ForeColor), BackColor);
		}
	}
	protected override bool ShowFocusCues => false;
}
internal sealed class ThemedLabel : Label
{
	public ThemedLabel()
	{
		Font = Theme.Font;
		ForeColor = Theme.ForeColor;
	}
}
internal class ThemedForm : Form
{
	public ThemedForm()
	{
		BackColor = Theme.BackColor;
		Icon = Resources.Icon;
	}
}
internal class ThemedPanel : Panel
{
	public ThemedPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
	}
	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		using (var b = new SolidBrush(BackColor))
		{
			e.Graphics.FillRectangle(b, e.ClipRectangle);
		}
		using (var b = new SolidBrush(Theme.BorderColor))
		using (var p = new Pen(b, 2))
		{
			e.Graphics.DrawRectangle(p, e.ClipRectangle);
		}
	}
	private const int WM_PAINT = 0xF;
	protected override void WndProc(ref Message m)
	{
		if (m.Msg == WM_PAINT)
		{
			Invalidate();
		}
		base.WndProc(ref m);
	}
}
internal class ThemedTextBox : TextBox
{
	public ThemedTextBox()
	{
		BackColor = Theme.BackColor;
		Font = Theme.Font;
		ForeColor = Theme.ForeColor;
	}
	[DllImport("user32.dll")]
	private static extern IntPtr GetWindowDC(IntPtr hWnd);
	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
	[DllImport("user32.dll")]
	private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);
	private const int WM_NCPAINT = 0x85;
	private const uint RDW_INVALIDATE = 0x1;
	private const uint RDW_IUPDATENOW = 0x100;
	private const uint RDW_FRAME = 0x400;
	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		if (m.Msg == WM_NCPAINT && BorderStyle == BorderStyle.Fixed3D)
		{
			IntPtr hdc = GetWindowDC(Handle);
			using (var g = Graphics.FromHdcInternal(hdc))
			using (var p = new Pen(Theme.BorderColor))
			{
				g.DrawRectangle(p, new Rectangle(0, 0, Width - 1, Height - 1));
			}
			ReleaseDC(Handle, hdc);
		}
	}
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
	}
}
internal sealed class ThemedRichTextBox : RichTextBox
{
	public ThemedRichTextBox()
	{
		BackColor = Theme.BackColor;
		Font = Theme.Font;
		ForeColor = Theme.ForeColor;
		SelectionColor = Theme.SelectionColor;
	}
}
internal sealed class ThemedNumeric : NumericUpDown
{
	public ThemedNumeric()
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
