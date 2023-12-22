using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

/// <summary>
///     Interaction logic for PhotoListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class PhotoListWindow
{
    private PhotoListWindow(PhotoListWithActionsContext toLoad)
    {
        InitializeComponent();
        ListContext = toLoad;
        DataContext = this;
        WindowTitle = $"Photo List - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public PhotoListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoListWindow> CreateInstance(PhotoListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoListWindow(toLoad ?? await PhotoListWithActionsContext.CreateInstance(null, null, null));

        return window;
    }
}