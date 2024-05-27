using System.ComponentModel;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using Timer = System.Timers.Timer;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class JobListListItem
{
    private readonly Timer _progressTimer = new(240000);
    
    private JobListListItem(BackupJob job)
    {
        DbJob = job;
        PersistentId = job.PersistentId;
        
        PropertyChanged += OnPropertyChanged;
        _progressTimer.Elapsed += RemoveProgress;
    }
    
    public BackupJob? DbJob { get; set; }
    public DateTime? LastCloudScan { get; set; }
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
    
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProgressString) && !string.IsNullOrWhiteSpace(ProgressString))
        {
            _progressTimer.Stop();
            _progressTimer.Start();
        }
    }
    
    public async Task RefreshLatestBatch()
    {
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
        
        LastCloudScan = context.CloudTransferBatches.Where(x => x.BackupJobId == DbJob.Id && x.BasedOnNewCloudFileScan)
            .OrderByDescending(x => x.CreatedOn).FirstOrDefault()?.CreatedOn;
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