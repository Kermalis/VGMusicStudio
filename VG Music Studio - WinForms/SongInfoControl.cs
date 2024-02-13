using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
internal sealed class SongInfoControl : Control
{
	private const int CHECKBOX_SIZE = 15;

	private readonly CheckBox[] _mutes;
	private readonly CheckBox[] _pianos;
	private readonly SolidBrush _solidBrush;
	private readonly Pen _pen;

	public readonly SongState Info;
	private int _numTracksToDraw;

	private readonly StringBuilder _keysCache;

	private float _infoHeight, _infoY, _positionX, _keysX, _delayX, _typeEndX, _typeX, _voicesX, _row2ElementAdditionX, _yMargin, _trackHeight, _row2Offset, _tempoX;
	private int _barHeight, _barStartX, _barWidth, _bwd, _barRightBoundX, _barCenterX;

	public SongInfoControl()
	{
		_keysCache = new StringBuilder();
		_solidBrush = new(Theme.PlayerColor);
		_pen = new(Color.Transparent);

		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.Selectable, false);
		Font = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
		Size = new Size(675, 675);

		_pianos = new CheckBox[SongState.MAX_TRACKS + 1];
		_mutes = new CheckBox[SongState.MAX_TRACKS + 1];
		for (int i = 0; i < SongState.MAX_TRACKS + 1; i++)
		{
			_pianos[i] = new CheckBox
			{
				BackColor = Color.Transparent,
				Checked = true,
				Size = new Size(CHECKBOX_SIZE, CHECKBOX_SIZE),
				TabStop = false
			};
			_pianos[i].CheckStateChanged += TogglePiano;
			_mutes[i] = new CheckBox
			{
				BackColor = Color.Transparent,
				Checked = true,
				Size = new Size(CHECKBOX_SIZE, CHECKBOX_SIZE),
				TabStop = false
			};
			_mutes[i].CheckStateChanged += ToggleMute;
		}
		Controls.AddRange(_pianos);
		Controls.AddRange(_mutes);

