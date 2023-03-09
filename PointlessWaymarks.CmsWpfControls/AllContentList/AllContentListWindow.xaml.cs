using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.AllContentList;

/// <summary>
///     Interaction logic for AllItemsWithActionsWindow.xaml
/// </summary>
[ObservableObject]
public partial class AllContentListWindow
{
    [ObservableProperty] private AllContentListWithActionsContext _listContext;
    [ObservableProperty] private string _windowTitle = "All Content List";

    private AllContentListWindow(AllContentListWithActionsContext toLoad)
    {
        InitializeComponent();

        _listContext = toLoad;

        DataContext = this;
    }

    public static async Task<AllContentListWindow> CreateInstance(AllContentListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new AllContentListWindow(toLoad ?? await AllContentListWithActionsContext.CreateInstance(null, windowStatus: null));
        return window;
    }
}