using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchLocalFilesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId, IProgress<string> progress)
    {
        progress.Report("Querying db for Cloud Transfer Batch Information");
        
        var db = await CloudBackupContext.CreateReportingInstance();
        var batch = await db.CloudTransferBatches.Include(cloudTransferBatch => cloudTransferBatch.Job!).SingleAsync(x => x.Id == batchId);

        var projectedFiles = db.FileSystemFiles.Where(x => x.CloudTransferBatchId == batch.Id).OrderBy(x => x.FileName).Select(x => new
        {
            x.FileName,
            x.FileSize,
            x.FileSystemDateTime,
            x.CreatedOn,
            x.FileHash,
            x.Id
        }).ToList();

        progress.Report("Building Excel File");

        var uploadsWorksheet = workbook.Worksheets.Add("Local Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value =
            $"File System Files - {batch.Job!.Name} (Id {batch.Job!.Id}) - Batch Id {batch.Id} Created On {batch.CreatedOn}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"{projectedFiles.Count} Files - {FileAndFolderTools.GetBytesReadable(projectedFiles.Sum(x => x.FileSize))}";
        currentRow++;
        currentRow++;

        var tableRange = uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedFiles);

        tableRange.CommonFormats();
        
        return uploadsWorksheet;
    }

    public static async Task<string> Run(int batchId, IProgress<string> progress)
    {
        progress.Report("Setting up Excel File");

        var newExcelFile = new XLWorkbook();

        await AddWorksheet(newExcelFile, batchId, progress);

        progress.Report("Querying Job Information");
        
        var db = await CloudBackupContext.CreateReportingInstance();
        var batch = db.CloudTransferBatches.Include(cloudTransferBatch => cloudTransferBatch.Job!).Single(x => x.Id == batchId);
        var job = batch.Job!;

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---LocalFiles-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");
        
        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}