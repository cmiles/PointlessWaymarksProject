using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;
using ColumnSortControlContext = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlContext;
using ColumnSortControlSortItem = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlSortItem;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class MapComponentEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation, IDropTarget, IWebViewMessenger
{
    public EventHandler? RequestContentEditorWindowClose;

    private MapComponentEditorContext(StatusControlContext statusContext, MapComponent dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        MapPreviewNavigationManager = MapCmsJson.LocalActionNavigation(StatusContext);

        this.SetupCmsLeafletMapHtmlAndJs("Map", UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault,
            UserSettingsSingleton.CurrentSettings().CalTopoApiKey, UserSettingsSingleton.CurrentSettings().BingApiKey);

        HelpContext = new HelpDisplayContext([CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock]);

        ListSort = SortContextMapElementsDefault();

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, MapElements));

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public Envelope? ContentBounds { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public List<MapElement> DbElements { get; set; } = [];
    public MapComponent DbEntry { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public HelpDisplayContext HelpContext { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public ObservableCollection<IMapElementListItem>? MapElements { get; set; }
    public Action<Uri, string> MapPreviewNavigationManager { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public StringDataEntryContext? SummaryEntry { get; set; }
    public StringDataEntryContext? TitleEntry { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    public string UserFilterText { get; set; } = string.Empty;
    public string UserGeoContentInput { get; set; } = string.Empty;

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

        var newGeoJsonItem = await MapElementListGeoJsonItem.CreateInstance(new GeoJsonContentActions(StatusContext));

        newGeoJsonItem.DbEntry = possibleGeoJson;
        newGeoJsonItem.SmallImageUrl = ContentListContext.GetSmallImageUrl(possibleGeoJson) ?? string.Empty;
        newGeoJsonItem.ShowType = true;
        newGeoJsonItem.InInitialView = loopContent?.IncludeInDefaultView ?? true;
        newGeoJsonItem.ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false;
        newGeoJsonItem.Title = possibleGeoJson.Title ?? string.Empty;

        MapElements.Add(newGeoJsonItem);

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

        var newLineItem = await MapElementListLineItem.CreateInstance(new LineContentActions(StatusContext));

        newLineItem.DbEntry = possibleLine;
        newLineItem.SmallImageUrl = ContentListContext.GetSmallImageUrl(possibleLine) ?? string.Empty;
        newLineItem.ShowType = true;
        newLineItem.InInitialView = loopContent?.IncludeInDefaultView ?? true;
        newLineItem.ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false;
        newLineItem.Title = possibleLine.Title ?? string.Empty;

        MapElements.Add(newLineItem);

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

        var newPointContent = await MapElementListPointItem.CreateInstance(new PointContentActions(StatusContext));

        newPointContent.DbEntry = possiblePoint.ToDbObject();
        newPointContent.SmallImageUrl = ContentListContext.GetSmallImageUrl(possiblePoint) ?? string.Empty;
        newPointContent.InInitialView = loopContent?.IncludeInDefaultView ?? true;
        newPointContent.ShowInitialDetails = loopContent?.ShowDetailsDefault ?? false;
        newPointContent.Title = possiblePoint.Title ?? string.Empty;

        MapElements.Add(newPointContent);

        if (guiNotificationAndMapRefreshWhenAdded)
        {
            StatusContext.ToastSuccess($"Added Point - {possiblePoint.Title}");
            await RefreshMapPreview();
        }
    }

    public static async Task<MapComponentEditorContext> CreateInstance(StatusControlContext? statusContext,
        MapComponent? mapComponent)
    {
        var newControl = new MapComponentEditorContext(statusContext ?? new StatusControlContext(),
            NewContentModels.InitializeMapComponent(mapComponent));
        await newControl.LoadData(mapComponent);
        return newControl;
    }

    public MapComponentDto CurrentStateToContent()
    {
        var newEntry = MapComponent.CreateInstance();

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay?.UpdatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
        }

        newEntry.Summary = SummaryEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.Title = TitleEntry?.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.CreatedBy = CreatedUpdatedDisplay?.CreatedByEntry.UserValue.TrimNullToEmpty() ?? string.Empty;
        newEntry.UpdateNotes = UpdateNotes?.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes?.UpdateNotesFormat.SelectedContentFormatAsString ?? string.Empty;

        var currentElementList = MapElements?.ToList() ?? [];
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

    public async Task Edit(PointContent? content)
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
            x is IMapElementListItem element &&
            element.Title.Contains(UserFilterText, StringComparison.CurrentCultureIgnoreCase);
    }

    [BlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
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

        DbEntry = NewContentModels.InitializeMapComponent(toLoad);

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
        TitleEntry.ValidationFunctions = [CommonContentValidation.ValidateTitle];

        SummaryEntry = StringDataEntryContext.CreateInstance();
        SummaryEntry.Title = "Summary";
        SummaryEntry.HelpText = "A short text entry that will show in Search and short references to the content";
        SummaryEntry.ReferenceValue = DbEntry.Summary ?? string.Empty;
        SummaryEntry.UserValue = StringTools.NullToEmptyTrim(DbEntry.Summary);
        SummaryEntry.ValidationFunctions = [CommonContentValidation.ValidateSummary];

        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);

        var db = await Db.Context();

        await ThreadSwitcher.ResumeForegroundAsync();

        MapElements ??= [];

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

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), MapElements);
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

    [BlockingCommand]
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

    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        return Task.CompletedTask;
    }

    [BlockingCommand]
    public async Task RefreshMapPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var mapInformation =
            await MapCmsJson.ProcessContentToMapInformation(MapElements?.Select(x => x.ContentId()!.Value).ToList() ??
                                                            new List<Guid>());

        if (mapInformation.fileCopyList.Any())
        {
            var fileBuilder = new FileBuilder();
            fileBuilder.Copy.AddRange(mapInformation.fileCopyList.Select(x => new FileBuilderCopy(x)));

            ToWebView.Enqueue(fileBuilder);
        }

        ContentBounds = mapInformation.bounds.ToEnvelope();

        ToWebView.Enqueue(new JsonData
        {
            Json = await MapCmsJson.NewMapFeatureCollectionDtoSerialized(
                mapInformation.featureList, mapInformation.bounds.ExpandToMinimumMeters(1000))
        });
    }

    [BlockingCommand]
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

    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(true);
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
            Items =
            [
                new ColumnSortControlSortItem
                {
                    DisplayName = "Title",
                    ColumnName = "Title",
                    Order = 1,
                    DefaultSortDirection = ListSortDirection.Ascending
                },

                new ColumnSortControlSortItem
                {
                    DisplayName = "Type",
                    ColumnName = "Type",
                    DefaultSortDirection = ListSortDirection.Ascending
                }
            ]
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

    [BlockingCommand]
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
}