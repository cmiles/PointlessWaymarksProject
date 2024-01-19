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
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.FileList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;
using XmpCore;
using Directory = System.IO.Directory;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ConnectBasedGeoTaggerContext
{
    public ConnectBasedGeoTaggerContext(StatusControlContext statusContext, WindowIconStatus? windowStatus)
    {
        StatusContext = statusContext;
        WindowStatus = windowStatus;

        BuildCommands();

        Settings = new ConnectBasedGeoTaggerSettings();

        PropertyChanged += OnPropertyChanged;
    }

    public bool ArchiveDirectoryExists { get; set; }
    public string CurrentCredentialsNote { get; set; } = string.Empty;
    public bool ExifToolExists { get; set; }
    public FileListContext? FilesToTagFileList { get; set; }
    public ConnectBasedGeoTagFilesToTagSettings? FilesToTagSettings { get; set; }
    public int OffsetPhotoTimeInMinutes { get; set; }
    public bool PreviewHasWritablePoints { get; set; }
    public WebViewMessenger PreviewMap { get; set; } = new();
    public GeoTag.GeoTagProduceActionsResult? PreviewResults { get; set; }
    public int SelectedTab { get; set; }
    public ConnectBasedGeoTaggerSettings Settings { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }
    public WebViewMessenger WriteMap { get; set; } = new();
    public GeoTag.GeoTagWriteMetadataToFilesResult? WriteToFileResults { get; set; }

    public async Task CheckThatArchiveDirectoryExists(bool writeSettings)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ArchiveDirectory))
        {
            ArchiveDirectoryExists = false;
            return;
        }

        var exists = Directory.Exists(Settings.ArchiveDirectory.Trim());

        if (exists && writeSettings)
        {
            await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);
            WeakReferenceMessenger.Default.Send(
                new ArchiveDirectoryUpdateMessage((this, Settings.ArchiveDirectory)));
        }

        ArchiveDirectoryExists = exists;
    }

    public async Task CheckThatExifToolExists(bool writeSettings)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ExifToolFullName))
        {
            ExifToolExists = false;
            return;
        }

        var exists = File.Exists(Settings.ExifToolFullName.Trim());

        if (exists && writeSettings)
        {
            await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);
            WeakReferenceMessenger.Default.Send(
                new ExifToolSettingsUpdateMessage((this, Settings.ExifToolFullName)));
        }

        ExifToolExists = exists;
    }

    [BlockingCommand]
    public async Task ChooseArchiveDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = false };

        Debug.Assert(Settings != null, nameof(GeoToolsGui.Settings) + " != null");

        if (!string.IsNullOrWhiteSpace(Settings.ArchiveDirectory))
        {
            var currentDirectory = new DirectoryInfo(Settings.ArchiveDirectory);
            if (currentDirectory.Exists) folderPicker.SelectedPath = $"{currentDirectory.FullName}\\";
        }

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        Settings.ArchiveDirectory = folderPicker.SelectedPath;
    }

    [BlockingCommand]
    public async Task ChooseExifFile()
    {
        Debug.Assert(Settings != null, nameof(GeoToolsGui.Settings) + " != null");

        var newFile = await ExifFilePicker.ChooseExifFile(StatusContext, Settings.ExifToolFullName);

        if (!newFile.validFileFound) return;

        if (Settings.ExifToolFullName.Equals(newFile.pickedFileName)) return;

        Settings.ExifToolFullName = newFile.pickedFileName;
    }

    public static async Task<ConnectBasedGeoTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryStatusContext = statusContext ?? new StatusControlContext();

        var control = new ConnectBasedGeoTaggerContext(factoryStatusContext, windowStatus);
        await control.Load();
        return control;
    }

    [BlockingCommand]
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

    [BlockingCommand]
    public async Task GeneratePreview()
    {
        Debug.Assert(Settings != null, nameof(GeoToolsGui.Settings) + " != null");
        await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);

        if (FilesToTagFileList?.Files == null || !FilesToTagFileList.Files.Any())
        {
            StatusContext.ToastError("No Files to Tag Selected?");
            return;
        }

        WriteToFileResults = null;
        WriteMap.ToWebView.Enqueue(JsonData.CreateRequest(await ResetMapGeoJsonDto()));

        var credentials = GarminConnectCredentialTools.GetGarminConnectCredentials();

        if (string.IsNullOrWhiteSpace(credentials.userName) || string.IsNullOrWhiteSpace(credentials.password))
        {
            StatusContext.ToastError("No valid Garmin Connect Credentials Found?");
            return;
        }

        Debug.Assert(Settings != null, nameof(GeoToolsGui.Settings) + " != null");

        if (string.IsNullOrWhiteSpace(Settings.ArchiveDirectory) ||
            !Directory.Exists(Settings.ArchiveDirectory))
        {
            StatusContext.ToastError($"Archive Directory {Settings.ArchiveDirectory} Not Found/Valid?");
            return;
        }

        var fileListGpxService =
            new GarminConnectGpxService(Settings.ArchiveDirectory,
                new ConnectGpxService
                    { ConnectUsername = credentials.userName, ConnectPassword = credentials.password });
        var tagger = new GeoTag();
        PreviewResults = await tagger.ProduceGeoTagActions(FilesToTagFileList.Files!.ToList(),
            [fileListGpxService],
            Settings.PointsMustBeWithinMinutes, OffsetPhotoTimeInMinutes,
            Settings.OverwriteExistingGeoLocation,
            StatusContext.ProgressTracker(),
            Settings.ExifToolFullName);

        var resultsWithLocation =
            PreviewResults.FileResults.Where(x => x is { Latitude: not null, Longitude: not null }).ToList();

        PreviewHasWritablePoints = resultsWithLocation.Any();

        if (PreviewHasWritablePoints)
        {
            var features = new FeatureCollection();

            var locationGroupedList = resultsWithLocation.GroupBy(x => new { x.Latitude, x.Longitude }).ToList();

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

        Settings = await ConnectBasedGeoTaggerSettingTools.ReadSettings();
        Settings.PropertyChanged += OnSettingsPropertyChanged;

        FilesToTagSettings = new ConnectBasedGeoTagFilesToTagSettings(this);

        FilesToTagFileList = await FileListContext.CreateInstance(StatusContext, FilesToTagSettings,
        [
            new ContextMenuItemData
                { ItemCommand = MetadataForSelectedFilesToTagCommand, ItemName = "Metadata Report for Selected" }
        ]);

        await ThreadSwitcher.ResumeForegroundAsync();

        Settings.ExifToolFullName = Settings.ExifToolFullName;

        PreviewMap.SetupCmsLeafletMapHtmlAndJs("Preview", 32.12063, -110.52313, string.Empty);
        WriteMap.SetupCmsLeafletMapHtmlAndJs("Write", 32.12063, -110.52313, string.Empty);

        await UpdateCredentialsNote();
        await CheckThatExifToolExists(false);
        await CheckThatArchiveDirectoryExists(false);
    }

    [BlockingCommand]
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

            var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
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

    [NonBlockingCommand]
    public async Task NextTab()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedTab++;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(GeoToolsGui.Settings))
        {
            StatusContext.RunNonBlockingTask(async () => await CheckThatArchiveDirectoryExists(false));
            StatusContext.RunNonBlockingTask(async () => await CheckThatExifToolExists(false));
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings.ArchiveDirectory))
            StatusContext.RunNonBlockingTask(async () => await CheckThatArchiveDirectoryExists(true));

        if (e.PropertyName == nameof(Settings.ExifToolFullName))
            StatusContext.RunNonBlockingTask(async () => await CheckThatExifToolExists(true));
    }

    [NonBlockingCommand]
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

        var jsonDto = await MapJson.NewMapFeatureCollectionDtoSerialized(features.AsList(),
            SpatialBounds.FromEnvelope(bounds));

        return await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
    }

    [NonBlockingCommand]
    public Task SendResultFilesToFeatureIntersectTagger()
    {
        if (WriteToFileResults?.FileResults == null || WriteToFileResults.FileResults.Count == 0)
        {
            StatusContext.ToastError("No Results to Send");
            return Task.CompletedTask;
        }

        WeakReferenceMessenger.Default.Send(new FeatureIntersectFileAddRequestMessage((this,
            WriteToFileResults.FileResults.Select(x => x.FileName).ToList())));

        return Task.CompletedTask;
    }

    [NonBlockingCommand]
    public async Task ShowArchiveDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await CheckThatArchiveDirectoryExists(false);

        if (!ArchiveDirectoryExists)
        {
            StatusContext.ToastError("Directory Does Not Exist - can not show...");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.Assert(Settings != null, nameof(GeoToolsGui.Settings) + " != null");

        await ProcessHelpers.OpenExplorerWindowForDirectory(Settings.ArchiveDirectory.Trim());
    }

    public async Task UpdateCredentialsNote()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentCredentials = GarminConnectCredentialTools.GetGarminConnectCredentials();

        if (!string.IsNullOrWhiteSpace(currentCredentials.userName) &&
            !string.IsNullOrWhiteSpace(currentCredentials.password))
            CurrentCredentialsNote = $"{currentCredentials.userName.Truncate(8)}...";
        else
            CurrentCredentialsNote = "No Credentials Found...";
    }

    [BlockingCommand]
    public async Task WriteResultsToFile()
    {
        await ConnectBasedGeoTaggerSettingTools.WriteSettings(Settings);

        if (PreviewResults == null)
        {
            StatusContext.ToastError("No Results to Write?");
            return;
        }

        var tagger = new GeoTag();

        WriteToFileResults = await tagger.WriteGeoTagActions(
            PreviewResults.FileResults.Where(x => x.ShouldWriteMetadata).ToList(),
            Settings.CreateBackups, Settings.CreateBackupsInDefaultStorage,
            Settings.ExifToolFullName, StatusContext.ProgressTracker());

        var writtenResults = WriteToFileResults.FileResults.Where(x => x.WroteMetadata).ToList();

        if (!writtenResults.Any())
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
}