using ClosedXML.Excel;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData.Reports;

public static class ExcelHelper
{
    public static IXLTable CommonFormats(this IXLTable table)
    {
        if (table.RowCount() < 1) return table;

        if (table.Fields.Any(x => x.Name.Equals("FileSize")))
            table.Field("FileSize").Column.Style.NumberFormat.Format = ExcelTools.NumberFormatThousandsCommaNoDecimal;

        foreach (var loopHeaders in table.HeadersRow().Cells())
        {
            if (loopHeaders.Value.TryGetText(out var headerText))
                loopHeaders.Value = headerText.CamelCaseToSpacedString();
        }

        table.AsRange().Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        table.Worksheet.Columns(table.Columns().First().WorksheetColumn().ColumnNumber(),
            table.Columns().Last().WorksheetColumn().ColumnNumber()).AdjustToContents(
            table.Rows().First().WorksheetRow().RowNumber(),
            table.Rows().Last().WorksheetRow().RowNumber());

        foreach (var loopColumns in table.Columns())
        {
            if (loopColumns.WorksheetColumn().Width > 120)
            {
                loopColumns.WorksheetColumn().Width = 120;
                loopColumns.AsRange().Style.Alignment.WrapText = true;
            }
        }

        //[IXLRows.AdjustToContents() ignores text wrapping · Issue #934 · ClosedXML/ClosedXML · GitHub](https://github.com/ClosedXML/ClosedXML/issues/934)
        foreach (var loopTableRows in table.Worksheet.Rows(table.Rows().First().WorksheetRow().RowNumber(),
                     table.Rows().Last().WorksheetRow().RowNumber()))
        {
            loopTableRows.ClearHeight();
        }

        return table;
    }
}