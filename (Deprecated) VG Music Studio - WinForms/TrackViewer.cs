using BrightIdeasSoftware;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
internal sealed class TrackViewer : ThemedForm
{
    private int _currentTrack = 0;
    private List<SongEvent>? _events;

    private readonly ObjectListView _listView;
    private readonly ThemedLabel[] _argLabels = new ThemedLabel[3];
    private readonly ThemedNumeric[] _argNumerics = new ThemedNumeric[3];

    private readonly ComboBox _tracksBox, _commandsBox;
    private readonly ThemedButton _trackAddEventButton, _trackRemoveEventButton;

    public TrackViewer()
    {
        const int W = (600 / 2) - 12 - 6;
        const int H = 400 - 12 - 11;

        _listView = new ObjectListView
        {
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            HideSelection = false,
            Location = new Point(12, 12),
            MultiSelect = false,
            RowFormatter = RowColorer,
            ShowGroups = false,
            Size = new Size(W, H),
            UseFiltering = true,
            UseFilterIndicator = true
        };
        OLVColumn c1, c2, c3, c4;
        c1 = new OLVColumn(Strings.TrackEditorEvent, "Command.Label");
        c2 = new OLVColumn(Strings.TrackEditorArguments, "Command.Arguments") { UseFiltering = false };
        c3 = new OLVColumn(Strings.TrackEditorOffset, "Offset") { AspectToStringFormat = "0x{0:X}", UseFiltering = false };
        c4 = new OLVColumn(Strings.TrackEditorTicks, "Ticks") { AspectGetter = (o) => string.Join(", ", ((SongEvent)o).Ticks), UseFiltering = false };
        c1.Width = c2.Width = c3.Width = 72;
        c4.Width = 47;
        c1.Hideable = c2.Hideable = c3.Hideable = c4.Hideable = false;
        c1.TextAlign = c2.TextAlign = c3.TextAlign = c4.TextAlign = HorizontalAlignment.Center;
        _listView.AllColumns.AddRange([c1, c2, c3, c4]);
        _listView.RebuildColumns();
        _listView.SelectedIndexChanged += SelectedIndexChanged;
        _listView.ItemActivate += ListView_ItemActivate;

        int h2 = (H / 3) - 4;
        var panel1 = new ThemedPanel { Location = new Point(306, 12), Size = new Size(W, h2) };
        var panel2 = new ThemedPanel { Location = new Point(306, 140), Size = new Size(W, h2) };
        var panel3 = new ThemedPanel { Location = new Point(306, 268), Size = new Size(W, h2) };

        // Track controls
        _tracksBox = new ComboBox
        {
            Enabled = false,
            Location = new Point(4, 4),
            Size = new Size(100, 21)
        };
        _tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;
        _trackAddEventButton = new ThemedButton
        {
            Location = new Point(13, 30 + 25 + 5),
            Size = new Size(100, 25),
            Text = Strings.TrackEditorAddEvent
        };
        _trackAddEventButton.Click += AddEvent;
        _commandsBox = new ComboBox
        {
            Location = new Point(115, 30 + 25 + 5 + 2),
            Size = new Size(100, 21)
        };
        _trackRemoveEventButton = new ThemedButton
        {
            Location = new Point(13, 30 + 25 + 5 + 25 + 5),
            Text = Strings.TrackEditorRemoveEvent
        };
        _trackRemoveEventButton.Click += RemoveEvent;
        _tracksBox.Enabled = _trackAddEventButton.Enabled = _trackRemoveEventButton.Enabled = _commandsBox.Enabled = false;
        _trackAddEventButton.Size = _trackRemoveEventButton.Size = new Size(95, 25);
        panel1.Controls.AddRange([_tracksBox, _trackAddEventButton, _commandsBox, _trackRemoveEventButton]);

        // Arguments Info
        for (int i = 0; i < 3; i++)
        {
            int y = 17 + (33 * i);
            _argLabels[i] = new ThemedLabel
            {
                AutoSize = true,
                Location = new Point(52, y + 3),
                Text = string.Format(Strings.TrackEditorArgX, i + 1),
                Visible = false,
            };
            _argNumerics[i] = new ThemedNumeric
            {
                Location = new Point(W - 152, y),
                Maximum = int.MaxValue,
                Minimum = int.MinValue,
                Size = new Size(100, 25),
                Visible = false
            };
            _argNumerics[i].ValueChanged += ArgumentChanged;
            panel2.Controls.AddRange([_argLabels[i], _argNumerics[i]]);
        }

        // Global controls
        ClientSize = new Size(600, 400);
        Controls.AddRange([_listView, panel1, panel2, panel3]);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Text = $"{ConfigUtils.PROGRAM_NAME} ― {Strings.TrackEditorTitle}";

        UpdateTracks();
    }

    private void ListView_ItemActivate(object? sender, EventArgs e)
    {
        List<long> list = ((SongEvent)_listView.SelectedItem.RowObject).Ticks;
        if (list.Count > 0)
        {
            Engine.Instance!.Player.SetSongPosition(list[0]);
            MainForm.Instance.LetUIKnowPlayerIsPlaying();
        }
    }

