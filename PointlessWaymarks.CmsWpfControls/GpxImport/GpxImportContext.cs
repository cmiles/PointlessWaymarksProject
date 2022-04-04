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
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportContext
{
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

        var waypoints = new List<GpxImportWaypoint>();
        foreach (var loopWaypoint in gpxFile.Waypoints)
        {
            var toAdd = new GpxImportWaypoint();
            await toAdd.Load(loopWaypoint);
            waypoints.Add(toAdd);
        }

        StatusContext.Progress($"Found {waypoints.Count} Waypoints");

        var tracks = new List<GpxImportTrack>();
        foreach (var loopTrack in gpxFile.Tracks)
        {
            var toAdd = new GpxImportTrack();
            await toAdd.Load(loopTrack);
            tracks.Add(toAdd);
        }

        StatusContext.Progress($"Found {tracks.Count} Tracks");

        var routes = new List<GpxImportRoute>();
        foreach (var loopRoutes in gpxFile.Routes)
        {
            var toAdd = new GpxImportRoute();
            await toAdd.Load(loopRoutes);
            routes.Add(toAdd);
        }

        StatusContext.Progress($"Found {routes.Count} Routes");

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