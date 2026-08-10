using BrightIdeasSoftware;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
class SoundBankEditor : ThemedForm
{
    private readonly ObjectListView _voicesListView, _subVoicesListView;
    private readonly ThemedPanel _voicePanel;
    private readonly ThemedLabel _bytesLabel, _addressLabel,
        _voiceALabel, _voiceDLabel, _voiceSLabel, _voiceRLabel;
    private readonly ValueTextBox _addressValue,
        _voiceAValue, _voiceDValue, _voiceSValue, _voiceRValue;
    private SoundBank? _table;
    private int _voiceIndex; // The voice entry being edited

    public SoundBankEditor()
    {
        int w = (600 / 2) - 12 - 6, h = 400 - 12 - 11;
        // Main SoundBank view
        _voicesListView = new ObjectListView
        {
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            HideSelection = false,
            Location = new Point(12, 12),
            MultiSelect = false,
            ShowGroups = false,
            Size = new Size(w, h)
        };
        _voicesListView.FormatRow += FormatRow;
        OLVColumn c1, c2, c3;
        c1 = new OLVColumn("#", "");
        c2 = new OLVColumn(Strings.PlayerType, "ToString");
        c3 = new OLVColumn(Strings.TrackEditorOffset, "GetOffset") { AspectToStringFormat = "0x{0:X7}" };
        c1.Width = 45;
        c2.Width = c3.Width = 108;
        c1.Hideable = c2.Hideable = c3.Hideable = false;
        c1.TextAlign = c2.TextAlign = c3.TextAlign = HorizontalAlignment.Center;
        _voicesListView.AllColumns.AddRange([c1, c2, c3]);
        _voicesListView.RebuildColumns();
        _voicesListView.SelectedIndexChanged += MainIndexChanged;

        int h2 = (h / 2) - 5;
        // View of the selected voice's sub-voices
        _subVoicesListView = new ObjectListView
        {
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            HideSelection = false,
            Location = new Point(306, 12),
            MultiSelect = false,
            ShowGroups = false,
            Size = new Size(w, h2)
        };
        _subVoicesListView.FormatRow += FormatRow;
        c1 = new OLVColumn("#", "");
        c2 = new OLVColumn(Strings.PlayerType, "ToString");
        c3 = new OLVColumn(Strings.TrackEditorOffset, "GetOffset") { AspectToStringFormat = "0x{0:X7}" };
        c1.Width = 45;
        c2.Width = c3.Width = 108;
        c1.Hideable = c2.Hideable = c3.Hideable = false;
        c1.TextAlign = c2.TextAlign = c3.TextAlign = HorizontalAlignment.Center;
        _subVoicesListView.AllColumns.AddRange([c1, c2, c3]);
        _subVoicesListView.RebuildColumns();
        _subVoicesListView.SelectedIndexChanged += SubIndexChanged;

        // Panel to edit a voice
        _voicePanel = new ThemedPanel { Location = new Point(306, 206), Size = new Size(w, h2) };

        // Panel controls
        _bytesLabel = new ThemedLabel { Location = new Point(2, 2) };
        _addressLabel = new ThemedLabel { Location = new Point(2, 130), Text = $"{Strings.SoundBankEditorAddress}:" };
        _voiceALabel = new ThemedLabel { Location = new Point(0 * w / 4 + 2, 160), Text = "A:" };
        _voiceDLabel = new ThemedLabel { Location = new Point(1 * w / 4 + 2, 160), Text = "D:" };
        _voiceSLabel = new ThemedLabel { Location = new Point(2 * w / 4 + 2, 160), Text = "S:" };
        _voiceRLabel = new ThemedLabel { Location = new Point(3 * w / 4 + 2, 160), Text = "R:" };
        _bytesLabel.AutoSize = _addressLabel.AutoSize =
            _voiceALabel.AutoSize = _voiceDLabel.AutoSize = _voiceSLabel.AutoSize = _voiceRLabel.AutoSize = true;

        _addressValue = new ValueTextBox { Location = new Point(w / 5, 127), Size = new Size(78, 24) };
        _voiceAValue = new ValueTextBox { Location = new Point(0 * w / 4 + 20, 157) };
        _voiceDValue = new ValueTextBox { Location = new Point(1 * w / 4 + 20, 157) };
        _voiceSValue = new ValueTextBox { Location = new Point(2 * w / 4 + 20, 157) };
        _voiceRValue = new ValueTextBox { Location = new Point(3 * w / 4 + 20, 157) };
        _voiceAValue.Size = _voiceDValue.Size = _voiceSValue.Size = _voiceRValue.Size = new Size(44, 22);
        _voiceAValue.ValueChanged += ArgumentChanged; _voiceDValue.ValueChanged += ArgumentChanged; _voiceSValue.ValueChanged += ArgumentChanged; _voiceRValue.ValueChanged += ArgumentChanged;

        _voicePanel.Controls.AddRange([ _bytesLabel, _addressLabel, _addressValue,
            _voiceALabel, _voiceDLabel, _voiceSLabel, _voiceRLabel,
            _voiceAValue, _voiceDValue, _voiceSValue, _voiceRValue ]);

        ClientSize = new Size(600, 400);
        Controls.AddRange([_voicesListView, _subVoicesListView, _voicePanel]);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        UpdateTable();
    }

