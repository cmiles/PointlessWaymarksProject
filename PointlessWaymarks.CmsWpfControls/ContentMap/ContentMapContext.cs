using System.Collections.Specialized;
using System.Dynamic;
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

                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.DbEntry.Longitude.Value, loopElements.DbEntry.Latitude.Value,
                        loopElements.DbEntry.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopElements.DbEntry.Title ?? string.Empty } })));
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
    public async Task RequestMapCenterOnContent(IContentListItem toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        dynamic foo = new ExpandoObject();
        foo.MessageType = "CenterFeatureRequest";
        foo.DisplayId = toCenter.ContentId()!;

        await ThreadSwitcher.ResumeForegroundAsync();

        MapRequest?.Invoke(this, JsonSerializer.Serialize(foo));
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnCurrentBounds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (Bounds == null) return;

        dynamic foo = new ExpandoObject();
        foo.MessageType = "CenterBoundingBoxRequest";
        foo.BoundingBox = new[] { Bounds.MinX, Bounds.MinY, Bounds.MaxX, Bounds.MaxY };

        await ThreadSwitcher.ResumeForegroundAsync();

        var serializedData = JsonSerializer.Serialize(foo);

        MapRequest?.Invoke(this, serializedData);
    }
}