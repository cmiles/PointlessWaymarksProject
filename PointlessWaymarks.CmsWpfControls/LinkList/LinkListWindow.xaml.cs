using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

/// <summary>
///     Interaction logic for LinkListWindow.xaml
/// </summary>
[ObservableObject]
public partial class LinkListWindow
{
    [ObservableProperty] private LinkListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "Link List";

    private LinkListWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Creates a new instance - this method can be called from any thread and will
    /// switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<LinkListWindow> CreateInstance(LinkListWithActionsContext toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new LinkListWindow
        {
            ListContext = toLoad ?? new LinkListWithActionsContext(null)
        };

        return window;
    }
}