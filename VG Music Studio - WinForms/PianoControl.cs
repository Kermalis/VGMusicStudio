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
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
internal sealed class PianoControl : Control
{
	private enum KeyType : byte
	{
		Black,
		White,
	}

	private const double BLACK_KEY_SCALE = 2.0 / 3;
	public const int WHITE_KEY_COUNT = 75;

	private static readonly KeyType[] KeyTypeTable = new KeyType[12]
	{
		KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White,
		KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White, KeyType.Black, KeyType.White,
	};

	public sealed class PianoKey : Control
	{
		public bool PrevPressed;
		public bool Pressed;

		public readonly SolidBrush OnBrush;
		private readonly SolidBrush _offBrush;

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
			e.Graphics.FillRectangle(Pressed ? OnBrush : _offBrush, 1, 1, Width - 2, Height - 2);
			e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
			base.OnPaint(e);
		}
	}

	private readonly PianoKey[] _keys;
	public int WhiteKeyWidth;

	public PianoControl()
	{
		SetStyle(ControlStyles.Selectable, false);

		_keys = new PianoKey[0x80];
		for (byte k = 0; k <= 0x7F; k++)
		{
			var key = new PianoKey(k);
			_keys[k] = key;
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
		WhiteKeyWidth = Width / WHITE_KEY_COUNT;
		int blackKeyWidth = (int)(WhiteKeyWidth * BLACK_KEY_SCALE);
		int blackKeyHeight = (int)(Height * BLACK_KEY_SCALE);
		int offset = WhiteKeyWidth - (blackKeyWidth / 2);
		int w = 0;
		for (int k = 0; k <= 0x7F; k++)
		{
			PianoKey key = _keys[k];
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

	public void UpdateKeys(SongState.Track[] tracks, bool[] enabledTracks)
	{
		for (int k = 0; k <= 0x7F; k++)
		{
			PianoKey key = _keys[k];
			key.PrevPressed = key.Pressed;
			key.Pressed = false;
		}
		for (int i = SongState.MAX_TRACKS - 1; i >= 0; i--)
		{
			if (!enabledTracks[i])
			{
				continue;
			}

			SongState.Track track = tracks[i];
			for (int nk = 0; nk < SongState.MAX_KEYS; nk++)
			{
				byte k = track.Keys[nk];
				if (k == byte.MaxValue)
				{
					break;
				}

				PianoKey key = _keys[k];
				key.OnBrush.Color = GlobalConfig.Instance.Colors[track.Voice];
				key.Pressed = true;
			}
		}
		for (int k = 0; k <= 0x7F; k++)
		{
			PianoKey key = _keys[k];
			if (key.Pressed != key.PrevPressed)
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
				_keys[k].Dispose();
			}
		}
		base.Dispose(disposing);
	}
}
