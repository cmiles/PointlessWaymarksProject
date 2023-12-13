using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ContentMapContext
{
    private ContentMapContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus,
        ContentListContext factoryListContext, bool loadInBackground = true)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);
        ListContext = factoryListContext;

        MapHtml = WpfCmsHtmlDocument.ToHtmlLeafletMapDocument("Map",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);

        BuildCommands();

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public Envelope? Bounds { get; set; }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public string MapHtml { get; set; }
    public string MapJsonDto { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public static async Task<ContentMapContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus, bool loadInBackground = true)
    {
        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new AllContentListLoader(100), windowStatus);

        return new ContentMapContext(factoryStatusContext, windowStatus, factoryListContext, loadInBackground);
    }

    public static async Task<ContentMapContext> CreateInstance(StatusControlContext? statusContext,
        IContentListLoader reportFilter, bool loadInBackground = true)
    {
        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext = await ContentListContext.CreateInstance(factoryStatusContext, reportFilter);

        return new ContentMapContext(factoryStatusContext, null, factoryListContext, loadInBackground);
    }

    public Envelope GetBounds(List<IContentListItem> toMeasure)
    {
        var boundsKeeper = new List<Point>();

        foreach (var loopElements in toMeasure)
            switch (loopElements)
            {
                case GeoJsonListListItem { DbEntry.GeoJson: not null } mapGeoJson:
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMaxLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMinLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMinLatitude));
                    break;
                case LineListListItem { DbEntry.Line: not null } mapLine:
                    var lineFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(mapLine.DbEntry.Line);
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMaxLongitude,
                        mapLine.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMinLongitude,
                        mapLine.DbEntry.InitialViewBoundsMinLatitude));
                    break;
            }

        if (toMeasure.Any(x => x is PointListListItem))
            foreach (var loopElements in toMeasure.Where(x => x is PointListListItem).Cast<PointListListItem>()
                         .ToList())
                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude));

        if (toMeasure.Any(x => x is PhotoListListItem))
            foreach (var loopElements in toMeasure.Where(x => x is PhotoListListItem).Cast<PhotoListListItem>()
                         .ToList())
            {
                if (loopElements.DbEntry.Latitude is null || loopElements.DbEntry.Longitude is null) continue;

                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude.Value, loopElements.DbEntry.Latitude.Value));
            }

        return SpatialConverters.PointBoundingBox(boundsKeeper);
    }

    private void ItemsViewOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StatusContext.RunNonBlockingTask(RefreshMap);
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext.ItemsView().CollectionChanged -= ItemsViewOnCollectionChanged;

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Code to Clipboard", ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand }
        };

        await ListContext.LoadData();

        await RefreshMap();

        ListContext.ItemsView().CollectionChanged += ItemsViewOnCollectionChanged;
    }

    public event EventHandler<string>? MapRequest;

    [BlockingCommand]
    public async Task RefreshMap()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = ListContext.FilteredListItems();

        if (frozenItems.Count == 0) frozenItems = ListContext.Items.ToList();

        if (frozenItems.Count < 1)
        {
            MapJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(new MapComponentEditorContext.MapJsonDto(
                Guid.NewGuid(),
                new GeoJsonData.SpatialBounds(0, 0, 0, 0), new List<FeatureCollection>()));
            return;
        }

        var geoJsonList = new List<FeatureCollection>();

        var boundsKeeper = new List<Point>();

        foreach (var loopElements in frozenItems)
            switch (loopElements)
            {
                case GeoJsonListListItem { DbEntry.GeoJson: not null } mapGeoJson:
                    geoJsonList.Add(GeoJsonTools.DeserializeStringToFeatureCollection(mapGeoJson.DbEntry.GeoJson));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMaxLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMinLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMinLatitude));
                    break;
                case LineListListItem { DbEntry.Line: not null } mapLine:
                    var lineFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(mapLine.DbEntry.Line);
                    geoJsonList.Add(lineFeatureCollection);
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMaxLongitude,
                        mapLine.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMinLongitude,
                        mapLine.DbEntry.InitialViewBoundsMinLatitude));
                    break;
            }

        if (frozenItems.Any(x => x is PointListListItem))
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in frozenItems.Where(x => x is PointListListItem).Cast<PointListListItem>()
                         .ToList())
            {
                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude,
                        loopElements.DbEntry.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopElements.DbEntry.Title ?? string.Empty } })));
                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude));
            }

            geoJsonList.Add(featureCollection);
        }

        if (frozenItems.Any(x => x is PhotoListListItem))
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in frozenItems.Where(x => x is PhotoListListItem).Cast<PhotoListListItem>()
                         .ToList())
            {
                if (loopElements.DbEntry.Latitude is null || loopElements.DbEntry.Longitude is null) continue;

                var description = string.Empty;

                if (!string.IsNullOrWhiteSpace(loopElements.SmallImageUrl))
                {
                    var tempImg = Path.Combine(FileLocationTools.TempStorageHtmlDirectory().FullName,
                        Path.GetFileName(loopElements.SmallImageUrl));
                    File.Copy(loopElements.SmallImageUrl, tempImg, true);
                    description = $"""
                                   <img src="https://localcms.pointlesswaymarks.com/{Path.GetFileName(loopElements.SmallImageUrl)}"/>
                                   """;
                }
                else
                {
                    description = $"""
                                    <p>{loopElements.DbEntry.Summary}</p>
                                   """;
                }

                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.DbEntry.Longitude.Value, loopElements.DbEntry.Latitude.Value,
                        loopElements.DbEntry.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        { "title", loopElements.DbEntry.Title ?? string.Empty },
                        { "description", description }
                    })));
                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude.Value, loopElements.DbEntry.Latitude.Value));
            }

            geoJsonList.Add(featureCollection);
        }

        Bounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        var dto = new MapComponentEditorContext.MapJsonDto(Guid.NewGuid(),
            new GeoJsonData.SpatialBounds(Bounds.MaxY, Bounds.MaxX, Bounds.MinY, Bounds.MinX), geoJsonList);

        MapJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(dto);
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnAllItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!ListContext.Items.Any())
        {
            StatusContext.ToastError("No Items?");
            return;
        }

        if (Bounds == null) return;

        await RequestMapCenterOnEnvelope(Bounds);
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnContent(IContentListItem toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        dynamic foo = new ExpandoObject();
        foo.MessageType = "CenterFeatureRequest";
        foo.DisplayId = toCenter.ContentId()!;

        await ThreadSwitcher.ResumeForegroundAsync();

        MapRequest?.Invoke(this, JsonSerializer.Serialize(foo));
    }

    public async Task RequestMapCenterOnEnvelope(Envelope toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toCenter is { Width: 0, Height: 0 })
        {
            await RequestMapCenterOnPoint(toCenter.MinX, toCenter.MinY);
            return;
        }

        dynamic foo = new ExpandoObject();
        foo.MessageType = "CenterBoundingBoxRequest";
        foo.BoundingBox = new[] { toCenter.MinX, toCenter.MinY, toCenter.MaxX, toCenter.MaxY };

        await ThreadSwitcher.ResumeForegroundAsync();

        var serializedData = JsonSerializer.Serialize(foo);

        MapRequest?.Invoke(this, serializedData);
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnFilteredItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var filteredItems = ListContext.FilteredListItems();

        if (!filteredItems.Any())
        {
            StatusContext.ToastError("No Visible Items?");
            return;
        }

        var bounds = GetBounds(ListContext.FilteredListItems());

        await RequestMapCenterOnEnvelope(bounds);
    }

    public async Task RequestMapCenterOnPoint(double longitude, double latitude)
    {
        dynamic foo = new ExpandoObject();
        foo.MessageType = "CenterCoordinateRequest";
        foo.Point = new[] { longitude, latitude };

        await ThreadSwitcher.ResumeForegroundAsync();

        var serializedData = JsonSerializer.Serialize(foo);

        MapRequest?.Invoke(this, serializedData);
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task RequestMapCenterOnSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var bounds = GetBounds(ListContext.SelectedListItems());

        await RequestMapCenterOnEnvelope(bounds);
    }

    public IContentListItem? SelectedListItem()
    {
        return ListContext.ListSelection.Selected;
    }

    public List<IContentListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems;
    }
}