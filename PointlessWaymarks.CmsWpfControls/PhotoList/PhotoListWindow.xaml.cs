using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

/// <summary>
///     Interaction logic for PhotoListWindow.xaml
/// </summary>
[ObservableObject]
public partial class PhotoListWindow
{
    [ObservableProperty] private PhotoListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Photo List";

    private PhotoListWindow(PhotoListWithActionsContext listContext)
    {
        InitializeComponent();

        _listContext = listContext;

        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<PhotoListWindow> CreateInstance(PhotoListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PhotoListWindow(toLoad ?? await PhotoListWithActionsContext.CreateInstance(null, null, null));

        return window;
    }
}