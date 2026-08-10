using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Gtk;
using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core;
using static Gtk.SignalListItemFactory;

namespace Kermalis.VGMusicStudio.GTK4;

[GObject.Subclass<Viewport>]
internal partial class SequencedAudio_List
{
	private bool _isSongTable = false;
	internal bool IsInitialized = false;
	public bool HasSelectedRow = false;
	private string? _selectedRowName;

	private int _curSong = 0;

	private EndianBinaryReader? _reader;

	private readonly Gio.ListStore _model = Gio.ListStore.New<Data>();
	private SignalListItemFactory? _seqListItemFactory;
	private SingleSelection? _selectionModel;
	private ScrollInfo? _scrollInfo = ScrollInfo.New();

	private GestureClick? _columnViewGestureClick;

	private ColumnView? _columnView;

	private ColumnViewColumn _columnName = null!;
	private ColumnViewColumn _columnPlist = null!;
	private ColumnViewColumn _columnSongTableOffset = null!;
	private ColumnViewColumn _columnSequenceOffset = null!;

	public Data[]? SoundData { get; set; }

	[GObject.Subclass<GObject.Object>]
	internal partial class Data
	{
		internal int Id { get; private set; }
		internal string? InternalName { get; private set; }
		internal string? PlaylistName { get; private set; }
		internal string? SongTableOffset { get; private set; }
		internal string? SequenceOffset { get; private set; }

		internal static Data AddEntry(int id, string name, string plistname, string songTableOffset, string seqOffset)
		{
			var dat = NewWithProperties([]);
			dat.Id = id;
			dat.InternalName = name;
			dat.PlaylistName = plistname;
			if (songTableOffset is not null)
			{
				dat.SongTableOffset = songTableOffset;
				dat.SequenceOffset = seqOffset;
			}
			return dat;
		}
	}

	public void AddEntries(long numSongs, Config config)
	{
		if (_model.GetNItems() is not 0)
		{
			_model.RemoveAll();
		}

		SoundData = new Data[numSongs];
		var sNames = new string[numSongs];
		for (int i = 0; i < sNames.Length; i++)
		{
			sNames[i] = "";
		}
		if (config.InternalSongNames is not null)
		{
			foreach (Config.InternalSongName sf in config.InternalSongNames)
			{
				foreach (Config.Song s in sf.Songs)
				{
					sNames[s.Index] = s.Name;
				}
			}
		}

		var plistNames = new string[numSongs];
		for (int i = 0; i < plistNames.Length; i++)
		{
			plistNames[i] = "";
		}
		if (config.Playlists is not null)
		{
			foreach (Config.Playlist p in config.Playlists)
			{
				foreach (Config.Song s in p.Songs)
				{
					plistNames[s.Index] = s.Name;
				}
			}
		}

		var sTEntryOffsetString = new string[numSongs];
		var seqOffsetString = new string[numSongs];
		if (config.SongTableOffset is not null)
		{
			_isSongTable = true;
			_reader ??= new EndianBinaryReader(new MemoryStream(config.ROM!));
			for (int i = 0, s = 0; i < SoundData.Length; i++)
			{
				sTEntryOffsetString[i] = string.Format("0x{0:X}", config.SongTableOffset[s] + (i * 8));
				_reader.Stream.Position = config.SongTableOffset[s] + (i * 8);
				var seqOffset = (_reader.ReadUInt32() << 8) >> 8; // To remove the "08" modifier value from the offset (which that value is only for memory usage anyways)
				seqOffsetString[i] = string.Format("0x{0:X}", seqOffset);
				if (s < config.SongTableOffset.Length - 1)
				{
					s++;
				}
			}
		}
		else
		{
			_isSongTable = false;
		}
		for (int i = 0; i < SoundData!.Length; i++)
		{
			SoundData[i] = Data.AddEntry(i, sNames[i], plistNames[i], sTEntryOffsetString[i], seqOffsetString[i]);
		}

		foreach (var data in SoundData!)
		{
			_model.Append(data);
		}
	}

