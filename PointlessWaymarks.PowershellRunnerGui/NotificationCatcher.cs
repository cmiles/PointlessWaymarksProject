using PointlessWaymarks.CommonTools;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui;

public class NotificationCatcher
{
    public NotificationCatcher()
    {
        DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public Guid? DbId { get; set; } = Guid.Empty;

    public Func<DataNotifications.InterProcessJobDataNotification, Task> JobDataNotification { get; set; } =
        _ => Task.CompletedTask;

    public Func<DataNotifications.InterProcessRunDataNotification, Task> RunDataNotification { get; set; } =
        _ => Task.CompletedTask;

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }

    public Func<DataNotifications.InterProcessProcessingError, Task> ErrorNotification { get; set; } = x =>
    {
        Log.Error("Data Notification Failure. Error Note {0}.",
            x.ErrorMessage);
        return Task.CompletedTask;
    };

    public Func<DataNotifications.InterProcessPowershellProgressNotification, Task> ProgressNotification { get; set; } =
        _ => Task.CompletedTask;

    public Func<DataNotifications.InterProcessPowershellStateNotification, Task> StateNotification { get; set; } =
        _ => Task.CompletedTask;

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(JobDataNotification,
            RunDataNotification,
            ProgressNotification,
            StateNotification,
            ErrorNotification
        );

        if (toRun is not null) await toRun;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }
}