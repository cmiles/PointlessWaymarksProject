using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchCloudFilesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId)
    {
        var db = await CloudBackupContext.CreateInstance();
        var batch = await db.CloudTransferBatches.SingleAsync(x => x.Id == batchId);

        var projectedFiles = batch.CloudFiles.OrderBy(x => x.Key).Select(x => new
        {
            x.Key,
            x.FileSize,
            x.FileSystemDateTime,
            x.FileHash,
            x.Id,
            x.CreatedOn
        }).ToList();

        var uploadsWorksheet = workbook.Worksheets.Add("Cloud Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value =
            $"File System Files - {batch.Job!.Name} (Id {batch.Job!.Id}) - Batch Id {batch.Id} Created On {batch.CreatedOn}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"{projectedFiles.Count()} Files";
        currentRow++;
        currentRow++;

        uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedFiles);

        uploadsWorksheet.Columns().AdjustToContents(currentRow);

        return uploadsWorksheet;
    }

    public static async Task<string> Run(int batchId)
    {
        var newExcelFile = new XLWorkbook();

        await AddWorksheet(newExcelFile, batchId);

        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---CloudFiles-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}