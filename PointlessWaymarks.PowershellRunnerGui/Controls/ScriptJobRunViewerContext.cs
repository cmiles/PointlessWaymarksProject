using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobRunViewerContext
{
    private string _databaseFile = string.Empty;
    public Guid _dbId = Guid.Empty;
    private string _key = string.Empty;
    public NotificationCatcher? DataNotificationsProcessor { get; set; }
    public ScriptJob? Job { get; set; }
    public ScriptJobRun? Run { get; set; }
    public ScriptJobRunGuiView? RunView { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ScriptJobRunViewerContext> CreateInstance(StatusControlContext? statusContext,
        Guid scriptJobRunId, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);
        var run = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == scriptJobRunId);

        if (run != null)
        {
            var jobId = run.ScriptJobPersistentId;
            var job = await db.ScriptJobs.SingleAsync(x => x.PersistentId == jobId);

            var toAdd = ScriptJobRunGuiView.CreateInstance(run, job, key);

            var factoryContext = new ScriptJobRunViewerContext
            {
                StatusContext = statusContext ?? new StatusControlContext(),
                Run = run,
                Job = job,
                RunView = toAdd,
                _key = key,
                _databaseFile = databaseFile,
                _dbId = dbId
            };

            factoryContext.DataNotificationsProcessor = new NotificationCatcher()
            {
                JobDataNotification = factoryContext.ProcessJobDataUpdateNotification,
                RunDataNotification = factoryContext.ProcessRunDataUpdateNotification
            };

            return factoryContext;
        }
        else
        {
            var toReturn = new ScriptJobRunViewerContext
            {
                StatusContext = statusContext ?? new StatusControlContext(),
                Run = null,
                Job = null,
                RunView = null
            };

            await toReturn.StatusContext.ShowMessageWithOkButton(
                $"Script Job Run PersistentId {scriptJobRunId} Not Found!",
                $"A Script Job Run with PersistentId {scriptJobRunId} was not found in {databaseFile}??");

            return toReturn;
        }
    }

    private async Task ProcessJobDataUpdateNotification(
        DataNotifications.InterProcessJobDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            interProcessUpdateNotification.JobPersistentId != Job?.PersistentId ||
            interProcessUpdateNotification.UpdateType == DataNotifications.DataNotificationUpdateType.Delete)
            return;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var updatedJob =
            await db.ScriptJobs.SingleOrDefaultAsync(x =>
                x.PersistentId == interProcessUpdateNotification.JobPersistentId);

        if (updatedJob is null) return;

        Job = updatedJob;

        if (RunView != null) RunView.Job = updatedJob;
    }

    private async Task ProcessRunDataUpdateNotification(
        DataNotifications.InterProcessRunDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            interProcessUpdateNotification.JobPersistentId != Job?.PersistentId ||
            interProcessUpdateNotification.RunPersistentId == Run?.PersistentId ||
            interProcessUpdateNotification.UpdateType == DataNotifications.DataNotificationUpdateType.Delete)
            return;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var updatedRun =
            await db.ScriptJobRuns.SingleOrDefaultAsync(x =>
                x.PersistentId == interProcessUpdateNotification.RunPersistentId);

        if (updatedRun is null) return;

        if (RunView is null) RunView = ScriptJobRunGuiView.CreateInstance(updatedRun, Job, _key);
        else RunView.Update(updatedRun, RunView.Job, _key);
    }
}