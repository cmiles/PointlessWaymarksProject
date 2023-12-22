using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

/// <summary>
///     Interaction logic for VideoListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class VideoListWindow
{
    private VideoListWindow(VideoListWithActionsContext listContext)
    {
        InitializeComponent();
        ListContext = listContext;
        DataContext = this;
        WindowTitle = $"Video List - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public VideoListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoListWindow> CreateInstance(VideoListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new VideoListWindow(toLoad ?? await VideoListWithActionsContext.CreateInstance(null));

        return window;
    }
}