using BrightIdeasSoftware;
using GBAMusicStudio.Core;
using GBAMusicStudio.Core.M4A;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [System.ComponentModel.DesignerCategory("")]
    internal class TrackEditor : ThemedForm
    {
        int currentTrack = 0;
        List<SongEvent> events;

        readonly ObjectListView listView;
        readonly ThemedLabel[] labels = new ThemedLabel[3];
        readonly ThemedNumeric[] args = new ThemedNumeric[3];

        readonly ComboBox tracksBox;
        readonly ThemedButton tvButton;
        readonly ThemedNumeric[] tvArgs = new ThemedNumeric[2];

        readonly ComboBox remapsBox;
        readonly ThemedButton rfButton, rtButton, gvButton;
        readonly ThemedNumeric[] gvArgs = new ThemedNumeric[2];

        internal TrackEditor()
        {
            int w = 300 - 12 - 6, h = 400 - 24;
            listView = new ObjectListView
            {
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                Location = new Point(12, 12),
                MultiSelect = false,
                RowFormatter = RowFormatter,
                ShowGroups = false,
                Size = new Size(w, h),
                UseFiltering = true,
                UseFilterIndicator = true
            };
            OLVColumn c1, c2, c3, c4;
            c1 = new OLVColumn("Event", "Command.Name");
            c2 = new OLVColumn("Arguments", "Command.Arguments") { UseFiltering = false };
            c3 = new OLVColumn("Offset", "Offset") { AspectToStringFormat = "0x{0:X}", UseFiltering = false };
            c4 = new OLVColumn("Ticks", "AbsoluteTicks") { UseFiltering = false };
            c1.Width = c2.Width = c3.Width = 72;
            c4.Width = 45;
            c1.Hideable = c2.Hideable = c3.Hideable = c4.Hideable = false;
            c1.TextAlign = c2.TextAlign = c3.TextAlign = c4.TextAlign = HorizontalAlignment.Center;
            listView.AllColumns.AddRange(new OLVColumn[] { c1, c2, c3, c4 });
            listView.RebuildColumns();
            listView.SelectedIndexChanged += SelectedIndexChanged;

            int h2 = h / 3 - 4;
            var panel1 = new ThemedPanel { Location = new Point(306, 12), Size = new Size(w, h2) };
            var panel2 = new ThemedPanel { Location = new Point(306, 140), Size = new Size(w, h2 - 1) };
            var panel3 = new ThemedPanel { Location = new Point(306, 267), Size = new Size(w, h2) };

            // Arguments numericals
            for (int i = 0; i < 3; i++)
            {
                int y = 17 + (33 * i);
                labels[i] = new ThemedLabel
                {
                    AutoSize = true,
                    Location = new Point(52, y + 3),
                    Text = "Arg. " + (i + 1).ToString(),
                    Visible = false,
                };
                args[i] = new ThemedNumeric
                {
                    Location = new Point(w - 152, y),
                    Maximum = int.MaxValue,
                    Minimum = int.MinValue,
                    Size = new Size(100, 25),
                    Visible = false
                };
                args[i].ValueChanged += ArgumentChanged;
                panel1.Controls.AddRange(new Control[] { labels[i], args[i] });
            }

            // Track controls
            tracksBox = new ComboBox
            {
                Enabled = false,
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;
            tvButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(13, 48),
                Size = new Size(75, 23),
                Text = "Change Voices"
            };
            tvButton.Click += ChangeEvents;
            var tvFrom = new ThemedLabel { Location = new Point(115, 50 + 3), Text = "From" };
            tvArgs[0] = new ThemedNumeric { Location = new Point(149, 50) };
            var tvTo = new ThemedLabel { Location = new Point(204, 50 + 3), Text = "To" };
            tvArgs[1] = new ThemedNumeric { Location = new Point(224, 50) };
            tvArgs[0].Maximum = tvArgs[1].Maximum = 0xFF;
            tvArgs[0].Size = tvArgs[1].Size = new Size(45, 23);
            tvArgs[0].TextAlign = tvArgs[1].TextAlign = HorizontalAlignment.Center;
            tvButton.AutoSize = tvFrom.AutoSize = tvTo.AutoSize = true;
            panel2.Controls.AddRange(new Control[] { tracksBox, tvButton, tvFrom, tvTo, tvArgs[0], tvArgs[1] });

            // Global controls
            remapsBox = new ComboBox
            {
                DataSource = Config.InstrumentRemaps.Keys.ToArray(),
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            rfButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(116, 3),
                Text = "From"
            };
            rfButton.Click += (s, e) => ApplyRemap(true);
            rtButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(203, 3),
                Text = "To"
            };
            rtButton.Click += (s, e) => ApplyRemap(false);
            gvButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(13, 48),
                Size = new Size(75, 23),
                Text = "Change Voices"
            };
            gvButton.Click += ChangeAllEvents;
            var gvFrom = new ThemedLabel { Location = new Point(115, 50 + 3), Text = "From" };
            gvArgs[0] = new ThemedNumeric { Location = new Point(149, 50) };
            var gvTo = new ThemedLabel { Location = new Point(204, 50 + 3), Text = "To" };
            gvArgs[1] = new ThemedNumeric { Location = new Point(224, 50) };
            gvArgs[0].Maximum = gvArgs[1].Maximum = 0xFF;
            gvArgs[0].Size = gvArgs[1].Size = new Size(45, 23);
            gvArgs[0].TextAlign = gvArgs[1].TextAlign = HorizontalAlignment.Center;
            gvButton.AutoSize = gvFrom.AutoSize = gvTo.AutoSize = true;
            panel3.Controls.AddRange(new Control[] { remapsBox, rfButton, rtButton, gvButton, gvFrom, gvTo, gvArgs[0], gvArgs[1] });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { listView, panel1, panel2, panel3 });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Text = "GBA Music Studio ― Track Editor";

            UpdateTracks();
        }

        void RowFormatter(OLVListItem item)
        {
            var e = (SongEvent)item.RowObject;
            if (e.Command is GoToCommand || e.Command is CallCommand || e.Command is ReturnCommand || e.Command is FinishCommand)
                item.BackColor = Color.MediumSpringGreen;
            else if (e.Command is VoiceCommand)
                item.BackColor = Color.DarkSalmon;
            else if (e.Command is RestCommand)
                item.BackColor = Color.PaleVioletRed;
            else if (e.Command is KeyShiftCommand || e.Command is NoteCommand || e.Command is EndOfTieCommand)
                item.BackColor = Color.SkyBlue;
            else if (e.Command is ModDepthCommand || e.Command is ModTypeCommand)
                item.BackColor = Color.LightSteelBlue;
            else if (e.Command is TuneCommand || e.Command is BendCommand || e.Command is BendRangeCommand)
                item.BackColor = Color.MediumPurple;
            else if (e.Command is PanpotCommand || e.Command is LFODelayCommand || e.Command is LFOSpeedCommand)
                item.BackColor = Color.GreenYellow;
            else if (e.Command is TempoCommand)
                item.BackColor = Color.DeepSkyBlue;
            else
                item.BackColor = Color.SteelBlue;
        }

        void ApplyRemap(bool from)
        {
            string remap = (string)remapsBox.SelectedItem;
            foreach (var track in SongPlayer.Song.Commands)
                foreach (var ev in track)
                    if (ev.Command is VoiceCommand voice)
                        voice.Voice = Config.GetRemap(voice.Voice, remap, from);
            LoadTrack(currentTrack);
        }
        void ChangeEvents(object sender, EventArgs e)
        {
            foreach (var ev in events)
                if (sender == tvButton && ev.Command is VoiceCommand voice && voice.Voice == tvArgs[0].Value)
                    voice.Voice = (byte)tvArgs[1].Value;
            LoadTrack(currentTrack);
        }
        void ChangeAllEvents(object sender, EventArgs e)
        {
            foreach (var track in SongPlayer.Song.Commands)
                foreach (var ev in track)
                    if (sender == gvButton && ev.Command is VoiceCommand voice && voice.Voice == gvArgs[0].Value)
                        voice.Voice = (byte)gvArgs[1].Value;
            LoadTrack(currentTrack);
        }

        void LoadTrack(int track)
        {
            currentTrack = track;
            events = SongPlayer.Song.Commands[track];
            listView.SetObjects(events);
            SelectedIndexChanged(null, null);
        }
        void TracksBox_SelectedIndexChanged(object sender, EventArgs e) => LoadTrack(tracksBox.SelectedIndex);
        internal void UpdateTracks()
        {
            bool tracks = SongPlayer.NumTracks > 0;
            tracksBox.Enabled = tvButton.Enabled = gvButton.Enabled = tracks;
            tracksBox.DataSource = Enumerable.Range(1, SongPlayer.NumTracks).Select(i => $"Track {i}").ToList();
            rfButton.Enabled = rtButton.Enabled = tracks && remapsBox.Items.Count > 0;
            if (!tracks)
                listView.Items.Clear();
        }

        void ArgumentChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < 3; i++)
            {
                if (sender == args[i])
                {
                    var se = events[listView.SelectedIndices[0]];
                    object value = args[i].Value;
                    var m = se.Command.GetType().GetMember(labels[i].Text)[0];
                    if (m is FieldInfo f)
                        f.SetValue(se.Command, Convert.ChangeType(value, f.FieldType));
                    else if (m is PropertyInfo p)
                        p.SetValue(se.Command, Convert.ChangeType(value, p.PropertyType));

                    var control = ActiveControl;
                    int selected = listView.SelectedIndices[0];
                    LoadTrack(currentTrack);
                    listView.Items[selected].Selected = true;
                    listView.Select();
                    listView.EnsureVisible(selected);
                    control.Select();

                    return;
                }
            }
        }
        void SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count != 1)
            {
                labels[0].Visible = labels[1].Visible = labels[2].Visible =
                    args[0].Visible = args[1].Visible = args[2].Visible = false;
            }
            else
            {
                var se = events[listView.SelectedIndices[0]];
                var ignore = typeof(ICommand).GetMembers();
                var mi = se.Command.GetType().GetMembers().Where(m => !ignore.Any(a => m.Name == a.Name) && (m is FieldInfo || m is PropertyInfo)).ToArray();
                for (int i = 0; i < 3; i++)
                {
                    labels[i].Visible = args[i].Visible = i < mi.Length;
                    if (args[i].Visible)
                    {
                        labels[i].Text = mi[i].Name;

                        args[i].ValueChanged -= ArgumentChanged;

                        dynamic m = mi[i];

                        args[i].Hexadecimal = se.Command is CallCommand || se.Command is GoToCommand;

                        TypeInfo valueType;
                        if (mi[i].MemberType == MemberTypes.Field)
                            valueType = m.FieldType;
                        else
                            valueType = m.PropertyType;
                        args[i].Maximum = valueType.DeclaredFields.Single(f => f.Name == "MaxValue").GetValue(m);
                        args[i].Minimum = valueType.DeclaredFields.Single(f => f.Name == "MinValue").GetValue(m);

                        object value = m.GetValue(se.Command);
                        args[i].Value = (decimal)Convert.ChangeType(value, TypeCode.Decimal);

                        args[i].ValueChanged += ArgumentChanged;
                    }
                }
            }
            labels[0].Parent.Refresh();
        }
    }
}
