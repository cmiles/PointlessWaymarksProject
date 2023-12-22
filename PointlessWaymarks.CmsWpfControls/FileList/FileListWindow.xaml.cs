using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.FileList;

/// <summary>
///     Interaction logic for FileListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class FileListWindow
{
    private FileListWindow(FileListWithActionsContext toLoad)
    {
        InitializeComponent();
        ListContext = toLoad;
        DataContext = this;
        WindowTitle = $"File List - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public FileListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<FileListWindow> CreateInstance(FileListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new FileListWindow(toLoad ?? await FileListWithActionsContext.CreateInstance(null, null));

        return window;
    }
}