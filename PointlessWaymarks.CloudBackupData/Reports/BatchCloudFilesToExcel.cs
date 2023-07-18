using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class CloudFilesToExcel
{
    public static async Task<string> Run(int batchId)
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

        var newExcelFile = new XLWorkbook();
        var uploadsWorksheet = newExcelFile.Worksheets.Add("Uploaded Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value =
            $"File System Files - {batch.Job!.Name} (Id {batch.Job!.Id}) - Batch Id {batch.Id} Created On {batch.CreatedOn}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"{projectedFiles.Count()} Files";
        currentRow++;
        currentRow++;

        var tableRange = uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedFiles);

        uploadsWorksheet.Columns().AdjustToContents(currentRow);

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---CloudFiles-{FileAndFolderTools.TryMakeFilenameValid(batch.Job.Name)}-Id-{batch.Job.Id}-Batch-{batch.Id}.xlsx"));

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}