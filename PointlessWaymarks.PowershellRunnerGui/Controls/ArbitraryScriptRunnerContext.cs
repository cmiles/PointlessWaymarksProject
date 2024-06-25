using System.Collections.ObjectModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
[GenerateStatusCommands]
public partial class ArbitraryScriptRunnerContext
{
    private readonly int _runId = -999;
    private readonly int _scheduleId = -888;

    public ArbitraryScriptRunnerContext(StatusControlContext statusContext,
        ObservableCollection<ScriptProgressListItem> items)
    {
        StatusContext = statusContext;
        Items = items;

        BuildCommands();

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public ObservableCollection<ScriptProgressListItem> Items { get; set; }
    public bool ScriptRunning { get; set; }
    public ScriptProgressListItem? SelectedItem { get; set; }
    public List<ScriptProgressListItem> SelectedItems { get; set; } = [];
    public StatusControlContext StatusContext { get; set; }
    public string UserScript { get; set; } = string.Empty;

    public static async Task<ArbitraryScriptRunnerContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new ArbitraryScriptRunnerContext(statusContext ?? new StatusControlContext(), []);
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(null,
            ProcessProgressNotification,
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

    private async Task ProcessProgressNotification(DataNotifications.InterProcessProgressNotification arg)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ScriptProgressListItem
            { ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender });
    }

    [NonBlockingCommand]
    public async Task RunScript()
    {
        if (string.IsNullOrWhiteSpace(UserScript))
        {
            StatusContext.ToastError("No Script to Run?");
            return;
        }

        ScriptRunning = true;

        try
        {
            await PowerShellRun.Execute(UserScript, _scheduleId, _runId, "Arbitrary Script");
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