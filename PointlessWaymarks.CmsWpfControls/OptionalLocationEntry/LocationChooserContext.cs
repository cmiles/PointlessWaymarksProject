using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.GeoSearch;
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

namespace PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LocationChooserContext : IHasChanges, ICheckForChangesAndValidation,
    IHasValidationIssues, IWebViewMessenger
{
    public LocationChooserContext(StatusControlContext statusContext, string serializedMapIcons,
        GeoSearchContext factoryLocationSearchContext)
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
        
        LocationSearchContext = factoryLocationSearchContext;
        
        LocationSearchContext.LocationSelected += (sender, args) =>
        {
            var centerData = new MapJsonCoordinateDto(args.Latitude, args.Longitude, "CenterCoordinateRequest");
            
            var serializedData = JsonSerializer.Serialize(centerData);
            
            ToWebView.Enqueue(new JsonData { Json = serializedData });
        };
    }
    
    public bool BroadcastLatLongChange { get; set; } = true;
    public List<Guid> DisplayedContentGuids { get; set; } = [];
    public ConversionDataEntryContext<double?>? ElevationEntry { get; set; }
    public double? InitialElevation { get; set; }
    public double InitialLatitude { get; set; }
    public double InitialLongitude { get; set; }
    public ConversionDataEntryContext<double>? LatitudeEntry { get; set; }
    public GeoSearchContext LocationSearchContext { get; set; }
    public ConversionDataEntryContext<double>? LongitudeEntry { get; set; }
    public SpatialBounds? MapBounds { get; set; }
    public Action<Uri, string> MapPreviewNavigationManager { get; set; }
    public StatusControlContext StatusContext { get; set; }
    
    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }
    
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    
    [NonBlockingCommand]
    public async Task CenterMapOnSelectedLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var centerData =
            new MapJsonCoordinateDto(LatitudeEntry.UserValue, LongitudeEntry.UserValue, "CenterCoordinateRequest");
        
        var serializedData = JsonSerializer.Serialize(centerData);
        
        ToWebView.Enqueue(JsonData.CreateRequest(serializedData));
    }
    
    [NonBlockingCommand]
    public async Task ClearSearchInBounds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapBounds == null)
        {
            await StatusContext.ToastError("No Map Bounds?");
            return;
        }
        
        var searchResultIds = (await (await Db.Context()).ContentFromBoundingBox(MapBounds)).Select(x => x.ContentId)
            .Cast<Guid>().ToList();
        
        if (!searchResultIds.Any())
        {
            await StatusContext.ToastWarning("No Items Found in Bounds?");
            return;
        }
        
        DisplayedContentGuids = DisplayedContentGuids.Where(x => !searchResultIds.Contains(x)).ToList();
        
        ToWebView.Enqueue(
            JsonData.CreateRequest(
                JsonSerializer.Serialize(new MapJsonFeatureListDto(searchResultIds, "RemoveFeatures"))));
    }
    
    public static async Task<LocationChooserContext> CreateInstance(StatusControlContext windowStatusContext,
        double? initialLatitude, double? initialLongitude, double? initialElevation)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var factoryMapIcons = await MapIconGenerator.SerializedMapIcons();
        var factoryLocationSearchContext = await GeoSearchContext.CreateInstance(windowStatusContext);
        
        return new LocationChooserContext(windowStatusContext, factoryMapIcons, factoryLocationSearchContext)
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
            await StatusContext.ToastError("Lat Long is not valid");
            return;
        }
        
        var possibleElevation =
            await ElevationGuiHelper.GetElevation(LatitudeEntry.UserValue, LongitudeEntry.UserValue, StatusContext);
        
        if (possibleElevation != null) ElevationEntry!.UserText = possibleElevation.Value.MetersToFeet().ToString("N0");
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
        ElevationEntry.Title = "Elevation (feet)";
        ElevationEntry.HelpText = "Elevation in Feet";
        ElevationEntry.ReferenceValue = InitialElevation;
        ElevationEntry.UserText = InitialElevation?.ToString("N0") ?? string.Empty;
        
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
    public async Task SearchGeoJsonInBounds()
    {
        await SearchInBounds([Db.ContentTypeDisplayStringForGeoJson]);
    }
    
    public async Task SearchInBounds(List<string> searchContentTypes)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (MapBounds == null)
        {
            await StatusContext.ToastError("No Map Bounds?");
            return;
        }
        
        var searchResult = (await (await Db.Context()).ContentFromBoundingBox(MapBounds, searchContentTypes))
            .Where(x => !DisplayedContentGuids.Contains(x.ContentId)).ToList();
        
        if (!searchResult.Any())
        {
            await StatusContext.ToastWarning("No New Items Found");
            return;
        }
        
        await StatusContext.ToastSuccess($"Added {searchResult.Count} Item{(searchResult.Count > 1 ? "s" : string.Empty)}");
        
        var mapInformation = await MapCmsJson.ProcessContentToMapInformation(searchResult.Cast<object>().ToList(), false);
        DisplayedContentGuids =
            DisplayedContentGuids.Union(searchResult.Select(x => x.ContentId).Cast<Guid>()).ToList();
        
        ToWebView.Enqueue(
            FileBuilder.CreateRequest(mapInformation.fileCopyList.Select(x => new FileBuilderCopy(x, false)).ToList(),
                []));
        ToWebView.Enqueue(JsonData.CreateRequest(await MapCmsJson.NewMapFeatureCollectionDtoSerialized(
            mapInformation.featureList,
            mapInformation.bounds.ExpandToMinimumMeters(1000), "AddFeatureCollection")));
    }
    
    [NonBlockingCommand]
    public async Task SearchLinesInBounds()
    {
        await SearchInBounds([Db.ContentTypeDisplayStringForLine]);
    }
    
    [NonBlockingCommand]
    public async Task SearchPhotosInBounds()
    {
        await SearchInBounds([Db.ContentTypeDisplayStringForPhoto]);
    }
    
    [NonBlockingCommand]
    public async Task SearchPointsInBounds()
    {
        await SearchInBounds([Db.ContentTypeDisplayStringForPoint]);
    }
}