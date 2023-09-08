using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchDeletesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId, IProgress<string> progress)
    {
        progress.Report("Querying db for Cloud Transfer Batch Information");
        
        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;

        var projectedDeletes = batch.CloudDeletions.Select(x => new
        {
            x.CloudObjectKey,
            x.FileSize,
            x.DeletionCompletedSuccessfully,
            x.ErrorMessage,
            x.LastUpdatedOn,
            x.CreatedOn,
            x.Id
        }).OrderBy(x => x.CloudObjectKey).ToList();

        progress.Report("Building Excel File");

        var uploadsWorksheet = workbook.Worksheets.Add("Deletes");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value = $"Deletions - {job.Name} Id {job.Id}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"Batch {batch.Id} Created {batch.CreatedOn} - {(batch.BasedOnNewCloudFileScan ? "Based on a New Cloud File Scan" : "Based on the Cloud File Cache")}";
        currentRow++;

        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total {projectedDeletes.Count}, Completed Successfully {projectedDeletes.Count(x => x.DeletionCompletedSuccessfully)}, Not Uploaded Successfully {projectedDeletes.Count(x => !x.DeletionCompletedSuccessfully)}, With Error Messages {projectedDeletes.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))}";
        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total {FileAndFolderTools.GetBytesReadable(projectedDeletes.Sum(x => x.FileSize))}, Completed Successfully {FileAndFolderTools.GetBytesReadable(projectedDeletes.Where(x => x.DeletionCompletedSuccessfully).Sum(x => x.FileSize))}, Not Uploaded Successfully {FileAndFolderTools.GetBytesReadable(projectedDeletes.Where(x => !x.DeletionCompletedSuccessfully).Sum(x => x.FileSize))}, With Error Messages {FileAndFolderTools.GetBytesReadable(projectedDeletes.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)).Sum(x => x.FileSize))}";

        currentRow++;

        var table = uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedDeletes.OrderBy(x => x.CloudObjectKey));

        table.CommonFormats();

        return uploadsWorksheet;
    }

    public static async Task<string> Run(int batchId, IProgress<string> progress)
    {
        progress.Report("Setting up Excel File");

        var newExcelFile = new XLWorkbook();

        await AddWorksheet(newExcelFile, batchId, progress);

        progress.Report("Querying Job Information");

        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---Deletes-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}