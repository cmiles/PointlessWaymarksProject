using System.Collections.ObjectModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
[GenerateStatusCommands]
public partial class CustomScriptRunnerContext
{
    // ReSharper disable once NotAccessedField.Local
    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;

    public NotificationCatcher? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<IScriptMessageItem> Items { get; set; }
    public required Guid ScriptJobId { get; set; }
    public required Guid ScriptJobRunId { get; set; }
    public bool ScriptRunning { get; set; }
    public IScriptMessageItem? SelectedItem { get; set; }
    public List<IScriptMessageItem> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }
    public required StringDataEntryNoIndicatorsContext UserScriptEntryContext { get; set; }

    public static Guid ArbitraryScriptRunnerIdGuid()
    {
        var guidString = Guid.NewGuid().ToString("N");
        var modifiedGuidString = new string('0', 12) + guidString.Substring(12);
        var formattedGuidString =
            $"{modifiedGuidString.Substring(0, 8)}-{modifiedGuidString.Substring(8, 4)}-{modifiedGuidString.Substring(12, 4)}-{modifiedGuidString.Substring(16, 4)}-{modifiedGuidString.Substring(20, 12)}";

        var modifiedGuid = Guid.Parse(formattedGuidString);
        return modifiedGuid;
    }

    [NonBlockingCommand]
    public async Task CancelScript()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.PublishRunCancelRequest("Custom Script Runner", _dbId, ScriptJobRunId);
    }

    public static async Task<CustomScriptRunnerContext> CreateInstance(StatusControlContext? statusContext,
        string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryScriptEntry = StringDataEntryNoIndicatorsContext.CreateInstance();
        factoryScriptEntry.Title = "PowerShell Script";
        factoryScriptEntry.HelpText =
            "Enter a PowerShell Script to run.";

        var factoryContext = new CustomScriptRunnerContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            UserScriptEntryContext = factoryScriptEntry,
            Items = [],
            _databaseFile = databaseFile,
            _dbId = dbId,
            ScriptJobId = ArbitraryScriptRunnerIdGuid(),
            ScriptJobRunId = ArbitraryScriptRunnerIdGuid()
        };

        factoryContext.BuildCommands();

        factoryContext.DataNotificationsProcessor = new NotificationCatcher
        {
            ProgressNotification = factoryContext.ProcessProgressNotification,
            StateNotification = factoryContext.ProcessStateNotification
        };

        return factoryContext;
    }

    private async Task ProcessProgressNotification(DataNotifications.InterProcessPowershellProgressNotification arg)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (arg.ScriptJobPersistentId != ScriptJobId || arg.ScriptJobRunPersistentId != ScriptJobRunId ||
            arg.DatabaseId != _dbId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptMessageItemProgress
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
        if (arg.ScriptJobPersistentId != ScriptJobId || arg.ScriptJobRunPersistentId != ScriptJobRunId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptMessageItemState
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobPersistentId = arg.ScriptJobPersistentId, ScriptJobRunPersistentId = arg.ScriptJobRunPersistentId,
            State = arg.State
        });
    }

    [NonBlockingCommand]
    public async Task RunScript()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserScriptEntryContext.UserValue))
        {
            StatusContext.ToastError("No Script to Run?");
            return;
        }

        ScriptRunning = true;

        try
        {
            await PowerShellRunner.ExecuteScript(UserScriptEntryContext.UserValue, _dbId, ScriptJobId, ScriptJobRunId,
                "Custom Script Runner");
        }
        catch (Exception e)
        {
            await StatusContext.ShowMessageWithOkButton("Error Running Script", e.ToString());
        }
        finally
        {
            ScriptRunning = false;
        }
    }
}