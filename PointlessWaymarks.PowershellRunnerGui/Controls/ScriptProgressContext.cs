using System.Collections.ObjectModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
[StaThreadConstructorGuard]
public partial class ScriptProgressContext
{
    private static string _databaseFile = string.Empty;
    private static Guid _dbId = Guid.Empty;

    public ScriptProgressContext()
    {
    }

    public NotificationCatcher? DataNotificationsProcessor { get; set; }
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

        var factoryStatusContext = context ?? new StatusControlContext();

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = new ScriptProgressContext
        {
            StatusContext = factoryStatusContext, Items = [], ScriptJobIdFilter = jobIdFilter,
            ScriptJobRunIdFilter = runIdFilter
        };

        factoryContext.BuildCommands();

        factoryContext.DataNotificationsProcessor = new NotificationCatcher
        {
            ProgressNotification = factoryContext.ProcessProgressNotification,
            StateNotification = factoryContext.ProcessStateNotification,
            ErrorNotification = factoryContext.ProcessErrorNotification
        };

        return factoryContext;
    }

    private async Task ProcessErrorNotification(DataNotifications.InterProcessProcessingError arg)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptErrorMessageItem { ReceivedOn = DateTime.Now, Message = arg.ErrorMessage });
    }

    private async Task ProcessProgressNotification(DataNotifications.InterProcessPowershellProgressNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != _dbId) return;
        if (ScriptJobIdFilter.Any() && !ScriptJobIdFilter.Contains(arg.ScriptJobPersistentId)) return;
        if (ScriptJobRunIdFilter.Any() && !ScriptJobRunIdFilter.Contains(arg.ScriptJobRunPersistentId)) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptProgressMessageItem
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
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.DatabaseId != _dbId) return;
        if (ScriptJobIdFilter.Any() && !ScriptJobIdFilter.Contains(arg.ScriptJobPersistentId)) return;
        if (ScriptJobRunIdFilter.Any() && !ScriptJobRunIdFilter.Contains(arg.ScriptJobRunPersistentId)) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptStateMessageItem
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId,
            State = arg.State
        });
    }

    [NonBlockingCommand]
    public async Task ViewScriptRun(Guid? runPersistentId)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (runPersistentId == null)
        {
            StatusContext.ToastError("No Run Id Provided?");
            return;
        }

        if (runPersistentId.Value.ToString("N").StartsWith("000000000000"))
        {
            StatusContext.ToastError("Progress from Arbitrary Script Run - No Run/Job to Show.");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(runPersistentId.Value, _databaseFile);
    }
}