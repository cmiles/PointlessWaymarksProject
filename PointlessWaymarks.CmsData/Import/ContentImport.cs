using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using Microsoft.Office.Interop.Excel;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.ExcelInteropExtensions;
using Serilog;
using Range = Microsoft.Office.Interop.Excel.Range;

namespace PointlessWaymarks.CmsData.Import
{
    public static class ContentImport
    {
        public static ContentImportValueParse<bool?> GetBoolFromExcelRow(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Values[contentIdColumn.ColumnNumber];

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ContentImportValueParse<bool?>
                    {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (bool.TryParse(stringValue, out var parsedValue))
                return new ContentImportValueParse<bool?>
                {
                    ParsedValue = parsedValue,
                    StringValue = stringValue,
                    ValueParsed = true
                };

            return new ContentImportValueParse<bool?>
                {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ContentImportValueParse<DateTime?> GetDateTimeFromExcelRow(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess, string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Values[contentIdColumn.ColumnNumber];

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ContentImportValueParse<DateTime?>
                {
                    ParsedValue = null,
                    StringValue = stringValue,
                    ValueParsed = true
                };

            if (DateTime.TryParse(stringValue, out var parsedValue))
                return new ContentImportValueParse<DateTime?>
                {
                    ParsedValue = parsedValue,
                    StringValue = stringValue,
                    ValueParsed = true
                };

            return new ContentImportValueParse<DateTime?>
                {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ContentImportValueParse<double?> GetDoubleFromExcelRow(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Values[contentIdColumn.ColumnNumber];

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ContentImportValueParse<double?>
                    {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (double.TryParse(stringValue, out var parsedValue))
                return new ContentImportValueParse<double?>
                {
                    ParsedValue = parsedValue,
                    StringValue = stringValue,
                    ValueParsed = true
                };

            return new ContentImportValueParse<double?>
                {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ContentImportValueParse<Guid?> GetGuidFromExcelRow(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.SingleOrDefault(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            if (contentIdColumn == null)
                return new ContentImportValueParse<Guid?> {ParsedValue = null, StringValue = null, ValueParsed = false};

            var stringValue = toProcess.Values[contentIdColumn.ColumnNumber];

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ContentImportValueParse<Guid?>
                    {ParsedValue = null, StringValue = string.Empty, ValueParsed = true};

            if (Guid.TryParse(stringValue, out var parsedValue))
                return new ContentImportValueParse<Guid?>
                {
                    ParsedValue = parsedValue,
                    StringValue = stringValue,
                    ValueParsed = true
                };

            return new ContentImportValueParse<Guid?>
                {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static ContentImportValueParse<int?> GetIntFromExcelRow(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Values[contentIdColumn.ColumnNumber];

            if (string.IsNullOrWhiteSpace(stringValue))
                return new ContentImportValueParse<int?>
                    {ParsedValue = null, StringValue = stringValue, ValueParsed = true};

            if (int.TryParse(stringValue, out var parsedValue))
                return new ContentImportValueParse<int?>
                {
                    ParsedValue = parsedValue,
                    StringValue = stringValue,
                    ValueParsed = true
                };

            return new ContentImportValueParse<int?>
                {ParsedValue = null, StringValue = stringValue, ValueParsed = false};
        }

        public static List<ContentImportValueParse<PointDetail?>> GetPointDetails(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess)
        {
            var contentColumns =
                headerInfo.Columns.Where(x => x.ColumnHeader != null && x.ColumnHeader.StartsWith("PointDetail"));

            var returnList = new List<ContentImportValueParse<PointDetail?>>();

            foreach (var loopColumns in contentColumns)
            {
                var stringValue = toProcess.Values[loopColumns.ColumnNumber];

                if (string.IsNullOrWhiteSpace(stringValue)) continue;

                var toAdd = new ContentImportValueParse<PointDetail?> {StringValue = stringValue};
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
                    var contentIdString = splitList[0][10..].TrimNullToEmpty();

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

                var dataTypeString = splitList[1][5..].TrimNullToEmpty();

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
                    var jsonString = splitList[2][5..];
                    var detailData = Db.PointDetailDataFromIdentifierAndJson(dataTypeString, jsonString);
                    var (isValid, _) = detailData == null ? new IsValid(false, "Null Value") : detailData.Validate();

                    if (!isValid)
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

        public static ContentImportValueParse<string> GetStringFromExcelRow(ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess,
            string columnName)
        {
            var contentIdColumn = headerInfo.Columns.Single(x => string.Equals(x.ColumnHeader,
                columnName.TrimNullToEmpty(), StringComparison.CurrentCultureIgnoreCase));

            var stringValue = toProcess.Values[contentIdColumn.ColumnNumber];

            return new ContentImportValueParse<string>
            {
                ParsedValue = stringValue.TrimNullToEmpty(),
                StringValue = stringValue,
                ValueParsed = true
            };
        }

        public static async Task<(bool hasError, string errorNotes, dynamic? processContent)> ImportContentFromExcelRow(
            ContentImportHeaderRow headerInfo, ContentImportRow toProcess)
        {
            // ReSharper disable once StringLiteralTypo
            var contentId = GetGuidFromExcelRow(headerInfo, toProcess, "contentid");

            dynamic? dbEntry;

            if (contentId.ParsedValue == null)
            {
                // ReSharper disable once StringLiteralTypo
                var newContentType = GetStringFromExcelRow(headerInfo, toProcess, "newcontenttype");

                if (string.IsNullOrWhiteSpace(newContentType.ParsedValue))
                    return (true, "No ContentId or NewContentId Found", null);

                dbEntry = NewContentTypeToImportDbType(newContentType.ParsedValue);

                if (dbEntry == null)
                    return (true, "Content Type Not Found", null);

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

        public static async Task<ContentImportResults> ImportContentTable(List<ContentImportRow>? toProcess,
            IProgress<string>? progress = null)
        {
            if (toProcess == null || toProcess.Count < 2)
                return new ContentImportResults(true, "Nothing to Process", new List<ContentImportUpdateSuggestion>());

            var headerInfo = new ContentImportHeaderRow(toProcess.First().Values);

            var errorNotes = new List<string>();
            var updateList = new List<ContentImportUpdateSuggestion>();

            var db = await Db.Context();

            var lastRow = toProcess.Count;

            progress?.Report($"{lastRow} to Process");

            foreach (var loopRow in toProcess.Skip(1))
            {
                var importResult = await ImportContentFromExcelRow(headerInfo, loopRow);

                if (importResult.hasError)
                {
                    errorNotes.Add($"Excel Row {loopRow.RowIdentifier} - {importResult.errorNotes}");
                    continue;
                }

                if (importResult.processContent == null)
                {
                    errorNotes.Add($"Excel Row {loopRow.RowIdentifier} - Unexpected Null");
                    continue;
                }

                try
                {
                    Db.DefaultPropertyCleanup(importResult.processContent);
                    importResult.processContent.Tags = Db.TagListCleanup(importResult.processContent.Tags);
                }
                catch
                {
                    Log.Warning(
                        "Excel Import - Excel Row {0} - Excel Import via dynamics - Tags threw an error on ContentId {1} - property probably not present",
                        loopRow.RowIdentifier, importResult.processContent.ContentId ?? "New Entry");
                    errorNotes.Add($"Excel Row {loopRow.RowIdentifier} - Tags could not be processed");
                    continue;
                }

                Guid contentId = importResult.processContent.ContentId;
                int contentDbId = importResult.processContent.Id;

                string differenceString;

                if (contentDbId > 0)
                {
                    var currentDbEntry = await db.ContentFromContentId(contentId);

                    if (currentDbEntry == null)
                    {
                        errorNotes.Add($"Excel Row {loopRow.RowIdentifier} - Didn't find expected DB Entry");
                        continue;
                    }

                    var compareLogic = new CompareLogic
                    {
                        Config = {MembersToIgnore = new List<string> {"LastUpdatedBy"}, MaxDifferences = 100}
                    };
                    ComparisonResult comparisonResult =
                        compareLogic.Compare(currentDbEntry, importResult.processContent);

                    if (comparisonResult.AreEqual)
                    {
                        progress?.Report(
                            $"Excel Row {loopRow.RowIdentifier} of {lastRow} - No Changes - Title: {currentDbEntry.Title}");
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

                var validationResult = importResult.processContent switch
                {
                    PhotoContent p => await PhotoGenerator.Validate(p,
                        UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(p)),
                    FileContent f => await FileGenerator.Validate(f,
                        UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileContentFile(f)),
                    ImageContent i => await ImageGenerator.Validate(i,
                        UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(i)),
                    PointContentDto pc => await PointGenerator.Validate(pc),
                    PostContent pc => await PostGenerator.Validate(pc),
                    LinkContent l => await LinkGenerator.Validate(l),
                    NoteContent n => await NoteGenerator.Validate(n),
                    _ => GenerationReturn.Error("Excel Import - No Content Type Generator found?")
                };

                if (validationResult.HasError)
                {
                    errorNotes.Add($"Excel Row {loopRow.RowIdentifier} - {validationResult.GenerationNote}");
                    progress?.Report($"Excel Row {loopRow.RowIdentifier} of {lastRow} - Validation Error.");
                    continue;
                }

                updateList.Add(new ContentImportUpdateSuggestion(importResult.processContent.Title, differenceString,
                    importResult.processContent));

                progress?.Report(
                    $"Excel Row {loopRow.RowIdentifier} of {lastRow} - Adding To Changed List ({updateList.Count}) - Title: {importResult.processContent.Title}");
            }

            if (!errorNotes.Any())
            {
                var internalContentIdDuplicates = updateList.Where(x => x.ToUpdate != null).Select(x => x.ToUpdate!)
                    .GroupBy(x => x.ContentId)
                    .Where(x => x.Count() > 1).Select(x => x.Key).Cast<Guid>().ToList();

                if (internalContentIdDuplicates.Any())
                    return new ContentImportResults(true,
                        $"Content Ids can only appear once in an update list - {string.Join(", ", internalContentIdDuplicates)}",
                        updateList);

                var internalSlugDuplicates = updateList.Where(x => x.ToUpdate != null).Select(x => x.ToUpdate!)
                    .Where(x => !(x is LinkContent))
                    .GroupBy(x => x.Slug).Where(x => x.Count() > 1).Select(x => x.Key).Cast<string>().ToList();

                if (internalSlugDuplicates.Any())
                    return new ContentImportResults(true,
                        $"This import appears to create duplicate slugs - {string.Join(", ", internalSlugDuplicates)}",
                        updateList);
            }

            return new ContentImportResults(errorNotes.Any(), string.Join(Environment.NewLine, errorNotes), updateList);
        }

        public static async Task<ContentImportResults> ImportFromFile(string fileName,
            IProgress<string>? progress = null)
        {
            progress?.Report($"Opening {fileName} for Excel Import");

            await using Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheets.First();

            var tableRange = worksheet.RangeUsed();

            var translated = new List<ContentImportRow>();

            foreach (var loopRows in tableRange.Rows())
            {
                var valuesToAdd = new List<string>();

                foreach (var loopCells in loopRows.Cells()) valuesToAdd.Add(loopCells.Value.ToString() ?? string.Empty);

                translated.Add(new ContentImportRow(valuesToAdd, $"Row {loopRows.RowNumber()}"));
            }

            progress?.Report($"Excel Import - {fileName} - Range {tableRange.RangeAddress.ToStringRelative(true)}");

            return await ImportContentTable(translated, progress);
        }

        public static async Task<ContentImportResults> ImportFromTopMostExcelInstance(
            IProgress<string>? progress = null)
        {
            progress?.Report("Getting Reference to Excel");

            var currentExcel = Session.Current.TopMost;

            if (currentExcel?.ActiveWorkbook?.ActiveSheet == null)
            {
                progress?.Report("No Active Excel Instance with an open File Found?");
                return await ImportContentTable(null, progress);
            }

            var worksheet = (Worksheet) currentExcel.ActiveWorkbook.ActiveSheet;
            var tableRange = worksheet.UsedRange;

            if (tableRange == null)
            {
                progress?.Report(
                    $"No data found in Workbook {currentExcel.ActiveWorkbook.Name} - Worksheet {worksheet.Name}?");
                return await ImportContentTable(null, progress);
            }

            if (tableRange.Columns.Count < 2 || tableRange.Rows.Count < 2)
            {
                progress?.Report(
                    $"Not enough data found in Workbook {currentExcel.ActiveWorkbook.Name} - Worksheet {worksheet.Name}?");
                return await ImportContentTable(null, progress);
            }

            progress?.Report(
                $"Excel Open File Import - Workbook {currentExcel.ActiveWorkbook.Name} - Worksheet {worksheet.Name} - Range {tableRange.Address}");

            var excelObjects = (object?[,]) tableRange.Value;

            var rowLength = excelObjects.GetLength(0);
            var columnLength = excelObjects.GetLength(1);

            var excelRow = ((Range) tableRange.Cells[1, 1]).Row;

            var translated = new List<ContentImportRow>();

            for (var r = 1; r <= rowLength; r++)
            {
                var valuesToAdd = new List<string>();
                for (var c = 1; c <= columnLength; c++) valuesToAdd.Add(excelObjects[r, c]?.ToString() ?? string.Empty);

                translated.Add(new ContentImportRow(valuesToAdd, $"Row {excelRow}"));

                excelRow++;
            }

            return await ImportContentTable(translated, progress);
        }

        private static dynamic? NewContentTypeToImportDbType(string? newContentTypeString)
        {
            if (string.IsNullOrWhiteSpace(newContentTypeString)) return null;

            return newContentTypeString.ToLower() switch
            {
                "file" => new FileContent(),
                "image" => new ImageContent(),
                "link" => new LinkContent(),
                "note" => new NoteContent(),
                "photo" => new PhotoContent(),
                "point" => new PointContentDto(),
                "post" => new PostContent(),
                _ => null
            };
        }

        public static async Task<(bool hasError, string errorMessage)> SaveAndGenerateHtmlFromExcelImport(
            ContentImportResults contentImportResult, IProgress<string>? progress = null)
        {
            var errorList = new List<string>();

            var totalToUpdate = contentImportResult.ToUpdate.Count;
            var currentUpdate = 0;

            foreach (var loopUpdates in contentImportResult.ToUpdate)
            {
                currentUpdate++;

                progress?.Report($"Excel Import Update {currentUpdate} of {totalToUpdate}");

                GenerationReturn generationResult;
                switch (loopUpdates.ToUpdate)
                {
                    case PhotoContent photo:
                    {
                        var archiveFile = UserSettingsSingleton.CurrentSettings()
                            .LocalMediaArchivePhotoContentFile(photo);

                        if (archiveFile == null || !archiveFile.Exists)
                        {
                            generationResult = GenerationReturn.Error(
                                $"Can not find media archive file for Photo Titled {photo.Title} - file: {archiveFile?.FullName}",
                                photo.ContentId);
                            break;
                        }

                        generationResult = (await PhotoGenerator.SaveAndGenerateHtml(photo,
                            archiveFile, false,
                            null, progress)).generationReturn;
                        break;
                    }
                    case FileContent file:
                    {
                        var archiveFile = UserSettingsSingleton.CurrentSettings()
                            .LocalMediaArchiveFileContentFile(file);

                        if (archiveFile == null || !archiveFile.Exists)
                        {
                            generationResult = GenerationReturn.Error(
                                $"Can not find media archive file for Photo Titled {file.Title} - file: {archiveFile?.FullName}",
                                file.ContentId);
                            break;
                        }

                        generationResult = (await FileGenerator.SaveAndGenerateHtml(file,
                            archiveFile, false, null,
                            progress)).generationReturn;
                        break;
                    }
                    case ImageContent image:
                    {
                        var archiveFile = UserSettingsSingleton.CurrentSettings()
                            .LocalMediaArchiveImageContentFile(image);

                        if (archiveFile == null || !archiveFile.Exists)
                        {
                            generationResult = GenerationReturn.Error(
                                $"Can not find media archive file for Photo Titled {image.Title} - file: {archiveFile?.FullName}",
                                image.ContentId);
                            break;
                        }

                        generationResult = (await ImageGenerator.SaveAndGenerateHtml(image,
                            archiveFile, false,
                            null, progress)).generationReturn;
                        break;
                    }
                    case PointContentDto point:
                    {
                        generationResult = (await PointGenerator.SaveAndGenerateHtml(point, null, progress))
                            .generationReturn;
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
                            GenerationReturn.Error("Excel Import - No Content Type Generator found?");
                        break;
                }

                if (!generationResult.HasError)
                    progress?.Report(
                        $"Updated Content Id {loopUpdates.ToUpdate?.ContentId} - Title {loopUpdates.Title} - Saved");
                else
                    errorList.Add(
                        $"Updating Failed: Content Id {loopUpdates} - Title {loopUpdates.Title} - Failed:{Environment.NewLine}{generationResult.GenerationNote}");
            }

            if (errorList.Any()) return (true, string.Join(Environment.NewLine, errorList));

            return (false, string.Empty);
        }

        public static List<string> UpdateContentFromExcelRow<T>(T toUpdate, ContentImportHeaderRow headerInfo,
            ContentImportRow toProcess)
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
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue.TrimNullToEmpty());
                }
                else if (loopProperties.PropertyType == typeof(Guid?))
                {
                    var excelResult = GetGuidFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(Guid))
                {
                    var excelResult = GetGuidFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(DateTime?))
                {
                    var excelResult = GetDateTimeFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(DateTime))
                {
                    var excelResult = GetDateTimeFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(double?))
                {
                    var excelResult = GetDoubleFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(double))
                {
                    var excelResult = GetDoubleFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(int?))
                {
                    var excelResult = GetIntFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(int))
                {
                    var excelResult = GetIntFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(bool?))
                {
                    var excelResult = GetBoolFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else if (loopProperties.PropertyType == typeof(bool))
                {
                    var excelResult = GetBoolFromExcelRow(headerInfo, toProcess, loopProperties.Name);

                    if (excelResult.ValueParsed == null || !excelResult.ValueParsed.Value ||
                        excelResult.ParsedValue == null)
                        returnString.Add($"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}");

                    loopProperties.SetValue(toUpdate, excelResult.ParsedValue);
                }
                else
                {
                    returnString.Add(
                        $"Row {toProcess.RowIdentifier} - could not process {loopProperties.Name}, not a recognized type");
                }

            if (toUpdate is PointContentDto pointDto)
            {
                var excelResult = GetPointDetails(headerInfo, toProcess);

                if (excelResult.Any(x => x.ValueParsed == null) || excelResult.Any(x => !x.ValueParsed!.Value) ||
                    excelResult.Any(x => x.ParsedValue == null))
                {
                    returnString.Add($"Row {toProcess.RowIdentifier} - could not process Point Details");
                }
                else
                {
                    pointDto.PointDetails = excelResult.Select(x => x.ParsedValue!).ToList();
                    pointDto.PointDetails.ForEach(x => x.PointContentId = pointDto.ContentId);
                }
            }

            return returnString;
        }

        public record ContentImportResults(bool HasError, string? ErrorNotes,
            List<ContentImportUpdateSuggestion> ToUpdate);

        public record ContentImportRow(List<string> Values, string RowIdentifier);

        public record ContentImportUpdateSuggestion(string? Title, string? DifferenceNotes, dynamic? ToUpdate);
    }
}