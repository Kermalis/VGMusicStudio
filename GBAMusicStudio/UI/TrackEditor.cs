using GBAMusicStudio.Core;
using GBAMusicStudio.Properties;
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
    internal class TrackEditor : Form
    {
        int currentTrack = 0;
        List<SongEvent> events;

        readonly ListView listView;
        readonly Label[] labels = new Label[3];
        readonly NumericUpDown[] args = new NumericUpDown[3];

        readonly ComboBox tracksBox;
        readonly Button tvButton;
        readonly NumericUpDown[] tvArgs = new NumericUpDown[2];

        readonly ComboBox remapsBox;
        readonly Button rfButton, rtButton, gvButton;
        readonly NumericUpDown[] gvArgs = new NumericUpDown[2];

        internal TrackEditor()
        {
            int w = 300 - 12 - 6, h = 400 - 24;
            listView = new ListView
            {
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Location = new Point(12, 12),
                Size = new Size(w, h),
                View = View.Details
            };
            listView.Columns.Add("Event", 70);
            listView.Columns.Add("Arguments", 70);
            listView.Columns.Add("Offset", 70);
            listView.Columns.Add("Ticks", 50);
            listView.Columns[0].TextAlign = listView.Columns[1].TextAlign = listView.Columns[2].TextAlign = listView.Columns[3].TextAlign = HorizontalAlignment.Center;
            listView.SelectedIndexChanged += SelectedIndexChanged;

            int h2 = h / 3 - 4;
            var panel1 = new Panel { Location = new Point(306, 12), Size = new Size(w, h2) };
            var panel2 = new Panel { Location = new Point(306, 140), Size = new Size(w, h2 - 1) };
            var panel3 = new Panel { Location = new Point(306, 267), Size = new Size(w, h2) };
            panel1.BorderStyle = panel2.BorderStyle = panel3.BorderStyle = BorderStyle.FixedSingle;

            // Arguments numericals
            for (int i = 0; i < 3; i++)
            {
                int y = 16 + (33 * i);
                labels[i] = new Label
                {
                    AutoSize = true,
                    Location = new Point(52, y + 3),
                    Text = "Arg. " + (i + 1).ToString(),
                    Visible = false,
                };
                args[i] = new NumericUpDown
                {
                    Location = new Point(w - 152, y),
                    Maximum = int.MaxValue,
                    Minimum = int.MinValue,
                    Size = new Size(100, 25),
                    TextAlign = HorizontalAlignment.Center,
                    Visible = false
                };
                args[i].ValueChanged += ArgumentChanged;
                panel1.Controls.AddRange(new Control[] { labels[i], args[i] });
            }

            // Track controls
            tracksBox = new ComboBox { Enabled = false, Size = new Size(100, 21) };
            tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;
            tvButton = new Button
            {
                Enabled = false,
                Location = new Point(14, 48),
                Size = new Size(75, 23),
                Text = "Change Voices"
            };
            tvButton.Click += ChangeEvents;
            var tvFrom = new Label { Location = new Point(115, 50 + 3), Text = "From" };
            tvArgs[0] = new NumericUpDown { Location = new Point(145, 50) };
            var tvTo = new Label { Location = new Point(200, 50 + 3), Text = "To" };
            tvArgs[1] = new NumericUpDown { Location = new Point(220, 50) };
            tvArgs[0].Maximum = tvArgs[1].Maximum = 0xFF;
            tvArgs[0].Size = tvArgs[1].Size = new Size(45, 23);
            tvArgs[0].TextAlign = tvArgs[1].TextAlign = HorizontalAlignment.Center;
            tvButton.AutoSize = tvFrom.AutoSize = tvTo.AutoSize = true;
            panel2.Controls.AddRange(new Control[] { tracksBox, tvButton, tvFrom, tvTo, tvArgs[0], tvArgs[1] });

            // Global controls
            remapsBox = new ComboBox { DataSource = Config.InstrumentRemaps.Keys.ToArray(), Size = new Size(100, 21) };
            rfButton = new Button
            {
                Enabled = false,
                Location = new Point(115, 0),
                Text = "From"
            };
            rfButton.Click += (s, e) => ApplyRemap(true);
            rtButton = new Button
            {
                Enabled = false,
                Location = new Point(200, 0),
                Text = "To"
            };
            rtButton.Click += (s, e) => ApplyRemap(false);
            gvButton = new Button
            {
                Enabled = false,
                Location = new Point(14, 48),
                Size = new Size(75, 23),
                Text = "Change Voices"
            };
            gvButton.Click += ChangeAllEvents;
            var gvFrom = new Label { Location = new Point(115, 50 + 3), Text = "From" };
            gvArgs[0] = new NumericUpDown { Location = new Point(145, 50) };
            var gvTo = new Label { Location = new Point(200, 50 + 3), Text = "To" };
            gvArgs[1] = new NumericUpDown { Location = new Point(220, 50) };
            gvArgs[0].Maximum = gvArgs[1].Maximum = 0xFF;
            gvArgs[0].Size = gvArgs[1].Size = new Size(45, 23);
            gvArgs[0].TextAlign = gvArgs[1].TextAlign = HorizontalAlignment.Center;
            gvButton.AutoSize = gvFrom.AutoSize = gvTo.AutoSize = true;
            panel3.Controls.AddRange(new Control[] { remapsBox, rfButton, rtButton, gvButton, gvFrom, gvTo, gvArgs[0], gvArgs[1] });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { listView, panel1, panel2, panel3 });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = Resources.Icon;
            MaximizeBox = false;
            Text = "GBA Music Studio ― Track Editor";

            UpdateTracks();
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
        
        void UpdateEvent()
        {
            var control = ActiveControl;
            int selected = listView.SelectedIndices[0];
            LoadTrack(currentTrack);
            listView.Items[selected].Selected = true;
            control.Select();
        }
        void LoadTrack(int track)
        {
            currentTrack = track;
            events = SongPlayer.Song.Commands[track];
            listView.Items.Clear();
            SelectedIndexChanged(null, null);
            foreach (var e in events)
            {
                var arr = new string[4];
                arr[0] = e.Command.Name;
                arr[1] = e.Command.Arguments;
                arr[2] = $"0x{e.Offset.ToString("X")}";
                arr[3] = e.AbsoluteTicks.ToString();
                var item = new ListViewItem(arr) { Tag = e };
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
                listView.Items.Add(item);
            }
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
                    UpdateEvent();
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