	partial void Initialize()
	{
		var scrolledWindow = ScrolledWindow.New();
		scrolledWindow.SetSizeRequest(600, 200);
		scrolledWindow.SetHexpand(true);

		_columnViewGestureClick = GestureClick.New();
		_columnViewGestureClick.Button = 1;
		_columnViewGestureClick.OnPressed += ColumnViewGestureClick_LeftClick;

		_columnView = ColumnView.New(null);
		_columnView.AddCssClass("data-table");
		_columnView.AddController(_columnViewGestureClick);
		_columnView.SetShowColumnSeparators(true);
		_columnView.SetShowRowSeparators(true);
		_columnView.SetReorderable(false);
		_columnView.SetHexpand(true);

		// ColumnSorter = (ColumnViewSorter)ColumnView.GetSorter()!;
		// ColumnSorter.GetPrimarySortColumn();
		// SortModel = SortListModel.New(Model, ColumnSorter);

		scrolledWindow.SetChild(_columnView);

		Child = scrolledWindow;

		SetVexpand(true);
		SetHexpand(true);
	}

	private void ColumnViewGestureClick_LeftClick(GestureClick sender, GestureClick.PressedSignalArgs args)
	{
		if (_selectionModel?.GetSelectedItem() is Data data)
		{
			if (IsInitialized)
			{
				MainWindow.Instance!.ChangeIndex(data.Id);
			}
		}
	}

	private void SelectionModel_Notified(GObject.Object sender, NotifySignalArgs args)
	{
		_selectedRowName = args.Pspec.GetName();
	}

	internal void Init()
	{
		IsInitialized = false;

		// ID Column
		_seqListItemFactory = SignalListItemFactory.New();
		_seqListItemFactory.OnSetup += OnSetupIDLabel;
		_seqListItemFactory.OnBind += OnBindIDText;

		var idColumn = ColumnViewColumn.New("#", _seqListItemFactory);
		idColumn.SetResizable(true);
		// NewWithProperties(GetGType(), ["id", "internalName", "playlistName", "offset"], [Id, InternalName, PlaylistName, Offset]);
		// var idExpression = Gtk.Internal.PropertyExpression.New(GetGType(), nint.Zero, GLib.Internal.NonNullableUtf8StringOwnedHandle.Create("Id"));
		// var idSorter = NumericSorter.New(new PropertyExpression(idExpression));
		// idColumn.SetSorter(idSorter);
		static int SortByID(Data a, Data b)
		{
			return a.Id.CompareTo(b.Id);
		}
		var idSorter = CustomSorter.New<Data>(SortByID);
		idColumn.SetSorter(idSorter);
		_columnView!.AppendColumn(idColumn);

		// Internal Name Column
		_seqListItemFactory = SignalListItemFactory.New();
		_seqListItemFactory.OnSetup += OnSetupNameLabel;
		_seqListItemFactory.OnBind += OnBindNameText;

		_columnName = ColumnViewColumn.New("Internal Name", _seqListItemFactory);
		_columnName.SetFixedWidth(160);
		_columnName.SetExpand(true);
		_columnName.SetResizable(true);
		static int SortByName(Data a, Data b)
		{
			return string.CompareOrdinal(a.InternalName, b.InternalName);
		}
		var nameSorter = CustomSorter.New<Data>(SortByName);
		_columnName.SetSorter(nameSorter);
		// nameColumn.SetSorter(ColumnSorter);
		_columnView.AppendColumn(_columnName);

		_selectionModel = SingleSelection.New(SortListModel.New(_model, _columnView.GetSorter()));
		_selectionModel.OnNotify += SelectionModel_Notified;

		_columnView.SetModel(_selectionModel);

		IsInitialized = true;
	}

