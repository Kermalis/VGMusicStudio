using System;
using System.Runtime.InteropServices;
using Gtk.Internal;

namespace Gtk;

public partial class AlertDialog : GObject.Object
{
    protected AlertDialog(IntPtr handle, bool ownedRef) : base(handle, ownedRef)
    {
    }

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_alert_dialog_new")]
    private static extern nint linux_gtk_alert_dialog_new(string format);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_alert_dialog_new")]
    private static extern nint macos_gtk_alert_dialog_new(string format);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_alert_dialog_new")]
    private static extern nint windows_gtk_alert_dialog_new(string format);

    private static IntPtr ObjPtr;

    public static AlertDialog New(string format)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ObjPtr = linux_gtk_alert_dialog_new(format);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ObjPtr = macos_gtk_alert_dialog_new(format);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ObjPtr = windows_gtk_alert_dialog_new(format);
        }
        return new AlertDialog(ObjPtr, true);
    }
}

public partial class FileDialog : GObject.Object
{
    [DllImport("libgobject-2.0.so.0", EntryPoint = "g_object_unref")]
    private static extern void LinuxUnref(nint obj);

    [DllImport("libgobject-2.0.0.dylib", EntryPoint = "g_object_unref")]
    private static extern void MacOSUnref(nint obj);

    [DllImport("libgobject-2.0-0.dll", EntryPoint = "g_object_unref")]
    private static extern void WindowsUnref(nint obj);

    [DllImport("libgio-2.0.so.0", EntryPoint = "g_task_return_value")]
    private static extern void LinuxReturnValue(nint task, nint result);

    [DllImport("libgio-2.0.0.dylib", EntryPoint = "g_task_return_value")]
    private static extern void MacOSReturnValue(nint task, nint result);

    [DllImport("libgio-2.0-0.dll", EntryPoint = "g_task_return_value")]
    private static extern void WindowsReturnValue(nint task, nint result);

    [DllImport("libgio-2.0.so.0", EntryPoint = "g_file_get_path")]
    private static extern string LinuxGetPath(nint file);

    [DllImport("libgio-2.0.0.dylib", EntryPoint = "g_file_get_path")]
    private static extern string MacOSGetPath(nint file);

    [DllImport("libgio-2.0-0.dll", EntryPoint = "g_file_get_path")]
    private static extern string WindowsGetPath(nint file);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_css_provider_load_from_data")]
    private static extern void LinuxLoadFromData(nint provider, string data, int length);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_css_provider_load_from_data")]
    private static extern void MacOSLoadFromData(nint provider, string data, int length);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_css_provider_load_from_data")]
    private static extern void WindowsLoadFromData(nint provider, string data, int length);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_new")]
    private static extern nint LinuxNew();

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_new")]
    private static extern nint MacOSNew();

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_new")]
    private static extern nint WindowsNew();

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_get_initial_file")]
    private static extern nint LinuxGetInitialFile(nint dialog);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_get_initial_file")]
    private static extern nint MacOSGetInitialFile(nint dialog);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_get_initial_file")]
    private static extern nint WindowsGetInitialFile(nint dialog);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_get_initial_folder")]
    private static extern nint LinuxGetInitialFolder(nint dialog);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_get_initial_folder")]
    private static extern nint MacOSGetInitialFolder(nint dialog);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_get_initial_folder")]
    private static extern nint WindowsGetInitialFolder(nint dialog);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_get_initial_name")]
    private static extern string LinuxGetInitialName(nint dialog);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_get_initial_name")]
    private static extern string MacOSGetInitialName(nint dialog);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_get_initial_name")]
    private static extern string WindowsGetInitialName(nint dialog);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_set_title")]
    private static extern void LinuxSetTitle(nint dialog, string title);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_set_title")]
    private static extern void MacOSSetTitle(nint dialog, string title);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_set_title")]
    private static extern void WindowsSetTitle(nint dialog, string title);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_set_filters")]
    private static extern void LinuxSetFilters(nint dialog, nint filters);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_set_filters")]
    private static extern void MacOSSetFilters(nint dialog, nint filters);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_set_filters")]
    private static extern void WindowsSetFilters(nint dialog, nint filters);

    public delegate void GAsyncReadyCallback(nint source, nint res, nint user_data);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_open")]
    private static extern void LinuxOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_open")]
    private static extern void MacOSOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_open")]
    private static extern void WindowsOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_open_finish")]
    private static extern nint LinuxOpenFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_open_finish")]
    private static extern nint MacOSOpenFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_open_finish")]
    private static extern nint WindowsOpenFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_save")]
    private static extern void LinuxSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_save")]
    private static extern void MacOSSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_save")]
    private static extern void WindowsSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_save_finish")]
    private static extern nint LinuxSaveFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_save_finish")]
    private static extern nint MacOSSaveFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_save_finish")]
    private static extern nint WindowsSaveFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_select_folder")]
    private static extern void LinuxSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_select_folder")]
    private static extern void MacOSSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_select_folder")]
    private static extern void WindowsSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_file_dialog_select_folder_finish")]
    private static extern nint LinuxSelectFolderFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4.1.dylib", EntryPoint = "gtk_file_dialog_select_folder_finish")]
    private static extern nint MacOSSelectFolderFinish(nint dialog, nint result, nint error);

