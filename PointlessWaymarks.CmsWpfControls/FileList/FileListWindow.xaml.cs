using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.FileList;

/// <summary>
///     Interaction logic for FileListWindow.xaml
/// </summary>
[ObservableObject]
public partial class FileListWindow
{
    [ObservableProperty] private FileListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Files List";

    private FileListWindow(FileListWithActionsContext toLoad)
    {
        InitializeComponent();

        _listContext = toLoad;

        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileListWindow> CreateInstance(FileListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileListWindow(toLoad ?? await FileListWithActionsContext.CreateInstance(null, null));

        return window;
    }
}