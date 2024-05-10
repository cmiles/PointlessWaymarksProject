using System.Collections.ObjectModel;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class ProgressTrackerContext
{
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<ProgressTrackerListItem> Items { get; set; }
    public string JobName { get; init; } = string.Empty;
    public required Guid JobPersistentId { get; set; }
    public ProgressTrackerListItem? SelectedProgress { get; set; }
    public List<ProgressTrackerListItem> SelectedProgresses { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ProgressTrackerContext> CreateInstance(StatusControlContext statusContext,
        Guid jobPersistentId, string jobName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newItems = new ObservableCollection<ProgressTrackerListItem>();

        var context = new ProgressTrackerContext
        {
            Items = newItems,
            JobPersistentId = jobPersistentId,
            JobName = jobName,
            StatusContext = statusContext
        };

        context.Setup();

        return context;
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

    private async Task ProcessProgressNotification(InterProcessProgressNotification arg)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new ProgressTrackerListItem { ReceivedOn = DateTime.Now, Message = arg.ProgressMessage });
    }

    public void Setup()
    {
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }
}