using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchCloudFilesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId, IProgress<string> progress)
    {
        progress.Report("Querying db for Cloud Transfer Batch Information");
        
        var db = await CloudBackupContext.CreateInstance();
        var batch = await db.CloudTransferBatches.SingleAsync(x => x.Id == batchId);

        var projectedFiles = batch.CloudFiles.OrderBy(x => x.CloudObjectKey).Select(x => new
        {
            Key = x.CloudObjectKey,
            x.FileSize,
            x.FileSystemDateTime,
            x.CreatedOn,
            x.FileHash,
            x.Id,
        }).ToList();

        progress.Report("Building Excel File");
        
        var uploadsWorksheet = workbook.Worksheets.Add("Cloud Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value =
            $"Cloud Files - {batch.Job!.Name} (Id {batch.Job!.Id}) - Batch Id {batch.Id} Created On {batch.CreatedOn} - {(batch.BasedOnNewCloudFileScan ? "Based on a New Cloud File Scan" : "Based on the Cloud File Cache")}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"{projectedFiles.Count} Files- {FileAndFolderTools.GetBytesReadable(projectedFiles.Sum(x => x.FileSize))}";
        currentRow++;
        currentRow++;

        var table = uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedFiles);

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
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---CloudFiles-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}