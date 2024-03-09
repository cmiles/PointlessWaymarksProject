using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.ImageHelpers;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;

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
            ContentVersion = Db.ContentVersionDateTime(),
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
        Assert.That(contentDirectory.Exists, "Content Directory Not Found?");

        var expectedNumberOfFiles = PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < originalWidth) //
                                    + 1 //Original image
                                    + 1 //Display image
                                    + 1; //HTML file
        Assert.That(expectedNumberOfFiles, Is.EqualTo(contentDirectory.GetFiles().Length),
            "Expected Number of Files Does Not Match");

        var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newContent.ContentId);
        var pictureAssetDbEntry = (ImageContent)pictureAssetInformation.DbEntry;
        Assert.That(pictureAssetDbEntry.ContentId == newContent.ContentId,
            $"Picture Asset appears to have gotten an incorrect DB entry of {pictureAssetDbEntry.ContentId} rather than {newContent.ContentId}");

        var maxSize = PictureResizing.SrcSetSizeAndQualityList().Where(x => x.size < originalWidth).Max();
        var minSize = PictureResizing.SrcSetSizeAndQualityList().Min();

        Assert.Multiple(() =>
        {
            Assert.That(maxSize.size, Is.EqualTo(pictureAssetInformation.LargePicture.Width),
                    $"Picture Asset Large Width is not the expected Value - Expected {maxSize}, Actual {pictureAssetInformation.LargePicture.Width}");
            Assert.That(PictureResizing.SrcSetSizeAndQualityList().Min().size, Is.EqualTo(pictureAssetInformation.SmallPicture.Width),
                $"Picture Asset Small Width is not the expected Value - Expected {minSize}, Actual {pictureAssetInformation.SmallPicture.Width}");

            Assert.That(PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < originalWidth), Is.EqualTo(pictureAssetInformation.SrcsetImages.Count),
                "Did not find the expected number of SrcSet Images");
        });
    }

    public static void CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(ImageContent newContent)
    {
        var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newContent);
        Assert.That(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteImageHtmlFile(newContent);
        Assert.That(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        var expectedOriginalFileInContent =
            new FileInfo(Path.Combine(expectedDirectory.FullName, newContent.OriginalFileName));
        Assert.That(expectedOriginalFileInContent.Exists,
            $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

        var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
            expectedOriginalFileInContent.Name));
        Assert.That(expectedOriginalFileInMediaArchive.Exists,
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
                MembersToIgnore =
                [
                    "ContentId",
                    "ContentVersion",
                    "Id",
                    "OriginalFileName",
                    "MainPicture"
                ]
            }
        };

        var compareResult = compareLogic.Compare(reference, toCompare);

        return (compareResult.AreEqual, compareResult.DifferencesString);
    }

    public static async Task HtmlChecks(ImageContent newContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteImageHtmlFile(newContent);

        Assert.That(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);

        //Todo: Continue checking...
    }

    public static async Task<ImageContent> ImageTest(string fileName, ImageContent contentReference, int width)
    {
        var originalFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia", fileName));
        Assert.That(originalFile.Exists, "Test File Found");

        var contentToSave = ImageContent.CreateInstance();
        contentToSave.InjectFrom(contentReference);

        var validationReturn = await ImageGenerator.Validate(contentToSave, originalFile);
        Assert.That(validationReturn.HasError, Is.False, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, newContent) = await ImageGenerator.SaveAndGenerateHtml(contentToSave, originalFile,
            true, null, DebugTrackers.DebugProgressTracker());
        Assert.That(generationReturn.HasError, Is.False, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        var contentComparison = CompareContent(contentReference, newContent);
        Assert.Multiple(() =>
        {
            Assert.That(contentComparison.areEqual, contentComparison.comparisonNotes);

            Assert.That(newContent.MainPicture == newContent.ContentId,
                $"Main Picture - {newContent.MainPicture} - Should be set to Content Id {newContent.ContentId}");
        });

        CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newContent);

        CheckFileCountAndPictureAssetsAfterHtmlGeneration(newContent, width);

        JsonTest(newContent);

        await HtmlChecks(newContent);

        return newContent;
    }

    public static void JsonTest(ImageContent newContent)
    {
        //Check JSON File
        var jsonFile = UserSettingsSingleton.CurrentSettings().LocalSiteContentDataDirectoryDataFile(newContent.ContentId);

        Assert.That(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<ImageContentOnDiskData>(
            [jsonFile.FullName]).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported.Content);
        Assert.That(comparisonResult.AreEqual,
            $"Json Import does not match expected Content {comparisonResult.DifferencesString}");
    }
}