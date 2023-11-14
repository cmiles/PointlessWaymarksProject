using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

/// <summary>
///     Interaction logic for VideoListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
#pragma warning disable MVVMTK0033
public partial class VideoListWindow
#pragma warning restore MVVMTK0033
{
    private VideoListWindow(VideoListWithActionsContext listContext)
    {
        InitializeComponent();

        ListContext = listContext;

        DataContext = this;
    }

    public VideoListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Videos List";

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