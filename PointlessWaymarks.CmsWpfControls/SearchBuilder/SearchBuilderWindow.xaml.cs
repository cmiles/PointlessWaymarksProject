using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

/// <summary>
///     Interaction logic for SearchBuilderWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SearchBuilderWindow : Window
{
    public SearchBuilderWindow(SearchBuilderContext context)
    {
        SearchContext = context;
        StatusContext = context.StatusContext;

        BuildCommands();

        DataContext = this;
        InitializeComponent();
    }

    public SearchBuilderContext SearchContext { get; set; }
    public string SearchString { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public SearchBuilderWindowExitType WindowExitType { get; set; } = SearchBuilderWindowExitType.NotSet;

    [NonBlockingCommand]
    public async Task CancelSearch()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        WindowExitType = SearchBuilderWindowExitType.Cancel;
        DialogResult = false;
    }

    public static async Task<SearchBuilderWindow> CreateInstance(SearchBuilderContext searchContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new SearchBuilderWindow(searchContext);
    }

    [NonBlockingCommand]
    public async Task RunSearch()
    {
        SearchString = SearchContext.CreateSearch();

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