using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsTests;

public static class GrandCanyonPointInfo
{
    public static PointContentDto YumaPointContent01 =>
        new()
        {
            Title = "Yuma Point",
            Slug = "yuma-point",
            BodyContent = @"West of Hermit's Rest in Grand Canyon National Park.",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            ContentId = YumaPointContentId,
            CreatedBy = "Point Test",
            CreatedOn = new DateTime(2020, 9, 18, 7, 16, 16),
            FeedOn = new DateTime(2020, 9, 18, 7, 16, 16),
            Folder = "GrandCanyon",
            ShowInMainSiteFeed = false,
            Summary = "A named point on the South Rim of the Grand Canyon",
            Tags = "grand canyon,yuma point",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
            Latitude = 36.079982,
            Longitude = -112.229061,
            TextMarkerText = "vp",
            PointDetails = new List<PointDetail>
            {
                new()
                {
                    ContentId = Guid.NewGuid(),
                    PointContentId = YumaPointContentId,
                    CreatedOn = new DateTime(2020, 9, 18, 7, 16, 16),
                    DataType = "Peak",
                    StructuredDataAsJson =
                        "{\"DataTypeIdentifier\":\"Peak\",\"Notes\":\"GNIS Data...\",\"NotesContentFormat\":\"MarkdigMarkdown01\"}"
                }
            }
        };

    public static PointContentDto YumaPointContent02 =>
        new()
        {
            Title = "Yuma Point",
            Slug = "yuma-point",
            BodyContent = @"West of Hermit's Rest in Grand Canyon National Park.",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CreatedBy = "Point Test",
            CreatedOn = new DateTime(2020, 9, 18, 7, 16, 16),
            FeedOn = new DateTime(2020, 9, 18, 7, 16, 16),
            Folder = "GrandCanyon",
            ShowInMainSiteFeed = false,
            Summary =
                "A named point on the South Rim of the Grand Canyon - under the open skies and free flying helicopters.",
            Tags = "grand canyon,yuma point",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
            Latitude = 36.079982,
            Longitude = -112.229061,
            Elevation = 6630,
            TextMarkerText = "vp",
            LastUpdatedBy = "Elevation Updater",
            LastUpdatedOn = new DateTime(2020, 9, 21, 8, 20, 16),
            PointDetails = new List<PointDetail>
            {
                new()
                {
                    ContentId = Guid.NewGuid(),
                    CreatedOn = new DateTime(2020, 9, 18, 7, 16, 16),
                    PointContentId = YumaPointContentId,
                    DataType = "Peak",
                    StructuredDataAsJson =
                        "{\"DataTypeIdentifier\":\"Peak\",\"Notes\":\"Yuma Point|Cliff|AZ|04|Coconino|005|360448N|1121345W|36.0799823|-112.229061\",\"NotesContentFormat\":\"MarkdigMarkdown01\"}",
                    LastUpdatedOn = new DateTime(2020, 9, 21, 8, 20, 16)
                }
            }
        };

    public static Guid YumaPointContentId => Guid.Parse("6a32fa16-bdff-4690-ada9-0da017e99d0e");

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

        var historicPointDetails = await Db.HistoricPointDetailsForPoint(newContent.ContentId, db, 40);
        if (historicPointDetails.Any())
        {
            var historicDetailsJsonFile = filesInDirectory.SingleOrDefault(x =>
                x.Name == $"{Names.HistoricPointDetailsContentPrefix}{newContent.ContentId}.json");

            Assert.NotNull(historicDetailsJsonFile,
                "Historic Point Details Json File not Found in Content Directory");
        }
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
            Config =
            {
                MembersToIgnore = new List<string> { "ContentId", "ContentVersion", "Id", "PointDetails" }
            }
        };

        var pointCompareResult = compareLogic.Compare(reference, toCompare);

        if (!pointCompareResult.AreEqual || !reference.PointDetails.Any() && !toCompare.PointDetails.Any())
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

    public static async Task HtmlChecks(PointContentDto newContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSitePointHtmlFile(newContent);

        Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);
    }

    public static void JsonTest(PointContent newContent)
    {
        var jsonFile =
            new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSitePointContentDirectory(newContent).FullName,
                $"{Names.PointContentPrefix}{newContent.ContentId}.json"));
        Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<PointContent>(
            new List<string> { jsonFile.FullName }, Names.PointContentPrefix).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
        Assert.True(comparisonResult.AreEqual,
            $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
    }

    public static async Task<PointContentDto> PointTest(PointContentDto contentReference)
    {
        var validationReturn = await PointGenerator.Validate(contentReference);
        Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, newContent) =
            await PointGenerator.SaveAndGenerateHtml(contentReference, null, DebugTrackers.DebugProgressTracker());
        Assert.False(generationReturn.HasError, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        var contentComparison = CompareContent(contentReference, newContent);
        Assert.True(contentComparison.areEqual, contentComparison.comparisonNotes);

        await CheckForExpectedFilesAfterHtmlGeneration(newContent);

        JsonTest(Db.PointContentDtoToPointContentAndDetails(newContent).content);

        await HtmlChecks(newContent);

        return newContent;
    }
}