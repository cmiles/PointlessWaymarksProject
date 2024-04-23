using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class BatchCopiesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int batchId, IProgress<string> progress)
    {
        progress.Report("Querying db for Cloud Transfer Batch Information");
        
        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;
        
        var projectedCopies = db.CloudCopies.Where(x => x.CloudTransferBatchId == batch.Id).Select(x => new
        {
            x.FileSystemFile,
            FromCloudObjectKey = x.ExistingCloudObjectKey,
            ToCloudObjectKey = x.NewCloudObjectKey,
            x.FileSize,
            x.CopyCompletedSuccessfully,
            x.ErrorMessage,
            x.LastUpdatedOn,
            x.CreatedOn,
            x.Id
        }).OrderBy(x => x.FileSystemFile).ToList();
        
        progress.Report("Building Excel File");
        
        var copiesWorksheet = workbook.Worksheets.Add("Copies");
        
        var currentRow = 1;
        
        copiesWorksheet.Cell(currentRow, 1).Value = $"Copies - {job.Name} Id {job.Id}";
        copiesWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(copiesWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        copiesWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        copiesWorksheet.Cell(currentRow++, 1).Value = $"Batch {batch.Id} Created {batch.CreatedOn} - {(batch.BasedOnNewCloudFileScan ? "Based on a New Cloud File Scan" : "Based on the Cloud File Cache")}";
        currentRow++;
        
        copiesWorksheet.Cell(currentRow++, 1).Value =
            $"Total {projectedCopies.Count}, Completed Successfully {projectedCopies.Count(x => x.CopyCompletedSuccessfully)}, Not Copyed Successfully {projectedCopies.Count(x => !x.CopyCompletedSuccessfully)}, With Error Messages {projectedCopies.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))}";
        
        copiesWorksheet.Cell(currentRow++, 1).Value =
            $"Total Batch Size {FileAndFolderTools.GetBytesReadable(projectedCopies.Sum(x => x.FileSize))}, Copyed {FileAndFolderTools.GetBytesReadable(projectedCopies.Where(x => x.CopyCompletedSuccessfully).Sum(x => x.FileSize))}, Not Copyed Successfully {FileAndFolderTools.GetBytesReadable(projectedCopies.Where(x => !x.CopyCompletedSuccessfully).Sum(x => x.FileSize))}, With Error Messages {FileAndFolderTools.GetBytesReadable(projectedCopies.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)).Sum(x => x.FileSize))}";
        
        currentRow++;
        
        var table = copiesWorksheet.Cell(currentRow, 1).InsertTable(projectedCopies.OrderBy(x => x.FileSystemFile));
        
        table.CommonFormats();
        
        return copiesWorksheet;
    }
    
    public static async Task<string> Run(int batchId, IProgress<string> progress)
    {
        progress.Report("Setting up Excel File");
        
        var newExcelFile = new XLWorkbook();
        
        await AddWorksheet(newExcelFile, batchId, progress);
        
        var db = await CloudBackupContext.CreateInstance();
        var batch = db.CloudTransferBatches.Single(x => x.Id == batchId);
        var job = batch.Job!;
        
        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---Copies-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}-Batch-{batch.Id}.xlsx"));
        
        progress.Report($"Saving Excel File {file.FullName}");
        
        newExcelFile.SaveAs(file.FullName);
        
        return file.FullName;
    }
}