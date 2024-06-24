using System.IO;
using System.Windows;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerGui.Controls;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.PowerShellRunnerGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks PowerShell Runner Beta");

        InfoTitle = versionInfo.humanTitleString;

        var currentDateVersion = versionInfo.dateVersion;

        StatusContext = new StatusControlContext();

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext();

        StatusContext.RunFireAndForgetBlockingTask(async () => { await CheckForProgramUpdate(currentDateVersion); });
    }

    public ArbitraryScriptRunnerContext? ArbitraryRunnerContext { get; set; }

    public HelpDisplayContext? HelpContext { get; set; }

    public string HelpText => """
                              ## Pointless Waymarks PowerShell Runner

                              This program is designed to help you perform scheduled runs of PowerShell scripts.

                              """;

    public string InfoTitle { get; set; }

    public AppSettingsContext? SettingsContext { get; set; }

    public StatusControlContext StatusContext { get; set; }

    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = PowerShellRunnerGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarksPowerShellSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        StatusContext.RunFireAndForgetWithToastOnError(Setup);
    }

    public async Task Setup()
    {
        ArbitraryRunnerContext = await ArbitraryScriptRunnerContext.CreateInstance(null);
        SettingsContext = await AppSettingsContext.CreateInstance(null);
        HelpContext = new HelpDisplayContext([
            HelpText,
            HelpMarkdown.PointlessWaymarksAllProjectsQuickDescription,
            HelpMarkdown.SoftwareUsedBlock
        ]);

        var settings = PowerShellRunnerGuiSettingTools.ReadSettings();

        if (string.IsNullOrWhiteSpace(settings.DatabaseFile) || !File.Exists(settings.DatabaseFile))
        {
            var newDb = UniqueFileTools.UniqueFile(
                FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-PowerShellRunner.db");
            settings.DatabaseFile = newDb!.FullName;

            await PowerShellRunnerContext.CreateInstanceWithEnsureCreated(newDb.FullName);

            await PowerShellRunnerGuiSettingTools.WriteSettings(settings);
        }
        else
        {
            await PowerShellRunnerContext.CreateInstance(settings.DatabaseFile);
        }

        await ObfuscationKeyHelpers.GetObfuscationKeyWithUserCreateAsNeeded(StatusContext);
    }
}