using System;
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
internal sealed partial class TrackEditor
{
    private Gtk.CssProvider? _cssProvider;

    private Gtk.Button _buttonEventAdd = Gtk.Button.New();
    private Gtk.Button _buttonEventRemove = Gtk.Button.New();
    private Gtk.Box? _paramEditorBox;
    private Gtk.Box[]? _paramEditorParamBox;
    private Gtk.SpinButton[] _eventParamNum;
    private Gtk.CheckButton[] _eventParamCheck;
    private Gtk.Label[]? _eventParamLabel;
    private OffsetEntry? _eventParamOffset;
    private Gtk.StringList? _eventList;
    private Gtk.StringList? _trackList;
    private Gtk.DropDown? _trackDropDown;
    private Gtk.DropDown? _eventDropDown;

    private Gio.ListStore _eventsModel = Gio.ListStore.New<TrackData>();
    private Gtk.SingleSelection? _selectionModel;
    internal Gtk.ColumnView? EventsColumnView { get; set; }
    internal List<TrackData> EventsData { get; set; } = [];

    private int _clickSelectionIndex = 0;

    [GObject.Subclass<GObject.Object>]
    internal partial class TrackData
    {
        internal event Action? OnArgsChanged;

        internal ICommand Command;
        internal long Offset;
        internal long[]? Ticks;

        internal static TrackData AddEntry(ICommand command, long offset, Span<long> ticks)
        {
            var dat = NewWithProperties([]);
            dat.Command = command;
            dat.Offset = offset;
            dat.Ticks = ticks.ToArray();
            return dat;
        }
    }

