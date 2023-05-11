using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsTests;

public static class IronwoodVideoInfo
{
    public static VideoContent BlueSkyAndCloudsVideoContent01 =>
        new()
        {
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            BodyContent =
                "A simple video of the sky!",
            ContentId = Guid.NewGuid(),
            CreatedBy = "GC File Tester",
            CreatedOn = new DateTime(2020, 10, 6, 6, 18, 00),
            FeedOn = new DateTime(2020, 10, 6, 6, 18, 00),
            ContentVersion = Db.ContentVersionDateTime(),
            Folder = "2023",
            License = "Public Domain",
            ShowInMainSiteFeed = true,
            Slug = "2023-january-blue-sky-and-clouds-video",
            Summary = "2023 January Blue Sky and Clouds Video.",
            Tags = "sky",
            Title = "2023 January Blue Sky and Clouds Video",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
            VideoCreatedBy = "Anonymous",
            VideoCreatedOn = new DateTime(2023, 1, 24, 8, 13, 45)

        };
    
    public static string SkyFilename => "Blue-Sky-and-Clouds-Video.mp4";

    public static async Task CheckForExpectedFilesAfterHtmlGeneration(VideoContent newContent)
    {
        var contentDirectory = UserSettingsSingleton.CurrentSettings()
            .LocalSiteVideoContentDirectory(newContent, false);
        Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

        var filesInDirectory = contentDirectory.GetFiles().ToList();

        var fileFile = filesInDirectory.SingleOrDefault(x => x.Name == newContent.OriginalFileName);

        Assert.NotNull(fileFile, "Original File not Found in Video Content Directory");

        filesInDirectory.Remove(fileFile);

        var htmlFile = filesInDirectory.SingleOrDefault(x => x.Name == $"{newContent.Slug}.html");

        Assert.NotNull(htmlFile, "Video Content HTML File not Found");

        filesInDirectory.Remove(htmlFile);

        var jsonFile =
            filesInDirectory.SingleOrDefault(x =>
                x.Name == $"{Names.VideoContentPrefix}{newContent.ContentId}.json");

        Assert.NotNull(jsonFile, "Json File not Found in Video Content Directory");

        filesInDirectory.Remove(jsonFile);

        var db = await Db.Context();
        if (db.HistoricFileContents.Any(x => x.ContentId == newContent.ContentId))
        {
            var historicJsonFile = filesInDirectory.SingleOrDefault(x =>
                x.Name == $"{Names.HistoricVideoContentPrefix}{newContent.ContentId}.json");

            Assert.NotNull(historicJsonFile, "Historic Json File not Found in Video Content Directory");

            filesInDirectory.Remove(historicJsonFile);
        }

        Assert.AreEqual(0, filesInDirectory.Count,
            $"Unexpected files in Video Content Directory: {string.Join(",", filesInDirectory)}");
    }

    public static void CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(VideoContent newContent)
    {
        var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteVideoContentDirectory(newContent);
        Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteVideoHtmlFile(newContent);
        Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        var expectedOriginalFileInContent =
            new FileInfo(Path.Combine(expectedDirectory.FullName, newContent.OriginalFileName));
        Assert.IsTrue(expectedOriginalFileInContent.Exists,
            $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

        var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveVideoDirectory().FullName,
            expectedOriginalFileInContent.Name));
        Assert.IsTrue(expectedOriginalFileInMediaArchive.Exists,
            $"Expected to find original file in media archive file directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");
    }

    public static (bool areEqual, string comparisonNotes) CompareContent(VideoContent reference,
        VideoContent toCompare)
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
                    new List<string> { "ContentId", "ContentVersion", "Id", "OriginalFileName" }
            }
        };

        var compareResult = compareLogic.Compare(reference, toCompare);

        return (compareResult.AreEqual, compareResult.DifferencesString);
    }

    public static async Task<VideoContent> VideoTest(string fileName, VideoContent contentReference)
    {
        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia", fileName));
        Assert.True(testFile.Exists, "Test File Found");

        var contentToSave = VideoContent.CreateInstance();
        contentToSave.InjectFrom(contentReference);

        var validationReturn = await VideoGenerator.Validate(contentToSave, testFile);
        Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, newContent) = await VideoGenerator.SaveAndGenerateHtml(contentToSave, testFile, true,
            null, DebugTrackers.DebugProgressTracker());
        Assert.False(generationReturn.HasError, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        var contentComparison = CompareContent(contentReference, newContent);
        Assert.True(contentComparison.areEqual, contentComparison.comparisonNotes);

        CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newContent);

        await CheckForExpectedFilesAfterHtmlGeneration(newContent);

        JsonTest(newContent);

        await HtmlChecks(newContent);

        return newContent;
    }

    public static async Task HtmlChecks(VideoContent newVideoContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteVideoHtmlFile(newVideoContent);

        Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newVideoContent);
    }

    public static void JsonTest(VideoContent newContent)
    {
        //Check JSON File
        var jsonFile =
            new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteVideoContentDirectory(newContent).FullName,
                $"{Names.VideoContentPrefix}{newContent.ContentId}.json"));
        Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<VideoContent>(
            new List<string> { jsonFile.FullName }, Names.VideoContentPrefix).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
        Assert.True(comparisonResult.AreEqual,
            $"Json Import does not match expected Video Content {comparisonResult.DifferencesString}");
    }
}