    private void FormatRow(object? sender, FormatRowEventArgs e)
    {
        // Auto-number
        e.Item.Text = e.RowIndex.ToString();
        // Auto-color
        if (e.ListView == _voicesListView)
        {
            HSLColor color = new(GlobalConfig.Instance.Colors[(byte)e.RowIndex]);
            e.Item.BackColor = GlobalConfig.Instance.Colors[(byte)e.RowIndex];
            if (color.Lightness <= 0.6 && (color.R <= 0.7 && color.G <= 0.7))
            {
                e.Item.ForeColor = Color.White;
            }
        }
    }

    // Sets the subVoicesListView objects if the selected voice has sub-voices
    // Also enables editing of the selected voice
    private void MainIndexChanged(object? sender, EventArgs e)
    {
        if (_voicesListView.SelectedIndices.Count != 1)
        {
            return;
        }

        var elementSelected = _table!.ElementAt(_voicesListView.SelectedIndex);
        _subVoicesListView.SetObjects(elementSelected.GetSubVoices());
        SetVoice(_voicesListView.SelectedIndex);
    }
    private void SubIndexChanged(object? sender, EventArgs e)
    {
        if (_subVoicesListView.SelectedIndices.Count != 1)
        {
            return;
        }

        SetVoice(_subVoicesListView.SelectedIndex);
    }
    public void UpdateTable()
    {
        _voicesListView.SetObjects(_table = Engine.Instance!.Player.LoadedSong!.Bank);
        _subVoicesListView.ClearObjects();
        Text = $"{ConfigUtils.PROGRAM_NAME} ― {Strings.SoundBankEditorTitle} (0x{_table.Offset:X7})";
        _voicesListView.SelectedIndex = 0;
    }

