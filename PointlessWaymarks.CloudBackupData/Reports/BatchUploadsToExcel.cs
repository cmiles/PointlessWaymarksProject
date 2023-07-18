using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchReportToExcel
{
    public static async Task<string> Run(int jobId, int batchId)
    {
        var db = await CloudBackupContext.CreateInstance();

        var job = db.BackupJobs.Single(x => x.Id == jobId);
        var batch = job.Batches.Single(x => x.Id == batchId);
        var projectedUploads = batch.CloudUploads.Select(x => new
        {
            x.FileSystemFile,
            x.CloudObjectKey,
            x.FileSize,
            x.UploadCompletedSuccessfully,
            x.ErrorMessage,
            x.Id,
            x.CreatedOn,
            x.LastUpdatedOn
        }).OrderBy(x => x.FileSystemFile).ToList();
        
        var newExcelFile = new XLWorkbook();
        var uploadsWorksheet = newExcelFile.Worksheets.Add("Uploaded Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value = $"Uploads - {job.Name} Id {job.Id}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"Batch {batch.Id} Created {batch.CreatedOn}";
        currentRow++;

        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total {projectedUploads.Count}, Complete Successfully {projectedUploads.Count(x => x.UploadCompletedSuccessfully)}, Not Uploaded Successfully {projectedUploads.Count(x => !x.UploadCompletedSuccessfully)}, With Error Messages {projectedUploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))}";

        currentRow++;

        uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedUploads.OrderBy(x => x.FileSystemFile));

        uploadsWorksheet.Columns().AdjustToContents(currentRow);

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---Uploads-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}