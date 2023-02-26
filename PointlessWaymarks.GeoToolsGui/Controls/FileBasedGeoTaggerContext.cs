using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;
using static PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml.GeoJsonData;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public partial class FileBasedGeoTaggerContext : ObservableObject
{
    [ObservableProperty] private bool _createBackups;
    [ObservableProperty] private bool _createBackupsInDefaultStorage;
    [ObservableProperty] private bool _exifToolExists;
    [ObservableProperty] private FileListViewModel? _filesToTagFileList;
    [ObservableProperty] private FileBasedGeoTaggerFilesToTagSettings? _filesToTagSettings;
    [ObservableProperty] private FileListViewModel? _gpxFileList;
    [ObservableProperty] private FileBasedGeoTaggerGpxFilesSettings? _gpxFilesSettings;
    [ObservableProperty] private int _offsetPhotoTimeInMinutes;
    [ObservableProperty] private bool _overwriteExistingGeoLocation;
    [ObservableProperty] private int _pointsMustBeWithinMinutes = 10;
    [ObservableProperty] private string _previewGeoJsonDto = string.Empty;
    [ObservableProperty] private bool _previewHasWritablePoints;
    [ObservableProperty] private string _previewHtml = string.Empty;
    [ObservableProperty] private GeoTag.GeoTagProduceActionsResult? _previewResults;
    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private FileBasedGeoTaggerSettings _settings;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;
    [ObservableProperty] private string _writeToFileGeoJsonDto = string.Empty;
    [ObservableProperty] private string _writeToFileHtml = string.Empty;
    [ObservableProperty] private GeoTag.GeoTagWriteMetadataToFilesResult? _writeToFileResults;

    public FileBasedGeoTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;

        _settings = new FileBasedGeoTaggerSettings();

        MetadataForSelectedFilesToTagCommand = StatusContext.RunBlockingTaskCommand(MetadataForSelectedFilesToTag);
        ShowSelectedGpxFilesCommand = StatusContext.RunBlockingTaskCommand(ShowSelectedGpxFiles);
        ChooseExifFileCommand = StatusContext.RunBlockingTaskCommand(ChooseExifFile);

        GeneratePreviewCommand = StatusContext.RunBlockingTaskCommand(GeneratePreview);
        WriteToFilesCommand = StatusContext.RunBlockingTaskCommand(WriteResultsToFile);

        NextTabCommand = StatusContext.RunNonBlockingActionCommand(() => SelectedTab++);
        SendResultFilesToFeatureIntersectTaggerCommand =
            StatusContext.RunNonBlockingTaskCommand(SendResultFilesToFeatureIntersectTagger);
    }

    public RelayCommand? ChooseExifFileCommand { get; }

    public RelayCommand? GeneratePreviewCommand { get; }

    public RelayCommand? MetadataForSelectedFilesToTagCommand { get; }

    public RelayCommand NextTabCommand { get; }

    public RelayCommand? SendResultFilesToFeatureIntersectTaggerCommand { get; }

    public RelayCommand? ShowSelectedGpxFilesCommand { get; }

    public RelayCommand? WriteToFilesCommand { get; }

    public async Task CheckThatExifToolExistsAndSaveSettings()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ExifToolFullName))
        {
            ExifToolExists = false;
            return;
        }

        var exists = File.Exists(Settings.ExifToolFullName.Trim());

        if (exists)
        {
            await FileBasedGeoTaggerSettingTools.WriteSettings(Settings);
            WeakReferenceMessenger.Default.Send(new ExifToolSettingsUpdateMessage((this, Settings.ExifToolFullName)));
        }

        ExifToolExists = exists;
    }

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

    public async Task GeneratePreview()
    {
        await FileBasedGeoTaggerSettingTools.WriteSettings(Settings);

        Debug.Assert(GpxFileList != null, nameof(GpxFileList) + " != null");
        Debug.Assert(FilesToTagFileList != null, nameof(FilesToTagFileList) + " != null");
        
        if (GpxFileList.Files == null || !GpxFileList.Files.Any() || FilesToTagFileList.Files == null ||
            !FilesToTagFileList.Files.Any())
        {
            StatusContext.ToastError("No GPX Files Selected?");
            return;
        }

        if (FilesToTagFileList.Files == null || !FilesToTagFileList.Files.Any())
        {
            StatusContext.ToastError("No Files to Tag Selected?");
            return;
        }

        WriteToFileResults = null;
        WriteToFileGeoJsonDto = await ResetMapGeoJsonDto();

        var fileListGpxService = new FileListGpxService(GpxFileList.Files!.ToList());
        var tagger = new GeoTag();
        PreviewResults = await tagger.ProduceGeoTagActions(FilesToTagFileList.Files!.ToList(),
            new List<IGpxService> { fileListGpxService },
            PointsMustBeWithinMinutes, OffsetPhotoTimeInMinutes, OverwriteExistingGeoLocation,
            Settings.ExifToolFullName,
            StatusContext.ProgressTracker());

        var pointsToWrite = PreviewResults.FileResults.Where(x => x.ShouldWriteMetadata).ToList();

        PreviewHasWritablePoints = pointsToWrite.Any();

        if (PreviewHasWritablePoints)
        {
            var features = new FeatureCollection();

            foreach (var loopResults in pointsToWrite)
                features.Add(new Feature(PointTools.Wgs84Point(loopResults.Longitude!.Value, loopResults.Latitude!.Value),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopResults.FileName }, { "description", $"From {loopResults.Source}" } })));

            var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

            var jsonDto = new GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
                new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

            PreviewGeoJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
        }

        SelectedTab = 3;
    }

    public async Task Load()
    {
        Settings = await FileBasedGeoTaggerSettingTools.ReadSettings();

        FilesToTagSettings = new FileBasedGeoTaggerFilesToTagSettings(this);
        GpxFilesSettings = new FileBasedGeoTaggerGpxFilesSettings(this);

        FilesToTagFileList = await FileListViewModel.CreateInstance(StatusContext, FilesToTagSettings,
            new List<ContextMenuItemData>
            {
                new() { ItemCommand = MetadataForSelectedFilesToTagCommand, ItemName = "Metadata Report for Selected" }
            });

        GpxFileList = await FileListViewModel.CreateInstance(StatusContext, GpxFilesSettings,
            new List<ContextMenuItemData>
                { new() { ItemCommand = ShowSelectedGpxFilesCommand, ItemName = "Show  Selected" } });
        GpxFileList.FileImportFilter = "gpx files (*.gpx)|*.gpx|All files (*.*)|*.*";
        GpxFileList.DroppedFileExtensionAllowList = new List<string> { ".gpx" };

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("Preview",
            32.12063, -110.52313, string.Empty);

        WriteToFileHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("WrittenFiles",
            32.12063, -110.52313, string.Empty);

        Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    public async Task MetadataForSelectedFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (FilesToTagFileList?.SelectedFiles == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var frozenSelected = FilesToTagFileList.SelectedFiles.ToList();

        if (!frozenSelected.Any())
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        if (frozenSelected.Count > 10)
        {
            StatusContext.ToastWarning("Sorry - dumping metadata limited to 10 files at once...");
            return;
        }

        foreach (var loopFile in frozenSelected)
        {
            loopFile.Refresh();
            if (!loopFile.Exists)
            {
                StatusContext.ToastWarning($"File {loopFile.FullName} no longer exists?");
                continue;
            }

            var htmlParts = new List<string>();

            if (loopFile.Extension.Equals(".xmp", StringComparison.OrdinalIgnoreCase))
            {
                IXmpMeta xmp;
                await using (var stream = File.OpenRead(loopFile.FullName))
                {
                    xmp = XmpMetaFactory.Parse(stream);
                }

                htmlParts.Add(xmp.Properties.OrderBy(x => x.Namespace).ThenBy(x => x.Path)
                    .Select(x => new { x.Namespace, x.Path, x.Value })
                    .ToHtmlTable(new { @class = "pure-table pure-table-striped" }));
            }
            else
            {
                var photoMetaTags = ImageMetadataReader.ReadMetadata(loopFile.FullName);

                htmlParts.Add(photoMetaTags.SelectMany(x => x.Tags).OrderBy(x => x.DirectoryName).ThenBy(x => x.Name)
                    .ToList().Select(x => new
                    {
                        DataType = x.Type.ToString(),
                        x.DirectoryName,
                        Tag = x.Name,
                        TagValue = x.Description?.SafeObjectDump()
                    }).ToHtmlTable(new { @class = "pure-table pure-table-striped" }));

                var xmpDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName).OfType<XmpDirectory>()
                    .FirstOrDefault();

                var xmpMetadata = xmpDirectory?.GetXmpProperties()
                    .Select(x => new { XmpKey = x.Key, XmpValue = x.Value })
                    .ToHtmlTable(new { @class = "pure-table pure-table-striped" });

                if (!string.IsNullOrWhiteSpace(xmpMetadata)) htmlParts.Add(xmpMetadata);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"PhotoMetadata-{Path.GetFileNameWithoutExtension(loopFile.Name)}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                await
                    $"<h1>Metadata Report:</h1><h1>{HttpUtility.HtmlEncode(loopFile.FullName)}</h1><br><h1>Metadata</h1><br>{string.Join("<br><br>", htmlParts)}"
                        .ToHtmlDocumentWithPureCss("File Metadata", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
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

    public async Task SendResultFilesToFeatureIntersectTagger()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (WriteToFileResults?.FileResults == null || WriteToFileResults.FileResults.Count == 0)
        {
            StatusContext.ToastError("No Results to Send");
            return;
        }

        WeakReferenceMessenger.Default.Send(new FeatureIntersectFileAddRequestMessage((this,
            WriteToFileResults.FileResults.Select(x => x.FileName).ToList())));
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(Settings.ExifToolFullName)))
            StatusContext.RunNonBlockingTask(CheckThatExifToolExistsAndSaveSettings);
    }

    public async Task ShowSelectedGpxFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (GpxFileList?.SelectedFiles == null || !GpxFileList.SelectedFiles.Any())
        {
            StatusContext.ToastWarning("No gpx files selected?");
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

        var previewHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("GeoJson",
            32.12063, -110.52313, string.Empty);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newPreviewWindow = new WebViewWindow();
        newPreviewWindow.PositionWindowAndShow();
        newPreviewWindow.WindowTitle = "GPX Preview";
        newPreviewWindow.PreviewHtml = previewHtml;
        newPreviewWindow.PreviewGeoJsonDto = previewDto;
    }

    public async Task WriteResultsToFile()
    {
        await FileBasedGeoTaggerSettingTools.WriteSettings(Settings);

        if (PreviewResults?.FileResults == null || !PreviewResults.FileResults.Any() || !PreviewResults.FileResults.Any(x => x.ShouldWriteMetadata))
        {
            StatusContext.ToastError("No Results to Write");
            return;
        }

        var tagger = new GeoTag();

        WriteToFileResults = await tagger.WriteGeoTagActions(
            PreviewResults.FileResults.Where(x => x.ShouldWriteMetadata).ToList(),
            CreateBackups, CreateBackupsInDefaultStorage,
            Settings.ExifToolFullName, StatusContext.ProgressTracker());

        var writtenResults = WriteToFileResults.FileResults.Where(x => x.WroteMetadata).ToList();

        if (!writtenResults.Any())
        {
            WriteToFileGeoJsonDto = await ResetMapGeoJsonDto();
        }
        else
        {
            var features = new FeatureCollection();

            foreach (var loopResults in writtenResults)
                features.Add(new Feature(PointTools.Wgs84Point(loopResults.Longitude!.Value, loopResults.Latitude!.Value),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopResults.FileName }, { "description", $"From {loopResults.Source}" } })));

            var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

            var jsonDto = new GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
                new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

            WriteToFileGeoJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
        }

        SelectedTab = 4;
    }
}