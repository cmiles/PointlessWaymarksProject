using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.TrailList;

/// <summary>
///     Interaction logic for TrailListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class TrailListWindow
{
    private TrailListWindow(TrailListWithActionsContext toLoad)
    {
        InitializeComponent();
        ListContext = toLoad;
        DataContext = this;
        WindowTitle = $"Trail List - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public TrailListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<TrailListWindow> CreateInstance(TrailListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new TrailListWindow(toLoad ?? await TrailListWithActionsContext.CreateInstance(null));

        return window;
    }
}