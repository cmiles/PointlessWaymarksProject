using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

/// <summary>
///     Interaction logic for MapComponentListWindow.xaml
/// </summary>
[ObservableObject]
public partial class MapComponentListWindow
{
    [ObservableProperty] private MapComponentListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Map List";

    private MapComponentListWindow(MapComponentListWithActionsContext toLoad)
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
    public static async Task<MapComponentListWindow> CreateInstance(MapComponentListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window =
            new MapComponentListWindow(toLoad ?? await MapComponentListWithActionsContext.CreateInstance(null));

        return window;
    }
}