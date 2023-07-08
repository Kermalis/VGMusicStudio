//using System;
//using System.IO;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using Gtk.Internal;

//namespace Gtk;

//internal partial class AlertDialog : GObject.Object
//{
//    protected AlertDialog(IntPtr handle, bool ownedRef) : base(handle, ownedRef)
//    {
//    }

//    [DllImport("Gtk", EntryPoint = "gtk_alert_dialog_new")]
//    private static extern nint InternalNew(string format);

//    private static IntPtr ObjPtr;

//    internal static AlertDialog New(string format)
//    {
//        ObjPtr = InternalNew(format);
//        return new AlertDialog(ObjPtr, true);
//    }
//}

//internal partial class FileDialog : GObject.Object
//{
//    [DllImport("GObject", EntryPoint = "g_object_unref")]
//    private static extern void InternalUnref(nint obj);

//    [DllImport("Gio", EntryPoint = "g_task_return_value")]
//    private static extern void InternalReturnValue(nint task, nint result);

//    [DllImport("Gio", EntryPoint = "g_file_get_path")]
//    private static extern nint InternalGetPath(nint file);

//    [DllImport("Gtk", EntryPoint = "gtk_css_provider_load_from_data")]
//    private static extern void InternalLoadFromData(nint provider, string data, int length);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_new")]
//    private static extern nint InternalNew();

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_get_initial_file")]
//    private static extern nint InternalGetInitialFile(nint dialog);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_get_initial_folder")]
//    private static extern nint InternalGetInitialFolder(nint dialog);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_get_initial_name")]
//    private static extern string InternalGetInitialName(nint dialog);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_set_title")]
//    private static extern void InternalSetTitle(nint dialog, string title);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_set_filters")]
//    private static extern void InternalSetFilters(nint dialog, nint filters);

//    internal delegate void GAsyncReadyCallback(nint source, nint res, nint user_data);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_open")]
//    private static extern void InternalOpen(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_open_finish")]
//    private static extern nint InternalOpenFinish(nint dialog, nint result, nint error);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_save")]
//    private static extern void InternalSave(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_save_finish")]
//    private static extern nint InternalSaveFinish(nint dialog, nint result, nint error);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_select_folder")]
//    private static extern void InternalSelectFolder(nint dialog, nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data);

//    [DllImport("Gtk", EntryPoint = "gtk_file_dialog_select_folder_finish")]
//    private static extern nint InternalSelectFolderFinish(nint dialog, nint result, nint error);


//    private static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
//    private static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
//    private static bool IsFreeBSD() => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
//    private static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

//    private static IntPtr ObjPtr;

//    // Based on the code from the Nickvision Application template https://github.com/NickvisionApps/Application
//    // Code reference: https://github.com/NickvisionApps/Application/blob/28e3307b8242b2d335f8f65394a03afaf213363a/NickvisionApplication.GNOME/Program.cs#L50
//    private static void ImportNativeLibrary() => NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), LibraryImportResolver);

//    // Code reference: https://github.com/NickvisionApps/Application/blob/28e3307b8242b2d335f8f65394a03afaf213363a/NickvisionApplication.GNOME/Program.cs#L136
//    private static IntPtr LibraryImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
//    {
//        string fileName;
//        if (IsWindows())
//        {
//            fileName = libraryName switch
//            {
//                "GObject" => "libgobject-2.0-0.dll",
//                "Gio" => "libgio-2.0-0.dll",
//                "Gtk" => "libgtk-4-1.dll",
//                _ => libraryName
//            };
//        }
//        else if (IsMacOS())
//        {
//            fileName = libraryName switch
//            {
//                "GObject" => "libgobject-2.0.0.dylib",
//                "Gio" => "libgio-2.0.0.dylib",
//                "Gtk" => "libgtk-4.1.dylib",
//                _ => libraryName
//            };
//        }
//        else
//        {
//            fileName = libraryName switch
//            {
//                "GObject" => "libgobject-2.0.so.0",
//                "Gio" => "libgio-2.0.so.0",
//                "Gtk" => "libgtk-4.so.1",
//                _ => libraryName
//            };
//        }
//        return NativeLibrary.Load(fileName, assembly, searchPath);
//    }

//    private FileDialog(IntPtr handle, bool ownedRef) : base(handle, ownedRef)
//    {
//    }

//    // GtkFileDialog* gtk_file_dialog_new (void)
//    internal static FileDialog New()
//    {
//        ImportNativeLibrary();
//        ObjPtr = InternalNew();
//        return new FileDialog(ObjPtr, true);
//    }

//    // void gtk_file_dialog_open (GtkFileDialog* self, GtkWindow* parent, GCancellable* cancellable, GAsyncReadyCallback callback, gpointer user_data)
//    internal void Open(nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data) => InternalOpen(ObjPtr, parent, cancellable, callback, user_data);

//    // GFile* gtk_file_dialog_open_finish (GtkFileDialog* self, GAsyncResult* result, GError** error)
//    internal nint OpenFinish(nint result, nint error)
//    {
//        return InternalOpenFinish(ObjPtr, result, error);
//    }

//    // void gtk_file_dialog_save (GtkFileDialog* self, GtkWindow* parent, GCancellable* cancellable, GAsyncReadyCallback callback, gpointer user_data)
//    internal void Save(nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data) => InternalSave(ObjPtr, parent, cancellable, callback, user_data);

//    // GFile* gtk_file_dialog_save_finish (GtkFileDialog* self, GAsyncResult* result, GError** error)
//    internal nint SaveFinish(nint result, nint error)
//    {
//        return InternalSaveFinish(ObjPtr, result, error);
//    }

//    // void gtk_file_dialog_select_folder (GtkFileDialog* self, GtkWindow* parent, GCancellable* cancellable, GAsyncReadyCallback callback, gpointer user_data)
//    internal void SelectFolder(nint parent, nint cancellable, GAsyncReadyCallback callback, nint user_data) => InternalSelectFolder(ObjPtr, parent, cancellable, callback, user_data);

//    // GFile* gtk_file_dialog_select_folder_finish(GtkFileDialog* self, GAsyncResult* result, GError** error)
//    internal nint SelectFolderFinish(nint result, nint error)
//    {
//        return InternalSelectFolderFinish(ObjPtr, result, error);
//    }

//    // GFile* gtk_file_dialog_get_initial_file (GtkFileDialog* self)
//    internal nint GetInitialFile()
//    {
//        return InternalGetInitialFile(ObjPtr);
//    }

//    // GFile* gtk_file_dialog_get_initial_folder (GtkFileDialog* self)
//    internal nint GetInitialFolder()
//    {
//        return InternalGetInitialFolder(ObjPtr);
//    }

//    // const char* gtk_file_dialog_get_initial_name (GtkFileDialog* self)
//    internal string GetInitialName()
//    {
//        return InternalGetInitialName(ObjPtr);
//    }

//    // void gtk_file_dialog_set_title (GtkFileDialog* self, const char* title)
//    internal void SetTitle(string title) => InternalSetTitle(ObjPtr, title);

//    // void gtk_file_dialog_set_filters (GtkFileDialog* self, GListModel* filters)
//    internal void SetFilters(Gio.ListModel filters) => InternalSetFilters(ObjPtr, filters.Handle);





//    internal static nint GetPath(nint path)
//    {
//        return InternalGetPath(path);
//    }
//}