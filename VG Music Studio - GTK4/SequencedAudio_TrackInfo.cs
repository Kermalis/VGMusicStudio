using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cairo;
using Gtk;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;

namespace Kermalis.VGMusicStudio.GTK4;

[GObject.Subclass<Box>]
internal partial class SequencedAudio_TrackInfo
{
    private Label? _tempoLabel;
    private ushort _baseTempo;
    private Label? _baseTempoLabel;
    private SpinButton _tempoSpinButton;

    private CheckButton? _trackToggleCheckButtonHeader;
    private Label? _velocityHeader;

    private CheckButton[]? _trackToggleCheckButton;
    private Label[]? _labelPosition;
    private Label[]? _labelRest;
    private Label[]? _labelVoice;
    private Label[]? _labelNotes;
    private Label[]? _labelPanpot;
    private Label[]? _labelVolume;
    private Label[]? _labelLFO;
    private Label[]? _labelPitchBend;
    private Label[]? _labelExtra;
    private VelocityBar[]? _velocity;
    private Label[]? _labelType;

    private ListBox? _listBox;
    private static List<string> _keysCache = new(128);

    public bool[]? NumTracks;
    public SongState? Info;
    internal static int NumTracksToDraw;

    internal SequencedAudio_TrackInfo[]? TrackInfo { get; set; }

    partial void Initialize()
    {
        NumTracks = new bool[SongState.MAX_TRACKS];
        for (int i = 0; i < SongState.MAX_TRACKS; i++)
        {
            NumTracks[i] = true;
        }

        Info = new SongState();
        
        _tempoLabel = Label.New(string.Format("{0} - {1}", Strings.PlayerTempo, Info!.Tempo));
        _tempoSpinButton = SpinButton.New(Adjustment.New(0, 0, 10000, 1, 10, 0), 0, 0);
        _tempoSpinButton.SetNumeric(true);
        _tempoSpinButton.OnValueChanged += ChangeTempo;
        _tempoSpinButton.OnChangeValue += ChangeTempo;
        _baseTempo = Info.Tempo;
        _baseTempoLabel = Label.New(string.Format("{0} + ", _baseTempo));
        var tempoBox = New(Orientation.Vertical, 4);
        var tempoControlBox = New(Orientation.Horizontal, 4);
        tempoControlBox.Append(_baseTempoLabel);
        tempoControlBox.Append(_tempoSpinButton);
        tempoControlBox.SetHalign(Align.Center);
        tempoBox.Append(_tempoLabel);
        tempoBox.Append(tempoControlBox);
        var listHeader = CreateListHeader();
        var viewport = Viewport.New(Adjustment.New(0, double.MinValue, double.MaxValue, 1, 1, 1), Adjustment.New(0, double.MinValue, double.MaxValue, 1, 1, 1));
        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetSizeRequest(150, 150);
        scrolledWindow.SetHexpand(true);
        scrolledWindow.SetVexpand(true);

        _listBox = ListBox.New();
        _listBox.SetHexpand(true);
        _listBox.SetSelectionMode(SelectionMode.None);

        scrolledWindow.SetChild(_listBox);

        viewport.Child = scrolledWindow;

        SetOrientation(Orientation.Vertical);
        Append(tempoBox);
        Append(listHeader);
        Append(viewport);
        SetVexpand(true);
        SetHexpand(true);
        SetNumTracks(0);
        ConfigureTimer();
    }

    private void ChangeTempo(SpinButton sender, EventArgs args)
    {
        if (Engine.Instance is not null)
        {
            Engine.Instance.Player.Tempo = (ushort)(_baseTempo! + _tempoSpinButton.Value);
        }
    }
    
    internal void ResetTempo()
    {
        _tempoSpinButton.Value = 0;
    }

    internal static void SetNumTracks(int num) => NumTracksToDraw = num;

