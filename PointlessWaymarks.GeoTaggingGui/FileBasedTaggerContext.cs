using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using HtmlTableHelper;
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor;
using NetTopologySuite.Features;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.WpfHtml;
using static PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml.GeoJsonData;
using XmpCore;

namespace PointlessWaymarks.GeoTaggingGui;

[ObservableObject]
public partial class FileBasedTaggerContext
{
    [ObservableProperty] private bool _createBackups;

    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private ObservableCollection<FileInfo>? _filesToTag;
    [ObservableProperty] private ObservableCollection<FileInfo>? _filesToTagSelected;
    [ObservableProperty] private ObservableCollection<FileInfo>? _gpxFiles;
    [ObservableProperty] private ObservableCollection<FileInfo>? _gpxFilesSelected;
    [ObservableProperty] private GeoTag.GeoTagResult? _lastTagOutput;
    [ObservableProperty] private int _offsetPhotoTimeInMinutes;
    [ObservableProperty] private bool _overwriteExistingGeoLocation;
    [ObservableProperty] private int _pointsMustBeWithinMinutes = 10;
    [ObservableProperty] private string _previewGeoJsonDto;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private bool _testRunOnly;
    [ObservableProperty] private WindowIconStatus? _windowStatus;


    public FileBasedTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;

        AddFilesToTagFromDirectoryCommand = StatusContext.RunBlockingTaskCommand(AddFilesToTagFromDirectory);
        AddFilesToTagCommand = StatusContext.RunBlockingTaskCommand(AddFilesToTag);
        AddFilesToTagFromDirectoryAndSubdirectoriesCommand =
            StatusContext.RunBlockingTaskCommand(AddFilesToTagFromDirectoryAndSubdirectories);

        AddGpxFilesFromDirectoryCommand = StatusContext.RunBlockingTaskCommand(AddGpxFilesFromDirectory);
        AddGpxFilesCommand = StatusContext.RunBlockingTaskCommand(AddGpxFiles);
        AddGpxFilesFromDirectoryAndSubdirectoriesCommand =
            StatusContext.RunBlockingTaskCommand(AddGpxFilesFromDirectoryAndSubdirectories);

        MetadataForSelectedFilesToTagCommand = StatusContext.RunBlockingTaskCommand(MetadataForSelectedFilesToTag);

