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
        private List<SongEvent> _events;
        private readonly ObjectListView _listView;
        private readonly ComboBox _tracksBox;

        public TrackViewer()
        {
            int w = (600 / 2) - 12 - 6, h = 400 - 12 - 11;
            _listView = new ObjectListView
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
            c1 = new OLVColumn(Strings.TrackViewerEvent, "Command.Label");
            c2 = new OLVColumn(Strings.TrackViewerArguments, "Command.Arguments") { UseFiltering = false };
            c3 = new OLVColumn(Strings.TrackViewerOffset, "Offset") { AspectToStringFormat = "0x{0:X}", UseFiltering = false };
            c4 = new OLVColumn(Strings.TrackViewerTicks, "Ticks") { AspectGetter = (o) => string.Join(", ", ((SongEvent)o).Ticks), UseFiltering = false };
            c1.Width = c2.Width = c3.Width = 72;
            c4.Width = 47;
            c1.Hideable = c2.Hideable = c3.Hideable = c4.Hideable = false;
            c1.TextAlign = c2.TextAlign = c3.TextAlign = c4.TextAlign = HorizontalAlignment.Center;
            _listView.AllColumns.AddRange(new OLVColumn[] { c1, c2, c3, c4 });
            _listView.RebuildColumns();
            _listView.ItemActivate += ListView_ItemActivate;

            var panel1 = new ThemedPanel { Location = new Point(306, 12), Size = new Size(w, h) };
            _tracksBox = new ComboBox
            {
                Enabled = false,
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            _tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;
            panel1.Controls.AddRange(new Control[] { _tracksBox });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { _listView, panel1 });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Text = $"{Utils.ProgramName} ― {Strings.TrackViewerTitle}";

            UpdateTracks();
        }

        private void ListView_ItemActivate(object sender, EventArgs e)
        {
            List<long> list = ((SongEvent)_listView.SelectedItem.RowObject).Ticks;
            if (list.Count > 0)
            {
                Engine.Instance?.Player.SetCurrentPosition(list[0]);
                MainForm.Instance.LetUIKnowPlayerIsPlaying();
            }
        }

        private void RowColorer(OLVListItem item)
        {
            item.BackColor = ((SongEvent)item.RowObject).Command.Color;
        }

        private void TracksBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = _tracksBox.SelectedIndex;
            if (i != -1)
            {
                _events = Engine.Instance.Player.Events[i];
                _listView.SetObjects(_events);
            }
            else
            {
                _listView.Items.Clear();
            }
        }
        public void UpdateTracks()
        {
            int numTracks = (Engine.Instance?.Player.Events?.Length).GetValueOrDefault();
            bool tracks = numTracks > 0;
            _tracksBox.Enabled = tracks;
            if (tracks)
            {
                // Track 0, Track 1, ...
                _tracksBox.DataSource = Enumerable.Range(0, numTracks).Select(i => string.Format(Strings.TrackViewerTrackX, i)).ToList();
            }
            else
            {
                _tracksBox.DataSource = null;
            }
        }
    }
}