    private void ConfigureTimer()
    {
        var timer = GLib.Timer.New(); // Creates a new timer variable
        var context = GLib.MainContext.GetThreadDefault(); // Reads the main context default thread
        var source = GLib.Functions.TimeoutSourceNew(1); // Creates and configures the timeout interval at 1 microsecond, so it updates in real time
        source.SetCallback(TrackTimerCallback); // Sets the callback for the timer interval to be used on
        var microsec = new CULong(source.Attach(context)); // Configures the microseconds based on attaching the GLib MainContext thread
        // timer.Elapsed(ref microsec); // Adds the pointer to the configured microseconds source
        GLib.Internal.Timer.Elapsed(timer.Handle, ref microsec); // GLib.Timer.Elapsed was removed in GirCore 0.6.3, so we're using this workaround instead
        timer.Start(); // Starts the timer
    }

    private bool TrackTimerCallback()
    {
        if (Engine.Instance is not null)
        {
            _tempoLabel!.SetLabel(string.Format("{0} - {1}", Strings.PlayerTempo, Engine.Instance!.Player.Tempo));
        }
        if (_trackToggleCheckButton is not null &&
            _labelPosition is not null &&
            _labelRest is not null &&
            _labelVoice is not null &&
            _labelNotes is not null &&
            _labelPanpot is not null &&
            _labelVolume is not null &&
            _labelLFO is not null &&
            _labelPitchBend is not null &&
            _labelExtra is not null &&
            _velocity is not null &&
            _labelType is not null)
        {
            if (_labelPosition.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < NumTracksToDraw; i++)
            {
                if (_trackToggleCheckButton[i] is not null &&
                    _labelPosition[i] is not null &&
                    _labelRest[i] is not null &&
                    _labelVoice[i] is not null &&
                    _labelNotes[i] is not null &&
                    _labelPanpot[i] is not null &&
                    _labelVolume[i] is not null &&
                    _labelLFO[i] is not null &&
                    _labelPitchBend[i] is not null &&
                    _labelExtra[i] is not null &&
                    _velocity[i] is not null &&
                    _labelType[i] is not null)
                {
                    if (Engine.Instance!.Player.State is not PlayerState.Stopped)
                    {
                        ToggleTrack(i, _trackToggleCheckButton[i].Active);
                        _labelPosition[i].SetText(string.Format("0x{0:X}", Info!.Tracks[i].Position));
                        _labelRest[i].SetText(Info.Tracks[i].Rest.ToString());
                        _labelVoice[i].SetText(Info.Tracks[i].Voice.ToString());
                        _labelNotes[i].SetText(GetNote(Info.Tracks[i]));
                        _labelPanpot[i].SetText(Info.Tracks[i].Panpot.ToString());
                        _labelVolume[i].SetText(Info.Tracks[i].Volume.ToString());
                        _labelLFO[i].SetText(Info.Tracks[i].LFO.ToString());
                        _labelPitchBend[i].SetText(Info.Tracks[i].PitchBend.ToString());
                        _labelExtra[i].SetText(Info.Tracks[i].Extra.ToString());
                        _velocityHeader!.WidthRequest = GetWidth() / 4;
                        _velocity[i].WidthRequest = GetWidth() / 4;
                        _velocity[i].UpdateColor(Info.Tracks[i], _trackToggleCheckButton[i].Active);
                        _velocity[i].QueueDraw();
                        if (Info.Tracks[i].Type is not null)
                        {
                            _labelType[i].SetText(Info.Tracks[i].Type);
                        }
                    }
                    else
                    {
                        ToggleTrack(i, _trackToggleCheckButton[i].Active);
                        _labelPosition[i].SetText(string.Format("0x{0:X}", 0));
                        _labelRest[i].SetText(0.ToString());
                        _labelVoice[i].SetText(0.ToString());
                        _labelNotes[i].SetText("");
                        _labelPanpot[i].SetText(0.ToString());
                        _labelVolume[i].SetText(0.ToString());
                        _labelLFO[i].SetText(0.ToString());
                        _labelPitchBend[i].SetText(0.ToString());
                        _labelExtra[i].SetText(Info!.Tracks[i].Extra.ToString());
                        _velocityHeader!.WidthRequest = GetWidth() / 4;
                        _velocity[i].WidthRequest = GetWidth() / 4;
                        _velocity[i].UpdateColor(Info.Tracks[i], _trackToggleCheckButton[i].Active);
                        _velocity[i].QueueDraw();
                        if (Info.Tracks[i].Type is not null)
                        {
                            _labelType[i].SetText("");
                        }
                    }
                }
            }
        }
        return true;
    }

