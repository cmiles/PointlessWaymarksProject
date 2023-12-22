using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

/// <summary>
///     Interaction logic for GeoJsonListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class GeoJsonListWindow
{
    private GeoJsonListWindow(GeoJsonListWithActionsContext toLoad)
    {
        InitializeComponent();
        ListContext = toLoad;
        DataContext = this;
        WindowTitle = $"GeoJson List - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public GeoJsonListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; }


    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<GeoJsonListWindow> CreateInstance(GeoJsonListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new GeoJsonListWindow(toLoad ?? await GeoJsonListWithActionsContext.CreateInstance(null));

        return window;
    }
}