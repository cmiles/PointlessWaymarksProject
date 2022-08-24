using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ImageList;

/// <summary>
///     Interaction logic for ImageListWindow.xaml
/// </summary>
[ObservableObject]
public partial class ImageListWindow
{
    [ObservableProperty] private ImageListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Image List";

    private ImageListWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<ImageListWindow> CreateInstance(ImageListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new ImageListWindow
        {
            ListContext = toLoad ?? new ImageListWithActionsContext(null)
        };

        return window;
    }
}