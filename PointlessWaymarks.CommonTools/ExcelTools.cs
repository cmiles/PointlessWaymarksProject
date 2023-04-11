using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace PointlessWaymarks.CommonTools
{
    public static class ExcelTools
    {
        public static FileInfo ToExcelFileAsTable(List<object> toDisplay, string fileName,
            bool openAfterSaving = true, bool limitRowHeight = true, IProgress<string>? progress = null)
        {
            progress?.Report($"Starting transfer of {toDisplay.Count} to Excel");

            var file = new FileInfo(fileName);

            //var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
            //    $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---{FileAndFolderTools.TryMakeFilenameValid(fileName)}.xlsx"));

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

                var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };
                Process.Start(ps);
            }

            return file;
        }
    }
}
