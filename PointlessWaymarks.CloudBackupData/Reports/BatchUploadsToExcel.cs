using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchUploadsToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId, IProgress<string> progress)
    {
        progress.Report("Querying db for Cloud Transfer Batch Information");
        
        var db = await CloudBackupContext.CreateReportingInstance();
        var batch = db.CloudTransferBatches.Include(cloudTransferBatch => cloudTransferBatch.Job!).Single(x => x.Id == batchId);
        var job = batch.Job!;

        var projectedUploads = db.CloudUploads.Where(x => x.CloudTransferBatchId == batch.Id).Select(x => new
        {
            x.FileSystemFile,
            x.CloudObjectKey,
            x.FileSize,
            x.UploadCompletedSuccessfully,
            x.ErrorMessage,
            x.LastUpdatedOn,
            x.CreatedOn,
            x.Id
        }).OrderBy(x => x.FileSystemFile).ToList();

        progress.Report("Building Excel File");

        var uploadsWorksheet = workbook.Worksheets.Add("Uploads");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value = $"Uploads - {job.Name} Id {job.Id}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"Batch {batch.Id} Created {batch.CreatedOn} - {(batch.BasedOnNewCloudFileScan ? "Based on a New Cloud File Scan" : "Based on the Cloud File Cache")}";
        currentRow++;

        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total {projectedUploads.Count}, Completed Successfully {projectedUploads.Count(x => x.UploadCompletedSuccessfully)}, Not Uploaded Successfully {projectedUploads.Count(x => !x.UploadCompletedSuccessfully)}, With Error Messages {projectedUploads.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))}";

        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total Batch Size {FileAndFolderTools.GetBytesReadable(projectedUploads.Sum(x => x.FileSize))}, Uploaded {FileAndFolderTools.GetBytesReadable(projectedUploads.Where(x => x.UploadCompletedSuccessfully).Sum(x => x.FileSize))}, Not Uploaded Successfully {FileAndFolderTools.GetBytesReadable(projectedUploads.Where(x => !x.UploadCompletedSuccessfully).Sum(x => x.FileSize))}, With Error Messages {FileAndFolderTools.GetBytesReadable(projectedUploads.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)).Sum(x => x.FileSize))}";
        
        currentRow++;

        var table = uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedUploads.OrderBy(x => x.FileSystemFile));

        table.CommonFormats();
        
        return uploadsWorksheet;
    }

    public static async Task<string> Run(int batchId, IProgress<string> progress)
    {
        progress.Report("Setting up Excel File");

        var newExcelFile = new XLWorkbook();

        await AddWorksheet(newExcelFile, batchId, progress);

        var db = await CloudBackupContext.CreateReportingInstance();
        var batch = db.CloudTransferBatches.Include(cloudTransferBatch => cloudTransferBatch.Job!).Single(x => x.Id == batchId);
        var job = batch.Job!;

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---Uploads-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");
        
        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}