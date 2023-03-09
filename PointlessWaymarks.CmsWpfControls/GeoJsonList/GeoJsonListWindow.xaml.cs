using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

/// <summary>
///     Interaction logic for GeoJsonListWindow.xaml
/// </summary>
[ObservableObject]
public partial class GeoJsonListWindow
{
    [ObservableProperty] private GeoJsonListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "GeoJson List";

    private GeoJsonListWindow(GeoJsonListWithActionsContext toLoad)
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
    public static async Task<GeoJsonListWindow> CreateInstance(GeoJsonListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GeoJsonListWindow(toLoad ?? await GeoJsonListWithActionsContext.CreateInstance(null));

        return window;
    }
}