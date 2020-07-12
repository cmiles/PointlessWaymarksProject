using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksTests
{
    public class IronwoodIntegrationTests
    {
        public const string UrlProclamationPdf = "https://www.blm.gov/sites/blm.gov/files/documents/ironwood_proc.pdf";
        public const string UrlBlmSite = "https://www.blm.gov/visit/ironwood";
        public const string UrlBlmMapPdf = "https://www.blm.gov/sites/blm.gov/files/documents/AZ_IronwoodForest_NM_map.pdf";
        public const string UrlFriendsOfIronwood = "https://ironwoodforest.org/";
        public const string ContributorOneName = "Ironwood Enthusiast";

        public static UserSettings TestSiteSettings;

        [Test]
        public void A01_TestSiteBasicStructureCheck()
        {
            Assert.True(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchiveImageDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchivePhotoDirectory().Exists);
            Assert.True(TestSiteSettings.LocalSiteDirectory().Exists);
        }

        [Test]
        public async Task A10_Photo1705Ironwood02Import()
        {
            var fullSizePhotoTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "A10 Photo1705Ironwood02Import",
                "1705-Ironwood-02.jpg"));
            Assert.True(fullSizePhotoTest.Exists,
                "1705-Ironwood-02 not found");

            var (generationReturn, newPhotoContent) =
                await PhotoGenerator.PhotoMetadataToNewPhotoContent(fullSizePhotoTest, DebugProgressTracker());

            //TODO:Finish out this test with new info - extract this a little so all photos can use it.
            var expectedPhotoContent = new PhotoContent
            {
                Title = "2017 May Ironwood 02",
                Tags = "ironwood,ironwood forest national monument,sun",
            };

            //Check the Metadata
            Assert.IsTrue(newPhotoContent.Title == "2017 May Ironwood 02",
                $"Title Does Not Match - Found '{newPhotoContent.Title}' - Found '{newPhotoContent.Title}' Expected 2019 January Bridge Under Highway 77 on the Arizona Trail");
            Assert.IsTrue(newPhotoContent.Tags == "arizona trail,bridge,fence,gate,highway 77,oracle state park,wash",
                $"Tags Do Not Match - Found '{newPhotoContent.Tags}' Expected arizona trail,bridge,fence,gate,highway 77,oracle state park,wash");
            Assert.IsTrue(newPhotoContent.License == "Public Domain",
                $"License Does Not Match - Found '{newPhotoContent.License}' Expected Public Domain");
            Assert.IsTrue(newPhotoContent.Aperture == "f/9.0",
                $"Aperture Does Not Match - Found '{newPhotoContent.Aperture}' Expected f/9.0");
            Assert.IsTrue(newPhotoContent.CameraMake == "SONY",
                $"CameraMake Does Not Match - Found '{newPhotoContent.CameraMake}' Expected SONY");
            Assert.IsTrue(newPhotoContent.CameraModel == "ILCE-7RM2",
                $"CameraModel Does Not Match - Found '{newPhotoContent.CameraModel}' Expected ILCE-7RM2");
            Assert.IsTrue(newPhotoContent.Lens == "FE 35mm F2.8 ZA",
                $"Lens Does Not Match - Found '{newPhotoContent.Lens}' Expected FE 35mm F2.8 ZA");
            Assert.IsTrue(newPhotoContent.PhotoCreatedBy == "Charles Miles",
                $"Photo Created By Does Not Match - Found '{newPhotoContent.PhotoCreatedBy}' Expected Charles Miles");
            Assert.IsTrue(newPhotoContent.ShutterSpeed == "1/800",
                $"Shutter Speed Does Not Match - Found '{newPhotoContent.ShutterSpeed}' Expected 1/800");
            Assert.IsTrue(newPhotoContent.Summary == "Bridge Under Highway 77 on the Arizona Trail.",
                $"Summary Does Not Match - Found '{newPhotoContent.Summary}' Expected Bridge Under Highway 77 on the Arizona Trail");
            Assert.IsTrue(newPhotoContent.Iso != null && newPhotoContent.Iso.Value == 100,
                $"ISO Does Not Match - Found '{newPhotoContent.Iso}' Expected 100");
            Assert.IsTrue(newPhotoContent.FocalLength == "35 mm",
                $"Focal Length Does Not Match - Found '{newPhotoContent.FocalLength}' Expected 35 mm");
            Assert.IsTrue(newPhotoContent.PhotoCreatedOn == new DateTime(2019, 1, 29, 13, 18, 25),
                $"Photo Created On Does Not Match - Found '{newPhotoContent.PhotoCreatedOn}' Expected {new DateTime(2019, 1, 29, 13, 18, 25)}");
            Assert.IsTrue(newPhotoContent.Folder == newPhotoContent.PhotoCreatedOn.ToString("yyyy"),
                $"Default Folder was {newPhotoContent.Folder} but should be Photo Taken on Year {newPhotoContent.PhotoCreatedOn:yyyy}");
            Assert.IsTrue(newPhotoContent.Slug == "2019-january-bridge-under-highway-77-on-the-arizona-trail",
                $"Slug is {newPhotoContent.Slug} Should be 2019-january-bridge-under-highway-77-on-the-arizona-trail");

            Assert.False(generationReturn.HasError);

            var validationReturn = await PhotoGenerator.Validate(newPhotoContent, fullSizePhotoTest);

            Assert.False(validationReturn.HasError);

            var saveReturn = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent, fullSizePhotoTest, true,
                DebugProgressTracker());
            Assert.False(saveReturn.generationReturn.HasError);

            Assert.IsTrue(newPhotoContent.MainPicture == newPhotoContent.ContentId,
                $"Main Picture - {newPhotoContent.MainPicture} - Should be set to Content Id {newPhotoContent.ContentId}");

            var expectedDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newPhotoContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newPhotoContent);
            Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

            var expectedOriginalPhotoFileInContent =
                new FileInfo(Path.Combine(expectedDirectory.FullName, fullSizePhotoTest.Name));
            Assert.IsTrue(expectedOriginalPhotoFileInContent.Exists,
                $"Expected to find original photo in content directory but {expectedOriginalPhotoFileInContent.FullName} does not exist");

            var expectedOriginalPhotoFileInMediaArchive = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                expectedOriginalPhotoFileInContent.Name));
            Assert.IsTrue(expectedOriginalPhotoFileInMediaArchive.Exists,
                $"Expected to find original photo in media archive photo directory but {expectedOriginalPhotoFileInMediaArchive.FullName} does not exist");

            //Checking the count of files is useful to make sure there are not any unexpected files
            var expectedNumberOfFiles =
                PictureResizing.SrcSetSizeAndQualityList()
                    .Count //This image should trigger all sizes atm, this will need adjustment if the size list changes
                + 1 //Original image
                + 1 //Display image
                + 1 //html file
                + 1; //json file
            Assert.AreEqual(expectedDirectory.GetFiles().Length, expectedNumberOfFiles,
                "Expected Number of Files Does Not Match");

            //Check that the Picture Asset processing finds all the files

            var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newPhotoContent.ContentId);
            var pictureAssetPhotoDbEntry = (PhotoContent) pictureAssetInformation.DbEntry;
            Assert.IsTrue(pictureAssetPhotoDbEntry.ContentId == newPhotoContent.ContentId,
                $"Picture Asset appears to have gotten an incorrect DB entry of {pictureAssetPhotoDbEntry.ContentId} rather than {newPhotoContent.ContentId}");
            Assert.AreEqual(pictureAssetInformation.LargePicture.Width, 4000,
                "Picture Asset Large Width is not the expected Value");
            Assert.AreEqual(pictureAssetInformation.SmallPicture.Width, 100,
                "Picture Asset Small Width is not the expected Value");
            Assert.AreEqual(pictureAssetInformation.SrcsetImages.Count,
                PictureResizing.SrcSetSizeAndQualityList().Count, "Did not find the expected number of SrcSet Images");

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

            //?Check some details of the HTML?
            var updateWithoutUpdateResult = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent,
                expectedOriginalPhotoFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
                "Should not be able to update an entry without LastUpdated Values set");

            var updatedTime = DateTime.Now;
            newPhotoContent.Tags += ",testupdatetag";
            newPhotoContent.Title += " Updated";
            newPhotoContent.LastUpdatedOn = updatedTime;
            newPhotoContent.LastUpdatedBy = "Test Photo Updater";

            //?Check some details of the HTML?
            var updateResult = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent,
                expectedOriginalPhotoFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

            var updatedJsonFileImported = Import.ContentFromFiles<PhotoContent>(
                new List<string> {jsonFile.FullName}, Names.PhotoContentPrefix).Single();
            var updateComparisonResult = compareLogic.Compare(newPhotoContent, updatedJsonFileImported);
            Assert.True(updateComparisonResult.AreEqual,
                $"Updated Json Import does not match expected Updated Photo Content {comparisonResult.DifferencesString}");

            //Check Historic JSON File
            var historicJsonFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newPhotoContent).FullName,
                $"{Names.HistoricPhotoContentPrefix}{newPhotoContent.ContentId}.json"));
            var historicJsonFileImported = Import
                .ContentFromFiles<List<HistoricPhotoContent>>(new List<string> {historicJsonFile.FullName},
                    Names.HistoricPhotoContentPrefix).SelectMany(x => x).ToList();

            Assert.AreEqual(1, historicJsonFileImported.Count,
                "Wrong number of Historic Entries in the Historic Json File");

            var expectedHistoricValues = new HistoricPhotoContent();
            expectedHistoricValues.InjectFrom(jsonFileImported);
            expectedHistoricValues.Id = historicJsonFileImported.First().Id;

            var historicJsonComparisonResult =
                compareLogic.Compare(expectedHistoricValues, historicJsonFileImported.First());
            Assert.IsTrue(historicJsonComparisonResult.AreEqual,
                $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

            newPhotoContent.Title += " Again";
            newPhotoContent.LastUpdatedOn = DateTime.Now;
            newPhotoContent.LastUpdatedBy = "Test Photo Updater 2";

            var updateTwoResult = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent,
                expectedOriginalPhotoFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

            historicJsonFileImported = Import
                .ContentFromFiles<List<HistoricPhotoContent>>(new List<string> {historicJsonFile.FullName},
                    Names.HistoricPhotoContentPrefix).SelectMany(x => x).ToList();
            Assert.AreEqual(2, historicJsonFileImported.Count,
                "Wrong number of Historic Entries in the Historic Json File");
        }

        [Test]
        public async Task A20_FileContentImportAndUpdate()
        {
            var fileToImport = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestContent",
                "Papago-Saguaro-NM-Brief.pdf"));
            Assert.True(fileToImport.Exists, "Test file Papago-Saguaro-NM-Brief.pdf not found");

            var newFileContent = new FileContent
            {
                BodyContent = "Simple Test Content",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                ContentId = Guid.NewGuid(),
                ContentVersion = DateTime.Now.ToUniversalTime(),
                CreatedBy = "Test Series A",
                CreatedOn = DateTime.Now,
                Folder = "NationalParks",
                PublicDownloadLink = true,
                OriginalFileName = fileToImport.Name,
                Title = "Papago Saguaro",
                ShowInMainSiteFeed = true,
                Slug = SlugUtility.Create(true, "Papago Saguaro"),
                Summary = "A Summary",
                Tags = "national parks,phoenix,papago saguaro"
            };

            var validationReturn = await FileGenerator.Validate(newFileContent, fileToImport);

            Assert.False(validationReturn.HasError);

            var saveReturn = await FileGenerator.SaveAndGenerateHtml(newFileContent, fileToImport, true,
                DebugProgressTracker());
            Assert.False(saveReturn.generationReturn.HasError);

            var expectedDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newFileContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileHtmlFile(newFileContent);
            Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

            var expectedOriginalFileInContent =
                new FileInfo(Path.Combine(expectedDirectory.FullName, fileToImport.Name));
            Assert.IsTrue(expectedOriginalFileInContent.Exists,
                $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

            var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                expectedOriginalFileInContent.Name));
            Assert.IsTrue(expectedOriginalFileInMediaArchive.Exists,
                $"Expected to find original file in media archive file directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");

            Assert.AreEqual(expectedDirectory.GetFiles().Length, 3, "Expected Number of Files Does Not Match");

            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newFileContent).FullName,
                    $"{Names.FileContentPrefix}{newFileContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<FileContent>(
                new List<string> {jsonFile.FullName}, Names.FileContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newFileContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");

            //?Check some details of the HTML?
            var updateWithoutUpdateResult = await FileGenerator.SaveAndGenerateHtml(newFileContent,
                expectedOriginalFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
                "Should not be able to update an entry without LastUpdated Values set");

            var updatedTime = DateTime.Now;
            newFileContent.Tags += ",testupdatetag";
            newFileContent.Title += " NP";
            newFileContent.LastUpdatedOn = updatedTime;
            newFileContent.LastUpdatedBy = "Test Updater";

            //?Check some details of the HTML?
            var updateResult = await FileGenerator.SaveAndGenerateHtml(newFileContent,
                expectedOriginalFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

            var updatedJsonFileImported = Import.ContentFromFiles<FileContent>(
                new List<string> {jsonFile.FullName}, Names.FileContentPrefix).Single();
            var updateComparisonResult = compareLogic.Compare(newFileContent, updatedJsonFileImported);
            Assert.True(updateComparisonResult.AreEqual,
                $"Updated Json Import does not match expected Updated File Content {comparisonResult.DifferencesString}");

            //Check Historic JSON File
            var historicJsonFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newFileContent).FullName,
                $"{Names.HistoricFileContentPrefix}{newFileContent.ContentId}.json"));
            var historicJsonFileImported = Import
                .ContentFromFiles<List<HistoricFileContent>>(new List<string> {historicJsonFile.FullName},
                    Names.HistoricFileContentPrefix).SelectMany(x => x).ToList();

            Assert.AreEqual(1, historicJsonFileImported.Count,
                "Wrong number of Historic Entries in the Historic Json File");

            var expectedHistoricValues = new HistoricFileContent();
            expectedHistoricValues.InjectFrom(jsonFileImported);
            expectedHistoricValues.Id = historicJsonFileImported.First().Id;

            var historicJsonComparisonResult =
                compareLogic.Compare(expectedHistoricValues, historicJsonFileImported.First());
            Assert.IsTrue(historicJsonComparisonResult.AreEqual,
                $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

            newFileContent.Title += " Again";
            newFileContent.LastUpdatedOn = DateTime.Now;
            newFileContent.LastUpdatedBy = "Test Updater 2";

            var updateTwoResult = await FileGenerator.SaveAndGenerateHtml(newFileContent,
                expectedOriginalFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

            historicJsonFileImported = Import
                .ContentFromFiles<List<HistoricFileContent>>(new List<string> {historicJsonFile.FullName},
                    Names.HistoricFileContentPrefix).SelectMany(x => x).ToList();
            Assert.AreEqual(2, historicJsonFileImported.Count,
                "Wrong number of Historic Entries in the Historic Json File");
        }

        [Test]
        public async Task A30_ImageContentImportAndUpdate()
        {
            var fullSizeImageTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestContent",
                "2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg"));
            Assert.True(fullSizeImageTest.Exists,
                "Test Image 2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg not found");

            var newImageContent = new ImageContent
            {
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                BodyContent = "Image Body Content",
                ContentId = Guid.NewGuid(),
                ContentVersion = DateTime.Now.ToUniversalTime(),
                CreatedBy = "A30_ImageContentImportAndUpdate",
                CreatedOn = DateTime.Now,
                Folder = "Bridges",
                ShowInSearch = false,
                Slug = SlugUtility.Create(true, "AZT Under Highway"),
                Summary = "A trail and bridge story",
                Tags = "arizona trail,highway 77,wash",
                Title = "AZT Under Highway",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
            };

            var validationReturn = await ImageGenerator.Validate(newImageContent, fullSizeImageTest);

            Assert.False(validationReturn.HasError);

            var saveReturn = await ImageGenerator.SaveAndGenerateHtml(newImageContent, fullSizeImageTest, true,
                DebugProgressTracker());
            Assert.False(saveReturn.generationReturn.HasError);

            Assert.IsTrue(newImageContent.MainPicture == newImageContent.ContentId,
                $"Main Picture - {newImageContent.MainPicture} - Should be set to Content Id {newImageContent.ContentId}");

            var expectedDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newImageContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteImageHtmlFile(newImageContent);
            Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

            var expectedOriginalImageFileInContent =
                new FileInfo(Path.Combine(expectedDirectory.FullName, fullSizeImageTest.Name));
            Assert.IsTrue(expectedOriginalImageFileInContent.Exists,
                $"Expected to find original image in content directory but {expectedOriginalImageFileInContent.FullName} does not exist");

            var expectedOriginalImageFileInMediaArchive = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
                expectedOriginalImageFileInContent.Name));
            Assert.IsTrue(expectedOriginalImageFileInMediaArchive.Exists,
                $"Expected to find original image in media archive image directory but {expectedOriginalImageFileInMediaArchive.FullName} does not exist");

            //Checking the count of files is useful to make sure there are not any unexpected files
            var expectedNumberOfFiles =
                PictureResizing.SrcSetSizeAndQualityList()
                    .Count //This image should trigger all sizes atm, this will need adjustment if the size list changes
                + 1 //Original image
                + 1 //Display image
                + 1 //html file
                + 1; //json file
            Assert.AreEqual(expectedDirectory.GetFiles().Length, expectedNumberOfFiles,
                "Expected Number of Files Does Not Match");

            //Check that the Picture Asset processing finds all the files

            var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newImageContent.ContentId);
            var pictureAssetImageDbEntry = (ImageContent) pictureAssetInformation.DbEntry;
            Assert.IsTrue(pictureAssetImageDbEntry.ContentId == newImageContent.ContentId,
                $"Picture Asset appears to have gotten an incorrect DB entry of {pictureAssetImageDbEntry.ContentId} rather than {newImageContent.ContentId}");
            Assert.AreEqual(pictureAssetInformation.LargePicture.Width, 4000,
                "Picture Asset Large Width is not the expected Value");
            Assert.AreEqual(pictureAssetInformation.SmallPicture.Width, 100,
                "Picture Asset Small Width is not the expected Value");
            Assert.AreEqual(pictureAssetInformation.SrcsetImages.Count,
                PictureResizing.SrcSetSizeAndQualityList().Count, "Did not find the expected number of SrcSet Images");

            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newImageContent).FullName,
                    $"{Names.ImageContentPrefix}{newImageContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<ImageContent>(
                new List<string> {jsonFile.FullName}, Names.ImageContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newImageContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected Image Content {comparisonResult.DifferencesString}");

            //?Check some details of the HTML?
            var updateWithoutUpdateResult = await ImageGenerator.SaveAndGenerateHtml(newImageContent,
                expectedOriginalImageFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
                "Should not be able to update an entry without LastUpdated Values set");

            var updatedTime = DateTime.Now;
            newImageContent.Tags += ",testupdatetag";
            newImageContent.Title += " Updated";
            newImageContent.LastUpdatedOn = updatedTime;
            newImageContent.LastUpdatedBy = "Test Image Updater";

            //?Check some details of the HTML?
            var updateResult = await ImageGenerator.SaveAndGenerateHtml(newImageContent,
                expectedOriginalImageFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

            var updatedJsonFileImported = Import.ContentFromFiles<ImageContent>(
                new List<string> {jsonFile.FullName}, Names.ImageContentPrefix).Single();
            var updateComparisonResult = compareLogic.Compare(newImageContent, updatedJsonFileImported);
            Assert.True(updateComparisonResult.AreEqual,
                $"Updated Json Import does not match expected Updated Image Content {comparisonResult.DifferencesString}");

            //Check Historic JSON File
            var historicJsonFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newImageContent).FullName,
                $"{Names.HistoricImageContentPrefix}{newImageContent.ContentId}.json"));
            var historicJsonFileImported = Import
                .ContentFromFiles<List<HistoricImageContent>>(new List<string> {historicJsonFile.FullName},
                    Names.HistoricImageContentPrefix).SelectMany(x => x).ToList();

            Assert.AreEqual(1, historicJsonFileImported.Count,
                "Wrong number of Historic Entries in the Historic Json File");

            var expectedHistoricValues = new HistoricImageContent();
            expectedHistoricValues.InjectFrom(jsonFileImported);
            expectedHistoricValues.Id = historicJsonFileImported.First().Id;

            var historicJsonComparisonResult =
                compareLogic.Compare(expectedHistoricValues, historicJsonFileImported.First());
            Assert.IsTrue(historicJsonComparisonResult.AreEqual,
                $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

            newImageContent.Title += " Again";
            newImageContent.LastUpdatedOn = DateTime.Now;
            newImageContent.LastUpdatedBy = "Test Image Updater 2";

            var updateTwoResult = await ImageGenerator.SaveAndGenerateHtml(newImageContent,
                expectedOriginalImageFileInMediaArchive, false, DebugProgressTracker());
            Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

            historicJsonFileImported = Import
                .ContentFromFiles<List<HistoricImageContent>>(new List<string> {historicJsonFile.FullName},
                    Names.HistoricImageContentPrefix).SelectMany(x => x).ToList();
            Assert.AreEqual(2, historicJsonFileImported.Count,
                "Wrong number of Historic Entries in the Historic Json File");
        }

        [Test]
        public async Task A40_NoteContentImportAndUpdate()
        {
            var newNoteContent = new NoteContent
            {
                BodyContent = "Grand Canyon Permit Info Link",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                ContentId = Guid.NewGuid(),
                ContentVersion = DateTime.Now.ToUniversalTime(),
                CreatedBy = "Test Series A",
                CreatedOn = DateTime.Now,
                Folder = "GrandCanyon",
                ShowInMainSiteFeed = true,
                Slug = await NoteGenerator.UniqueNoteSlug(),
                Summary = "GC Quick Info",
                Tags = "national parks,grand canyon,permits"
            };

            var validationReturn = await NoteGenerator.Validate(newNoteContent);

            Assert.False(validationReturn.HasError);

            var saveReturn = await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
            Assert.False(saveReturn.generationReturn.HasError);

            var expectedDirectory =
                UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newNoteContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedNote = UserSettingsSingleton.CurrentSettings().LocalSiteNoteHtmlFile(newNoteContent);
            Assert.IsTrue(expectedNote.Exists, $"Expected html Note {expectedNote.FullName} does not exist");

            Assert.AreEqual(expectedDirectory.GetFiles().Length, 2, "Expected Number of Files Does Not Match");

            //Check JSON Note
            var jsonNote =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newNoteContent).FullName,
                    $"{Names.NoteContentPrefix}{newNoteContent.ContentId}.json"));
            Assert.True(jsonNote.Exists, $"Json Note {jsonNote.FullName} does not exist?");

            var jsonNoteImported = Import.ContentFromFiles<NoteContent>(
                new List<string> {jsonNote.FullName}, Names.NoteContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newNoteContent, jsonNoteImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected Note Content {comparisonResult.DifferencesString}");

            //?Check some details of the HTML?
            var updateWithoutUpdateResult =
                await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
            Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
                "Should not be able to update an entry without LastUpdated Values set");

            var updatedTime = DateTime.Now;
            newNoteContent.Tags += ",testupdatetag";
            newNoteContent.LastUpdatedOn = updatedTime;
            newNoteContent.LastUpdatedBy = "Test Updater";

            //?Check some details of the HTML?
            var updateResult = await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
            Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

            var updatedJsonNoteImported = Import.ContentFromFiles<NoteContent>(
                new List<string> {jsonNote.FullName}, Names.NoteContentPrefix).Single();
            var updateComparisonResult = compareLogic.Compare(newNoteContent, updatedJsonNoteImported);
            Assert.True(updateComparisonResult.AreEqual,
                $"Updated Json Import does not match expected Updated Note Content {comparisonResult.DifferencesString}");

            //Check Historic JSON Note
            var historicJsonNote = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newNoteContent).FullName,
                $"{Names.HistoricNoteContentPrefix}{newNoteContent.ContentId}.json"));
            var historicJsonNoteImported = Import
                .ContentFromFiles<List<HistoricNoteContent>>(new List<string> {historicJsonNote.FullName},
                    Names.HistoricNoteContentPrefix).SelectMany(x => x).ToList();

            Assert.AreEqual(1, historicJsonNoteImported.Count,
                "Wrong number of Historic Entries in the Historic Json Note");

            var expectedHistoricValues = new HistoricNoteContent();
            expectedHistoricValues.InjectFrom(jsonNoteImported);
            expectedHistoricValues.Id = historicJsonNoteImported.First().Id;

            var historicJsonComparisonResult =
                compareLogic.Compare(expectedHistoricValues, historicJsonNoteImported.First());
            Assert.IsTrue(historicJsonComparisonResult.AreEqual,
                $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

            newNoteContent.LastUpdatedOn = DateTime.Now;
            newNoteContent.LastUpdatedBy = "Test Updater 2";

            var updateTwoResult = await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
            Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

            historicJsonNoteImported = Import
                .ContentFromFiles<List<HistoricNoteContent>>(new List<string> {historicJsonNote.FullName},
                    Names.HistoricNoteContentPrefix).SelectMany(x => x).ToList();
            Assert.AreEqual(2, historicJsonNoteImported.Count,
                "Wrong number of Historic Entries in the Historic Json Note");
        }

        [OneTimeSetUp]
        public async Task CreateTestSite()
        {
            var outSettings =
                await UserSettingsUtilities.SetupNewSite($"IronwoodForestTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}",
                    DebugProgressTracker());
            TestSiteSettings = outSettings;
            TestSiteSettings.SiteName = TestSiteName;
            TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
            TestSiteSettings.SiteAuthors = TestSiteAuthors;
            TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
            TestSiteSettings.SiteKeywords = TestSiteKeywords;
            TestSiteSettings.SiteSummary = TestSummary;
            TestSiteSettings.SiteUrl = "IronwoodTest.com";
            await TestSiteSettings.EnsureDbIsPresent(DebugProgressTracker());
        }

        public const string TestSiteName = "Ironwood Forest's Test Site";
        public const string TestDefaultCreatedBy = "Ironwood Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Ironwood 'Testers'";
        public const string TestSiteEmailTo = "Ironwood@Forest.Fake";
        public const string TestSiteKeywords = "ironwood forest national monument, samaniego hills, waterman mountains, test'ing";
        public const string TestSummary = "'Testing' in the beautiful Sonoran Desert";



        public static IProgress<string> DebugProgressTracker()
        {
            var toReturn = new Progress<string>();
            toReturn.ProgressChanged += DebugProgressTrackerChange;
            return toReturn;
        }

        private static void DebugProgressTrackerChange(object sender, string e)
        {
            Debug.WriteLine(e);
        }
    }
}