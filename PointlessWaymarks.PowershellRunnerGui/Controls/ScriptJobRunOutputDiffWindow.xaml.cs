using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Shows a script and output diff view with a selection list of Script Job Runs for the left and right side of the
///     diff.
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
[GenerateStatusCommands]
public partial class ScriptJobRunOutputDiffWindow
{
    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;
    private Guid _jobId = Guid.Empty;
    private string _key = string.Empty;

    public ScriptJobRunOutputDiffWindow()
    {
        InitializeComponent();

        DataContext = this;

        PropertyChanged += OnPropertyChanged;
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<ScriptJobRunGuiView> LeftRuns { get; set; }
    public bool RemoveOutputTimeStamp { get; set; } = true;
    public required ObservableCollection<ScriptJobRunGuiView> RightRuns { get; set; }
    public ScriptJobRunGuiView? SelectedLeftRun { get; set; }
    public ScriptJobRunGuiView? SelectedRightRun { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     The initial left script job run must be specified and will be used to identify the Script Job to use, a right job
    ///     can
    ///     be specified or left null (if null a right job will be auto-selected).
    /// </summary>
    /// <param name="initialLeftScriptJobRun"></param>
    /// <param name="initialRightScript"></param>
    /// <param name="databaseFile"></param>
    /// <returns></returns>
    public static async Task CreateInstance(Guid initialLeftScriptJobRun, Guid? initialRightScript, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var leftRun = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == initialLeftScriptJobRun);
        var jobId = leftRun?.ScriptJobPersistentId ?? Guid.Empty;
        var job = db.ScriptJobs.Single(x => x.PersistentId == jobId);

        var allRuns = await db.ScriptJobRuns.Where(x => x.ScriptJobPersistentId == jobId)
            .OrderByDescending(x => x.CompletedOnUtc).AsNoTracking().ToListAsync();

        var allRunsTranslated = new List<ScriptJobRunGuiView>();

        foreach (var loopRun in allRuns)
        {
            var toAdd = new ScriptJobRunGuiView
            {
                Id = loopRun.Id,
                CompletedOnUtc = loopRun.CompletedOnUtc,
                CompletedOn = loopRun.CompletedOnUtc?.ToLocalTime(),
                Errors = loopRun.Errors,
                Output = loopRun.Output,
                PersistentId = loopRun.PersistentId,
                RunType = loopRun.RunType,
                Script = loopRun.Script,
                StartedOnUtc = loopRun.StartedOnUtc,
                StartedOn = loopRun.StartedOnUtc.ToLocalTime(),
                ScriptJobPersistentId = loopRun.ScriptJobPersistentId,
                TranslatedOutput = loopRun.Output.Decrypt(key),
                TranslatedScript = loopRun.Script.Decrypt(key),
                Job = job
            };

            toAdd.TranslatedOutput = Regex.Replace(toAdd.TranslatedOutput,
                @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                string.Empty, RegexOptions.Multiline);

            allRunsTranslated.Add(toAdd);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryLeftRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);
        var factoryRightRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);

        var factorySelectedLeftRun = leftRun != null
            ? factoryLeftRuns.FirstOrDefault(x => x.PersistentId == leftRun.PersistentId)
            : null;

        ScriptJobRunGuiView? factorySelectedRightRun = null;
        if (jobId == Guid.Empty || leftRun == null || !factoryRightRuns.Any()) factorySelectedRightRun = null;
        if (factoryRightRuns.Count == 1)
        {
            factorySelectedRightRun = factoryRightRuns.First();
        }
        else if (initialRightScript is null)
        {
            var previousRun = factoryRightRuns.Where(x => x.StartedOnUtc < leftRun.StartedOnUtc)
                .MaxBy(x => x.CompletedOnUtc);
            if (previousRun != null)
            {
                factorySelectedRightRun = previousRun;
            }
            else
            {
                var nextRun = factoryRightRuns
                    .Where(x => x.ScriptJobPersistentId == jobId && x.StartedOnUtc > leftRun.StartedOnUtc)
                    .MinBy(x => x.CompletedOnUtc);
                if (nextRun != null) factorySelectedRightRun = nextRun;
            }
        }
        else
        {
            factorySelectedRightRun = factoryRightRuns.SingleOrDefault(x => x.PersistentId == initialRightScript);
        }

        var factoryWindow = new ScriptJobRunOutputDiffWindow
        {
            StatusContext = new StatusControlContext(),
            LeftRuns = factoryLeftRuns,
            RightRuns = factoryRightRuns,
            SelectedLeftRun = factorySelectedLeftRun,
            SelectedRightRun = factorySelectedRightRun,
            _key = key,
            _databaseFile = databaseFile,
            _dbId = dbId,
            _jobId = jobId
        };

        factoryWindow.BuildCommands();

        factoryWindow.DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = factoryWindow.DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += factoryWindow.OnDataNotificationReceived;

        await factoryWindow.PositionWindowAndShowOnUiThread();
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(ProcessDataUpdateNotification,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            x =>
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context PersistentId {1}",
                    x.ErrorMessage,
                    StatusContext.StatusControlContextId);
                return Task.CompletedTask;
            }
        );

        if (toRun is not null) await toRun;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(RemoveOutputTimeStamp)))
            StatusContext.RunNonBlockingTask(ProcessTimeStampInclusionChange);
    }

    private async Task ProcessDataUpdateNotification(
        DataNotifications.InterProcessDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            (interProcessUpdateNotification.ContentType == DataNotifications.DataNotificationContentType.ScriptJob &&
             interProcessUpdateNotification.PersistentId != _jobId)) return;

        //TODO: Process Data Update Notification
    }

    private async Task ProcessTimeStampInclusionChange()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopRun in LeftRuns)
        {
            loopRun.TranslatedOutput = loopRun.Output.Decrypt(_key);
            if (RemoveOutputTimeStamp)
                loopRun.TranslatedOutput = Regex.Replace(loopRun.TranslatedOutput,
                    @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                    string.Empty, RegexOptions.Multiline);
        }

        foreach (var loopRun in RightRuns)
        {
            loopRun.TranslatedOutput = loopRun.Output.Decrypt(_key);
            if (RemoveOutputTimeStamp)
                loopRun.TranslatedOutput = Regex.Replace(loopRun.TranslatedOutput,
                    @"^([1-9]|1[0-2])/([1-9]|1[0-2])/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>",
                    string.Empty, RegexOptions.Multiline);
        }
    }

    [NonBlockingCommand]
    public async Task ViewRun(ScriptJobRunGuiView? toView)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toView == null)
        {
            StatusContext.ToastError("No Run Selected?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(toView.PersistentId, _databaseFile);
    }
}