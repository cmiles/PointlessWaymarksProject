using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointList;

/// <summary>
///     Interaction logic for PointListWindow.xaml
/// </summary>
[ObservableObject]
public partial class PointListWindow
{
    [ObservableProperty] private PointListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Point List";

    private PointListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<PointListWindow> CreateInstance(PointListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new PointListWindow
        {
            ListContext = toLoad ?? new PointListWithActionsContext(null)
        };

        return window;
    }
}