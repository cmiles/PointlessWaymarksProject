using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CloudBackupGui;

[ObservableObject]
public partial class MainWindow
{
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private ProgramUpdateMessageContext _updateMessageContext;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks Cloud Backup Beta");

        _infoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        _statusContext = new StatusControlContext { BlockUi = false };

        DataContext = this;

        _updateMessageContext = new ProgramUpdateMessageContext();


        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            await CheckForProgramUpdate(currentDateVersion);

            await LoadData();
        });
    }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = CloudBackupGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksCloudBackupSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task LoadData()
    {
        throw new NotImplementedException();
    }
}