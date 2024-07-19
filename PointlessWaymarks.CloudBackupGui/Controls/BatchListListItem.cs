using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class BatchListListItem
{
    private DateTime? _latestStatisticsRefresh;

    private BatchListListItem()
    {
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required BatchStatistics Statistics { get; set; }

    public static async Task<BatchListListItem> CreateInstance(CloudTransferBatch batch)
    {
        var newItem = new BatchListListItem { Statistics = await BatchStatistics.CreateInstance(batch.Id) };

        return newItem;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(ProcessDataUpdateNotification,
            x => Task.CompletedTask,
            x => Task.CompletedTask
        );

        if (toRun is not null) await toRun;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.JobPersistentId != Statistics.JobPersistentId) return;
        if (interProcessUpdateNotification.BatchId != Statistics.BatchId) return;

        if (interProcessUpdateNotification is { ContentType: DataNotificationContentType.CloudTransferBatch })
            await RefreshStatistics();

        if (interProcessUpdateNotification is { ContentType: DataNotificationContentType.CloudUpload } or
            { ContentType: DataNotificationContentType.CloudCopy } or
            { ContentType: DataNotificationContentType.CloudDelete })
        {
            Statistics.LatestCloudActivity = DateTime.Now;
            if (_latestStatisticsRefresh == null || (DateTime.Now - _latestStatisticsRefresh.Value).TotalSeconds >= 60)
                await Statistics.Refresh();
        }
    }

    private async Task RefreshStatistics()
    {
        _latestStatisticsRefresh = DateTime.Now;
        await Statistics.Refresh();
    }
}