    [GObject.Subclass<DrawingArea>]
    internal partial class VelocityBar
    {
        private SongState.Track? _track;
        private HSLColor _color;
        private HSLColor _overampColor;
        partial void Initialize()
        {
            _color = new HSLColor();
            _overampColor = new HSLColor(0, 1, 0.5);
            SetHexpand(true);
            SetVexpand(true);
            SetDrawFunc(DrawVelocityBar);
        }

        internal void UpdateColor(SongState.Track track, bool trackEnabled)
        {
            _track = track;
            if (GlobalConfig.Instance is not null) // Nullability check
            {
                _color = new HSLColor(GlobalConfig.Instance.Colors[track.Voice]);
                if (!trackEnabled)
                {
                    _color = new HSLColor(_color.Hue, 0, _color.Lightness);
                }
            }
            _overampColor = new HSLColor(0, 1, 0.5);
            if (!trackEnabled)
            {
                _overampColor = new HSLColor(_overampColor.Hue, 0, 0.8);
            }
        }

        private void DrawVelocityBar(DrawingArea drawingArea, Context cr, int width, int height)
        {
            // cr.LineWidth = 3;

            DrawLineL(cr, width, height);
            DrawLineR(cr, width, height);

            cr.Save();

            cr.SetSourceRgb(0.5, 0.5, 0.5);

            cr.Rectangle(width / 2.0, 0, 5 * (width / 599f), height);
            cr.Fill();
            cr.Restore();
            // DrawRounded(cr, (width / 2.0) - 20, 150);

            cr.Restore();
        }

        private void DrawLineL(Context cr, int width, int height)
        {
            DrawTrough(cr, height, width / 2, -(width / 3));
            if (_track is not null)
            {
                DrawVolumeLine(cr, height, width / 2, -(_track.LeftVolume * ((width / 3) + (width / 9))));
            }
            DrawText(cr, height, (width / 2) - (width / 3) - 8, (width / 2) - (width / 3) - (width / 9) - 7.5, "-1.0", "L");
            DrawOverampLine(cr, height, (width / 2) - (width / 3), -(width / 9));
        }

        private void DrawLineR(Context cr, int width, int height)
        {
            DrawTrough(cr, height, (width / 2) + (5 * (width / 599f)), width / 3);
            if (_track is not null)
            {
                DrawVolumeLine(cr, height, (width / 2) + (5 * (width / 599f)), _track.LeftVolume * ((width / 3) + (width / 9)));
            }
            DrawText(cr, height, (width / 2) + (5 * (width / 599f)) + (width / 3) - 8, width / 1.045, "+1.0", "R");
            DrawOverampLine(cr, height, (width / 2) + (5 * (width / 599f)) + (width / 3), width / 9);
        }

        private void DrawTrough(Context cr, int height, float pos, double length)
        {
            cr.Save();
            cr.SetSourceRgba(0.5, 0.5, 0.5, 0.5);
            cr.LineWidth = 5;
            cr.MoveTo(pos, height / 2);
            cr.LineTo(pos + length, height / 2);
            cr.Stroke();
            cr.Restore();
        }

        private void DrawVolumeLine(Context cr, int height, float pos, float length)
        {
            cr.Save();
            cr.SetSourceRgb(_color.R, _color.G, _color.B);
            cr.LineWidth = 5;
            cr.MoveTo(pos, height / 2);
            cr.LineTo(pos + length, height / 2);
            cr.Stroke();
            cr.Restore();
        }

        private void DrawText(Context cr, double height, double posVolLabel, double posChLabel, string volumeLabel, string channelLabel)
        {
            cr.Save();
            cr.SetSourceRgb(0.5, 0.5, 0.5);
            cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(7.0);
            cr.MoveTo(posVolLabel, height);
            cr.ShowText(volumeLabel);
            cr.Restore();

            cr.Save();
            cr.SetSourceRgb(0.5, 0.5, 0.5);
            cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(8.0);
            cr.MoveTo(posChLabel, height / 1.5);
            cr.ShowText(channelLabel);
            cr.Restore();
        }

