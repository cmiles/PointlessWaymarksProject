using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Garmin.Connect.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ConnectDownloadContext
{
    public ConnectDownloadContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus ?? new WindowIconStatus();

        BuildCommands();

        SearchStartDate = DateTime.Now.AddDays(-30);
        SearchEndDate = DateTime.Now.AddDays(1).AddTicks(-1);
        Settings = new ConnectDownloadSettings();

        PropertyChanged += OnPropertyChanged;
    }

    public bool ArchiveDirectoryExists { get; set; }
    public string CurrentCredentialsNote { get; set; } = string.Empty;
    public string FilterLocation { get; set; } = string.Empty;
    public bool FilterMatchingArchiveFile { get; set; }
    public string FilterName { get; set; } = string.Empty;
    public bool FilterNoMatchingArchiveFile { get; set; }
    public Guid SearchAndFilterLatestRequestId { get; set; }
    public DateTime SearchEndDate { get; set; }
    public List<GarminActivityAndLocalFiles> SearchResults { get; set; } = new();
    public List<GarminActivityAndLocalFiles> SearchResultsFiltered { get; set; } = new();
    public DateTime SearchStartDate { get; set; }
    public ConnectDownloadSettings Settings { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus WindowStatus { get; set; }

    public async Task CheckThatArchiveDirectoryExists()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(Settings.ArchiveDirectory))
        {
            ArchiveDirectoryExists = false;
            return;
        }

        var exists = Directory.Exists(Settings.ArchiveDirectory.Trim());

        if (exists)
        {
            await ConnectDownloadSettingTools.WriteSettings(Settings);
            WeakReferenceMessenger.Default.Send(new ArchiveDirectoryUpdateMessage((this, Settings.ArchiveDirectory)));
        }

        ArchiveDirectoryExists = exists;
    }

    [BlockingCommand]
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

    public static async Task<ConnectDownloadContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new ConnectDownloadContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    [BlockingCommand]
    public async Task DownloadActivity(GarminActivityAndLocalFiles? toDownload)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toDownload == null)
        {
            StatusContext.ToastError("Null Archive Directory?");
            return;
        }

        await CheckThatArchiveDirectoryExists();

        if (!ArchiveDirectoryExists)
        {
            StatusContext.ToastError("The Archive Directory Must Exist to Download Activities...");
            return;
        }

        Settings.ArchiveDirectory = Settings.ArchiveDirectory;
        await ConnectDownloadSettingTools.WriteSettings(Settings);

        var credentials = GarminConnectCredentialTools.GetGarminConnectCredentials();

        if (string.IsNullOrWhiteSpace(credentials.userName) || string.IsNullOrWhiteSpace(credentials.password))
        {
            StatusContext.ToastError("Current Garmin Connect Credentials don't appear to be valid?");
            return;
        }

        var archiveDirectory = new DirectoryInfo(Settings.ArchiveDirectory.Trim());

        toDownload.ArchivedJson =
            await GarminConnectTools.WriteJsonActivityArchiveFile(toDownload.Activity, archiveDirectory, true);
        toDownload.ArchivedGpx = await GarminConnectTools.GetGpx(toDownload.Activity, archiveDirectory, false, true,
            new ConnectGpxService { ConnectUsername = credentials.userName, ConnectPassword = credentials.password },
            StatusContext.ProgressTracker());

        StatusContext.ToastSuccess($"Downloaded {toDownload.ArchivedJson.Name} {toDownload.ArchivedGpx?.Name}");
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


    public async Task FilterAndSortResults(Guid requestId)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        List<GarminActivityAndLocalFiles> returnResult;

        if (!SearchResults.Any())
        {
            returnResult = new List<GarminActivityAndLocalFiles>();
            if (requestId == SearchAndFilterLatestRequestId) SearchResultsFiltered = returnResult;
            return;
        }

        returnResult = SearchResults;

        if (FilterMatchingArchiveFile && !FilterNoMatchingArchiveFile)
            returnResult = returnResult.Where(x =>
                    x is { ArchivedGpx.Exists: true, ArchivedJson.Exists: true })
                .ToList();
        if (FilterNoMatchingArchiveFile && !FilterMatchingArchiveFile)
            returnResult = returnResult.Where(x => x.ArchivedGpx is not { Exists: true } || x.ArchivedJson is not
                {
                    Exists: true
                })
                .ToList();

        if (!string.IsNullOrWhiteSpace(FilterName))
            returnResult = returnResult
                .Where(x => x.Activity.ActivityName != null &&
                            x.Activity.ActivityName.Contains(FilterName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(FilterLocation))
            returnResult = returnResult
                .Where(x => x.Activity.LocationName != null &&
                            x.Activity.LocationName.Contains(FilterLocation, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (requestId == SearchAndFilterLatestRequestId) SearchResultsFiltered = returnResult;
    }

    private async Task Load()
    {
        SearchResults = new List<GarminActivityAndLocalFiles>();
        SearchResultsFiltered = SearchResults;

        Settings = await ConnectDownloadSettingTools.ReadSettings();

        await UpdateCredentialsNote();
        await CheckThatArchiveDirectoryExists();

        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.StartsWith("Filter"))
        {
            var thisRequest = Guid.NewGuid();
            SearchAndFilterLatestRequestId = thisRequest;
            StatusContext.RunNonBlockingTask(async () => await FilterAndSortResults(thisRequest));
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings.ArchiveDirectory))
            StatusContext.RunNonBlockingTask(CheckThatArchiveDirectoryExists);
    }

    [NonBlockingCommand]
    public async Task RemoveAllGarminCredentials()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        GarminConnectCredentialTools.RemoveGarminConnectCredentials();
        await UpdateCredentialsNote();
        StatusContext.ToastWarning("Removed any Garmin Connect Credentials!");
    }

    [BlockingCommand]
    public async Task SearchGarminConnect(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var credentials = GarminConnectCredentialTools.GetGarminConnectCredentials();
        var activities = await GarminConnectTools.Search(SearchStartDate, SearchEndDate,
            new ConnectGpxService { ConnectUsername = credentials.userName, ConnectPassword = credentials.password },
            cancellationToken, StatusContext.ProgressTracker());

        var returnList = activities.Select(x => new GarminActivityAndLocalFiles(x)).ToList();

        if (!string.IsNullOrWhiteSpace(Settings.ArchiveDirectory) && Directory.Exists(Settings.ArchiveDirectory))
            foreach (var loopActivities in returnList)
            {
                var loopActivityArchiveJsonFileName = GarminConnectTools.ArchiveJsonFileName(loopActivities.Activity);
                var loopActivityArchiveGpxFileName = GarminConnectTools.ArchiveGpxFileName(loopActivities.Activity);

                loopActivities.ArchivedGpx =
                    new FileInfo(Path.Combine(Settings.ArchiveDirectory, loopActivityArchiveGpxFileName));
                loopActivities.ArchivedJson =
                    new FileInfo(Path.Combine(Settings.ArchiveDirectory, loopActivityArchiveJsonFileName));
            }

        SearchResults = returnList;
        var filterRequest = Guid.NewGuid();
        SearchAndFilterLatestRequestId = filterRequest;
        await FilterAndSortResults(filterRequest);
    }

    [NonBlockingCommand]
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

    [NonBlockingCommand]
    public async Task ShowFileInExplorer(string? fileName)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            StatusContext.ToastError("No Filename to Show?");
            return;
        }

        if (!File.Exists(fileName.Trim()))
        {
            StatusContext.ToastError($"{fileName} does not exist?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();
        await ProcessHelpers.OpenExplorerWindowForFile(fileName.Trim());
    }

    [BlockingCommand]
    public async Task ShowGpxFile(GarminActivityAndLocalFiles? toShow)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toShow == null)
        {
            StatusContext.ToastError("Null Archive Directory?");
            return;
        }

        await CheckThatArchiveDirectoryExists();

        if (!ArchiveDirectoryExists)
        {
            StatusContext.ToastError("A valid Archive Directory must be set to show a GPX file...");
            return;
        }

        if (toShow.ArchivedGpx is not { Exists: true }) await DownloadActivity(toShow);

        if (toShow.ArchivedGpx is not { Exists: true })
        {
            StatusContext.ToastError("Could not find or download GPX file...");
            return;
        }

        var featureList = new List<Feature>();
        var bounds = new Envelope();

        var fileFeatures = await GpxTools.TrackLinesFromGpxFile(toShow.ArchivedGpx);
        bounds.ExpandToInclude(fileFeatures.boundingBox);
        featureList.AddRange(fileFeatures.features);

        var newCollection = new FeatureCollection();
        featureList.ForEach(x => newCollection.Add(x));

        var jsonDto = new GeoJsonData.GeoJsonSiteJsonData(Guid.NewGuid().ToString(),
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

    [NotifyPropertyChanged]
    public partial class GarminActivityAndLocalFiles
    {
        public GarminActivityAndLocalFiles(GarminActivity activity)
        {
            Activity = activity;
        }

        public GarminActivity Activity { get; set; }
        public FileInfo? ArchivedGpx { get; set; }
        public FileInfo? ArchivedJson { get; set; }
    }
}