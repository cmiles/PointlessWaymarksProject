using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public SearchBuilderContext(StatusControlContext statusContext, ObservableCollection<object> searchFilters)
    {
        StatusContext = statusContext;
        SearchFilters = searchFilters;
        SearchFilters.CollectionChanged += SearchFiltersOnCollectionChanged;
        BuildCommands();
    }

    public ObservableCollection<object> SearchFilters { get; set; }

    public string SearchStringPreview { get; set; } = string.Empty;

    public StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task AddActivityLogFilter()
    {
        await AddSearchFilter(new BooleanSearchFieldBuilder { FieldTitle = "Activity Log" });
    }

    [NonBlockingCommand]
    public async Task AddContentTypeFilter()
    {
        await AddSearchFilter(new ContentTypeSearchBuilder());
    }

    [NonBlockingCommand]
    public async Task AddGeneralBodySearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Body" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralCreatedBySearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Created By" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralCreatedOnSearchFilter()
    {
        await AddSearchFilter(new DateTimeSearchFieldBuilder { FieldTitle = "Created On" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralFolderSearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Folder" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralLastUpdatedBySearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Last Updated By" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralLastUpdatedOnSearchFilter()
    {
        await AddSearchFilter(new DateTimeSearchFieldBuilder { FieldTitle = "Last Updated On" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralShowInMainSiteFeedSearchFilter()
    {
        await AddSearchFilter(new BooleanSearchFieldBuilder { FieldTitle = "Show In Main Site Feed" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralSlugFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Slug" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralSummarySearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Summary" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralTagsSearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Tags" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralTitleSearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Title" });
    }

    [NonBlockingCommand]
    public async Task AddLineClimbFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Climb", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineDescentFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Descent", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMaxElevationFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Max Elevation", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMilesFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Miles", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMinElevationFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Min Elevation", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddMultiTypeElevationFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Elevation", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddMultiTypeOriginalFileNameFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Original File Name" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoCameraSearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Camera" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoCreatedOnFilter()
    {
        await AddSearchFilter(new DateTimeSearchFieldBuilder { FieldTitle = "Photo Created On" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoFocalLengthSearchFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Focal Length", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoIsoSearchFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "ISO", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoLensSearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "Lens" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoLicenseSearchFilter()
    {
        await AddSearchFilter(new TextSearchFieldBuilder { FieldTitle = "License" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoShutterSpeedSearchFilter()
    {
        await AddSearchFilter(new NumericSearchFieldBuilder
            { FieldTitle = "Shutter Speed", NumberConverterFunction = x => Fraction.TryParse(x, out _) });
    }

    public async Task AddSearchFilter(INotifyPropertyChanged toAdd)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        toAdd.PropertyChanged += ToAdd_PropertyChanged;
        SearchFilters.Add(toAdd);
    }

    [NonBlockingCommand]
    public async Task ClearSearchFilters()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        SearchFilters.Clear();
    }

    public static async Task<SearchBuilderContext> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new SearchBuilderContext(new StatusControlContext(), new ObservableCollection<object>());
    }

    public string CreateSearch()
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
                case BooleanSearchFieldBuilder t:
                    searchString.AppendLine(
                        $"{t.FieldTitle}: {t.SearchBoolean}");
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
                case DateTimeSearchFieldBuilder t:
                    if (t.UserDateTimeOneTextConverts && !string.IsNullOrWhiteSpace(t.UserDateTimeTextOne))
                    {
                        var numericSearchString =
                            $"{(t.Not ? "!" : "")}{t.FieldTitle}: {t.SelectedOperatorOne} {t.UserDateTimeTextOne}";
                        if (t.UserDateTimeTwoTextConverts && !string.IsNullOrWhiteSpace(t.UserDateTimeTextTwo))
                            numericSearchString += $" {t.SelectedOperatorTwo} {t.UserDateTimeTextTwo}";
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

        return searchString.ToString();
    }

    [NonBlockingCommand]
    public async Task RemoveSearchFilter(object toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        if (toRemove is INotifyPropertyChanged remove)
            remove.PropertyChanged -= ToAdd_PropertyChanged;
        SearchFilters.Remove(toRemove);
    }

    private void SearchFiltersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SearchStringPreview = CreateSearch();
    }

    private void ToAdd_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SearchStringPreview = CreateSearch();
    }
}