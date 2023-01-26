using System.ComponentModel;
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
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;
using Directory = System.IO.Directory;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public partial class ConnectBasedGeoTaggerContext : ObservableObject
{
    [ObservableProperty] private bool _archiveDirectoryExists;
    [ObservableProperty] private string _currentCredentialsNote;
    [ObservableProperty] private FileListViewModel _filesToTagFileList;
    [ObservableProperty] private ConnectBasedGeoTagFilesToTagSettings _filesToTagSettings;
    [ObservableProperty] private int _offsetPhotoTimeInMinutes;
    [ObservableProperty] private string _previewGeoJsonDto;
    [ObservableProperty] private bool _previewHasWritablePoints;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private GeoTag.GeoTagProduceActionsResult? _previewResults;
    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private ConnectBasedGeoTaggerSettings _settings;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;
    [ObservableProperty] private string _writeToFileGeoJsonDto;
    [ObservableProperty] private string _writeToFileHtml;
    [ObservableProperty] private GeoTag.GeoTagWriteMetadataToFilesResult? _writeToFileResults;


    public ConnectBasedGeoTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;

        MetadataForSelectedFilesToTagCommand = StatusContext.RunBlockingTaskCommand(MetadataForSelectedFilesToTag);

        GeneratePreviewCommand = StatusContext.RunBlockingTaskCommand(GeneratePreview);
        WriteToFilesCommand = StatusContext.RunBlockingTaskCommand(WriteResultsToFile);
        EnterGarminCredentialsCommand = StatusContext.RunBlockingTaskCommand(EnterGarminCredentials);
        RemoveAllGarminCredentialsCommand = StatusContext.RunNonBlockingTaskCommand(RemoveAllGarminCredentials);
        ChooseArchiveDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChooseArchiveDirectory);
        ShowArchiveDirectoryCommand = StatusContext.RunNonBlockingTaskCommand(ShowArchiveDirectory);
        NextTabCommand = StatusContext.RunNonBlockingActionCommand(() => SelectedTab++);

        PropertyChanged += OnPropertyChanged;
    }

    public RelayCommand ChooseArchiveDirectoryCommand { get; set; }
    public RelayCommand EnterGarminCredentialsCommand { get; set; }
    public RelayCommand GeneratePreviewCommand { get; set; }
    public RelayCommand MetadataForSelectedFilesToTagCommand { get; set; }
    public RelayCommand NextTabCommand { get; set; }
    public RelayCommand RemoveAllGarminCredentialsCommand { get; set; }
    public RelayCommand ShowArchiveDirectoryCommand { get; set; }

    public RelayCommand ShowSelectedGpxFilesCommand { get; set; }
    public RelayCommand WriteToFilesCommand { get; set; }

    public async Task CheckThatArchiveDirectoryExists()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ArchiveDirectory))
        {
            ArchiveDirectoryExists = false;
            return;
        }

        ArchiveDirectoryExists = Directory.Exists(Settings.ArchiveDirectory.Trim());
    }

    public async Task ChooseArchiveDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = false };

        if (!string.IsNullOrWhiteSpace(Settings.ArchiveDirectory))
        {
            var currentDirectory = new DirectoryInfo(Settings.ArchiveDirectory);
            if (currentDirectory.Exists) folderPicker.SelectedPath = $"{currentDirectory.FullName}\\";
        }

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        Settings.ArchiveDirectory = folderPicker.SelectedPath;
    }

    public static async Task<ConnectBasedGeoTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new ConnectBasedGeoTaggerContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }


    public async Task EnterGarminCredentials()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newKeyEntry = await StatusContext.ShowStringEntry("Garmin Connect User Name",
            "Enter the Garmin Connect User Name", string.Empty);

        if (!newKeyEntry.Item1)
        {
            StatusContext.ToastWarning("Garmin Connect Credential Entry Cancelled");
            await UpdateCredentialsNote();
            return;
        }

        var cleanedKey = newKeyEntry.Item2.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedKey)) return;

        var newSecretEntry = await StatusContext.ShowStringEntry("Garmin Connect Password",
            "Enter the Garmin Connect Password", string.Empty);

        if (!newSecretEntry.Item1) return;

        var cleanedSecret = newSecretEntry.Item2.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedSecret))
        {
            StatusContext.ToastError("Garmin Connect Password Entry Cancelled - password can not be blank");
            await UpdateCredentialsNote();
            return;
        }

        GarminConnectCredentialTools.SaveGarminConnectCredentials(cleanedKey, cleanedSecret);
        await UpdateCredentialsNote();
    }

    public async Task GeneratePreview()
    {
        await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);

        if (FilesToTagFileList.Files == null || !FilesToTagFileList.Files.Any())
        {
            StatusContext.ToastError("No Files to Tag Selected?");
            return;
        }

        WriteToFileResults = null;
        WriteToFileGeoJsonDto = await ResetMapGeoJsonDto();

        var credentials = GarminConnectCredentialTools.GetGarminConnectCredentials();

        if (string.IsNullOrWhiteSpace(credentials.userName) || string.IsNullOrWhiteSpace(credentials.password))
        {
            StatusContext.ToastError("No valid Garmin Connect Credentials Found?");
            return;
        }

        if (string.IsNullOrWhiteSpace(Settings.ArchiveDirectory) || !Directory.Exists(Settings.ArchiveDirectory))
        {
            StatusContext.ToastError($"Archive Directory {Settings.ArchiveDirectory} Not Found/Valid?");
            return;
        }

        var fileListGpxService =
            new GarminConnectGpxService(Settings.ArchiveDirectory, credentials.userName, credentials.password);
        var tagger = new GeoTag();
        PreviewResults = await tagger.ProduceGeoTagActions(FilesToTagFileList.Files!.ToList(),
            new List<IGpxService> { fileListGpxService },
            Settings.PointsMustBeWithinMinutes, OffsetPhotoTimeInMinutes, Settings.OverwriteExistingGeoLocation,
            Settings.ExifToolFullName,
            StatusContext.ProgressTracker());

        var resultsWithLocation =
            PreviewResults.FileResults.Where(x => x is { Latitude: { }, Longitude: { } }).ToList();

        PreviewHasWritablePoints = resultsWithLocation.Any();

        if (PreviewHasWritablePoints)
        {
            var features = new FeatureCollection();

            foreach (var loopResults in resultsWithLocation)
                features.Add(new Feature(PointTools.Wgs84Point(loopResults.Longitude.Value, loopResults.Latitude.Value),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopResults.FileName }, { "description", $"From {loopResults.Source}" } })));

            var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

            var jsonDto = new GeoJsonData.GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
                new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

            PreviewGeoJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
        }

        SelectedTab = 3;
    }

    public async Task Load()
    {
        Settings = await ConnectBasedGeoTaggerSettingTools.ReadSettings();

        FilesToTagSettings = new ConnectBasedGeoTagFilesToTagSettings(this);

        FilesToTagFileList = await FileListViewModel.CreateInstance(StatusContext, FilesToTagSettings,
            new List<ContextMenuItemData>
            {
                new() { ItemCommand = MetadataForSelectedFilesToTagCommand, ItemName = "Metadata Report for Selected" }
            });

        await ThreadSwitcher.ResumeForegroundAsync();

        Settings.ExifToolFullName = (await ConnectBasedGeoTaggerSettingTools.ReadSettings()).ExifToolFullName;

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("Preview",
            32.12063, -110.52313, string.Empty);

        WriteToFileHtml = WpfHtmlDocument.ToHtmlLeafletBasicGeoJsonDocument("WrittenFiles",
            32.12063, -110.52313, string.Empty);

        await UpdateCredentialsNote();
        await CheckThatArchiveDirectoryExists();
    }

    public async Task MetadataForSelectedFilesToTag()
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


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e?.PropertyName)) return;

        if (e.PropertyName == nameof(Settings.ArchiveDirectory))
        {
            StatusContext.RunNonBlockingTask(async () => await CheckThatArchiveDirectoryExists());
            StatusContext.RunNonBlockingTask(async () =>
            {
                await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);
            });
        }
    }


    public async Task RemoveAllGarminCredentials()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        GarminConnectCredentialTools.RemoveGarminConnectCredentials();
        await UpdateCredentialsNote();
        StatusContext.ToastWarning("Removed any Garmin Connect Credentials!");
    }

    private async Task<string> ResetMapGeoJsonDto()
    {
        var features = new FeatureCollection();

        var basePoint = PointTools.Wgs84Point(-110.52313, 32.12063);
        var bounds = new Envelope();
        bounds.ExpandToInclude(basePoint.Coordinate);
        bounds.ExpandBy(1000);

        var jsonDto = new GeoJsonData.GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

        return await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
    }

    public async Task ShowArchiveDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await CheckThatArchiveDirectoryExists();

        if (!ArchiveDirectoryExists)
        {
            StatusContext.ToastError("Directory Does Not Exist - can not show...");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        await ProcessHelpers.OpenExplorerWindowForDirectory(Settings.ArchiveDirectory.Trim());
    }

    public async Task UpdateCredentialsNote()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentCredentials = GarminConnectCredentialTools.GetGarminConnectCredentials();

        if (!string.IsNullOrWhiteSpace(currentCredentials.userName) &&
            !string.IsNullOrWhiteSpace(currentCredentials.password))
            CurrentCredentialsNote = $"Using {currentCredentials.userName.Truncate(8)}...";
        else
            CurrentCredentialsNote = "No Credentials Found...";
    }

    public async Task WriteResultsToFile()
    {
        await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);

        var tagger = new GeoTag();

        WriteToFileResults = await tagger.WriteGeoTagActions(
            PreviewResults.FileResults.Where(x => x.ShouldWriteMetadata).ToList(),
            Settings.CreateBackups, Settings.CreateBackupsInDefaultStorage,
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
                features.Add(new Feature(PointTools.Wgs84Point(loopResults.Longitude.Value, loopResults.Latitude.Value),
                    new AttributesTable(new Dictionary<string, object>
                        { { "title", loopResults.FileName }, { "description", $"From {loopResults.Source}" } })));

            var bounds = GeoJsonTools.GeometryBoundingBox(features.Select(x => x.Geometry).ToList());

            var jsonDto = new GeoJsonData.GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
                new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), features);

            WriteToFileGeoJsonDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
        }

        SelectedTab = 4;
    }
}