	internal void ChangeColumns(bool isSongTable = false)
	{
		_isSongTable = isSongTable;
		if (_isSongTable)
		{
			_columnName.SetExpand(false);

			// Playlist Name Column
			_seqListItemFactory = SignalListItemFactory.New();
			_seqListItemFactory.OnSetup += OnSetupPlistLabel;
			_seqListItemFactory.OnBind += OnBindPlistText;

			_columnPlist = ColumnViewColumn.New("Playlist Name", _seqListItemFactory);
			_columnPlist.SetFixedWidth(160);
			_columnPlist.SetResizable(true);
			static int SortByPlistName(Data a, Data b)
			{
				return string.CompareOrdinal(a.PlaylistName, b.PlaylistName);
			}
			var plistSorter = CustomSorter.New<Data>(SortByPlistName);
			_columnPlist.SetSorter(plistSorter);
			// plistColumn.SetSorter(ColumnSorter);
			_columnView!.AppendColumn(_columnPlist);

			// Song Table Offset Column
			_seqListItemFactory = SignalListItemFactory.New();
			_seqListItemFactory.OnSetup += OnSetupSongTableOffsetLabel;
			_seqListItemFactory.OnBind += OnBindSongTableOffsetText;

			_columnSongTableOffset = ColumnViewColumn.New("Song Table Offset", _seqListItemFactory);
			_columnSongTableOffset.SetFixedWidth(80);
			_columnSongTableOffset.SetResizable(true);
			static int SortBySongTableOffset(Data a, Data b)
			{
				return string.CompareOrdinal(a.SongTableOffset, b.SongTableOffset);
			}
			var songTableOffsetSorter = CustomSorter.New<Data>(SortBySongTableOffset);
			_columnSongTableOffset.SetSorter(songTableOffsetSorter);
			// offsetColumn.SetSorter(ColumnSorter);
			_columnView.AppendColumn(_columnSongTableOffset);

			// Sequence Offset Column
			_seqListItemFactory = SignalListItemFactory.New();
			_seqListItemFactory.OnSetup += OnSetupSeqOffsetLabel;
			_seqListItemFactory.OnBind += OnBindSeqOffsetText;

			_columnSequenceOffset = ColumnViewColumn.New("Sequence Offset", _seqListItemFactory);
			_columnSequenceOffset.SetFixedWidth(80);
			_columnSequenceOffset.SetExpand(true);
			_columnSequenceOffset.SetResizable(true);
			static int SortBySeqOffset(Data a, Data b)
			{
				return string.CompareOrdinal(a.SequenceOffset, b.SequenceOffset);
			}
			var seqOffsetSorter = CustomSorter.New<Data>(SortBySeqOffset);
			_columnSequenceOffset.SetSorter(seqOffsetSorter);
			// offsetColumn.SetSorter(ColumnSorter);
			_columnView.AppendColumn(_columnSequenceOffset);
		}
		else
		{
			if (_columnPlist is not null)
			{
				_columnView!.RemoveColumn(_columnPlist);
			}
			if (_columnSongTableOffset is not null)
			{
				_columnView!.RemoveColumn(_columnSongTableOffset);
			}
			if (_columnSequenceOffset is not null)
			{
				_columnView!.RemoveColumn(_columnSequenceOffset);
			}
			_columnName.SetExpand(true);
		}
		ConfigureTimer();
	}
	internal void SelectRow(int index)
	{
		HasSelectedRow = true;
		for (uint i = 0; i < _selectionModel!.GetNItems(); i++)
		{
			var obj = _selectionModel?.GetObject(i);
			if (obj is Data data)
			{
				if (data.Id == index)
				{
					_selectionModel?.SelectItem(i, true);
					_columnView!.ScrollTo(i, null, ListScrollFlags.Select, _scrollInfo);
					_curSong = data.Id;
					break;
				}
			}
		}
		HasSelectedRow = false;
	}

