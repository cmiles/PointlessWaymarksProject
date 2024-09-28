using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using Path = System.IO.Path;

namespace PointlessWaymarks.CmsTests;

public static class TestFileInfo
{
    public static FileContent GrandviewContent01 =>
        new()
        {
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            BodyContent =
                "NPS Overview Information on the Grandview Trail including a map but lacking" +
                "detailed information on the day hike 'full' loops.",
            ContentId = Guid.NewGuid(),
            CreatedBy = "GC File Test",
            CreatedOn = new DateTime(2020, 10, 6, 6, 18, 00),
            FeedOn = new DateTime(2020, 10, 6, 6, 18, 00),
            ContentVersion = Db.ContentVersionDateTime(),
            Folder = "Trails",
            PublicDownloadLink = true,
            Title = "Grandview Trail",
            ShowInMainSiteFeed = true,
            ShowInSearch = true,
            Slug = SlugTools.CreateSlug(true, "Grandview Trail"),
            Summary = "NPS Grandview Overview.",
            Tags = "grand canyon national park, grandview trail, nps",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static FileContent MapContent01 =>
        new()
        {
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            BodyContent = "A map of Ironwood Forest National Monument",
            ContentId = Guid.NewGuid(),
            CreatedBy = "File Test",
            CreatedOn = new DateTime(2020, 7, 24, 5, 55, 55),
            FeedOn = new DateTime(2020, 7, 24, 5, 55, 55),
            ContentVersion = Db.ContentVersionDateTime(),
            Folder = "Maps",
            PublicDownloadLink = true,
            Title = "Ironwood Forest National Monument Map",
            ShowInMainSiteFeed = true,
            ShowInSearch = true,
            Slug = SlugTools.CreateSlug(true, "Ironwood Forest National Monument Map"),
            Summary = "A map of Ironwood.",
            Tags = "ironwood forest national monument,map",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static string MapFilename => "AZ_IronwoodForest_NM_map.pdf";
    public static string ProclamationFilename => "ironwood_proc.pdf";
    public static string TrailInfoGrandviewFilename => "GrandviewTrail.pdf";

    public static async Task CheckForExpectedFilesAfterHtmlGeneration(FileContent newContent)
    {
        var contentDirectory = UserSettingsSingleton.CurrentSettings()
            .LocalSiteFileContentDirectory(newContent, false);
        Assert.That(contentDirectory.Exists, "Content Directory Not Found?");

        var filesInDirectory = contentDirectory.GetFiles().ToList();

        var fileFile = filesInDirectory.SingleOrDefault(x => x.Name == newContent.OriginalFileName);

        Assert.That(fileFile, Is.Not.Null, "Original File not Found in File Content Directory");

        filesInDirectory.Remove(fileFile);

        var htmlFile = filesInDirectory.SingleOrDefault(x => x.Name == $"{newContent.Slug}.html");

        Assert.That(htmlFile, Is.Not.Null, "File Content HTML File not Found");

        filesInDirectory.Remove(htmlFile);

        var jsonDataFile = UserSettingsSingleton.CurrentSettings()
            .LocalSiteContentDataDirectoryDataFile(newContent.ContentId);

        Assert.That(jsonDataFile.Exists, Is.True, "Json File not Found in File Content Directory");

        var db = await Db.Context();
        if (db.HistoricFileContents.Any(x => x.ContentId == newContent.ContentId))
        {
            var historicJsonFile = filesInDirectory.SingleOrDefault(x =>
                x.Name == $"{UserSettingsUtilities.HistoricFileContentPrefix}{newContent.ContentId}.json");

            Assert.That(historicJsonFile, Is.Not.Null, "Historic Json File not Found in File Content Directory");

            filesInDirectory.Remove(historicJsonFile);
        }

        Assert.That(filesInDirectory.Count, Is.EqualTo(0),
            $"Unexpected files in File Content Directory: {string.Join(",", filesInDirectory)}");
    }

    public static void CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(FileContent newContent)
    {
        var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newContent);
        Assert.That(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileHtmlFile(newContent);
        Assert.That(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        var expectedOriginalFileInContent =
            new FileInfo(Path.Combine(expectedDirectory.FullName, newContent.OriginalFileName));
        Assert.That(expectedOriginalFileInContent.Exists,
            $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

        var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
            expectedOriginalFileInContent.Name));
        Assert.That(expectedOriginalFileInMediaArchive.Exists,
            $"Expected to find original file in media archive file directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");
    }

    public static (bool areEqual, string comparisonNotes) CompareContent(FileContent reference,
        FileContent toCompare)
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
                MembersToIgnore =
                    ["ContentId", "ContentVersion", "Id", "OriginalFileName"]
            }
        };

        var compareResult = compareLogic.Compare(reference, toCompare);

        return (compareResult.AreEqual, compareResult.DifferencesString);
    }

    public static async Task<FileContent> FileTest(string fileName, FileContent contentReference)
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia", fileName));
        Assert.That(testFile.Exists, "Test File Found");

        var contentToSave = FileContent.CreateInstance();
        contentToSave.InjectFrom(contentReference);

        var validationReturn = await FileGenerator.Validate(contentToSave, testFile);
        Assert.That(validationReturn.HasError, Is.False, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, newContent) = await FileGenerator.SaveAndGenerateHtml(contentToSave, testFile,
            null, DebugTrackers.DebugProgressTracker());
        Assert.That(generationReturn.HasError, Is.False, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        var contentComparison = CompareContent(contentReference, newContent);
        Assert.That(contentComparison.areEqual, contentComparison.comparisonNotes);

        CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newContent);

        await CheckForExpectedFilesAfterHtmlGeneration(newContent);

        JsonTest(newContent);

        await HtmlChecks(newContent);

        return newContent;
    }

    public static async Task HtmlChecks(FileContent newFileContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileHtmlFile(newFileContent);

        Assert.That(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newFileContent);
    }

    public static void JsonTest(FileContent newContent)
    {
        //Check JSON File
        var jsonFile = UserSettingsSingleton.CurrentSettings()
            .LocalSiteContentDataDirectoryDataFile(newContent.ContentId);
        Assert.That(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<FileContentOnDiskData>(
            [jsonFile.FullName]).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported.Content);
        Assert.That(comparisonResult.AreEqual,
            $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
    }
}