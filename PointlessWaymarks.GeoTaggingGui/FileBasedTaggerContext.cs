using System.Diagnostics;
using System.IO;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;
using static PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml.GeoJsonData;

namespace PointlessWaymarks.GeoTaggingGui;

[ObservableObject]
public partial class FileBasedTaggerContext
{
    [ObservableProperty] private bool _createBackups;

    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private FileListViewModel _filesToTagFileList;
    [ObservableProperty] private FileListViewModel _gpxFileList;
    [ObservableProperty] private GeoTag.GeoTagResult? _lastTagOutput;
    [ObservableProperty] private int _offsetPhotoTimeInMinutes;
    [ObservableProperty] private bool _overwriteExistingGeoLocation;
    [ObservableProperty] private int _pointsMustBeWithinMinutes = 10;
    [ObservableProperty] private string _previewGeoJsonDto;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private FilesToTagSettings _settingsFilesToTag;
    [ObservableProperty] private GpxFilesSettings _settingsGpxFiles;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private bool _testRunOnly;
    [ObservableProperty] private WindowIconStatus? _windowStatus;


    public FileBasedTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;

        SettingsFilesToTag = new FilesToTagSettings();
        SettingsGpxFiles = new GpxFilesSettings();

        MetadataForSelectedFilesToTagCommand = StatusContext.RunBlockingTaskCommand(MetadataForSelectedFilesToTag);
        ShowSelectedGpxFilesCommand = StatusContext.RunBlockingTaskCommand(ShowSelectedGpxFiles);

        TagCommand = StatusContext.RunBlockingTaskCommand(Tag);
    }

    public RelayCommand MetadataForSelectedFilesToTagCommand { get; set; }

    public RelayCommand ShowSelectedGpxFilesCommand { get; set; }

    public RelayCommand TagCommand { get; set; }


    public static async Task<FileBasedTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new FileBasedTaggerContext(statusContext, windowStatus);
        await control.LoadData();
        return control;
    }

    public async System.Threading.Tasks.Task LoadData()
    {
        FilesToTagFileList = await FileListViewModel.CreateInstance(StatusContext, SettingsFilesToTag,
            new List<ContextMenuItemData>
            {
                new() { ItemCommand = MetadataForSelectedFilesToTagCommand, ItemName = "Metadata Report for Selected" }
            });

        GpxFileList = await FileListViewModel.CreateInstance(StatusContext, SettingsGpxFiles,
            new List<ContextMenuItemData>
                { new() { ItemCommand = ShowSelectedGpxFilesCommand, ItemName = "Show  Selected" } });

        await ThreadSwitcher.ResumeForegroundAsync();

        ExifToolFullName = (await SettingTools.ReadSettings()).ExifToolFullName;

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("GeoJson",
            32.12063, -110.52313, string.Empty);
    }

    public async System.Threading.Tasks.Task MetadataForSelectedFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (FilesToTagFileList.SelectedFiles == null)
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

    public async System.Threading.Tasks.Task ShowSelectedGpxFiles()
    {
        if (GpxFileList.SelectedFiles == null || !GpxFileList.SelectedFiles.Any())
        {
            StatusContext.ToastWarning("No gpx files selected?");
            return;
        }

        var frozenSelected = GpxFileList.SelectedFiles.ToList();

        var featureList = new List<Feature>();
        var bounds = new Envelope();

        foreach (var loopFiles in frozenSelected)
        {
            var fileFeatures = await GpxTools.LinesFromGpxFile(loopFiles);
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

    public async System.Threading.Tasks.Task Tag()
    {
        await WriteExifToolSetting(ExifToolFullName);

        var fileListGpxService = new FileListGpxService(GpxFileList.Files!.ToList());
        var tagger = new GeoTag();
        LastTagOutput = await tagger.Tag(FilesToTagFileList.Files!.ToList(),
            new List<IGpxService> { fileListGpxService },
            TestRunOnly, CreateBackups,
            PointsMustBeWithinMinutes, OffsetPhotoTimeInMinutes, OverwriteExistingGeoLocation, ExifToolFullName,
            StatusContext.ProgressTracker());

        var resultsWithLocation =
            LastTagOutput.FileResults.Where(x => x.Latitude != null && x.Longitude != null).ToList();

        if (!resultsWithLocation.Any())
            //Todo: Blank/Clear GeoJson
            return;

        var features = new FeatureCollection();

        foreach (var loopResults in resultsWithLocation)
            features.Add(new Feature(PointTools.Wgs84Point(loopResults.Longitude.Value, loopResults.Latitude.Value),
                new AttributesTable(new Dictionary<string, object>
                    { { "title", loopResults.FileName }, { "description", $"From {loopResults.Source}" } })));

        //GeoJson Creation - ref the GeoJson control - boundaries?

        var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

        var jsonDto = new GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
            new SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

        PreviewGeoJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);

        SelectedTab = 3;
    }

    public async System.Threading.Tasks.Task WriteExifToolSetting(string? newDirectory)
    {
        var settings = await SettingTools.ReadSettings();
        settings.ExifToolFullName = ExifToolFullName;
        await SettingTools.WriteSettings(settings);
    }
}