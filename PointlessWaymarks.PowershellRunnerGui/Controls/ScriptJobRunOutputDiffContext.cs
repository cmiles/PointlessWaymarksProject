using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ScriptJobRunOutputDiffContext
{
    private const string RegexRemoveDateTimeStamp =
        @"^([1-9]|1[0-2])/([1-9]|[1-3]\d)/(1|2)\d\d\d ([1-9]|1[0-2]):([0-5])\d:([0-5])\d (AM|PM)>>";

    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;
    private Guid _jobId = Guid.Empty;
    private string _key = string.Empty;

    public ScriptJobRunOutputDiffContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public NotificationCatcher? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<ScriptJobRunGuiView> LeftRuns { get; set; }
    public object? LeftScrollItem { get; set; }
    public bool RemoveOutputTimeStamp { get; set; } = true;
    public required ObservableCollection<ScriptJobRunGuiView> RightRuns { get; set; }
    public object? RightScrollItem { get; set; }
    public ScriptJobRunGuiView? SelectedLeftRun { get; set; }
    public ScriptJobRunGuiView? SelectedRightRun { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     The initial left script job run must be specified and will be used to identify the Script Job to use, a right job
    ///     can
    ///     be specified or left null (if null a right job will be auto-selected).
    /// </summary>
    /// <param name="statusContext"></param>
    /// <param name="initialLeftScriptJobRun"></param>
    /// <param name="initialRightScript"></param>
    /// <param name="databaseFile"></param>
    /// <returns></returns>
    public static async Task<ScriptJobRunOutputDiffContext> CreateInstance(StatusControlContext? statusContext,
        Guid initialLeftScriptJobRun,
        Guid? initialRightScript, string databaseFile)
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
            var toAdd = ScriptJobRunToGuiView(loopRun, key, job, true);

            allRunsTranslated.Add(toAdd);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        var factoryLeftRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);
        var factoryRightRuns = new ObservableCollection<ScriptJobRunGuiView>(allRunsTranslated);

        var factorySelectedLeftRun = leftRun != null
            ? factoryLeftRuns.FirstOrDefault(x => x.PersistentId == leftRun.PersistentId)
            : null;

        ScriptJobRunGuiView? factorySelectedRightRun = null;
        if (jobId == Guid.Empty || leftRun == null || !factoryRightRuns.Any()) factorySelectedRightRun = null;
        else if (factoryRightRuns.Count == 1)
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

        var factoryContext = new ScriptJobRunOutputDiffContext
        {
            StatusContext = factoryStatusContext,
            LeftRuns = factoryLeftRuns,
            RightRuns = factoryRightRuns,
            SelectedLeftRun = factorySelectedLeftRun,
            SelectedRightRun = factorySelectedRightRun,
            _key = key,
            _databaseFile = databaseFile,
            _dbId = dbId,
            _jobId = jobId
        };

        factoryContext.BuildCommands();

        factoryContext.DataNotificationsProcessor = new NotificationCatcher
        {
            JobDataNotification = factoryContext.ProcessJobDataUpdateNotification,
            RunDataNotification = factoryContext.ProcessRunDataUpdateNotification
        };

        factoryContext.LeftScrollItem = factorySelectedLeftRun;
        factoryContext.RightScrollItem = factorySelectedRightRun;

        return factoryContext;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(RemoveOutputTimeStamp)))
            StatusContext.RunNonBlockingTask(ProcessTimeStampInclusionChange);
    }

    private async Task ProcessJobDataUpdateNotification(
        DataNotifications.InterProcessJobDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            interProcessUpdateNotification.JobPersistentId != _jobId) return;

        if (interProcessUpdateNotification.UpdateType == DataNotifications.DataNotificationUpdateType.Delete) return;

        var updatedJob = await (await PowerShellRunnerDbContext.CreateInstance(_databaseFile))
            .ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == interProcessUpdateNotification.JobPersistentId);

        if (updatedJob is null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopLeftRuns in LeftRuns) loopLeftRuns.Job = updatedJob;

        foreach (var loopRightRuns in RightRuns) loopRightRuns.Job = updatedJob;
    }

    private async Task ProcessRunDataUpdateNotification(
        DataNotifications.InterProcessRunDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            interProcessUpdateNotification.JobPersistentId != _jobId) return;

        if (interProcessUpdateNotification.UpdateType == DataNotifications.DataNotificationUpdateType.Delete)
        {
            var leftRunsToDelete = LeftRuns
                .Where(x => x.ScriptJobPersistentId == interProcessUpdateNotification.RunPersistentId).ToList();
            var rightRunsToDelete = RightRuns
                .Where(x => x.ScriptJobPersistentId == interProcessUpdateNotification.RunPersistentId).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();
            leftRunsToDelete.ForEach(x => LeftRuns.Remove(x));
            rightRunsToDelete.ForEach(x => RightRuns.Remove(x));

            return;
        }

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var newOrUpdatedRun =
            await db.ScriptJobRuns.SingleOrDefaultAsync(x =>
                x.PersistentId == interProcessUpdateNotification.RunPersistentId);

        if (newOrUpdatedRun is null) return;

        var job = await db.ScriptJobs.SingleOrDefaultAsync(x =>
            x.PersistentId == newOrUpdatedRun.ScriptJobPersistentId);

        if (job == null) return;

        var toAdd = ScriptJobRunToGuiView(newOrUpdatedRun, _key, job, RemoveOutputTimeStamp);

        var existingLeft = LeftRuns.SingleOrDefault(x => x.PersistentId == newOrUpdatedRun.PersistentId);
        var existingRight = RightRuns.SingleOrDefault(x => x.PersistentId == newOrUpdatedRun.PersistentId);

        await ThreadSwitcher.ResumeForegroundAsync();

        if (existingLeft is null)
            LeftRuns.Add(toAdd);
        else
            existingLeft.InjectFrom(toAdd);

        if (existingRight is null)
            RightRuns.Add(toAdd);
        else
            existingRight.InjectFrom(toAdd);
    }

    private async Task ProcessTimeStampInclusionChange()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopRun in LeftRuns)
        {
            loopRun.TranslatedOutput = loopRun.Output.Decrypt(_key);
            if (RemoveOutputTimeStamp)
                loopRun.TranslatedOutput = Regex.Replace(loopRun.TranslatedOutput, RegexRemoveDateTimeStamp,
                    string.Empty, RegexOptions.Multiline);
        }

        foreach (var loopRun in RightRuns)
        {
            loopRun.TranslatedOutput = loopRun.Output.Decrypt(_key);
            if (RemoveOutputTimeStamp)
                loopRun.TranslatedOutput = Regex.Replace(loopRun.TranslatedOutput, RegexRemoveDateTimeStamp,
                    string.Empty, RegexOptions.Multiline);
        }
    }

    private static ScriptJobRunGuiView ScriptJobRunToGuiView(ScriptJobRun loopRun, string key, ScriptJob job,
        bool removeOutputTimeStamp)
    {
        var toAdd = ScriptJobRunGuiView.CreateInstance(loopRun, job, key);

        if (removeOutputTimeStamp)
            toAdd.TranslatedOutput = Regex.Replace(toAdd.TranslatedOutput, RegexRemoveDateTimeStamp,
                string.Empty, RegexOptions.Multiline);

        return toAdd;
    }

    [NonBlockingCommand]
    public async Task ViewRun(ScriptJobRunGuiView? toView)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toView == null)
        {
            await StatusContext.ToastError("No Run Selected?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(toView.PersistentId, _databaseFile);
    }
}