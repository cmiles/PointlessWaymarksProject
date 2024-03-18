using System.Collections.ObjectModel;
using System.Text;
using Fractions;
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

    public string SearchString { get; set; } = string.Empty;

    public StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task AddContentTypeFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new ContentTypeSearchBuilder());
    }

    [NonBlockingCommand]
    public async Task AddGeneralBodySearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Body" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralFolderSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Folder" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralSummarySearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Summary" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralTagsSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Tags" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralTitleSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Title" });
    }

    [NonBlockingCommand]
    public async Task AddLineClimbFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Climb", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineDescentFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Descent", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMaxElevationFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Max Elevation", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMilesFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Miles", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMinElevationFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Min Elevation", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoFocalLengthSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Focal Length", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoIsoSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "ISO", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoLensSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "Lens" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoLicenseSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new TextSearchFieldBuilder { FieldTitle = "License" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoShutterSpeedSearchFilter()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Add(new NumericSearchFieldBuilder
            { FieldTitle = "Shutter Speed", NumberConverterFunction = x => Fraction.TryParse(x, out _) });
    }

    public static async Task<SearchBuilderContext> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new SearchBuilderContext(new StatusControlContext())
            { SearchFilters = new ObservableCollection<object>() };
    }

    [NonBlockingCommand]
    public Task CreateSearch()
    {
        var searchString = new StringBuilder();
        var searchFilters = SearchFilters.ToList();

        foreach (var v in searchFilters)
            switch (v)
            {
                case TextSearchFieldBuilder t:
                    searchString.AppendLine(
                        $"{(t.Not ? "!" : "")}{t.FieldTitle}: {t.SearchText}");
                    break;
                case NumericSearchFieldBuilder t:
                    if (t.UserNumberOneTextConverts && !string.IsNullOrWhiteSpace(t.UserNumberTextOne))
                    {
                        var numericSearchString =
                            $"{(t.Not ? "!" : "")}{t.FieldTitle}: {t.SelectedOperatorOne} {t.UserNumberTextOne}";
                        if (t.UserNumberTwoTextConverts && !string.IsNullOrWhiteSpace(t.UserNumberTextTwo))
                            numericSearchString += $" {t.SelectedOperatorTwo} {t.UserNumberTextTwo}";
                        if (!string.IsNullOrWhiteSpace(numericSearchString))
                            searchString.AppendLine(numericSearchString);
                    }

                    break;
                case ContentTypeSearchBuilder t:
                    if (t.ContentTypeChoices.Any(x => x.IsSelected))
                        searchString.AppendLine(
                            $"{(t.Not ? "!" : "")}Type: {string.Join(" ", t.ContentTypeChoices.Where(x => x.IsSelected).Select(x => x.TypeDescription))}");
                    break;
            }

        SearchString = searchString.ToString();
        return Task.CompletedTask;
    }
}