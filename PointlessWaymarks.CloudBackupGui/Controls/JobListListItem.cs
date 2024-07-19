using System.ComponentModel;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using TinyIpc.Messaging;
using Timer = System.Timers.Timer;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class JobListListItem
{
    private readonly Timer _progressTimer = new(240000);
    private DateTime? _lastLatestBatchRefresh = null;

    private JobListListItem(BackupJob job)
    {
        DbJob = job;
        PersistentId = job.PersistentId;

        PropertyChanged += OnPropertyChanged;
        _progressTimer.Elapsed += RemoveProgress;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public BackupJob? DbJob { get; set; }
    public BatchStatistics? LatestBatch { get; set; }
    public Guid PersistentId { get; set; }
    public int? ProgressProcess { get; set; }
    public string ProgressString { get; set; } = string.Empty;

    public static async Task<JobListListItem> CreateInstance(BackupJob job)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new JobListListItem(job);
        await toReturn.RefreshLatestBatch();

        return toReturn;
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProgressString) && !string.IsNullOrWhiteSpace(ProgressString))
        {
            _progressTimer.Stop();
            _progressTimer.Start();
        }
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.JobPersistentId != PersistentId) return;

        if (interProcessUpdateNotification is { ContentType: DataNotificationContentType.CloudTransferBatch })
            await RefreshLatestBatch();

        if (interProcessUpdateNotification is { ContentType: DataNotificationContentType.CloudUpload } or
            { ContentType: DataNotificationContentType.CloudCopy } or
            { ContentType: DataNotificationContentType.CloudDelete })
        {
            if (LatestBatch != null) LatestBatch.LatestCloudActivity = DateTime.Now;
            if (_lastLatestBatchRefresh == null || (DateTime.Now - _lastLatestBatchRefresh.Value).TotalSeconds >= 60)
                await RefreshLatestBatch();
        }
    }


    public async Task RefreshLatestBatch()
    {
        _lastLatestBatchRefresh = DateTime.Now;

        var context = await CloudBackupContext.CreateInstance();

        if (DbJob == null)
        {
            LatestBatch = null;
            return;
        }

        var possibleLastBatch = await context.CloudTransferBatches.Where(x => x.BackupJobId == DbJob.Id)
            .OrderByDescending(x => x.CreatedOn).FirstOrDefaultAsync();

        if (possibleLastBatch == null)
        {
            LatestBatch = null;
            return;
        }

        LatestBatch = await BatchStatistics.CreateInstance(possibleLastBatch.Id);
    }

    public async Task RefreshLatestBatchStatistics()
    {
        if (LatestBatch == null)
        {
            await RefreshLatestBatch();
            return;
        }

        await LatestBatch.Refresh();
    }

    private void RemoveProgress(object? sender, ElapsedEventArgs e)
    {
        ProgressString = string.Empty;
        ProgressProcess = null;
        _progressTimer.Stop();
    }
}