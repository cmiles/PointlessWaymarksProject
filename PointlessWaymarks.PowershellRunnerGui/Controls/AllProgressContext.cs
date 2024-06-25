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
public partial class AllProgressContext
{
    public AllProgressContext()
    {
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<AllProgressListItem> Items { get; set; }
    public ArbitraryScriptRunnerProgressListItem? SelectedProgress { get; set; }
    public List<ArbitraryScriptRunnerProgressListItem> SelectedProgresses { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<AllProgressContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var toReturn = new AllProgressContext
        {
            StatusContext = statusContext ?? new StatusControlContext(), Items = []
        };

        toReturn.DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = toReturn.DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += toReturn.OnDataNotificationReceived;

        return toReturn;
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

        Items.Add(new AllProgressListItem()
            { ReceivedOn = DateTime.Now, Message = arg.ProgressMessage, Sender = arg.Sender });

        if (Items.Count > 1200)
        {
            var toRemove = Items.OrderBy(x => x.ReceivedOn).Take(300).ToList();
            toRemove.ForEach(x => Items.Remove(x));
        }
    }
}