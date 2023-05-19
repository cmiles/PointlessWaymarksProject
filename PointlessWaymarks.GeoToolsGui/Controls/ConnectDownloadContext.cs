﻿using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Garmin.Connect.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public partial class ConnectDownloadContext : ObservableObject
{
    [ObservableProperty] private bool _archiveDirectoryExists;
    [ObservableProperty] private string _currentCredentialsNote = string.Empty;
    [ObservableProperty] private string _filterLocation = string.Empty;
    [ObservableProperty] private bool _filterMatchingArchiveFile;
    [ObservableProperty] private string _filterName = string.Empty;
    [ObservableProperty] private bool _filterNoMatchingArchiveFile;
    private Guid _searchAndFilterLatestRequestId;
    [ObservableProperty] private DateTime _searchEndDate;
    [ObservableProperty] private List<GarminActivityAndLocalFiles> _searchResults = new();
    [ObservableProperty] private List<GarminActivityAndLocalFiles> _searchResultsFiltered = new();
    [ObservableProperty] private DateTime _searchStartDate;
    [ObservableProperty] private ConnectDownloadSettings _settings;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public ConnectDownloadContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus ?? new WindowIconStatus();

        _searchStartDate = DateTime.Now.AddDays(-30);
        _searchEndDate = DateTime.Now.AddDays(1).AddTicks(-1);
        _settings = new ConnectDownloadSettings();

        RunSearchCommand = StatusContext.RunBlockingTaskWithCancellationCommand(RunSearch, "Cancel Search");
        EnterGarminCredentialsCommand = StatusContext.RunBlockingTaskCommand(EnterGarminCredentials);
        RemoveAllGarminCredentialsCommand = StatusContext.RunNonBlockingTaskCommand(RemoveAllGarminCredentials);
        ChooseArchiveDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChooseArchiveDirectory);
        DownloadActivityCommand = StatusContext.RunBlockingTaskCommand<GarminActivityAndLocalFiles>(async x => await DownloadActivity(x, StatusContext.ProgressTracker()));
        ShowGpxFileCommand = StatusContext.RunBlockingTaskCommand<GarminActivityAndLocalFiles>(ShowGpxFile);
        ShowFileInExplorerCommand = StatusContext.RunNonBlockingTaskCommand<string>(ShowFileInExplorer);

        ShowArchiveDirectoryCommand = StatusContext.RunNonBlockingTaskCommand(ShowArchiveDirectory);

        PropertyChanged += OnPropertyChanged;
    }

    public RelayCommand ChooseArchiveDirectoryCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global - Used in Xaml
    public RelayCommand<GarminActivityAndLocalFiles> DownloadActivityCommand { get; }

    public RelayCommand EnterGarminCredentialsCommand { get; }

    public RelayCommand RemoveAllGarminCredentialsCommand { get; }

    public RelayCommand RunSearchCommand { get; }

    public RelayCommand ShowArchiveDirectoryCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global - Used in Xaml
    public RelayCommand<string> ShowFileInExplorerCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global - Used in Xaml
    public RelayCommand<GarminActivityAndLocalFiles> ShowGpxFileCommand { get; }

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

    public async Task DownloadActivity(GarminActivityAndLocalFiles? toDownload, IProgress<string> progress)
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
            new ConnectGpxService { ConnectUsername = credentials.userName, ConnectPassword = credentials.password }, progress);

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

        if (requestId == _searchAndFilterLatestRequestId) SearchResultsFiltered = returnResult;
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
            _searchAndFilterLatestRequestId = thisRequest;
            StatusContext.RunNonBlockingTask(async () => await FilterAndSortResults(thisRequest));
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(Settings.ArchiveDirectory))
            StatusContext.RunNonBlockingTask(CheckThatArchiveDirectoryExists);
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

        if (toShow.ArchivedGpx is not { Exists: true }) await DownloadActivity(toShow, StatusContext.ProgressTracker());

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
            CurrentCredentialsNote = $"{currentCredentials.userName.Truncate(8)}...";
        else
            CurrentCredentialsNote = "No Credentials Found...";
    }

    public partial class GarminActivityAndLocalFiles : ObservableObject
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