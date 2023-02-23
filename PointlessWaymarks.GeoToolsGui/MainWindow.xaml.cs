using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.GeoToolsGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
//Disabled for XAML Window
public partial class MainWindow
#pragma warning restore MVVMTK0033
{
    private readonly string _currentDateVersion;
    [ObservableProperty] private HelpDisplayContext? _aboutContext;
    [ObservableProperty] private ConnectBasedGeoTaggerContext? _connectGeoTaggerContext;
    [ObservableProperty] private FeatureIntersectTaggerContext? _featureIntersectContext;
    [ObservableProperty] private FileBasedGeoTaggerContext? _fileGeoTaggerContext;
    [ObservableProperty] private ConnectDownloadContext? _garminConnectDownloadContext;
    [ObservableProperty] private string _infoTitle = string.Empty;
    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private AppSettingsContext? _settingsContext;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private ProgramUpdateMessageContext _updateMessageContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
                "Pointless Waymarks GeoTools Beta");

        InfoTitle = versionInfo.humanTitleString;

        _currentDateVersion = versionInfo.dateVersion;

        DataContext = this;

        _statusContext = new StatusControlContext();

        _windowStatus = new WindowIconStatus();

        _updateMessageContext = new ProgramUpdateMessageContext();

        StatusContext.RunBlockingTask(LoadData);
    }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {GeoToolsGuiAppSettings.Default.ProgramUpdateLocation}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            GeoToolsGuiAppSettings.Default.ProgramUpdateLocation,
            "PointlessWaymarksGeoToolsSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {GeoToolsGuiAppSettings.Default.ProgramUpdateLocation}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task LoadData()
    {
        FileGeoTaggerContext = await FileBasedGeoTaggerContext.CreateInstance(StatusContext, WindowStatus);
        ConnectGeoTaggerContext = await ConnectBasedGeoTaggerContext.CreateInstance(StatusContext, WindowStatus);
        FeatureIntersectContext = await FeatureIntersectTaggerContext.CreateInstance(StatusContext, WindowStatus);
        GarminConnectDownloadContext = await ConnectDownloadContext.CreateInstance(StatusContext, WindowStatus);
        AboutContext = new HelpDisplayContext(new List<string>
            { HelpMarkdown.GeoToolsGeneralDescriptionBlock, HelpMarkdown.SoftwareUsedBlock });
        SettingsContext = new AppSettingsContext();

        await CheckForProgramUpdate(_currentDateVersion);


        WeakReferenceMessenger.Default.Register<ExifToolSettingsUpdateMessage>(this,  (_, m) =>
        {
            StatusContext.RunFireAndForgetNonBlockingTask(async () =>
            {
                await ThreadSwitcher.ResumeBackgroundAsync();

                if (ConnectGeoTaggerContext?.Settings != null)
                    ConnectGeoTaggerContext.Settings.ExifToolFullName = m.Value.exifToolFullName;
                if (FeatureIntersectContext?.Settings != null)
                    FeatureIntersectContext.Settings.ExifToolFullName = m.Value.exifToolFullName;
                if (FileGeoTaggerContext?.Settings != null)
                    FileGeoTaggerContext.Settings.ExifToolFullName = m.Value.exifToolFullName;
            });
        });

        WeakReferenceMessenger.Default.Register<ArchiveDirectoryUpdateMessage>(this,  (_, m) =>
        {
            StatusContext.RunFireAndForgetNonBlockingTask(async () =>
            {
                await ThreadSwitcher.ResumeBackgroundAsync();

                if (GarminConnectDownloadContext?.Settings != null)
                    GarminConnectDownloadContext.Settings.ArchiveDirectory = m.Value.archiveDirectory;
                if (ConnectGeoTaggerContext?.Settings != null)
                    ConnectGeoTaggerContext.Settings.ArchiveDirectory = m.Value.archiveDirectory;
            });
        });

        WeakReferenceMessenger.Default.Register<FeatureIntersectFileAddRequestMessage>(this, (_, m) =>
        {
            StatusContext.RunFireAndForgetNonBlockingTask(async () =>
            {
                await ThreadSwitcher.ResumeBackgroundAsync();

                if (FeatureIntersectContext?.FilesToTagFileList is null) return;

                await
                    FeatureIntersectContext.FilesToTagFileList.AddFilesToTag(m.Value.files);
                SelectedTab = 2;
                FeatureIntersectContext.SelectedTab = 0;
            });
        });
    }
}