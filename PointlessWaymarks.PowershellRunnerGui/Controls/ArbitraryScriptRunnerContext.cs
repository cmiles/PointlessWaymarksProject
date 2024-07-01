using System.Collections.ObjectModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
[GenerateStatusCommands]
public partial class ArbitraryScriptRunnerContext
{
    private readonly int _scriptJobId = -888;
    private readonly int _scriptRunId = -999;

    public ArbitraryScriptRunnerContext()
    {
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<IPowerShellProgress> Items { get; set; }
    public bool ScriptRunning { get; set; }
    public IPowerShellProgress? SelectedItem { get; set; }
    public List<IPowerShellProgress> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }
    public required StringDataEntryNoIndicatorsContext UserScriptEntryContext { get; set; }

    public static async Task<ArbitraryScriptRunnerContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryScriptEntry = StringDataEntryNoIndicatorsContext.CreateInstance();
        factoryScriptEntry.Title = "PowerShell Script";
        factoryScriptEntry.HelpText =
            "Enter a PowerShell Script to run.";

        var factoryContext = new ArbitraryScriptRunnerContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            UserScriptEntryContext = factoryScriptEntry,
            Items = []
        };

        factoryContext.BuildCommands();

        factoryContext.DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = factoryContext.DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += factoryContext.OnDataNotificationReceived;

        return factoryContext;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(null,
            ProcessProgressNotification,
            ProcessStateNotification,
            x =>
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
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

    private async Task ProcessProgressNotification(DataNotifications.InterProcessPowershellProgressNotification arg)
    {
        if (arg.ScriptJobId != _scriptJobId || arg.ScriptJobRunId != _scriptRunId) return;

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
        if (arg.ScriptJobId != _scriptJobId || arg.ScriptJobRunId != _scriptRunId) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptStateMessageItem()
        {
            ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender,
            ScriptJobId = arg.ScriptJobId, ScriptJobRunId = arg.ScriptJobRunId, State = arg.State
        });
    }

    [NonBlockingCommand]
    public async Task RunScript()
    {
        if (string.IsNullOrWhiteSpace(UserScriptEntryContext.UserValue))
        {
            StatusContext.ToastError("No Script to Run?");
            return;
        }

        ScriptRunning = true;

        try
        {
            await PowerShellRun.ExecuteScript(UserScriptEntryContext.UserValue, _scriptJobId, _scriptRunId, "Arbitrary Script");
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