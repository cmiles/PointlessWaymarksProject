using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptJobListListItem
{
    private readonly int _numberOfRunsToShow = 5;
    public string _databaseFile = string.Empty;
    public Guid _dbId = Guid.Empty;

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
    public IPowerShellProgress? LastProgressItem { get; set; }
    public DateTime? NextRun { get; set; }
    public ScriptJobRun? SelectedItem { get; set; }
    public List<ScriptJobRun> SelectedItems { get; set; } = [];

    public static async Task<ScriptJobListListItem> CreateInstance(ScriptJob dbEntry, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

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
            _databaseFile = databaseFile,
            _dbId = dbId
        };
    }

    private async Task ProcessJobDataNotification(DataNotifications.InterProcessJobDataNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != _dbId || arg.JobPersistentId != DbEntry.PersistentId) return;

        if (arg.UpdateType == DataNotifications.DataNotificationUpdateType.Delete) return;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var newDbEntry = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == DbEntry.PersistentId);

        if (newDbEntry is null) return;

        DbEntry = newDbEntry;
    }

    private async Task ProcessProgressNotification(DataNotifications.InterProcessPowershellProgressNotification arg)
    {
        if (arg.ScriptJobPersistentId != DbEntry.PersistentId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        LastProgressItem = new ScriptProgressMessageItem
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId
        };
    }

    private async Task ProcessRunDataNotification(DataNotifications.InterProcessRunDataNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != _dbId || arg.JobPersistentId != DbEntry.PersistentId) return;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var newDbEntry = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == DbEntry.PersistentId);

        if (newDbEntry is null) return;

        if (Items.Count < _numberOfRunsToShow)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Items.Add(newDbEntry);
            return;
        }

        var currentMin = Items.MinBy(x => x.StartedOnUtc);
        if (newDbEntry.StartedOnUtc > currentMin!.StartedOnUtc)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Items.Remove(currentMin);
            Items.Add(newDbEntry);
        }
    }

    private async Task ProcessStateNotification(DataNotifications.InterProcessPowershellStateNotification arg)
    {
        if (arg.ScriptJobPersistentId != DbEntry.PersistentId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        LastProgressItem = new ScriptStateMessageItem
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId,
            State = arg.State
        };
    }
}