        private void DrawOverampLine(Context cr, int height, float pos, int length)
        {
            cr.Save();
            cr.SetSourceRgba(_overampColor.R, _overampColor.G, _overampColor.B, 0.5);
            cr.LineWidth = 5;
            cr.MoveTo(pos, height / 2);
            cr.LineTo(pos + length, height / 2);
            cr.Stroke();
            cr.ClosePath();
            cr.Restore();
        }
    }

    private static string GetNote(SongState.Track track)
    {
        string key = "";
        if (track.Keys[0] == byte.MaxValue)
        {
            if (track.PreviousKeysTime != 0)
            {
                track.PreviousKeysTime--;
                key = track.PreviousKeys;
            }
            else
            {
                key = string.Empty;
            }
        }
        else // Keys are held down
        {
            _keysCache.Clear();
            string noteName = "";
            for (int nk = 0; nk < SongState.MAX_KEYS; nk++)
            {
                byte k = track.Keys[nk];
                if (k == byte.MaxValue)
                {
                    break;
                }

                noteName = ConfigUtils.GetKeyName(k);
                if (nk != 0)
                {
                    _keysCache.Add(' ' + noteName);
                }
                else
                {
                    _keysCache.Add(noteName);
                }
            }
            foreach (var k in _keysCache)
            {
                if (k == noteName)
                {
                    key = k;
                }
            }

            track.PreviousKeysTime = 120;
            track.PreviousKeys = key;
        }
        return key;
    }
    private Box CreateListHeader()
    {
        var columns = New(Orientation.Horizontal, 4);
        columns.SetHexpand(true);

        _trackToggleCheckButtonHeader = CheckButton.New();
        _trackToggleCheckButtonHeader.Active = true;
        _trackToggleCheckButtonHeader.OnToggled += ToggleAllTracks;
        _trackToggleCheckButtonHeader.SetMarginStart(2); // So that the starting margin is aligned with the check buttons in the list box
        _trackToggleCheckButtonHeader.SetHalign(Align.Start);
        columns.Append(_trackToggleCheckButtonHeader);

        var positionLabelHeader = Label.New(Strings.PlayerPosition);
        positionLabelHeader.SetMaxWidthChars(1);
        positionLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        positionLabelHeader.SetHalign(Align.Start);
        positionLabelHeader.WidthRequest = 100;
        positionLabelHeader.SetHexpand(false);
        columns.Append(positionLabelHeader);

        var restLabelHeader = Label.New(Strings.PlayerRest);
        restLabelHeader.SetMaxWidthChars(1);
        restLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        restLabelHeader.WidthRequest = 30;
        restLabelHeader.SetHexpand(true);
        columns.Append(restLabelHeader);

        var voiceLabelHeader = Label.New("Voice");
        voiceLabelHeader.SetMaxWidthChars(1);
        voiceLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        voiceLabelHeader.WidthRequest = 30;
        voiceLabelHeader.SetHexpand(true);
        columns.Append(voiceLabelHeader);

        var notesLabelHeader = Label.New(Strings.PlayerNotes);
        notesLabelHeader.SetMaxWidthChars(1);
        notesLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        notesLabelHeader.WidthRequest = 30;
        notesLabelHeader.SetHexpand(true);
        columns.Append(notesLabelHeader);

        var panpotLabelHeader = Label.New("Panpot");
        panpotLabelHeader.SetMaxWidthChars(1);
        panpotLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        panpotLabelHeader.WidthRequest = 30;
        panpotLabelHeader.SetHexpand(true);
        columns.Append(panpotLabelHeader);

        var volumeLabelHeader = Label.New("Volume");
        volumeLabelHeader.SetMaxWidthChars(1);
        volumeLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        volumeLabelHeader.WidthRequest = 30;
        volumeLabelHeader.SetHexpand(true);
        columns.Append(volumeLabelHeader);

        var lfoLabelHeader = Label.New("LFO");
        lfoLabelHeader.SetMaxWidthChars(1);
        lfoLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        lfoLabelHeader.WidthRequest = 30;
        lfoLabelHeader.SetHexpand(true);
        columns.Append(lfoLabelHeader);

        var pitchBendLabelHeader = Label.New("Pitch Bend");
        pitchBendLabelHeader.SetMaxWidthChars(1);
        pitchBendLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        pitchBendLabelHeader.WidthRequest = 30;
        pitchBendLabelHeader.SetHexpand(true);
        columns.Append(pitchBendLabelHeader);

        var extraLabelHeader = Label.New("Extra");
        extraLabelHeader.SetMaxWidthChars(1);
        extraLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        extraLabelHeader.WidthRequest = 30;
        extraLabelHeader.SetHexpand(true);
        columns.Append(extraLabelHeader);

        _velocityHeader = Label.New("");
        _velocityHeader.SetMaxWidthChars(1);
        _velocityHeader.WidthRequest = GetWidth() / 4;
        _velocityHeader.SetHexpand(true);
        columns.Append(_velocityHeader);

        var typeLabelHeader = Label.New(Strings.PlayerType);
        typeLabelHeader.SetMaxWidthChars(1);
        typeLabelHeader.SetEllipsize(Pango.EllipsizeMode.End);
        typeLabelHeader.WidthRequest = 30;
        typeLabelHeader.SetHexpand(true);
        columns.Append(typeLabelHeader);
        return columns;
    }

