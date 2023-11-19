using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using Kermalis.VGMusicStudio.Core;
using Pango;

namespace Kermalis.VGMusicStudio.GTK4.Util;

internal class SoundSequenceList : Widget, IDisposable
{
	internal static ListItem? ListItem { get; set; }
	internal static long? Index { get; set; }
	internal static new string? Name { get; set; }
	internal static List<Config.Song>? Songs { get; set; }
	//internal SingleSelection Selection { get; set; }
	//private SignalListItemFactory Factory;

	internal SoundSequenceList()
	{
		var box = Box.New(Orientation.Horizontal, 0);
		var label = Label.New("");
		label.SetWidthChars(2);
		label.SetHexpand(true);
		box.Append(label);

		var sw = ScrolledWindow.New();
		sw.SetPropagateNaturalWidth(true);
		var listView = Create(label);
		sw.SetChild(listView);
		box.Prepend(sw);
	}
	
	private static void SetupLabel(SignalListItemFactory factory, EventArgs e)
	{
		var label = Label.New("");
		label.SetXalign(0);
		ListItem!.SetChild(label);
		//e.Equals(label);
	}
	private static void BindName(SignalListItemFactory factory, EventArgs e)
	{
		var label = ListItem!.GetChild();
		var item = ListItem!.GetItem();
		var name = item.Equals(Name);

		label!.SetName(name.ToString());
	}

	private static Widget Create(object item)
	{
		if (item is Config.Song song)
		{
			Index = song.Index;
			Name = song.Name;
		}
		else if (item is Config.Playlist playlist)
		{
			Songs = playlist.Songs;
			Name = playlist.Name;
		}
		var model = Gio.ListStore.New(ColumnView.GetGType());

		var selection = SingleSelection.New(model);
		selection.SetAutoselect(true);
		selection.SetCanUnselect(false);


		var cv = ColumnView.New(selection);
		cv.SetShowColumnSeparators(true);
		cv.SetShowRowSeparators(true);

		var factory = SignalListItemFactory.New();
		factory.OnSetup += SetupLabel;
		factory.OnBind += BindName;
		var column = ColumnViewColumn.New("Name", factory);
		column.SetResizable(true);
		cv.AppendColumn(column);
		column.Unref();

		return cv;
	}

	internal int Add(object item)
	{
		return Add(item);
	}
	internal int AddRange(Span<object> items)
	{
		foreach (object item in items)
		{
			Create(item);
		}
		return AddRange(items);
	}

	//internal SignalListItemFactory Items
	//{
	//	get
	//	{
	//		if (Factory is null)
	//		{
	//			Factory = SignalListItemFactory.New();
	//		}

	//		return Factory;
	//	}
	//}

	internal object SelectedItem
	{
		get
		{
			int index = (int)Index!;
			return (index == -1) ? null : ListItem.Item.Equals(index);
		}
		set
		{
			int x = -1;

			if (ListItem is not null)
			{
				//
				if (value is not null)
				{
					x = ListItem.GetPosition().CompareTo(value);
				}
				else
				{
					Index = -1;
				}
			}

			if (x != -1)
			{
				Index = x;
			}
		}
	}
}

internal class SoundSequenceListItem
{
	internal object Item { get; }
	internal SoundSequenceListItem(object item)
	{
		Item = item;
	}

	public override string ToString()
	{
		return Item.ToString();
	}
}
