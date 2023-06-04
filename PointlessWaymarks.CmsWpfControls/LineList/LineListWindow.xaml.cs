using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineList;

/// <summary>
///     Interaction logic for LineListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class LineListWindow
{
    private LineListWindow(LineListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public LineListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Line List";

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<LineListWindow> CreateInstance(LineListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LineListWindow(toLoad ?? await LineListWithActionsContext.CreateInstance(null));

        return window;
    }
}