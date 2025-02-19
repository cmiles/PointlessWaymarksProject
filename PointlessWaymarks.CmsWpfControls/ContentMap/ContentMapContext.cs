using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.GeoSearch;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ContentMapContext : IWebViewMessenger
{
    private ContentMapContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
        ContentListContext factoryListContext, string serializedMapIcons, GeoSearchContext factoryLocationSearchContext,
        bool loadInBackground = true)
    {
        StatusContext = statusContext;
        WindowStatus = windowStatus;

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        MapPreviewNavigationManager = MapCmsJson.LocalActionNavigation(StatusContext);

        this.SetupCmsLeafletMapHtmlAndJs("Map", UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, false, serializedMapIcons,
            UserSettingsSingleton.CurrentSettings().CalTopoApiKey, UserSettingsSingleton.CurrentSettings().BingApiKey);

        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);
        ListContext = factoryListContext;
        BuildCommands();
        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);

        LocationSearchContext = factoryLocationSearchContext;

        LocationSearchContext.LocationSelected += (sender, args) =>
        {
            var centerData = new MapJsonCoordinateDto(args.Latitude, args.Longitude, "CenterCoordinateRequest");

            var serializedData = JsonSerializer.Serialize(centerData);

            ToWebView.Enqueue(new JsonData { Json = serializedData });
        };

        PropertyChanged += OnPropertyChanged;
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public Envelope? ContentBounds { get; set; }
    public ContentListContext ListContext { get; set; }
    public GeoSearchContext LocationSearchContext { get; set; }
    public SpatialBounds? MapBounds { get; set; } = null;
    public Action<Uri, string> MapPreviewNavigationManager { get; set; }
    public bool RefreshMapOnCollectionChanged { get; set; }
    public bool ShowPhotoDirectionBearingLines { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public async Task ClearBasedOnBounds(bool clearInside)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapBounds == null)
        {
            await StatusContext.ToastError("No Map Bounds?");
            return;
        }

        var currentItems = ListContext.Items.ToList();

        var itemsToRemove = new List<IContentListItem>();

        foreach (var loopItem in currentItems)
        {
            var isInside = loopItem switch
            {
                FileListListItem file => Db.OptionalLocationContentIsInBoundingBox(file.DbEntry, MapBounds),
                GeoJsonListListItem geo => Db.GeoJsonBoundingBoxOverlaps(geo.DbEntry, MapBounds),
                ImageListListItem image => Db.OptionalLocationContentIsInBoundingBox(image.DbEntry, MapBounds),
                LineListListItem line => Db.LineContentBoundingBoxOverlaps(line.DbEntry, MapBounds),
                PhotoListListItem photo => Db.OptionalLocationContentIsInBoundingBox(photo.DbEntry, MapBounds),
                PostListListItem post => Db.OptionalLocationContentIsInBoundingBox(post.DbEntry, MapBounds),
                PointListListItem point => Db.PointContentIsInBoundingBox(point.DbEntry.ToDbObject(), MapBounds),
                VideoListListItem video => Db.OptionalLocationContentIsInBoundingBox(video.DbEntry, MapBounds),
                _ => false
            };

            if ((clearInside && isInside) || (!clearInside && !isInside)) itemsToRemove.Add(loopItem);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        itemsToRemove.ForEach(x => ListContext.Items.Remove(x));
    }

    [NonBlockingCommand]
    public async Task ClearInsideMapBounds()
    {
        await ClearBasedOnBounds(true);
    }

    [NonBlockingCommand]
    public async Task ClearOutsideMapBounds()
    {
        await ClearBasedOnBounds(false);
    }

    [NonBlockingCommand]
    public Task CloseAllPopups()
    {
        var jsRequest = new ExecuteJavaScript
            { JavaScriptToExecute = "closeAllPopups()", RequestTag = "Map Component Editor Close All Popups Command" };
        ToWebView.Enqueue(jsRequest);
        return Task.CompletedTask;
    }

    public static async Task<ContentMapContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus, bool loadInBackground = true)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new AllContentListLoader(100), [],
                windowStatus);
        var factoryIcons = await MapIconGenerator.SerializedMapIcons();
        var factoryLocationSearchContext = await GeoSearchContext.CreateInstance(factoryStatusContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new ContentMapContext(factoryStatusContext, windowStatus, factoryListContext, factoryIcons,
            factoryLocationSearchContext,
            loadInBackground);
        toReturn.ListContext.ItemsView().CollectionChanged += toReturn.ItemsViewOnCollectionChanged;

        return toReturn;
    }

    public static async Task<ContentMapContext> CreateInstance(StatusControlContext? statusContext,
        IContentListLoader reportFilter, bool loadInBackground = true)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryListContext = await ContentListContext.CreateInstance(factoryStatusContext, reportFilter, []);
        var factoryIcons = await MapIconGenerator.SerializedMapIcons();
        var factoryLocationSearchContext = await GeoSearchContext.CreateInstance(factoryStatusContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new ContentMapContext(factoryStatusContext, null, factoryListContext, factoryIcons,
            factoryLocationSearchContext,
            loadInBackground);
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

        ListContext.ContextMenuItems =
        [
            new ContextMenuItemData { ItemName = "Center Map", ItemCommand = RequestMapCenterOnSelectedItemsCommand },
            new ContextMenuItemData { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "Code to Clipboard", ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },

            new ContextMenuItemData { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new ContextMenuItemData { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new ContextMenuItemData { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "View Selected Pictures and Videos",
                ItemCommand = ListContext.PicturesAndVideosViewWindowSelectedCommand
            }
        ];

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
            try
            {
                MapBounds = new SpatialBounds(parsedJson["bounds"]["_northEast"]["lat"].GetValue<double>(),
                    parsedJson["bounds"]["_northEast"]["lng"].GetValue<double>(),
                    parsedJson["bounds"]["_southWest"]["lat"].GetValue<double>(),
                    parsedJson["bounds"]["_southWest"]["lng"].GetValue<double>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName == nameof(ShowPhotoDirectionBearingLines))
            StatusContext.RunBlockingTask(async () => await RefreshMap(MapBounds));
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

        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }

    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        if (!string.IsNullOrWhiteSpace(args.Message))
            StatusContext.RunFireAndForgetNonBlockingTask(async () => await MapMessageReceived(args.Message));
        return Task.CompletedTask;
    }

    [BlockingCommand]
    public async Task RefreshMap(SpatialBounds? bounds = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenItems = await ListContext.FilteredListItems();

        if (frozenItems.Count == 0) frozenItems = ListContext.Items.ToList();

        if (frozenItems.Count < 1)
        {
            ToWebView.Enqueue(new JsonData
            {
                Json = await GeoJsonTools.SerializeWithGeoJsonSerializer(
                    new MapJsonNewFeatureCollectionDto(
                        Guid.NewGuid(),
                        new SpatialBounds(0, 0, 0, 0), []))
            });
            return;
        }

        var mapInformation =
            await MapCmsJson.ProcessContentToMapInformation(frozenItems, ShowPhotoDirectionBearingLines);

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
                mapInformation.featureList, bounds ??
                                            mapInformation.bounds.ExpandToMinimumMeters(1000))
        });
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnAllItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!ListContext.Items.Any())
        {
            await StatusContext.ToastError("No Items?");
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
        var serializedData = JsonSerializer.Serialize(centerData);

        await ThreadSwitcher.ResumeForegroundAsync();

        ToWebView.Enqueue(new JsonData { Json = serializedData });
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

        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }

    [NonBlockingCommand]
    public async Task RequestMapCenterOnFilteredItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var filteredItems = await ListContext.FilteredListItems();

        if (!filteredItems.Any())
        {
            await StatusContext.ToastError("No Visible Items?");
            return;
        }

        var bounds = MapCmsJson.GetBounds(filteredItems);

        await RequestMapCenterOnEnvelope(bounds);
    }

    public async Task RequestMapCenterOnPoint(double longitude, double latitude)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var centerData = new MapJsonCoordinateDto(latitude, longitude, "CenterCoordinateRequest");

        var serializedData = JsonSerializer.Serialize(centerData);

        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task RequestMapCenterOnSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var bounds = MapCmsJson.GetBounds(ListContext.SelectedListItems());

        await RequestMapCenterOnEnvelope(bounds);
    }

    [NonBlockingCommand]
    public async Task SearchInBounds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (MapBounds == null)
        {
            await StatusContext.ToastError("No Map Bounds?");
            return;
        }

        var currentGuids = ListContext.Items.Select(x => x.ContentId()).Where(x => x is not null).Select(x => x!.Value)
            .ToList();
        var searchResultGuids = await (await Db.Context()).ContentIdsFromBoundingBox(MapBounds);

        if (searchResultGuids.All(x => currentGuids.Contains(x)))
        {
            await StatusContext.ToastWarning("No New Items Found");
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