    [DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_dialog_select_folder_finish")]
    private static extern nint WindowsSelectFolderFinish(nint dialog, nint result, nint error);

    private static IntPtr ObjPtr;
    private GAsyncReadyCallback callbackHandle { get; set; }

    private FileDialog(IntPtr handle, bool ownedRef) : base(handle, ownedRef)
    {
    }

    // GtkFileDialog* gtk_file_dialog_new (void)
    public static FileDialog New()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ObjPtr = LinuxNew();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ObjPtr = MacOSNew();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ObjPtr = WindowsNew();
        }
        return new FileDialog(ObjPtr, true);
    }

    // void gtk_file_dialog_open (GtkFileDialog* self, GtkWindow* parent, GCancellable* cancellable, GAsyncReadyCallback callback, gpointer user_data)
    public void Open(nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxOpen(ObjPtr, parent, cancellable, callback, user_data);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSOpen(ObjPtr, parent, cancellable, callback, user_data);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsOpen(ObjPtr, parent, cancellable, callback, user_data);
        }
    }

    // GFile* gtk_file_dialog_open_finish (GtkFileDialog* self, GAsyncResult* result, GError** error)
    public nint OpenFinish(nint result, nint error)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LinuxOpenFinish(ObjPtr, result, error);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOSOpenFinish(ObjPtr, result, error);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsOpenFinish(ObjPtr, result, error);
        }
        return OpenFinish(result, error);
    }

    // void gtk_file_dialog_save (GtkFileDialog* self, GtkWindow* parent, GCancellable* cancellable, GAsyncReadyCallback callback, gpointer user_data)
    public void Save(nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxSave(ObjPtr, parent, cancellable, callback, user_data);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSSave(ObjPtr, parent, cancellable, callback, user_data);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsSave(ObjPtr, parent, cancellable, callback, user_data);
        }
    }

    // GFile* gtk_file_dialog_save_finish (GtkFileDialog* self, GAsyncResult* result, GError** error)
    public nint SaveFinish(nint result, nint error)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LinuxSaveFinish(ObjPtr, result, error);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOSSaveFinish(ObjPtr, result, error);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsSaveFinish(ObjPtr, result, error);
        }
        return SaveFinish(result, error);
    }

    // void gtk_file_dialog_select_folder (GtkFileDialog* self, GtkWindow* parent, GCancellable* cancellable, GAsyncReadyCallback callback, gpointer user_data)
    public void SelectFolder(nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data)
    {
        // if (cancellable is null)
        // {
        //     cancellable = nint.New();
        //     cancellable.Handle.Equals(IntPtr.Zero);
        //     cancellable.Cancel();
        //     user_data = IntPtr.Zero;
        // }

        
        // callback = (source, res) =>
        // {
        //     var data = new nint();
        //     callbackHandle.BeginInvoke(source.Handle, res.Handle, data, callback, callback);
        // };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxSelectFolder(ObjPtr, parent, cancellable, callback, user_data);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSSelectFolder(ObjPtr, Handle, cancellable, callback, user_data);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsSelectFolder(ObjPtr, Handle, cancellable, callback, user_data);
        }
    }

    // GFile* gtk_file_dialog_select_folder_finish(GtkFileDialog* self, GAsyncResult* result, GError** error)
    public nint SelectFolderFinish(nint result, nint error)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LinuxSelectFolderFinish(ObjPtr, result, error);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOSSelectFolderFinish(ObjPtr, result, error);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsSelectFolderFinish(ObjPtr, result, error);
        }
        return SelectFolderFinish(result, error);
    }

    // GFile* gtk_file_dialog_get_initial_file (GtkFileDialog* self)
    public nint GetInitialFile()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxGetInitialFile(ObjPtr);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSGetInitialFile(ObjPtr);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsGetInitialFile(ObjPtr);
        }
        return GetInitialFile();
    }

    // GFile* gtk_file_dialog_get_initial_folder (GtkFileDialog* self)
    public nint GetInitialFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxGetInitialFolder(ObjPtr);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSGetInitialFolder(ObjPtr);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsGetInitialFolder(ObjPtr);
        }
        return GetInitialFolder();
    }

    // const char* gtk_file_dialog_get_initial_name (GtkFileDialog* self)
    public string GetInitialName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LinuxGetInitialName(ObjPtr);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOSGetInitialName(ObjPtr);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsGetInitialName(ObjPtr);
        }
        return GetInitialName();
    }

    // void gtk_file_dialog_set_title (GtkFileDialog* self, const char* title)
    public void SetTitle(string title)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxSetTitle(ObjPtr, title);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSSetTitle(ObjPtr, title);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsSetTitle(ObjPtr, title);
        }
    }

    // void gtk_file_dialog_set_filters (GtkFileDialog* self, GListModel* filters)
    public void SetFilters(Gio.ListModel filters)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxSetFilters(ObjPtr, filters.Handle);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSSetFilters(ObjPtr, filters.Handle);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsSetFilters(ObjPtr, filters.Handle);
        }
    }





    public string GetPath(nint path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LinuxGetPath(path);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOSGetPath(path);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsGetPath(path);
        }
        return path.ToString();
    }
}