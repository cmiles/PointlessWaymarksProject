using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchReportToExcel
{
    public static async Task<string> Run(int batchId, IProgress<string> progress)
    {
        progress.Report("Setting up Excel File");

        var newExcelFile = new XLWorkbook();

        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;

        await BatchCopiesToExcel.AddWorksheet(newExcelFile, batchId, progress);
        await BatchUploadsToExcel.AddWorksheet(newExcelFile, batchId, progress);
        await BatchDeletesToExcel.AddWorksheet(newExcelFile, batchId, progress);
        await BatchLocalFilesToExcel.AddWorksheet(newExcelFile, batchId, progress);
        await BatchCloudFilesToExcel.AddWorksheet(newExcelFile, batchId, progress);
        await CloudCacheFilesToExcel.AddWorksheet(newExcelFile, job.Id, progress);

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---BatchReport-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}