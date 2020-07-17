using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KellermanSoftware.CompareNetObjects;
using PointlessWaymarksCmsData.Database;

namespace PointlessWaymarksCmsData.ExcelImport
{
    public static class ExcelRowImports
    {
        public static List<string> FillFromExcel<T>(T toFill, ExcelHeaderRow headerInfo, IXLRangeRow toProcess)
        {
            var skipColumns = new List<string>
            {
                "contentid",
                "id",
                "contentversion",
                "lastupdatedon",
                "originalfilename"
            };

            var properties = typeof(T).GetProperties().ToList();

            var propertyNames = properties.Select(x => x.Name.ToLower()).ToList();
            var columnNames = headerInfo.Columns.Where(x => !string.IsNullOrWhiteSpace(x.ColumnHeader))
                .Select(x => x.ColumnHeader.TrimNullToEmpty().ToLower()).ToList();
            var namesToProcess = propertyNames.Intersect(columnNames).Where(x => !skipColumns.Contains(x)).ToList();

            //Mutate the two sources to only the valid properties
            var propertiesToUpdate = properties.Where(x => namesToProcess.Contains(x.Name.ToLower())).ToList();

            var returnString = new List<string>();

            foreach (var loopProperties in propertiesToUpdate)
                if (loopProperties.PropertyType == typeof(string))
                {
                    var excelResult = GetString(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue.TrimNullToEmpty());
                }
                else if (loopProperties.PropertyType == typeof(Guid?))
                {
                    var excelResult = GetGuid(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(Guid))
                {
                    var excelResult = GetGuid(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(DateTime?))
                {
                    var excelResult = GetDateTime(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(DateTime))
                {
                    var excelResult = GetDateTime(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(int?))
                {
                    var excelResult = GetInt(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(int))
                {
                    var excelResult = GetInt(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(bool?))
                {
                    var excelResult = GetBool(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(bool))
                {
                    var excelResult = GetBool(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toFill, excelResult.ParsedValue);
                }
                else
                {
                    returnString.Add(
                        $"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}, not a recognized type");
                }

            return returnString;
        }

        public static ExcelValueParse<bool?> GetBool(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ExcelValueParse<bool?> {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (bool.TryParse(stringValue, out var parsedValue))
                return new ExcelValueParse<bool?>
                {
                    ParsedValue = parsedValue, StringValue = stringValue, ValueParsed = true
                };

            return new ExcelValueParse<bool?> {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ExcelValueParse<DateTime?> GetDateTime(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ExcelValueParse<DateTime?>
                {
                    ParsedValue = null, StringValue = stringValue, ValueParsed = true
                };

            if (DateTime.TryParse(stringValue, out var parsedValue))
                return new ExcelValueParse<DateTime?>
                {
                    ParsedValue = parsedValue, StringValue = stringValue, ValueParsed = true
                };

            return new ExcelValueParse<DateTime?> {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ExcelValueParse<Guid?> GetGuid(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ExcelValueParse<Guid?> {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (Guid.TryParse(stringValue, out var parsedValue))
                return new ExcelValueParse<Guid?>
                {
                    ParsedValue = parsedValue, StringValue = stringValue, ValueParsed = true
                };

            return new ExcelValueParse<Guid?> {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ExcelValueParse<int?> GetInt(ExcelHeaderRow headerInfo, IXLRangeRow toProcess, string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ExcelValueParse<int?> {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (int.TryParse(stringValue, out var parsedValue))
                return new ExcelValueParse<int?>
                {
                    ParsedValue = parsedValue, StringValue = stringValue, ValueParsed = true
                };

            return new ExcelValueParse<int?> {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ExcelValueParse<string> GetString(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            return new ExcelValueParse<string>
            {
                ParsedValue = stringValue, StringValue = stringValue, ValueParsed = true
            };
        }


        public static async Task<(bool hasError, string errorNotes, dynamic processContent)> ImportContentRow(
            ExcelHeaderRow headerInfo, IXLRangeRow toProcess)
        {
            var contentId = GetGuid(headerInfo, toProcess, "contentid");

            if (contentId?.ParsedValue == null) return (true, "No ContentId Found", null);

            var db = await Db.Context();

            var dbEntry = await db.ContentFromContentId(contentId.ParsedValue.Value);

            if (dbEntry == null) return (true, $"No Db Entry for {contentId.ParsedValue} found", null);

            var errors = FillFromExcel(dbEntry, headerInfo, toProcess);

            return (errors.Any(), string.Join(Environment.NewLine, errors), dbEntry);
        }

        public static async Task<(bool hasError, string errorNotes, List<dynamic> toUpdate)>
            ImportContentRowsWithChanges(IXLRange toProcess, IProgress<string> progress)
        {
            if (toProcess == null || toProcess.Rows().Count() < 2)
                return (true, "Nothing to process", new List<dynamic>());

            var headerInfo = new ExcelHeaderRow(toProcess.Row(1));

            var errorNotes = new List<string>();
            var updateList = new List<object>();

            var db = await Db.Context();

            foreach (var loopRow in toProcess.Rows().Skip(1))
            {
                var importResult = await ImportContentRow(headerInfo, loopRow);

                if (importResult.hasError)
                {
                    errorNotes.Add(importResult.errorNotes);
                    continue;
                }

                Guid contentId = importResult.processContent.ContentId;

                var currentDbEntry = await db.ContentFromContentId(contentId);

                if (currentDbEntry == null)
                {
                    progress.Report(
                        $"Excel Row {loopRow.RowNumber()} - Title {importResult.processContent.Title} - skipping, no longer in db");
                    continue;
                }

                try
                {
                    importResult.processContent.Tags = Db.TagListCleanup(importResult.processContent.Tags);
                }
                catch
                {
                    await EventLogContext.TryWriteDiagnosticMessageToLog(
                        $"Excel Import via dynamics - Tags threw an error on ContentId {contentId} - property probably not present",
                        "Excel Import");
                }

                var compareLogic = new CompareLogic();
                var comparisonResult = compareLogic.Compare(currentDbEntry, importResult.processContent);

                if (comparisonResult.AreEqual)
                {
                    progress.Report(
                        $"Excel Row {loopRow.RowNumber()} - Content Id {currentDbEntry.Title} - no changes");
                    continue;
                }

                importResult.processContent.LastUpdatedOn = DateTime.Now;

                updateList.Add(importResult.processContent);
            }

            return (errorNotes.Any(), string.Join(Environment.NewLine, errorNotes), updateList);
        }
    }
}