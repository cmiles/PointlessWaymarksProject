using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchDeletesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId)
    {
        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;

        var projectedDeletes = batch.CloudDeletions.Select(x => new
        {
            x.CloudObjectKey,
            x.DeletionCompletedSuccessfully,
            x.ErrorMessage,
            x.Id,
            x.CreatedOn,
            x.LastUpdatedOn
        }).OrderBy(x => x.CloudObjectKey).ToList();

        var uploadsWorksheet = workbook.Worksheets.Add("Deletes");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value = $"Deletions - {job.Name} Id {job.Id}";
        uploadsWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(uploadsWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        uploadsWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        uploadsWorksheet.Cell(currentRow++, 1).Value = $"Batch {batch.Id} Created {batch.CreatedOn}";
        currentRow++;

        uploadsWorksheet.Cell(currentRow++, 1).Value =
            $"Total {projectedDeletes.Count}, Complete Successfully {projectedDeletes.Count(x => x.DeletionCompletedSuccessfully)}, Not Uploaded Successfully {projectedDeletes.Count(x => !x.DeletionCompletedSuccessfully)}, With Error Messages {projectedDeletes.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))}";

        currentRow++;

        uploadsWorksheet.Cell(currentRow, 1).InsertTable(projectedDeletes.OrderBy(x => x.CloudObjectKey));

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
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---Deletes-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}