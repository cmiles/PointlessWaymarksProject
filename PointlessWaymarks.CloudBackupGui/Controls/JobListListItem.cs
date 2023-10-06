using System.ComponentModel;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class JobListListItem
{
    private readonly System.Timers.Timer _progressTimer = new(240000);

    private JobListListItem(BackupJob job)
    {
        DbJob = job;
        PersistentId = job.PersistentId;

        PropertyChanged += OnPropertyChanged;
        _progressTimer.Elapsed += RemoveProgress;
    }

    public static async Task<JobListListItem> CreateInstance(BackupJob job)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new JobListListItem(job);
        await toReturn.RefreshLatestBatch();

        return toReturn;
    }

    public async Task RefreshLatestBatch()
    {
        var context = await CloudBackupContext.CreateInstance();

        if (DbJob == null)
        {
            LatestBatch = null;
            return;
        }

        var possibleLastBatch = await context.CloudTransferBatches.Where(x => x.JobId == DbJob.Id)
            .OrderByDescending(x => x.CreatedOn).FirstOrDefaultAsync();

        if (possibleLastBatch == null)
        {
            LatestBatch = null;
            return;
        }

        LatestBatch = await BatchListListItem.CreateInstance(possibleLastBatch);
    }

    private void RemoveProgress(object? sender, ElapsedEventArgs e)
    {
        ProgressString = string.Empty;
        _progressTimer.Stop();
    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProgressString) && !string.IsNullOrWhiteSpace(ProgressString))
        {
            _progressTimer.Stop();
            _progressTimer.Start();
        }
    }

    public BackupJob? DbJob { get; set; }
    public Guid PersistentId { get; set; }
    public string ProgressString { get; set; } = string.Empty;
    public BatchListListItem? LatestBatch { get; set; }
}