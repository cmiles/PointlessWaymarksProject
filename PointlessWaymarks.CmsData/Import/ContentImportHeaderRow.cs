using ClosedXML.Excel;

namespace PointlessWaymarks.CmsData.Import
{
    public class ContentImportHeaderRow
    {
        public ContentImportHeaderRow(List<string> headerRow)
        {
            Columns = new List<ContentImportColumn>();
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
            Columns = new List<ContentImportColumn>();
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
}