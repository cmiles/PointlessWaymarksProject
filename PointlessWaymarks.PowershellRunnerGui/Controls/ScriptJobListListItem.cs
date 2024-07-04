using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptJobListListItem
{
    public string _databaseFile = string.Empty;
    public Guid _dbId = Guid.Empty;

    public ScriptJobListListItem()
    {
        DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ScriptJob DbEntry { get; set; }
    public required ObservableCollection<ScriptJobRun> Items { get; set; } = [];
    public IPowerShellProgress? LastProgressItem { get; set; }
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
            .Take(10)
            .ToListAsync();

        return new ScriptJobListListItem
        {
            DbEntry = dbEntry,
            Items = new ObservableCollection<ScriptJobRun>(recentRuns),
            _databaseFile = databaseFile,
            _dbId = dbId
        };
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(_ => Task.CompletedTask,
            ProcessProgressNotification,
            ProcessStateNotification,
            null
        );

        if (toRun is not null) await toRun;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
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

    private async Task ProcessStateNotification(DataNotifications.InterProcessPowershellStateNotification arg)
    {
        if (arg.ScriptJobPersistentId != DbEntry.PersistentId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        LastProgressItem = new ScriptStateMessageItem
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId, State = arg.State
        };
    }
}