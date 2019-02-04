using GBAMusicStudio.Core;
using GBAMusicStudio.Properties;
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
        Song song;
        string midiFileName;
        readonly ThemedButton previewButton;
        readonly ValueTextBox offsetValueBox;

        public MIDIConverterDialog()
        {
            var openButton = new ThemedButton
            {
                Location = new Point(150, 0),
                Text = Strings.MIDIConverterOpenFile
            };
            openButton.Click += OpenMIDI;
            previewButton = new ThemedButton
            {
                Enabled = false,
                Location = new Point(150, 50),
                Size = new Size(120, 23),
                Text = Strings.MIDIConverterPreviewSong
            };
            previewButton.Click += PreviewSong;
            offsetValueBox = new ValueTextBox
            {
                Hexadecimal = true,
                Maximum = ROM.Capacity - 1,
                Value = SongPlayer.Instance.Song.VoiceTable.GetOffset()
            };

            Controls.AddRange(new Control[] { openButton, previewButton, offsetValueBox });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Size = new Size(600, 400);
            Text = $"GBA Music Studio ― {Strings.MIDIConverterTitle}";
        }

        void PreviewSong(object sender, EventArgs e)
        {
            ((MainForm)Owner).PreviewSong(song, Path.GetFileName(midiFileName));
        }
        void OpenMIDI(object sender, EventArgs e)
        {
            var d = new OpenFileDialog { Title = Strings.TitleOpenMIDI, Filter = $"{Strings.FilterOpenMIDI}|*.mid" };
            if (d.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                midiFileName = d.FileName;
                switch (ROM.Instance.Game.Engine.Type)
                {
                    case EngineType.M4A:
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "midi2agb.exe",
                                    Arguments = string.Format("\"{0}\" \"{1}\"", midiFileName, "temp.s")
                                }
                            };
                            process.Start();
                            process.WaitForExit();
                            var ass = new Assembler("temp.s", ROM.Pak, new Dictionary<string, int> { { "voicegroup000", (int)(ROM.Pak + offsetValueBox.Value) } });
                            File.Delete("temp.s");
                            song = new M4AASMSong(ass, "temp");
                            break;
                        }
                    case EngineType.MLSS:
                        {
                            song = new MLSSMIDISong(midiFileName);
                            break;
                        }
                }
                previewButton.Enabled = true;
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(string.Format(Strings.MIDIConverterError, Environment.NewLine + ex.Message), Strings.MIDIConverterTitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
