using ClosedXML.Excel;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchReportToExcel
{
    public static async Task<string> Run(int jobId, int batchId)
    {
        var db = await CloudBackupContext.CreateInstance();

        var job = db.BackupJobs.Single(x => x.Id == jobId);
        var batch = job.Batches.Single(x => x.Id == batchId);
        var uploads = batch.CloudUploads.ToList();
        var deletes = batch.CloudDeletions.ToList();
        var fileSystemFiles = batch.FileSystemFiles.ToList();
        var cloudFiles = batch.CloudFiles.ToList();

        var newExcelFile = new XLWorkbook();
        var uploadsWorksheet = newExcelFile.Worksheets.Add("Uploaded Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value = $"Uploads - {job.Name} - Id {job.Id}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"Batch {batch.Id} Created {batch.CreatedOn}";
        currentRow++;

        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total {uploads.Count}, Complete Successfully {uploads.Count(x => x.UploadCompletedSuccessfully)}, Not Uploaded Successfully {uploads.Count(x => !x.UploadCompletedSuccessfully)}, With Error Messages {uploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))}";

        currentRow++;

        var tableRange = uploadsWorksheet.Cell(currentRow++, 1).InsertTable(uploads.OrderBy(x => x.FileSystemFile));

        uploadsWorksheet.Columns().AdjustToContents(currentRow);

        return string.Empty;
    }
}