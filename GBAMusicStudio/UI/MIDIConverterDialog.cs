using GBAMusicStudio.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    [DesignerCategory("")]
    class MIDIConverterDialog : ThemedForm
    {
        Assembler assembler;
        string midiFileName;
        readonly ThemedButton previewButton;
        readonly ValueTextBox offsetValueBox;
        readonly ThemedLabel sizeLabel;

        public MIDIConverterDialog()
        {
            var openButton = new ThemedButton
            {
                Location = new Point(150, 0),
                Text = "Apri File MIDI"
            };
            openButton.Click += OpenMIDI;
            previewButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(150, 50),
                Size = new Size(120, 23),
                Text = "Anteprima Canzone"
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
                Maximum = ROM.Capacity - 1,
                Value = SongPlayer.Instance.Song.VoiceTable.GetOffset()
            };

            Controls.AddRange(new Control[] { openButton, previewButton, sizeLabel, offsetValueBox });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Size = new Size(600, 400);
            Text = "GBA Music Studio ― Convertitore MIDI";
        }

        void PreviewASM(object sender, EventArgs e)
        {
            ((MainForm)Owner).PreviewASM(assembler, "temp", Path.GetFileName(midiFileName));
        }
        void OpenMIDI(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = "Apri File MIDI", Filter = "File MIDI|*.mid" };
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
                assembler = new Assembler("temp.s", ROM.Pak, new Dictionary<string, int> { { "voicegroup000", (int)(ROM.Pak + offsetValueBox.Value) } });
                File.Delete("temp.s");
                sizeLabel.Text = $"Peso in bytes: {assembler.BinaryLength}";
                previewButton.Enabled = true;
            }
            catch
            {
                FlexibleMessageBox.Show("C'è stato un errore nella conversione del file MIDI.", "Errore nella conversione MIDI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
