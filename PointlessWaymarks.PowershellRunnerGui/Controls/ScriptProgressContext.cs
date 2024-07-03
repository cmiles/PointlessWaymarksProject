using System.Collections.ObjectModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptProgressContext
{
    private static string _databaseFile = string.Empty;
    private static Guid _dbId = Guid.Empty;

    public ScriptProgressContext()
    {
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<IPowerShellProgress> Items { get; set; }
    public List<Guid> ScriptJobIdFilter { get; set; } = [];
    public List<Guid> ScriptJobRunIdFilter { get; set; } = [];
    public IPowerShellProgress? SelectedItem { get; set; }
    public List<IPowerShellProgress> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ScriptProgressContext> CreateInstance(StatusControlContext? context,
        List<Guid> jobIdFilter, List<Guid> runIdFilter, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        _databaseFile = databaseFile;
        _dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = context ?? new StatusControlContext();

        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new ScriptProgressContext
        {
            StatusContext = factoryContext, Items = [], ScriptJobIdFilter = jobIdFilter,
            ScriptJobRunIdFilter = runIdFilter
        };
        DataNotifications.NewDataNotificationChannel().MessageReceived += toReturn.OnDataNotificationReceived;

        return toReturn;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(_ => Task.CompletedTask,
            ProcessProgressNotification,
            ProcessStateNotification,
            ProcessErrorNotification
        );

        if (toRun is not null) await toRun;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    private async Task ProcessErrorNotification(DataNotifications.InterProcessProcessingError arg)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptErrorMessageItem()
            { ReceivedOn = DateTime.Now, Message = arg.ErrorMessage });
    }

    private async Task ProcessProgressNotification(DataNotifications.InterProcessPowershellProgressNotification arg)
    {
        if (ScriptJobIdFilter.Any() && !ScriptJobIdFilter.Contains(arg.ScriptJobPersistentId)) return;
        if (ScriptJobRunIdFilter.Any() && !ScriptJobRunIdFilter.Contains(arg.ScriptJobRunPersistentId)) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptProgressMessageItem()
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId
        });

        if (Items.Count > 1200)
        {
            var toRemove = Items.OrderBy(x => x.ReceivedOn).Take(300).ToList();
            toRemove.ForEach(x => Items.Remove(x));
        }
    }

    private async Task ProcessStateNotification(DataNotifications.InterProcessPowershellStateNotification arg)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptStateMessageItem()
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobId, ScriptJobRunPersistentId = arg.ScriptJobRunId, State = arg.State
        });
    }
}