    private void ToggleTrack(int index, bool active)
    {
        if (active)
        {
            Engine.Instance!.Mixer!.Mutes[index] = false;
            NumTracks![index] = true;
        }
        else
        {
            Engine.Instance!.Mixer!.Mutes[index] = true;
            NumTracks![index] = false;
        }

        var numActive = 0;
        for (int i = 0; i < NumTracksToDraw; i++)
        {
            if (NumTracks[i])
            {
                numActive++;
            }
        }
        if (numActive == NumTracksToDraw)
        {
            _trackToggleCheckButtonHeader!.Inconsistent = false;
            _trackToggleCheckButtonHeader.Active = true;
        }
        else if (numActive < NumTracksToDraw && numActive is not 0)
        {
            _trackToggleCheckButtonHeader!.Inconsistent = true;
        }
        else
        {
            _trackToggleCheckButtonHeader!.Inconsistent = false;
            _trackToggleCheckButtonHeader.Active = false;
        }
    }

    private void ToggleAllTracks(CheckButton sender, EventArgs args)
    {
        if (sender.Active)
        {
            for (int i = 0; i < NumTracksToDraw; i++)
            {
                Engine.Instance!.Mixer!.Mutes[i] = false;
                NumTracks![i] = _trackToggleCheckButton![i].Active = true;
            }
        }
        else
        {
            for (int i = 0; i < NumTracksToDraw; i++)
            {
                Engine.Instance!.Mixer!.Mutes[i] = true;
                NumTracks![i] = _trackToggleCheckButton![i].Active = false;
            }
        }
    }

