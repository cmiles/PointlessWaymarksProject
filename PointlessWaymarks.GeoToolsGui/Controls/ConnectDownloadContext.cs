using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Garmin.Connect.Models;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Task = System.Threading.Tasks.Task;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[ObservableObject]
public partial class ConnectDownloadContext
{
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private DateTime _searchStartDate;
    [ObservableProperty] private DateTime _searchEndDate;
    [ObservableProperty] private string _archiveDirectory;
    [ObservableProperty] private List<GarminActivityAndLocalFiles> _searchResults = new();
    [ObservableProperty] private List<GarminActivityAndLocalFiles> _searchResultsFiltered = new();
    [ObservableProperty] private string _currentCredentialsNote;
    [ObservableProperty] private bool _archiveDirectoryExists;
    [ObservableProperty] private bool _filterNoMatchingArchiveFile;
    [ObservableProperty] private bool _filterMatchingArchiveFile;
    [ObservableProperty] private string _filterName;
    [ObservableProperty] private string _filterLocation;
    private Guid _searchAndFilterLatestRequestId;

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


        PropertyChanged += OnPropertyChanged;
    }

    public RelayCommand<GarminActivityAndLocalFiles> DownloadActivityCommand { get; set; }

    public RelayCommand ChooseArchiveDirectoryCommand { get; set; }

    public RelayCommand RemoveAllGarminCredentialsCommand { get; set; }

    public async System.Threading.Tasks.Task ChooseArchiveDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = false };

        if (!string.IsNullOrWhiteSpace(ArchiveDirectory))
        {
            var currentDirectory = new DirectoryInfo(ArchiveDirectory);
            if (currentDirectory.Exists) folderPicker.SelectedPath = $"{currentDirectory.FullName}\\";
        }

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        ArchiveDirectory = folderPicker.SelectedPath;
    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e?.PropertyName)) return;

        if (e.PropertyName.StartsWith("Filter"))
        {
            var thisRequest = Guid.NewGuid();
            _searchAndFilterLatestRequestId = thisRequest;
            StatusContext.RunNonBlockingTask(async () => await FilterAndSortResults(thisRequest));
        }

        if (e.PropertyName == nameof(ArchiveDirectory))
        {
            CheckThatArchiveDirectoryExists();
        }
    }

    public void CheckThatArchiveDirectoryExists()
    {
        if (string.IsNullOrWhiteSpace(ArchiveDirectory))
        {
            ArchiveDirectoryExists = false;
            return;
        }

        ArchiveDirectoryExists = Directory.Exists(ArchiveDirectory.Trim());
    }

    public RelayCommand EnterGarminCredentialsCommand { get; set; }

    public RelayCommand RunSearchCommand { get; set; }

    public static async Task<ConnectDownloadContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new ConnectDownloadContext(statusContext, windowStatus);
        await control.Load();
        return control;
    }

    private async System.Threading.Tasks.Task Load()
    {
        SearchResults = new List<GarminActivityAndLocalFiles>();
        SearchResultsFiltered = SearchResults;

        await UpdateCredentialsNote();
    }

    public async System.Threading.Tasks.Task UpdateCredentialsNote()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentCredentials = GarminConnectCredentials.GetGarminConnectCredentials();

        if (!string.IsNullOrWhiteSpace(currentCredentials.userName) &&
            !string.IsNullOrWhiteSpace(currentCredentials.password))
        {
            CurrentCredentialsNote = $"(Using {currentCredentials.userName.Truncate(8)}...)";
        }
        else
        {
            CurrentCredentialsNote = "(No Credentials Found...)";
        }
    }

    [ObservableObject]
    public partial class GarminActivityAndLocalFiles
    {
        public GarminActivityAndLocalFiles(GarminActivity activity)
        {
            _activity = activity;
        }

        [ObservableProperty] private GarminActivity _activity;
        [ObservableProperty] private FileInfo? _archivedJson;
        [ObservableProperty] private FileInfo? _archivedGpx;
    }

    public async System.Threading.Tasks.Task RunSearch(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var credentials = GarminConnectCredentials.GetGarminConnectCredentials();
        var activities = await GarminConnectTools.Search(SearchStartDate, SearchEndDate, credentials.userName,
            credentials.password, cancellationToken, StatusContext.ProgressTracker());

        var returnList = activities.Select(x => new GarminActivityAndLocalFiles(x)).ToList();

        if (!string.IsNullOrWhiteSpace(_archiveDirectory) && Directory.Exists(_archiveDirectory))
        {
            foreach (var loopActivities in returnList)
            {
                var loopActivityArchiveJsonFileName = GarminConnectTools.ArchiveJsonFileName(loopActivities.Activity);
                var loopActivityArchiveGpxFileName = GarminConnectTools.ArchiveJsonFileName(loopActivities.Activity);

                loopActivities.ArchivedGpx =
                    new FileInfo(Path.Combine(_archiveDirectory, loopActivityArchiveGpxFileName));
                loopActivities.ArchivedJson =
                    new FileInfo(Path.Combine(_archiveDirectory, loopActivityArchiveJsonFileName));
            }
        }

        SearchResults = returnList;
        var filterRequest = Guid.NewGuid();
        _searchAndFilterLatestRequestId = filterRequest;
        await FilterAndSortResults(filterRequest);
    }

    public async System.Threading.Tasks.Task DownloadActivity(GarminActivityAndLocalFiles toDownload)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        CheckThatArchiveDirectoryExists();

        if (!ArchiveDirectoryExists)
        {
            StatusContext.ToastError("The Archive Directory Must Exist to Download Activities...");
            return;
        }

        var credentials = GarminConnectCredentials.GetGarminConnectCredentials();

        if (string.IsNullOrWhiteSpace(credentials.userName) || string.IsNullOrWhiteSpace(credentials.password))
        {
            StatusContext.ToastError("Current Garmin Connect Credentials don't appear to be valid?");
            return;
        }

        var archiveDirectory = new DirectoryInfo(ArchiveDirectory.Trim());

        toDownload.ArchivedJson =
            await GarminConnectTools.WriteJsonActivityArchiveFile(toDownload.Activity, archiveDirectory, true);
        toDownload.ArchivedGpx = await GarminConnectTools.GetGpx(toDownload.Activity, archiveDirectory, true,
            credentials.userName,
            credentials.password);

        StatusContext.ToastSuccess($"Downloaded {toDownload.ArchivedJson.Name} {toDownload.ArchivedGpx?.Name}");
    }

    public async System.Threading.Tasks.Task EnterGarminCredentials()
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

        GarminConnectCredentials.SaveGarminConnectCredentials(cleanedKey, cleanedSecret);
        await UpdateCredentialsNote();
    }

    public async System.Threading.Tasks.Task RemoveAllGarminCredentials()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        GarminConnectCredentials.RemoveGarminConnectCredentials();
        await UpdateCredentialsNote();
        StatusContext.ToastWarning("Removed any Garmin Connect Credentials!");
    }


    public async System.Threading.Tasks.Task FilterAndSortResults(Guid requestId)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        List<GarminActivityAndLocalFiles> returnResult;

        if (!SearchResults.Any())
        {
            returnResult = new();
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
        {
            returnResult = returnResult
                .Where(x => x.Activity.ActivityName.Contains(FilterName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(FilterLocation))
        {
            returnResult = returnResult
                .Where(x => x.Activity.LocationName.Contains(FilterLocation, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (requestId == _searchAndFilterLatestRequestId) SearchResultsFiltered = returnResult;
    }
}