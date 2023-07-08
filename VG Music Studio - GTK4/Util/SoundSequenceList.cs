using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;

namespace Kermalis.VGMusicStudio.GTK4.Util;

internal class SoundSequenceList : Widget, IDisposable
{
    internal static Gio.ListStore Store;
    internal static SortListModel Model;
    internal static ColumnView View;
    static SignalListItemFactory Factory;

    static long Index;
    static string Name;

    internal object Item { get; }

    internal SoundSequenceList(string name)
    {
        // Import the variables
        Name = name;

        // Allow the box to be scrollable
        var sw = ScrolledWindow.New();
        sw.SetHasFrame(true);
        sw.SetPolicy(PolicyType.Never, PolicyType.Automatic);

        // Create a Sort List Model
        Model = CreateModel();
        Model.Unref();

        sw.SetChild(View);

        Item = View;

        // Add the Columns
        AddColumns(View);
    }
    internal SoundSequenceList()
    {
        // Allow the box to be scrollable
        var sw = ScrolledWindow.New();
        sw.SetHasFrame(true);
        sw.SetPolicy(PolicyType.Never, PolicyType.Automatic);

        // Create a Sort List Model
        Model = CreateModel();
        Model.Unref();

        sw.SetChild(View);

        // Create a new name
        Name = new string("Name");

        // Add the Columns
        AddColumns(View);

        Item = View;
    }

    static SortListModel CreateModel()
    {
        // Create List Store
        Store = Gio.ListStore.New(SortListModel.GetGType());

        // Create Column View with a Single Selection that reads from List Store
        View = ColumnView.New(SingleSelection.New(Store));

        // Create Sort List Model
        var model = SortListModel.New(Store, View.Sorter);

        // Add data to the list store
        for (int i = 0; i < Index; i++)
        {
            Store.Append(model);
        }

        return model;
    }
    
    //static void FixedToggled(object sender, EventArgs e)
    //{
    //    var model = Model;

    //    var fixedBit = new bool();
    //    fixedBit ^= true;
    //}

    static void AddColumns(ColumnView columnView)
    {
        Factory = SignalListItemFactory.New();
        //var renderer = ToggleButton.New();
        //renderer.OnToggled += FixedToggled;
        var colName = ColumnViewColumn.New(Name, Factory);
        columnView.AppendColumn(colName);
    }

    internal int AddItem(object item)
    {
        return AddItem(item);
    }
    internal int AddRange(Span<object> item)
    {
        return AddRange(item);
    }
}
