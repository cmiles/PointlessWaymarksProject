using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.ImageList;

/// <summary>
///     Interaction logic for ImageListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class ImageListWindow
{
    private ImageListWindow(ImageListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public ImageListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Image List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<ImageListWindow> CreateInstance(ImageListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ImageListWindow(toLoad ?? await ImageListWithActionsContext.CreateInstance(null));

        return window;
    }
}