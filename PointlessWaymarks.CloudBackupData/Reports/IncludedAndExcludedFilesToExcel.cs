using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Batch;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class IncludedAndExcludedFilesToExcel
{
    public static async Task<FileInfo> Run(int jobId,
        IProgress<string> progress)
    {
        progress.Report("Querying Job Information");

        var db = await CloudBackupContext.CreateInstance();

        var job = await db.BackupJobs.Include(backupJob => backupJob.ExcludedDirectories)
            .Include(backupJob => backupJob.ExcludedDirectoryNamePatterns)
            .Include(backupJob => backupJob.ExcludedFileNamePatterns).SingleAsync(x => x.Id == jobId);

        return await Run(job.Name, job.LocalDirectory, job.ExcludedDirectories.Select(x => x.Directory).ToList(),
            job.ExcludedDirectoryNamePatterns.Select(x => x.Pattern).ToList(),
            job.ExcludedFileNamePatterns.Select(x => x.Pattern).ToList(), progress);
    }

    public static async Task<FileInfo> Run(string jobName, string initialDirectory,
        List<string> excludedDirectories, List<string> excludedDirectoryPatterns, List<string> excludedFilePatterns,
        IProgress<string> progress)
    {
        var includedFiles = await CreationTools.GetIncludedLocalFiles(initialDirectory,
            excludedDirectories, excludedDirectoryPatterns,
            excludedFilePatterns, progress);
        var excludedFiles = await CreationTools.GetExcludedLocalFiles(initialDirectory,
            excludedDirectories, excludedDirectoryPatterns,
            excludedFilePatterns, progress);

        progress.Report("Creating and Formatting Included Files Excel Sheet");
        
        var newExcelFile = new XLWorkbook();
        var includedWorksheet = newExcelFile.Worksheets.Add("Included Files");

        var currentRow = 1;

        includedWorksheet.Cell(currentRow, 1).Value = $"Included - {jobName}";
        includedWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(includedWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        includedWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        currentRow++;

        includedWorksheet.Cell(currentRow++, 1).Value =
            $"Initial Local Directory: {initialDirectory}";
        includedWorksheet.Cell(currentRow++, 1).Value = $"Included Files ({includedFiles.Count})";
        includedWorksheet.Cell(currentRow++, 1).Value = $"Excluded Files ({excludedFiles.Count})";
        currentRow++;

        includedWorksheet.Cell(currentRow++, 1).Value = $"Excluded Directories ({excludedDirectories.Count})";
        excludedDirectories.ForEach(x =>
            includedWorksheet.Cell(currentRow++, 1).Value = x);
        currentRow++;

        includedWorksheet.Cell(currentRow++, 1).Value =
            $"Excluded Directory Name Patterns ({excludedDirectoryPatterns.Count})";
        excludedDirectoryPatterns.ForEach(x =>
            includedWorksheet.Cell(currentRow++, 1).Value = x);
        currentRow++;

        includedWorksheet.Cell(currentRow++, 1).Value =
            $"Excluded File Name Patterns ({excludedFilePatterns.Count})";
        excludedFilePatterns.ForEach(x =>
            includedWorksheet.Cell(currentRow++, 1).Value = x);

        currentRow++;
        currentRow++;

        includedWorksheet.Cell(currentRow++, 1)
            .InsertTable(includedFiles.Select(x => new { Included_Files = x.LocalFile.FullName }));

        includedWorksheet.Columns().AdjustToContents();

        progress.Report("Creating and Formatting Excluded Files Excel Sheet");
        
        var excludedWorksheet = newExcelFile.Worksheets.Add("Excluded Files");

        currentRow = 1;

        excludedWorksheet.Cell(currentRow, 1).Value = $"Excluded - {jobName}";
        excludedWorksheet.Cell(currentRow, 1).Style.Font
            .SetFontSize(excludedWorksheet.Cell(currentRow, 1).Style.Font.FontSize + 4);
        excludedWorksheet.Cell(currentRow++, 1).Style.Font.SetBold(true);
        currentRow++;

        excludedWorksheet.Cell(currentRow++, 1).Value =
            $"Initial Local Directory: {initialDirectory}";
        excludedWorksheet.Cell(currentRow++, 1).Value = $"Included Files ({includedFiles.Count})";
        excludedWorksheet.Cell(currentRow++, 1).Value = $"Excluded Files ({excludedFiles.Count})";
        currentRow++;

        excludedWorksheet.Cell(currentRow++, 1).Value = $"Excluded Directories ({excludedDirectories.Count})";
        excludedDirectories.ForEach(x =>
            excludedWorksheet.Cell(currentRow++, 1).Value = x);
        currentRow++;

        excludedWorksheet.Cell(currentRow++, 1).Value =
            $"Excluded Directory Name Patterns ({excludedDirectoryPatterns.Count})";
        excludedDirectoryPatterns.ForEach(x =>
            excludedWorksheet.Cell(currentRow++, 1).Value = x);
        currentRow++;

        excludedWorksheet.Cell(currentRow++, 1).Value =
            $"Excluded File Name Patterns ({excludedFilePatterns.Count})";
        excludedFilePatterns.ForEach(x =>
            excludedWorksheet.Cell(currentRow++, 1).Value = x);

        currentRow++;
        currentRow++;

        excludedWorksheet.Cell(currentRow++, 1)
            .InsertTable(excludedFiles.Select(x => new { Excluded_Files = x.LocalFile.FullName }));

        excludedWorksheet.Columns().AdjustToContents();

        var file = new FileInfo(Path.Combine(FileLocationHelpers.ReportsDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---IncludedAndExcludedBackupFiles-{FileAndFolderTools.TryMakeFilenameValid(jobName)}.xlsx"));

        progress.Report($"Saving Excel File {file.FullName}");
        
        newExcelFile.SaveAs(file.FullName);

        progress?.Report($"Opening Excel File {file.FullName}");

        var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };

        Process.Start(ps);

        return file;
    }
}