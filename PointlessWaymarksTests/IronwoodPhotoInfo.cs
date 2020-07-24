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
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksTests
{
    public static class IronwoodPhotoInfo
    {
        public static PhotoContent AguaBlancaContent =>
            new PhotoContent
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
                Slug = "2018-august-agua-blanca-ranch-sign-at-the-manville-road-entrance-to-the-ironwood",
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
            new PhotoContent
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

        public static PhotoContent IronwoodPodContent01 =>
            new PhotoContent
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

        public static PhotoContent IronwoodPodContent02_CamerModelLensSummary =>
            new PhotoContent
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
            new PhotoContent
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
            new PhotoContent
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
            new PhotoContent
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
            new PhotoContent
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

        public static void PhotoCheckFileCountAndPictureAssetsAfterHtmlGeneration(PhotoContent newPhotoContent,
            int photoWidth)
        {
            var contentDirectory = UserSettingsSingleton.CurrentSettings()
                .LocalSitePhotoContentDirectory(newPhotoContent, false);
            Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

            var expectedNumberOfFiles = PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < photoWidth) //
                                        + 1 //Original image
                                        + 1 //Display image
                                        + 1 //HTML file
                                        + 1; //json file
            Assert.AreEqual(contentDirectory.GetFiles().Length, expectedNumberOfFiles,
                "Expected Number of Files Does Not Match");

            var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newPhotoContent.ContentId);
            var pictureAssetPhotoDbEntry = (PhotoContent) pictureAssetInformation.DbEntry;
            Assert.IsTrue(pictureAssetPhotoDbEntry.ContentId == newPhotoContent.ContentId,
                $"Picture Asset appears to have gotten an incorrect DB entry of {pictureAssetPhotoDbEntry.ContentId} rather than {newPhotoContent.ContentId}");

            var maxSize = PictureResizing.SrcSetSizeAndQualityList().Where(x => x.size < photoWidth).Max();
            var minSize = PictureResizing.SrcSetSizeAndQualityList().Min();

            Assert.AreEqual(pictureAssetInformation.LargePicture.Width, maxSize.size,
                $"Picture Asset Large Width is not the expected Value - Expected {maxSize}, Actual {pictureAssetInformation.LargePicture.Width}");
            Assert.AreEqual(pictureAssetInformation.SmallPicture.Width,
                PictureResizing.SrcSetSizeAndQualityList().Min().size,
                $"Picture Asset Small Width is not the expected Value - Expected {minSize}, Actual {pictureAssetInformation.SmallPicture.Width}");

            Assert.AreEqual(pictureAssetInformation.SrcsetImages.Count,
                PictureResizing.SrcSetSizeAndQualityList().Count(x => x.size < photoWidth),
                "Did not find the expected number of SrcSet Images");
        }

        public static void PhotoCheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(
            PhotoContent newPhotoContent)
        {
            var expectedDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newPhotoContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newPhotoContent);
            Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

            var expectedOriginalPhotoFileInContent =
                new FileInfo(Path.Combine(expectedDirectory.FullName, newPhotoContent.OriginalFileName));
            Assert.IsTrue(expectedOriginalPhotoFileInContent.Exists,
                $"Expected to find original photo in content directory but {expectedOriginalPhotoFileInContent.FullName} does not exist");

            var expectedOriginalPhotoFileInMediaArchive = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                expectedOriginalPhotoFileInContent.Name));
            Assert.IsTrue(expectedOriginalPhotoFileInMediaArchive.Exists,
                $"Expected to find original photo in media archive photo directory but {expectedOriginalPhotoFileInMediaArchive.FullName} does not exist");
        }

        public static (bool hasInvalidComparison, string comparisonNotes) PhotoComparePhotoReferenceToPhotoObject(
            PhotoContent reference, PhotoContent toCompare)
        {
            Db.DefaultPropertyCleanup(reference);
            reference.Tags = Db.TagListCleanup(reference.Tags);

            Db.DefaultPropertyCleanup(toCompare);
            toCompare.Tags = Db.TagListCleanup(toCompare.Tags);

            var compareLogic = new CompareLogic
            {
                Config = {MembersToIgnore = new List<string> {"ContentId", "ContentVersion", "Id"}}
            };

            var compareResult = compareLogic.Compare(reference, toCompare);

            return (compareResult.AreEqual, compareResult.DifferencesString);
        }

        public static void PhotoHtmlChecks(PhotoContent newPhotoContent)
        {
            var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newPhotoContent);

            Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

            var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

            IronwoodHtmlHelpers.CommonContentChecks(document, newPhotoContent);

            //Todo: Continue checking...
        }

        public static void PhotoJsonTest(PhotoContent newPhotoContent)
        {
            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newPhotoContent).FullName,
                    $"{Names.PhotoContentPrefix}{newPhotoContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<PhotoContent>(
                new List<string> {jsonFile.FullName}, Names.PhotoContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newPhotoContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected Photo Content {comparisonResult.DifferencesString}");
        }

        public static async Task PhotoTest(string photoFileName, PhotoContent photoReference, int photoWidth)
        {
            var fullSizePhotoTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "IronwoodTestContent",
                photoFileName));
            Assert.True(fullSizePhotoTest.Exists, "Test File Found");

            var (metadataGenerationReturn, newPhotoContent) =
                await PhotoGenerator.PhotoMetadataToNewPhotoContent(fullSizePhotoTest,
                    IronwoodTests.DebugProgressTracker());
            Assert.False(metadataGenerationReturn.HasError, metadataGenerationReturn.GenerationNote);

            var photoComparison = PhotoComparePhotoReferenceToPhotoObject(photoReference, newPhotoContent);
            Assert.False(photoComparison.hasInvalidComparison, photoComparison.comparisonNotes);

            var validationReturn = await PhotoGenerator.Validate(newPhotoContent, fullSizePhotoTest);
            Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

            var saveReturn = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent, fullSizePhotoTest, true,
                IronwoodTests.DebugProgressTracker());
            Assert.False(saveReturn.generationReturn.HasError,
                $"Unexpected Save Error - {saveReturn.generationReturn.GenerationNote}");

            Assert.IsTrue(newPhotoContent.MainPicture == newPhotoContent.ContentId,
                $"Main Picture - {newPhotoContent.MainPicture} - Should be set to Content Id {newPhotoContent.ContentId}");

            PhotoCheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newPhotoContent);

            PhotoCheckFileCountAndPictureAssetsAfterHtmlGeneration(newPhotoContent, photoWidth);

            PhotoJsonTest(newPhotoContent);

            PhotoHtmlChecks(newPhotoContent);
        }
    }
}