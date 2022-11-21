using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.LoggingTools;

namespace PointlessWaymarks.CmsTests;

public static class IronwoodImageInfo
{
    public static ImageContent MapContent01 =>
        new()
        {
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            BodyContent = "Cover page from A map of Ironwood Forest National Monument",
            ContentId = Guid.NewGuid(),
            CreatedBy = "Image Test",
            CreatedOn = new DateTime(2020, 7, 25, 5, 55, 55),
            FeedOn = new DateTime(2020, 7, 25, 5, 55, 55),
            Folder = "Maps",
            Title = "Ironwood Forest National Monument Map Cover Page",
            ShowInMainSiteFeed = false,
            ShowInSearch = false,
            Slug = SlugTools.CreateSlug(true, "Ironwood Forest National Monument Map Cover Page"),
            Summary = "Cover Page From Ironwood Forest National Monument Map.",
            Tags = "ironwood forest national monument,map",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static string MapFilename => "AZ_IronwoodForest_NM_map-CoverPage.jpg";

    public static int MapWidth => 1200;

    public static void CheckFileCountAndPictureAssetsAfterHtmlGeneration(ImageContent newContent, int originalWidth)
    {
        var contentDirectory = UserSettingsSingleton.CurrentSettings()
            .LocalSiteImageContentDirectory(newContent, false);
        Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

        var expectedNumberOfFiles = PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < originalWidth) //
                                    + 1 //Original image
                                    + 1 //Display image
                                    + 1 //HTML file
                                    + 1; //json file
        Assert.AreEqual(contentDirectory.GetFiles().Length, expectedNumberOfFiles,
            "Expected Number of Files Does Not Match");

        var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newContent.ContentId);
        var pictureAssetDbEntry = (ImageContent)pictureAssetInformation.DbEntry;
        Assert.IsTrue(pictureAssetDbEntry.ContentId == newContent.ContentId,
            $"Picture Asset appears to have gotten an incorrect DB entry of {pictureAssetDbEntry.ContentId} rather than {newContent.ContentId}");

        var maxSize = PictureResizing.SrcSetSizeAndQualityList().Where(x => x.size < originalWidth).Max();
        var minSize = PictureResizing.SrcSetSizeAndQualityList().Min();

        Assert.AreEqual(pictureAssetInformation.LargePicture.Width, maxSize.size,
            $"Picture Asset Large Width is not the expected Value - Expected {maxSize}, Actual {pictureAssetInformation.LargePicture.Width}");
        Assert.AreEqual(pictureAssetInformation.SmallPicture.Width,
            PictureResizing.SrcSetSizeAndQualityList().Min().size,
            $"Picture Asset Small Width is not the expected Value - Expected {minSize}, Actual {pictureAssetInformation.SmallPicture.Width}");

        Assert.AreEqual(pictureAssetInformation.SrcsetImages.Count,
            PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < originalWidth),
            "Did not find the expected number of SrcSet Images");
    }

    public static void CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(ImageContent newContent)
    {
        var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newContent);
        Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteImageHtmlFile(newContent);
        Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        var expectedOriginalFileInContent =
            new FileInfo(Path.Combine(expectedDirectory.FullName, newContent.OriginalFileName));
        Assert.IsTrue(expectedOriginalFileInContent.Exists,
            $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

        var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
            expectedOriginalFileInContent.Name));
        Assert.IsTrue(expectedOriginalFileInMediaArchive.Exists,
            $"Expected to find original file in media archive directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");
    }

    public static (bool areEqual, string comparisonNotes) CompareContent(ImageContent reference,
        ImageContent toCompare)
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
                MembersToIgnore = new List<string>
                {
                    "ContentId",
                    "ContentVersion",
                    "Id",
                    "OriginalFileName",
                    "MainPicture"
                }
            }
        };

        var compareResult = compareLogic.Compare(reference, toCompare);

        return (compareResult.AreEqual, compareResult.DifferencesString);
    }

    public static async Task HtmlChecks(ImageContent newContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteImageHtmlFile(newContent);

        Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);

        //Todo: Continue checking...
    }

    public static async Task<ImageContent> ImageTest(string fileName, ImageContent contentReference, int width)
    {
        var originalFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia", fileName));
        Assert.True(originalFile.Exists, "Test File Found");

        var contentToSave = new ImageContent();
        contentToSave.InjectFrom(contentReference);

        var validationReturn = await ImageGenerator.Validate(contentToSave, originalFile);
        Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, newContent) = await ImageGenerator.SaveAndGenerateHtml(contentToSave, originalFile,
            true, null, DebugTrackers.DebugProgressTracker());
        Assert.False(generationReturn.HasError, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        var contentComparison = CompareContent(contentReference, newContent);
        Assert.True(contentComparison.areEqual, contentComparison.comparisonNotes);

        Assert.IsTrue(newContent.MainPicture == newContent.ContentId,
            $"Main Picture - {newContent.MainPicture} - Should be set to Content Id {newContent.ContentId}");

        CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newContent);

        CheckFileCountAndPictureAssetsAfterHtmlGeneration(newContent, width);

        JsonTest(newContent);

        await HtmlChecks(newContent);

        return newContent;
    }

    public static void JsonTest(ImageContent newContent)
    {
        //Check JSON File
        var jsonFile =
            new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newContent).FullName,
                $"{Names.ImageContentPrefix}{newContent.ContentId}.json"));
        Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<ImageContent>(
            new List<string> { jsonFile.FullName }, Names.ImageContentPrefix).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
        Assert.True(comparisonResult.AreEqual,
            $"Json Import does not match expected Content {comparisonResult.DifferencesString}");
    }
}