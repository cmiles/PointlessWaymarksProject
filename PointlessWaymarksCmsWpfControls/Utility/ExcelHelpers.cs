using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.ExcelImport;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class ExcelHelpers
    {
        public static FileInfo ContentToExcelFileAsTable(List<object> toDisplay, string fileName,
            bool openAfterSaving = true)
        {
            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---{FolderFileUtility.TryMakeFilenameValid(fileName)}.xlsx"));

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Exported Data");

            var insertedTable = ws.Cell(1, 1).InsertTable(toDisplay);

            ws.Columns().AdjustToContents();

            foreach (var loopColumn in ws.ColumnsUsed().Where(x => x.Width > 70))
            {
                loopColumn.Width = 70;
                loopColumn.Style.Alignment.WrapText = true;
            }

            ws.Rows().AdjustToContents();

            foreach (var loopRow in ws.RowsUsed().Where(x => x.Height > 70)) loopRow.Height = 70;

            wb.SaveAs(file.FullName);

            if (openAfterSaving)
            {
                var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }

            return file;
        }

        public static async Task ImportFromExcel(StatusControlContext statusContext)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            statusContext.Progress("Starting excel load.");

            var dialog = new VistaOpenFileDialog();

            if (!(dialog.ShowDialog() ?? false)) return;

            var newFile = new FileInfo(dialog.FileName);

            if (!newFile.Exists)
            {
                statusContext.ToastError("File doesn't exist?");
                return;
            }

            ExcelContentImports.ExcelContentTableImportResults contentTableImportResult;

            try
            {
                contentTableImportResult =
                    await ExcelContentImports.ImportFromFile(newFile.FullName, statusContext.ProgressTracker());
            }
            catch (Exception e)
            {
                await statusContext.ShowMessageWithOkButton("Import File Errors",
                    $"Import Stopped because of an error processing the file:{Environment.NewLine}{e.Message}");
                return;
            }

            if (contentTableImportResult.HasError)
            {
                await statusContext.ShowMessageWithOkButton("Import Errors",
                    $"Import Stopped because errors were reported:{Environment.NewLine}{contentTableImportResult.ErrorNotes}");
                return;
            }

            var shouldContinue = await statusContext.ShowMessage("Confirm Import",
                $"Continue?{Environment.NewLine}{Environment.NewLine}{contentTableImportResult.ToUpdate.Count} updates from {newFile.FullName} {Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, contentTableImportResult.ToUpdate.Select(x => $"{Environment.NewLine}{x.Title}{Environment.NewLine}{x.DifferenceNotes}"))}",
                new List<string> {"Yes", "No"});

            if (shouldContinue == "No") return;

            var saveResult =
                await ExcelContentImports.SaveAndGenerateHtmlFromExcelImport(contentTableImportResult,
                    statusContext.ProgressTracker());

            if (saveResult.hasError)
            {
                await statusContext.ShowMessageWithOkButton("Excel Import Save Errors",
                    $"There were error saving changes from the Excel Content:{Environment.NewLine}{saveResult.errorMessage}");
                return;
            }

            statusContext.ToastSuccess(
                $"Imported {contentTableImportResult.ToUpdate.Count} items with changes from {newFile.FullName}");
        }

        public static async Task SelectedToExcel(List<dynamic> selected, StatusControlContext statusContext)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (selected == null || !selected.Any())
            {
                statusContext.ToastError("Nothing to send to Excel?");
                return;
            }

            ContentToExcelFileAsTable(selected.Select(x => x.DbEntry).Cast<object>().ToList(), "SelectedPhotos");
        }
    }
}