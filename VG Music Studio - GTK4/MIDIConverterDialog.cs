using System;
using System.IO;
using Kermalis.MIDI;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.GTK4.Util;
using PlatinumLucario.MIDI.GBA.MP2K;

namespace Kermalis.VGMusicStudio.GTK4;

[GObject.Subclass<Adw.Dialog>]
internal partial class MIDIConverterDialog
{
    private Gtk.Label _midiFilePathLabel = Gtk.Label.New("");
    private Gtk.Button _buttonSaveASM = Gtk.Button.NewWithLabel(Strings.TitleSaveASM);

    // MP2K param config
    private Gtk.Box _engineBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Box _mp2kParamBox = Gtk.Box.New(Gtk.Orientation.Vertical, 5);
    private Gtk.Box _masterVolumeBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _masterVolumeLabel;
    private Gtk.SpinButton? _masterVolume;
    private Gtk.Box _voiceGroupBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _voiceGroupLabel;
    private Gtk.Entry? _voiceGroup;
    private Gtk.Box _priorityBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _priorityLabel;
    private Gtk.SpinButton? _priority;
    private Gtk.Box _reverbBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _reverbLabel;
    private Gtk.SpinButton? _reverb;
    private Gtk.Box _clocksPerBeatBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _clocksPerBeatLabel;
    private Gtk.SpinButton? _clocksPerBeat;
    private Gtk.Box _gateTimeBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _gateTimeLabel;
    private Gtk.CheckButton? _gateTime;
    private Gtk.Box _compressionBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
    private Gtk.Label? _compressionLabel;
    private Gtk.CheckButton? _compression;

    private MP2KConverter? _converter;

    partial void Initialize()
    {
        Title = $"{MainWindow.GetProgramName()} — {Strings.MIDIConverterTitle}";

        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 5);

        var header = Adw.HeaderBar.New();
        header.SetShowEndTitleButtons(true);
        header.SetShowStartTitleButtons(true);

        var buttonOpenMIDI = Gtk.Button.NewWithLabel(Strings.TitleOpenMIDI);
        buttonOpenMIDI.OnClicked += ButtonOpenMIDI_Clicked;
        _buttonSaveASM.Sensitive = false;

        _engineBox.Append(_buttonSaveASM);

        mainBox.Append(header);
        mainBox.Append(_midiFilePathLabel);
        mainBox.Append(buttonOpenMIDI);
        mainBox.Append(_engineBox);

