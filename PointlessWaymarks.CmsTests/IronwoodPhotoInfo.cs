using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsTests;

public static class IronwoodPhotoInfo
{
    public static PhotoContent AguaBlancaContent =>
        new()
        {
            Aperture = "f/11.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2018",
            Iso = 100,
            FocalLength = "35 mm",
            Lens = "FE 35mm F2.8 ZA",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2018, 8, 6, 15, 54, 52),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/1,000",
            Slug =
                "2018-august-agua-blanca-ranch-sign-at-the-manville-road-entrance-to-the-ironwood-forest-national-mon",
            Summary =
                "Agua Blanca Ranch Sign at the Manville Road Entrance to the Ironwood Forest National Monument.",
            Title =
                "2018 August Agua Blanca Ranch Sign at the Manville Road Entrance to the Ironwood Forest National Monument",
            Tags = "agua blanca ranch,ironwood forest national monument,manville road,manville road entrance",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static string AguaBlancaFileName =>
        "1808-Agua-Blanca-Ranch-Sign-at-the-Manville-Road-Entrance-to-the-Ironwood-Forest-National-Monument.jpg";

    public static int AguaBlancaWidth => 900;

    public static PhotoContent DisappearingContent =>
        new()
        {
            Aperture = "f/11.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2020",
            Iso = 250,
            FocalLength = "90 mm",
            Lens = "FE 90mm F2.8 Macro G OSS",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2020, 6, 19, 14, 49, 41),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/1,000",
            Slug = "2020-june-disappearing-into-the-flower",
            Summary = "Disappearing into the Flower.",
            Title = "2020 June Disappearing into the Flower",
            Tags = "barrel cactus,bee,flower,ironwood forest national monument,waterman mountains",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static string DisappearingFileName => "2020-06-Disappearing-into-the-Flower.jpg";

    public static int DisappearingWidth => 800;

    public static string IronwoodFileBarrelFileName => "2020-05-Fire-Barrel-Cactus.jpg";

    public static PhotoContent IronwoodFireBarrelContent01 =>
        new()
        {
            Aperture = "f/13.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2020",
            Iso = 200,
            FocalLength = "90 mm",
            Lens = "FE 90mm F2.8 Macro G OSS",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2020, 5, 28, 13, 39, 56),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/200",
            Slug = "2020-may-fire-barrel-cactus",
            Summary = "Fire Barrel Cactus.",
            Title = "2020 May Fire Barrel Cactus",
            Tags = "fire barrel cactus,ironwood forest national monument,waterman mountains",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static int IronwoodFireBarrelWidth => 700;

