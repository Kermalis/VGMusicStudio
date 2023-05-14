using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

partial class PianoControl
{
	private sealed class PianoKey : Control
	{
		public bool PrevIsHeld;
		public bool IsHeld;

		public readonly SolidBrush OnBrush;
		private readonly SolidBrush _offBrush;

		private readonly string? _cName;

		public PianoKey(byte k)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.Selectable, false);

			OnBrush = new(Color.Transparent);
			byte c;
			if (KeyTypeTable[k % 12] == KeyType.White)
			{
				if (k / 12 % 2 == 0)
				{
					c = 255;
				}
				else
				{
					c = 127;
				}
			}
			else
			{
				c = 0;
			}
			_offBrush = new SolidBrush(Color.FromArgb(c, c, c));

			if (k % 12 == 0)
			{
				_cName = ConfigUtils.GetKeyName(k);
				Font = new Font(Font.FontFamily, GetFontSize());
			}
		}

		private float GetFontSize()
		{
			return Math.Max(1, Width / 2.75f);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				OnBrush.Dispose();
				_offBrush.Dispose();
			}
			base.Dispose(disposing);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(IsHeld ? OnBrush : _offBrush, 1, 1, Width - 2, Height - 2);
			e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

			if (_cName is not null)
			{
				SizeF strSize = e.Graphics.MeasureString(_cName, Font);
				float x = (Width - strSize.Width) / 2f;
				float y = Height - strSize.Height - 2;
				e.Graphics.DrawString(_cName, Font, Brushes.Black, new RectangleF(x, y, 0, 0));
			}

			base.OnPaint(e);
		}
		protected override void OnResize(EventArgs e)
		{
			Font = new Font(Font.FontFamily, GetFontSize());
			base.OnResize(e);
		}
	}
}
