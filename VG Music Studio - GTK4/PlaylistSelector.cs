using System;
using System.Collections.Generic;
using GObject;
using Gtk;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Properties;

namespace Kermalis.VGMusicStudio.GTK4;

[Subclass<Box>]
internal partial class PlaylistSelector
{
    public PlaylistDropDown? PlaylistSelectDropDown { get; set; }
    public PlaylistDropDown? PlaylistSelectSongDropDown { get; set; }
    public GestureClick? PlaylistClick { get; set; }
    public GestureClick? PlaylistSongClick { get; set; }
    public ToggleButton? ButtonPlayPlist;
    public Button? ButtonPlistStyle;
    public Button? ButtonPrevPlistSong, ButtonNextPlistSong;
    public uint SelectedPlaylistIndex, SelectedSongIndex = 0;
    public static int PrevSelectedPlaylistIndex, PrevSelectedSongIndex = -1;
    private static Gio.ListStore PlaylistModel = Gio.ListStore.New<PlaylistData>();
    private static Gio.ListStore PlaylistSongModel = Gio.ListStore.New<PlaylistData>();
    private List<Config.Playlist>? Playlists { get; set; }
    internal List<Config.Song>? Songs { get; set; }
    private Box? PlaylistBox, PlaylistSongBoxDropDown;
    internal Box? PlaylistSongBox;

    [Subclass<GObject.Object>]
    internal partial class PlaylistData
    {
        internal int Index { get; set; }
        internal string Title { get; set; }
        internal string ImageName { get; set; }

        internal static PlaylistData AddEntry(string title, string image, int index)
        {
            var obj = NewWithProperties([]);
            obj.Title = title;
            obj.ImageName = image;
            obj.Index = index;
            return obj;
        }

        internal string GetTitle()
        {
            return Title;
        }
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        Spacing = 1;
        Halign = Align.Center;

        IconTheme.GetForDisplay(Gdk.Display.GetDefault()!).AddResourcePath("/org/Kermalis/VGMusicStudio/GTK4/icons/scalable/actions");

        ButtonPlayPlist = ToggleButton.NewWithProperties([]);
        ButtonPlayPlist.Sensitive = false; ButtonPlayPlist.TooltipText = "Play Playlist"; ButtonPlayPlist.IconName = "vgms-play-playlist-symbolic";
        ButtonPlistStyle = Button.NewWithProperties([]);
        ButtonPlistStyle.Sensitive = false; ButtonPlistStyle.TooltipText = "Play Style";

        ButtonPrevPlistSong = Button.NewWithProperties([]);
        ButtonPrevPlistSong.Sensitive = false; ButtonPrevPlistSong.TooltipText = Strings.PlayerPreviousSong; ButtonPrevPlistSong.IconName = "media-skip-backward-symbolic";
        ButtonNextPlistSong = Button.NewWithProperties([]);
        ButtonNextPlistSong.Sensitive = false; ButtonNextPlistSong.TooltipText = Strings.PlayerNextSong; ButtonNextPlistSong.IconName = "media-skip-forward-symbolic";

        static ConstructArgument[] DropDownArgs()
        {
            ConstructArgument[] args =
            [
                new ConstructArgument("width_request", new Value(300)),
                new ConstructArgument("sensitive", new Value(0)),
            ];
            return args;
        }
        PlaylistSelectDropDown = PlaylistDropDown.NewWithProperties(DropDownArgs());
        PlaylistSelectDropDown.SetModel(PlaylistModel);
        PlaylistSelectSongDropDown = PlaylistDropDown.NewWithProperties(DropDownArgs());
        PlaylistSelectSongDropDown.SetModel(PlaylistSongModel);

        PlaylistBox = New(Orientation.Horizontal, 4);
        PlaylistBox.Halign = Align.Center;
        PlaylistSongBox = New(Orientation.Horizontal, 4);
        PlaylistSongBox.Halign = Align.Center;

