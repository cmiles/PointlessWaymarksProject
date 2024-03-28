using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

/// <summary>
///     Interaction logic for ListFilterBuilderWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ListFilterBuilderWindow
{
    public ListFilterBuilderWindow(ListFilterBuilderContext context)
    {
        ListFilterContext = context;
        StatusContext = context.StatusContext;

        BuildCommands();

        DataContext = this;
        InitializeComponent();
    }

    public ListFilterBuilderContext ListFilterContext { get; set; }
    public string SearchString { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; set; }
    public SearchBuilderWindowExitType WindowExitType { get; set; } = SearchBuilderWindowExitType.NotSet;

    [NonBlockingCommand]
    public async Task CancelSearch()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        WindowExitType = SearchBuilderWindowExitType.Cancel;
        DialogResult = false;
    }

    public static async Task<ListFilterBuilderWindow> CreateInstance(ListFilterBuilderContext listFilterContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new ListFilterBuilderWindow(listFilterContext);
    }

    [NonBlockingCommand]
    public async Task RunSearch()
    {
        SearchString = ListFilterContext.CreateSearch();

        await ThreadSwitcher.ResumeForegroundAsync();

        WindowExitType = SearchBuilderWindowExitType.RunSearch;
        DialogResult = true;
    }
}

public enum SearchBuilderWindowExitType
{
    NotSet,
    Cancel,
    RunSearch
}