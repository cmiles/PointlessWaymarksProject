#region

using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garmin.Connect.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Controls;

[ObservableObject]
public partial class ConnectDownloadContext
{
    [ObservableProperty] private bool _archiveDirectoryExists;
    [ObservableProperty] private string _currentCredentialsNote;
    [ObservableProperty] private string _filterLocation;
    [ObservableProperty] private bool _filterMatchingArchiveFile;
    [ObservableProperty] private string _filterName;
    [ObservableProperty] private bool _filterNoMatchingArchiveFile;
    private Guid _searchAndFilterLatestRequestId;
    [ObservableProperty] private DateTime _searchEndDate;
    [ObservableProperty] private List<GarminActivityAndLocalFiles> _searchResults = new();
    [ObservableProperty] private List<GarminActivityAndLocalFiles> _searchResultsFiltered = new();
    [ObservableProperty] private DateTime _searchStartDate;
    [ObservableProperty] private ConnectDownloadSettings _settings;
    [ObservableProperty] private StatusControlContext _statusContext;

    public ConnectDownloadContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        windowStatus = windowStatus ?? new WindowIconStatus();

        _searchStartDate = DateTime.Now.AddDays(-30);
        _searchEndDate = DateTime.Now.AddDays(1).AddTicks(-1);

        RunSearchCommand = StatusContext.RunBlockingTaskWithCancellationCommand(RunSearch, "Cancel Search");
        EnterGarminCredentialsCommand = StatusContext.RunBlockingTaskCommand(EnterGarminCredentials);
        RemoveAllGarminCredentialsCommand = StatusContext.RunNonBlockingTaskCommand(RemoveAllGarminCredentials);
        ChooseArchiveDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChooseArchiveDirectory);
        DownloadActivityCommand = StatusContext.RunBlockingTaskCommand<GarminActivityAndLocalFiles>(DownloadActivity);
        ShowGpxFileCommand = StatusContext.RunBlockingTaskCommand<GarminActivityAndLocalFiles>(ShowGpxFile);
        ShowFileInExplorerCommand = StatusContext.RunNonBlockingTaskCommand<string>(ShowFileInExplorer);

        ShowArchiveDirectoryCommand = StatusContext.RunNonBlockingTaskCommand(ShowArchiveDirectory);


        PropertyChanged += OnPropertyChanged;
    }

    public RelayCommand ChooseArchiveDirectoryCommand { get; set; }

    public RelayCommand<GarminActivityAndLocalFiles> DownloadActivityCommand { get; set; }

    public RelayCommand EnterGarminCredentialsCommand { get; set; }

    public RelayCommand RemoveAllGarminCredentialsCommand { get; set; }

    public RelayCommand RunSearchCommand { get; set; }

    public RelayCommand ShowArchiveDirectoryCommand { get; set; }

    public RelayCommand<string> ShowFileInExplorerCommand { get; set; }

    public RelayCommand<GarminActivityAndLocalFiles> ShowGpxFileCommand { get; set; }

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

    public static async Task<ConnectDownloadContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new ConnectDownloadContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    public async Task DownloadActivity(GarminActivityAndLocalFiles toDownload)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await CheckThatArchiveDirectoryExists();

        if (!ArchiveDirectoryExists)
        {
            StatusContext.ToastError("The Archive Directory Must Exist to Download Activities...");
            return;
        }

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
            credentials.userName,
            credentials.password);

        StatusContext.ToastSuccess($"Downloaded {toDownload.ArchivedJson.Name} {toDownload.ArchivedGpx?.Name}");
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


    public async Task FilterAndSortResults(Guid requestId)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        List<GarminActivityAndLocalFiles> returnResult;

        if (!SearchResults.Any())
        {
            returnResult = new List<GarminActivityAndLocalFiles>();
            if (requestId == _searchAndFilterLatestRequestId) SearchResultsFiltered = returnResult;
            return;
        }

        returnResult = SearchResults;

        if (FilterMatchingArchiveFile && !FilterNoMatchingArchiveFile)
            returnResult = returnResult.Where(x =>
                    x is { ArchivedGpx: { Exists: true }, ArchivedJson.Exists: true })
                .ToList();
        if (FilterNoMatchingArchiveFile && !FilterMatchingArchiveFile)
            returnResult = returnResult.Where(x => x.ArchivedGpx is not { Exists: true } || x.ArchivedJson is not
                {
                    Exists: true
                })
                .ToList();

        if (!string.IsNullOrWhiteSpace(FilterName))
            returnResult = returnResult
                .Where(x => x.Activity.ActivityName != null && x.Activity.ActivityName.Contains(FilterName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(FilterLocation))
            returnResult = returnResult
                .Where(x => x.Activity.LocationName != null && x.Activity.LocationName.Contains(FilterLocation, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (requestId == _searchAndFilterLatestRequestId) SearchResultsFiltered = returnResult;
    }

    private async Task Load()
    {
        SearchResults = new List<GarminActivityAndLocalFiles>();
        SearchResultsFiltered = SearchResults;

        Settings = await ConnectDownloadSettingTools.ReadSettings();

        await UpdateCredentialsNote();
        await CheckThatArchiveDirectoryExists();
    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.StartsWith("Filter"))
        {
            var thisRequest = Guid.NewGuid();
            _searchAndFilterLatestRequestId = thisRequest;
            StatusContext.RunNonBlockingTask(async () => await FilterAndSortResults(thisRequest));
        }

        if (e.PropertyName == nameof(Settings.ArchiveDirectory))
        {
            StatusContext.RunNonBlockingTask(async () => await CheckThatArchiveDirectoryExists());
            StatusContext.RunNonBlockingTask(async () =>
            {
                var settings = await ConnectDownloadSettingTools.ReadSettings();
                settings.ArchiveDirectory = Settings.ArchiveDirectory;
                await ConnectDownloadSettingTools.WriteSettings(settings);
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

    public async Task RunSearch(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var credentials = GarminConnectCredentialTools.GetGarminConnectCredentials();
        var activities = await GarminConnectTools.Search(SearchStartDate, SearchEndDate, credentials.userName,
            credentials.password, cancellationToken, StatusContext.ProgressTracker());

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
        _searchAndFilterLatestRequestId = filterRequest;
        await FilterAndSortResults(filterRequest);
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

    public async Task ShowFileInExplorer(string fileName)
    {
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

    public async Task ShowGpxFile(GarminActivityAndLocalFiles toShow)
    {
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
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX), newCollection);

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
            CurrentCredentialsNote = $"(Using {currentCredentials.userName.Truncate(8)}...)";
        else
            CurrentCredentialsNote = "(No Credentials Found...)";
    }

    [ObservableObject]
    public partial class GarminActivityAndLocalFiles
    {
        [ObservableProperty] private GarminActivity _activity;
        [ObservableProperty] private FileInfo? _archivedGpx;
        [ObservableProperty] private FileInfo? _archivedJson;

        public GarminActivityAndLocalFiles(GarminActivity activity)
        {
            _activity = activity;
        }
    }
}