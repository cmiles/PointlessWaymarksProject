using System.Collections.ObjectModel;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class BatchListContext
{
    public BackupJob DbJob { get; set; }
    public required ObservableCollection<BatchListListItem> Items { get; set; }
    public required int JobId { get; set; }
    public BatchListListItem? SelectedBatch { get; set; }
    public List<BatchListListItem> SelectedBatches { get; set; } = new();
    public required StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
    public async Task BatchToExcel(CloudTransferBatch dbBatch)
    {
        ProcessTools.Open(await BatchReportToExcel.Run(dbBatch.Id));
    }

    [BlockingCommand]
    public async Task CloudFilesToExcel(CloudTransferBatch dbBatch)
    {
        ProcessTools.Open(await BatchCloudFilesToExcel.Run(dbBatch.Id));
    }

    public static async Task<BatchListContext> CreateInstance(StatusControlContext statusContext, int jobId)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var factoryItems = new ObservableCollection<BatchListListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new BatchListContext
        {
            JobId = jobId,
            Items = factoryItems,
            StatusContext = statusContext
        };

        await toReturn.Setup();
        await toReturn.RefreshList();

        return toReturn;
    }

    [BlockingCommand]
    public async Task DeletesToExcel(CloudTransferBatch dbBatch)
    {
        ProcessTools.Open(await BatchDeletesToExcel.Run(dbBatch.Id));
    }

    [BlockingCommand]
    public async Task LocalFilesToExcel(CloudTransferBatch dbBatch)
    {
        ProcessTools.Open(await BatchLocalFilesToExcel.Run(dbBatch.Id));
    }

    [BlockingCommand]
    public async Task RefreshList()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await CloudBackupContext.CreateInstance();

        var job = db.BackupJobs.Single(x => x.Id == JobId);

        DbJob = job;

        var batchList = new List<BatchListListItem>();

        foreach (var loopBatch in job.Batches) batchList.Add(BatchListListItem.CreateInstance(loopBatch));

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        batchList.ForEach(x => Items.Add(x));
    }

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        BuildCommands();
        await RefreshList();
    }

    [BlockingCommand]
    public async Task UploadsToExcel(CloudTransferBatch dbBatch)
    {
        ProcessTools.Open(await BatchUploadsToExcel.Run(dbBatch.Id));
    }
}