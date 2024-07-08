using System.IO;
using System.Windows;
using Cronos;
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
    private readonly PeriodicTimer _cronNextTimer = new(TimeSpan.FromSeconds(60));
    private string _databaseFile = string.Empty;
    private DateTime? _mainTimerLastCheck;
    private DateTime? _mostRecentScheduleCheckTime;

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

    public ScriptJobListContext? JobListContext { get; set; }

    public ScriptProgressContext? ProgressContext { get; set; }

    public AppSettingsContext? SettingsContext { get; set; }

    public StatusControlContext StatusContext { get; set; }

    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }

    private void CheckAndRunJobsBasedOnCronExpression()
    {
        var frozenNow = DateTime.Now;
        if (frozenNow <= _mostRecentScheduleCheckTime) return;
        else _mostRecentScheduleCheckTime = frozenNow;

        //No main timer tick yet - set it and wait
        if (_mainTimerLastCheck is null)
        {
            _mainTimerLastCheck = frozenNow;
            return;
        }

        //We are behind the main tick? Don't worry about it and wait for the next tick...
        if (frozenNow <= _mainTimerLastCheck) return;
        if (frozenNow.Year == _mainTimerLastCheck.Value.Year &&
            frozenNow.Month == _mainTimerLastCheck.Value.Month &&
            frozenNow.Day == _mainTimerLastCheck.Value.Day &&
            frozenNow.Hour == _mainTimerLastCheck.Value.Hour &&
            frozenNow.Minute == _mainTimerLastCheck.Value.Minute) return;

        if (JobListContext is null) return;
        var jobs = JobListContext.Items.ToList();

        var frozenUtcOffset = new DateTimeOffset(frozenNow.AddMinutes(-1));

        foreach (var loopJobs in jobs)
        {
            if (!loopJobs.DbEntry.ScheduleEnabled ||
                string.IsNullOrWhiteSpace(loopJobs.DbEntry.CronExpression)) continue;

            try
            {
                var expression = CronExpression.Parse(loopJobs.DbEntry.CronExpression);
                var nextRun = expression.GetNextOccurrence(frozenUtcOffset, TimeZoneInfo.Local);
                if (nextRun is null) continue;
                var nextRunDateTime = nextRun.Value.DateTime;

                if (nextRunDateTime.Year == frozenNow.Year && nextRunDateTime.Month == frozenNow.Month &&
                    nextRunDateTime.Day == frozenNow.Day &&
                    nextRunDateTime.Hour == frozenNow.Hour && nextRunDateTime.Minute == frozenNow.Minute)
                    StatusContext.RunFireAndForgetNonBlockingTask(() =>
                        PowerShellRunner.ExecuteJob(loopJobs.DbEntry.PersistentId, _databaseFile,
                            "Main Program Timer"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }
        }
    }

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

    private async Task MainTimerCheckForNewRuns()
    {
        try
        {
            while (await _cronNextTimer.WaitForNextTickAsync())
                CheckAndRunJobsBasedOnCronExpression();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        StatusContext.RunFireAndForgetWithToastOnError(Setup);
    }

    public async Task Setup()
    {
        var settings = PowerShellRunnerGuiSettingTools.ReadSettings();

        if (string.IsNullOrWhiteSpace(settings.DatabaseFile) || !File.Exists(settings.DatabaseFile))
        {
            var newDb = UniqueFileTools.UniqueFile(
                FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-PowerShellRunner.db");
            settings.DatabaseFile = newDb!.FullName;

            await PowerShellRunnerDbContext.CreateInstanceWithEnsureCreated(newDb.FullName);

            await PowerShellRunnerGuiSettingTools.WriteSettings(settings);
        }
        else
        {
            await PowerShellRunnerDbContext.CreateInstanceWithEnsureCreated(settings.DatabaseFile);
        }

        _databaseFile = settings.DatabaseFile;

        await ObfuscationKeyGuiHelpers.GetObfuscationKeyWithUserCreateAsNeeded(StatusContext, _databaseFile);

        JobListContext = await ScriptJobListContext.CreateInstance(StatusContext, settings.DatabaseFile);
        ArbitraryRunnerContext = await ArbitraryScriptRunnerContext.CreateInstance(null, _databaseFile);
        ProgressContext = await ScriptProgressContext.CreateInstance(null, [], [], _databaseFile);
        SettingsContext = await AppSettingsContext.CreateInstance(null);
        HelpContext = new HelpDisplayContext([
            HelpText,
            HelpMarkdown.PointlessWaymarksAllProjectsQuickDescription,
            HelpMarkdown.SoftwareUsedBlock
        ]);

        MainTimerCheckForNewRuns();
    }
}