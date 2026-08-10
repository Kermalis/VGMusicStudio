using System;
using System.Runtime.InteropServices;
using Adw;
using GObject;
using Kermalis.VGMusicStudio.Core;

namespace Kermalis.VGMusicStudio.GTK4;

[Subclass<Window>]
internal partial class Preferences
{
    private Mixer.AudioBackend PlaybackBackend;

    private Gtk.CheckButton RadioButtonPortAudio { get; set; }
    private Gtk.CheckButton RadioButtonMiniAudio { get; set; }
    private Gtk.CheckButton RadioButtonNAudio { get; set; }
    
    partial void Initialize()
    {
        Title = $"Preferences - {MainWindow.GetProgramName()}";
        FocusVisible = true;
        FocusOnClick = true;

        var header = HeaderBar.New();

        var labelAudioBackend = Gtk.Label.New("Audio Backend:");

        RadioButtonMiniAudio = Gtk.CheckButton.New();
        RadioButtonMiniAudio.Label = "MiniAudio";
        RadioButtonMiniAudio.OnNotify += OnNotify_MiniAudio;
        RadioButtonPortAudio = Gtk.CheckButton.New();
        RadioButtonPortAudio.Label = "PortAudio";
        RadioButtonPortAudio.OnNotify += OnNotify_PortAudio;
        RadioButtonNAudio = Gtk.CheckButton.New();
        RadioButtonNAudio.Label = "NAudio (Legacy, Deprecated, Windows Only)";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RadioButtonNAudio.Sensitive = true;
            RadioButtonNAudio.OnNotify += OnNotify_NAudio;
        }
        else
        {
            RadioButtonNAudio.Sensitive = false;
        }
        RadioButtonPortAudio.SetGroup(RadioButtonMiniAudio);
        RadioButtonPortAudio.SetGroup(RadioButtonNAudio);
        
        switch (Mixer.PlaybackBackend)
        {
            case Mixer.AudioBackend.MiniAudio:
                {
                    RadioButtonMiniAudio.Active = true;
                    break;
                }
            case Mixer.AudioBackend.PortAudio:
                {
                    RadioButtonPortAudio.Active = true;
                    break;
                }
            case Mixer.AudioBackend.NAudio:
                {
                    RadioButtonNAudio.Active = true;
                    break;
                }
        }

        var buttonOK = Gtk.Button.New();
        buttonOK.Label = "OK";
        buttonOK.SetValign(Gtk.Align.End);
        buttonOK.OnClicked += ButtonOK_Clicked;
        var buttonApply = Gtk.Button.New();
        buttonApply.Label = "Accept";
        buttonApply.SetValign(Gtk.Align.End);
        buttonApply.OnClicked += ButtonApply_Clicked;
        var buttonCancel = Gtk.Button.New();
        buttonCancel.Label = "Cancel";
        buttonCancel.SetValign(Gtk.Align.End);
        buttonCancel.OnClicked += ButtonCancel_Clicked;

        var buttonBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 15);
        buttonBox.SetHalign(Gtk.Align.Center);
        buttonBox.SetBaselinePosition(Gtk.BaselinePosition.Bottom);
        buttonBox.SetVexpand(true);
        buttonBox.SetMarginBottom(10);
        buttonBox.Append(buttonOK);
        buttonBox.Append(buttonApply);
        buttonBox.Append(buttonCancel);

        var box = Gtk.Box.New(Gtk.Orientation.Vertical, 5);
        box.SetVexpand(true);
        box.Append(header);
        box.Append(labelAudioBackend);
        box.Append(RadioButtonMiniAudio);
        box.Append(RadioButtonPortAudio);
        box.Append(RadioButtonNAudio);
        box.Append(buttonBox);

        SetContent(box);

        OnCloseRequest += Preferences_WindowClosed;
    }

    private void ButtonOK_Clicked(Gtk.Button sender, EventArgs args)
    {
        Mixer.PlaybackBackend = PlaybackBackend;
        MainWindow.Instance!.ReloadEngine();
        Preferences_WindowClosed(null!, null!);
        Close();
    }
    private void ButtonApply_Clicked(Gtk.Button sender, EventArgs args)
    {
        Mixer.PlaybackBackend = PlaybackBackend;
        MainWindow.Instance!.ReloadEngine();
    }
    private void ButtonCancel_Clicked(Gtk.Button sender, EventArgs args)
    {
        Preferences_WindowClosed(null!, null!);
        Close();
    }

    private bool Preferences_WindowClosed(Gtk.Window sender, EventArgs args)
    {
        OnCloseRequest -= Preferences_WindowClosed;
        RadioButtonMiniAudio.OnNotify -= OnNotify_MiniAudio;
        RadioButtonPortAudio.OnNotify -= OnNotify_PortAudio;
        RadioButtonNAudio.OnNotify -= OnNotify_NAudio;
        MainWindow.Instance!.SetCanTarget(true);
        MainWindow.Instance.SetSensitive(true);
        Dispose();
        return false;
    }

    private void OnNotify_MiniAudio(object sender, EventArgs args)
    {
        if (args is NotifySignalArgs notifyArgs)
        {
            var name = notifyArgs.Pspec.GetName();
            if (name is "active" && RadioButtonMiniAudio.Active is true)
            {
                PlaybackBackend = Mixer.AudioBackend.MiniAudio;
                RadioButtonPortAudio.Active = false;
                RadioButtonNAudio.Active = false;
            }
        }
    }

    private void OnNotify_PortAudio(object sender, EventArgs args)
    {
        if (args is NotifySignalArgs notifyArgs)
        {
            var name = notifyArgs.Pspec.GetName();
            if (name is "active" && RadioButtonPortAudio.Active is true)
            {
                PlaybackBackend = Mixer.AudioBackend.PortAudio;
                RadioButtonMiniAudio.Active = false;
                RadioButtonNAudio.Active = false;
            }
        }
    }

    private void OnNotify_NAudio(object sender, EventArgs args)
    {
        if (args is NotifySignalArgs notifyArgs)
        {
            var name = notifyArgs.Pspec.GetName();
            if (name is "active" && RadioButtonNAudio.Active is true)
            {
                PlaybackBackend = Mixer.AudioBackend.NAudio;
                RadioButtonMiniAudio.Active = false;
                RadioButtonPortAudio.Active = false;
            }
        }
    }
}