using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobListListItem
{
    private readonly int _numberOfRunsToShow = 5;
    public string DatabaseFile = string.Empty;
    public Guid DbId = Guid.Empty;
    public required string Key = string.Empty;

    public ScriptJobListListItem()
    {
        IpcNotifications = new NotificationCatcher
        {
            RunDataNotification = ProcessRunDataNotification, JobDataNotification = ProcessJobDataNotification,
            ProgressNotification = ProcessProgressNotification, StateNotification = ProcessStateNotification
        };
    }

    public string CronDescription { get; set; } = string.Empty;
    public required ScriptJob DbEntry { get; set; }
    public NotificationCatcher IpcNotifications { get; set; }
    public required ObservableCollection<ScriptJobRun> Items { get; set; } = [];
    public IScriptMessageItem? LastProgressItem { get; set; }
    public DateTime NextRun { get; set; } = DateTime.MaxValue;
    public required ScriptJobListContext Owner { get; set; }
    public ScriptJobRun? SelectedItem { get; set; }
    public List<ScriptJobRun> SelectedItems { get; set; } = [];
    public required string TranslatedScript { get; set; }

    public static async Task<ScriptJobListListItem> CreateInstance(ScriptJob dbEntry, ScriptJobListContext owner,
        string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var recentRuns = await db.ScriptJobRuns
            .Where(x => x.ScriptJobPersistentId == dbEntry.PersistentId)
            .OrderBy(x => x.CompletedOnUtc == null)
            .ThenByDescending(x => x.CompletedOnUtc)
            .Take(5)
            .ToListAsync();

        return new ScriptJobListListItem
        {
            DbEntry = dbEntry,
            Items = new ObservableCollection<ScriptJobRun>(recentRuns),
            DatabaseFile = databaseFile,
            DbId = dbId,
            Key = key,
            TranslatedScript = dbEntry.Script.Decrypt(key),
            Owner = owner
        };
    }

    private async Task ProcessJobDataNotification(DataNotifications.InterProcessJobDataNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != DbId || arg.JobPersistentId != DbEntry.PersistentId) return;

        if (arg.UpdateType == DataNotifications.DataNotificationUpdateType.Delete) return;

        var db = await PowerShellRunnerDbContext.CreateInstance(DatabaseFile);
        var newDbEntry = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == DbEntry.PersistentId);

        if (newDbEntry is null) return;

        DbEntry = newDbEntry;
        TranslatedScript = DbEntry.Script.Decrypt(Key);

        Owner.StatusContext.RunFireAndForgetNonBlockingTask(Owner.FilterList);
    }

    private async Task ProcessProgressNotification(DataNotifications.InterProcessPowershellProgressNotification arg)
    {
        if (arg.ScriptJobPersistentId != DbEntry.PersistentId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        LastProgressItem = new ScriptMessageItemProgress
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId
        };
    }

    private async Task ProcessRunDataNotification(DataNotifications.InterProcessRunDataNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != DbId || arg.JobPersistentId != DbEntry.PersistentId) return;

        if (arg.UpdateType == DataNotifications.DataNotificationUpdateType.Delete)
        {
            var toRemove = Items.Where(x => x.PersistentId == arg.RunPersistentId).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            foreach (var loopDeletes in toRemove) Items.Remove(loopDeletes);

            Owner.StatusContext.RunFireAndForgetNonBlockingTask(Owner.FilterList);

            return;
        }

        var db = await PowerShellRunnerDbContext.CreateInstance(DatabaseFile);
        var newDbEntry = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == arg.RunPersistentId);

        if (newDbEntry is null) return;

        if (Items.Any(x => x.PersistentId == newDbEntry.PersistentId))
        {
            var listItemToUpdate = Items.First(x => x.PersistentId == newDbEntry.PersistentId);

            await ThreadSwitcher.ResumeForegroundAsync();

            var index = Items.IndexOf(listItemToUpdate);
            Items[index] = newDbEntry;

            Owner.StatusContext.RunFireAndForgetNonBlockingTask(Owner.FilterList);

            return;
        }

        if (Items.Count < _numberOfRunsToShow)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Items.Add(newDbEntry);
            Owner.StatusContext.RunFireAndForgetNonBlockingTask(Owner.FilterList);
            return;
        }

        var currentMin = Items.MinBy(x => x.StartedOnUtc);
        if (newDbEntry.StartedOnUtc > currentMin!.StartedOnUtc)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Items.Remove(currentMin);
            Items.Add(newDbEntry);
            Owner.StatusContext.RunFireAndForgetNonBlockingTask(Owner.FilterList);
        }
    }

    private async Task ProcessStateNotification(DataNotifications.InterProcessPowershellStateNotification arg)
    {
        if (arg.ScriptJobPersistentId != DbEntry.PersistentId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        LastProgressItem = new ScriptMessageItemState
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId,
            State = arg.State
        };
    }
}