        PlaylistBox.Append(ButtonPlayPlist);
        PlaylistBox.Append(PlaylistSelectDropDown);
        PlaylistBox.Append(ButtonPlistStyle);
        PlaylistSongBoxDropDown = New(Orientation.Horizontal, 1);
        PlaylistSongBoxDropDown.Halign = Align.Center;
        PlaylistSongBoxDropDown.Append(PlaylistSelectSongDropDown);
        PlaylistSongBox.MarginStart = 40;
        PlaylistSongBox.MarginEnd = 40;
        PlaylistSongBox.Append(ButtonPrevPlistSong);
        PlaylistSongBox.Append(PlaylistSongBoxDropDown);
        PlaylistSongBox.Append(ButtonNextPlistSong);

        Append(PlaylistBox);
        Append(PlaylistSongBox);
    }

    [Subclass<DropDown>]
    internal partial class PlaylistDropDown
    {
        private SignalListItemFactory SelectionFactory;
        private SignalListItemFactory EntryListFactory;

        private SelectedEntry[] ContentSelection;
        private ListEntry[] ContentList;

        partial void Initialize()
        {
            SelectionFactory = SignalListItemFactory.New();
            SelectionFactory.OnSetup += SelectionSetup;
            SelectionFactory.OnBind += SelectionBind;
            SetFactory(SelectionFactory);

            EntryListFactory = SignalListItemFactory.New();
            EntryListFactory.OnSetup += EntryListSetup;
            EntryListFactory.OnBind += EntryListBind;
            SetListFactory(EntryListFactory);
        }

        internal void StorePlaylists(List<Config.Playlist> playlists)
        {
            InitData(playlists.Count);
            for (int i = 0; i < playlists.Count; i++)
            {
                ContentSelection[i] = SelectedEntry.Add(playlists[i].Name);
                ContentList[i] = ListEntry.Add(playlists[i].Name);
            }
        }

        internal void StoreSongs(List<Config.Song> songs)
        {
            InitData(songs.Count);
            for (int i = 0; i < songs.Count; i++)
            {
                ContentSelection[i] = SelectedEntry.Add(songs[i].Name);
                ContentList[i] = ListEntry.Add(songs[i].Name);
            }
        }

        private void InitData(int length)
        {
            ContentSelection = new SelectedEntry[length];
            ContentList = new ListEntry[length];
        }

        private void SelectionSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
        {
            if (args.Object is not ListItem)
            {
                return;
            }
        }

        private void SelectionBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
        {
            if (args.Object is not ListItem item)
            {
                return;
            }
            if (item.GetItem() is not PlaylistData holder)
            {
                return;
            }
            item.SetChild(ContentSelection[holder.Index]);
        }

        private void EntryListSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
        {
            if (args.Object is not ListItem)
            {
                return;
            }
        }

        private void EntryListBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
        {
            if (args.Object is not ListItem item)
            {
                return;
            }
            if (item.Item is not PlaylistData holder)
            {
                return;
            }
            item.SetChild(ContentList[holder.Index]);
            void ChangeCheckmark(GObject.Object sender, NotifySignalArgs args)
            {
                OnSelectedItemChanged(item);
            }
            SelectedItemPropertyDefinition.Notify(this, ChangeCheckmark);

            OnSelectedItemChanged(item);
        }

        private void OnSelectedItemChanged(ListItem item)
        {
            if (item.GetChild() is not ListEntry contents)
            {
                return;
            }
            var checkmark = contents.GetLastChild() as Image;
            checkmark!.SetVisible(GetSelectedItem() == item.Item);
        }

        [Subclass<Box>]
        internal partial class SelectedEntry
        {
            internal Label Title = Label.New("");
            internal Image Image = Image.New();

            internal static SelectedEntry Add(string title)
            {
                var box = NewWithProperties([]);
                box.SetOrientation(Orientation.Horizontal);
                box.SetSpacing(10);
                box.Title = Label.New(title);
                box.Title.SetXalign(0);
                box.Title.SetMaxWidthChars(50);
                box.Title.SetEllipsize(Pango.EllipsizeMode.End);
                box.Append(box.Title);
                box.Append(box.Image);
                return box;
            }
        }

