using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using TinyIpc.Messaging;
using TypeSupport.Extensions;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptJobListListItem
{
    public ScriptJobListListItem()
    {
        DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ScriptJob DbEntry { get; set; }
    public required ObservableCollection<IPowerShellProgress> Items { get; set; }
    public ObservableCollection<ScriptJobRun> RecentRuns { get; set; } = [];
    public ScriptProgressMessageItem? SelectedItem { get; set; }
    public List<ScriptProgressMessageItem> SelectedItems { get; set; } = [];

    public static async Task<ScriptJobListListItem> CreateInstance(ScriptJob dbEntry)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var db = await PowerShellRunnerContext.CreateInstance();
        var recentRuns = await db.ScriptJobRuns
            .Where(x => x.ScriptJobId == dbEntry.Id)
            .OrderBy(x => x.CompletedOnUtc == null)
            .ThenByDescending(x => x.CompletedOnUtc)
            .Take(10)
            .ToListAsync();

        return new ScriptJobListListItem
        {
            DbEntry = dbEntry,
            RecentRuns = new ObservableCollection<ScriptJobRun>(recentRuns),
            Items = []
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
        if (arg.ScriptJobId != DbEntry.Id) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptProgressMessageItem()
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobId = arg.ScriptJobId, ScriptJobRunId = arg.ScriptJobRunId
        });

        if (Items.Count > 1200)
        {
            var toRemove = Items.OrderBy(x => x.ReceivedOn).Take(300).ToList();
            toRemove.ForEach(x => Items.Remove(x));
        }
    }

    private async Task ProcessStateNotification(DataNotifications.InterProcessPowershellStateNotification arg)
    {
        if (arg.ScriptJobId != DbEntry.Id) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptStateMessageItem()
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobId = arg.ScriptJobId, ScriptJobRunId = arg.ScriptJobRunId, State = arg.State
        });
    }
}