    private void AddEvent(object? sender, EventArgs e)
    {
        var cmd = (ICommand)Activator.CreateInstance(Engine.Instance!.GetCommands()[_commandsBox.SelectedIndex].GetType())!;
        var ev = new SongEvent(int.MaxValue, cmd);
        int index = _listView.SelectedIndex + 1;
        Engine.Instance.Player.LoadedSong!.InsertEvent(ev, _currentTrack, index);
        Engine.Instance.Player.RefreshSong();
        LoadTrack(_currentTrack);
        SelectItem(index);
    }
    private void RemoveEvent(object? sender, EventArgs e)
    {
        if (_listView.SelectedIndex == -1)
        {
            return;
        }
        Engine.Instance!.Player.LoadedSong!.RemoveEvent(_currentTrack, _listView.SelectedIndex);
        Engine.Instance!.Player.RefreshSong();
        LoadTrack(_currentTrack);
    }

    private void RowColorer(OLVListItem item)
    {
        item.BackColor = ((SongEvent)item.RowObject).Command.Color;
    }

    private void TracksBox_SelectedIndexChanged(object? sender, EventArgs? e)
    {
        int i = _tracksBox.SelectedIndex;
        if (i != -1)
        {
            _listView.SetObjects(Engine.Instance!.Player.LoadedSong!.Events[i]);
            _currentTrack = i;
            _events = Engine.Instance!.Player.LoadedSong!.Events[i]!;
            _listView.SetObjects(_events);
            SelectedIndexChanged(null, null!);
        }
        else
        {
            _listView.Items.Clear();
        }
    }
    private void SelectItem(int index)
    {
        _listView.Items[index].Selected = true;
        _listView.Select();
        _listView.EnsureVisible(index);
    }

    private void ArgumentChanged(object? sender, EventArgs e)
    {
        for (int i = 0; i < 3; i++)
        {
            if (sender == _argNumerics[i])
            {
                SongEvent se = _events![_listView.SelectedIndices[0]];
                object value = _argNumerics[i].Value;
                MemberInfo m = se.Command.GetType().GetMember(_argLabels[i].Text)[0];
                if (m is FieldInfo f)
                {
                    f.SetValue(se.Command, Convert.ChangeType(value, f.FieldType));
                }
                else if (m is PropertyInfo p)
                {
                    p.SetValue(se.Command, Convert.ChangeType(value, p.PropertyType));
                }

                Engine.Instance!.Player.RefreshSong();

                Control control = ActiveControl!;
                int index = _listView.SelectedIndex;
                LoadTrack(_currentTrack);
                SelectItem(index);
                control.Select();

                return;
            }
        }
    }
    private void SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_listView.SelectedIndices.Count != 1)
        {
            _argLabels[0].Visible = _argLabels[1].Visible = _argLabels[2].Visible =
                _argNumerics[0].Visible = _argNumerics[1].Visible = _argNumerics[2].Visible = false;
        }
        else
        {
            var se = (SongEvent)_listView.SelectedObject;
            MemberInfo[] ignore = typeof(ICommand).GetMembers();
            MemberInfo[] mi = se.Command == null ? [] : se.Command.GetType().GetMembers().Where(m => !ignore.Any(a => m.Name == a.Name) && (m is FieldInfo || m is PropertyInfo)).ToArray();
            for (int i = 0; i < 3; i++)
            {
                _argLabels[i].Visible = _argNumerics[i].Visible = i < mi.Length;
                if (_argNumerics[i].Visible)
                {
                    _argLabels[i].Text = mi[i].Name;

                    _argNumerics[i].ValueChanged -= ArgumentChanged;

                    dynamic m = mi[i];

                    _argNumerics[i].Hexadecimal = Engine.Instance!.Player.LoadedSong!.CallOrJumpCommand(se);

                    TypeInfo valueType;
                    if (mi[i].MemberType == MemberTypes.Field)
                    {
                        valueType = m.FieldType;
                    }
                    else
                    {
                        valueType = m.PropertyType;
                    }
                    _argNumerics[i].Maximum = valueType.DeclaredFields.Single(f => f.Name == "MaxValue").GetValue(m);
                    _argNumerics[i].Minimum = valueType.DeclaredFields.Single(f => f.Name == "MinValue").GetValue(m);

                    object value = m.GetValue(se.Command);
                    _argNumerics[i].Value = (decimal)Convert.ChangeType(value, TypeCode.Decimal);

                    _argNumerics[i].ValueChanged += ArgumentChanged;
                }
            }
        }
        _argLabels[0].Parent!.Refresh();
    }
    private void LoadTrack(int track)
    {
        _currentTrack = track;
        _events = Engine.Instance!.Player.LoadedSong!.Events[track]!;
        _listView.SetObjects(_events);
        SelectedIndexChanged(null, null!);
    }
    public void UpdateTracks()
    {
        int numTracks = Engine.Instance?.Player.LoadedSong?.Events.Length ?? 0;
        bool tracks = numTracks > 0;
        _tracksBox.Enabled = _trackAddEventButton.Enabled = _trackRemoveEventButton.Enabled = _commandsBox.Enabled = tracks;
        if (tracks)
        {
            // Track 0, Track 1, ...
            _tracksBox.DataSource = Enumerable.Range(0, numTracks).Select(i => string.Format(Strings.TrackEditorTrackX, i)).ToList();
            _commandsBox.DataSource = Engine.Instance!.GetCommands().Select(c => c.Label).ToList();
        }
        else
        {
            _tracksBox.DataSource = null;
        }
    }
}
