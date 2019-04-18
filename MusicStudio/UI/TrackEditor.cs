using BrightIdeasSoftware;
using Kermalis.MusicStudio.Core;
using Kermalis.MusicStudio.Properties;
using Kermalis.MusicStudio.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Kermalis.MusicStudio.UI
{
    [DesignerCategory("")]
    class TrackEditor : ThemedForm
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

        public TrackEditor()
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
            c1 = new OLVColumn(Strings.TrackEditorEvent, "Command.Name");
            c2 = new OLVColumn(Strings.TrackEditorArguments, "Command.Arguments") { UseFiltering = false };
            c3 = new OLVColumn(Strings.TrackEditorOffset, "GetOffset") { AspectToStringFormat = "0x{0:X7}", UseFiltering = false };
            c4 = new OLVColumn(Strings.TrackEditorTicks, "AbsoluteTicks") { UseFiltering = false };
            c1.Width = c2.Width = c3.Width = 72;
            c4.Width = 45;
            c1.Hideable = c2.Hideable = c3.Hideable = c4.Hideable = false;
            c1.TextAlign = c2.TextAlign = c3.TextAlign = c4.TextAlign = HorizontalAlignment.Center;
            listView.AllColumns.AddRange(new OLVColumn[] { c1, c2, c3, c4 });
            listView.RebuildColumns();
            listView.SelectedIndexChanged += SelectedIndexChanged;
            listView.ItemActivate += ListView_ItemActivate;

            int h2 = (h / 3) - 4;
            var panel1 = new ThemedPanel { Location = new Point(306, 12), Size = new Size(w, h2) };
            var panel2 = new ThemedPanel { Location = new Point(306, 140), Size = new Size(w, h2) };
            var panel3 = new ThemedPanel { Location = new Point(306, 268), Size = new Size(w, h2) };

            // Arguments Info
            for (int i = 0; i < 3; i++)
            {
                int y = 17 + (33 * i);
                argLabels[i] = new ThemedLabel
                {
                    AutoSize = true,
                    Location = new Point(52, y + 3),
                    Text = string.Format(Strings.TrackEditorArgX, i + 1),
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
                Text = Strings.TrackEditorChangeVoices
            };
            trackChangeVoicesButton.Click += ChangeEvents;
            trackAddEventButton = new ThemedButton
            {
                Location = new Point(13, 30 + 25 + 5),
                Size = new Size(100, 25),
                Text = Strings.TrackEditorAddEvent
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
                Text = Strings.TrackEditorRemoveEvent
            };
            trackRemoveEventButton.Click += RemoveEvent;
            tracksBox.Enabled = trackChangeVoicesButton.Enabled = trackAddEventButton.Enabled = trackRemoveEventButton.Enabled = commandsBox.Enabled = false;
            trackChangeVoicesButton.Size = trackAddEventButton.Size = trackRemoveEventButton.Size = new Size(95, 25);
            var trackFromVoiceButton = new ThemedLabel { Location = new Point(115, 30 + 2 + 3), Text = Strings.TrackEditorFrom };
            trackVoiceArgs[0] = new ThemedNumeric { Location = new Point(149, 30 + 2) };
            var trackToVoiceButton = new ThemedLabel { Location = new Point(204, 30 + 2 + 3), Text = Strings.TrackEditorTo };
            trackVoiceArgs[1] = new ThemedNumeric { Location = new Point(224, 30 + 2) };
            trackVoiceArgs[0].Maximum = trackVoiceArgs[1].Maximum = 0xFF;
            trackVoiceArgs[0].Size = trackVoiceArgs[1].Size = new Size(45, 23);
            trackVoiceArgs[0].TextAlign = trackVoiceArgs[1].TextAlign = HorizontalAlignment.Center;
            trackFromVoiceButton.AutoSize = trackToVoiceButton.AutoSize = true;
            panel2.Controls.AddRange(new Control[] { tracksBox, trackChangeVoicesButton, trackFromVoiceButton, trackToVoiceButton, trackVoiceArgs[0], trackVoiceArgs[1], trackAddEventButton, commandsBox, trackRemoveEventButton });

            // Global controls
            remapsBox = new ComboBox
            {
                DataSource = Config.Instance.InstrumentRemaps.Keys.ToArray(),
                Location = new Point(4, 4),
                Size = new Size(100, 21)
            };
            remapFromButton = new ThemedButton
            {
                Location = new Point(116, 3),
                Text = Strings.TrackEditorFrom
            };
            remapFromButton.Click += (s, e) => ApplyRemap(true);
            remapToButton = new ThemedButton
            {
                Location = new Point(203, 3),
                Text = Strings.TrackEditorTo
            };
            remapToButton.Click += (s, e) => ApplyRemap(false);
            globalChangeVoicesButton = new ThemedButton
            {
                Location = new Point(13, 30),
                Size = new Size(95, 25),
                Text = Strings.TrackEditorChangeVoices
            };
            globalChangeVoicesButton.Click += ChangeAllEvents;
            var globalFromVoiceButton = new ThemedLabel { Location = new Point(115, 30 + 2 + 3), Text = Strings.TrackEditorFrom };
            globalVoiceArgs[0] = new ThemedNumeric { Location = new Point(149, 30 + 2) };
            var globalToVoiceButton = new ThemedLabel { Location = new Point(204, 30 + 2 + 3), Text = Strings.TrackEditorTo };
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
            Text = $"{Utils.ProgramName} ― {Strings.TrackEditorTitle}";

            UpdateTracks();
        }

        void ListView_ItemActivate(object sender, EventArgs e)
        {
            SongPlayer.Instance.SetSongPosition(((SongEvent)listView.SelectedItem.RowObject).AbsoluteTicks);
        }

        void AddEvent(object sender, EventArgs e)
        {
            var cmd = (ICommand)Activator.CreateInstance(Engine.GetCommands()[commandsBox.SelectedIndex].GetType());
            var ev = new SongEvent(int.MaxValue, cmd);
            int index = listView.SelectedIndex + 1;
            SongPlayer.Instance.Song.InsertEvent(ev, currentTrack, index);
            SongPlayer.Instance.RefreshSong();
            LoadTrack(currentTrack);
            SelectItem(index);
        }
        void RemoveEvent(object sender, EventArgs e)
        {
            if (listView.SelectedIndex == -1)
            {
                return;
            }
            SongPlayer.Instance.Song.RemoveEvent(currentTrack, listView.SelectedIndex);
            SongPlayer.Instance.RefreshSong();
            LoadTrack(currentTrack);
        }

        void RowColorer(OLVListItem item)
        {
            switch (((SongEvent)item.RowObject).Command)
            {
                case GoToCommand _:
                case CallCommand _:
                case ReturnCommand _:
                case FinishCommand _: item.BackColor = Color.MediumSpringGreen; break;
                case VoiceCommand _: item.BackColor = Color.DarkSalmon; break;
                case RestCommand _: item.BackColor = Color.PaleVioletRed; break;
                case KeyShiftCommand _:
                case NoteCommand _:
                case EndOfTieCommand _:
                case FreeNoteCommand _: item.BackColor = Color.SkyBlue; break;
                case ModDepthCommand _:
                case ModTypeCommand _: item.BackColor = Color.LightSteelBlue; break;
                case TuneCommand _:
                case BendCommand _:
                case BendRangeCommand _: item.BackColor = Color.MediumPurple; break;
                case PanpotCommand _:
                case LFODelayCommand _:
                case LFOSpeedCommand _: item.BackColor = Color.GreenYellow; break;
                case TempoCommand _: item.BackColor = Color.DeepSkyBlue; break;
                default: item.BackColor = Color.SteelBlue; break;
            }
        }

        void ApplyRemap(bool from)
        {
            bool changed = false;
            string remap = (string)remapsBox.SelectedItem;
            foreach (List<SongEvent> track in SongPlayer.Instance.Song.Commands)
            {
                foreach (SongEvent ev in track)
                {
                    if (ev.Command is VoiceCommand voice)
                    {
                        voice.Voice = Config.Instance.GetRemap(voice.Voice, remap, from);
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                SongPlayer.Instance.RefreshSong();
                LoadTrack(currentTrack);
            }
        }
        void ChangeEvents(object sender, EventArgs e)
        {
            bool changed = false;
            foreach (SongEvent ev in events)
            {
                if (sender == trackChangeVoicesButton && ev.Command is VoiceCommand voice && voice.Voice == trackVoiceArgs[0].Value)
                {
                    voice.Voice = (byte)trackVoiceArgs[1].Value;
                    changed = true;
                }
            }
            if (changed)
            {
                SongPlayer.Instance.RefreshSong();
                LoadTrack(currentTrack);
            }
        }
        void ChangeAllEvents(object sender, EventArgs e)
        {
            bool changed = false;
            foreach (List<SongEvent> track in SongPlayer.Instance.Song.Commands)
            {
                foreach (SongEvent ev in track)
                {
                    if (sender == globalChangeVoicesButton && ev.Command is VoiceCommand voice && voice.Voice == globalVoiceArgs[0].Value)
                    {
                        voice.Voice = (byte)globalVoiceArgs[1].Value;
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                SongPlayer.Instance.RefreshSong();
                LoadTrack(currentTrack);
            }
        }

        void LoadTrack(int track)
        {
            currentTrack = track;
            events = SongPlayer.Instance.Song.Commands[track];
            listView.SetObjects(events);
            SelectedIndexChanged(null, null);
        }
        void TracksBox_SelectedIndexChanged(object sender, EventArgs e) => LoadTrack(tracksBox.SelectedIndex);
        public void UpdateTracks()
        {
            bool tracks = SongPlayer.Instance.NumTracks > 0;
            tracksBox.Enabled = trackChangeVoicesButton.Enabled = trackAddEventButton.Enabled = trackRemoveEventButton.Enabled = commandsBox.Enabled = globalChangeVoicesButton.Enabled = tracks;

            // Track 1, Track 2, ...
            tracksBox.DataSource = Enumerable.Range(1, SongPlayer.Instance.NumTracks).Select(i => string.Format(Strings.TrackEditorTrackX, i)).ToList();
            remapsBox.Enabled = remapFromButton.Enabled = remapToButton.Enabled = tracks && remapsBox.Items.Count > 0;

            commandsBox.DataSource = Engine.GetCommands().Select(c => c.Name).ToList();

            if (!tracks)
            {
                listView.Items.Clear();
            }
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
                    SongEvent se = events[listView.SelectedIndices[0]];
                    object value = argNumerics[i].Value;
                    MemberInfo m = se.Command.GetType().GetMember(argLabels[i].Text)[0];
                    if (m is FieldInfo f)
                    {
                        f.SetValue(se.Command, Convert.ChangeType(value, f.FieldType));
                    }
                    else if (m is PropertyInfo p)
                    {
                        p.SetValue(se.Command, Convert.ChangeType(value, p.PropertyType));
                    }

                    SongPlayer.Instance.RefreshSong();

                    Control control = ActiveControl;
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
                MemberInfo[] ignore = typeof(ICommand).GetMembers();
                MemberInfo[] mi = se.Command == null ? new MemberInfo[0] : se.Command.GetType().GetMembers().Where(m => !ignore.Any(a => m.Name == a.Name) && (m is FieldInfo || m is PropertyInfo)).ToArray();
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
                        {
                            valueType = m.FieldType;
                        }
                        else
                        {
                            valueType = m.PropertyType;
                        }
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
