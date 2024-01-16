using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Data;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.ContentFolder;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using Serilog;
using ColumnSortControlContext = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlContext;
using ColumnSortControlSortItem = PointlessWaymarks.WpfCommon.ColumnSort.ColumnSortControlSortItem;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class GpxImportContext : IWebViewMessenger
{
    private GpxImportContext(StatusControlContext statusContext, ObservableCollection<IGpxImportListItem> items,
        ObservableCollection<IGpxImportListItem> listSelection, ContentFolderContext folderContext,
        TagsEditorContext tagsEditor)
    {
        StatusContext = statusContext;

        Items = items;
        ListSelection = listSelection;

        BuildCommands();

        FolderEntry = folderContext;
        TagEntry = tagsEditor;

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.AddRange(WpfCmsHtmlDocument.CmsLeafletMapHtmlAndJs("Map",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault));

        ToWebView.Enqueue(initialWebFilesMessage);

        ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

        ListSort = new ColumnSortControlContext
        {
            Items =
            [
                new ColumnSortControlSortItem
                {
                    DisplayName = "Name",
                    ColumnName = "UserContentName",
                    DefaultSortDirection = ListSortDirection.Ascending,
                    Order = 1
                },

                new ColumnSortControlSortItem
                {
                    DisplayName = "Created",
                    ColumnName = "CreatedOn",
                    DefaultSortDirection = ListSortDirection.Descending
                }
            ]
        };

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, Items));

        PropertyChanged += OnPropertyChanged;
    }

    public bool AutoSaveImports { get; set; }
    public ContentFolderContext FolderEntry { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public string ImportFileName { get; set; } = string.Empty;
    public ObservableCollection<IGpxImportListItem> Items { get; set; }
    public ObservableCollection<IGpxImportListItem> ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public IGpxImportListItem? SelectedItem { get; set; }
    public List<IGpxImportListItem>? SelectedItems { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext TagEntry { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    public string UserFilterText { get; set; } = string.Empty;

    public async Task BuildMap()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var itemList = Items.ToList();

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!itemList.Any())
        {
            ToWebView.Enqueue(JsonData.CreateRequest(await GeoJsonTools.SerializeWithGeoJsonSerializer(
                new MapJsonNewFeatureCollectionDto(
                    Guid.NewGuid(),
                    new SpatialBounds(0, 0, 0, 0), []))));
            return;
        }

        var boundsKeeper = new List<Point>();

        var featureCollection = new FeatureCollection();

        foreach (var loopItem in itemList.Where(x => x is GpxImportTrack).Cast<GpxImportTrack>().ToList())
        {
            featureCollection.Add(new Feature(
                // ReSharper disable once CoVariantArrayConversion
                GeoJsonTools.Wgs84GeometryFactory().CreateLineString(loopItem.TrackInformation.Track.ToArray()),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopItem.TrackInformation.Name }, { "displayId", loopItem.DisplayId } })));

            var boundingBox = GeoJsonTools.GeometryBoundingBox(loopItem.LineGeoJson);
            boundsKeeper.Add(new Point(boundingBox.MaxX,
                boundingBox.MaxY));
            boundsKeeper.Add(new Point(boundingBox.MinX, boundingBox.MinY));
        }

        foreach (var loopItem in itemList.Where(x => x is GpxImportRoute).Cast<GpxImportRoute>().ToList())
        {
            featureCollection.Add(new Feature(
                // ReSharper disable once CoVariantArrayConversion
                GeoJsonTools.Wgs84GeometryFactory().CreateLineString(loopItem.RouteInformation.Track.ToArray()),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopItem.RouteInformation.Name }, { "displayId", loopItem.DisplayId } })));

            var boundingBox = GeoJsonTools.GeometryBoundingBox(loopItem.LineGeoJson);
            boundsKeeper.Add(new Point(boundingBox.MaxX,
                boundingBox.MaxY));
            boundsKeeper.Add(new Point(boundingBox.MinX, boundingBox.MinY));
        }


        foreach (var loopItem in itemList.Where(x => x is GpxImportWaypoint).Cast<GpxImportWaypoint>().ToList())
        {
            featureCollection.Add(new Feature(
                PointTools.Wgs84Point(loopItem.Waypoint.Longitude, loopItem.Waypoint.Latitude,
                    loopItem.Waypoint.ElevationInMeters ?? 0),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopItem.Waypoint.Name ?? string.Empty }, { "displayId", loopItem.DisplayId } })));
            boundsKeeper.Add(new Point(loopItem.Waypoint.Longitude, loopItem.Waypoint.Latitude));
        }

        var bounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        var dto = new MapJsonNewFeatureCollectionDto(Guid.NewGuid(),
            new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX),
            [featureCollection]);

        //Using the new Guid as the page URL forces a changed value into the LineJsonDto
        ToWebView.Enqueue(JsonData.CreateRequest(await GeoJsonTools.SerializeWithGeoJsonSerializer(dto)));
    }

    [BlockingCommand]
    public async Task ChooseAndLoadFile()
    {
        StatusContext.Progress("Starting File Chooser");

        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Filter = "gpx files (*.gpx)|*.gpx|tcx files (*.tcx)|*.tcx|fit files (*.fit)|*.fit|All files (*.*)|*.*" };

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Checking that file exists");

        var possibleFile = new FileInfo(filePicker.FileName);

        if (!possibleFile.Exists)
        {
            StatusContext.ToastError("File doesn't exist?");
            return;
        }

        await LoadFile(possibleFile.FullName);
    }

    [BlockingCommand]
    public async Task ClearAllForElevationReplacement()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        foreach (var loopItems in Items) loopItems.ReplaceElevationOnImport = false;
    }

    [BlockingCommand]
    public async Task ClearAllForImport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        foreach (var loopItems in Items) loopItems.MarkedForImport = false;
    }

    public static async Task<GpxImportContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var factoryFolderEntry = await ContentFolderContext.CreateInstanceForAllGeoTypes(factoryContext);
        factoryFolderEntry.Title = "Folder for All Imports";

        var factoryTagEntry = await TagsEditorContext.CreateInstance(factoryContext, null);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newContext = new GpxImportContext(factoryContext, [],
            [], factoryFolderEntry, factoryTagEntry);

        return newContext;
    }

    private async Task FilterList()
    {
        if (!Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = _ => true;
            return;
        }

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (o is not IGpxImportListItem listItem) return false;

            return listItem.UserContentName.ToUpper().Contains(UserFilterText.ToUpper().Trim()) ||
                   listItem.UserSummary.Contains(UserFilterText.ToUpper().Trim());
        };
    }

    public async Task<(LineContent newLine, bool validationError, string validationErrorNote)> GpxImportRouteToLine(
        GpxImportRoute toImport, List<CoordinateZ> elevationLookupCache)
    {
        StatusContext.Progress($"Processing Route {toImport.UserContentName} into Line Content");

        //use elevation Lookup Cache
        if (toImport.ReplaceElevationOnImport)
        {
            StatusContext.Progress($"Replacing Elevations for Route {toImport.UserContentName}");

            foreach (var loopRoutePoint in toImport.RouteInformation.Track)
            {
                var newElevation =
                    elevationLookupCache.FirstOrDefault(x =>
                        x.Equals2D(loopRoutePoint));
                if (newElevation != null) loopRoutePoint.Z = newElevation.Z;
            }
        }

        var newLine =
            await LineGenerator.NewFromGpxTrack(toImport.RouteInformation, false, StatusContext.ProgressTracker());

        newLine.ContentId = Guid.NewGuid();
        newLine.Title = toImport.UserContentName;
        newLine.Slug = SlugTools.CreateSlug(true, toImport.UserContentName);
        newLine.Summary = string.IsNullOrWhiteSpace(toImport.Route.Comment)
            ? string.IsNullOrWhiteSpace(toImport.Route.Description)
                ? toImport.UserContentName
                : toImport.Route.Description
            : toImport.Route.Comment;
        newLine.BodyContent = string.IsNullOrWhiteSpace(toImport.Route.Comment)
            ? string.Empty
            : toImport.Route.Description;
        newLine.CreatedBy = string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().DefaultCreatedBy)
            ? "GPX Importer"
            : UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;
        newLine.Folder = FolderEntry.UserValue;
        newLine.Tags = TagEntry.TagListString();

        var validationResult = await LineGenerator.Validate(newLine);

        return (newLine, validationResult.HasError, validationResult.GenerationNote);
    }

    public async Task<(PointContentDto newPoint, bool validationError, string validationErrorNote)> GpxImportToPoint(
        GpxImportWaypoint toImport, List<CoordinateZ> elevationLookupCache)
    {
        StatusContext.Progress($"Processing Waypoint {toImport.UserContentName} into Point Content");

        var frozenNow = DateTime.Now;

        var newPoint = new PointContentDto
        {
            ContentId = Guid.NewGuid(),
            Title = toImport.UserContentName,
            Slug = SlugTools.CreateSlug(true, toImport.UserContentName),
            Summary = string.IsNullOrWhiteSpace(toImport.Waypoint.Comment)
                ? string.IsNullOrWhiteSpace(toImport.Waypoint.Description)
                    ? toImport.UserContentName
                    : toImport.Waypoint.Description
                : toImport.Waypoint.Comment,
            MapLabel = toImport.UserMapLabel,
            BodyContent = string.IsNullOrWhiteSpace(toImport.Waypoint.Comment)
                ? string.Empty
                : toImport.Waypoint.Description,
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CreatedOn = toImport.CreatedOn ?? frozenNow,
            CreatedBy = string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().DefaultCreatedBy)
                ? "GPX Importer"
                : UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            FeedOn = frozenNow,
            Latitude = toImport.Waypoint.Latitude,
            Longitude = toImport.Waypoint.Longitude,
            Elevation = toImport.Waypoint.ElevationInMeters.MetersToFeet(),
            Tags = TagEntry.TagListString(),
            Folder = FolderEntry.UserValue,
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

        //use elevation Lookup Cache
        if (toImport.ReplaceElevationOnImport)
        {
            StatusContext.Progress($"Checking for replacement Elevation for {toImport.UserContentName}");

            var newElevation =
                elevationLookupCache.FirstOrDefault(x =>
                    x.Equals2D(new Coordinate(newPoint.Longitude, newPoint.Latitude)));
            if (newElevation != null) newPoint.Elevation = newElevation.Z;
            else StatusContext.Progress($"Elevation NOT FOUND for replacement for {newPoint.Title}...");
        }

        var stateCounty =
            await StateCountyService.GetStateCounty(newPoint.Latitude, newPoint.Longitude);
        List<string> tagList = [stateCounty.state, stateCounty.county];

        try
        {
            tagList.AddRange(
                new Feature(new Point(newPoint.Longitude, newPoint.Latitude), new AttributesTable())
                    .IntersectionTags(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
                        CancellationToken.None));
        }
        catch (Exception e)
        {
            Log.Error(e, "Silent Error with FeatureIntersectionTags in Point GPX Import");
        }

        newPoint.Tags = Db.TagListJoin(tagList);

        var validationResult = await PointGenerator.Validate(newPoint);

        return (newPoint, validationResult.HasError, validationResult.GenerationNote);
    }

    public async Task<(LineContent newLine, bool validationError, string validationErrorNote)?> GpxImportTrackToLine(
        GpxImportTrack toImport, List<CoordinateZ> elevationLookupCache)
    {
        StatusContext.Progress($"Processing Track {toImport.UserContentName} into Line Content");

        //use elevation Lookup Cache
        if (toImport.ReplaceElevationOnImport)
        {
            StatusContext.Progress($"Replacing Elevations for Track {toImport.UserContentName}");

            foreach (var loopTrackPoint in toImport.TrackInformation.Track)
            {
                var newElevation =
                    elevationLookupCache.FirstOrDefault(x =>
                        x.Equals2D(loopTrackPoint));
                if (newElevation != null) loopTrackPoint.Z = newElevation.Z;
            }
        }

        var newLine =
            await LineGenerator.NewFromGpxTrack(toImport.TrackInformation, false, false, false,
                StatusContext.ProgressTracker());

        newLine.ContentId = Guid.NewGuid();
        newLine.Title = toImport.UserContentName;
        newLine.Slug = SlugTools.CreateSlug(true, toImport.UserContentName);
        newLine.Summary = string.IsNullOrWhiteSpace(toImport.Track.Comment)
            ? string.IsNullOrWhiteSpace(toImport.Track.Description)
                ? toImport.UserContentName
                : toImport.Track.Description
            : toImport.Track.Comment;
        newLine.BodyContent = string.IsNullOrWhiteSpace(toImport.Track.Comment)
            ? string.Empty
            : toImport.Track.Description;
        newLine.CreatedBy = string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().DefaultCreatedBy)
            ? "GPX Importer"
            : UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;
        newLine.Folder = FolderEntry.UserValue;
        newLine.Tags = TagEntry.TagListString();

        var validationResult = await LineGenerator.Validate(newLine);

        return (newLine, validationResult.HasError, validationResult.GenerationNote);
    }

    [BlockingCommand]
    public async Task Import()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items.Any())
        {
            StatusContext.ToastError("Nothing to import?");
            return;
        }

        var importItems = Items.Where(x => x.MarkedForImport).ToList();

        if (!importItems.Any())
        {
            StatusContext.ToastError("Nothing marked to Import?");
            return;
        }

        if (importItems.Count > 10 && !AutoSaveImports)
        {
            await StatusContext.ShowMessageWithOkButton("Import Overload",
                $"You have selected {importItems.Count} items to import but don't have 'Auto-Save' selected - each item will open a new editor window and you are currently trying to open more than 10 editor windows at once. Either select fewer items or retry your import with 'Auto-Save' selected.");
            return;
        }

        if (AutoSaveImports && await importItems.AnyAsyncLoop(async x =>
                !(await CommonContentValidation.ValidateTitle(x.UserContentName)).Valid))
        {
            await StatusContext.ShowMessageWithOkButton("Import Validation Error",
                "With Auto-Save selected all items for import must have a Name that isn't blank.");
            return;
        }

        if (AutoSaveImports && FolderEntry.HasValidationIssues)
        {
            await StatusContext.ShowMessageWithOkButton("Auto-Save Folder Validation",
                $"Folder Validation Problems... {FolderEntry.ValidationMessage}");
            return;
        }

        if (AutoSaveImports && TagEntry.HasValidationIssues)
        {
            await StatusContext.ShowMessageWithOkButton("Auto-Save Tag Validation",
                $"Tag Validation Problems... {TagEntry.TagsValidationMessage}");
            return;
        }

        var elevationCache = new List<CoordinateZ>();

        if (importItems.Any(x => x.ReplaceElevationOnImport))
        {
            var elevationsToReplace = new List<CoordinateZ>();

            var elevationItems = importItems.Where(x => x.ReplaceElevationOnImport).ToList();

            foreach (var loopItems in elevationItems)
                switch (loopItems)
                {
                    case GpxImportWaypoint waypoint:
                        elevationsToReplace.Add(new CoordinateZ(waypoint.Waypoint.Longitude, waypoint.Waypoint.Latitude,
                            waypoint.Waypoint.ElevationInMeters ?? 0));
                        break;
                    case GpxImportTrack track:
                        elevationsToReplace.AddRange(track.TrackInformation.Track);
                        break;
                    case GpxImportRoute route:
                        elevationsToReplace.AddRange(route.RouteInformation.Track);
                        break;
                    default:
                        Log.Warning(
                            "Unexpected case in GPX Import - when creating the elevation cache the type switch fell thru to the default with an unrecognized type?");
                        break;
                }

            elevationCache =
                await ElevationService.OpenTopoNedElevation(elevationsToReplace, StatusContext.ProgressTracker());
        }

        var pointReturns =
            new List<(PointContentDto point, GpxImportWaypoint listWaypoint, bool hasError, string validationNote)>();

        foreach (var gpxImportWaypoints in importItems.Where(x => x is GpxImportWaypoint).Cast<GpxImportWaypoint>()
                     .ToList())
        {
            var convertedPoint = await GpxImportToPoint(gpxImportWaypoints, elevationCache);
            pointReturns.Add((convertedPoint.newPoint, gpxImportWaypoints, convertedPoint.validationError,
                convertedPoint.validationErrorNote));
        }

        var trackReturns =
            new List<(LineContent line, GpxImportTrack listTrack, bool hasError, string validationNote)>();

        foreach (var gpxImportTrack in importItems.Where(x => x is GpxImportTrack).Cast<GpxImportTrack>()
                     .ToList())
        {
            var convertedPoint = await GpxImportTrackToLine(gpxImportTrack, elevationCache);

            if (convertedPoint == null) continue;

            trackReturns.Add((convertedPoint.Value.newLine, gpxImportTrack, convertedPoint.Value.validationError,
                convertedPoint.Value.validationErrorNote));
        }

        var routeReturns =
            new List<(LineContent line, GpxImportRoute listRoute, bool hasError, string validationNote)>();

        foreach (var gpxImportRoute in importItems.Where(x => x is GpxImportRoute).Cast<GpxImportRoute>()
                     .ToList())
        {
            var convertedPoint = await GpxImportRouteToLine(gpxImportRoute, elevationCache);
            routeReturns.Add((convertedPoint.newLine, gpxImportRoute, convertedPoint.validationError,
                convertedPoint.validationErrorNote));
        }

        if (AutoSaveImports)
        {
            var failedPointValidationReturns = pointReturns.Where(x => x.hasError).ToList();
            var failedTrackValidationReturns = trackReturns.Where(x => x.hasError).ToList();
            var failedRouteValidationReturns = routeReturns.Where(x => x.hasError).ToList();

            if (failedPointValidationReturns.Any() || failedTrackValidationReturns.Any() ||
                failedRouteValidationReturns.Any())
            {
                var errorBuilder = new StringBuilder();

                if (failedPointValidationReturns.Any())
                    errorBuilder.AppendLine(string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        failedPointValidationReturns.Select(x =>
                            $"Point: Title '{x.point.Title}', {x.point.Latitude:F2}, {x.point.Longitude:F2}:{Environment.NewLine}{x.validationNote}")));

                if (failedTrackValidationReturns.Any())
                    errorBuilder.AppendLine(string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        failedTrackValidationReturns.Select(x =>
                            $"Track: Title '{x.line.Title}', Distance: {x.line.LineDistance:F2} miles:{Environment.NewLine}{x.validationNote}")));

                if (failedRouteValidationReturns.Any())
                    errorBuilder.AppendLine(string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        failedTrackValidationReturns.Select(x =>
                            $"Route: Title '{x.line.Title}', Distance: {x.line.LineDistance:F2} miles:{Environment.NewLine}{x.validationNote}")));

                await StatusContext.ShowMessageWithOkButton("Import Auto-Save Failure",
                    $"There were validation problems - nothing was saved - errors: {Environment.NewLine}{errorBuilder}");

                return;
            }

            var importTime = DateTime.Now;

            foreach (var loopPoints in pointReturns)
            {
                var saveResult =
                    await PointGenerator.SaveAndGenerateHtml(loopPoints.point, importTime,
                        StatusContext.ProgressTracker());

                if (saveResult.generationReturn.HasError)
                {
                    var editorPoint = PointContent.CreateInstance();
                    editorPoint.InjectFrom(loopPoints.point);

                    var editor = await PointContentEditorWindow.CreateInstance(editorPoint);
                    await editor.PositionWindowAndShowOnUiThread();

#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Saving",
                        saveResult.generationReturn.GenerationNote);
#pragma warning restore 4014

                    await ThreadSwitcher.ResumeBackgroundAsync();
                }

                await RemoveFromList(loopPoints.listWaypoint.DisplayId);
            }

            foreach (var loopTrack in trackReturns)
            {
                var saveResult =
                    await LineGenerator.SaveAndGenerateHtml(loopTrack.line, importTime,
                        StatusContext.ProgressTracker());

                if (saveResult.generationReturn.HasError)
                {
                    var editorLine = LineContent.CreateInstance();
                    editorLine.InjectFrom(loopTrack.line);

                    await ThreadSwitcher.ResumeForegroundAsync();

                    var editor = await LineContentEditorWindow.CreateInstance(editorLine);
                    await editor.PositionWindowAndShowOnUiThread();

#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Saving",
                        saveResult.generationReturn.GenerationNote);
#pragma warning restore 4014

                    await ThreadSwitcher.ResumeBackgroundAsync();
                }

                await RemoveFromList(loopTrack.listTrack.DisplayId);
            }

            foreach (var loopRoute in routeReturns)
            {
                var saveResult =
                    await LineGenerator.SaveAndGenerateHtml(loopRoute.line, importTime,
                        StatusContext.ProgressTracker());

                if (saveResult.generationReturn.HasError)
                {
                    var editorLine = LineContent.CreateInstance();
                    editorLine.InjectFrom(loopRoute.line);

                    await ThreadSwitcher.ResumeForegroundAsync();

                    var editor = await LineContentEditorWindow.CreateInstance(editorLine);
                    await editor.PositionWindowAndShowOnUiThread();

#pragma warning disable 4014
                    //Allow execution to continue so Automation can continue
                    editor.StatusContext.ShowMessageWithOkButton("Problem Saving",
                        saveResult.generationReturn.GenerationNote);
#pragma warning restore 4014

                    await ThreadSwitcher.ResumeBackgroundAsync();
                }

                await RemoveFromList(loopRoute.listRoute.DisplayId);
            }
        }
        else
        {
            foreach (var loopPoints in pointReturns)
            {
                var editorPoint = PointContent.CreateInstance();
                editorPoint.InjectFrom(loopPoints.point);

                var editor = await PointContentEditorWindow.CreateInstance(editorPoint);
                await editor.PositionWindowAndShowOnUiThread();

                await RemoveFromList(loopPoints.listWaypoint.DisplayId);
            }

            foreach (var loopTrack in trackReturns)
            {
                var editorLine = LineContent.CreateInstance();
                editorLine.InjectFrom(loopTrack.line);

                var editor = await LineContentEditorWindow.CreateInstance(editorLine);
                await editor.PositionWindowAndShowOnUiThread();

                await RemoveFromList(loopTrack.listTrack.DisplayId);
            }

            foreach (var loopRoute in routeReturns)
            {
                var editorLine = LineContent.CreateInstance();
                editorLine.InjectFrom(loopRoute.line);

                var editor = await LineContentEditorWindow.CreateInstance(editorLine);
                await editor.PositionWindowAndShowOnUiThread();

                await RemoveFromList(loopRoute.listRoute.DisplayId);
            }
        }

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();
    }

    public async Task LoadFile(string fileName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var fileInfo = new FileInfo(fileName);

        if (!fileInfo.Exists)
        {
            StatusContext.ToastError("File does not exist?");
            return;
        }

        GpxFile gpxFile;
        try
        {
            StatusContext.Progress($"Parsing GPX File {fileInfo.FullName}...");
            gpxFile = GpxFile.Parse(await File.ReadAllTextAsync(fileInfo.FullName),
                new GpxReaderSettings
                {
                    BuildWebLinksForVeryLongUriValues = true, IgnoreBadDateTime = true,
                    IgnoreUnexpectedChildrenOfTopLevelElement = true, IgnoreVersionAttribute = true
                });
        }
        catch (Exception e)
        {
            await StatusContext.ShowMessageWithOkButton("GPX File Parse Error",
                $"Parsing {fileInfo.FullName} as a GPX file resulted in an error - {e.Message}");
            return;
        }

        var importWarnings = new List<string>();

        var waypoints = new List<GpxImportWaypoint>();
        foreach (var loopWaypoint in gpxFile.Waypoints)
            try
            {
                var toAdd = await GpxImportWaypoint.CreateInstance(loopWaypoint, StatusContext.ProgressTracker());
                waypoints.Add(toAdd);
            }
            catch (Exception e)
            {
                importWarnings.Add($"Waypoint Import Failure - {e.Message}");
                Log.ForContext("Help",
                        "This event is logged as an Error to give a maximum amount of information if there is a need to troubleshoot a GPX import - the failure but the error is not fatal to the program and has been handled.")
                    .Error(e, $"Caught Exception for GPX Import {fileInfo.FullName}");
            }

        StatusContext.Progress($"Found {waypoints.Count} Waypoints");

        var tracks = new List<GpxImportTrack>();
        foreach (var loopTrack in gpxFile.Tracks)
            try
            {
                var toAdd = await GpxImportTrack.CreateInstance(loopTrack, StatusContext.ProgressTracker());
                tracks.Add(toAdd);
            }
            catch (Exception e)
            {
                importWarnings.Add($"Track Import Failure - {e.Message}");
                Log.ForContext("Help",
                        "This event is logged as an Error to give a maximum amount of information if there is a need to troubleshoot a GPX import - the failure but the error is not fatal to the program and has been handled.")
                    .Error(e, $"Caught Exception for GPX Import {fileInfo.FullName}");
            }

        StatusContext.Progress($"Found {tracks.Count} Tracks");

        var routes = new List<GpxImportRoute>();
        foreach (var loopRoutes in gpxFile.Routes)
            try
            {
                var toAdd = await GpxImportRoute.CreateInstance(loopRoutes, StatusContext.ProgressTracker());
                routes.Add(toAdd);
            }
            catch (Exception e)
            {
                importWarnings.Add($"Route Import Failure - {e.Message}");
                Log.ForContext("Help",
                        "This event is logged as an Error to give a maximum amount of information if there is a need to troubleshoot a GPX import - the failure but the error is not fatal to the program and has been handled.")
                    .Error(e, $"Caught Exception for GPX Import {fileInfo.FullName}");
            }

        StatusContext.Progress($"Found {routes.Count} Routes");

        if (importWarnings.Any())
            if (await StatusContext.ShowMessage("GPX Import Warnings",
                    $"The GPX import of {fileInfo.FullName} generated the errors below - there were {waypoints.Count} Waypoints, {tracks.Count} Tracks and {routes.Count} Routes successfully imported. {Environment.NewLine}{Environment.NewLine}{string.Join($"{Environment.NewLine}", importWarnings)}",
                    ["Continue", "Cancel"]) == "Cancel")
                return;

        ImportFileName = fileInfo.FullName;

        StatusContext.Progress("Setting up list of import items...");

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        waypoints.ForEach(x => Items.Add(x));
        tracks.ForEach(x => Items.Add(x));
        routes.ForEach(x => Items.Add(x));

        await BuildMap();
    }

    private async Task MapMessageReceived(string json)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var parsedJson = JsonNode.Parse(json);

        if (parsedJson == null) return;

        var messageType = parsedJson["messageType"]?.ToString() ?? string.Empty;

        if (messageType != "featureClicked") return;

        Console.WriteLine("clicked");
    }

    public event EventHandler<string>? MapRequest;

    [BlockingCommand]
    public async Task MarkAllForElevationReplacement()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items.Any()) return;

        foreach (var loopItems in Items) loopItems.ReplaceElevationOnImport = true;
    }

    [BlockingCommand]
    public async Task MarkAllForImport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items.Any()) return;

        foreach (var loopItems in Items) loopItems.MarkedForImport = true;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        if (!string.IsNullOrWhiteSpace(args.Message))
            StatusContext.RunFireAndForgetBlockingTask(async () => await MapMessageReceived(args.Message));
        return Task.CompletedTask;
    }

    public async Task RemoveFromList(Guid displayId)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        await RemoveFromList(Items.Where(x => x.DisplayId == displayId).Select(x => x.DisplayId).ToList());
    }

    public async Task RemoveFromList(List<Guid> displayIds)
    {
        if (!displayIds.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var toRemove = Items.Where(x => displayIds.Contains(x.DisplayId)).ToList();

        toRemove.ForEach(x => Items.Remove(x));
    }

    [BlockingCommand]
    public async Task RemoveSelectedFromList()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenSelected = SelectedItems?.ToList() ?? [];

        if (!frozenSelected.Any())
        {
            StatusContext.ToastWarning("Nothing to Remove?");
            return;
        }

        await RemoveFromList(frozenSelected.Select(x => x.DisplayId).ToList());
    }

    [NonBlockingCommand]
    public async Task RequestMapCenter(IGpxImportListItem? toCenter)
    {
        if (toCenter == null) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var centerData = new MapJsonFeatureDto(toCenter.DisplayId, "CenterFeatureRequest");

        await ThreadSwitcher.ResumeForegroundAsync();

        MapRequest?.Invoke(this, JsonSerializer.Serialize(centerData));
    }

    [BlockingCommand]
    public async Task ToggleSelectedForElevationReplacement()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        if (!SelectedItems?.Any() ?? true)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        foreach (var loopItems in SelectedItems!)
            loopItems.ReplaceElevationOnImport = !loopItems.ReplaceElevationOnImport;
    }

    [BlockingCommand]
    public async Task ToggleSelectedForImport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        if (!SelectedItems?.Any() ?? true)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        foreach (var loopItems in SelectedItems!) loopItems.MarkedForImport = !loopItems.MarkedForImport;
    }
}