    // Places voice info into the panel
    private void SetVoice(int index)
    {
        _subVoicesListView.SelectedIndexChanged -= SubIndexChanged;
        _addressValue.ValueChanged -= ArgumentChanged;
        _voiceAValue.ValueChanged -= ArgumentChanged; _voiceDValue.ValueChanged -= ArgumentChanged; _voiceSValue.ValueChanged -= ArgumentChanged; _voiceRValue.ValueChanged -= ArgumentChanged;
        _addressValue.Visible = _addressLabel.Visible =
            _voiceAValue.Visible = _voiceDValue.Visible = _voiceSValue.Visible = _voiceRValue.Visible =
        _voiceALabel.Visible = _voiceDLabel.Visible = _voiceSLabel.Visible = _voiceRLabel.Visible = false;

        IVoiceInfo voice = _table!.ElementAt(index);
        IVoiceInfo[] subs = [.. voice.GetSubVoices()];

        if (Engine.Instance is MP2KEngine && _table.HasADSR(index))
        {
            _bytesLabel.Text = _table.GetBytesToString(index);

            #region Addresses (Direct, Key Split, Drum, Wave)

            if (_table.IsValidVoiceAddress(index))
            {
                _addressValue.Hexadecimal = true;
                _addressValue.Maximum = Engine.Instance!.Config.ROM!.Length - 1;
                _addressValue.Visible = _addressLabel.Visible = true;
                _addressValue.Value = _table.GetVoiceAddress(index);
            }

            #endregion

            #region ADSR (everything except Key Split, Drum and invalids)

            if (_table.IsValidADSR(index))
            {
                bool isPCM8 = !_table.IsPSGInstrument(index);
                _voiceAValue.Hexadecimal = _voiceDValue.Hexadecimal = _voiceSValue.Hexadecimal = _voiceRValue.Hexadecimal = false;
                _voiceAValue.Maximum = _voiceDValue.Maximum = _voiceRValue.Maximum = isPCM8 ? byte.MaxValue : 0x7;
                _voiceSValue.Maximum = isPCM8 ? byte.MaxValue : 0xF;
                _voiceAValue.Minimum = _voiceDValue.Minimum = _voiceSValue.Minimum = _voiceRValue.Minimum = byte.MinValue;
                _voiceAValue.Visible = _voiceDValue.Visible = _voiceSValue.Visible = _voiceRValue.Visible =
                _voiceALabel.Visible = _voiceDLabel.Visible = _voiceSLabel.Visible = _voiceRLabel.Visible = true;
                _table.GetADSRValues(index, out byte a, out byte d, out byte s, out byte r);
                _voiceAValue.Value = a;
                _voiceDValue.Value = d;
                _voiceSValue.Value = s;
                _voiceRValue.Value = r;
            }

            #endregion
        }
        //else if (voice.Voice is MLSSVoice mlss)
        //{
        //    if (mlss.Entries.Length > 0)
        //    {
        //        if (subIndex == -1)
        //        {
        //            subIndex = 0;
        //        }
        //        MLSSVoiceEntry mlssEntry = mlss.Entries[subIndex];
        //        entry = mlssEntry;

        //        bytesLabel.Text = mlssEntry.GetBytesToString();

        //        #region Last 4 values are probably ADSR

        //        voiceAValue.Hexadecimal = voiceDValue.Hexadecimal = voiceSValue.Hexadecimal = voiceRValue.Hexadecimal = true;
        //        voiceAValue.Maximum = voiceDValue.Maximum = voiceSValue.Maximum = voiceRValue.Maximum = byte.MaxValue;
        //        voiceAValue.Minimum = voiceDValue.Minimum = voiceSValue.Minimum = voiceRValue.Minimum = byte.MinValue;
        //        voiceAValue.Visible = voiceDValue.Visible = voiceSValue.Visible = voiceRValue.Visible =
        //        voiceALabel.Visible = voiceDLabel.Visible = voiceSLabel.Visible = voiceRLabel.Visible = true;

        //        voiceAValue.Value = mlssEntry.Unknown1;
        //        voiceDValue.Value = mlssEntry.Unknown2;
        //        voiceSValue.Value = mlssEntry.Unknown3;
        //        voiceRValue.Value = mlssEntry.Unknown4;

        //        #endregion
        //    }
        //}

        _subVoicesListView.SelectedIndex = 0;
        _subVoicesListView.SelectedIndexChanged += SubIndexChanged;
        _addressValue.ValueChanged += ArgumentChanged;
        _voiceAValue.ValueChanged += ArgumentChanged; _voiceDValue.ValueChanged += ArgumentChanged; _voiceSValue.ValueChanged += ArgumentChanged; _voiceRValue.ValueChanged += ArgumentChanged;
    }

    private void ArgumentChanged(object? sender, EventArgs e)
    {
        if (_table!.HasADSR(_voiceIndex!))
        {
            if (_addressValue.Visible)
            {
                _table.SetAddressPointer(_voiceIndex!, (int)_addressValue.Value);
            }
            if (_voiceAValue.Visible)
            {
                byte a = (byte)_voiceAValue.Value;
                byte d = (byte)_voiceDValue.Value;
                byte s = (byte)_voiceSValue.Value;
                byte r = (byte)_voiceRValue.Value;
                _table.SetADSRValues(_voiceIndex!, in a, in d, in s, in r);
            }

            _bytesLabel.Text = _table.GetBytesToString(_voiceIndex!);
        }
        //else if (entry is MLSSVoiceEntry mlssEntry)
        //{
        //    mlssEntry.Unknown1 = (byte)voiceAValue.Value;
        //    mlssEntry.Unknown2 = (byte)voiceDValue.Value;
        //    mlssEntry.Unknown3 = (byte)voiceSValue.Value;
        //    mlssEntry.Unknown4 = (byte)voiceRValue.Value;

        //    bytesLabel.Text = mlssEntry.GetBytesToString();
        //}
    }
}
