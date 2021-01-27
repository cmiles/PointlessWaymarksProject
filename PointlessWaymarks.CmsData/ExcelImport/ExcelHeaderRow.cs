using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace PointlessWaymarks.CmsData.ExcelImport
{
    public class ExcelHeaderRow
    {
        public ExcelHeaderRow(List<string> headerRow)
        {
            Columns = new List<ExcelImportColumn>();
            if (!headerRow.Any()) return;

            foreach (var loopCells in headerRow)
            {
                var columnStringValue = loopCells;

                if (string.IsNullOrWhiteSpace(columnStringValue)) continue;

                Columns.Add(new ExcelImportColumn
                {
                    ColumnHeader = columnStringValue,
                    ExcelSheetColumn = headerRow.IndexOf(loopCells)
                });
            }
        }

        public ExcelHeaderRow(IXLRangeRow headerRow)
        {
            Columns = new List<ExcelImportColumn>();
            if (headerRow == null || !headerRow.Cells().Any()) return;

            foreach (var loopCells in headerRow.Cells())
            {
                var columnStringValue = loopCells.Value.ToString();

                if (string.IsNullOrWhiteSpace(columnStringValue)) continue;

                Columns.Add(new ExcelImportColumn
                {
                    ColumnHeader = columnStringValue, ExcelSheetColumn = loopCells.WorksheetColumn().ColumnNumber()
                });
            }
        }

        public List<ExcelImportColumn> Columns { get; set; }
    }
}