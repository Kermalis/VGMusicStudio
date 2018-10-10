using BrightIdeasSoftware;
using GBAMusicStudio.Core;
using GBAMusicStudio.Properties;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [DesignerCategory("")]
    class VoiceTableEditor : ThemedForm
    {
        readonly ObjectListView voicesListView, subVoicesListView;
        readonly ThemedPanel voicePanel;
        readonly ThemedLabel bytesLabel, addressLabel,
            voiceALabel, voiceDLabel, voiceSLabel, voiceRLabel;
        readonly ValueTextBox addressValue,
            voiceAValue, voiceDValue, voiceSValue, voiceRValue;
        VoiceTable table;
        object entry; // The voice entry being edited

        public VoiceTableEditor()
        {
            int w = (600 / 2) - 12 - 6, h = 400 - 12 - 11;
            // Main VoiceTable view
            voicesListView = new ObjectListView
            {
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                Location = new Point(12, 12),
                MultiSelect = false,
                ShowGroups = false,
                Size = new Size(w, h)
            };
            voicesListView.FormatRow += FormatRow;
            OLVColumn c1, c2, c3;
            c1 = new OLVColumn("#", "");
            c2 = new OLVColumn(Strings.PlayerType, "ToString");
            c3 = new OLVColumn(Strings.TrackEditorOffset, "GetOffset") { AspectToStringFormat = "0x{0:X7}" };
            c1.Width = 45;
            c2.Width = c3.Width = 108;
            c1.Hideable = c2.Hideable = c3.Hideable = false;
            c1.TextAlign = c2.TextAlign = c3.TextAlign = HorizontalAlignment.Center;
            voicesListView.AllColumns.AddRange(new OLVColumn[] { c1, c2, c3 });
            voicesListView.RebuildColumns();
            voicesListView.SelectedIndexChanged += MainIndexChanged;

            int h2 = (h / 2) - 5;
            // View of the selected voice's sub-voices
            subVoicesListView = new ObjectListView
            {
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                Location = new Point(306, 12),
                MultiSelect = false,
                ShowGroups = false,
                Size = new Size(w, h2)
            };
            subVoicesListView.FormatRow += FormatRow;
            c1 = new OLVColumn("#", "");
            c2 = new OLVColumn(Strings.PlayerType, "ToString");
            c3 = new OLVColumn(Strings.TrackEditorOffset, "GetOffset") { AspectToStringFormat = "0x{0:X7}" };
            c1.Width = 45;
            c2.Width = c3.Width = 108;
            c1.Hideable = c2.Hideable = c3.Hideable = false;
            c1.TextAlign = c2.TextAlign = c3.TextAlign = HorizontalAlignment.Center;
            subVoicesListView.AllColumns.AddRange(new OLVColumn[] { c1, c2, c3 });
            subVoicesListView.RebuildColumns();
            subVoicesListView.SelectedIndexChanged += SubIndexChanged;

            // Panel to edit a voice
            voicePanel = new ThemedPanel { Location = new Point(306, 206), Size = new Size(w, h2) };

            // Panel controls
            bytesLabel = new ThemedLabel { Location = new Point(2, 2) };
            addressLabel = new ThemedLabel { Location = new Point(2, 130), Text = $"{Strings.VoiceTableEditorAddress}:" };
            voiceALabel = new ThemedLabel { Location = new Point(0 * w / 4 + 2, 160), Text = "A:" };
            voiceDLabel = new ThemedLabel { Location = new Point(1 * w / 4 + 2, 160), Text = "D:" };
            voiceSLabel = new ThemedLabel { Location = new Point(2 * w / 4 + 2, 160), Text = "S:" };
            voiceRLabel = new ThemedLabel { Location = new Point(3 * w / 4 + 2, 160), Text = "R:" };
            bytesLabel.AutoSize = addressLabel.AutoSize =
                voiceALabel.AutoSize = voiceDLabel.AutoSize = voiceSLabel.AutoSize = voiceRLabel.AutoSize = true;

            addressValue = new ValueTextBox { Location = new Point(w / 5, 127), Size = new Size(78, 24) };
            voiceAValue = new ValueTextBox { Location = new Point(0 * w / 4 + 20, 157) };
            voiceDValue = new ValueTextBox { Location = new Point(1 * w / 4 + 20, 157) };
            voiceSValue = new ValueTextBox { Location = new Point(2 * w / 4 + 20, 157) };
            voiceRValue = new ValueTextBox { Location = new Point(3 * w / 4 + 20, 157) };
            voiceAValue.Size = voiceDValue.Size = voiceSValue.Size = voiceRValue.Size = new Size(44, 22);
            voiceAValue.ValueChanged += ArgumentChanged; voiceDValue.ValueChanged += ArgumentChanged; voiceSValue.ValueChanged += ArgumentChanged; voiceRValue.ValueChanged += ArgumentChanged;

            voicePanel.Controls.AddRange(new Control[] { bytesLabel, addressLabel, addressValue,
                voiceALabel, voiceDLabel, voiceSLabel, voiceRLabel,
                voiceAValue, voiceDValue, voiceSValue, voiceRValue });

            ClientSize = new Size(600, 400);
            Controls.AddRange(new Control[] { voicesListView, subVoicesListView, voicePanel });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            UpdateTable();
        }

        void FormatRow(object sender, FormatRowEventArgs e)
        {
            // Auto-number
            e.Item.Text = e.RowIndex.ToString();
            // Auto-color
            if (e.ListView == voicesListView)
            {
                var color = Config.Instance.GetColor((byte)e.RowIndex, ROM.Instance.Game.Remap, true);
                e.Item.BackColor = color;
                if (color.Luminosity <= 100)
                    e.Item.ForeColor = Color.White;
            }
        }

        // Sets the subVoicesListView objects if the selected voice has sub-voices
        // Also enables editing of the selected voice
        void MainIndexChanged(object sender, EventArgs e)
        {
            if (voicesListView.SelectedIndices.Count != 1) return;

            subVoicesListView.SetObjects(table[voicesListView.SelectedIndex].GetSubVoices());
            SetVoice(voicesListView.SelectedIndex, subVoicesListView.SelectedIndex);
        }
        void SubIndexChanged(object sender, EventArgs e)
        {
            if (subVoicesListView.SelectedIndices.Count != 1) return;

            SetVoice(voicesListView.SelectedIndex, subVoicesListView.SelectedIndex);
        }
        public void UpdateTable()
        {
            voicesListView.SetObjects(table = SongPlayer.Instance.Song.VoiceTable);
            subVoicesListView.ClearObjects();
            Text = $"GBA Music Studio ― {Strings.VoiceTableEditorTitle} (0x{table.GetOffset():X7})";
            voicesListView.SelectedIndex = 0;
        }

        // Places voice info into the panel
        void SetVoice(int mainIndex, int subIndex)
        {
            subVoicesListView.SelectedIndexChanged -= SubIndexChanged;
            addressValue.ValueChanged -= ArgumentChanged;
            voiceAValue.ValueChanged -= ArgumentChanged; voiceDValue.ValueChanged -= ArgumentChanged; voiceSValue.ValueChanged -= ArgumentChanged; voiceRValue.ValueChanged -= ArgumentChanged;
            addressValue.Visible = addressLabel.Visible =
                voiceAValue.Visible = voiceDValue.Visible = voiceSValue.Visible = voiceRValue.Visible =
            voiceALabel.Visible = voiceDLabel.Visible = voiceSLabel.Visible = voiceRLabel.Visible = false;

            WrappedVoice voice = table[mainIndex];
            var subs = voice.GetSubVoices().ToArray();

            if (voice.Voice is M4AVoiceEntry m4aEntry)
            {
                if (subIndex != -1)
                    m4aEntry = (M4AVoiceEntry)((WrappedVoice)subs[subIndex]).Voice;
                entry = m4aEntry;
                
                bytesLabel.Text = m4aEntry.GetBytesString();

                var flags = (M4AVoiceFlags)m4aEntry.Type;
                var type = (M4AVoiceType)(m4aEntry.Type & 0x7);

                #region Addresses (Direct, Key Split, Drum, Wave)

                if (type == M4AVoiceType.Direct || type == M4AVoiceType.Wave || flags == M4AVoiceFlags.KeySplit || flags == M4AVoiceFlags.Drum)
                {
                    addressValue.Hexadecimal = true;
                    addressValue.Maximum = ROM.Capacity - 1;
                    addressValue.Visible = addressLabel.Visible = true;
                    addressValue.Value = m4aEntry.Address - ROM.Pak;
                }

                #endregion

                #region ADSR (everything except Key Split, Drum and invalids)

                if (flags != M4AVoiceFlags.KeySplit && flags != M4AVoiceFlags.Drum && !m4aEntry.IsInvalid())
                {
                    bool bDirect = !m4aEntry.IsGBInstrument();
                    voiceAValue.Hexadecimal = voiceDValue.Hexadecimal = voiceSValue.Hexadecimal = voiceRValue.Hexadecimal = false;
                    voiceAValue.Maximum = voiceDValue.Maximum = voiceRValue.Maximum = bDirect ? byte.MaxValue : 0x7;
                    voiceSValue.Maximum = bDirect ? byte.MaxValue : 0xF;
                    voiceAValue.Minimum = voiceDValue.Minimum = voiceSValue.Minimum = voiceRValue.Minimum = byte.MinValue;
                    voiceAValue.Visible = voiceDValue.Visible = voiceSValue.Visible = voiceRValue.Visible =
                    voiceALabel.Visible = voiceDLabel.Visible = voiceSLabel.Visible = voiceRLabel.Visible = true;
                    voiceAValue.Value = m4aEntry.ADSR.A;
                    voiceDValue.Value = m4aEntry.ADSR.D;
                    voiceSValue.Value = m4aEntry.ADSR.S;
                    voiceRValue.Value = m4aEntry.ADSR.R;
                }

                #endregion
            }
            else if (voice.Voice is MLSSVoice mlss)
            {
                if (mlss.Entries.Length > 0)
                {
                    if (subIndex == -1)
                        subIndex = 0;
                    var mlssEntry = mlss.Entries[subIndex];
                    entry = mlssEntry;

                    bytesLabel.Text = mlssEntry.GetBytesString();

                    #region Last 4 values are probably ADSR

                    voiceAValue.Hexadecimal = voiceDValue.Hexadecimal = voiceSValue.Hexadecimal = voiceRValue.Hexadecimal = true;
                    voiceAValue.Maximum = voiceDValue.Maximum = voiceSValue.Maximum = voiceRValue.Maximum = byte.MaxValue;
                    voiceAValue.Minimum = voiceDValue.Minimum = voiceSValue.Minimum = voiceRValue.Minimum = byte.MinValue;
                    voiceAValue.Visible = voiceDValue.Visible = voiceSValue.Visible = voiceRValue.Visible =
                    voiceALabel.Visible = voiceDLabel.Visible = voiceSLabel.Visible = voiceRLabel.Visible = true;

                    voiceAValue.Value = mlssEntry.Unknown1;
                    voiceDValue.Value = mlssEntry.Unknown2;
                    voiceSValue.Value = mlssEntry.Unknown3;
                    voiceRValue.Value = mlssEntry.Unknown4;

                    #endregion
                }
            }

            subVoicesListView.SelectedIndex = subIndex;
            subVoicesListView.SelectedIndexChanged += SubIndexChanged;
            addressValue.ValueChanged += ArgumentChanged;
            voiceAValue.ValueChanged += ArgumentChanged; voiceDValue.ValueChanged += ArgumentChanged; voiceSValue.ValueChanged += ArgumentChanged; voiceRValue.ValueChanged += ArgumentChanged;
        }

        void ArgumentChanged(object sender, EventArgs e)
        {
            if (entry is M4AVoiceEntry m4aEntry)
            {
                if (addressValue.Visible)
                {
                    m4aEntry.Address = (int)addressValue.Value + ROM.Pak;
                }
                if (voiceAValue.Visible)
                {
                    m4aEntry.ADSR.A = (byte)voiceAValue.Value;
                    m4aEntry.ADSR.D = (byte)voiceDValue.Value;
                    m4aEntry.ADSR.S = (byte)voiceSValue.Value;
                    m4aEntry.ADSR.R = (byte)voiceRValue.Value;
                }

                bytesLabel.Text = m4aEntry.GetBytesString();
            }
            else if (entry is MLSSVoiceEntry mlssEntry)
            {
                mlssEntry.Unknown1 = (byte)voiceAValue.Value;
                mlssEntry.Unknown2 = (byte)voiceDValue.Value;
                mlssEntry.Unknown3 = (byte)voiceSValue.Value;
                mlssEntry.Unknown4 = (byte)voiceRValue.Value;

                bytesLabel.Text = mlssEntry.GetBytesString();
            }
        }
    }
}
