using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

/// <summary>
///     Interaction logic for MapComponentListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MapComponentListWindow
{
    private MapComponentListWindow(MapComponentListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public MapComponentListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Map List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
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