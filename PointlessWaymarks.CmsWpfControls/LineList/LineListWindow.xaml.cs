using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineList;

/// <summary>
///     Interaction logic for LineListWindow.xaml
/// </summary>
[ObservableObject]
public partial class LineListWindow
{
    [ObservableProperty] private LineListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Line List";

    private LineListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<LineListWindow> CreateInstance(LineListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new LineListWindow
        {
            ListContext = toLoad ?? new LineListWithActionsContext(null)
        };

        return window;
    }
}