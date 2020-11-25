using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.ExcelImport
{
    public static class ExcelContentImports
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

        public static ExcelValueParse<double?> GetDoubleFromExcelRow(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ExcelValueParse<double?> {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (double.TryParse(stringValue, out var parsedValue))
                return new ExcelValueParse<double?>
                {
                    ParsedValue = parsedValue, StringValue = stringValue, ValueParsed = true
                };

            return new ExcelValueParse<double?> {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ExcelValueParse<Guid?> GetGuidFromExcelRow(ExcelHeaderRow headerInfo, IXLRangeRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.SingleOrDefault(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            if (contentIdColumn == null)
                return new ExcelValueParse<Guid?> {ParsedValue = null, StringValue = null, ValueParsed = false};

            var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), contentIdColumn.ExcelSheetColumn).Value
                .ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ExcelValueParse<Guid?> {ParsedValue = null, StringValue = string.Empty, ValueParsed = true};

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

        public static List<ExcelValueParse<PointDetail>> GetPointDetails(ExcelHeaderRow headerInfo,
            IXLRangeRow toProcess)
        {
            var contentColumns = headerInfo.Columns.Where(x => x.ColumnHeader.StartsWith("PointDetail"));

            var returnList = new List<ExcelValueParse<PointDetail>>();

            foreach (var loopColumns in contentColumns)
            {
                var stringValue = toProcess.Worksheet.Cell(toProcess.RowNumber(), loopColumns.ExcelSheetColumn)
                    .GetString();

                if (string.IsNullOrWhiteSpace(stringValue)) continue;

                var toAdd = new ExcelValueParse<PointDetail> {StringValue = stringValue};
                returnList.Add(toAdd);


                var splitList = stringValue.RemoveNewLines().TrimNullToEmpty().Split("||")
                    .Select(x => x.TrimNullToEmpty()).ToList();

                if (splitList.Count != 3)
                {
                    toAdd.ParsedValue = null;
                    toAdd.ValueParsed = false;
                    continue;
                }

                PointDetail pointDetail;

                //
                // Content Id - new or db retrieved PointDetail()
                //
                if (splitList[0].Length <= 10 || !splitList[0].StartsWith("ContentId:"))
                {
                    pointDetail = new PointDetail {ContentId = Guid.NewGuid(), CreatedOn = DateTime.Now};
                }
                else
                {
                    var contentIdString = splitList[0].Substring(10, splitList[0].Length - 10).TrimNullToEmpty();

                    if (!Guid.TryParse(contentIdString, out var contentId))
                    {
                        toAdd.ParsedValue = null;
                        toAdd.ValueParsed = false;
                        continue;
                    }

                    var db = Db.Context().Result;
                    var possiblePoint = db.PointDetails.Single(x => x.ContentId == contentId);

                    //Content Id specified but no db entry - error, exit
                    if (possiblePoint == null)
                    {
                        toAdd.ParsedValue = null;
                        toAdd.ValueParsed = false;
                        continue;
                    }

                    pointDetail = possiblePoint;
                    pointDetail.LastUpdatedOn = DateTime.Now;
                }

                //
                //Get the data type first so it can be used to create a new point if needed
                //
                if (splitList[1].Length <= 5 || !splitList[1].StartsWith("Type:"))
                {
                    toAdd.ParsedValue = null;
                    toAdd.ValueParsed = false;
                    continue;
                }

                var dataTypeString = splitList[1].Substring(5, splitList[1].Length - 5).TrimNullToEmpty();

                if (!Db.PointDetailDataTypeIsValid(dataTypeString))
                {
                    toAdd.ParsedValue = null;
                    toAdd.ValueParsed = false;
                    continue;
                }

                pointDetail.DataType = dataTypeString;


                //
                // Point Detail Data
                //
                if (splitList[2].Length <= 5 || !splitList[2].StartsWith("Data:"))
                {
                    //Empty Data - error
                    toAdd.ParsedValue = null;
                    toAdd.ValueParsed = false;
                    continue;
                }

                try
                {
                    var jsonString = splitList[2].Substring(5, splitList[2].Length - 5);
                    var detailData = Db.PointDetailDataFromIdentifierAndJson(dataTypeString, jsonString);
                    var validationResult = detailData.Validate();

                    if (!validationResult.isValid)
                    {
                        toAdd.ParsedValue = null;
                        toAdd.ValueParsed = false;
                        continue;
                    }

                    pointDetail.StructuredDataAsJson = jsonString;
                }
                catch
                {
                    toAdd.ParsedValue = null;
                    toAdd.ValueParsed = false;
                    continue;
                }

                toAdd.ParsedValue = pointDetail;
                toAdd.ValueParsed = true;
            }

            return returnList;
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

            dynamic dbEntry;

            if (contentId?.ParsedValue == null)
            {
                var newContentType = GetStringFromExcelRow(headerInfo, toProcess, "newcontenttype");

                if (string.IsNullOrWhiteSpace(newContentType.ParsedValue))
                    return (true, "No ContentId or NewContentId Found", null);

                dbEntry = NewContentTypeToImportDbType(newContentType.ParsedValue);
                dbEntry.ContentId = Guid.NewGuid();
            }
            else
            {
                var db = await Db.Context();

                dbEntry = await db.ContentFromContentId(contentId.ParsedValue.Value);
            }

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

            var lastRow = toProcess.Rows().Last().RowNumber();

            progress?.Report($"{lastRow} to Process");

            foreach (var loopRow in toProcess.Rows().Skip(1))
            {
                var importResult = await ImportContentFromExcelRow(headerInfo, loopRow);

                if (importResult.hasError)
                {
                    errorNotes.Add($"Excel Row {loopRow.RowNumber()} - {importResult.errorNotes}");
                    continue;
                }

                try
                {
                    Db.DefaultPropertyCleanup(importResult.processContent);
                    importResult.processContent.Tags = Db.TagListCleanup(importResult.processContent.Tags);
                }
                catch
                {
                    await EventLogContext.TryWriteDiagnosticMessageToLog(
                        $"Excel Row {loopRow.RowNumber()} - Excel Import via dynamics - Tags threw an error on ContentId {importResult.processContent.ContentId ?? "New Entry"} - property probably not present",
                        "Excel Import");
                    continue;
                }

                Guid contentId = importResult.processContent.ContentId;
                int contentDbId = importResult.processContent.Id;

                string differenceString;

                if (contentDbId > 0)
                {
                    var currentDbEntry = await db.ContentFromContentId(contentId);

                    var compareLogic = new CompareLogic
                    {
                        Config = {MembersToIgnore = new List<string> {"LastUpdatedBy"}, MaxDifferences = 100}
                    };
                    ComparisonResult comparisonResult =
                        compareLogic.Compare(currentDbEntry, importResult.processContent);

                    if (comparisonResult.AreEqual)
                    {
                        progress?.Report(
                            $"Excel Row {loopRow.RowNumber()} of {lastRow} - No Changes - Title: {currentDbEntry.Title}");
                        continue;
                    }

                    var friendlyReport = new UserFriendlyReport();
                    differenceString = friendlyReport.OutputString(comparisonResult.Differences);

                    importResult.processContent.LastUpdatedOn = DateTime.Now;
                }
                else
                {
                    differenceString = "New Entry";
                }

                GenerationReturn validationResult;

                switch (importResult.processContent)
                {
                    case PhotoContent p:
                        validationResult = await PhotoGenerator.Validate(p,
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(p));
                        break;
                    case FileContent f:
                        validationResult = await FileGenerator.Validate(f,
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileContentFile(f));
                        break;
                    case ImageContent i:
                        validationResult = await ImageGenerator.Validate(i,
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(i));
                        break;
                    case PointContentDto pc:
                        validationResult = await PointGenerator.Validate(pc);
                        break;
                    case PostContent pc:
                        validationResult = await PostGenerator.Validate(pc);
                        break;
                    case LinkContent l:
                        validationResult = await LinkGenerator.Validate(l);
                        break;
                    case NoteContent n:
                        validationResult = await NoteGenerator.Validate(n);
                        break;
                    default:
                        validationResult =
                            await GenerationReturn.Error("Excel Import - No Content Type Generator found?");
                        break;
                }

                if (validationResult.HasError)
                {
                    errorNotes.Add($"Excel Row {loopRow.RowNumber()} - {validationResult.GenerationNote}");
                    progress?.Report($"Excel Row {loopRow.RowNumber()} of {lastRow} - Validation Error.");
                    continue;
                }

                updateList.Add(new ExcelImportContentUpdateSuggestion
                {
                    DifferenceNotes = differenceString,
                    Title = importResult.processContent.Title,
                    ToUpdate = importResult.processContent
                });

                progress?.Report(
                    $"Excel Row {loopRow.RowNumber()} of {lastRow} - Adding To Changed List ({updateList.Count}) - Title: {importResult.processContent.Title}");
            }

            if (!errorNotes.Any())
            {
                var internalContentIdDuplicates = updateList.Select(x => x.ToUpdate).GroupBy(x => x.ContentId)
                    .Where(x => x.Count() > 1).Select(x => x.Key).Cast<Guid>().ToList();

                if (internalContentIdDuplicates.Any())
                {
                    return new ExcelContentTableImportResults
                    {
                        HasError = true,
                        ErrorNotes = $"Content Ids can only appear once in an update list - {string.Join(", ", internalContentIdDuplicates)}",
                        ToUpdate = updateList
                    };
                }

                var internalSlugDuplicates = updateList.Select(x => x.ToUpdate).Where(x => !(x is LinkContent)).GroupBy(x => x.Slug).Where(x => x.Count() > 1).Select(x => x.Key).Cast<string>().ToList();

                if (internalSlugDuplicates.Any())
                {
                    return new ExcelContentTableImportResults
                    {
                        HasError = true,
                        ErrorNotes = $"This import appears to create duplicate slugs - {string.Join(", ", internalSlugDuplicates)}",
                        ToUpdate = updateList
                    };
                }
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

        private static dynamic NewContentTypeToImportDbType(string newContentTypeString)
        {
            switch (newContentTypeString.ToLower())
            {
                case "file":
                    return new FileContent();
                case "image":
                    return new ImageContent();
                case "link":
                    return new LinkContent();
                case "note":
                    return new NoteContent();
                case "photo":
                    return new PhotoContent();
                case "point":
                    return new PointContentDto();
                case "post":
                    return new PostContent();
                default:
                    return null;
            }
        }

        public static async Task<(bool hasError, string errorMessage)> SaveAndGenerateHtmlFromExcelImport(
            ExcelContentTableImportResults contentTableImportResult, IProgress<string> progress)
        {
            var errorList = new List<string>();

            var totalToUpdate = contentTableImportResult.ToUpdate.Count;
            var currentUpdate = 0;

            foreach (var loopUpdates in contentTableImportResult.ToUpdate)
            {
                currentUpdate++;

                progress?.Report($"Excel Import Update {currentUpdate} of {totalToUpdate}");

                GenerationReturn generationResult;
                switch (loopUpdates.ToUpdate)
                {
                    case PhotoContent photo:
                    {
                        generationResult = (await PhotoGenerator.SaveAndGenerateHtml(photo,
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(photo), false,
                            null, progress)).generationReturn;
                        break;
                    }
                    case FileContent file:
                    {
                        generationResult = (await FileGenerator.SaveAndGenerateHtml(file,
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileContentFile(file), false, null,
                            progress)).generationReturn;
                        break;
                    }
                    case ImageContent image:
                    {
                        generationResult = (await ImageGenerator.SaveAndGenerateHtml(image,
                            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(image), false,
                            null, progress)).generationReturn;
                        break;
                    }
                    case PointContentDto point:
                    {
                        generationResult = (await PointGenerator.SaveAndGenerateHtml(point, null, progress)).generationReturn;
                        break;
                    }
                    case PostContent post:
                    {
                        generationResult = (await PostGenerator.SaveAndGenerateHtml(post, null, progress))
                            .generationReturn;
                        break;
                    }
                    case NoteContent note:
                    {
                        generationResult = (await NoteGenerator.SaveAndGenerateHtml(note, null, progress))
                            .generationReturn;
                        break;
                    }
                    case LinkContent link:
                    {
                        generationResult = (await LinkGenerator.SaveAndGenerateHtml(link, null, progress))
                            .generationReturn;
                        break;
                    }
                    default:
                        generationResult =
                            await GenerationReturn.Error("Excel Import - No Content Type Generator found?");
                        break;
                }

                if (!generationResult.HasError)
                    progress?.Report(
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
                "originalfilename",
                "pointdetail"
            };
            // ReSharper restore StringLiteralTypo

            var properties = typeof(T).GetProperties().ToList();

            var propertyNames = properties.Select(x => x.Name.ToLower()).ToList();
            var columnNames = headerInfo.Columns.Where(x => !string.IsNullOrWhiteSpace(x.ColumnHeader))
                .Select(x => x.ColumnHeader.TrimNullToEmpty().ToLower()).ToList();
            var namesToProcess = propertyNames.Intersect(columnNames).Where(x => !skipColumns.Any(x.StartsWith))
                .ToList();

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
                else if (loopProperties.PropertyType == typeof(double?))
                {
                    var excelResult = GetDoubleFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowNumber()} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(double))
                {
                    var excelResult = GetDoubleFromExcelRow(headerInfo, toProcess, loopProperties.Name);

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

            if (toUpdate is PointContentDto pointDto)
            {
                var excelResult = GetPointDetails(headerInfo, toProcess);

                if (excelResult.Any(x =>
                    x.ValueParsed == null) || excelResult.Any(x => !x.ValueParsed.Value) ||
                    excelResult.Any(x => x.ParsedValue == null))
                    returnString.Add($"Row {toProcess.RowNumber()} - could not process Point Details");
                else
                {
                    pointDto.PointDetails = excelResult.Select(x => x.ParsedValue).ToList();
                    pointDto.PointDetails.ForEach(x => x.PointContentId = pointDto.ContentId);
                }
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