    public static PhotoContent IronwoodPodContent01 =>
        new()
        {
            Aperture = "f/16.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2020",
            Iso = 200,
            FocalLength = "90 mm",
            Lens = "FE 90mm F2.8 Macro G OSS",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2020, 5, 28, 14, 19, 10),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/320",
            Slug = "2020-may-ironwood-pod",
            Summary = "Ironwood Pod.",
            Title = "2020 May Ironwood Pod",
            Tags = "ironwood,ironwood forest national monument,seed pod,waterman mountains",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static PhotoContent IronwoodPodContent02_CameraModelLensSummary =>
        new()
        {
            Aperture = "f/16.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2 (A7RII)",
            Folder = "2020",
            Iso = 200,
            FocalLength = "90 mm",
            Lens = "FE 90mm F2.8 Macro G OSS (Super Zoom)",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2020, 5, 28, 14, 19, 10),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/320",
            Slug = "2020-may-ironwood-pod",
            Summary = "A browning Ironwood Pod under the summer sun.",
            Title = "2020 May Ironwood Pod",
            Tags = "ironwood,ironwood forest national monument,seed pod,waterman mountains",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
            LastUpdatedBy = "Integration Tester"
        };

    public static string IronwoodPodFileName => "2020-05-Ironwood-Pod.jpg";

    public static int IronwoodPodWidth => 700;

    public static PhotoContent IronwoodTreeContent01 =>
        new()
        {
            Aperture = "f/16.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2017",
            Iso = 100,
            FocalLength = "35 mm",
            Lens = "FE 35mm F2.8 ZA",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2017, 05, 15, 14, 49, 49),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/640",
            Slug = "2017-may-ironwood-02",
            Summary = "Ironwood 02.",
            Title = "2017 May Ironwood 02",
            Tags = "ironwood,ironwood forest national monument,sun",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static PhotoContent IronwoodTreeContent02_SlugTitleSummaryTagsUpdateNotesUpdatedBy =>
        new()
        {
            Aperture = "f/16.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2017",
            Iso = 100,
            FocalLength = "35 mm",
            Lens = "FE 35mm F2.8 ZA",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2017, 05, 15, 14, 49, 49),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/640",
            Slug = "2017-may-ironwood-tree-against-the-sky",
            Summary = "An Ironwood Tree against the Sky.",
            Title = "2017 May Ironwood Tree Against The Sky",
            Tags = "ironwood,ironwood forest national monument,sun,tree,sky",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString(),
            UpdateNotes = "Improved Title, Summary and Tags for Photo",
            LastUpdatedBy = "Charles Miles"
        };

    public static string IronwoodTreeFileName => "1705-Ironwood-02.jpg";

    public static int IronwoodTreeWidth => 734;

    public static PhotoContent QuarryContent01 =>
        new()
        {
            Aperture = "f/9.0",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2020",
            Iso = 125,
            FocalLength = "150 mm",
            Lens = "FE 24-240mm F3.5-6.3 OSS",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2020, 5, 21, 15, 35, 39),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/400",
            Slug = "2020-may-a-quarry-in-ironwood-forest-national-monument",
            Summary = "A Quarry in Ironwood Forest National Monument.",
            Title = "2020 May A Quarry in Ironwood Forest National Monument",
            Tags = "agua dulce road,ironwood forest national monument,quarry",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static PhotoContent QuarryContent02_BodyContentUpdateNotesTags =>
        new()
        {
            Aperture = "f/9.0",
            BodyContent = "Mining is part of both the past and the future of the Waterman Mountains.",
            BodyContentFormat = ContentFormatDefaults.Content.ToString(),
            CameraMake = "SONY",
            CameraModel = "ILCE-7RM2",
            Folder = "2020",
            Iso = 125,
            FocalLength = "150 mm",
            Lens = "FE 24-240mm F3.5-6.3 OSS",
            License = "Public Domain",
            PhotoCreatedOn = new DateTime(2020, 5, 21, 15, 35, 39),
            PhotoCreatedBy = "Charles Miles",
            ShutterSpeed = "1/400",
            Slug = "2020-may-a-quarry-in-ironwood-forest-national-monument",
            Summary = "A Quarry in Ironwood Forest National Monument.",
            Title = "2020 May A Quarry in Ironwood Forest National Monument",
            Tags = "agua dulce road,ironwood forest national monument,mining,quarry,waterman mountains",
            UpdateNotes = "Updated information on mining in the Waterman Mountains.",
            UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        };

    public static string QuarryFileName => "2020-05-A-Quarry-in-Ironwood-Forest-National-Monument.jpg";

    public static int QuarryWidth => 1300;

    public static void CheckFileCountAndPictureAssetsAfterHtmlGeneration(PhotoContent newContent, int originalWidth)
    {
        var contentDirectory = UserSettingsSingleton.CurrentSettings()
            .LocalSitePhotoContentDirectory(newContent, false);
        Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

        var expectedNumberOfFiles = PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < originalWidth) //
                                    + 1 //Original image
                                    + 1 //Display image
                                    + 1 //HTML file
                                    + 1; //json file
        Assert.AreEqual(contentDirectory.GetFiles().Length, expectedNumberOfFiles,
            "Expected Number of Files Does Not Match");

        var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newContent.ContentId);
        var pictureAssetDbEntry = (PhotoContent)pictureAssetInformation.DbEntry;
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

    public static void CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(PhotoContent newContent)
    {
        var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newContent);
        Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newContent);
        Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        var expectedOriginalFileInContent =
            new FileInfo(Path.Combine(expectedDirectory.FullName, newContent.OriginalFileName));
        Assert.IsTrue(expectedOriginalFileInContent.Exists,
            $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

        var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
            expectedOriginalFileInContent.Name));
        Assert.IsTrue(expectedOriginalFileInMediaArchive.Exists,
            $"Expected to find original file in media archive directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");
    }

    public static (bool areEqual, string comparisonNotes) CompareContent(PhotoContent reference,
        PhotoContent toCompare)
    {
        if (string.IsNullOrWhiteSpace(reference.CreatedBy))
            reference.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;
        if (reference.CreatedOn == default) reference.CreatedOn = toCompare.CreatedOn;
        if (reference.FeedOn == default) reference.FeedOn = toCompare.FeedOn;
        if (string.IsNullOrWhiteSpace(reference.LastUpdatedBy) &&
            !string.IsNullOrWhiteSpace(toCompare.LastUpdatedBy))
            reference.LastUpdatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;
        if (reference.LastUpdatedOn == null && toCompare.LastUpdatedOn != null)
            reference.LastUpdatedOn = toCompare.LastUpdatedOn;


        Db.DefaultPropertyCleanup(reference);
        reference.Tags = Db.TagListCleanup(reference.Tags);

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

    public static async Task HtmlChecks(PhotoContent newContent)
    {
        var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newContent);

        Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

        var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

        await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);

        var tagContainers = document.QuerySelectorAll(".tags-detail-link-container");
        var contentTags = Db.TagListParseToSlugsAndIsExcluded(newContent);
        Assert.AreEqual(tagContainers.Length, contentTags.Count);

        var tagLinks = document.QuerySelectorAll(".tag-detail-link");
        Assert.AreEqual(tagLinks.Length, contentTags.Count(x => !x.IsExcluded));

        var tagNoLinks = document.QuerySelectorAll(".tag-detail-text");
        Assert.AreEqual(tagNoLinks.Length, contentTags.Count(x => x.IsExcluded));
    }

    public static void JsonTest(PhotoContent newContent)
    {
        //Check JSON File
        var jsonFile =
            new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newContent).FullName,
                $"{Names.PhotoContentPrefix}{newContent.ContentId}.json"));
        Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        var jsonFileImported = Import.ContentFromFiles<PhotoContent>(
            new List<string> { jsonFile.FullName }, Names.PhotoContentPrefix).Single();
        var compareLogic = new CompareLogic();
        var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
        Assert.True(comparisonResult.AreEqual,
            $"Json Import does not match expected Content {comparisonResult.DifferencesString}");
    }

    public static async Task PhotoTest(string fileName, PhotoContent contentReference, int width)
    {
        var originalFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia", fileName));
        Assert.True(originalFile.Exists, "Test File Found");

        var (metadataGenerationReturn, newContent) =
            PhotoGenerator.PhotoMetadataToNewPhotoContent(originalFile, DebugTrackers.DebugProgressTracker());
        Assert.False(metadataGenerationReturn.HasError, metadataGenerationReturn.GenerationNote);

        var (areEqual, comparisonNotes) = CompareContent(contentReference, newContent);
        Assert.True(areEqual, comparisonNotes);

        var validationReturn = await PhotoGenerator.Validate(newContent, originalFile);
        Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

        var (generationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(newContent, originalFile, true, null,
            DebugTrackers.DebugProgressTracker());
        Assert.False(generationReturn.HasError, $"Unexpected Save Error - {generationReturn.GenerationNote}");

        Assert.IsTrue(newContent.MainPicture == newContent.ContentId,
            $"Main Picture - {newContent.MainPicture} - Should be set to Content Id {newContent.ContentId}");

        CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newContent);

        CheckFileCountAndPictureAssetsAfterHtmlGeneration(newContent, width);

        JsonTest(newContent);

        await HtmlChecks(newContent);
    }
}