		Info = new SongState();
		SetNumTracks(0);
	}

	private void TogglePiano(object? sender, EventArgs e)
	{
		var check = (CheckBox)sender!;
		CheckBox master = _pianos[SongState.MAX_TRACKS];
		if (check == master)
		{
			bool b = check.CheckState != CheckState.Unchecked;
			for (int i = 0; i < SongState.MAX_TRACKS; i++)
			{
				_pianos[i].Checked = b;
			}
		}
		else
		{
			int numChecked = 0;
			for (int i = 0; i < SongState.MAX_TRACKS; i++)
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
			master.CheckState = numChecked == SongState.MAX_TRACKS ? CheckState.Checked : (numChecked == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
			master.CheckStateChanged += TogglePiano;
		}
	}
	private void ToggleMute(object? sender, EventArgs e)
	{
		var check = (CheckBox)sender!;
		CheckBox master = _mutes[SongState.MAX_TRACKS];
		if (check == master)
		{
			bool b = check.CheckState != CheckState.Unchecked;
			for (int i = 0; i < SongState.MAX_TRACKS; i++)
			{
				_mutes[i].Checked = b;
			}
		}
		else
		{
			int numChecked = 0;
			for (int i = 0; i < SongState.MAX_TRACKS; i++)
			{
				if (_mutes[i] == check)
				{
					Engine.Instance!.Mixer.Mutes[i] = !check.Checked;
				}
				if (_mutes[i].Checked)
				{
					numChecked++;
				}
			}
			master.CheckStateChanged -= ToggleMute;
			master.CheckState = numChecked == SongState.MAX_TRACKS ? CheckState.Checked : (numChecked == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
			master.CheckStateChanged += ToggleMute;
		}
	}

	public void Reset()
	{
		Info.Reset();
		Invalidate();
	}
	public void SetNumTracks(int num)
	{
		_numTracksToDraw = num;
		bool visible = num > 0;
		_pianos[SongState.MAX_TRACKS].Enabled = visible;
		_mutes[SongState.MAX_TRACKS].Enabled = visible;
		for (int i = 0; i < SongState.MAX_TRACKS; i++)
		{
			visible = i < num;
			_pianos[i].Visible = visible;
			_mutes[i].Visible = visible;
		}
		OnResize(EventArgs.Empty);
	}
	public void ResetMutes()
	{
		for (int i = 0; i < SongState.MAX_TRACKS + 1; i++)
		{
			CheckBox mute = _mutes[i];
			mute.CheckStateChanged -= ToggleMute;
			mute.CheckState = CheckState.Checked;
			mute.CheckStateChanged += ToggleMute;
		}
	}

	protected override void OnResize(EventArgs e)
	{
		if (_mutes is null)
		{
			return; // This can run before init is finished
		}

		_infoHeight = Height / 30f;
		_infoY = _infoHeight - (TextRenderer.MeasureText("A", Font).Height * 1.125f);
		_positionX = (CHECKBOX_SIZE * 2) + 2;
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

		int x1 = 3;
		int x2 = CHECKBOX_SIZE + 4;
		int y = (int)_infoY + 3;
		_mutes[SongState.MAX_TRACKS].Location = new Point(x1, y);
		_pianos[SongState.MAX_TRACKS].Location = new Point(x2, y);
		for (int i = 0; i < _numTracksToDraw; i++)
		{
			float r1y = _infoHeight + _yMargin + (i * _trackHeight);
			y = (int)r1y + 4;
			_mutes[i].Location = new Point(x1, y);
			_pianos[i].Location = new Point(x2, y);
		}

		base.OnResize(e);
	}

	// TODO: This stuff shouldn't be calculated every frame (multiple ToString(), the colors, etc).
	// It should be calculated after being retrieved from the player
	#region Drawing

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;

		_solidBrush.Color = Theme.PlayerColor;
		g.FillRectangle(_solidBrush, e.ClipRectangle);

		DrawTopRow(g);

		for (int i = 0; i < _numTracksToDraw; i++)
		{
			SongState.Track track = Info.Tracks[i];

			// Set color
			Color color = GlobalConfig.Instance.Colors[track.Voice];
			_solidBrush.Color = color;
			_pen.Color = color;

			float row1Y = _infoHeight + _yMargin + (i * _trackHeight);
			float row2Y = row1Y + _row2Offset;

			DrawLeftInfo(g, track, row1Y, row2Y);

			int vBarY1 = (int)(row1Y + _yMargin);
			int vBarY2 = vBarY1 + _barHeight;

			// The "Type" string has a special place alone on the right and resizes
			g.DrawString(track.Type, Font, Brushes.DeepPink, _typeEndX - g.MeasureString(track.Type, Font).Width, vBarY1 + (_row2Offset / (Font.Size / 2.5f)));

			DrawVerticalBars(g, track, vBarY1, vBarY2, color);

			DrawHeldKeys(g, track, row1Y);
		}
		base.OnPaint(e);
	}

	private void DrawTopRow(Graphics g)
	{
		g.DrawString(Strings.PlayerPosition, Font, Brushes.Lime, _positionX, _infoY); // Position
		g.DrawString(Strings.PlayerRest, Font, Brushes.Crimson, _delayX, _infoY); // Rest
		g.DrawString(Strings.PlayerNotes, Font, Brushes.Turquoise, _keysX, _infoY); // Notes
		g.DrawString("L", Font, Brushes.GreenYellow, _barStartX - 5, _infoY); // L
		g.DrawString(string.Format("{0} - {1}", Strings.PlayerTempo, Info.Tempo), Font, Brushes.Cyan, _tempoX, _infoY); // Tempo
		g.DrawString("R", Font, Brushes.GreenYellow, _barRightBoundX - 5, _infoY); // R
		g.DrawString(Strings.PlayerType, Font, Brushes.DeepPink, _typeX, _infoY); // Type

		g.DrawLine(Pens.Gold, 0, _infoHeight, Width, _infoHeight);
	}
	private void DrawLeftInfo(Graphics g, SongState.Track track, float row1Y, float row2Y)
	{
		g.DrawString(string.Format("0x{0:X}", track.Position), Font, Brushes.Lime, _positionX, row1Y);
		g.DrawString(track.Rest.ToString(), Font, Brushes.Crimson, _delayX, row1Y);

		g.DrawString(track.Voice.ToString(), Font, _solidBrush, _voicesX, row2Y);
		g.DrawString(track.Panpot.ToString(), Font, Brushes.OrangeRed, _voicesX + _row2ElementAdditionX, row2Y);
		g.DrawString(track.Volume.ToString(), Font, Brushes.LightSeaGreen, _voicesX + (_row2ElementAdditionX * 2), row2Y);
		g.DrawString(track.LFO.ToString(), Font, Brushes.SkyBlue, _voicesX + (_row2ElementAdditionX * 3), row2Y);
		g.DrawString(track.PitchBend.ToString(), Font, Brushes.Purple, _voicesX + (_row2ElementAdditionX * 4), row2Y);
		g.DrawString(track.Extra.ToString(), Font, Brushes.HotPink, _voicesX + (_row2ElementAdditionX * 5), row2Y);
	}
	private void DrawVerticalBars(Graphics g, SongState.Track track, int vBarY1, int vBarY2, in Color color)
	{
		g.DrawLine(Pens.GreenYellow, _barStartX, vBarY1, _barStartX, vBarY2); // Left bounds
		g.DrawLine(Pens.GreenYellow, _barRightBoundX, vBarY1, _barRightBoundX, vBarY2); // Right bounds

		// Draw pan bar before velocity bar
		if (GlobalConfig.Instance.PanpotIndicators)
		{
			int panBarX = (int)(_barStartX + (_barWidth / 2) + (_barWidth / 2 * (track.Panpot / (float)0x40)));
			g.DrawLine(Pens.OrangeRed, panBarX, vBarY1, panBarX, vBarY2);
		}

		// Try to draw velocity bar
		var rect = new Rectangle((int)(_barStartX + (_barWidth / 2) - (track.LeftVolume * _barWidth / 2)) + _bwd,
			vBarY1,
			(int)((track.LeftVolume + track.RightVolume) * _barWidth / 2),
			_barHeight);
		if (rect.Width > 0)
		{
			float velocity = track.LeftVolume + track.RightVolume;
			int alpha;
			if (velocity >= 2f)
			{
				alpha = 255;
			}
			else
			{
				const int DELTA = 125;
				alpha = (int)WinFormsUtils.Lerp(velocity * 0.5f, 0f, DELTA);
				alpha += 255 - DELTA;
			}
			_solidBrush.Color = Color.FromArgb(alpha, color);
			g.FillRectangle(_solidBrush, rect);
			g.DrawRectangle(_pen, rect);
			//_solidBrush.Color = color;
		}

		// Draw center bar last
		if (GlobalConfig.Instance.CenterIndicators)
		{
			g.DrawLine(_pen, _barCenterX, vBarY1, _barCenterX, vBarY2);
		}
	}
	private void DrawHeldKeys(Graphics g, SongState.Track track, float row1Y)
	{
		string keys;
		if (track.Keys[0] == byte.MaxValue)
		{
			if (track.PreviousKeysTime != 0)
			{
				track.PreviousKeysTime--;
				keys = track.PreviousKeys;
			}
			else
			{
				keys = string.Empty;
			}
		}
		else // Keys are held down
		{
			_keysCache.Clear();
			for (int nk = 0; nk < SongState.MAX_KEYS; nk++)
			{
				byte k = track.Keys[nk];
				if (k == byte.MaxValue)
				{
					break;
				}

				string noteName = ConfigUtils.GetKeyName(k);
				if (nk != 0)
				{
					_keysCache.Append(' ' + noteName);
				}
				else
				{
					_keysCache.Append(noteName);
				}
			}
			keys = _keysCache.ToString();

			track.PreviousKeysTime = GlobalConfig.Instance.RefreshRate << 2;
			track.PreviousKeys = keys;
		}
		if (keys.Length != 0)
		{
			g.DrawString(keys, Font, Brushes.Turquoise, _keysX, row1Y);
		}
	}

	#endregion

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_solidBrush.Dispose();
			_pen.Dispose();
		}
		base.Dispose(disposing);
	}
}
