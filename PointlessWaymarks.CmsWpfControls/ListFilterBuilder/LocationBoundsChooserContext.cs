using System.Text.Json;
using System.Text.Json.Nodes;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.GeoSearch;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LocationBoundsChooserContext : IWebViewMessenger
{
    private LocationBoundsChooserContext(StatusControlContext statusContext, GeoSearchContext searchContext,
        string serializedMapIcons)
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
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, false, serializedMapIcons,
            UserSettingsSingleton.CurrentSettings().CalTopoApiKey,
            UserSettingsSingleton.CurrentSettings().BingApiKey);

        LocationSearchContext = searchContext;

        LocationSearchContext.LocationSelected += (sender, args) =>
        {
            var centerData = new MapJsonCoordinateDto(args.Latitude, args.Longitude, "CenterCoordinateRequest");

            var serializedData = JsonSerializer.Serialize(centerData);

            ToWebView.Enqueue(new JsonData { Json = serializedData });
        };
    }

    public bool BroadcastLatLongChange { get; set; } = true;
    public List<Guid> DisplayedContentGuids { get; set; } = [];
    public bool HasValidationIssues { get; set; }
    public SpatialBounds? InitialBounds { get; set; }
    public GeoSearchContext LocationSearchContext { get; set; }
    public SpatialBounds? MapBounds { get; set; }
    public Action<Uri, string> MapPreviewNavigationManager { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public static async Task<LocationBoundsChooserContext> CreateInstance(StatusControlContext windowStatusContext,
        SpatialBounds? initialBounds)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryMapIcons = await MapIconGenerator.SerializedMapIcons();
        var factoryGeoSearchContext = await GeoSearchContext.CreateInstance(windowStatusContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        return new LocationBoundsChooserContext(windowStatusContext, factoryGeoSearchContext, factoryMapIcons)
        {
            InitialBounds = initialBounds
        };
    }

    public async Task LoadData()
    {
        var centerOn = InitialBounds ?? new SpatialBounds(UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault,
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault).ExpandToMinimumMeters(1000);

        var centerData = new MapJsonBoundsDto(
            centerOn, "CenterBoundingBoxRequest");

        await ThreadSwitcher.ResumeForegroundAsync();

        var serializedData = JsonSerializer.Serialize(centerData);

        ToWebView.Enqueue(new JsonData { Json = serializedData });
    }

    public async Task MapMessageReceived(string json)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var parsedJson = JsonNode.Parse(json);

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

    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        if (!string.IsNullOrWhiteSpace(args.Message))
            StatusContext.RunFireAndForgetNonBlockingTask(async () => await MapMessageReceived(args.Message));
        return Task.CompletedTask;
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

        var searchResult = (await (await Db.Context()).ContentFromBoundingBox(MapBounds)).ToList();

        if (!searchResult.Any())
        {
            await StatusContext.ToastWarning("No New Items Found");
            return;
        }

        await StatusContext.ToastSuccess(
            $"Added {searchResult.Count} Item{(searchResult.Count > 1 ? "s" : string.Empty)}");

        var mapInformation = await MapCmsJson.ProcessContentToMapInformation(searchResult.Cast<object>().ToList(), false);
        DisplayedContentGuids =
            DisplayedContentGuids.Union(searchResult.Select(x => x.ContentId).Cast<Guid>()).ToList();

        ToWebView.Enqueue(FileBuilder.CreateRequest(
            mapInformation.fileCopyList.Select(x => new FileBuilderCopy(x, false)).ToList(),
            []));

        ToWebView.Enqueue(JsonData.CreateRequest(await MapCmsJson.NewMapFeatureCollectionDtoSerialized(
            mapInformation.featureList,
            mapInformation.bounds.ExpandToMinimumMeters(1000), "AddFeatureCollection")));
    }
}