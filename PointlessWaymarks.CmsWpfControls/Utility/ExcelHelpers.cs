using System.Diagnostics;
using System.IO;
using ClosedXML.Excel;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Import;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.Utility;

public static class ExcelHelpers
{
    public static FileInfo ContentToExcelFileAsTable(List<object> toDisplay, string fileName,
        bool openAfterSaving = true, bool limitRowHeight = true, IProgress<string> progress = null)
    {
        progress?.Report($"Starting transfer of {toDisplay.Count} to Excel");

        var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---{FolderFileUtility.TryMakeFilenameValid(fileName)}.xlsx"));

        progress?.Report($"File Name: {file.FullName}");

        progress?.Report("Creating Workbook");

        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Exported Data");

        progress?.Report("Inserting Data");

        ws.Cell(1, 1).InsertTable(toDisplay);

        progress?.Report("Applying Formatting");

        ws.Columns().AdjustToContents();

        foreach (var loopColumn in ws.ColumnsUsed().Where(x => x.Width > 70))
        {
            loopColumn.Width = 70;
            loopColumn.Style.Alignment.WrapText = true;
        }

        ws.Rows().AdjustToContents();

        if (limitRowHeight)
            foreach (var loopRow in ws.RowsUsed().Where(x => x.Height > 70))
                loopRow.Height = 70;

        progress?.Report($"Saving Excel File {file.FullName}");

        wb.SaveAs(file.FullName);

        if (openAfterSaving)
        {
            progress?.Report($"Opening Excel File {file.FullName}");

            var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        return file;
    }

    public static async Task ImportFromExcelFile(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        statusContext.Progress("Starting Excel File import.");

        var dialog = new VistaOpenFileDialog();

        if (!(dialog.ShowDialog() ?? false)) return;

        var newFile = new FileInfo(dialog.FileName);

        if (!newFile.Exists)
        {
            statusContext.ToastError("File doesn't exist?");
            return;
        }

        ContentImport.ContentImportResults contentImportResult;

        try
        {
            contentImportResult =
                await ContentImport.ImportFromFile(newFile.FullName, statusContext.ProgressTracker());
        }
        catch (Exception e)
        {
            await statusContext.ShowMessageWithOkButton("Import File Errors",
                $"Import Stopped because of an error processing the file:{Environment.NewLine}{e.Message}");
            return;
        }

        if (contentImportResult.HasError)
        {
            await statusContext.ShowMessageWithOkButton("Import Errors",
                $"Import Stopped because errors were reported:{Environment.NewLine}{contentImportResult.ErrorNotes}");
            return;
        }

        var shouldContinue = await statusContext.ShowMessage("Confirm Import",
            $"Continue?{Environment.NewLine}{Environment.NewLine}{contentImportResult.ToUpdate.Count} updates from {newFile.FullName} {Environment.NewLine}" +
            $"{string.Join(Environment.NewLine, contentImportResult.ToUpdate.Select(x => $"{Environment.NewLine}{x.Title}{Environment.NewLine}{x.DifferenceNotes}"))}",
            new List<string> {"Yes", "No"});

        if (shouldContinue == "No") return;

        var saveResult =
            await ContentImport.SaveAndGenerateHtmlFromExcelImport(contentImportResult,
                statusContext.ProgressTracker());

        if (saveResult.hasError)
        {
            await statusContext.ShowMessageWithOkButton("Excel Import Save Errors",
                $"There were error saving changes from the Excel Content:{Environment.NewLine}{saveResult.errorMessage}");
            return;
        }

        statusContext.ToastSuccess(
            $"Imported {contentImportResult.ToUpdate.Count} items with changes from {newFile.FullName}");
    }

    public static async Task ImportFromOpenExcelInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        statusContext.Progress("Starting Excel Open Instance import.");

        ContentImport.ContentImportResults contentImportResult;

        try
        {
            contentImportResult =
                await ContentImport.ImportFromTopMostExcelInstance(statusContext.ProgressTracker());
        }
        catch (Exception e)
        {
            await statusContext.ShowMessageWithOkButton("Import Errors",
                $"Import Stopped because of an error processing the file:{Environment.NewLine}{e.Message}");
            return;
        }

        if (contentImportResult.HasError)
        {
            await statusContext.ShowMessageWithOkButton("Import Errors",
                $"Import Stopped because errors were reported:{Environment.NewLine}{contentImportResult.ErrorNotes}");
            return;
        }

        var shouldContinue = await statusContext.ShowMessage("Confirm Import",
            $"Continue?{Environment.NewLine}{Environment.NewLine}{contentImportResult.ToUpdate.Count} updates from Excel {Environment.NewLine}" +
            $"{string.Join(Environment.NewLine, contentImportResult.ToUpdate.Select(x => $"{Environment.NewLine}{x.Title}{Environment.NewLine}{x.DifferenceNotes}"))}",
            new List<string> {"Yes", "No"});

        if (shouldContinue == "No") return;

        var saveResult =
            await ContentImport.SaveAndGenerateHtmlFromExcelImport(contentImportResult,
                statusContext.ProgressTracker());

        if (saveResult.hasError)
        {
            await statusContext.ShowMessageWithOkButton("Excel Import Save Errors",
                $"There were error saving changes from the Excel Content:{Environment.NewLine}{saveResult.errorMessage}");
            return;
        }

        statusContext.ToastSuccess($"Imported {contentImportResult.ToUpdate.Count} items with changes from Excel");
    }

