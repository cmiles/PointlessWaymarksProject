using System.Collections.ObjectModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SearchBuilderContext
{
    public SearchBuilderContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();
    }

    public required ObservableCollection<object> SearchFilters { get; set; }

    public StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task AddContentFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new ContentTypeSearchBuilder());
    }

    [NonBlockingCommand]
    public async Task AddFolderSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Folder" });
    }

    [NonBlockingCommand]
    public async Task AddIsoSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "ISO", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddSummarySearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Summary" });
    }

    [NonBlockingCommand]
    public async Task AddTagsSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Tags" });
    }

    [NonBlockingCommand]
    public async Task AddTitleSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Title" });
    }

    public static async Task<SearchBuilderContext> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new SearchBuilderContext(new StatusControlContext())
            { SearchFilters = new ObservableCollection<object>() };
    }
}