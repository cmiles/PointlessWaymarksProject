using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchToExcel
{
    public static async Task<string> Run(int batchId)
    {
        var newExcelFile = new XLWorkbook();

        await BatchUploadsToExcel.AddWorksheet(newExcelFile, batchId);
        await BatchDeletesToExcel.AddWorksheet(newExcelFile, batchId);
        await BatchLocalFilesToExcel.AddWorksheet(newExcelFile, batchId);
        await BatchCloudFilesToExcel.AddWorksheet(newExcelFile, batchId);
        
        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---BatchReport-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}