using System.Diagnostics;
using ClosedXML.Excel;

namespace PointlessWaymarks.CommonTools
{
    public static class ExcelTools
    {
        public static FileInfo ToExcelFileAsTable(List<object> toDisplay, string fileName,
            bool openAfterSaving = true, bool limitRowHeight = true, IProgress<string>? progress = null)
        {
            progress?.Report($"Starting transfer of {toDisplay.Count} items to Excel");

            var file = new FileInfo(fileName);

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

        public static readonly string NumberFormatThousandsCommaNoDecimal = "#,##0";
    }
}