    public void AddTrackInfo()
    {
        _listBox!.RemoveAll();
        for (int i = 0; i < NumTracks!.Length; i++)
        {
            if (i < NumTracksToDraw)
            {
                NumTracks[i] = true;
            }
            else
            {
                NumTracks[i] = false;
            }
        }

        _tempoSpinButton.Value = 0;
        _baseTempo = Engine.Instance!.Player.Tempo;
        _baseTempoLabel!.SetLabel(string.Format("{0} + ", _baseTempo));
        _tempoSpinButton.SetRange(-_baseTempo, short.MaxValue);

        _trackToggleCheckButton = new CheckButton[NumTracksToDraw];
        _labelPosition = new Label[NumTracksToDraw];
        _labelRest = new Label[NumTracksToDraw];
        _labelVoice = new Label[NumTracksToDraw];
        _labelNotes = new Label[NumTracksToDraw];
        _labelPanpot = new Label[NumTracksToDraw];
        _labelVolume = new Label[NumTracksToDraw];
        _labelLFO = new Label[NumTracksToDraw];
        _labelPitchBend = new Label[NumTracksToDraw];
        _labelExtra = new Label[NumTracksToDraw];
        _velocity = new VelocityBar[NumTracksToDraw];
        _labelType = new Label[NumTracksToDraw];
        for (int i = 0; i < NumTracksToDraw; i++)
        {
            var columns = New(Orientation.Horizontal, 4);
            columns.SetHexpand(true);

            _trackToggleCheckButton[i] = CheckButton.New();
            _trackToggleCheckButton[i].Active = true;
            _trackToggleCheckButton[i].SetHalign(Align.Start);
            columns.Append(_trackToggleCheckButton[i]);

            _labelPosition[i] = Label.New(string.Format("0x{0:X}", Info!.Tracks[i].Position));
            _labelPosition[i].SetEllipsize(Pango.EllipsizeMode.Start);
            _labelPosition[i].SetMaxWidthChars(1);
            _labelPosition[i].SetHalign(Align.Start);
            _labelPosition[i].WidthRequest = 100;
            _labelPosition[i].SetHexpand(false);
            columns.Append(_labelPosition[i]);

            _labelRest[i] = Label.New(Info.Tracks[i].Rest.ToString());
            _labelRest[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelRest[i].SetMaxWidthChars(1);
            _labelRest[i].WidthRequest = 30;
            _labelRest[i].SetHexpand(true);
            columns.Append(_labelRest[i]);

            _labelVoice[i] = Label.New(Info.Tracks[i].Voice.ToString());
            _labelVoice[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelVoice[i].SetMaxWidthChars(1);
            _labelVoice[i].WidthRequest = 30;
            _labelVoice[i].SetHexpand(true);
            columns.Append(_labelVoice[i]);

            _labelNotes[i] = Label.New("");
            _labelNotes[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelNotes[i].SetMaxWidthChars(1);
            _labelNotes[i].WidthRequest = 30;
            _labelNotes[i].SetHexpand(true);
            columns.Append(_labelNotes[i]);

            _labelPanpot[i] = Label.New(Info.Tracks[i].Panpot.ToString());
            _labelPanpot[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelPanpot[i].SetMaxWidthChars(1);
            _labelPanpot[i].WidthRequest = 30;
            _labelPanpot[i].SetHexpand(true);
            columns.Append(_labelPanpot[i]);

            _labelVolume[i] = Label.New(Info.Tracks[i].Volume.ToString());
            _labelVolume[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelVolume[i].SetMaxWidthChars(1);
            _labelVolume[i].WidthRequest = 30;
            _labelVolume[i].SetHexpand(true);
            columns.Append(_labelVolume[i]);

            _labelLFO[i] = Label.New(Info.Tracks[i].LFO.ToString());
            _labelLFO[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelLFO[i].SetMaxWidthChars(1);
            _labelLFO[i].WidthRequest = 30;
            _labelLFO[i].SetHexpand(true);
            columns.Append(_labelLFO[i]);

            _labelPitchBend[i] = Label.New(Info.Tracks[i].PitchBend.ToString());
            _labelPitchBend[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelPitchBend[i].SetMaxWidthChars(1);
            _labelPitchBend[i].WidthRequest = 30;
            _labelPitchBend[i].SetHexpand(true);
            columns.Append(_labelPitchBend[i]);

            _labelExtra[i] = Label.New(Info.Tracks[i].Extra.ToString());
            _labelExtra[i].SetEllipsize(Pango.EllipsizeMode.End);
            _labelExtra[i].SetMaxWidthChars(1);
            _labelExtra[i].WidthRequest = 30;
            _labelExtra[i].SetHexpand(true);
            columns.Append(_labelExtra[i]);

            _velocity[i] = VelocityBar.NewWithProperties([]);
            _velocity[i].SetHalign(Align.Center);
            _velocity[i].WidthRequest = GetWidth() / 4;
            _velocity[i].SetHexpand(true);
            columns.Append(_velocity[i]);

            _labelType[i] = Label.New("");
            _labelType[i].SetEllipsize(Pango.EllipsizeMode.Middle);
            _labelType[i].SetMaxWidthChars(1);
            _labelType[i].WidthRequest = 30;
            _labelType[i].SetHexpand(true);
            columns.Append(_labelType[i]);

            _listBox.Append(columns);
        }
    }

    internal void ResetMutes()
    {
        for (int i = 0; i < SongState.MAX_TRACKS; i++)
        {
            NumTracks![i] = true;
            if (_trackToggleCheckButton is not null && i < _trackToggleCheckButton.Length)
            {
                _trackToggleCheckButton[i].Active = true;
            }
        }
    }
}