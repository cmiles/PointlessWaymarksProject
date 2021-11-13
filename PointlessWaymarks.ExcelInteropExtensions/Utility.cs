namespace PointlessWaymarks.ExcelInteropExtensions;

public static class ExcelUtilities
{
    public static string ExcelColumnFromNumber(int column)
    {
        var columnString = "";
        decimal columnNumber = column;
        while (columnNumber > 0)
        {
            var currentLetterNumber = (columnNumber - 1) % 26;
            var currentLetter = (char) (currentLetterNumber + 65);
            columnString = $"{currentLetter}{columnString}";
            columnNumber = (columnNumber - (currentLetterNumber + 1)) / 26;
        }

        return columnString;
    }

    public static int NumberFromExcelColumn(string column)
    {
        var retVal = 0;
        var col = column.ToUpper();
        for (var iChar = col.Length - 1; iChar >= 0; iChar--)
        {
            var colPiece = col[iChar];
            var colNum = colPiece - 64;
            retVal += colNum * (int) Math.Pow(26, col.Length - (iChar + 1));
        }

        return retVal;
    }
}