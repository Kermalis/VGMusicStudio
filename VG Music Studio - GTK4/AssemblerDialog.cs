// using Kermalis.VGMusicStudio.Core;
// using Kermalis.VGMusicStudio.Core.Properties;
// using Kermalis.VGMusicStudio.Core.GBA.MP2K;
// using Kermalis.VGMusicStudio.GTK4.Util;
// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Drawing;
// using System.IO;
// using System.Linq;
// using Adw;

// namespace Kermalis.VGMusicStudio.GTK4;

// internal class AssemblerDialog : Window
// {
//     private readonly Assembler Assembler;
//     private ILoadedSong Song;
//     private readonly Gtk.Button PreviewButton;
//     private readonly ValueTextBox OffsetValueBox;
//     private readonly Gtk.Label SizeLabel;
//     private readonly TextBox HeaderLabelTextBox;
//     private readonly DataGridView AddedDefsGrid;

//     public AssemblerDialog()
//     {
//         var openButton = new Gtk.Button
//         {
//             Location = new Point(150, 0),
//             Text = Strings.AssemblerOpenFile
//         };
//         openButton.Click += OpenASM;
//         PreviewButton = new Gtk.Button
//         {
//             Enabled = false,
//             Location = new Point(150, 50),
//             Size = new Size(120, 23),
//             Text = Strings.AssemblerPreviewSong
//         };
//         PreviewButton.Click += PreviewSong;
//         SizeLabel = new Label
//         {
//             Location = new Point(0, 100),
//             Size = new Size(150, 23)
//         };
//         OffsetValueBox = new ValueTextBox
//         {
//             Hexadecimal = true,
//             Maximum = ROM.Capacity - 1
//         };
//         HeaderLabelTextBox = new TextBox { Location = new Point(0, 50), Size = new Size(150, 22) };
//         AddedDefsGrid = new DataGridView
//         {
//             ColumnCount = 2,
//             Location = new Point(0, 150),
//             MultiSelect = false
//         };
//         AddedDefsGrid.Columns[0].Name = Strings.AssemblerDefinition;
//         AddedDefsGrid.Columns[1].Name = Strings.AssemblerValue;
//         AddedDefsGrid.Columns[1].DefaultCellStyle.NullValue = "0";
//         AddedDefsGrid.Rows.Add(new string[] { "voicegroup000", $"0x{SongPlayer.Instance.Song.VoiceTable.GetOffset() + ROM.Pak:X7}" });
//         AddedDefsGrid.CellValueChanged += AddedDefsGrid_CellValueChanged;

//         Controls.AddRange(new Control[] { openButton, PreviewButton, SizeLabel, OffsetValueBox, HeaderLabelTextBox, AddedDefsGrid });
//         FormBorderStyle = FormBorderStyle.FixedDialog;
//         MaximizeBox = false;
//         Size = new Size(600, 400);
//         Text = $"GBA Music Studio ― {Strings.AssemblerTitle}";
//     }

//     void AddedDefsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
//     {
//         DataGridViewCell cell = AddedDefsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
//         if (cell.Value == null)
//         {
//             return;
//         }

//         if (e.ColumnIndex == 0)
//         {
//             if (char.IsDigit(cell.Value.ToString()[0]))
//             {
//                 FlexibleDialog.Show(Strings.AssemblerErrorDefinitionDigit, Strings.TitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
//                 cell.Value = cell.Value.ToString().Substring(1);
//             }
//         }
//         else
//         {
//             if (!Utils.TryParseValue(cell.Value.ToString(), out long val))
//             {
//                 FlexibleDialog.Show(string.Format(Strings.AssemblerErrorInvalidValue, cell.Value), Strings.TitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
//                 cell.Value = null;
//             }
//         }
//     }
//     void PreviewSong(object sender, EventArgs e)
//     {
//         ((MainForm)Owner).PreviewSong(Song, Path.GetFileName(Assembler.FileName));
//     }
//     void OpenASM(object sender, EventArgs e)
//     {
//         var d = new Gtk.FileDialog { Title = Strings.TitleOpenASM, Filter = $"{Strings.FilterOpenASM}|*.s" };
//         if (d.ShowDialog() != DialogResult.OK)
//         {
//             return;
//         }

//         try
//         {
//             var s = new Dictionary<string, int>();
//             foreach (DataGridViewRow r in AddedDefsGrid.Rows.Cast<DataGridViewRow>())
//             {
//                 if (r.Cells[0].Value == null || r.Cells[1].Value == null)
//                 {
//                     continue;
//                 }
//                 s.Add(r.Cells[0].Value.ToString(), (int)Utils.ParseValue(r.Cells[1].Value.ToString()));
//             }
//             Song = new MP2KASMSong(Assembler = new Assembler(d.FileName, (int)(ROM.Pak + OffsetValueBox.Value), s),
//                 HeaderLabelTextBox.Text = Assembler.FixLabel(Path.GetFileNameWithoutExtension(d.FileName)));
//             SizeLabel.Text = string.Format(Strings.AssemblerSizeInBytes, Assembler.BinaryLength);
//             PreviewButton.Enabled = true;
//         }
//         catch (Exception ex)
//         {
//             FlexibleDialog.Show(ex.Message, Strings.TitleAssemblerError, MessageBoxButtons.OK, MessageBoxIcon.Error);
//         }
//     }
// }
