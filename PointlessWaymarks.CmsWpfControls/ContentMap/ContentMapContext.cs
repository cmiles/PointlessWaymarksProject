using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocumentFormat.OpenXml.Spreadsheet;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ContentMapContext : IWebViewMessenger
{
    private ContentMapContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus,
        ContentListContext factoryListContext, bool loadInBackground = true)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        MapHtml = WpfCmsHtmlDocument.ToHtmlLeafletMapDocument("Map",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);

        JsonToWebView = new WorkQueue<WebViewMessage>(true);

        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);
        ListContext = factoryListContext;

        BuildCommands();

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public Envelope? ContentBounds { get; set; }
    public WorkQueue<WebViewMessage> JsonToWebView { get; set; }
    public ContentListContext ListContext { get; set; }
    public SpatialBounds? MapBounds { get; set; } = null;
    public string MapHtml { get; set; }

    public bool RefreshMapOnCollectionChanged { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public void JsonFromWebView(object? o, WebViewMessage args)
    {
        if (!string.IsNullOrWhiteSpace(args.Message))
            StatusContext.RunFireAndForgetBlockingTask(async () => await MapMessageReceived(args.Message));
    }

    public static async Task<ContentMapContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new AllContentListLoader(100), windowStatus);

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new ContentMapContext(factoryStatusContext, windowStatus, factoryListContext, loadInBackground);
        toReturn.ListContext.ItemsView().CollectionChanged += toReturn.ItemsViewOnCollectionChanged;

        return toReturn;
    }

    public static async Task<ContentMapContext> CreateInstance(StatusControlContext? statusContext,
        IContentListLoader reportFilter, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext = await ContentListContext.CreateInstance(factoryStatusContext, reportFilter);

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new ContentMapContext(factoryStatusContext, null, factoryListContext, loadInBackground);
        toReturn.ListContext.ItemsView().CollectionChanged += toReturn.ItemsViewOnCollectionChanged;

        return toReturn;
    }

    private void ItemsViewOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (RefreshMapOnCollectionChanged) StatusContext.RunNonBlockingTask(async () => await RefreshMap(MapBounds));
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        RefreshMapOnCollectionChanged = false;

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Center Map", ItemCommand = RequestMapCenterOnSelectedItemsCommand },
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Code to Clipboard", ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand }
        };

        await ListContext.LoadData(true);

        RefreshMapOnCollectionChanged = true;

        await RefreshMap();
    }

    private async Task MapMessageReceived(string mapMessage)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var parsedJson = JsonNode.Parse(mapMessage);

        if (parsedJson == null) return;

        var messageType = parsedJson["messageType"]?.ToString() ?? string.Empty;

        if (messageType == "mapBoundsChange")
        {
            MapBounds = new SpatialBounds(parsedJson["bounds"]["_northEast"]["lat"].GetValue<double>(),
                parsedJson["bounds"]["_northEast"]["lng"].GetValue<double>(),
                parsedJson["bounds"]["_southWest"]["lat"].GetValue<double>(),
                parsedJson["bounds"]["_southWest"]["lng"].GetValue<double>());
            return;
        }
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task PopupsForSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var bounds = SelectedListItems().Select(x => x.ContentId()).Where(x => x is not null).Select(x => x!.Value)
            .ToList();

        var popupData = new MapJsonFeatureListDto(bounds, "ShowPopupsFor");

        await ThreadSwitcher.ResumeForegroundAsync();

        var serializedData = JsonSerializer.Serialize(popupData);

        JsonToWebView.Enqueue(new WebViewMessage(serializedData));
    }

    [BlockingCommand]
    public async Task RefreshMap(SpatialBounds? bounds = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = await ListContext.FilteredListItems();

        if (frozenItems.Count == 0) frozenItems = ListContext.Items.ToList();

        if (frozenItems.Count < 1)
        {
            JsonToWebView.Enqueue(new WebViewMessage(await GeoJsonTools.SerializeWithGeoJsonSerializer(
                new MapJsonNewFeatureCollectionDto(
                    Guid.NewGuid(),
                    new SpatialBounds(0, 0, 0, 0), new List<FeatureCollection>()))));
            return;
        }

        var mapInformation = MapJson.ProcessContentToMapInformation(frozenItems);

        ContentBounds = mapInformation.bounds.ToEnvelope();

        JsonToWebView.Enqueue(new WebViewMessage(await MapJson.NewMapFeatureCollectionDtoSerialized(
            mapInformation.featureList, bounds ??
            mapInformation.bounds.ExpandToMinimumMeters(1000))));
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

        if (ContentBounds == null) return;

        await RequestMapCenterOnEnvelope(ContentBounds);
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnContent(IContentListItem toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var centerData = new MapJsonFeatureDto(toCenter.ContentId()!.Value, "CenterFeatureRequest");

        await ThreadSwitcher.ResumeForegroundAsync();

        JsonToWebView.Enqueue(new WebViewMessage(JsonSerializer.Serialize(centerData)));
    }

    public async Task RequestMapCenterOnEnvelope(Envelope toCenter)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toCenter is { Width: 0, Height: 0 })
        {
            await RequestMapCenterOnPoint(toCenter.MinX, toCenter.MinY);
            return;
        }

        var centerData = new MapJsonBoundsDto(
            new SpatialBounds(toCenter.MaxY, toCenter.MaxX, toCenter.MinY, toCenter.MinX).ExpandToMinimumMeters(1000),
            "CenterBoundingBoxRequest");

        await ThreadSwitcher.ResumeForegroundAsync();

        var serializedData = JsonSerializer.Serialize(centerData);

        JsonToWebView.Enqueue(new WebViewMessage(serializedData));
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnFilteredItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var filteredItems = await ListContext.FilteredListItems();

        if (!filteredItems.Any())
        {
            StatusContext.ToastError("No Visible Items?");
            return;
        }

        var bounds = MapJson.GetBounds(filteredItems);

        await RequestMapCenterOnEnvelope(bounds);
    }

    public async Task RequestMapCenterOnPoint(double longitude, double latitude)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var centerData = new MapJsonCoordinateDto(latitude, longitude, "CenterCoordinateRequest");

        var serializedData = JsonSerializer.Serialize(centerData);

        JsonToWebView.Enqueue(new WebViewMessage(serializedData));
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task RequestMapCenterOnSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var bounds = MapJson.GetBounds(ListContext.SelectedListItems());

        await RequestMapCenterOnEnvelope(bounds);
    }

    [NonBlockingCommand]
    public async Task SearchInBounds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapBounds == null)
        {
            StatusContext.ToastError("No Map Bounds?");
            return;
        }

        var currentGuids = ListContext.Items.Select(x => x.ContentId()).Where(x => x is not null).Select(x => x!.Value)
            .ToList();
        var searchResultGuids = await (await Db.Context()).ContentIdsFromBoundingBox(MapBounds);

        if (searchResultGuids.All(x => currentGuids.Contains(x)))
        {
            StatusContext.ToastWarning("No New Items Found");
            return;
        }

        var allGuids = currentGuids.Union(searchResultGuids).ToList();

        RefreshMapOnCollectionChanged = false;

        ListContext.ContentListLoader = new ContentListLoaderReport(async () =>
            (await (await Db.Context()).ContentFromContentIds(allGuids)).Cast<object>().ToList());
        await ListContext.LoadData(true);

        RefreshMapOnCollectionChanged = true;

        await RefreshMap(MapBounds);
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