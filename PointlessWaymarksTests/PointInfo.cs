using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksTests
{
    public static class IronwoodPointInfo
    {
        public static PointContent WikiQuotePointContent01 =>
            new PointContent
            {
                Title = "Yuma Point",
                Slug = "yuma-point",
                BodyContent = @"West of Hermit's Rest in Grand Canyon National Park.",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                ContentId = Guid.NewGuid(),
                CreatedBy = "Point Test",
                CreatedOn = new DateTime(2020, 9, 18, 7, 16, 16),
                Folder = "GrandCanyon",
                ShowInMainSiteFeed = false,
                Summary = "A named point on the South Rim of the Grand Canyon",
                Tags = "grand canyon,yuma point",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
                Latitude = 36.079982,
                Longitude = -112.229061
            };

        public static async Task CheckForExpectedFilesAfterHtmlGeneration(PointContentDto newContent)
        {
            var contentDirectory = UserSettingsSingleton.CurrentSettings()
                .LocalSitePointContentDirectory(newContent, false);
            Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

            var filesInDirectory = contentDirectory.GetFiles().ToList();

            Assert.True(filesInDirectory.Any(x => x.Name == $"{newContent.Slug}.html"),
                "Point Html file not found in Content Directory");

            Assert.True(filesInDirectory.Any(x => x.Name == $"{Names.PointContentPrefix}{newContent.ContentId}.json"),
                "Point Json file not found in Content Directory");

            if (newContent.PointDetails.Any())
                Assert.True(
                    filesInDirectory.Any(x =>
                        x.Name == $"{Names.PointDetailsContentPrefix}{newContent.ContentId}.json"),
                    "Point Details Json file not found in Content Directory");
            else
                Assert.True(
                    filesInDirectory.All(x =>
                        x.Name != $"{Names.PointDetailsContentPrefix}{newContent.ContentId}.json"),
                    "Point Details Json file not found with no Details Entry?");

            var db = await Db.Context();
            if (db.HistoricPointContents.Any(x => x.ContentId == newContent.ContentId))
            {
                var historicJsonFile = filesInDirectory.SingleOrDefault(x =>
                    x.Name == $"{Names.HistoricPointContentPrefix}{newContent.ContentId}.json");

                Assert.NotNull(historicJsonFile, "Historic Point Json File not Found in Content Directory");
            }

            //TODO:Check point details
        }

        public static (bool areEqual, string comparisonNotes) CompareContent(PointContentDto reference,
            PointContentDto toCompare)
        {
            Db.DefaultPropertyCleanup(reference);
            reference.Tags = Db.TagListCleanup(reference.Tags);
            if (string.IsNullOrWhiteSpace(reference.CreatedBy))
                reference.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;

            Db.DefaultPropertyCleanup(toCompare);
            toCompare.Tags = Db.TagListCleanup(toCompare.Tags);

            var compareLogic = new CompareLogic
            {
                Config = {MembersToIgnore = new List<string> {"ContentId", "ContentVersion", "Id", "PointDetails"}}
            };

            var pointCompareResult = compareLogic.Compare(reference, toCompare);

            if (!pointCompareResult.AreEqual || (!reference.PointDetails.Any() && !toCompare.PointDetails.Any()))
                return (pointCompareResult.AreEqual, pointCompareResult.DifferencesString);

            var referenceDetailDataTypeList = reference.PointDetails.Select(x => x.DataType).OrderBy(x => x).ToList();
            var toCompareDetailDataTypeList = toCompare.PointDetails.Select(x => x.DataType).OrderBy(x => x).ToList();

            if (!referenceDetailDataTypeList.SequenceEqual(toCompareDetailDataTypeList))
                return (false, "Detail Data Type - Lists are not the Same");

            foreach (var loopDetails in reference.PointDetails)
            {
                var possibleMatch = toCompare.PointDetails.Where(x =>
                    x.DataType == loopDetails.DataType && x.StructuredDataAsJson == loopDetails.StructuredDataAsJson);

                if (possibleMatch.Count() != 1)
                    return (false, $"Failed to match {loopDetails.DataType} entry in Reference Point Details");
            }

            return (true, "Apparent Match");
        }

        public static async Task HtmlChecks(PointContent newContent)
        {
            var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSitePointHtmlFile(newContent);

            Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

            var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

            await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);
        }

        public static void JsonTest(PointContent newContent)
        {
            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSitePointContentDirectory(newContent).FullName,
                    $"{Names.PointContentPrefix}{newContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<PointContent>(
                new List<string> {jsonFile.FullName}, Names.PointContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
        }
        public static async Task<PointContent> PointTest(PointContentDto contentReference)
        {
            var validationReturn = await PointGenerator.Validate(contentReference);
            Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

            var (generationReturn, newContent) =
                await PointGenerator.SaveAndGenerateHtml(contentReference, DebugTrackers.DebugProgressTracker());
            Assert.False(generationReturn.HasError, $"Unexpected Save Error - {generationReturn.GenerationNote}");

            var contentComparison = CompareContent(contentReference, newContent);
            Assert.True(contentComparison.areEqual, contentComparison.comparisonNotes);

            await CheckForExpectedFilesAfterHtmlGeneration(newContent);

            JsonTest(newContent);

            await HtmlChecks(newContent);

            return newContent;
        }
    }
}