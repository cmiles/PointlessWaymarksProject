#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Point = NetTopologySuite.Geometries.Point;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public partial class MapComponentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation,
    IDropTarget
{
    [ObservableProperty] private ContentIdViewerControlContext? _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext? _createdUpdatedDisplay;
    [ObservableProperty] private List<MapElement> _dbElements = new();
    [ObservableProperty] private MapComponent? _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext _helpContext;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private ColumnSortControlContext _listSort;
    [ObservableProperty] private ObservableCollection<IMapElementListItem>? _mapElements;
    [ObservableProperty] private RelayCommand<IMapElementListItem> _openItemEditorCommand;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private string? _previewMapJsonDto = string.Empty;
    [ObservableProperty] private RelayCommand _refreshMapPreviewCommand;
    [ObservableProperty] private RelayCommand<IMapElementListItem> _removeItemCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private StringDataEntryContext? _summaryEntry;
    [ObservableProperty] private StringDataEntryContext? _titleEntry;
    [ObservableProperty] private UpdateNotesEditorContext? _updateNotes;
    [ObservableProperty] private string _userFilterText = string.Empty;
    [ObservableProperty] private RelayCommand _userGeoContentIdInputToMapCommand;
    [ObservableProperty] private string _userGeoContentInput = string.Empty;

    public EventHandler? RequestContentEditorWindowClose;

    private MapComponentEditorContext(StatusControlContext statusContext)
    {
        _statusContext = statusContext;

        PropertyChanged += OnPropertyChanged;

        _userGeoContentIdInputToMapCommand = StatusContext.RunBlockingTaskCommand(UserGeoContentIdInputToMap);
        _saveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
        _saveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
        _refreshMapPreviewCommand = StatusContext.RunBlockingTaskCommand(RefreshMapPreview);
        _removeItemCommand = StatusContext.RunBlockingTaskCommand<IMapElementListItem>(RemoveItem);
        _linkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);
        _openItemEditorCommand = StatusContext.RunBlockingTaskCommand<IMapElementListItem>(OpenItemEditor);

        _previewHtml = WpfHtmlDocument.ToHtmlLeafletMapDocument("Map",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);

        _helpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        _listSort = SortContextMapElementsDefault();

        _listSort.SortUpdated += (_, list) =>
            Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, MapElements); });
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public void DragOver(IDropInfo dropInfo)
    {
        var data = dropInfo.Data;
        if (data is string) dropInfo.Effects = DragDropEffects.Copy;
        if (data is not DataObject dataObject) return;
        if (dataObject.ContainsText()) dropInfo.Effects = DragDropEffects.Copy;
    }

    public async void Drop(IDropInfo dropInfo)
    {
        var textToProcess = string.Empty;

        var data = dropInfo.Data;
        textToProcess = data switch
        {
            string stringData => stringData,
            DataObject dataObject when dataObject.ContainsText() => dataObject.GetText(),
            _ => textToProcess
        };

        var contentIds = BracketCodeCommon.BracketCodeContentIds(textToProcess);
        foreach (var loopGuids in contentIds) await TryAddSpatialType(loopGuids);

        await RefreshMapPreview();
    }

    private async Task AddGeoJson(GeoJsonContent possibleGeoJson, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapElements == null) return;

        if (MapElements.Any(x => x.ContentId() == possibleGeoJson.ContentId))
        {
            StatusContext.ToastWarning($"GeoJson {possibleGeoJson.Title} is already on the map");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        MapElements.Add(new MapElementListGeoJsonItem
        {
            DbEntry = possibleGeoJson,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(possibleGeoJson) ?? string.Empty,
            InInitialView = loopContent?.IncludeInDefaultView ?? true,
            ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false,
            Title = possibleGeoJson.Title ?? string.Empty
        });

        if (guiNotificationAndMapRefreshWhenAdded)
        {
            StatusContext.ToastSuccess($"Added Point - {possibleGeoJson.Title}");
            await RefreshMapPreview();
        }
    }

    private async Task AddLine(LineContent possibleLine, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapElements == null) return;

        if (MapElements.Any(x => x.ContentId() == possibleLine.ContentId))
        {
            StatusContext.ToastWarning($"Line {possibleLine.Title} is already on the map");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        MapElements.Add(new MapElementListLineItem
        {
            DbEntry = possibleLine,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(possibleLine) ?? string.Empty,
            InInitialView = loopContent?.IncludeInDefaultView ?? true,
            ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false,
            Title = possibleLine.Title ?? string.Empty
        });

        if (guiNotificationAndMapRefreshWhenAdded)
        {
            StatusContext.ToastSuccess($"Added Point - {possibleLine.Title}");
            await RefreshMapPreview();
        }
    }

    private async Task AddPoint(PointContentDto possiblePoint, MapElement? loopContent = null,
        bool guiNotificationAndMapRefreshWhenAdded = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapElements == null) return;

        if (MapElements.Any(x => x.ContentId() == possiblePoint.ContentId))
        {
            StatusContext.ToastWarning($"Point {possiblePoint.Title} is already on the map");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        MapElements.Add(new MapElementListPointItem
        {
            DbEntry = possiblePoint,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(possiblePoint) ?? string.Empty,
            InInitialView = loopContent?.IncludeInDefaultView ?? true,
            ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false,
            Title = possiblePoint.Title ?? string.Empty
        });

        if (guiNotificationAndMapRefreshWhenAdded)
        {
            StatusContext.ToastSuccess($"Added Point - {possiblePoint.Title}");
            await RefreshMapPreview();
        }
    }

    public static async Task<MapComponentEditorContext> CreateInstance(StatusControlContext statusContext,
        MapComponent mapComponent)
    {
        var newControl = new MapComponentEditorContext(statusContext);
        await newControl.LoadData(mapComponent);
        return newControl;
    }

    public MapComponentDto CurrentStateToContent()
    {
        var newEntry = new MapComponent();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            newEntry.ContentId = Guid.NewGuid();
            newEntry.CreatedOn = DbEntry?.CreatedOn ?? DateTime.Now;
            if (newEntry.CreatedOn == DateTime.MinValue) newEntry.CreatedOn = DateTime.Now;
        }
        else
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay?.UpdatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
        }

        newEntry.Summary = SummaryEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.Title = TitleEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.CreatedBy = CreatedUpdatedDisplay?.CreatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.UpdateNotes = UpdateNotes?.UpdateNotes.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes?.UpdateNotesFormat.SelectedContentFormatAsString ?? string.Empty;

        var currentElementList = MapElements?.ToList() ?? new List<IMapElementListItem>();
        var finalElementList = currentElementList.Select(x => new MapElement
        {
            MapComponentContentId = newEntry.ContentId,
            ElementContentId = x.ContentId() ?? Guid.Empty,
            IsFeaturedElement = x.IsFeaturedElement,
            IncludeInDefaultView = x.InInitialView,
            ShowDetailsDefault = x.ShowInitialDetails
        }).ToList();

        return new MapComponentDto(newEntry, finalElementList);
    }

    public async Task Edit(PointContentDto? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task Edit(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task Edit(GeoJsonContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    private async Task FilterList()
    {
        if (MapElements == null || !MapElements.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(MapElements)).Filter = _ => true;
            return;
        }

        ((CollectionView)CollectionViewSource.GetDefaultView(MapElements)).Filter = x =>
        {
            var element = x as IMapElementListItem;
            if (element == null) return false;
            return element.Title.Contains(UserFilterText, StringComparison.CurrentCultureIgnoreCase);
        };
    }

    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeMapComponents.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(MapComponent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = toLoad ?? new MapComponent();

        if (DbEntry.Id > 0)
        {
            var context = await Db.Context();
            DbElements = await context.MapComponentElements.Where(x => x.MapComponentContentId == DbEntry.ContentId)
                .ToListAsync();
        }

        TitleEntry = StringDataEntryContext.CreateInstance();
        TitleEntry.Title = "Title";
        TitleEntry.HelpText = "Title Text";
        TitleEntry.ReferenceValue = DbEntry.Title.TrimNullToEmpty();
        TitleEntry.UserValue = DbEntry.Title.TrimNullToEmpty();
        TitleEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
            { CommonContentValidation.ValidateTitle };

        SummaryEntry = StringDataEntryContext.CreateInstance();
        SummaryEntry.Title = "Summary";
        SummaryEntry.HelpText = "A short text entry that will show in Search and short references to the content";
        SummaryEntry.ReferenceValue = DbEntry.Summary ?? string.Empty;
        SummaryEntry.UserValue = StringTools.NullToEmptyTrim(DbEntry.Summary);
        SummaryEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
            { CommonContentValidation.ValidateSummary };

        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);

        var db = await Db.Context();

        await ThreadSwitcher.ResumeForegroundAsync();

        MapElements ??= new ObservableCollection<IMapElementListItem>();

        MapElements.Clear();

        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopContent in DbElements)
        {
            var elementContent = await db.ContentFromContentId(loopContent.ElementContentId);

            switch (elementContent)
            {
                case GeoJsonContent gj:
                    await AddGeoJson(gj, loopContent);
                    break;
                case LineContent l:
                    await AddLine(l, loopContent);
                    break;
                case PointContentDto p:
                    await AddPoint(p, loopContent);
                    break;
            }
        }

        if (MapElements.Any()) await RefreshMapPreview();

        await ThreadSwitcher.ResumeForegroundAsync();

        ListContextSortHelpers.SortList(ListSort.SortDescriptions(), MapElements);
        await FilterList();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName.Equals(nameof(UserFilterText)))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    private async Task OpenItemEditor(IMapElementListItem? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toEdit == null)
        {
            StatusContext.ToastError("No Item to Edit?");
            return;
        }

        switch (toEdit)
        {
            case MapElementListPointItem p:
                await Edit(p.DbEntry);
                break;
            case MapElementListLineItem l:
                await Edit(l.DbEntry);
                break;
            case MapElementListGeoJsonItem g:
                await Edit(g.DbEntry);
                break;
        }
    }

    public async Task RefreshMapPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapElements == null || !MapElements.Any())
        {
            PreviewMapJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(new MapJsonDto(Guid.NewGuid(),
                new GeoJsonData.SpatialBounds(0, 0, 0, 0), new List<FeatureCollection>()));
            return;
        }

        var geoJsonList = new List<FeatureCollection>();

        var boundsKeeper = new List<Point>();

        foreach (var loopElements in MapElements)
            switch (loopElements)
            {
                case MapElementListGeoJsonItem { DbEntry.GeoJson: not null } mapGeoJson:
                    geoJsonList.Add(GeoJsonTools.DeserializeStringToFeatureCollection(mapGeoJson.DbEntry.GeoJson));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMaxLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMinLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMinLatitude));
                    break;
                case MapElementListLineItem { DbEntry.Line: not null } mapLine:
                    var lineFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(mapLine.DbEntry.Line);
                    geoJsonList.Add(lineFeatureCollection);
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMaxLongitude,
                        mapLine.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMinLongitude,
                        mapLine.DbEntry.InitialViewBoundsMinLatitude));
                    break;
            }

        if (MapElements.Any(x => x is MapElementListPointItem))
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in MapElements.Where(x => x is MapElementListPointItem)
                         .Cast<MapElementListPointItem>().Where(x => x.DbEntry != null).ToList())
            {
                if (loopElements.DbEntry == null) continue;

                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude,
                        loopElements.DbEntry.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopElements.DbEntry.Title ?? string.Empty } })));
                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude));
            }

            geoJsonList.Add(featureCollection);
        }

        var bounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        var dto = new MapJsonDto(Guid.NewGuid(),
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), geoJsonList);

        //Using the new Guid as the page URL forces a changed value into the LineJsonDto
        PreviewMapJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(dto);
    }

    private async Task RemoveItem(IMapElementListItem? toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (toRemove == null)
        {
            StatusContext.ToastError("No Item Selected?");
            return;
        }

        try
        {
            MapElements?.Remove(toRemove);
        }
        catch (Exception e)
        {
            StatusContext.ToastError($"Trouble Removing Map Element - {e.Message}");
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        await RefreshMapPreview();
    }

    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) =
            await MapComponentGenerator.SaveAndGenerateData(CurrentStateToContent(), null,
                StatusContext.ProgressTracker());

        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                generationReturn.GenerationNote);
            return;
        }

        await LoadData(newContent.Map);

        if (closeAfterSave)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }

    public static ColumnSortControlContext SortContextMapElementsDefault()
    {
        return new ColumnSortControlContext
        {
            Items = new List<ColumnSortControlSortItem>
            {
                new()
                {
                    DisplayName = "Title",
                    ColumnName = "Title",
                    Order = 1,
                    DefaultSortDirection = ListSortDirection.Ascending
                },
                new()
                {
                    DisplayName = "Type",
                    ColumnName = "Type",
                    DefaultSortDirection = ListSortDirection.Ascending
                }
            }
        };
    }

    /// <summary>
    ///     Will add an item to the map if it is a point, line or geojson type - this does NOT refresh the map
    /// </summary>
    /// <param name="toAdd"></param>
    /// <returns></returns>
    private async Task TryAddSpatialType(Guid toAdd)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (db.PointContents.Any(x => x.ContentId == toAdd))
        {
            await AddPoint((await Db.PointAndPointDetails(toAdd))!)
                .ConfigureAwait(false);
            return;
        }

        if (db.GeoJsonContents.Any(x => x.ContentId == toAdd))
        {
            await AddGeoJson(await db.GeoJsonContents.SingleAsync(x => x.ContentId == toAdd)).ConfigureAwait(false);
            return;
        }

        if (db.LineContents.Any(x => x.ContentId == toAdd))
        {
            await AddLine(await db.LineContents.SingleAsync(x => x.ContentId == toAdd)).ConfigureAwait(false);
            return;
        }

        StatusContext.ToastError("Item isn't a spatial type or isn't in the db?");
    }

    public async Task UserGeoContentIdInputToMap()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserGeoContentInput))
        {
            StatusContext.ToastError("Nothing to add?");
            return;
        }

        var codes = new List<Guid>();

        var regexObj = new Regex(@"(?:(\()|(\{))?\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b(?(1)\))(?(2)\})",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        var matchResult = regexObj.Match(UserGeoContentInput);
        while (matchResult.Success)
        {
            codes.Add(Guid.Parse(matchResult.Value));
            matchResult = matchResult.NextMatch();
        }

        codes.AddRange(BracketCodeCommon.BracketCodeContentIds(UserGeoContentInput));

        codes = codes.Distinct().ToList();

        if (!codes.Any())
        {
            StatusContext.ToastError("No Content Ids found?");
            return;
        }

        var db = await Db.Context();

        foreach (var loopCode in codes)
        {
            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possiblePoint != null)
            {
                var pointDto = (await Db.PointAndPointDetails(possiblePoint.ContentId, db))!;
                await AddPoint(pointDto);
                continue;
            }

            var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleLine != null)
            {
                await AddLine(possibleLine);
                continue;
            }

            var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == loopCode);
            if (possibleGeoJson != null)
            {
                await AddGeoJson(possibleGeoJson);
                continue;
            }

            StatusContext.ToastWarning(
                $"ContentId {loopCode} doesn't appear to be a valid point, line or GeoJson content for the map?");
        }

        UserGeoContentInput = string.Empty;

        await RefreshMapPreview();
    }

    public record MapJsonDto(Guid Identifier, GeoJsonData.SpatialBounds Bounds, List<FeatureCollection> GeoJsonLayers,
        string MessageType = "MapJsonDto");
}