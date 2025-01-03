using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Web;
using CommunityToolkit.Mvvm.Messaging;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.FileMetadataDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FileBasedGeoTaggerContext
{
    public FileBasedGeoTaggerContext(StatusControlContext statusContext, WindowIconStatus? windowStatus)
    {
        StatusContext = statusContext;
        WindowStatus = windowStatus;

        BuildCommands();

        Settings = new FileBasedGeoTaggerSettings();

        PropertyChanged += OnPropertyChanged;
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    public bool CreateBackups { get; set; }
    public bool CreateBackupsInDefaultStorage { get; set; }
    public bool ExifToolExists { get; set; }
    public FileListContext? FilesToTagFileList { get; set; }
    public FileBasedGeoTaggerFilesToTagSettings? FilesToTagSettings { get; set; }
    public FileListContext? GpxFileList { get; set; }
    public FileBasedGeoTaggerGpxFilesSettings? GpxFilesSettings { get; set; }
    public int OffsetPhotoTimeInMinutes { get; set; }
    public bool OverwriteExistingGeoLocation { get; set; }
    public int PointsMustBeWithinMinutes { get; set; } = 10;
    public bool PreviewHasWritablePoints { get; set; }
    public WebViewMessenger PreviewMap { get; set; } = new();
    public GeoTag.GeoTagProduceActionsResult? PreviewResults { get; set; }
    public int SelectedTab { get; set; }
    public FileBasedGeoTaggerSettings Settings { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }
    public WebViewMessenger WriteMap { get; set; } = new();
    public GeoTag.GeoTagWriteMetadataToFilesResult? WriteToFileResults { get; set; }

    public async Task CheckThatExifToolExists(bool saveSettings)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ExifToolFullName))
        {
            ExifToolExists = false;
            return;
        }

        var exists = File.Exists(Settings.ExifToolFullName.Trim());

        if (exists && saveSettings)
        {
            await FileBasedGeoTaggerSettingTools.WriteSettings(Settings);
            WeakReferenceMessenger.Default.Send(new ExifToolSettingsUpdateMessage((this, Settings.ExifToolFullName)));
        }

        ExifToolExists = exists;
    }

    [BlockingCommand]
    public async Task ChooseExifFile()
    {
        var newFile = await ExifFilePicker.ChooseExifFile(StatusContext, Settings.ExifToolFullName);

        if (!newFile.validFileFound) return;

        if (Settings.ExifToolFullName.Equals(newFile.pickedFileName)) return;

        Settings.ExifToolFullName = newFile.pickedFileName;
    }

    public static async Task<FileBasedGeoTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new FileBasedGeoTaggerContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    [BlockingCommand]
    public async Task GeneratePreview()
    {
        await FileBasedGeoTaggerSettingTools.WriteSettings(Settings);

        Debug.Assert(GpxFileList != null, nameof(GpxFileList) + " != null");
        Debug.Assert(FilesToTagFileList != null, nameof(FilesToTagFileList) + " != null");

        if (GpxFileList.Files == null || !GpxFileList.Files.Any() || FilesToTagFileList.Files == null ||
            !FilesToTagFileList.Files.Any())
        {
            await StatusContext.ToastError("No GPX Files Selected?");
            return;
        }

        if (FilesToTagFileList.Files == null || !FilesToTagFileList.Files.Any())
        {
            await StatusContext.ToastError("No Files to Tag Selected?");
            return;
        }

        WriteToFileResults = null;
        WriteMap.ToWebView.Enqueue(JsonData.CreateRequest(await ResetMapGeoJsonDto()));

        var fileListGpxService = new FileListGpxService(GpxFileList.Files!.ToList());
        var tagger = new GeoTag();
        PreviewResults = await tagger.ProduceGeoTagActions(FilesToTagFileList.Files!.ToList(),
            [fileListGpxService],
            PointsMustBeWithinMinutes, OffsetPhotoTimeInMinutes, OverwriteExistingGeoLocation,
            StatusContext.ProgressTracker(),
            Settings.ExifToolFullName);

        var pointsToWrite = PreviewResults.FileResults.Where(x => x.ShouldWriteMetadata).ToList();

        PreviewHasWritablePoints = pointsToWrite.Count != 0;

        if (PreviewHasWritablePoints)
        {
            var features = new FeatureCollection();

            var locationGroupedList = pointsToWrite.GroupBy(x => new { x.Latitude, x.Longitude }).ToList();

            foreach (var loopResults in locationGroupedList)
            {
                var sources = loopResults.GroupBy(x => x.Source).SelectMany(x => x.Select(y => y.Source)).Distinct()
                    .ToList();

                features.Add(new Feature(
                    PointTools.Wgs84Point(loopResults.Key.Longitude!.Value, loopResults.Key.Latitude!.Value),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        { "title", $"From {string.Join(", ", sources)}" },
                        { "description", string.Join("<br>", loopResults.Select(x => x.FileName)) }
                    })));
            }

            var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

            var jsonDto = await MapJson.NewMapFeatureCollectionDtoSerialized(features.AsList(),
                SpatialBounds.FromEnvelope(bounds));

            PreviewMap.ToWebView.Enqueue(JsonData.CreateRequest(jsonDto));
        }

        SelectedTab = 3;
    }

    public async Task Load()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        Settings = await FileBasedGeoTaggerSettingTools.ReadSettings();
        Settings.PropertyChanged += OnSettingsPropertyChanged;

        FilesToTagSettings = new FileBasedGeoTaggerFilesToTagSettings(this);
        GpxFilesSettings = new FileBasedGeoTaggerGpxFilesSettings(this);

        FilesToTagFileList = await FileListContext.CreateInstance(StatusContext, FilesToTagSettings,
        [
            new ContextMenuItemData
                { ItemCommand = MetadataForSelectedFilesToTagCommand, ItemName = "Metadata Report for Selected" }
        ]);

        GpxFileList = await FileListContext.CreateInstance(StatusContext, GpxFilesSettings,
            [new ContextMenuItemData { ItemCommand = ShowSelectedGpxFilesCommand, ItemName = "Show  Selected" }]);
        GpxFileList.FileImportFilter = "gpx files (*.gpx)|*.gpx|All files (*.*)|*.*";
        GpxFileList.DroppedFileExtensionAllowList = [".gpx"];

        PreviewMap.SetupCmsLeafletMapHtmlAndJs("Preview", 32.12063, -110.52313, true);
        WriteMap.SetupCmsLeafletMapHtmlAndJs("Write", 32.12063, -110.52313, true);

        await CheckThatExifToolExists(false);
    }

    [BlockingCommand]
    public async Task MetadataForSelectedFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (FilesToTagFileList?.SelectedFiles == null)
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var frozenSelected = FilesToTagFileList.SelectedFiles.ToList();

        if (frozenSelected.Count == 0)
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        if (frozenSelected.Count > 10)
        {
            await StatusContext.ToastWarning("Sorry - dumping metadata limited to 10 files at once...");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopFile in frozenSelected)
        {
            var metadataWindow = await FileMetadataDisplayWindow.CreateInstance(loopFile.FullName);
            await metadataWindow.PositionWindowAndShowOnUiThread();
        }
    }

    [NonBlockingCommand]
    public async Task NextTab()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedTab++;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings))
            StatusContext.RunNonBlockingTask(async () => await CheckThatExifToolExists(false));
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings.ExifToolFullName))
            StatusContext.RunNonBlockingTask(async () => await CheckThatExifToolExists(true));
    }

    private async Task<string> ResetMapGeoJsonDto()
    {
        var features = new FeatureCollection();

        var basePoint = PointTools.Wgs84Point(-110.52313, 32.12063);
        var bounds = new Envelope();
        bounds.ExpandToInclude(basePoint.Coordinate);
        bounds.ExpandBy(1000);

        var jsonDto = new GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
            new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

        return await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
    }

    [NonBlockingCommand]
    public async Task SendResultFilesToFeatureIntersectTagger()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (WriteToFileResults?.FileResults == null || WriteToFileResults.FileResults.Count == 0)
        {
            await StatusContext.ToastError("No Results to Send");
            return;
        }

        WeakReferenceMessenger.Default.Send(new FeatureIntersectFileAddRequestMessage((this,
            WriteToFileResults.FileResults.Select(x => x.FileName).ToList())));
    }

    [BlockingCommand]
    public async Task ShowSelectedGpxFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (GpxFileList?.SelectedFiles == null || !GpxFileList.SelectedFiles.Any())
        {
            await StatusContext.ToastWarning("No gpx files selected?");
            return;
        }

        var frozenSelected = GpxFileList.SelectedFiles.ToList();

        var featureList = new List<Feature>();
        var bounds = new Envelope();

        foreach (var loopFiles in frozenSelected)
        {
            var fileFeatures = await GpxTools.TrackLinesFromGpxFile(loopFiles);
            bounds.ExpandToInclude(fileFeatures.boundingBox);
            featureList.AddRange(fileFeatures.features);
        }

        var newCollection = new FeatureCollection();
        featureList.ForEach(x => newCollection.Add(x));

        var jsonDto = new GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
            new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), newCollection);

        var previewDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newPreviewWindow = await WebViewWindow.CreateInstance();
        newPreviewWindow.PositionWindowAndShow();
        newPreviewWindow.SetupCmsLeafletMapHtmlAndJs("GPX Preview", 32.12063, -110.52313, true);
        newPreviewWindow.ToWebView.Enqueue(JsonData.CreateRequest(previewDto));
    }

    [BlockingCommand]
    public async Task WriteResultsToFile()
    {
        await FileBasedGeoTaggerSettingTools.WriteSettings(Settings);

        if (PreviewResults == null)
        {
            await StatusContext.ToastError("No Results to Write?");
            return;
        }

        var tagger = new GeoTag();

        WriteToFileResults = await tagger.WriteGeoTagActions(
            PreviewResults.FileResults.Where(x => x.ShouldWriteMetadata).ToList(),
            CreateBackups, CreateBackupsInDefaultStorage,
            Settings.ExifToolFullName, StatusContext.ProgressTracker());

        var writtenResults = WriteToFileResults.FileResults.Where(x => x.WroteMetadata).ToList();

        if (writtenResults.Count == 0)
        {
            WriteMap.ToWebView.Enqueue(JsonData.CreateRequest(await ResetMapGeoJsonDto()));
        }
        else
        {
            var features = new FeatureCollection();

            var locationGroupedList = writtenResults.GroupBy(x => new { x.Latitude, x.Longitude }).ToList();

            foreach (var loopResults in locationGroupedList)
            {
                var sources = loopResults.GroupBy(x => x.Source).SelectMany(x => x.Select(y => y.Source)).Distinct()
                    .ToList();

                features.Add(new Feature(
                    PointTools.Wgs84Point(loopResults.Key.Longitude!.Value, loopResults.Key.Latitude!.Value),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        { "title", $"From {string.Join(", ", sources)}" },
                        { "description", string.Join("<br>", loopResults.Select(x => x.FileName)) }
                    })));
            }

            var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

            var jsonDto = await MapJson.NewMapFeatureCollectionDtoSerialized(features.AsList(),
                SpatialBounds.FromEnvelope(bounds));

            WriteMap.ToWebView.Enqueue(JsonData.CreateRequest(jsonDto));
        }

        SelectedTab = 4;
    }

    public record GeoJsonSiteJsonData(string PageUrl, SpatialBounds Bounds, FeatureCollection GeoJson);
}