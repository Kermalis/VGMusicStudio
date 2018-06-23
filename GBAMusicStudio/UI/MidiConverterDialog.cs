using GBAMusicStudio.Core;
using GBAMusicStudio.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [System.ComponentModel.DesignerCategory("")]
    internal class MIDIConverterDialog : Form
    {
        Assembler assembler;
        string midiFileName;
        readonly Button previewButton;
        readonly ValueTextBox offsetValueBox;
        readonly Label sizeLabel;

        internal MIDIConverterDialog()
        {
            var openButton = new Button
            {
                Location = new Point(150, 0),
                Text = "Open File"
            };
            openButton.Click += OpenMidi;
            previewButton = new Button
            {
                Enabled = false,
                Location = new Point(150, 50),
                Text = "Preview Song"
            };
            previewButton.Click += PreviewASM;
            sizeLabel = new Label
            {
                Location = new Point(0, 100),
                Size = new Size(150, 23)
            };
            offsetValueBox = new ValueTextBox
            {
                Hexadecimal = true,
                Maximum = ROM.Capacity - 1
            };

            Controls.AddRange(new Control[] { openButton, previewButton, sizeLabel, offsetValueBox });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = Resources.Icon;
            MaximizeBox = false;
            Size = new Size(600, 400);
            Text = "GBA Music Studio ― MIDI Converter";
        }
        
        void PreviewASM(object sender, EventArgs e)
        {
            ((MainForm)Owner).PreviewASM(assembler, "temp", Path.GetFileName(midiFileName));
        }
        void OpenMidi(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = "Open MIDI", Filter = "MIDI files|*.mid" };
            if (d.ShowDialog() != DialogResult.OK) return;

            try
            {
                midiFileName = d.FileName;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "mid2agb.exe",
                        Arguments = string.Format("\"{0}\" \"{1}\"", d.FileName, "temp.s")
                    }
                };
                process.Start();
                process.WaitForExit();
                assembler = new Assembler("temp.s", (uint)(ROM.Pak + offsetValueBox.Value), new Dictionary<string, int> { { "voicegroup000", (int)SongPlayer.VoiceTable.Offset } });
                File.Delete("temp.s");
                sizeLabel.Text = $"Size in bytes: {assembler.BinaryLength}";
                previewButton.Enabled = true;
            }
            catch
            {
                FlexibleMessageBox.Show("There was an error converting the MIDI file.", "Error Converting MIDI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
