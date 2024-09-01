using CommunityToolkit.Mvvm.Messaging;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.GeoToolsGui.Messages;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.GeoToolsGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MainWindow
{
    private readonly string _currentDateVersion;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks GeoTools Beta");

        InfoTitle = versionInfo.humanTitleString;

        _currentDateVersion = versionInfo.dateVersion;

        DataContext = this;

        StatusContext = new StatusControlContext();

        WindowStatus = new WindowIconStatus();

        UpdateMessageContext = new ProgramUpdateMessageContext();

        StatusContext.RunBlockingTask(LoadData);
    }

    public HelpDisplayContext? AboutContext { get; set; }
    public ConnectBasedGeoTaggerContext? ConnectGeoTaggerContext { get; set; }
    public FeatureIntersectTaggerContext? FeatureIntersectContext { get; set; }
    public FileBasedGeoTaggerContext? FileGeoTaggerContext { get; set; }
    public ConnectDownloadContext? GarminConnectDownloadContext { get; set; }
    public string InfoTitle { get; set; }
    public int SelectedTab { get; set; }
    public AppSettingsContext? SettingsContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }
    public WindowIconStatus WindowStatus { get; set; }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {GeoToolsGuiSettingTools.ReadSettings().ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            GeoToolsGuiSettingTools.ReadSettings().ProgramUpdateDirectory,
            "PointlessWaymarksGeoToolsSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {GeoToolsGuiSettingTools.ReadSettings().ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

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
        AboutContext = new HelpDisplayContext([
            HelpMarkdown.CombinedAboutToolsAndPackages
        ]);
        SettingsContext = new AppSettingsContext();

        await CheckForProgramUpdate(_currentDateVersion);

        WeakReferenceMessenger.Default.Register<ExifToolSettingsUpdateMessage>(this, (_, m) =>
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

        WeakReferenceMessenger.Default.Register<ArchiveDirectoryUpdateMessage>(this, (_, m) =>
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