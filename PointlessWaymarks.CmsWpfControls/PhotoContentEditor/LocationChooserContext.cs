using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LocationChooserContext : IHasChanges, ICheckForChangesAndValidation,
    IHasValidationIssues, IWebViewMessenger
{
    public LocationChooserContext(StatusControlContext statusContext, string serializedMapIcons)
    {
        StatusContext = statusContext;

        BuildCommands();

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        MapPreviewNavigationManager = MapCmsJson.LocalActionNavigation(StatusContext);

        this.SetupCmsLeafletPointChooserMapHtmlAndJs("Map", UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, serializedMapIcons,
            UserSettingsSingleton.CurrentSettings().CalTopoApiKey, UserSettingsSingleton.CurrentSettings().BingApiKey);

        PropertyChanged += OnPropertyChanged;
    }

    public bool BroadcastLatLongChange { get; set; } = true;
    public List<Guid> DisplayedContentGuids { get; set; } = [];
    public ConversionDataEntryContext<double?>? ElevationEntry { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public double? InitialElevation { get; set; }
    public double InitialLatitude { get; set; }
    public double InitialLongitude { get; set; }
    public ConversionDataEntryContext<double>? LatitudeEntry { get; set; }
    public ConversionDataEntryContext<double>? LongitudeEntry { get; set; }
    public SpatialBounds? MapBounds { get; set; }
    public Action<Uri, string> MapPreviewNavigationManager { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    [NonBlockingCommand]
    public async Task CenterMapOnSelectedLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var centerData =
            new MapJsonCoordinateDto(LatitudeEntry.UserValue, LongitudeEntry.UserValue, "CenterCoordinateRequest");

        var serializedData = JsonSerializer.Serialize(centerData);

        ToWebView.Enqueue(JsonData.CreateRequest(serializedData));
    }

    public static async Task<LocationChooserContext> CreateInstance(StatusControlContext windowStatusContext,
        double? initialLatitude, double? initialLongitude, double? initialElevation)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryMapIcons = await MapIconGenerator.SerializedMapIcons();

        return new LocationChooserContext(windowStatusContext, factoryMapIcons)
        {
            InitialLatitude = initialLatitude ?? UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            InitialLongitude = initialLongitude ?? UserSettingsSingleton.CurrentSettings().LongitudeDefault,
            InitialElevation = initialElevation
        };
    }

    [BlockingCommand]
    public async Task GetElevation()
    {
        if (LatitudeEntry!.HasValidationIssues || LongitudeEntry!.HasValidationIssues)
        {
            StatusContext.ToastError("Lat Long is not valid");
            return;
        }

        var possibleElevation =
            await ElevationGuiHelper.GetElevation(LatitudeEntry.UserValue, LongitudeEntry.UserValue, StatusContext);

        if (possibleElevation != null) ElevationEntry!.UserText = possibleElevation.Value.ToString("F2");
    }

    private void LatitudeLongitudeChangeBroadcast()
    {
        if (BroadcastLatLongChange && !LatitudeEntry!.HasValidationIssues && !LongitudeEntry!.HasValidationIssues)
        {
            var centerData = new MapJsonCoordinateDto(LatitudeEntry.UserValue, LongitudeEntry.UserValue,
                "MoveUserLocationSelection");

            var serializedData = JsonSerializer.Serialize(centerData);

            ToWebView.Enqueue(JsonData.CreateRequest(serializedData));
        }
    }

    public async Task LoadData()
    {
        ElevationEntry =
            await ConversionDataEntryContext<double?>.CreateInstance(
                ConversionDataEntryHelpers.DoubleNullableConversion);
        ElevationEntry.ValidationFunctions = [CommonContentValidation.ElevationValidation];
        ElevationEntry.ComparisonFunction = (o, u) => (o == null && u == null) || o.IsApproximatelyEqualTo(u, .001);
        ElevationEntry.Title = "Elevation (m)";
        ElevationEntry.HelpText = "Elevation in Meters";
        ElevationEntry.ReferenceValue = InitialElevation;
        ElevationEntry.UserText = InitialElevation?.ToString("F2") ?? string.Empty;

        LatitudeEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LatitudeEntry.ValidationFunctions = [CommonContentValidation.LatitudeValidation];
        LatitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
        LatitudeEntry.Title = "Latitude";
        LatitudeEntry.HelpText = "In DDD.DDDDDD°";
        LatitudeEntry.ReferenceValue = InitialLatitude;
        LatitudeEntry.UserText = InitialLatitude.ToString("F6");
        LatitudeEntry.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
            if (args.PropertyName == nameof(LatitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
        };

        LongitudeEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LongitudeEntry.ValidationFunctions = [CommonContentValidation.LongitudeValidation];
        LongitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
        LongitudeEntry.Title = "Longitude";
        LongitudeEntry.HelpText = "In DDD.DDDDDD°";
        LongitudeEntry.ReferenceValue = InitialLongitude;
        LongitudeEntry.UserText = InitialLongitude.ToString("F6");
        LongitudeEntry.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
            if (args.PropertyName == nameof(LongitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
        };

        LatitudeLongitudeChangeBroadcast();

        var db = await Db.Context();
        var searchBounds = SpatialBounds.FromCoordinates(LatitudeEntry.UserValue, LongitudeEntry.UserValue, 5000);

        var closeByFeatures = (await db.ContentFromBoundingBox(searchBounds)).ToList();
        var mapInformation = await MapCmsJson.ProcessContentToMapInformation(closeByFeatures.Cast<object>().ToList());
        DisplayedContentGuids =
            DisplayedContentGuids.Union(closeByFeatures.Select(x => x.ContentId).Cast<Guid>()).ToList();

        ToWebView.Enqueue(
            FileBuilder.CreateRequest(mapInformation.fileCopyList.Select(x => new FileBuilderCopy(x, false)).ToList(),
                []));

        ToWebView.Enqueue(JsonData.CreateRequest(await MapCmsJson.NewMapFeatureCollectionDtoSerialized(
            mapInformation.featureList,
            mapInformation.bounds.ExpandToMinimumMeters(1000), "NewFeatureCollection")));

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public async Task MapMessageReceived(string json)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var parsedJson = JsonNode.Parse(json);

        if (parsedJson == null) return;

        var messageType = parsedJson["messageType"]?.ToString() ?? string.Empty;

        if (messageType.Equals("userSelectedLatitudeLongitudeChanged",
                StringComparison.InvariantCultureIgnoreCase))
        {
            var latitude = parsedJson["latitude"]?.GetValue<double>();
            var longitude = parsedJson["longitude"]?.GetValue<double>();

            if (latitude == null || longitude == null) return;

            BroadcastLatLongChange = false;

            LatitudeEntry!.UserText = latitude.Value.ToString("F6");
            LongitudeEntry!.UserText = longitude.Value.ToString("F6");

            BroadcastLatLongChange = true;
        }

        if (messageType == "mapBoundsChange")
        {
            MapBounds = new SpatialBounds(parsedJson["bounds"]["_northEast"]["lat"].GetValue<double>(),
                parsedJson["bounds"]["_northEast"]["lng"].GetValue<double>(),
                parsedJson["bounds"]["_southWest"]["lat"].GetValue<double>(),
                parsedJson["bounds"]["_southWest"]["lng"].GetValue<double>());
            return;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
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
            StatusContext.ToastError("No Map Bounds?");
            return;
        }

        var searchResult = (await (await Db.Context()).ContentFromBoundingBox(MapBounds)).ToList();

        if (!searchResult.Any())
        {
            StatusContext.ToastWarning("No New Items Found");
            return;
        }

        StatusContext.ToastSuccess($"Added {searchResult.Count} Item{(searchResult.Count > 1 ? "s" : string.Empty)}");

        var mapInformation = await MapCmsJson.ProcessContentToMapInformation(searchResult.Cast<object>().ToList());
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