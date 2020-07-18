using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KellermanSoftware.CompareNetObjects;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.ExcelImport
{
    public static class ExcelRowImports
    {
        public static ExcelValueParse<bool?> GetBoolFromExcelRow(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
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

        public static ExcelValueParse<DateTime?> GetDateTimeFromExcelRow(ExcelHeaderRow headerInfo,
            IXLRangeRow toProcess, string columnName)
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

        public static ExcelValueParse<Guid?> GetGuidFromExcelRow(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
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

        public static ExcelValueParse<int?> GetIntFromExcelRow(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
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

        public static ExcelValueParse<string> GetStringFromExcelRow(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
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


        public static async Task<(bool hasError, string errorNotes, dynamic processContent)> ImportContentFromExcelRow(
            ExcelHeaderRow headerInfo, IXLRangeRow toProcess)
        {
            // ReSharper disable once StringLiteralTypo
            var contentId = GetGuidFromExcelRow(headerInfo, toProcess, "contentid");

            if (contentId?.ParsedValue == null) return (true, "No ContentId Found", null);

            var db = await Db.Context();

            var dbEntry = await db.ContentFromContentId(contentId.ParsedValue.Value);

            if (dbEntry == null) return (true, $"No Db Entry for {contentId.ParsedValue} found", null);

            var errors = UpdateContentFromExcelRow(dbEntry, headerInfo, toProcess);

            return (errors.Count > 0, string.Join(Environment.NewLine, errors), dbEntry);
        }

        public static async Task<ExcelContentTableImportResults> ImportExcelContentTable(IXLRange toProcess,
            IProgress<string> progress)
        {
            if (toProcess == null || toProcess.Rows().Count() < 2)
                return new ExcelContentTableImportResults
                {
                    HasError = true,
                    ErrorNotes = "Nothing to process",
                    ToUpdate = new List<ExcelImportContentUpdateSuggestion>()
                };

            var headerInfo = new ExcelHeaderRow(toProcess.Row(1));

            var errorNotes = new List<string>();
            var updateList = new List<ExcelImportContentUpdateSuggestion>();

            var db = await Db.Context();

            foreach (var loopRow in toProcess.Rows().Skip(1))
            {
                var importResult = await ImportContentFromExcelRow(headerInfo, loopRow);

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
                ComparisonResult comparisonResult = compareLogic.Compare(currentDbEntry, importResult.processContent);

                if (comparisonResult.AreEqual)
                {
                    progress.Report(
                        $"Excel Row {loopRow.RowNumber()} - Content Id {currentDbEntry.Title} - no changes");
                    continue;
                }

                importResult.processContent.LastUpdatedOn = DateTime.Now;

                updateList.Add(new ExcelImportContentUpdateSuggestion
                {
                    DifferenceNotes = string.Join(Environment.NewLine, comparisonResult.Differences),
                    Title = importResult.processContent.Title,
                    ToUpdate = importResult.processContent
                });
            }

            return new ExcelContentTableImportResults
            {
                HasError = errorNotes.Any(),
                ErrorNotes = string.Join(Environment.NewLine, errorNotes),
                ToUpdate = updateList
            };
        }

        public static async Task<ExcelContentTableImportResults> ImportFromFile(string fileName,
            IProgress<string> progress)
        {
            progress?.Report($"Opening {fileName} for Excel Import");

            await using Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheets.First();

            var tableRange = worksheet.RangeUsed();

            progress?.Report($"Excel Import - {fileName} - Range {tableRange.RangeAddress.ToStringRelative(true)}");

            return await ImportExcelContentTable(tableRange, progress);
        }

        public static async Task<(bool hasError, string errorMessage)> SaveAndGenerateHtmlFromExcelImport(
            ExcelContentTableImportResults contentTableImportResult, IProgress<string> progress)
        {
            var errorList = new List<string>();

            foreach (var loopUpdates in contentTableImportResult.ToUpdate)
            {
                GenerationReturn generationResult;
                switch (loopUpdates.ToUpdate)
                {
                    case PhotoContent photo:
                    {
                        var mediaArchiveFile = new FileInfo(Path.Combine(
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                            photo.OriginalFileName));
                        generationResult = (await PhotoGenerator.SaveAndGenerateHtml(photo, mediaArchiveFile, true,
                            progress)).generationReturn;
                        break;
                    }
                    case FileContent file:
                    {
                        var mediaArchiveFile = new FileInfo(Path.Combine(
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                            file.OriginalFileName));
                        generationResult = (await FileGenerator.SaveAndGenerateHtml(file, mediaArchiveFile, true,
                            progress)).generationReturn;
                        break;
                    }
                    case ImageContent image:
                    {
                        var mediaArchiveFile = new FileInfo(Path.Combine(
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
                            image.OriginalFileName));
                        generationResult = (await ImageGenerator.SaveAndGenerateHtml(image, mediaArchiveFile, true,
                            progress)).generationReturn;
                        break;
                    }
                    case PostContent post:
                    {
                        generationResult = (await PostGenerator.SaveAndGenerateHtml(post, progress)).generationReturn;
                        break;
                    }
                    case NoteContent note:
                    {
                        generationResult = (await NoteGenerator.SaveAndGenerateHtml(note, progress)).generationReturn;
                        break;
                    }
                    case LinkStream link:
                    {
                        generationResult = (await LinkGenerator.SaveAndGenerateHtml(link, progress)).generationReturn;
                        break;
                    }
                    default:
                        generationResult =
                            await GenerationReturn.Error("Excel Import - No Content Type Generator found?");
                        break;
                }

                if (!generationResult.HasError)
                    progress.Report(
                        $"Updated Content Id {loopUpdates.ToUpdate.ContentId} - Title {loopUpdates.Title} - Saved");
                else
                    errorList.Add(
                        $"Updating Failed: Content Id {loopUpdates} - Title {loopUpdates.Title} - Failed:{Environment.NewLine}{generationResult.GenerationNote}");
            }

            if (errorList.Any()) return (true, string.Join(Environment.NewLine, errorList));

            return (false, string.Empty);
        }

        public static List<string> UpdateContentFromExcelRow<T>(T toUpdate, ExcelHeaderRow headerInfo,
            IXLRangeRow toProcess)
        {
            // ReSharper disable StringLiteralTypo
            var skipColumns = new List<string>
            {
                "contentid",
                "id",
                "contentversion",
                "lastupdatedon",
                "originalfilename"
            };
            // ReSharper restore StringLiteralTypo

            var properties = typeof(T).GetProperties().ToList();

            var propertyNames = properties.Select(x => x.Name.ToLower()).ToList();
            var columnNames = headerInfo.Columns.Where(x => !string.IsNullOrWhiteSpace(x.ColumnHeader))
                .Select(x => x.ColumnHeader.TrimNullToEmpty().ToLower()).ToList();
            var namesToProcess = propertyNames.Intersect(columnNames).Where(x => !skipColumns.Contains(x)).ToList();

            var propertiesToUpdate = properties.Where(x => namesToProcess.Contains(x.Name.ToLower())).ToList();

            var returnString = new List<string>();

            foreach (var loopProperties in propertiesToUpdate)
                if (loopProperties.PropertyType == typeof(string))
                {
                    var excelResult = GetStringFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue.TrimNullToEmpty());
                }
                else if (loopProperties.PropertyType == typeof(Guid?))
                {
                    var excelResult = GetGuidFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(Guid))
                {
                    var excelResult = GetGuidFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(DateTime?))
                {
                    var excelResult = GetDateTimeFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(DateTime))
                {
                    var excelResult = GetDateTimeFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(int?))
                {
                    var excelResult = GetIntFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(int))
                {
                    var excelResult = GetIntFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(bool?))
                {
                    var excelResult = GetBoolFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(bool))
                {
                    var excelResult = GetBoolFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else
                {
                    returnString.Add(
                        $"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}, not a recognized type");
                }

            return returnString;
        }

        public class ExcelContentTableImportResults
        {
            public string ErrorNotes { get; set; }
            public bool HasError { get; set; }

            public List<ExcelImportContentUpdateSuggestion> ToUpdate { get; set; }
        }

        public class ExcelImportContentUpdateSuggestion
        {
            public string DifferenceNotes { get; set; }

            public string Title { get; set; }
            public dynamic ToUpdate { get; set; }
        }
    }
}