using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
internal class AssemblerDialog : ThemedForm
{
    private Assembler? _assembler;
    private readonly LoadedSong _song = (LoadedSong)Engine.Instance!.Player.LoadedSong!;
    private readonly ThemedButton _previewButton;
    private readonly ValueTextBox _offsetValueBox;
    private readonly ThemedLabel _sizeLabel;
    private readonly ThemedTextBox _headerLabelTextBox;
    private readonly DataGridView _addedDefsGrid;

    public AssemblerDialog()
    {
        var openButton = new ThemedButton
        {
            Location = new Point(150, 0),
            Text = Strings.AssemblerOpenFile
        };
        openButton.Click += OpenASM;
        _previewButton = new ThemedButton
        {
            Enabled = false,
            Location = new Point(150, 50),
            Size = new Size(120, 23),
            Text = Strings.AssemblerPreviewSong
        };
        _previewButton.Click += PreviewSong;
        _sizeLabel = new ThemedLabel
        {
            Location = new Point(0, 100),
            Size = new Size(150, 23)
        };
        _offsetValueBox = new ValueTextBox
        {
            Hexadecimal = true,
            Maximum = Engine.Instance!.Config.ROM!.LongLength - 1
        };
        _headerLabelTextBox = new ThemedTextBox { Location = new Point(0, 50), Size = new Size(150, 22) };
        _addedDefsGrid = new DataGridView
        {
            ColumnCount = 2,
            Location = new Point(0, 150),
            MultiSelect = false
        };
        _addedDefsGrid.Columns[0].Name = Strings.AssemblerDefinition;
        _addedDefsGrid.Columns[1].Name = Strings.AssemblerValue;
        _addedDefsGrid.Columns[1].DefaultCellStyle.NullValue = "0";
        _addedDefsGrid.Rows.Add(["voicegroup000", $"0x{Engine.Instance!.Player.LoadedSong!.Bank.Offset + GBAUtils.CARTRIDGE_OFFSET:X7}"]);
        _addedDefsGrid.CellValueChanged += AddedDefsGrid_CellValueChanged;

        Controls.AddRange(new Control[] { openButton, _previewButton, _sizeLabel, _offsetValueBox, _headerLabelTextBox, _addedDefsGrid });
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Size = new Size(600, 400);
        Text = $"VG Music Studio ― {Strings.AssemblerTitle}";
    }

    void AddedDefsGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        DataGridViewCell cell = _addedDefsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (cell.Value == null)
        {
            return;
        }

        if (e.ColumnIndex == 0)
        {
            if (char.IsDigit(cell.Value.ToString()![0]))
            {
                FlexibleMessageBox.Show(Strings.AssemblerErrorDefinitionDigit, Strings.ErrorTitleAssembler, MessageBoxButtons.OK, MessageBoxIcon.Error);
                cell.Value = cell.Value.ToString()!.Substring(1);
            }
        }
        else
        {
            if (!ConfigUtils.TryParseValue(cell.Value.ToString()!, 0, long.MaxValue, out long val))
            {
                FlexibleMessageBox.Show(string.Format(Strings.AssemblerErrorInvalidValue, cell.Value), Strings.ErrorTitleAssembler, MessageBoxButtons.OK, MessageBoxIcon.Error);
                cell.Value = null;
            }
        }
    }
    void PreviewSong(object? sender, EventArgs e)
    {
        ((MainForm)Owner!).PreviewSong(_song, Path.GetFileName(_assembler!.FileName));
    }
    void OpenASM(object? sender, EventArgs e)
    {
        var d = new OpenFileDialog { Title = Strings.TitleOpenASM, Filter = $"{Strings.FilterOpenASM}|*.s" };
        if (d.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            var s = new Dictionary<string, int>();
            foreach (DataGridViewRow r in _addedDefsGrid.Rows.Cast<DataGridViewRow>())
            {
                if (r.Cells[0].Value == null || r.Cells[1].Value == null)
                {
                    continue;
                }
                s.Add(r.Cells[0].Value.ToString()!, (int)ConfigUtils.ParseValue(nameof(r.Index), r.Cells[1].Value.ToString()!, 0, long.MaxValue));
            }
            _song.OpenASM(_assembler = new Assembler(d.FileName, (int)(GBAUtils.CARTRIDGE_OFFSET + _offsetValueBox.Value), EndianBinaryIO.Endianness.LittleEndian, s),
                _headerLabelTextBox.Text = Assembler.FixLabel(Path.GetFileNameWithoutExtension(d.FileName)));
            _sizeLabel.Text = string.Format(Strings.AssemblerSizeInBytes, _assembler.BinaryLength);
            _previewButton.Enabled = true;
        }
        catch (Exception ex)
        {
            FlexibleMessageBox.Show(ex.Message, Strings.ErrorTitleAssembler, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
