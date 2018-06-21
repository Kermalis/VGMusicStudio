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

        internal TrackEditor()
        {
            int w = 300 - 24 - 5, h = 400 - 24 - 24 - 12 - 2;
            listView = new ListView
            {
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Location = new Point(12, 12),
                Size = new Size(w, h),
                View = View.Details
            };
            listView.Columns.Add("Event", 71);
            listView.Columns.Add("Arguments", 96);
            listView.Columns.Add("Offset", 71);
            listView.Columns[0].TextAlign = listView.Columns[1].TextAlign = listView.Columns[2].TextAlign = HorizontalAlignment.Center;
            listView.SelectedIndexChanged += SelectedIndexChanged;

            tracksBox = new ComboBox { Enabled = false };
            tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;

            var panel = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(300, 12),
                Size = new Size(w, h)
            };
            panel.Controls.Add(tracksBox);

            for (int i = 0; i < 3; i++)
            {
                int y = 30 + (30 * i);
                labels[i] = new Label
                {
                    Location = new Point(0, y),
                    Size = new Size(50, 25),
                    Text = "Arg" + (i + 1).ToString(),
                    Visible = false,
                };
                args[i] = new NumericUpDown
                {
                    Location = new Point(labels[i].Width, y - 2),
                    Maximum = int.MaxValue,
                    Minimum = int.MinValue,
                    Size = new Size(100, 25),
                    TextAlign = HorizontalAlignment.Center,
                    Visible = false
                };
                args[i].ValueChanged += ArgumentChanged;
                panel.Controls.AddRange(new Control[] { labels[i], args[i] });
            }

            Controls.AddRange(new Control[] { listView, panel });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = Resources.Icon;
            MaximizeBox = false;
            Size = new Size(600, 400);
            Text = "GBA Music Studio ― Track Viewer";

            UpdateTracks();
        }

        void LoadTrack(int index)
        {
            events = SongPlayer.Song.Commands[index];
            listView.Items.Clear();
            foreach (var e in events)
            {
                var arr = new string[3];
                arr[0] = e.Command.ToString();
                arr[1] = e.Arguments.Print(false);
                arr[2] = SongPlayer.Song is ROMSong ? $"0x{e.Offset.ToString("X")}" : e.Offset.ToString();
                var item = new ListViewItem(arr);
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
            for (int i = 0; i < args.Length; i++)
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
            if (listView.SelectedIndices.Count != 1) return;
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
