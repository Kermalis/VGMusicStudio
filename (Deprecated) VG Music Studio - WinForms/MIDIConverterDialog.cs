using Kermalis.MIDI;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.GBA;
using Kermalis.VGMusicStudio.Core.GBA.AlphaDream;
using Kermalis.VGMusicStudio.Core.GBA.MP2K;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.WinForms.Properties;
using Kermalis.VGMusicStudio.WinForms.Util;
using PlatinumLucario.MIDI.GBA.MP2K;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

[DesignerCategory("")]
class MIDIConverterDialog : ThemedForm
{
    private readonly LoadedSong _song = (LoadedSong)Engine.Instance!.Player.LoadedSong!;
    private string? _midiFilePath;
    private readonly ThemedButton _previewButton;
    private readonly ValueTextBox _offsetValueBox;

    public MIDIConverterDialog()
    {
        var openButton = new ThemedButton
        {
            Location = new Point(150, 0),
            Text = Strings.MIDIConverterOpenFile
        };
        openButton.Click += OpenMIDI;
        _previewButton = new ThemedButton
        {
            Enabled = false,
            Location = new Point(150, 50),
            Size = new Size(120, 23),
            Text = Strings.MIDIConverterPreviewSong
        };
        _previewButton.Click += PreviewSong;
        _offsetValueBox = new ValueTextBox
        {
            Hexadecimal = true,
            Maximum = Engine.Instance!.Config.ROM!.Length - 1,
            Value = Engine.Instance.Player.LoadedSong!.Bank.Offset
        };

        Controls.AddRange([openButton, _previewButton, _offsetValueBox]);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Size = new Size(600, 400);
        Text = $"VG Music Studio ― {Strings.MIDIConverterTitle}";
    }

    private void PreviewSong(object? sender, EventArgs e)
    {
        ((MainForm)Owner!).PreviewSong(_song, Path.GetFileName(_midiFilePath)!);
    }
    private void OpenMIDI(object? sender, EventArgs e)
    {
        string? inFile = WinFormsUtils.CreateLoadDialog(".mid", Strings.MenuOpenMIDI, Strings.FilterOpenMIDI + " (*.mid;*.midi)|*.mid;*.midi" );
        if (inFile is null)
        {
            return;
        }

        try
        {
            _midiFilePath = inFile;
            switch (Engine.Instance)
            {
                case MP2KEngine:
                    {
                        MP2KConverter converter = new(new MIDIFile(new FileStream(inFile, FileMode.Open)));
                        string asmFilePath = Path.ChangeExtension(Path.GetFullPath(_midiFilePath), "s");
                        string asmFileName = Path.GetFileName(asmFilePath);
                        converter.SaveAsASM(asmFilePath);
                        // var process = new Process
                        // {
                        //     StartInfo = new ProcessStartInfo
                        //     {
                        //         FileName = "midi2agb.exe",
                        //         Arguments = string.Format("\"{0}\" \"{1}\"", _midiFilePath, "temp.s")
                        //     }
                        // };
                        // process.Start();
                        // process.WaitForExit();
                        var asm = new Assembler(asmFileName, GBAUtils.CARTRIDGE_CAPACITY, EndianBinaryIO.Endianness.LittleEndian, new Dictionary<string, int> { { "voicegroup000", (int)(GBAUtils.CARTRIDGE_CAPACITY + _offsetValueBox.Value) } });
                        File.Delete(asmFileName);
                        _song.OpenASM(asm, Path.GetFileNameWithoutExtension(asmFilePath));
                        break;
                    }
            }
            _previewButton.Enabled = true;
        }
        catch (Exception ex)
        {
            FlexibleMessageBox.Show(string.Format(Strings.MIDIConverterError, Environment.NewLine + ex.Message), Strings.MIDIConverterTitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
