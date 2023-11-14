using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.AllContentList;

/// <summary>
///     Interaction logic for AllItemsWithActionsWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class AllContentListWindow
{
    private AllContentListWindow(AllContentListWithActionsContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public AllContentListWithActionsContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "All Content List";

    public static async Task<AllContentListWindow> CreateInstance(AllContentListWithActionsContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window =
            new AllContentListWindow(toLoad ?? await AllContentListWithActionsContext.CreateInstance(null, null));
        return window;
    }
}