using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class CloudCacheFilesToExcel
{
    public static async Task<IXLWorksheet> AddWorksheet(XLWorkbook workbook, int jobId, IProgress<string> progress)
    {
        progress.Report("Querying db for Cloud Batch, Job and Cache File Information");

        var db = await CloudBackupContext.CreateInstance();
        var job = await db.BackupJobs.SingleAsync(x => x.Id == jobId);

        var projectedFiles = job.CloudCacheFiles.OrderBy(x => x.CloudObjectKey).Select(x => new
        {
            Key = x.CloudObjectKey,
            x.Note,
            x.FileSize,
            x.FileSystemDateTime,
            x.LastEditOn,
            x.FileHash,
            x.Id,
        }).ToList();

        progress.Report("Building Excel File");

        var uploadsWorksheet = workbook.Worksheets.Add("Cloud Cache Files");

        var currentRow = 1;

        uploadsWorksheet.Cell(currentRow, 1).Value =
            $"Cloud Cache Files - {job.Name} (Id {job.Id})";
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

    public static async Task<string> Run(int jobId, IProgress<string> progress)
    {
        progress.Report("Setting up Excel File");

        var newExcelFile = new XLWorkbook();

        await AddWorksheet(newExcelFile, jobId, progress);

        progress.Report("Querying Job Information");

        var db = await CloudBackupContext.CreateInstance();
        var job = db.BackupJobs.Single(x => x.Id == jobId);

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---CloudCacheFiles-{FileAndFolderTools.TryMakeFilenameValid(job.Name)}-Id-{job.Id}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");

        newExcelFile.SaveAs(file.FullName);

        return file.FullName;
    }
}