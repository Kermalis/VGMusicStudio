using GBAMusicStudio.Core;
using GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [DesignerCategory("")]
    class AssemblerDialog : ThemedForm
    {
        Assembler assembler;
        readonly ThemedButton previewButton;
        readonly ValueTextBox offsetValueBox;
        readonly ThemedLabel sizeLabel;
        readonly ThemedTextBox headerLabelTextBox;
        readonly DataGridView addedDefsGrid;

        public AssemblerDialog()
        {
            var openButton = new ThemedButton
            {
                Location = new Point(150, 0),
                Text = "Open File"
            };
            openButton.Click += OpenASM;
            previewButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(150, 50),
                Text = "Preview Song"
            };
            previewButton.Click += PreviewASM;
            sizeLabel = new ThemedLabel
            {
                Location = new Point(0, 100),
                Size = new Size(150, 23)
            };
            offsetValueBox = new ValueTextBox
            {
                Hexadecimal = true,
                Maximum = ROM.Capacity - 1
            };
            headerLabelTextBox = new ThemedTextBox { Location = new Point(0, 50), Size = new Size(150, 22) };
            addedDefsGrid = new DataGridView
            {
                ColumnCount = 2,
                Location = new Point(0, 150),
                MultiSelect = false
            };
            addedDefsGrid.Columns[0].Name = "Definition";
            addedDefsGrid.Columns[1].Name = "Value";
            addedDefsGrid.Columns[1].DefaultCellStyle.NullValue = "0";
            addedDefsGrid.Rows.Add(new string[] { "voicegroup000", $"0x{SongPlayer.Instance.Song.VoiceTable.Offset:X7}" });
            addedDefsGrid.CellValueChanged += AddedDefsGrid_CellValueChanged;

            Controls.AddRange(new Control[] { openButton, previewButton, sizeLabel, offsetValueBox, headerLabelTextBox, addedDefsGrid });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Size = new Size(600, 400);
            Text = "GBA Music Studio ― Assembler";
        }

        void AddedDefsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var cell = addedDefsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.Value == null) return;

            if (e.ColumnIndex == 0)
            {
                if (char.IsDigit(cell.Value.ToString()[0]))
                {
                    FlexibleMessageBox.Show("Definitions cannot start with a digit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cell.Value = cell.Value.ToString().Substring(1);
                }
            }
            else
            {
                if (!Utils.TryParseValue(cell.Value.ToString(), out long val))
                {
                    FlexibleMessageBox.Show("Invalid value: " + cell.Value.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cell.Value = null;
                }
            }
        }
        void PreviewASM(object sender, EventArgs e)
        {
            ((MainForm)Owner).PreviewASM(assembler, headerLabelTextBox.Text, Path.GetFileName(assembler.FileName));
        }
        void OpenASM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = "Open Assembly", Filter = "Assembly files|*.s" };
            if (d.ShowDialog() != DialogResult.OK) return;

            try
            {
                var s = new Dictionary<string, int>();
                foreach (var r in addedDefsGrid.Rows.Cast<DataGridViewRow>())
                {
                    if (r.Cells[0].Value == null || r.Cells[1].Value == null)
                        continue;
                    s.Add(r.Cells[0].Value.ToString(), (int)Utils.ParseValue(r.Cells[1].Value.ToString()));
                }
                assembler = new Assembler(d.FileName, (uint)(ROM.Pak + offsetValueBox.Value), s);
                headerLabelTextBox.Text = Assembler.FixLabel(Path.GetFileNameWithoutExtension(d.FileName));
                sizeLabel.Text = $"Size in bytes: {assembler.BinaryLength}";
                previewButton.Enabled = true;
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, "Error Assembling File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