        [Subclass<Box>]
        internal partial class ListEntry
        {
            internal Label Title = Label.New("");
            internal Image Image = Image.New();
            internal Image Checkmark = Image.NewFromIconName("object-select-symbolic");

            internal static ListEntry Add(string title)
            {
                var box = NewWithProperties([]);
                box.SetOrientation(Orientation.Horizontal);
                box.SetSpacing(10);
                box.Title = Label.New(title);
                box.Title.SetXalign(0);
                box.Title.SetMaxWidthChars(50);
                box.Title.SetEllipsize(Pango.EllipsizeMode.End);
                box.Checkmark.SetVisible(false);
                box.Append(box.Title);
                box.Append(box.Image);
                box.Append(box.Checkmark);
                return box;
            }
        }
    }

    internal void PlaylistStringSelect()
    {
        AddSongEntries();
    }

    internal Config.Playlist GetPlaylist()
    {
        Config.Playlist playlist = null!;
        foreach (Config.Playlist plist in Playlists!)
        {
            PlaylistData selectedItem = (PlaylistData)PlaylistSelectDropDown!.GetSelectedItem()!;
            var selectedItemName = selectedItem.Title;
            if (plist.Name == selectedItemName)
            {
                playlist = plist;
            }
        }
        return playlist;
    }

    internal uint GetPlaylistSongIndex(int index)
    {
        var numItems = PlaylistSongModel!.GetNItems();
        var newIndex = PlaylistSelectDropDown!.Selected;
        for (int i = 0; i < numItems; i++)
        {
            if (Songs![i].Index.Equals(index))
            {
                newIndex = (uint)i;
            }
        }
        return newIndex;
    }

    internal int GetSongIndex(uint index)
    {
        var strObj = (PlaylistData)PlaylistSelectSongDropDown!.SelectedItem!;
        var selectedItemName = strObj.Title;
        var newIndex = (int)index;
        foreach (var song in Songs!)
        {
            if (song.Name.Equals(selectedItemName))
            {
                newIndex = song.Index;
            }
        }
        return newIndex;
    }

    internal static int GetNumSongs()
    {
        return (int)PlaylistSongModel.GetNItems();
    }

    internal void AddPlaylistEntries(List<Config.Playlist> playlists)
    {
        Playlists = playlists;
        if (PlaylistModel!.GetNItems() is not 0)
        {
            PlaylistModel.RemoveAll();
        }
        PlaylistSelectDropDown!.StorePlaylists(playlists);
        int i = 0;
        foreach (Config.Playlist plist in Playlists)
        {
            var data = PlaylistData.AddEntry(plist.Name, "vgms-playlist-symbolic", i++);
            PlaylistModel.Append(data);
        }
        PrevSelectedPlaylistIndex = (int)(PlaylistSelectDropDown.Selected = SelectedPlaylistIndex = PlaylistSelectDropDown.Selected = PlaylistModel.GetNItems() - 1); // So that "All Songs" main playlist is selected
    }
    internal void AddSongEntries()
    {
        if (PlaylistSongModel!.GetNItems() is not 0)
        {
            PlaylistSongModel.RemoveAll();
        }
        Songs = Playlists![(int)PlaylistSelectDropDown!.Selected].Songs;
        PlaylistSelectSongDropDown!.StoreSongs(Playlists![(int)PlaylistSelectDropDown!.Selected].Songs);
        for (int i = 0; i < Songs.Count; i++)
        {
            var data = PlaylistData.AddEntry(Songs[i].Name, "vgms-song-symbolic", i);
            PlaylistSongModel.Append(data);
        }
        PrevSelectedSongIndex = (int)(SelectedSongIndex = PlaylistSelectSongDropDown.Selected);
    }
}