    public static async Task<FileInfo> PointContentToExcel(List<Guid> toDisplay, string fileName,
        bool openAfterSaving = true, IProgress<string> progress = null)
    {
        var pointsAndDetails = await Db.PointsAndPointDetails(toDisplay);

        return PointContentToExcel(pointsAndDetails, fileName, openAfterSaving, progress);
    }

    public static FileInfo PointContentToExcel(List<PointContentDto> toDisplay, string fileName,
        bool openAfterSaving = true, IProgress<string> progress = null)
    {
        if (toDisplay == null || !toDisplay.Any()) return null;

        progress?.Report("Setting up list to transfer to Excel");

        var transformedList = toDisplay.Select(x => new PointContent().InjectFrom(x)).Cast<PointContent>().ToList();

        var detailList = new List<(Guid, string)>();

        foreach (var loopContent in toDisplay)
        {
            progress?.Report($"Processing {loopContent.Title} with {loopContent.PointDetails.Count} details");
            // ! This content format is used by ContentImport !
            // Push the content into a compromise format that is ok for human generation (the target here is not creating 'by
            //  hand in Excel' rather taking something like GNIS data and concatenating/text manipulating the data into
            //  shape) and still ok for parsing in code
            foreach (var loopDetail in loopContent.PointDetails)
                detailList.Add((loopContent.ContentId,
                    $"ContentId:{loopDetail.ContentId}||{Environment.NewLine}Type:{loopDetail.DataType}||{Environment.NewLine}Data:{loopDetail.StructuredDataAsJson}"));
        }

        var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---{FolderFileUtility.TryMakeFilenameValid(fileName)}.xlsx"));

        progress?.Report($"File Name {file.FullName} - creating Excel Workbook");

        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Exported Data");

        progress?.Report("Inserting Content Data");

        var insertedTable = ws.Cell(1, 1).InsertTable(transformedList);

        progress?.Report("Adding Detail Columns...");

        var contentIdColumn = insertedTable.Row(1).Cells().Single(x => x.GetString() == "ContentId")
            .WorksheetColumn().ColumnNumber();

        //Create columns to the right of the existing table to hold the Point Details and expand the table
        var neededDetailColumns = detailList.GroupBy(x => x.Item1).Max(x => x.Count());

        var firstDetailColumn = insertedTable.Columns().Last().WorksheetColumn().ColumnNumber() + 1;

        for (var i = firstDetailColumn; i < firstDetailColumn + neededDetailColumns; i++)
            ws.Cell(1, i).Value = $"PointDetail {i - firstDetailColumn + 1}";

        if (neededDetailColumns > 0) insertedTable.Resize(ws.RangeUsed());

        //Match in the point details (match rather than assume list/excel ordering)
        foreach (var loopRow in insertedTable.Rows().Skip(1))
        {
            var rowContentId = Guid.Parse(loopRow.Cell(contentIdColumn).GetString());
            var matchedData = detailList.Where(x => x.Item1 == rowContentId);

            var currentColumn = firstDetailColumn;

            foreach (var loopDetail in matchedData)
            {
                loopRow.Cell(currentColumn).Value = loopDetail.Item2;
                currentColumn++;
            }
        }

        progress?.Report("Applying Formatting");

        //Format
        ws.Columns().AdjustToContents();

        foreach (var loopColumn in ws.ColumnsUsed().Where(x => x.Width > 70))
        {
            loopColumn.Width = 70;
            loopColumn.Style.Alignment.WrapText = true;
        }

        ws.Rows().AdjustToContents();

        foreach (var loopRow in ws.RowsUsed().Where(x => x.Height > 100)) loopRow.Height = 100;

        progress?.Report($"Saving Excel File {file.FullName}");

        wb.SaveAs(file.FullName);

        if (openAfterSaving)
        {
            progress?.Report($"Opening Excel File {file.FullName}");

            var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        return file;
    }

    public static async Task SelectedToExcel(List<dynamic> selected, StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (selected == null || !selected.Any())
        {
            statusContext?.ToastError("Nothing to send to Excel?");
            return;
        }

        ContentToExcelFileAsTable(selected.Select(x => x.DbEntry).Cast<object>().ToList(), "SelectedItems",
            progress: statusContext?.ProgressTracker());
    }
}