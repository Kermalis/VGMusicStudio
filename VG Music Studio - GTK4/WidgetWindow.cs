using Adw;

namespace Kermalis.VGMusicStudio.GTK4;

[GObject.Subclass<Window>]
internal partial class WidgetWindow
{
    internal Gtk.Box WidgetBox;
    internal static WidgetWindow CreateWindow(Gtk.Widget widget)
    {
        WidgetWindow args = NewWithProperties([]);
        args.Title = MainWindow.GetProgramName();

        var header = HeaderBar.New();
        args.WidgetBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        args.WidgetBox.Append(widget);

        var box = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        box.Append(header);
        box.Append(args.WidgetBox);
        
        args.SetContent(box);

        return args;
    }
}