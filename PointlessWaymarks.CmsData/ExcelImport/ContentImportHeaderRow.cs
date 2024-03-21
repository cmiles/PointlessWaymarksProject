using ClosedXML.Excel;

namespace PointlessWaymarks.CmsData.ExcelImport;

public class ContentImportHeaderRow
{
    public ContentImportHeaderRow(List<string> headerRow)
    {
        Columns = [];
        if (!headerRow.Any()) return;

        foreach (var loopCells in headerRow)
        {
            if (string.IsNullOrWhiteSpace(loopCells)) continue;

            Columns.Add(new ContentImportColumn
            {
                ColumnHeader = loopCells, ColumnNumber = headerRow.IndexOf(loopCells)
            });
        }
    }

    public ContentImportHeaderRow(IXLRangeRow headerRow)
    {
        Columns = [];
        if (!headerRow.Cells().Any()) return;

        foreach (var loopCells in headerRow.Cells())
        {
            var columnStringValue = loopCells.Value.ToString();

            if (string.IsNullOrWhiteSpace(columnStringValue)) continue;

            Columns.Add(new ContentImportColumn
            {
                ColumnHeader = columnStringValue, ColumnNumber = loopCells.WorksheetColumn().ColumnNumber()
            });
        }
    }

    public List<ContentImportColumn> Columns { get; }
}