        SetChild(mainBox);
    }

    private void AddParamsMP2K()
    {
        _masterVolumeLabel = Gtk.Label.New("Master Volume");
        _masterVolume = Gtk.SpinButton.New(Gtk.Adjustment.New(127, 0, 128, 1, 1, 1), 1, 0);
        _voiceGroupLabel = Gtk.Label.New("Voice Group Label");
        _voiceGroup = Gtk.Entry.New();
        _voiceGroup.Text_ = "_dummy";
        _priorityLabel = Gtk.Label.New("Priority");
        _priority = Gtk.SpinButton.New(Gtk.Adjustment.New(0, 0, 128, 1, 1, 1), 1, 0);
        _reverbLabel = Gtk.Label.New("Reverb");
        _reverb = Gtk.SpinButton.New(Gtk.Adjustment.New(-1, -1, 128, 1, 1, 1), 1, 0);
        _clocksPerBeatLabel = Gtk.Label.New("Clocks Per Beat");
        _clocksPerBeat = Gtk.SpinButton.New(Gtk.Adjustment.New(1, 1, 128, 1, 1, 1), 1, 0);
        _gateTimeLabel = Gtk.Label.New("Use Exact Gate Time");
        _gateTime = Gtk.CheckButton.New();
        _gateTime.Active = false;
        _compressionLabel = Gtk.Label.New("Use Compression");
        _compression = Gtk.CheckButton.New();
        _compression.Active = true;

        _masterVolumeBox.Append(_masterVolumeLabel);
        _masterVolumeBox.Append(_masterVolume);
        _voiceGroupBox.Append(_voiceGroupLabel);
        _voiceGroupBox.Append(_voiceGroup);
        _priorityBox.Append(_priorityLabel);
        _priorityBox.Append(_priority);
        _reverbBox.Append(_reverbLabel);
        _reverbBox.Append(_reverb);
        _clocksPerBeatBox.Append(_clocksPerBeatLabel);
        _clocksPerBeatBox.Append(_clocksPerBeat);
        _gateTimeBox.Append(_gateTimeLabel);
        _gateTimeBox.Append(_gateTime);
        _compressionBox.Append(_compressionLabel);
        _compressionBox.Append(_compression);

        _mp2kParamBox.Append(_masterVolumeBox);
        _mp2kParamBox.Append(_voiceGroupBox);
        _mp2kParamBox.Append(_priorityBox);
        _mp2kParamBox.Append(_reverbBox);
        _mp2kParamBox.Append(_clocksPerBeatBox);
        _mp2kParamBox.Append(_gateTimeBox);
        _mp2kParamBox.Append(_compressionBox);
    }

    private void ButtonOpenMIDI_Clicked(Gtk.Button sender, EventArgs args)
    {
        GTK4Utils.OnPathChanged += LoadFile;
        GTK4Utils.CreateLoadDialog(["*.mid", "*.midi"], Strings.TitleOpenMIDI, Strings.FilterOpenMIDI);

        void LoadFile(string path)
        {
            GTK4Utils.OnPathChanged -= LoadFile;
            if (path is null)
            {
                return;
            }

            try
            {
                _buttonSaveASM.OnClicked -= ButtonSaveASM_Clicked;
                _buttonSaveASM.OnClicked += ButtonSaveASM_Clicked;
                _buttonSaveASM.Sensitive = true;

                _midiFilePathLabel.SetLabel(path);
                AddParamsMP2K();
                _engineBox.Append(_mp2kParamBox);
            }
            catch (Exception ex)
            {
                FlexibleDialog.Show(ex, ex.Message);
            }
        }
    }

    private void ButtonSaveASM_Clicked(Gtk.Button sender, EventArgs args)
    {
        GTK4Utils.CreateSaveDialog(Path.GetFileNameWithoutExtension(_midiFilePathLabel.Label_!), ["*.s"], Strings.TitleSaveASM, Strings.FilterSaveASM);
        GTK4Utils.OnPathChanged += SaveFile;

        void SaveFile(string path)
        {
            GTK4Utils.OnPathChanged -= SaveFile;
            if (path is null)
            {
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            bool success = false;
            try
            {
                _converter = new(new MIDIFile(new FileStream(_midiFilePathLabel.Label_!, FileMode.Open)), fileName, (byte)_masterVolume!.Adjustment!.Value, _voiceGroup!.Text_!, (byte)_priority!.Adjustment!.Value, (byte)_reverb!.Adjustment!.Value, (byte)_clocksPerBeat!.Adjustment!.Value, _gateTime!.Active, _compression!.Active);
                success = true;
            }
            catch (Exception ex)
            {
                // try
                // {
                //     _converter = new(new MIDIFile(new FileStream(_midiFilePathLabel.Label_!, FileMode.Open), true), fileName, (byte)_masterVolume!.Adjustment!.Value, _voiceGroup!.Text_!, (byte)_priority!.Adjustment!.Value, (byte)_reverb!.Adjustment!.Value, (byte)_clocksPerBeat!.Adjustment!.Value, _gateTime!.Active, _compression!.Active);
                //     success = true;
                // }
                // catch (InvalidDataException idex)
                // {
                //     FlexibleDialog.Show(idex, idex.Message);
                // }
                // if (success)
                // {
                //     FlexibleDialog.Show("This MIDI file has the following non-critical issue:\n\n" + ex.Message, "This MIDI contains errors!", buttonsType: FlexibleDialog.ButtonsType.OK, icon: Gtk.MessageType.Warning);
                // }
                FlexibleDialog.Show("This MIDI file has the following issue:\n\n" + ex.Message, "This MIDI contains errors!", buttonsType: FlexibleDialog.ButtonsType.OK, icon: Gtk.MessageType.Warning);
            }
            if (success)
            {
                _converter!.SaveAsASM(path);
            }
        }
    }
}
