using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.PointList;

/// <summary>
///     Interaction logic for PointListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class PointListWindow
{
    private PointListWindow(PointListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public PointListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Point List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<PointListWindow> CreateInstance(PointListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PointListWindow(toLoad ?? await PointListWithActionsContext.CreateInstance(null));

        return window;
    }
}