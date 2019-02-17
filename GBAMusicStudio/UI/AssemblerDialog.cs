using Kermalis.GBAMusicStudio.Core;
using Kermalis.GBAMusicStudio.Properties;
using Kermalis.GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kermalis.GBAMusicStudio.UI
{
    [DesignerCategory("")]
    class AssemblerDialog : ThemedForm
    {
        Assembler assembler;
        Song song;
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
                Text = Strings.AssemblerOpenFile
            };
            openButton.Click += OpenASM;
            previewButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(150, 50),
                Size = new Size(120, 23),
                Text = Strings.AssemblerPreviewSong
            };
            previewButton.Click += PreviewSong;
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
            addedDefsGrid.Columns[0].Name = Strings.AssemblerDefinition;
            addedDefsGrid.Columns[1].Name = Strings.AssemblerValue;
            addedDefsGrid.Columns[1].DefaultCellStyle.NullValue = "0";
            addedDefsGrid.Rows.Add(new string[] { "voicegroup000", $"0x{SongPlayer.Instance.Song.VoiceTable.GetOffset() + ROM.Pak:X7}" });
            addedDefsGrid.CellValueChanged += AddedDefsGrid_CellValueChanged;

            Controls.AddRange(new Control[] { openButton, previewButton, sizeLabel, offsetValueBox, headerLabelTextBox, addedDefsGrid });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Size = new Size(600, 400);
            Text = $"GBA Music Studio ― {Strings.AssemblerTitle}";
        }

        void AddedDefsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewCell cell = addedDefsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.Value == null)
            {
                return;
            }

            if (e.ColumnIndex == 0)
            {
                if (char.IsDigit(cell.Value.ToString()[0]))
                {
                    FlexibleMessageBox.Show(Strings.AssemblerErrorDefinitionDigit, Strings.TitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cell.Value = cell.Value.ToString().Substring(1);
                }
            }
            else
            {
                if (!Utils.TryParseValue(cell.Value.ToString(), out long val))
                {
                    FlexibleMessageBox.Show(string.Format(Strings.AssemblerErrorInvalidValue, cell.Value), Strings.TitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cell.Value = null;
                }
            }
        }
        void PreviewSong(object sender, EventArgs e)
        {
            ((MainForm)Owner).PreviewSong(song, Path.GetFileName(assembler.FileName));
        }
        void OpenASM(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = Strings.TitleOpenASM, Filter = $"{Strings.FilterOpenASM}|*.s" };
            if (d.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                var s = new Dictionary<string, int>();
                foreach (DataGridViewRow r in addedDefsGrid.Rows.Cast<DataGridViewRow>())
                {
                    if (r.Cells[0].Value == null || r.Cells[1].Value == null)
                    {
                        continue;
                    }
                    s.Add(r.Cells[0].Value.ToString(), (int)Utils.ParseValue(r.Cells[1].Value.ToString()));
                }
                song = new M4AASMSong(assembler = new Assembler(d.FileName, (int)(ROM.Pak + offsetValueBox.Value), s),
                    headerLabelTextBox.Text = Assembler.FixLabel(Path.GetFileNameWithoutExtension(d.FileName)));
                sizeLabel.Text = string.Format(Strings.AssemblerSizeInBytes, assembler.BinaryLength);
                previewButton.Enabled = true;
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, Strings.TitleAssemblerError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
