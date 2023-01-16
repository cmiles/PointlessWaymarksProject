using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

/// <summary>
///     Interaction logic for VideoListWindow.xaml
/// </summary>
[ObservableObject]
public partial class VideoListWindow : Window
{
    [ObservableProperty] private VideoListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Videos List";

    private VideoListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<VideoListWindow> CreateInstance(VideoListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new VideoListWindow
        {
            ListContext = toLoad ?? new VideoListWithActionsContext(null)
        };

        return window;
    }
}