	private void ConfigureTimer()
	{
		var timer = GLib.Timer.New(); // Creates a new timer variable
		var context = GLib.MainContext.GetThreadDefault(); // Reads the main context default thread
		var source = GLib.Functions.TimeoutSourceNew(50); // Creates and configures the timeout interval at 50 microseconds, so it updates upon selection
		source.SetCallback(ListCallback); // Sets the callback for the timer interval to be used on
		var microsec = new CULong(source.Attach(context)); // Configures the microseconds based on attaching the GLib MainContext thread
														   // timer.Elapsed(ref microsec); // Adds the pointer to the configured microseconds source
		GLib.Internal.Timer.Elapsed(timer.Handle, ref microsec); // GLib.Timer.Elapsed was removed in GirCore 0.6.3, so we're using this workaround instead
		timer.Start(); // Starts the timer
	}

	private bool ListCallback()
	{
		if (_selectionModel!.GetSelected() != _curSong)
		{
			if (_selectionModel?.GetSelectedItem() is Data list)
			{
				if (IsInitialized)
				{
					MainWindow.Instance!.CheckIndex(list.Id);
					_curSong = list.Id;
				}
			}
		}
		return true;
	}

	private static void OnSetupIDLabel(SignalListItemFactory sender, SetupSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		var label = Label.New(null);
		label.SetEllipsize(Pango.EllipsizeMode.End);
		label.Halign = Align.Center;
		listItem.Child = label;
	}

	private static void OnSetupNameLabel(SignalListItemFactory sender, SetupSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		var label = Label.New(null);
		label.SetEllipsize(Pango.EllipsizeMode.End);
		label.Halign = Align.Start;
		listItem.Child = label;
	}

	private static void OnSetupPlistLabel(SignalListItemFactory sender, SetupSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		var label = Label.New(null);
		label.SetEllipsize(Pango.EllipsizeMode.End);
		label.Halign = Align.Start;
		listItem.Child = label;
	}

	private static void OnSetupSongTableOffsetLabel(SignalListItemFactory sender, SetupSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		var label = Label.New(null);
		label.SetEllipsize(Pango.EllipsizeMode.End);
		label.Halign = Align.Start;
		listItem.Child = label;
	}

	private static void OnSetupSeqOffsetLabel(SignalListItemFactory sender, SetupSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		var label = Label.New(null);
		label.SetEllipsize(Pango.EllipsizeMode.End);
		label.Halign = Align.Start;
		listItem.Child = label;
	}

	private void OnBindIDText(SignalListItemFactory sender, BindSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		if (listItem.Child is not Label label)
		{
			return;
		}

		if (listItem.Item is not Data userData)
		{
			return;
		}

		label.SetText(userData.Id.ToString());
	}

	private void OnBindNameText(SignalListItemFactory sender, BindSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		if (listItem.Child is not Label label)
		{
			return;
		}

		if (listItem.Item is not Data userData)
		{
			return;
		}

		if (userData.InternalName is not null)
		{
			label.SetText(userData.InternalName);
		}
	}

	private void OnBindPlistText(SignalListItemFactory sender, BindSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		if (listItem.Child is not Label label)
		{
			return;
		}

		if (listItem.Item is not Data userData)
		{
			return;
		}

		if (userData.PlaylistName is not null)
		{
			label.SetText(userData.PlaylistName);
		}
	}

	private void OnBindSongTableOffsetText(SignalListItemFactory sender, BindSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		if (listItem.Child is not Label label)
		{
			return;
		}

		if (listItem.Item is not Data userData)
		{
			return;
		}

		if (userData.SongTableOffset is not null)
		{
			label.SetText(userData.SongTableOffset);
		}
	}

	private void OnBindSeqOffsetText(SignalListItemFactory sender, BindSignalArgs args)
	{
		if (args.Object is not ListItem listItem)
		{
			return;
		}

		if (listItem.Child is not Label label)
		{
			return;
		}

		if (listItem.Item is not Data userData)
		{
			return;
		}

		if (userData.SequenceOffset is not null)
		{
			label.SetText(userData.SequenceOffset);
		}
	}
}
