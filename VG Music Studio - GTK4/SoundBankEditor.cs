using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.GTK4.Util;

namespace Kermalis.VGMusicStudio.GTK4;

[GObject.Subclass<Adw.Window>]
internal sealed partial class SoundBankEditor
{
    private IVoiceInfo? _voiceInfo;

    private SoundBank? _bank;
    private Gio.ListStore _voicesModel = Gio.ListStore.New(GetGType());
    private Gtk.SingleSelection? _singleSelectionModel;
    private Gtk.ColumnView? _voicesColumnView;
    internal List<SoundBankEditor>? VoicesData { get; set; } = [];

    private int _voicesClickSelectionIndex = 0;

    private Gtk.Box? _voicesEditorBox;
    private Gtk.Box[]? _voicesEditorParamBox;
    private Gtk.SpinButton[]? _voiceParamValue;
    private Gtk.Label[]? _voiceParamLabel;
    private OffsetEntry? _voiceParamOffset;
    private Gtk.Button? _buttonSubVoicesOffset;

    private SoundBankEditor AddEntry(IVoiceInfo voiceInfo)
    {
        _voiceInfo = voiceInfo;
        return this;
    }
    partial void Initialize()
    {
        _voicesEditorBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        var viewportBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        var contentBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 6);
        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);

        var header = Adw.HeaderBar.New();
        header.SetShowEndTitleButtons(true);
        header.SetShowStartTitleButtons(true);

        var viewport = Gtk.Viewport.New(Gtk.Adjustment.New(0, double.MinValue, double.MaxValue, 1, 1, 1), Gtk.Adjustment.New(0, double.MinValue, double.MaxValue, 1, 1, 1));
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.SetSizeRequest(300, 200);
        scrolledWindow.SetHexpand(true);
        scrolledWindow.SetVexpand(true);

        _singleSelectionModel = Gtk.SingleSelection.New(_voicesModel);

        _voicesColumnView = Gtk.ColumnView.New(_singleSelectionModel);
        _voicesColumnView.AddCssClass("data-table");
        _voicesColumnView.SetShowColumnSeparators(true);
        _voicesColumnView.SetShowRowSeparators(true);
        _voicesColumnView.SetReorderable(false);
        _voicesColumnView.SetHexpand(true);

        scrolledWindow.SetChild(_voicesColumnView);

        viewport.Child = scrolledWindow;

        var voicesFrame = Gtk.Frame.New("Voices");
        voicesFrame.Child = viewport;
        voicesFrame.SetMarginStart(10);
        voicesFrame.SetMarginEnd(10);
        voicesFrame.SetMarginTop(10);
        voicesFrame.SetMarginBottom(10);

        var voicesArgsFrame = Gtk.Frame.New("Voices");
        voicesArgsFrame.Child = _voicesEditorBox;
        voicesArgsFrame.SetMarginStart(10);
        voicesArgsFrame.SetMarginEnd(10);
        voicesArgsFrame.SetMarginTop(10);
        voicesArgsFrame.SetMarginBottom(10);

        viewportBox.Append(voicesFrame);

        contentBox.Append(viewportBox);
        contentBox.Append(voicesArgsFrame);

        mainBox.Append(header);
        mainBox.Append(contentBox);

        SetVexpand(true);
        SetHexpand(true);

        SetContent(mainBox);
    }

    internal void Init()
    {
        SetupColumns();
        ConfigureTimer();
    }

    private void SetupColumns()
    {
        // Rows
        var listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupRow;
        listItemFactory.OnBind += OnBindRow;

        _voicesColumnView!.SetRowFactory(listItemFactory);

        // Index Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindIndexText;

        var indexColumn = Gtk.ColumnViewColumn.New("#", listItemFactory);
        indexColumn.SetFixedWidth(50);
        indexColumn.SetResizable(true);
        _voicesColumnView.AppendColumn(indexColumn);

        // Voice Type Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindEventTypeText;

        var voiceTypeColumn = Gtk.ColumnViewColumn.New(Strings.PlayerType, listItemFactory);
        voiceTypeColumn.SetFixedWidth(150);
        voiceTypeColumn.SetResizable(true);
        _voicesColumnView.AppendColumn(voiceTypeColumn);

        // Offset Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindOffsetText;

        var offsetColumn = Gtk.ColumnViewColumn.New(Strings.TrackEditorOffset, listItemFactory);
        offsetColumn.SetFixedWidth(150);
        offsetColumn.SetExpand(true);
        offsetColumn.SetResizable(true);
        _voicesColumnView!.AppendColumn(offsetColumn);
    }

    private void ConfigureTimer()
    {
        var timer = GLib.Timer.New(); // Creates a new timer variable
        var context = GLib.MainContext.GetThreadDefault(); // Reads the main context default thread
        var source = GLib.Functions.TimeoutSourceNew(50); // Creates and configures the timeout interval at 50 microseconds, so it updates upon selection
        source.SetCallback(EventsCallback); // Sets the callback for the timer interval to be used on
        var microsec = new CULong(source.Attach(context)); // Configures the microseconds based on attaching the GLib MainContext thread
        GLib.Internal.Timer.Elapsed(timer.Handle, ref microsec); // GLib.Timer.Elapsed was removed in GirCore 0.6.3, so we're using this workaround instead
        timer.Start(); // Starts the timer
    }

    private bool EventsCallback()
    {
        if (_singleSelectionModel?.GetSelectedItem() is SoundBankEditor rowVoices && _voicesClickSelectionIndex != (int)_singleSelectionModel.Selected)
        {
            if (rowVoices._voiceInfo is not null)
            {
                _voicesClickSelectionIndex = (int)_singleSelectionModel.Selected;
                UpdateParamBoxes();
            }
        }
        return true;
    }

    private void ArgumentChanged(Gtk.SpinButton sender, EventArgs args)
    {
        if (_bank!.HasADSR((int)_singleSelectionModel.Selected!))
        {
            if (_voiceParamOffset is not null)
            {
                _bank.SetAddressPointer((int)_singleSelectionModel.Selected!, (int)BinaryPrimitives.ReadInt32LittleEndian(Convert.FromHexString(_voiceParamOffset.Entry.Text_)));
            }
            if (_voiceParamValue is not null)
            {
                byte a = (byte)_voiceParamValue[0].Value;
                byte d = (byte)_voiceParamValue[1].Value;
                byte s = (byte)_voiceParamValue[2].Value;
                byte r = (byte)_voiceParamValue[3].Value;
                _bank.SetADSRValues((int)_singleSelectionModel.Selected!, in a, in d, in s, in r);
            }
        }
    }
    private void UpdateParamBoxes()
    {
        if (_voicesEditorParamBox is not null)
        {
            for (int i = 0; i < _voicesEditorParamBox.Length; i++)
            {
                if (_voicesEditorParamBox[i].GetFirstChild() is not null)
                {
                    while (_voicesEditorParamBox[i].GetFirstChild() is not null)
                    {
                        _voicesEditorParamBox[i].Remove(_voicesEditorParamBox[i].GetFirstChild()!);
                    }
                    if (_voiceParamLabel is not null && _voiceParamLabel[i] is not null)
                    {
                        _voiceParamLabel[i].Dispose();
                        _voiceParamLabel[i] = null!;
                    }
                    if (_voiceParamValue is not null && _voiceParamValue[i] is not null)
                    {
                        _voiceParamValue[i].OnValueChanged -= ArgumentChanged;
                        _voiceParamValue[i].Dispose();
                        _voiceParamValue[i] = null!;
                    }
                    if (_voiceParamOffset is not null)
                    {
                        _voiceParamOffset.Dispose();
                        _voiceParamOffset = null;
                    }
                    if (_buttonSubVoicesOffset is not null)
                    {
                        _buttonSubVoicesOffset.OnClicked -= OpenSoundBankEditor;
                        _buttonSubVoicesOffset.Dispose();
                        _buttonSubVoicesOffset = null;
                    }
                }
            }
            while (true)
            {
                if (_voicesEditorBox.GetFirstChild() is not null)
                {
                    _voicesEditorBox!.Remove(_voicesEditorBox.GetFirstChild());
                }
                else
                {
                    break;
                }
            }
        }

        #region Addresses (PCM8, Key Split, Drum, PCM4)

        if (_bank.IsValidVoiceAddress((int)_singleSelectionModel.Selected))
        {
            _voicesEditorParamBox = new Gtk.Box[1];
            _voicesEditorParamBox[0] = Gtk.Box.New(Gtk.Orientation.Vertical, 3);
            _voicesEditorParamBox[0].SetMarginStart(5);
            _voicesEditorParamBox[0].SetMarginEnd(5);
            _voicesEditorParamBox[0].SetMarginTop(5);
            _voicesEditorParamBox[0].SetMarginBottom(5);
            _voiceParamLabel = new Gtk.Label[1];
            if (_bank.IsTableAddress((int)_singleSelectionModel.Selected))
            {
                _voiceParamLabel[0] = Gtk.Label.New("Table Offset");
                _buttonSubVoicesOffset = Gtk.Button.New();
                _buttonSubVoicesOffset.Label = "Open Voice Table";
                _buttonSubVoicesOffset.OnClicked += OpenSoundBankEditor;
            }
            else
            {
                _voiceParamLabel[0] = Gtk.Label.New("Sample Offset");
            }
            _voiceParamOffset = OffsetEntry.Initialize(_bank.GetVoiceAddress((int)_singleSelectionModel.Selected));
            _voicesEditorParamBox[0].Append(_voiceParamLabel[0]);
            _voicesEditorParamBox[0].Append(_voiceParamOffset);
            if (_bank.IsTableAddress((int)_singleSelectionModel.Selected))
            {
                _voicesEditorParamBox[0].Append(_buttonSubVoicesOffset!);
            }
            _voicesEditorBox.Append(_voicesEditorParamBox[0]);
        }

        #endregion

        #region ADSR (everything except Key Split, Drum and invalids)

        if (_bank.IsValidADSR((int)_singleSelectionModel.Selected))
        {
            bool isPCM8 = !_bank.IsPSGInstrument((int)_singleSelectionModel.Selected);
            _voiceParamLabel = new Gtk.Label[4];
            _voiceParamValue = new Gtk.SpinButton[4];
            _voicesEditorParamBox = new Gtk.Box[4];
            for (int i = 0; i < _voiceParamValue.Length; i++)
            {
                _voicesEditorParamBox[i] = Gtk.Box.New(Gtk.Orientation.Vertical, 3);
                _voicesEditorParamBox[i].SetMarginStart(5);
                _voicesEditorParamBox[i].SetMarginEnd(5);
                _voicesEditorParamBox[i].SetMarginTop(5);
                _voicesEditorParamBox[i].SetMarginBottom(5);

                _voiceParamValue[i] = Gtk.SpinButton.New(Gtk.Adjustment.New(0, 0, 100, 1, 1, 1), 1, 0);
                _voiceParamValue[i].SetNumeric(true);
            }
            _voiceParamValue[0].Adjustment.Upper = _voiceParamValue[1].Adjustment.Upper = _voiceParamValue[3].Adjustment.Upper = isPCM8 ? byte.MaxValue : 0x7;
            _voiceParamValue[2].Adjustment.Upper = isPCM8 ? byte.MaxValue : 0xF;
            _voiceParamValue[0].Adjustment.Lower = _voiceParamValue[1].Adjustment.Lower = _voiceParamValue[2].Adjustment.Lower = _voiceParamValue[3].Adjustment.Lower = byte.MinValue;
            _bank.GetADSRValues((int)_singleSelectionModel.Selected, out byte a, out byte d, out byte s, out byte r);
            _voiceParamValue[0].Value = a;
            _voiceParamValue[1].Value = d;
            _voiceParamValue[2].Value = s;
            _voiceParamValue[3].Value = r;

            _voiceParamLabel[0] = Gtk.Label.New("Attack");
            _voiceParamLabel[1] = Gtk.Label.New("Decay");
            _voiceParamLabel[2] = Gtk.Label.New("Sustain");
            _voiceParamLabel[3] = Gtk.Label.New("Release");

            for (int i = 0; i < _voicesEditorParamBox.Length; i++)
            {
                _voicesEditorParamBox[i].Append(_voiceParamLabel[i]);
                _voicesEditorParamBox[i].Append(_voiceParamValue[i]);
                _voicesEditorBox.Append(_voicesEditorParamBox[i]);
            }
            for (int i = 0; i < _voiceParamValue.Length; i++)
            {
                _voiceParamValue[i].OnValueChanged += ArgumentChanged;
            }
        }

        #endregion
    }

    private void OpenSoundBankEditor(Gtk.Button sender, EventArgs args)
    {
        var subBank = _bank!.LoadFromAddress(_bank.GetVoiceAddress((int)_singleSelectionModel.Selected));

        var soundBankEditor = NewWithProperties([]);
        if (Engine.Instance is not null)
        {
            soundBankEditor.Init();
            soundBankEditor.LoadVoices(subBank);
        }
        soundBankEditor.Present();

        soundBankEditor.OnCloseRequest += WindowClosed;

        bool WindowClosed(Gtk.Window sender, EventArgs args)
        {
            soundBankEditor!.Dispose();
            soundBankEditor = null!;
            return false;
        }
    }

    public void LoadVoices(SoundBank soundBank)
    {
        _bank = soundBank;
        if (Engine.Instance!.IsFileSystemFormat)
        {
            Title = $"{ConfigUtils.PROGRAM_NAME} ― {Strings.SoundBankEditorTitle}";
        }
        else
        {
            Title = $"{ConfigUtils.PROGRAM_NAME} ― {Strings.VoiceGroupEditorTitle} (0x{_bank!.Offset:X7})";
        }
        ReloadColumnEntries(_bank, _voicesModel, VoicesData);
        UpdateParamBoxes();
    }
    public void UpdateVoices()
    {
        // ReloadColumnEntries(Engine.Instance.Player.LoadedSong.Bank, _voicesModel, VoicesData);
        // ReloadColumnEntries(Engine.Instance.Player.LoadedSong.Bank.ElementAt(_voicesClickSelectionIndex).GetSubVoices(), _subVoicesModel, SubVoicesData);
    }
    public void ReloadColumnEntries(IEnumerable<IVoiceInfo>? instance, Gio.ListStore? listStore, List<SoundBankEditor>? voicesData)
    {
        if (listStore!.GetNItems() is not 0)
        {
            listStore.RemoveAll();
        }
        voicesData ??= [];
        voicesData.Clear();

        if (Engine.Instance is null) return;
        if (Engine.Instance.Player.LoadedSong is null) return;
        if (instance is null) return;

        foreach (var voice in instance)
        {
            voicesData.Add(AddEntry(voice));
        }
        if (voicesData.Count is not 0)
        {
            foreach (var voice in voicesData)
            {
                listStore.Append(voice);
            }
        }
    }

    private void OnSetupRow(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
    {

    }

    private void OnBindRow(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is Gtk.ColumnViewRow row)
        {

        }
    }

    private void OnSetupLabel(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        var label = Gtk.Label.New(null);
        label.Halign = Gtk.Align.Center;

        listItem.Child = label;
    }

    private void OnBindIndexText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;

        label.SetText($"{listItem.Position}");
    }
    private void OnBindEventTypeText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not SoundBankEditor userData) return;
        if (userData._voiceInfo is null) return;

        var voiceClass = $"{userData._voiceInfo}".ToLower().Replace(' ', '-').Trim('(');

        label.GetParent()!.GetParent()!.SetName($"row-{voiceClass}");

        label.SetText(userData._voiceInfo.ToString()!);
        label.SetEllipsize(Pango.EllipsizeMode.End);
    }
    private void OnBindOffsetText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not SoundBankEditor userData) return;

        label.SetText(string.Format("0x{0:X}", userData._voiceInfo!.Offset));
        label.SetEllipsize(Pango.EllipsizeMode.End);
    }
}