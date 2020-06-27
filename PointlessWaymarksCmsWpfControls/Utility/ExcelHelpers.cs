using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClosedXML.Excel;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class ExcelHelpers
    {
        public static void ContentToExcelFileAsTable(List<object> toDisplay, string fileName)
        {
            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"PhotoMetadata-{FolderFileUtility.TryMakeFilenameValid(fileName)}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.xlsx"));

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("PW Data");
            ws.Cell(1, 1).InsertTable(toDisplay);
            wb.SaveAs(file.FullName);

            var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}