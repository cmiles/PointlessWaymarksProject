using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobRunViewerContext
{
    private string _databaseFile = string.Empty;
    public Guid _dbId = Guid.Empty;
    private string _key = string.Empty;
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
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

            var toAdd = new ScriptJobRunGuiView
            {
                Id = run.Id,
                CompletedOnUtc = run.CompletedOnUtc,
                CompletedOn = run.CompletedOnUtc?.ToLocalTime(),
                Errors = run.Errors,
                Output = run.Output,
                RunType = run.RunType,
                Script = run.Script,
                StartedOnUtc = run.StartedOnUtc,
                StartedOn = run.StartedOnUtc.ToLocalTime(),
                ScriptJobPersistentId = run.ScriptJobPersistentId,
                TranslatedOutput = run.Output.Decrypt(key),
                TranslatedScript = run.Script.Decrypt(key),
                PersistentId = run.PersistentId,
                Job = job
            };

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

            factoryContext.DataNotificationsProcessor = new DataNotificationsWorkQueue
                { Processor = factoryContext.DataNotificationReceived };
            DataNotifications.NewDataNotificationChannel().MessageReceived += factoryContext.OnDataNotificationReceived;

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

            toReturn.StatusContext.ShowMessageWithOkButton($"Script Job Run PersistentId {scriptJobRunId} Not Found!",
                $"A Script Job Run with PersistentId {scriptJobRunId} was not found in {databaseFile}??");

            return toReturn;
        }
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

    private async Task ProcessDataUpdateNotification(
        DataNotifications.InterProcessDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId) return;
        if (interProcessUpdateNotification.ContentType == DataNotifications.DataNotificationContentType.ScriptJob &&
            interProcessUpdateNotification.PersistentId != RunView?.ScriptJobPersistentId) return;
        if (interProcessUpdateNotification.ContentType == DataNotifications.DataNotificationContentType.ScriptJobRun &&
            interProcessUpdateNotification.PersistentId != RunView?.PersistentId) return;

        //TODO: Process Data Update Notification
    }
}