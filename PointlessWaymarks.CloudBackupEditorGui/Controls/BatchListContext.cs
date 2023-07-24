using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupEditorGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public class BatchListContext
{
    public readonly string HelpText = """
                                      ## Batch List

                                      Any time a scan of the local and Amazon S3 files is done a Transfer Batch is created. A Transfer Batch holds a list of all the files and lists of the needed uploads and downloads.

                                      The Batch List gives you a way to get all the current data for a batch as Excel reports. This can be useful both for confirming what has (and has not) been backed up and for deciding what Batch to use when running a backup.
                                      """;

    public BackupJob DbJob { get; set; }

    public HelpDisplayContext? HelpContext { get; set; }
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

        return toReturn;
    }

    [BlockingCommand]
    public async Task DeleteSelectedBatches()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedBatches.Any())
        {
            StatusContext.ToastWarning("Nothing Selected to Delete?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        if (MessageBox.Show(
                "Deleting a Transfer Batch will NOT delete any files or directories - but it will delete all records associated with this Batch! Continue??",
                "Delete Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
            return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopBatch in SelectedBatches)
        {
            var db = await CloudBackupContext.CreateInstance();
            var currentItem = await db.CloudTransferBatches.SingleAsync(x => x.Id == loopBatch.DbBatch.Id);

            db.Remove(currentItem);
            await db.SaveChangesAsync();
        }

        await RefreshList();
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

        StatusContext.Progress($"Batch List - Found job {job.Name}");

        DbJob = job;

        var batchList = new List<BatchListListItem>();

        foreach (var loopBatch in job.Batches)
        {
            StatusContext.Progress($"Batch List - Creating Entry for Batch Id {loopBatch.Id}");
            batchList.Add(BatchListListItem.CreateInstance(loopBatch));
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        StatusContext.Progress("Batch List - Adding Batch Items to Gui");
        batchList.ForEach(x => Items.Add(x));
    }

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Batch List - Setting Up");

        HelpContext = new HelpDisplayContext(new List<string>
        {
            HelpText
        });

        BuildCommands();
        await RefreshList();
    }

    [BlockingCommand]
    public async Task UploadsToExcel(CloudTransferBatch dbBatch)
    {
        ProcessTools.Open(await BatchUploadsToExcel.Run(dbBatch.Id));
    }
}