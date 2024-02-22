using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.MapIconList;

/// <summary>
///     Interaction logic for MapIconListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MapIconListWindow
{
    public MapIconListWindow(MapIconListContext listContext, StatusControlContext statusContext)
    {
        InitializeComponent();

        StatusContext = statusContext;
        ListContext = listContext;
        WindowTitle = $"Map Icon List - {UserSettingsSingleton.CurrentSettings().SiteName}";
        DataContext = this;
    }

    public MapIconListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<MapIconListWindow> CreateInstance()
    {
        var factoryStatus = new StatusControlContext();
        var factoryContext = await MapIconListContext.CreateInstance(factoryStatus);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new MapIconListWindow(factoryContext, factoryStatus);

        factoryStatus.RunFireAndForgetBlockingTask(factoryContext.LoadData);

        return window;
    }
}