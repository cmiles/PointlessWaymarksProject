using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Fractions;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[GenerateStatusCommands]
[NotifyPropertyChanged]
public partial class ListFilterBuilderContext
{
    private readonly List<ListFilterBuilderFilterAdd> _generalBuilderItemsReference;
    private readonly List<ListFilterBuilderFilterAdd> _lineBuilderItemsReference;
    private readonly List<ListFilterBuilderFilterAdd> _photoBuilderItemsReference;
    private readonly List<ListFilterBuilderFilterAdd> _pointBuilderItemsReference;

    public ListFilterBuilderContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();

        _generalBuilderItemsReference =
        [
            new ListFilterBuilderFilterAdd
                { Description = "Type", AddFilterCommand = AddContentTypeFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
                { Description = "Title", AddFilterCommand = AddGeneralTitleSearchFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
                { Description = "Summary", AddFilterCommand = AddGeneralSummarySearchFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
                { Description = "Tags", AddFilterCommand = AddGeneralTagsSearchFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
                { Description = "Folder", AddFilterCommand = AddGeneralFolderSearchFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
                { Description = "Body", AddFilterCommand = AddGeneralBodySearchFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
                { Description = "Updates", AddFilterCommand = AddGeneralUpdateNotesFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
            {
                Description = "Created By", AddFilterCommand = AddGeneralCreatedBySearchFilterCommand, AppliesTo = []
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Created On", AddFilterCommand = AddGeneralCreatedOnSearchFilterCommand, AppliesTo = []
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Updated By", AddFilterCommand = AddGeneralLastUpdatedBySearchFilterCommand,
                AppliesTo = []
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Updated On", AddFilterCommand = AddGeneralLastUpdatedOnSearchFilterCommand,
                AppliesTo = []
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Main Site Feed", AddFilterCommand = AddGeneralShowInMainSiteFeedSearchFilterCommand,
                AppliesTo = []
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Bounds", AddFilterCommand = AddMultiBoundsFilterCommand,
                AppliesTo =
                [
                    Db.ContentTypeDisplayStringForLine, Db.ContentTypeDisplayStringForPhoto,
                    Db.ContentTypeDisplayStringForGeoJson, Db.ContentTypeDisplayStringForMap,
                    Db.ContentTypeDisplayStringForPoint
                ]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Elevation", AddFilterCommand = AddMultiTypeElevationFilterCommand,
                AppliesTo =
                [
                    Db.ContentTypeDisplayStringForLine, Db.ContentTypeDisplayStringForPhoto,
                    Db.ContentTypeDisplayStringForPoint
                ]
            },
            new ListFilterBuilderFilterAdd
                { Description = "Slug", AddFilterCommand = AddGeneralSlugFilterCommand, AppliesTo = [] },
            new ListFilterBuilderFilterAdd
            {
                Description = "File Name", AddFilterCommand = AddMultiTypeOriginalFileNameFilterCommand,
                AppliesTo =
                [
                    Db.ContentTypeDisplayStringForFile, Db.ContentTypeDisplayStringForImage,
                    Db.ContentTypeDisplayStringForPhoto, Db.ContentTypeDisplayStringForVideo
                ]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Public Download", AddFilterCommand = AddMultiPublicDownloadFilterCommand,
                AppliesTo =
                [
                    Db.ContentTypeDisplayStringForFile, Db.ContentTypeDisplayStringForGeoJson,
                    Db.ContentTypeDisplayStringForLine
                ]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "File - File Embed", AddFilterCommand = AddFileFileEmbedSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForFile]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Image - In Search", AddFilterCommand = AddImageShowInSearchSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForImage]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Picture - Shows Sizes", AddFilterCommand = AddMultiShowPictureSizesSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForImage, Db.ContentTypeDisplayStringForPhoto]
            }
        ];

        _photoBuilderItemsReference =
        [
            new ListFilterBuilderFilterAdd
            {
                Description = "Photo Created", AddFilterCommand = AddPhotoCreatedOnFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Shutter Speed", AddFilterCommand = AddPhotoShutterSpeedSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Focal Length", AddFilterCommand = AddPhotoFocalLengthSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "ISO", AddFilterCommand = AddPhotoIsoSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Camera", AddFilterCommand = AddPhotoCameraSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Lens", AddFilterCommand = AddPhotoLensSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "License", AddFilterCommand = AddPhotoLicenseSearchFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Show Position", AddFilterCommand = AddPhotoShowPicturePositionCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPhoto]
            }
        ];

        _lineBuilderItemsReference =
        [
            new ListFilterBuilderFilterAdd
            {
                Description = "Miles", AddFilterCommand = AddLineMilesFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Climb", AddFilterCommand = AddLineClimbFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Descent", AddFilterCommand = AddLineDescentFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Min Elevation", AddFilterCommand = AddLineMinElevationFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Max Elevation", AddFilterCommand = AddLineMaxElevationFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "In Activity Log", AddFilterCommand = AddLineIncludeInActivityLogFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Activity Type", AddFilterCommand = AddLineActivityTypeCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Map Content", AddFilterCommand = AddLineShowContentReferencesOnMapCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForLine]
            }
        ];

        _pointBuilderItemsReference =
        [
            new ListFilterBuilderFilterAdd
            {
                Description = "Icon", AddFilterCommand = AddPointMapIconFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPoint]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Marker Color", AddFilterCommand = AddPointMapMarkerColorFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPoint]
            },
            new ListFilterBuilderFilterAdd
            {
                Description = "Map Label", AddFilterCommand = AddPointMapLabelFilterCommand,
                AppliesTo = [Db.ContentTypeDisplayStringForPoint]
            }
        ];
    }

    public required ObservableCollection<ListFilterBuilderFilterAdd> GeneralBuilderItems { get; set; }
    public required ObservableCollection<ListFilterBuilderFilterAdd> LineBuilderItems { get; set; }
    public required ObservableCollection<ListFilterBuilderFilterAdd> PhotoBuilderItems { get; set; }
    public required ObservableCollection<ListFilterBuilderFilterAdd> PointBuilderItems { get; set; }
    public required ObservableCollection<object> SearchFilters { get; set; }
    public string SearchStringPreview { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task AddActivityLogFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder { FieldTitle = "Activity Log" });
    }

    [NonBlockingCommand]
    public async Task AddContentTypeFilter()
    {
        await AddSearchFilter(new ContentTypeListFilterBuilder());
    }

    [NonBlockingCommand]
    public async Task AddFileFileEmbedSearchFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder { FieldTitle = "File Embed" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralBodySearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Body" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralCreatedBySearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Created By" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralCreatedOnSearchFilter()
    {
        await AddSearchFilter(new DateTimeListFilterFieldBuilder { FieldTitle = "Created On" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralFolderSearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Folder" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralLastUpdatedBySearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Last Updated By" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralLastUpdatedOnSearchFilter()
    {
        await AddSearchFilter(new DateTimeListFilterFieldBuilder { FieldTitle = "Last Updated On" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralShowInMainSiteFeedSearchFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder { FieldTitle = "In Main Site Feed" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralSlugFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Slug" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralSummarySearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Summary" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralTagsSearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Tags" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralTitleSearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Title" });
    }

    [NonBlockingCommand]
    public async Task AddGeneralUpdateNotesFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Update Notes" });
    }

    [NonBlockingCommand]
    public async Task AddImageShowInSearchSearchFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder { FieldTitle = "Image In Search" });
    }

    [NonBlockingCommand]
    public async Task AddLineActivityType()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder
            { FieldTitle = "Activity Type" });
    }

    [NonBlockingCommand]
    public async Task AddLineClimbFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Climb", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineDescentFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Descent", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineIncludeInActivityLogFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder
            { FieldTitle = "In Activity Log" });
    }

    [NonBlockingCommand]
    public async Task AddLineMaxElevationFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Max Elevation", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMilesFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Miles", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineMinElevationFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Min Elevation", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddLineShowContentReferencesOnMap()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder
            { FieldTitle = "Map Content References" });
    }

    [NonBlockingCommand]
    public async Task AddMultiBoundsFilter()
    {
        await AddSearchFilter(new BoundsListFilterFieldBuilder(StatusContext)
            { FieldTitle = "Bounds" });
    }

    [NonBlockingCommand]
    public async Task AddMultiPublicDownloadFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder
            { FieldTitle = "Public Download" });
    }

    [NonBlockingCommand]
    public async Task AddMultiShowPictureSizesSearchFilter()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder { FieldTitle = "Show Picture Sizes" });
    }

    [NonBlockingCommand]
    public async Task AddMultiTypeElevationFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Elevation", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddMultiTypeOriginalFileNameFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Original File Name" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoCameraSearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Camera" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoCreatedOnFilter()
    {
        await AddSearchFilter(new DateTimeListFilterFieldBuilder { FieldTitle = "Photo Created On" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoFocalLengthSearchFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Focal Length", NumberConverterFunction = x => decimal.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoIsoSearchFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "ISO", NumberConverterFunction = x => int.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPhotoLensSearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Lens" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoLicenseSearchFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "License" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoShowPicturePosition()
    {
        await AddSearchFilter(new BooleanListFilterFieldBuilder { FieldTitle = "Photo Show Position" });
    }

    [NonBlockingCommand]
    public async Task AddPhotoShutterSpeedSearchFilter()
    {
        await AddSearchFilter(new NumericListFilterFieldBuilder
            { FieldTitle = "Shutter Speed", NumberConverterFunction = x => Fraction.TryParse(x, out _) });
    }

    [NonBlockingCommand]
    public async Task AddPointMapIconFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Map Icon" });
    }

    [NonBlockingCommand]
    public async Task AddPointMapLabelFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Map Label" });
    }

    [NonBlockingCommand]
    public async Task AddPointMapMarkerColorFilter()
    {
        await AddSearchFilter(new TextListFilterFieldBuilder { FieldTitle = "Map Marker Color" });
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

    public static async Task<ListFilterBuilderContext> CreateInstance(List<string> typesForSearch)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance();

        var newContext = new ListFilterBuilderContext(factoryContext)
        {
            GeneralBuilderItems = [],
            LineBuilderItems = [],
            PhotoBuilderItems = [],
            SearchFilters = [],
            PointBuilderItems = []
        };

        newContext.SearchFilters.CollectionChanged += newContext.SearchFiltersOnCollectionChanged;

        if (typesForSearch.Count == 0)
        {
            newContext._generalBuilderItemsReference.ForEach(x => newContext.GeneralBuilderItems.Add(x));
            newContext._lineBuilderItemsReference.ForEach(x => newContext.LineBuilderItems.Add(x));
            newContext._photoBuilderItemsReference.ForEach(x => newContext.PhotoBuilderItems.Add(x));
            newContext._pointBuilderItemsReference.ForEach(x => newContext.PointBuilderItems.Add(x));
        }
        else
        {
            newContext._generalBuilderItemsReference
                .Where(y => !y.AppliesTo.Any() || y.AppliesTo.Any(typesForSearch.Contains)).ToList()
                .ForEach(x => newContext.GeneralBuilderItems.Add(x));
            newContext._lineBuilderItemsReference
                .Where(y => !y.AppliesTo.Any() || y.AppliesTo.Any(typesForSearch.Contains)).ToList()
                .ForEach(x => newContext.LineBuilderItems.Add(x));
            newContext._photoBuilderItemsReference
                .Where(y => !y.AppliesTo.Any() || y.AppliesTo.Any(typesForSearch.Contains)).ToList()
                .ForEach(x => newContext.PhotoBuilderItems.Add(x));
            newContext._pointBuilderItemsReference
                .Where(y => !y.AppliesTo.Any() || y.AppliesTo.Any(typesForSearch.Contains)).ToList()
                .ForEach(x => newContext.PointBuilderItems.Add(x));
        }

        return newContext;
    }


    public string CreateSearch()
    {
        var searchString = new StringBuilder();
        var searchFilters = SearchFilters.ToList();

        foreach (var v in searchFilters)
            switch (v)
            {
                case TextListFilterFieldBuilder t:
                    searchString.AppendLine(
                        $"{(t.Not ? "!" : "")}{t.FieldTitle}: {t.SearchText}");
                    break;
                case BooleanListFilterFieldBuilder t:
                    searchString.AppendLine(
                        $"{t.FieldTitle}: {t.SearchBoolean}");
                    break;
                case NumericListFilterFieldBuilder t:
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
                case DateTimeListFilterFieldBuilder t:
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
                case BoundsListFilterFieldBuilder t:
                    if (t.AllConvert())
                    {
                        var searchBounds = t.CurrentBounds;
                        if (searchBounds is null) continue;

                        searchString.AppendLine(
                            $"{(t.Not ? "!" : "")}Bounds: {JsonSerializer.Serialize(searchBounds)}");
                    }

                    break;
                case ContentTypeListFilterBuilder t:
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