    partial void Initialize()
    {
        Title = $"{MainWindow.GetProgramName()} — {Strings.TrackEditorTitle}";

        var header = Adw.HeaderBar.New();
        header.SetShowEndTitleButtons(true);
        header.SetShowStartTitleButtons(true);

        _trackList = Gtk.StringList.New(null);
        _trackDropDown = Gtk.DropDown.NewWithProperties([new GObject.ConstructArgument("width_request", new GObject.Value(100))]);
        _trackDropDown.SetModel(_trackList);
        _trackDropDown.OnNotify += TrackSelected;

        _eventList = Gtk.StringList.New(null);
        _eventDropDown = Gtk.DropDown.NewWithProperties([new GObject.ConstructArgument("width_request", new GObject.Value(50))]);
        _eventDropDown.SetModel(_eventList);

        _buttonEventAdd.SetLabel(Strings.TrackEditorAddEvent);
        _buttonEventAdd.OnClicked += ButtonEventAdd_OnClicked;
        _buttonEventRemove.SetLabel(Strings.TrackEditorRemoveEvent);
        _buttonEventRemove.OnClicked += ButtonEventRemove_OnClicked;

        var viewport = Gtk.Viewport.New(Gtk.Adjustment.New(0, double.MinValue, double.MaxValue, 1, 1, 1), Gtk.Adjustment.New(0, double.MinValue, double.MaxValue, 1, 1, 1));
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.SetSizeRequest(700, 500);
        scrolledWindow.SetHexpand(true);
        scrolledWindow.SetVexpand(true);

        _selectionModel = Gtk.SingleSelection.New(_eventsModel);

        EventsColumnView = Gtk.ColumnView.New(_selectionModel);
        EventsColumnView.AddCssClass("data-table");
        EventsColumnView.SetShowColumnSeparators(true);
        EventsColumnView.SetShowRowSeparators(true);
        EventsColumnView.SetReorderable(false);
        EventsColumnView.SetHexpand(true);

        scrolledWindow.SetChild(EventsColumnView);

        viewport.Child = scrolledWindow;

        var paramFrame = Gtk.Frame.New(Strings.TrackEditorArgEditor);
        paramFrame.Child = _paramEditorBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        _paramEditorBox.SetMarginStart(6);
        _paramEditorBox.SetMarginEnd(6);
        _paramEditorBox.SetMarginTop(6);
        _paramEditorBox.SetMarginBottom(6);
        var eventEditorButtonBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 6);
        var eventEditorBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        var mainEditorBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        var contentBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 6);
        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);

        eventEditorButtonBox.Append(_buttonEventAdd);
        eventEditorButtonBox.Append(_buttonEventRemove);

        eventEditorBox.Append(_eventDropDown);
        eventEditorBox.Append(eventEditorButtonBox);

        mainEditorBox.Append(eventEditorBox);
        mainEditorBox.Append(paramFrame);

        contentBox.Append(viewport);
        contentBox.Append(mainEditorBox);

        mainBox.Append(header);
        mainBox.Append(_trackDropDown);
        mainBox.Append(contentBox);

        SetVexpand(true);
        SetHexpand(true);

        SetContent(mainBox);
    }

    internal void Init()
    {
        // Rows
        var listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupRow;
        listItemFactory.OnBind += OnBindRow;

        EventsColumnView!.SetRowFactory(listItemFactory);

        // Event Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindEventTypeText;

        var eventColumn = Gtk.ColumnViewColumn.New(Strings.TrackEditorEvent, listItemFactory);
        eventColumn.SetFixedWidth(100);
        eventColumn.SetExpand(true);
        eventColumn.SetResizable(true);
        EventsColumnView.AppendColumn(eventColumn);

        // Arguments Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindArgumentsText;
        listItemFactory.OnUnbind += OnUnbindArgumentsText;

        var argumentsColumn = Gtk.ColumnViewColumn.New(Strings.TrackEditorArguments, listItemFactory);
        argumentsColumn.SetFixedWidth(100);
        argumentsColumn.SetExpand(true);
        argumentsColumn.SetResizable(true);
        EventsColumnView.AppendColumn(argumentsColumn);

        // Offset Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindOffsetText;

        var offsetColumn = Gtk.ColumnViewColumn.New(Strings.TrackEditorOffset, listItemFactory);
        offsetColumn.SetFixedWidth(100);
        offsetColumn.SetExpand(true);
        offsetColumn.SetResizable(true);
        EventsColumnView!.AppendColumn(offsetColumn);

        // Ticks Column
        listItemFactory = Gtk.SignalListItemFactory.New();
        listItemFactory.OnSetup += OnSetupLabel;
        listItemFactory.OnBind += OnBindTicksText;

        var ticksColumn = Gtk.ColumnViewColumn.New(Strings.TrackEditorTicks, listItemFactory);
        ticksColumn.SetFixedWidth(100);
        ticksColumn.SetExpand(true);
        ticksColumn.SetResizable(true);
        EventsColumnView.AppendColumn(ticksColumn);

        ConfigureTimer();
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
        if (_selectionModel?.GetSelectedItem() is TrackData row && _clickSelectionIndex != (int)_selectionModel.Selected)
        {
            if (row.Command is not null)
            {
                UpdateParamBoxes();
                _clickSelectionIndex = (int)_selectionModel.Selected;
            }
        }
        return true;
    }

    private void ButtonEventAdd_OnClicked(Gtk.Button sender, EventArgs args)
    {
        var cmd = (ICommand)Activator.CreateInstance(Engine.Instance!.GetCommands()[_eventDropDown!.Selected].GetType())!;
        var ev = new SongEvent(int.MaxValue, cmd);
        int index = (int)(_selectionModel!.Selected + 1);
        Engine.Instance.Player.LoadedSong!.InsertEvent(ev, (int)_trackDropDown!.Selected, index);
        EventsData!.Insert(index, TrackData.AddEntry(ev.Command, ev.Offset, ev.Ticks.ToArray()));
        _eventsModel.Insert((uint)index, TrackData.AddEntry(ev.Command, ev.Offset, ev.Ticks.ToArray()));
    }

    private void ButtonEventRemove_OnClicked(Gtk.Button sender, EventArgs args)
    {
        if (_selectionModel!.Selected == 4294967295)
        {
            return;
        }
        Engine.Instance!.Player.LoadedSong!.RemoveEvent((int)_trackDropDown!.Selected, (int)_selectionModel!.Selected);
        EventsData!.RemoveAt((int)_selectionModel!.Selected);
        _eventsModel.Remove(_selectionModel.Selected);
    }

    private void UpdateParamBoxes()
    {
        if (_paramEditorParamBox is not null)
        {
            for (int i = 0; i < _paramEditorParamBox.Length; i++)
            {
                if (_paramEditorParamBox[i].GetFirstChild() is not null)
                {
                    if (_eventParamNum is not null && _eventParamNum[i] is not null)
                    {
                        _paramEditorParamBox[i].Remove(_eventParamNum![i]);
                        _eventParamNum[i] = null!;
                    }
                    if (_eventParamOffset is not null)
                    {
                        _paramEditorParamBox[i].Remove(_eventParamOffset);
                        _eventParamOffset.Dispose();
                        _eventParamOffset = null;
                    }
                    _paramEditorParamBox[i].Remove(_eventParamLabel![i]);
                }
                _paramEditorBox!.Remove(_paramEditorParamBox[i]);
            }
        }
        var trackIndex = 0;
        var eventIndex = 0;
        if (_trackDropDown!.Selected != 4294967295)
        {
            trackIndex = (int)_trackDropDown.Selected;
        }
        if (_selectionModel!.Selected != 4294967295)
        {
            eventIndex = (int)_selectionModel.Selected;
        }
        var se = Engine.Instance!.Player.LoadedSong!.Events[trackIndex]![eventIndex]!;
        CommandArg[] ca = Engine.Instance!.Player.LoadedSong!.GetCommandMembers(trackIndex, eventIndex);
        _paramEditorParamBox = new Gtk.Box[ca.Length];
        _eventParamLabel = new Gtk.Label[ca.Length];
        var isAnOffset = Engine.Instance!.Player.LoadedSong!.CallOrJumpCommand(se);
        // if (isAnOffset)
        // {
        //     _eventParamOffset = OffsetEntry.Initialize();
        // }
        // else
        // {
        //     _eventParamNum = new Gtk.SpinButton[mi.Length];
        // }
        _eventParamNum = new Gtk.SpinButton[ca.Length];
        _eventParamCheck = new Gtk.CheckButton[ca.Length];
        for (int i = 0; i < ca.Length; i++)
        {
            _paramEditorParamBox[i] = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
            _eventParamLabel[i] = Gtk.Label.New(ca[i].Name);
            _paramEditorParamBox[i].Append(_eventParamLabel[i]);
            if (_eventParamNum is not null && _eventParamNum[i] is not null)
            {
                _eventParamNum[i].OnValueChanged -= ArgumentChanged;
            }

            switch (ca[i].Type)
            {
                case CommandArg.ValueType.Pointer:
                    {
                        _eventParamLabel[i].SetLabel($"{ca[i].Name} | 0x{ca[i].Offset:X7}");
                        _eventParamNum![i] = Gtk.SpinButton.New(Gtk.Adjustment.New(ca[i].Value, ca[i].MinValue, ca[i].MaxValue, 1, 1, 1), 1, 0);
                        _eventParamNum[i].OnValueChanged += ArgumentChanged;
                        _eventParamNum[i].SetNumeric(true);
                        _paramEditorParamBox[i].Append(_eventParamNum[i]);
                        break;
                    }
                case CommandArg.ValueType.Boolean:
                    {
                        _eventParamCheck[i] = Gtk.CheckButton.New();
                        _eventParamCheck[i].Active = ca[i].Value != 0;
                        _eventParamCheck[i].OnToggled += ArgumentToggled;
                        _paramEditorParamBox[i].Append(_eventParamCheck[i]);
                        break;
                    }
                case CommandArg.ValueType.Number:
                    {
                        _eventParamNum![i] = Gtk.SpinButton.New(Gtk.Adjustment.New(ca[i].Value, ca[i].MinValue, ca[i].MaxValue, 1, 1, 1), 1, 0);
                        _eventParamNum[i].OnValueChanged += ArgumentChanged;
                        _eventParamNum[i].SetNumeric(true);
                        _paramEditorParamBox[i].Append(_eventParamNum[i]);
                        break;
                    }
            }

            // TypeInfo valueType;
            // object value;
            // if (ca[i].MemberType == MemberTypes.Field)
            // {
            //     valueType = (TypeInfo)((FieldInfo)ca[i]).FieldType;
            //     value = ((FieldInfo)ca[i]).GetValue(se.Command)!;
            // }
            // else
            // {
            //     valueType = (TypeInfo)((PropertyInfo)ca[i]).PropertyType;
            //     value = ((PropertyInfo)ca[i]).GetValue(se.Command)!;
            // }
            // object lower = null!;
            // object upper = null!;
            // foreach (var val in valueType.DeclaredFields)
            // {
            //     if (val.Name == "MinValue")
            //     {
            //         lower = val.GetValue(ca[i])!;
            //     }
            //     if (val.Name == "MaxValue")
            //     {
            //         upper = val.GetValue(ca[i])!;
            //     }
            // }

            // if (isAnOffset)
            // {
            //     _eventParamOffset!.SetValue((long)Convert.ChangeType(value, TypeCode.Int64));
            //     _paramEditorParamBox[i].Append(_eventParamOffset);
            // }
            // else
            // {
            //     value = (double)Convert.ChangeType(value, TypeCode.Double);
            //     if (lower is not null && upper is not null)
            //     {
            //         lower = (double)Convert.ChangeType(lower, TypeCode.Double);
            //         upper = (double)Convert.ChangeType(upper, TypeCode.Double);
            //     }
            //     else
            //     {
            //         lower = -100d;
            //         upper = 100d;
            //     }
            //     _eventParamNum![i] = Gtk.SpinButton.New(Gtk.Adjustment.New((double)value, (double)lower, (double)upper, 1, 1, 1), 1, 0);
            //     _eventParamNum[i].OnValueChanged += ArgumentChanged;
            //     _eventParamNum[i].SetNumeric(true);
            //     _paramEditorParamBox[i].Append(_eventParamNum[i]);
            // }
            // value = (double)Convert.ChangeType(value, TypeCode.Double);
            // if (lower is not null && upper is not null)
            // {
            //     lower = (double)Convert.ChangeType(lower, TypeCode.Double);
            //     upper = (double)Convert.ChangeType(upper, TypeCode.Double);
            // }
            // else
            // {
            //     lower = -100d;
            //     upper = 100d;
            // }
            // _eventParamNum![i] = Gtk.SpinButton.New(Gtk.Adjustment.New(ca[i].Value, ca[i].MinValue, ca[i].MaxValue, 1, 1, 1), 1, 0);
            // _eventParamNum[i].OnValueChanged += ArgumentChanged;
            // _eventParamNum[i].SetNumeric(true);
            // _paramEditorParamBox[i].Append(_eventParamNum[i]);

            _paramEditorBox!.Append(_paramEditorParamBox[i]);
        }
    }

    private void ArgumentToggled(Gtk.CheckButton sender, EventArgs args)
    {
        for (int i = 0; i < _paramEditorParamBox!.Length; i++)
        {
            if (sender == _eventParamCheck![i])
            {
                SongEvent se = Engine.Instance!.Player.LoadedSong!.Events[_trackDropDown!.Selected]![(int)_selectionModel!.Selected];
                object value = _eventParamCheck[i].Active;
                MemberInfo m = se.Command.GetType().GetMember(_eventParamLabel![i].Label_!)[0];
                if (m is FieldInfo f)
                {
                    f.SetValue(se.Command, Convert.ChangeType(value, f.FieldType));
                }
                else if (m is PropertyInfo p)
                {
                    p.SetValue(se.Command, Convert.ChangeType(value, p.PropertyType));
                }

                // ((TrackData)_eventsModel.GetObject(_selectionModel.Selected)!).OnArgsChanged!.Invoke();

                return;
            }
        }
    }

    private void ArgumentChanged(Gtk.SpinButton sender, EventArgs args)
    {
        for (int i = 0; i < _paramEditorParamBox!.Length; i++)
        {
            if (sender == _eventParamNum![i])
            {
                SongEvent se = Engine.Instance!.Player.LoadedSong!.Events[_trackDropDown!.Selected]![(int)_selectionModel!.Selected];
                object value = _eventParamNum[i].Value;
                MemberInfo m = se.Command.GetType().GetMember(_eventParamLabel![i].Label_!)[0];
                if (m is FieldInfo f)
                {
                    f.SetValue(se.Command, Convert.ChangeType(value, f.FieldType));
                }
                else if (m is PropertyInfo p)
                {
                    p.SetValue(se.Command, Convert.ChangeType(value, p.PropertyType));
                }

                // ((TrackData)_eventsModel.GetObject(_selectionModel.Selected)!).OnArgsChanged!.Invoke();

                return;
            }
        }
    }

    public void ReloadDropDownEntries()
    {
        if (_trackList!.NItems is not 0)
        {
            _trackList.Splice(0, _trackList.NItems, null);
        }
        if (_eventList!.NItems is not 0)
        {
            _eventList.Splice(0, _eventList.NItems, null);
        }

        for (int i = 0; i < Engine.Instance?.Player.LoadedSong?.Events.Length; i++)
        {
            _trackList.Append($"Track {i}");
        }
        for (int i = 0; i < Engine.Instance?.GetCommands().Length; i++)
        {
            _eventList.Append(Engine.Instance?.GetCommands()[i].Label!);
        }
    }

    public void ReloadColumnEntries()
    {
        if (_eventsModel.GetNItems() is not 0)
        {
            _eventsModel.RemoveAll();
            EventsData = [];
        }

        if (Engine.Instance is null) return;
        if (Engine.Instance.Player.LoadedSong is null) return;

        string cssCode = "";
        List<string> eventsUsed = [];
        foreach (var trackEvent in Engine.Instance.Player.LoadedSong.Events[_trackDropDown!.Selected]!)
        {
            var hexCode = $"{trackEvent.Command.Color.R:X2}{trackEvent.Command.Color.G:X2}{trackEvent.Command.Color.B:X2}".ToLower();
            var hexCodeLit = $"{(byte)(trackEvent.Command.Color.R + ((255 - trackEvent.Command.Color.R) / 2f)):X2}{(byte)(trackEvent.Command.Color.G + ((255 - trackEvent.Command.Color.G) / 2f)):X2}{(byte)(trackEvent.Command.Color.B + ((255 - trackEvent.Command.Color.B) / 2f)):X2}".ToLower();

            var eventClass = $"{trackEvent.Command.Label}command".ToLower().Replace(' ', '-');

            var hslColor = new HSLColor(trackEvent.Command.Color);
            var textColor = hslColor.Lightness <= 0.6 && hslColor.R <= 0.7 && hslColor.G <= 0.7 ? "white" : "black";
            if (!eventsUsed.Contains(eventClass))
            {
                cssCode += $"columnview > listview > #row-{eventClass}" + " {" +
                    "background-image: none;" +
                    $"background-color: #{hexCode};" +
                    $"color: {textColor};" +
                "} ";
                cssCode += $"columnview > listview > #row-{eventClass}:hover" + " {" +
                    "background-image: none;" +
                    $"background-color: #{hexCodeLit};" +
                    $"color: inherit;" +
                "} ";
                // cssCode += $"columnview > listview > #row-{eventClass}.activatable:hover" + " {" +
                //     "background-image: none;" +
                //     $"background-color: #{hexCodeLit};" +
                //     $"color: white;" +
                // "} ";
                // cssCode += $"columnview > listview > #row-{eventClass}.{eventClass}:hover" + " {" +
                //     "background-image: none;" +
                //     $"background-color: #{hexCodeLit};" +
                //     $"color: inherit;" +
                // "} ";
                cssCode += $"columnview > listview > #row-{eventClass}:selected" + " {" +
                    $"color: inherit;" +
                "} ";
                eventsUsed.Add(eventClass);
            }
        }
        _cssProvider ??= Gtk.CssProvider.New();
        _cssProvider.LoadFromString(cssCode);
        Gtk.StyleContext.RemoveProviderForDisplay(EventsColumnView!.GetDisplay(), _cssProvider);
        Gtk.StyleContext.AddProviderForDisplay(EventsColumnView!.GetDisplay(), _cssProvider, 0);
        foreach (var trackEvent in Engine.Instance.Player.LoadedSong.Events[_trackDropDown.Selected]!)
        {
            var numTicks = new long[trackEvent.Ticks.Count];
            int t = 0;
            foreach (var ticks in trackEvent.Ticks)
            {
                numTicks[t++] = ticks;
            }
            EventsData!.Add(TrackData.AddEntry(trackEvent.Command, trackEvent.Offset, numTicks));
        }

        foreach (var data in EventsData!)
        {
            _eventsModel.Append(data);
        }
        UpdateParamBoxes();
    }

    internal void UpdateTracks()
    {
        ReloadDropDownEntries();
        ReloadColumnEntries();
    }

    private void TrackSelected(GObject.Object sender, NotifySignalArgs args)
    {
        if (_trackDropDown!.SelectedItem is not null)
        {
            ReloadColumnEntries();
        }
    }

    private void OnSetupRow(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
    {
        //
    }

    private void OnBindRow(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is Gtk.ColumnViewRow row)
        {
            //
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

    private void OnBindEventTypeText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not TrackData userData) return;
        if (userData.Command is null) return;

        var eventClass = $"{userData.Command.Label}command".ToLower().Replace(' ', '-');

        label.GetParent()!.GetParent()!.SetName($"row-{eventClass}");

        label.SetText(userData.Command.Label);
    }
    private void OnBindArgumentsText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not TrackData userData) return;
        if (userData.Command is null) return;

        label.SetText(userData.Command.Arguments);

        userData.OnArgsChanged += ChangeArguments;

        void ChangeArguments()
        {
            label.SetText(userData.Command.Arguments);
        }
    }
    private void OnUnbindArgumentsText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.UnbindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not TrackData userData) return;
        if (userData.Command is null) return;

        // userData.OnArgsChanged = null;
    }
    private void OnBindOffsetText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not TrackData userData) return;

        label.SetText(string.Format("0x{0:X}", userData.Offset));
    }
    private void OnBindTicksText(Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
    {
        if (args.Object is not Gtk.ListItem listItem)
        {
            return;
        }

        if (listItem.Child is not Gtk.Label label) return;
        if (listItem.Item is not TrackData userData) return;
        if (userData.Ticks is null) return;

        var array = userData.Ticks;
        var str = "";
        foreach (var val in array)
        {
            str = string.Join(", ", val);
        }

        label.SetText(str);
    }
}