using BrightIdeasSoftware;
using GBAMusicStudio.Core;
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
        readonly ThemedLabel[] argLabels = new ThemedLabel[3];
        readonly ThemedNumeric[] argNumerics = new ThemedNumeric[3];

        readonly ComboBox tracksBox, commandsBox;
        readonly ThemedButton trackChangeVoicesButton, trackAddEventButton, trackRemoveEventButton;
        readonly ThemedNumeric[] trackVoiceArgs = new ThemedNumeric[2];

        readonly ComboBox remapsBox;
        readonly ThemedButton remapFromButton, remapToButton, globalChangeVoicesButton;
        readonly ThemedNumeric[] globalVoiceArgs = new ThemedNumeric[2];

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
            listView.ItemActivate += ListView_ItemActivate;

            int h2 = h / 3 - 4;
            var panel1 = new ThemedPanel { Location = new Point(306, 12), Size = new Size(w, h2) };
            var panel2 = new ThemedPanel { Location = new Point(306, 140), Size = new Size(w, h2 - 1) };
            var panel3 = new ThemedPanel { Location = new Point(306, 267), Size = new Size(w, h2) };

            // Arguments Info
            for (int i = 0; i < 3; i++)
            {
                int y = 17 + (33 * i);
                argLabels[i] = new ThemedLabel
                {
                    AutoSize = true,
                    Location = new Point(52, y + 3),
                    Text = "Arg. " + (i + 1).ToString(),
                    Visible = false,
                };
                argNumerics[i] = new ThemedNumeric
                {
                    Location = new Point(w - 152, y),
                    Maximum = int.MaxValue,
                    Minimum = int.MinValue,
                    Size = new Size(100, 25),
                    Visible = false
                };
                argNumerics[i].ValueChanged += ArgumentChanged;
                panel1.Controls.AddRange(new Control[] { argLabels[i], argNumerics[i] });
            }

            // Track controls
            tracksBox = new ComboBox
            {
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            tracksBox.SelectedIndexChanged += TracksBox_SelectedIndexChanged;
            trackChangeVoicesButton = new ThemedButton
            {
                Location = new Point(13, 30),
                Text = "Change Voices"
            };
            trackChangeVoicesButton.Click += ChangeEvents;
            trackAddEventButton = new ThemedButton
            {
                Location = new Point(13, 30 + 25 + 5),
                Text = "Add Event"
            };
            trackAddEventButton.Click += AddEvent;
            commandsBox = new ComboBox
            {
                Location = new Point(115, 30 + 25 + 5 + 2),
                Size = new Size(100, 21)
            };
            trackRemoveEventButton = new ThemedButton
            {
                Location = new Point(13, 30 + 25 + 5 + 25 + 5),
                Text = "Remove Event"
            };
            trackRemoveEventButton.Click += RemoveEvent;
            tracksBox.Enabled = trackChangeVoicesButton.Enabled = trackAddEventButton.Enabled = trackRemoveEventButton.Enabled = commandsBox.Enabled = false;
            trackChangeVoicesButton.Size = trackAddEventButton.Size = trackRemoveEventButton.Size = new Size(95, 25);
            var trackFromVoiceButton = new ThemedLabel { Location = new Point(115, 30 + 2 + 3), Text = "From" };
            trackVoiceArgs[0] = new ThemedNumeric { Location = new Point(149, 30 + 2) };
            var trackToVoiceButton = new ThemedLabel { Location = new Point(204, 30 + 2 + 3), Text = "To" };
            trackVoiceArgs[1] = new ThemedNumeric { Location = new Point(224, 30 + 2) };
            trackVoiceArgs[0].Maximum = trackVoiceArgs[1].Maximum = 0xFF;
            trackVoiceArgs[0].Size = trackVoiceArgs[1].Size = new Size(45, 23);
            trackVoiceArgs[0].TextAlign = trackVoiceArgs[1].TextAlign = HorizontalAlignment.Center;
            trackFromVoiceButton.AutoSize = trackToVoiceButton.AutoSize = true;
            panel2.Controls.AddRange(new Control[] { tracksBox, trackChangeVoicesButton, trackFromVoiceButton, trackToVoiceButton, trackVoiceArgs[0], trackVoiceArgs[1], trackAddEventButton, commandsBox, trackRemoveEventButton });

            // Global controls
            remapsBox = new ComboBox
            {
                DataSource = Config.InstrumentRemaps.Keys.ToArray(),
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            remapFromButton = new ThemedButton
            {
                Location = new Point(116, 3),
                Text = "From"
            };
            remapFromButton.Click += (s, e) => ApplyRemap(true);
            remapToButton = new ThemedButton
            {
                Location = new Point(203, 3),
                Text = "To"
            };
            remapToButton.Click += (s, e) => ApplyRemap(false);
            globalChangeVoicesButton = new ThemedButton
            {
                Location = new Point(13, 30),
                Size = new Size(95, 25),
                Text = "Change Voices"
            };
            globalChangeVoicesButton.Click += ChangeAllEvents;
            var globalFromVoiceButton = new ThemedLabel { Location = new Point(115, 30 + 2 + 3), Text = "From" };
            globalVoiceArgs[0] = new ThemedNumeric { Location = new Point(149, 30 + 2) };
            var globalToVoiceButton = new ThemedLabel { Location = new Point(204, 30 + 2 + 3), Text = "To" };
            globalVoiceArgs[1] = new ThemedNumeric { Location = new Point(224, 30 + 2) };
            globalVoiceArgs[0].Maximum = globalVoiceArgs[1].Maximum = 0xFF;
            globalVoiceArgs[0].Size = globalVoiceArgs[1].Size = new Size(45, 23);
            globalVoiceArgs[0].TextAlign = globalVoiceArgs[1].TextAlign = HorizontalAlignment.Center;
            globalFromVoiceButton.AutoSize = globalToVoiceButton.AutoSize = true;
            remapsBox.Enabled = remapFromButton.Enabled = remapToButton.Enabled = globalChangeVoicesButton.Enabled = false;
            panel3.Controls.AddRange(new Control[] { remapsBox, remapFromButton, remapToButton, globalChangeVoicesButton, globalFromVoiceButton, globalToVoiceButton, globalVoiceArgs[0], globalVoiceArgs[1] });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { listView, panel1, panel2, panel3 });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Text = "GBA Music Studio ― Track Editor";

            UpdateTracks();
        }

        void ListView_ItemActivate(object sender, EventArgs e)
        {
            SongPlayer.SetPosition(((SongEvent)listView.SelectedItem.RowObject).AbsoluteTicks);
        }

        void AddEvent(object sender, EventArgs e)
        {
            var cmd = (ICommand)Activator.CreateInstance(Engine.GetCommands()[commandsBox.SelectedIndex].GetType());
            var ev = new SongEvent(0xFFFFFFFF, cmd);
            int index = listView.SelectedIndex + 1;
            SongPlayer.Song.InsertEvent(ev, currentTrack, index);
            SongPlayer.RefreshSong();
            LoadTrack(currentTrack);
            SelectItem(index);
        }
        void RemoveEvent(object sender, EventArgs e)
        {
            if (listView.SelectedIndex == -1)
                return;
            SongPlayer.Song.RemoveEvent(currentTrack, listView.SelectedIndex);
            SongPlayer.RefreshSong();
            LoadTrack(currentTrack);
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
            else if (e.Command is KeyShiftCommand || e.Command is NoteCommand || e.Command is EndOfTieCommand || e.Command is FreeNoteCommand)
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
            bool changed = false;
            string remap = (string)remapsBox.SelectedItem;
            foreach (var track in SongPlayer.Song.Commands)
                foreach (var ev in track)
                    if (ev.Command is VoiceCommand voice)
                    {
                        voice.Voice = Config.GetRemap(voice.Voice, remap, from);
                        changed = true;
                    }
            if (changed)
            {
                SongPlayer.RefreshSong();
                LoadTrack(currentTrack);
            }
        }
        void ChangeEvents(object sender, EventArgs e)
        {
            bool changed = false;
            foreach (var ev in events)
                if (sender == trackChangeVoicesButton && ev.Command is VoiceCommand voice && voice.Voice == trackVoiceArgs[0].Value)
                {
                    voice.Voice = (byte)trackVoiceArgs[1].Value;
                    changed = true;
                }
            if (changed)
            {
                SongPlayer.RefreshSong();
                LoadTrack(currentTrack);
            }
        }
        void ChangeAllEvents(object sender, EventArgs e)
        {
            bool changed = false;
            foreach (var track in SongPlayer.Song.Commands)
                foreach (var ev in track)
                    if (sender == globalChangeVoicesButton && ev.Command is VoiceCommand voice && voice.Voice == globalVoiceArgs[0].Value)
                    {
                        voice.Voice = (byte)globalVoiceArgs[1].Value;
                        changed = true;
                    }
            if (changed)
            {
                SongPlayer.RefreshSong();
                LoadTrack(currentTrack);
            }
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
            tracksBox.Enabled = trackChangeVoicesButton.Enabled = trackAddEventButton.Enabled = trackRemoveEventButton.Enabled = commandsBox.Enabled = globalChangeVoicesButton.Enabled = tracks;

            tracksBox.DataSource = Enumerable.Range(1, SongPlayer.NumTracks).Select(i => $"Track {i}").ToList();
            remapsBox.Enabled = remapFromButton.Enabled = remapToButton.Enabled = tracks && remapsBox.Items.Count > 0;

            commandsBox.DataSource = Engine.GetCommands().Select(c => c.Name).ToList();

            if (!tracks)
                listView.Items.Clear();
        }
        void SelectItem(int index)
        {
            listView.Items[index].Selected = true;
            listView.Select();
            listView.EnsureVisible(index);
        }

        void ArgumentChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < 3; i++)
            {
                if (sender == argNumerics[i])
                {
                    var se = events[listView.SelectedIndices[0]];
                    object value = argNumerics[i].Value;
                    var m = se.Command.GetType().GetMember(argLabels[i].Text)[0];
                    if (m is FieldInfo f)
                        f.SetValue(se.Command, Convert.ChangeType(value, f.FieldType));
                    else if (m is PropertyInfo p)
                        p.SetValue(se.Command, Convert.ChangeType(value, p.PropertyType));
                    SongPlayer.RefreshSong();

                    var control = ActiveControl;
                    int index = listView.SelectedIndex;
                    LoadTrack(currentTrack);
                    SelectItem(index);
                    control.Select();

                    return;
                }
            }
        }
        void SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count != 1)
            {
                argLabels[0].Visible = argLabels[1].Visible = argLabels[2].Visible =
                    argNumerics[0].Visible = argNumerics[1].Visible = argNumerics[2].Visible = false;
            }
            else
            {
                var se = (SongEvent)listView.SelectedObject;
                var ignore = typeof(ICommand).GetMembers();
                var mi = se.Command == null ? new MemberInfo[0] : se.Command.GetType().GetMembers().Where(m => !ignore.Any(a => m.Name == a.Name) && (m is FieldInfo || m is PropertyInfo)).ToArray();
                for (int i = 0; i < 3; i++)
                {
                    argLabels[i].Visible = argNumerics[i].Visible = i < mi.Length;
                    if (argNumerics[i].Visible)
                    {
                        argLabels[i].Text = mi[i].Name;

                        argNumerics[i].ValueChanged -= ArgumentChanged;

                        dynamic m = mi[i];

                        argNumerics[i].Hexadecimal = se.Command is CallCommand || se.Command is GoToCommand;

                        TypeInfo valueType;
                        if (mi[i].MemberType == MemberTypes.Field)
                            valueType = m.FieldType;
                        else
                            valueType = m.PropertyType;
                        argNumerics[i].Maximum = valueType.DeclaredFields.Single(f => f.Name == "MaxValue").GetValue(m);
                        argNumerics[i].Minimum = valueType.DeclaredFields.Single(f => f.Name == "MinValue").GetValue(m);

                        object value = m.GetValue(se.Command);
                        argNumerics[i].Value = (decimal)Convert.ChangeType(value, TypeCode.Decimal);

                        argNumerics[i].ValueChanged += ArgumentChanged;
                    }
                }
            }
            argLabels[0].Parent.Refresh();
        }
    }
}
