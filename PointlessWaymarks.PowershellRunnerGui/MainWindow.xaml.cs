using System.ComponentModel;
using System.IO;
using System.Windows;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerGui.Controls;
using PointlessWaymarks.WpfCommon;
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
[GenerateStatusCommands]
[StaThreadConstructorGuard]
public partial class MainWindow
{
    private readonly PeriodicTimer _cronNextTimer = new(TimeSpan.FromSeconds(60));
    private Guid _dbId;
    private DateTime? _mainTimerLastCheck;
    private DateTime? _mostRecentScheduleCheckTime;
    private bool _windowCloseOk;

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

        StatusContext = new StatusControlContext { BlockUi = true };

        DataContext = this;

        UpdateMessageContext = new ProgramUpdateMessageContext(StatusContext);

        BuildCommands();

        StatusContext.RunFireAndForgetBlockingTask(async () => { await CheckForProgramUpdate(currentDateVersion); });

        DataNotificationsProcessor = new NotificationCatcher
        {
            RunCancelRequestNotification = ProcessCancelRequestForOrphans
        };
    }

    public ScriptJobRunListContext? AllRunListContext { get; set; }

    public CustomScriptRunnerContext? ArbitraryRunnerContext { get; set; }

    public CustomScriptRunnerContext CsArbitraryRunnerContext { get; set; }
    public string CurrentDatabase { get; set; } = string.Empty;
    public NotificationCatcher? DataNotificationsProcessor { get; set; }
    public ScriptJobRunListContext? ErrorRunListContext { get; set; }
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
                        PowerShellRunner.ExecuteJob(loopJobs.DbEntry.PersistentId,
                            loopJobs.DbEntry.AllowSimultaneousRuns, CurrentDatabase,
                            "Main Program Timer"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        var settings = PowerShellRunnerGuiSettingTools.ReadSettings();

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = await ProgramInfoTools.LatestInstaller(
            settings.ProgramUpdateDirectory,
            "PointlessWaymarks-PowerShellRunnerGui-Setup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {settings.ProgramUpdateDirectory}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile ?? string.Empty}");

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    [BlockingCommand]
    public async Task ChooseCurrentDb()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var initialDirectoryString = PowerShellRunnerGuiSettingTools.ReadSettings().LastDirectory;

        DirectoryInfo? initialDirectory = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(initialDirectoryString))
                initialDirectory = new DirectoryInfo(initialDirectoryString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting File Chooser");

        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Filter = "db files (*.db)|*.db|All files (*.*)|*.*" };

        if (initialDirectory != null) filePicker.FileName = $"{initialDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Checking that file exists");

        var possibleFile = new FileInfo(filePicker.FileName);

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!possibleFile.Exists) return;

        var currentSettings = PowerShellRunnerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(possibleFile.Directory?.Parent?.FullName))
            currentSettings.LastDirectory = possibleFile.Directory?.Parent?.FullName;
        currentSettings.DatabaseFile = possibleFile.FullName;
        await PowerShellRunnerGuiSettingTools.WriteSettings(currentSettings);
        CurrentDatabase = possibleFile.FullName;

        _dbId = await PowerShellRunnerDbQuery.DbId(CurrentDatabase);
        _ = PowerShellRunner.CleanUpOrphanRuns(CurrentDatabase, _dbId);

        JobListContext = await ScriptJobListContext.CreateInstance(StatusContext, CurrentDatabase);
        AllRunListContext = await ScriptJobRunListContext.CreateInstance(StatusContext, [], CurrentDatabase);
        ErrorRunListContext = await ScriptJobRunListContext.CreateInstance(StatusContext, [], CurrentDatabase,
            x => x.Errors, "Runs with Errors");
        ArbitraryRunnerContext =
            await CustomScriptRunnerContext.CreateInstance(ScriptType.PowerShell, null, CurrentDatabase);
        CsArbitraryRunnerContext =
            await CustomScriptRunnerContext.CreateInstance(ScriptType.CsScript, null, CurrentDatabase);
        ProgressContext = await ScriptProgressContext.CreateInstance(null, [], [], CurrentDatabase);
        SettingsContext = await AppSettingsContext.CreateInstance(StatusContext);
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

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_windowCloseOk)
        {
            e.Cancel = true;
            StatusContext.RunBlockingTask(OnCloseRequested);
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        StatusContext.RunBlockingTask(Setup);
    }

    [BlockingCommand]
    public async Task NewDatabase()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var initialDirectoryString = PowerShellRunnerGuiSettingTools.ReadSettings().LastDirectory;

        DirectoryInfo? initialDirectory = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(initialDirectoryString))
                initialDirectory = new DirectoryInfo(initialDirectoryString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting File Chooser");

        await ThreadSwitcher.ResumeForegroundAsync();

        var userFilePicker = new VistaSaveFileDialog
            { OverwritePrompt = true, CheckPathExists = true, Filter = "db files (*.db)|*.db|All files (*.*)|*.*" };

        if (initialDirectory != null) userFilePicker.FileName = $"{initialDirectory.FullName}\\";

        if (!userFilePicker.ShowDialog() ?? false) return;

        var userChoice = userFilePicker.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(userChoice))
        {
            await StatusContext.ToastWarning("No File Selected? New Db Cancelled...");
            return;
        }

        if (!Path.HasExtension(userChoice)) userChoice += ".db";

        var userDatabaseFile = new FileInfo(userChoice);

        if (!userDatabaseFile.Directory?.Exists ?? false)
        {
            await StatusContext.ToastError("Directory for New Database Doesn't Exist?");
            return;
        }

        var result = await PowerShellRunnerDbContext.TryCreateInstance(userDatabaseFile.FullName, true);

        if (!result.success)
        {
            await StatusContext.ToastError($"Trouble Creating New Database - {result.message}");
            return;
        }

        var currentSettings = PowerShellRunnerGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(userDatabaseFile.Directory?.Parent?.FullName))
            currentSettings.LastDirectory = userDatabaseFile.Directory?.Parent?.FullName;
        currentSettings.DatabaseFile = userDatabaseFile.FullName;
        await PowerShellRunnerGuiSettingTools.WriteSettings(currentSettings);
        CurrentDatabase = userDatabaseFile.FullName;

        _dbId = await PowerShellRunnerDbQuery.DbId(CurrentDatabase);
        _ = PowerShellRunner.CleanUpOrphanRuns(CurrentDatabase, _dbId);

        await ObfuscationKeyGuiHelpers.GetObfuscationKeyWithUserCreateAsNeeded(StatusContext, CurrentDatabase);

        JobListContext = await ScriptJobListContext.CreateInstance(StatusContext, CurrentDatabase);
        AllRunListContext = await ScriptJobRunListContext.CreateInstance(StatusContext, [], CurrentDatabase);
        ErrorRunListContext = await ScriptJobRunListContext.CreateInstance(StatusContext, [], CurrentDatabase,
            x => x.Errors, "Runs with Errors");
        ArbitraryRunnerContext =
            await CustomScriptRunnerContext.CreateInstance(ScriptType.PowerShell, null, CurrentDatabase);
        CsArbitraryRunnerContext =
            await CustomScriptRunnerContext.CreateInstance(ScriptType.CsScript, null, CurrentDatabase);
        ProgressContext = await ScriptProgressContext.CreateInstance(null, [], [], CurrentDatabase);
        SettingsContext = await AppSettingsContext.CreateInstance(StatusContext);
    }

    private async Task OnCloseRequested()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(CurrentDatabase);

        var openRuns = await db.ScriptJobRuns.Where(x => x.CompletedOnUtc == null).ToListAsync();

        if (!openRuns.Any())
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            _windowCloseOk = true;
            Close();
        }

        var openJobIds = openRuns.Select(x => x.ScriptJobPersistentId).Distinct().ToList();
        var openJobs = db.ScriptJobs.Where(x => openJobIds.Contains(x.PersistentId)).Select(x => x.Name).Distinct()
            .ToList();
        if (openJobIds.Any(x => x.ToString("N").StartsWith("000000000000"))) openJobs.Add("Custom Script");

        var continueClose = await StatusContext.ShowMessageWithYesNoButton("Running Jobs - Cancel and Close?",
            $"{openRuns.Count} Run{(openRuns.Count > 1 ? "s" : "")} still running - Job{(openRuns.Count > 1 ? "s" : "")}: {string.Join(", ", openJobs.OrderBy(x => x))}. {Environment.NewLine} {Environment.NewLine} Cancel Running Jobs and Close Window?");

        if (continueClose.Equals("no", StringComparison.OrdinalIgnoreCase)) return;

        var dbId = await db.DbId();

        foreach (var scriptJobRun in openRuns)
        {
            DataNotifications.PublishRunCancelRequest("Main Window Close", dbId, scriptJobRun.PersistentId);
            StatusContext.Progress($"Sending Cancel Message to {scriptJobRun.PersistentId}");
        }

        StatusContext.Progress("Waiting 5 seconds for Runs to stop...");
        await Task.Delay(5000);

        await ThreadSwitcher.ResumeForegroundAsync();
        _windowCloseOk = true;
        Close();
    }

    private async Task ProcessCancelRequestForOrphans(DataNotifications.InterProcessRunCancelRequest arg)
    {
        if (arg.DatabaseId != _dbId) return;

        await PowerShellRunner.CleanUpOrphanRuns(CurrentDatabase, _dbId);
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

        CurrentDatabase = settings.DatabaseFile;

        _dbId = await PowerShellRunnerDbQuery.DbId(CurrentDatabase);
        _ = PowerShellRunner.CleanUpOrphanRuns(CurrentDatabase, _dbId);

        await ObfuscationKeyGuiHelpers.GetObfuscationKeyWithUserCreateAsNeeded(StatusContext, CurrentDatabase);

        JobListContext = await ScriptJobListContext.CreateInstance(StatusContext, CurrentDatabase);
        AllRunListContext = await ScriptJobRunListContext.CreateInstance(StatusContext, [], CurrentDatabase);
        ErrorRunListContext = await ScriptJobRunListContext.CreateInstance(StatusContext, [], CurrentDatabase,
            x => x.Errors, "Runs with Errors");
        ArbitraryRunnerContext =
            await CustomScriptRunnerContext.CreateInstance(ScriptType.PowerShell, null, CurrentDatabase);
        CsArbitraryRunnerContext =
            await CustomScriptRunnerContext.CreateInstance(ScriptType.CsScript, null, CurrentDatabase);
        ProgressContext = await ScriptProgressContext.CreateInstance(null, [], [], CurrentDatabase);
        SettingsContext = await AppSettingsContext.CreateInstance(StatusContext);
        HelpContext = new HelpDisplayContext([
            HelpText,
            HelpMarkdown.CombinedAboutToolsAndPackages
        ]);

        _ = MainTimerCheckForNewRuns();

        StatusContext.RunFireAndForgetNonBlockingTask(async () =>
            await PowerShellRunnerDbQuery
                .DeleteScriptJobRunsBasedOnDeleteScriptJobRunsAfterMonthsSetting(CurrentDatabase));
    }
}