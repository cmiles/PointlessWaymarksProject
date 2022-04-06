using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsData.Spatial.Elevation;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportContext
{
    [ObservableProperty] private bool _autoSaveImports;
    [ObservableProperty] private RelayCommand _chooseAndLoadFileCommand;
    [ObservableProperty] private RelayCommand _clearAllForElevationReplacementCommand;
    [ObservableProperty] private RelayCommand _clearAllForImportCommand;
    [ObservableProperty] private string _importFileName;
    [ObservableProperty] private ObservableCollection<IGpxImportListItem> _items;
    [ObservableProperty] private ObservableCollection<IGpxImportListItem> _listSelection;
    [ObservableProperty] private RelayCommand<CoreWebView2WebMessageReceivedEventArgs> _mapMessageReceivedCommand;
    [ObservableProperty] private RelayCommand _markAllForElevationReplacementCommand;
    [ObservableProperty] private RelayCommand _markAllForImportCommand;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private string _previewMapJsonDto;
    [ObservableProperty] private RelayCommand<IGpxImportListItem> _requestMapCenterCommand;
    [ObservableProperty] private IGpxImportListItem _selectedItem;
    [ObservableProperty] private List<IGpxImportListItem> _selectedItems;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _tagsForAllImports;
    [ObservableProperty] private RelayCommand _toggleSelectedForElevationReplacementCommand;
    [ObservableProperty] private RelayCommand _toggleSelectedForImportCommand;


    public GpxImportContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        ChooseAndLoadFileCommand = StatusContext.RunBlockingTaskCommand(ChooseAndLoadFile);

        MarkAllForImportCommand = StatusContext.RunBlockingTaskCommand(MarkAllForImport);
        ClearAllForImportCommand = StatusContext.RunBlockingTaskCommand(ClearAllForImport);
        ToggleSelectedForImportCommand = StatusContext.RunBlockingTaskCommand(ToggleSelectedForImport);

        MarkAllForElevationReplacementCommand = StatusContext.RunBlockingTaskCommand(MarkAllForElevationReplacement);
        ClearAllForElevationReplacementCommand = StatusContext.RunBlockingTaskCommand(ClearAllForElevationReplacement);
        ToggleSelectedForElevationReplacementCommand =
            StatusContext.RunBlockingTaskCommand(ToggleSelectedForElevationReplacement);

        MapMessageReceivedCommand =
            StatusContext.RunNonBlockingTaskCommand<CoreWebView2WebMessageReceivedEventArgs>(MapMessageReceived);

        RequestMapCenterCommand = StatusContext.RunNonBlockingTaskCommand<IGpxImportListItem>(RequestMapCenter);

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletMapDocument("Map",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);
    }

    public async Task AutoSavePoint(GpxImportWaypoint toImport, List<CoordinateZ> elevationLookupCache)
    {
        var newPoint = new PointContent();
        newPoint.ContentId = Guid.NewGuid();
        newPoint.Title = string.IsNullOrWhiteSpace(toImport.Waypoint.Name)
            ? $"Waypoint {toImport.Waypoint.Latitude:F2}, {toImport.Waypoint.Longitude:F2}"
            : toImport.Waypoint.Name;
        newPoint.Slug = SlugUtility.Create(true);
        newPoint.Summary = string.IsNullOrWhiteSpace(toImport.Waypoint.Comment)
            ? string.IsNullOrWhiteSpace(toImport.Waypoint.Description)
                ? "Imported Waypoint"
                : toImport.Waypoint.Description
            : toImport.Waypoint.Comment;
        newPoint.BodyContent = string.IsNullOrWhiteSpace(toImport.Waypoint.Comment)
            ? string.Empty
            : toImport.Waypoint.Description;
        newPoint.BodyContentFormat = ContentFormatDefaults.Content.ToString();
        newPoint.CreatedOn = DateTime.Now;
        newPoint.CreatedBy = string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().DefaultCreatedBy)
            ? "GPX Importer"
            : UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;

        newPoint.Latitude = toImport.Waypoint.Latitude;
        newPoint.Longitude = toImport.Waypoint.Longitude;
        newPoint.Elevation = toImport.Waypoint.ElevationInMeters.MetersToFeet();

        //use elevation Lookup Cache
    }

    public async Task BuildMap()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var itemList = Items.ToList();

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!itemList.Any())
        {
            PreviewMapJsonDto = await SpatialHelpers.SerializeAsGeoJson(new MapComponentEditorContext.MapJsonDto(
                Guid.NewGuid(),
                new GeoJsonData.SpatialBounds(0, 0, 0, 0), new List<FeatureCollection>()));
            return;
        }

        var boundsKeeper = new List<Point>();

        var featureCollection = new FeatureCollection();

        foreach (var loopItem in itemList.Where(x => x is GpxImportTrack).Cast<GpxImportTrack>().ToList())
        {
            featureCollection.Add(new Feature(
                SpatialHelpers.Wgs84GeometryFactory().CreateLineString(loopItem.TrackInformation.Track.ToArray()),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopItem.TrackInformation.Name }, { "displayId", loopItem.DisplayId } })));

            var boundingBox = SpatialConverters.GeometryBoundingBox(loopItem.LineGeoJson);
            boundsKeeper.Add(new Point(boundingBox.MaxX,
                boundingBox.MaxY));
            boundsKeeper.Add(new Point(boundingBox.MinX, boundingBox.MinY));
        }

        foreach (var loopItem in itemList.Where(x => x is GpxImportRoute).Cast<GpxImportRoute>().ToList())
        {
            featureCollection.Add(new Feature(
                SpatialHelpers.Wgs84GeometryFactory().CreateLineString(loopItem.RouteInformation.Track.ToArray()),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopItem.RouteInformation.Name }, { "displayId", loopItem.DisplayId } })));

            var boundingBox = SpatialConverters.GeometryBoundingBox(loopItem.LineGeoJson);
            boundsKeeper.Add(new Point(boundingBox.MaxX,
                boundingBox.MaxY));
            boundsKeeper.Add(new Point(boundingBox.MinX, boundingBox.MinY));
        }


        foreach (var loopItem in itemList.Where(x => x is GpxImportWaypoint).Cast<GpxImportWaypoint>().ToList())
        {
            featureCollection.Add(new Feature(
                SpatialHelpers.Wgs84Point(loopItem.Waypoint.Longitude, loopItem.Waypoint.Latitude,
                    loopItem.Waypoint.ElevationInMeters ?? 0),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopItem.Waypoint.Name ?? string.Empty }, { "displayId", loopItem.DisplayId } })));
            boundsKeeper.Add(new Point(loopItem.Waypoint.Longitude, loopItem.Waypoint.Latitude));
        }

        var bounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        var dto = new MapComponentEditorContext.MapJsonDto(Guid.NewGuid(),
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX),
            new List<FeatureCollection> { featureCollection });

        //Using the new Guid as the page URL forces a changed value into the LineJsonDto
        PreviewMapJsonDto = await SpatialHelpers.SerializeAsGeoJson(dto);
    }

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

    public async Task ClearAllForElevationReplacement()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        foreach (var loopItems in Items) loopItems.ReplaceElevationOnImport = false;
    }

    public async Task ClearAllForImport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        foreach (var loopItems in Items) loopItems.MarkedForImport = false;
    }

    public async Task Import()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items.Any())
        {
            StatusContext.ToastError("Nothing to import?");
            return;
        }

        var importItems = Items.Where(x => x.MarkedForImport).ToList();

        if (importItems.Count > 10 && !AutoSaveImports)
        {
            await StatusContext.ShowMessageWithOkButton("Import Overload",
                $"You have selected {importItems.Count} items to import but don't have 'Auto-Save' selected - each item will open a new editor window and you are currently trying to open more than 10 editor windows at once. Either select fewer items or retry your import with 'Auto-Save' selected.");
            return;
        }

        if (AutoSaveImports && string.IsNullOrWhiteSpace(TagsForAllImports))
            await StatusContext.ShowMessageWithOkButton("Auto-Save without Tags",
                "Auto-Save will fill in many blank details but you must provide at least on Tag.");

        var elevationCache = new List<CoordinateZ>();

        if (importItems.Any(x => x.ReplaceElevationOnImport))
        {
            var elevationsToReplace = new List<CoordinateZ>();
            ;
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
                await ElevationService.OpenTopoNedElevation(elevationCache, StatusContext.ProgressTracker());
        }

        //loop imports
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
                var toAdd = new GpxImportWaypoint();
                await toAdd.Load(loopWaypoint);
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
                var toAdd = new GpxImportTrack();
                await toAdd.Load(loopTrack);
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
                var toAdd = new GpxImportRoute();
                await toAdd.Load(loopRoutes);
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
                    new List<string> { "Continue", "Cancel" }) == "Cancel")
                return;

        ImportFileName = fileInfo.FullName;

        StatusContext.Progress("Setting up list of import items...");

        await ThreadSwitcher.ResumeForegroundAsync();

        Items ??= new ObservableCollection<IGpxImportListItem>();
        Items.Clear();

        waypoints.ForEach(x => Items.Add(x));
        tracks.ForEach(x => Items.Add(x));
        routes.ForEach(x => Items.Add(x));

        await BuildMap();
    }

    private async Task MapMessageReceived(CoreWebView2WebMessageReceivedEventArgs mapMessage)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var rawMessage = mapMessage.WebMessageAsJson;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var parsedJson = JsonNode.Parse(rawMessage);

        if (parsedJson == null) return;

        if ((string)parsedJson["messageType"] != "featureClicked") return;

        Console.WriteLine("clicked");
    }

    public event EventHandler<string> MapRequest;

    public async Task MarkAllForElevationReplacement()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items?.Any() ?? true) return;

        foreach (var loopItems in Items) loopItems.ReplaceElevationOnImport = true;
    }

    public async Task MarkAllForImport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items?.Any() ?? true) return;

        foreach (var loopItems in Items) loopItems.MarkedForImport = true;
    }

    public async Task RequestMapCenter(IGpxImportListItem toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        dynamic foo = new ExpandoObject();
        foo.MessageType = "CenterFeatureRequest";
        foo.DisplayId = toCenter.DisplayId;

        await ThreadSwitcher.ResumeForegroundAsync();

        MapRequest?.Invoke(this, JsonSerializer.Serialize(foo));
    }

    public async Task ToggleSelectedForElevationReplacement()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        if (!SelectedItems?.Any() ?? true)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        ;

        foreach (var loopItems in SelectedItems)
            loopItems.ReplaceElevationOnImport = !loopItems.ReplaceElevationOnImport;
    }

    public async Task ToggleSelectedForImport()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems?.Any() ?? true) return;

        if (!SelectedItems?.Any() ?? true)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        ;

        foreach (var loopItems in SelectedItems) loopItems.MarkedForImport = !loopItems.MarkedForImport;
    }
}