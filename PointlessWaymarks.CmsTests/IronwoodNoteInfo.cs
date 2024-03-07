using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsTests;

public static class IronwoodNoteInfo
{
    public static NoteContent LinkNoteContent01 =>
        new()
        {
            BodyContent = @"Some useful links for Ironwood Forest National Monument:
* [Ironwood Forest National Monument | Bureau of Land Management](https://www.blm.gov/visit/ironwood)
* [Home - Friends of Ironwood Forest](https://ironwoodforest.org/)
* [Ironwood Forest National Monument - Wikipedia](https://en.wikipedia.org/wiki/Ironwood_Forest_National_Monument)
* [Ironwood Forest](https://www.desertmuseum.org/programs/ifnm_index.php)",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            ContentId = Guid.NewGuid(),
            CreatedBy = "Note Test",
            CreatedOn = new DateTime(2020, 8, 17, 7, 17, 17),
            FeedOn = new DateTime(2020, 8, 17, 7, 17, 17),
            ContentVersion = Db.ContentVersionDateTime(),
            Folder = "IronwoodForest",
            ShowInMainSiteFeed = true,
            Summary = "Basic links for Ironwood Forest National Monument",
            Tags = "ironwood forest national monument, excluded tag",
            Slug = NoteGenerator.UniqueNoteSlug().Result
        };

    public static async Task CheckForExpectedFilesAfterHtmlGeneration(NoteContent newContent)
    {
        var contentDirectory = UserSettingsSingleton.CurrentSettings()
            .LocalSiteNoteContentDirectory(newContent, false);
        Assert.That(contentDirectory.Exists, "Content Directory Not Found?");

        var filesInDirectory = contentDirectory.GetFiles().ToList();
        var jsonDataFile = UserSettingsSingleton.CurrentSettings()
            .LocalSiteContentDataDirectoryDataFile(newContent.ContentId);

        Assert.Multiple(() =>
        {
            Assert.That(filesInDirectory.Any(x => x.Name == $"{newContent.Slug}.html"),
                    "Note Html file not found in Content Directory");

            Assert.That(jsonDataFile.Exists,
                "Note Json file not found in Content Data Directory");
        });

        var db = await Db.Context();
        if (db.HistoricNoteContents.Any(x => x.ContentId == newContent.ContentId))
        {
            var historicJsonFile = filesInDirectory.SingleOrDefault(x =>
                x.Name == $"{UserSettingsUtilities.HistoricNoteContentPrefix}{newContent.ContentId}.json");

            Assert.That(historicJsonFile, Is.Not.Null, "Historic Note Json File not Found in Content Directory");
        }
    }

    public static (bool areEqual, string comparisonNotes) CompareContent(NoteContent reference,
        NoteContent toCompare)
    {
        Db.DefaultPropertyCleanup(reference);
        reference.Tags = Db.TagListCleanup(reference.Tags);
        if (string.IsNullOrWhiteSpace(reference.CreatedBy))
            reference.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;

        Db.DefaultPropertyCleanup(toCompare);
        toCompare.Tags = Db.TagListCleanup(toCompare.Tags);

        var compareLogic = new CompareLogic
        {
            Config = { MembersToIgnore = ["ContentId", "ContentVersion", "Id"] }
        };

        var compareResult = compareLogic.Compare(reference, toCompare);

        return (compareResult.AreEqual, compareResult.DifferencesString);
    }

    public static async Task HtmlChecks(NoteContent newContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteNoteHtmlFile(newContent);

        Assert.That(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);
    }

    public static void JsonTest(NoteContent newContent)
    {
        //Check JSON File
        var jsonFile = UserSettingsSingleton.CurrentSettings()
            .LocalSiteContentDataDirectoryDataFile(newContent.ContentId);
        Assert.That(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<NoteContentOnDiskData>(
            [jsonFile.FullName]).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported.Content);
        Assert.That(comparisonResult.AreEqual,
            $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
    }

    public static async Task<NoteContent> NoteTest(NoteContent contentReference)
    {
        var contentToSave = await NoteContent.CreateInstance();
        contentToSave.InjectFrom(contentReference);

        var validationReturn = await NoteGenerator.Validate(contentToSave);
        Assert.That(validationReturn.HasError, Is.False, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, newContent) =
            await NoteGenerator.SaveAndGenerateHtml(contentToSave, null, DebugTrackers.DebugProgressTracker());
        Assert.That(generationReturn.HasError, Is.False, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        var (areEqual, comparisonNotes) = CompareContent(contentReference, newContent);
        Assert.That(areEqual, comparisonNotes);

        await CheckForExpectedFilesAfterHtmlGeneration(newContent);

        JsonTest(newContent);

        await HtmlChecks(newContent);

        return newContent;
    }
}