        TagCommand = StatusContext.RunBlockingTaskCommand(Tag);
    }

    public RelayCommand MetadataForSelectedFilesToTagCommand { get; set; }

    public RelayCommand AddFilesToTagCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryAndSubdirectoriesCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryCommand { get; set; }

    public RelayCommand AddGpxFilesCommand { get; set; }

    public RelayCommand AddGpxFilesFromDirectoryAndSubdirectoriesCommand { get; set; }

    public RelayCommand AddGpxFilesFromDirectoryCommand { get; set; }

    public RelayCommand TagCommand { get; set; }

    public async System.Threading.Tasks.Task AddFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await LastTaggingDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Title = "Add Files", Multiselect = true, CheckFileExists = true, ValidateNames = true };
        if (lastDirectory != null) filePicker.FileName = $"{lastDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        FilesToTag?.Clear();

        await WriteLastTaggingDirectorySetting(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()));

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !FilesToTag!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async System.Threading.Tasks.Task AddFilesToTagFromDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await LastTaggingDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = false };

        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        FilesToTag?.Clear();

        await WriteLastTaggingDirectorySetting(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x => !FilesToTag!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async System.Threading.Tasks.Task AddFilesToTagFromDirectoryAndSubdirectories()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await LastTaggingDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = false };
        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        FilesToTag?.Clear();

        await WriteLastTaggingDirectorySetting(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !FilesToTag!.Contains(x)).ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async System.Threading.Tasks.Task AddGpxFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await LastGpxDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();
        var filePicker = new VistaOpenFileDialog
        {
            Title = "Select Gpx Files", Multiselect = true, CheckFileExists = true, ValidateNames = true,
            DefaultExt = ".gpx"
        };
        if (lastDirectory != null) filePicker.FileName = $"{lastDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        GpxFiles?.Clear();

        if (!result ?? false) return;

        await WriteLastGpxDirectorySetting(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()));

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x =>
                x.Extension.Equals(".GPX", StringComparison.InvariantCultureIgnoreCase) && !GpxFiles!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => GpxFiles!.Add(x));
    }

    public async System.Threading.Tasks.Task AddGpxFilesFromDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await LastGpxDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Add gpx files in Directory", Multiselect = false };
        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";
        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        GpxFiles?.Clear();

        await WriteLastGpxDirectorySetting(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x =>
                x.Extension.Equals(".GPX", StringComparison.InvariantCultureIgnoreCase) && !GpxFiles!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => GpxFiles!.Add(x));
    }

    public async System.Threading.Tasks.Task AddGpxFilesFromDirectoryAndSubdirectories()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var lastDirectory = await LastGpxDirectory();

        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Add GPX Files in Directory And Subdirectories", Multiselect = false };
        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";
        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        GpxFiles?.Clear();

        await WriteLastGpxDirectorySetting(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Where(x =>
                x.Extension.Equals(".GPX", StringComparison.InvariantCultureIgnoreCase) && !GpxFiles!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => GpxFiles!.Add(x));
    }

    public static async Task<FileBasedTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new FileBasedTaggerContext(statusContext, windowStatus);
        await control.LoadData();
        return control;
    }

    public async Task<DirectoryInfo?> LastGpxDirectory()
    {
        var lastDirectory = (await SettingTools.ReadSettings()).GpxLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async Task<DirectoryInfo?> LastTaggingDirectory()
    {
        var lastDirectory = (await SettingTools.ReadSettings()).PhotosLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async System.Threading.Tasks.Task LoadData()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        FilesToTag = new ObservableCollection<FileInfo>();
        FilesToTagSelected = new ObservableCollection<FileInfo>();
        GpxFiles = new ObservableCollection<FileInfo>();
        GpxFilesSelected = new ObservableCollection<FileInfo>();

        ExifToolFullName = (await SettingTools.ReadSettings()).ExifToolFullName;

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("GeoJson",
            32.12063, -110.52313, string.Empty);
    }

    public async System.Threading.Tasks.Task Tag()
    {
        await WriteExifToolSetting(ExifToolFullName);

        var fileListGpxService = new FileListGpxService(GpxFiles!.ToList());
        var tagger = new GeoTag();
        LastTagOutput = await tagger.Tag(FilesToTag!.ToList(), new List<IGpxService> { fileListGpxService },
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
    }

    public async System.Threading.Tasks.Task MetadataForSelectedFilesToTag()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (FilesToTagSelected == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var frozenSelected = FilesToTagSelected.ToList();

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

            List<string> htmlParts = new List<string>();

            if (loopFile.Extension.Equals(".xmp", StringComparison.OrdinalIgnoreCase))
            {
                IXmpMeta xmp;
                await using (var stream = File.OpenRead(loopFile.FullName))
                    xmp = XmpMetaFactory.Parse(stream);

                htmlParts.Add(xmp.Properties.OrderBy(x => x.Namespace).ThenBy(x => x.Path).Select(x => new {x.Namespace, x.Path, x.Value}).ToHtmlTable(new { @class = "pure-table pure-table-striped" }));
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
                await ($"<h1>Metadata Report:</h1><h1>{HttpUtility.HtmlEncode(loopFile.FullName)}</h1><br><h1>Metadata</h1><br>{string.Join("<br><br>", htmlParts)}")
                .ToHtmlDocumentWithPureCss("File Metadata", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
    }

    public async System.Threading.Tasks.Task WriteExifToolSetting(string? newDirectory)
    {
        var settings = await SettingTools.ReadSettings();
        settings.ExifToolFullName = ExifToolFullName;
        await SettingTools.WriteSettings(settings);
    }

    public async System.Threading.Tasks.Task WriteLastGpxDirectorySetting(string? newDirectory)
    {
        var settings = await SettingTools.ReadSettings();
        settings.GpxLastDirectoryFullName = newDirectory ?? string.Empty;
        await SettingTools.WriteSettings(settings);
    }

    public async System.Threading.Tasks.Task WriteLastTaggingDirectorySetting(string? newDirectory)
    {
        var settings = await SettingTools.ReadSettings();
        settings.PhotosLastDirectoryFullName = newDirectory ?? string.Empty;
        await SettingTools.WriteSettings(settings);
    }
}