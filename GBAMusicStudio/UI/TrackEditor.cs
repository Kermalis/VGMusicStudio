using GBAMusicStudio.Core;
using GBAMusicStudio.Properties;
using GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [System.ComponentModel.DesignerCategory("")]
    internal class TrackEditor : Form
    {
        List<SongEvent> events;

        readonly ListView listView;
        readonly ComboBox tracksBox;
        readonly Label[] labels = new Label[3];
        readonly NumericUpDown[] args = new NumericUpDown[3];

        readonly Button tvButton;
        readonly NumericUpDown[] tvArgs = new NumericUpDown[2];

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
            listView.Columns.Add("Event", 86);
            listView.Columns.Add("Arguments", 87);
            listView.Columns.Add("Offset", 86);
            listView.Columns[0].TextAlign = listView.Columns[1].TextAlign = listView.Columns[2].TextAlign = HorizontalAlignment.Center;
            listView.SelectedIndexChanged += SelectedIndexChanged;

            tracksBox = new ComboBox { Enabled = false };
            tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;

            int h2 = h / 3 - 4;
            var panel1 = new Panel
            {
                Location = new Point(306, 12),
                Size = new Size(w, h2)
            };
            var panel2 = new Panel
            {
                Location = new Point(306, 140),
                Size = new Size(w, h2 - 1)
            };
            panel2.Controls.Add(tracksBox);
            var panel3 = new Panel
            {
                Location = new Point(306, 267),
                Size = new Size(w, h2)
            };
            panel1.BorderStyle = panel2.BorderStyle = panel3.BorderStyle = BorderStyle.FixedSingle;

            // Arguments numericals
            for (int i = 0; i < 3; i++)
            {
                int y = 16 + (33 * i);
                labels[i] = new Label
                {
                    Location = new Point(52, y + 3),
                    Size = new Size(40, 25),
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
            tvButton = new Button
            {
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
            panel2.Controls.AddRange(new Control[] { tvButton, tvFrom, tvTo, tvArgs[0], tvArgs[1] });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { listView, panel1, panel2, panel3 });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = Resources.Icon;
            MaximizeBox = false;
            Text = "GBA Music Studio ― Track Editor";

            UpdateTracks();
        }

        void ChangeEvents(object sender, EventArgs e)
        {
            foreach (var ev in events)
            {
                if (sender == tvButton && ev.Command == Command.Voice && ev.Arguments[0] == tvArgs[0].Value)
                {
                    ev.Arguments[0] = (int)tvArgs[1].Value;
                    ChangeEventColor(ev, Color.LightPink);
                }
            }
        }
        void ChangeEventColor(SongEvent e, Color c)
        {
            var item = listView.Items.Cast<ListViewItem>().Single(i => i.Tag == e);
            item.BackColor = c;
        }

        void LoadTrack(int index)
        {
            events = SongPlayer.Song.Commands[index];
            listView.Items.Clear();
            SelectedIndexChanged(null, null);
            foreach (var e in events)
            {
                var arr = new string[3];
                arr[0] = e.Command.ToString();
                arr[1] = e.Arguments.Print(false);
                arr[2] = $"0x{e.Offset.ToString("X")}";
                var item = new ListViewItem(arr) { Tag = e };
                if (e.Command == Command.Voice)
                    item.BackColor = Color.LightSteelBlue;
                listView.Items.Add(item);
            }
        }
        void TracksBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTrack(tracksBox.SelectedIndex);
        }
        internal void UpdateTracks()
        {
            tracksBox.Enabled = SongPlayer.NumTracks > 0;
            tracksBox.DataSource = Enumerable.Range(1, SongPlayer.NumTracks).Select(i => $"Track {i}").ToList();
            if (SongPlayer.NumTracks == 0)
                listView.Items.Clear();
        }

        void ArgumentChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < 3; i++)
            {
                if (sender == args[i])
                {
                    events[listView.SelectedIndices[0]].Arguments[i] = (int)args[i].Value;
                    listView.SelectedItems[0].BackColor = Color.LightPink;
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

                for (int i = 0; i < 3; i++)
                {
                    labels[i].Visible = args[i].Visible = i < se.Arguments.Length;
                    if (args[i].Visible)
                    {
                        args[i].ValueChanged -= ArgumentChanged;
                        args[i].Value = se.Arguments[i];
                        args[i].ValueChanged += ArgumentChanged;
                    }
                }
            }
        }
    }
}
