using BrightIdeasSoftware;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    [DesignerCategory("")]
    internal class TrackViewer : ThemedForm
    {
        private List<SongEvent> events;
        private readonly ObjectListView listView;
        private readonly ComboBox tracksBox;

        public TrackViewer()
        {
            int w = (600 / 2) - 12 - 6, h = 400 - 12 - 11;
            listView = new ObjectListView
            {
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                Location = new Point(12, 12),
                MultiSelect = false,
                RowFormatter = RowColorer,
                ShowGroups = false,
                Size = new Size(w, h),
                UseFiltering = true,
                UseFilterIndicator = true
            };
            OLVColumn c1, c2, c3, c4;
            c1 = new OLVColumn(Strings.TrackEditorEvent, "Command.Label");
            c2 = new OLVColumn(Strings.TrackEditorArguments, "Command.Arguments") { UseFiltering = false };
            c3 = new OLVColumn(Strings.TrackEditorOffset, "Offset") { AspectToStringFormat = "0x{0:X4}", UseFiltering = false };
            c4 = new OLVColumn(Strings.TrackEditorTicks, "Ticks") { UseFiltering = false };
            c1.Width = c2.Width = c3.Width = 72;
            c4.Width = 47;
            c1.Hideable = c2.Hideable = c3.Hideable = c4.Hideable = false;
            c1.TextAlign = c2.TextAlign = c3.TextAlign = c4.TextAlign = HorizontalAlignment.Center;
            listView.AllColumns.AddRange(new OLVColumn[] { c1, c2, c3, c4 });
            listView.RebuildColumns();
            listView.ItemActivate += ListView_ItemActivate;

            var panel1 = new ThemedPanel { Location = new Point(306, 12), Size = new Size(w, h) };
            tracksBox = new ComboBox
            {
                Enabled = false,
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            tracksBox.SelectedIndexChanged += (o, e) => LoadTrack(tracksBox.SelectedIndex);
            panel1.Controls.AddRange(new Control[] { tracksBox });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { listView, panel1 });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Text = $"{Utils.ProgramName} ― {Strings.TrackEditorTitle}";

            UpdateTracks();
        }

        private void ListView_ItemActivate(object sender, EventArgs e)
        {
            //SongPlayer.Instance.SetSongPosition(((SongEvent)listView.SelectedItem.RowObject).Ticks);
        }

        private void RowColorer(OLVListItem item)
        {
            item.BackColor = ((SongEvent)item.RowObject).Command.Color;
        }

        private void LoadTrack(int track)
        {
            events = Engine.Instance.Player.Events[track];
            listView.SetObjects(events);
        }
        public void UpdateTracks()
        {
            int numTracks = (Engine.Instance?.Player.Events?.Length).GetValueOrDefault();
            bool tracks = numTracks > 0;
            tracksBox.Enabled = tracks;

            // Track 1, Track 2, ...
            tracksBox.DataSource = Enumerable.Range(1, numTracks).Select(i => string.Format(Strings.TrackEditorTrackX, i)).ToList();

            if (!tracks)
            {
                listView.Items.Clear();
            }
        }
    }
}