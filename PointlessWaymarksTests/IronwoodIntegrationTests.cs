using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Parser;
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
    public class IronwoodIntegrationTests
    {
        public const string UrlProclamationPdf = "https://www.blm.gov/sites/blm.gov/files/documents/ironwood_proc.pdf";
        public const string UrlBlmSite = "https://www.blm.gov/visit/ironwood";

        public const string UrlBlmMapPdf =
            "https://www.blm.gov/sites/blm.gov/files/documents/AZ_IronwoodForest_NM_map.pdf";

        public const string UrlFriendsOfIronwood = "https://ironwoodforest.org/";
        public const string ContributorOneName = "Ironwood Enthusiast";

        public const string TestSiteName = "Ironwood Forest's Test Site";
        public const string TestDefaultCreatedBy = "Ironwood Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Ironwood 'Testers'";
        public const string TestSiteEmailTo = "Ironwood@Forest.Fake";

        public const string TestSiteKeywords =
            "ironwood forest national monument, samaniego hills, waterman mountains, test'ing";

        public const string TestSummary = "'Testing' in the beautiful Sonoran Desert";

        public static UserSettings TestSiteSettings;

        public PhotoContent PhotoAguaBlancaContentReference =>
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
            };

        public string PhotoAguaBlancaFileName =>
            "1808-Agua-Blanca-Ranch-Sign-at-the-Manville-Road-Entrance-to-the-Ironwood-Forest-National-Monument.jpg";

        public int PhotoAguaBlancaWidth => 900;

        public PhotoContent PhotoDisappearingContentReference =>
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
            };

        public string PhotoDisappearingFileName => "2020-06-Disappearing-into-the-Flower.jpg";

        public int PhotoDisappearingWidth => 800;

        public string PhotoIronwood02FileName => "1705-Ironwood-02.jpg";

        public PhotoContent PhotoIronwood02Reference =>
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
            };

        public int PhotoIronwood02Width => 734;

        public PhotoContent PhotoIronwoodPodContentReference =>
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
            };

        public string PhotoIronwoodPodFileName => "2020-05-Ironwood-Pod.jpg";

        public int PhotoIronwoodPodWidth => 700;

        public PhotoContent PhotoQuarryContentReference =>
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
            };

        public string PhotoQuarryFileName => "2020-05-A-Quarry-in-Ironwood-Forest-National-Monument.jpg";

        public int PhotoQuarryWidth => 1300;

        [OneTimeSetUp]
        public async Task A00_CreateTestSite()
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
        public async Task A10_PhotoLoadTest()
        {
            await PhotoValidation(PhotoAguaBlancaFileName, PhotoAguaBlancaContentReference, PhotoAguaBlancaWidth);
            await PhotoValidation(PhotoIronwood02FileName, PhotoIronwood02Reference, PhotoIronwood02Width);
            await PhotoValidation(PhotoQuarryFileName, PhotoQuarryContentReference, PhotoQuarryWidth);
            await PhotoValidation(PhotoIronwoodPodFileName, PhotoIronwoodPodContentReference, PhotoIronwoodPodWidth);
            await PhotoValidation(PhotoDisappearingFileName, PhotoDisappearingContentReference, PhotoDisappearingWidth);
        }

        //[Test]
        //public async Task A20_FileContentImportAndUpdate()
        //{
        //    var fileToImport = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestContent",
        //        "Papago-Saguaro-NM-Brief.pdf"));
        //    Assert.True(fileToImport.Exists, "Test file Papago-Saguaro-NM-Brief.pdf not found");

        //    var newFileContent = new FileContent
        //    {
        //        BodyContent = "Simple Test Content",
        //        BodyContentFormat = ContentFormatDefaults.Content.ToString(),
        //        ContentId = Guid.NewGuid(),
        //        ContentVersion = DateTime.Now.ToUniversalTime(),
        //        CreatedBy = "Test Series A",
        //        CreatedOn = DateTime.Now,
        //        Folder = "NationalParks",
        //        PublicDownloadLink = true,
        //        OriginalFileName = fileToImport.Name,
        //        Title = "Papago Saguaro",
        //        ShowInMainSiteFeed = true,
        //        Slug = SlugUtility.Create(true, "Papago Saguaro"),
        //        Summary = "A Summary",
        //        Tags = "national parks,phoenix,papago saguaro"
        //    };

        //    var validationReturn = await FileGenerator.Validate(newFileContent, fileToImport);

        //    Assert.False(validationReturn.HasError);

        //    var saveReturn = await FileGenerator.SaveAndGenerateHtml(newFileContent, fileToImport, true,
        //        DebugProgressTracker());
        //    Assert.False(saveReturn.generationReturn.HasError);

        //    var expectedDirectory =
        //        UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newFileContent);
        //    Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        //    var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileHtmlFile(newFileContent);
        //    Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        //    var expectedOriginalFileInContent =
        //        new FileInfo(Path.Combine(expectedDirectory.FullName, fileToImport.Name));
        //    Assert.IsTrue(expectedOriginalFileInContent.Exists,
        //        $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

        //    var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
        //        UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
        //        expectedOriginalFileInContent.Name));
        //    Assert.IsTrue(expectedOriginalFileInMediaArchive.Exists,
        //        $"Expected to find original file in media archive file directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");

        //    Assert.AreEqual(expectedDirectory.GetFiles().Length, 3, "Expected Number of Files Does Not Match");

        //    //Check JSON File
        //    var jsonFile =
        //        new FileInfo(Path.Combine(
        //            UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newFileContent).FullName,
        //            $"{Names.FileContentPrefix}{newFileContent.ContentId}.json"));
        //    Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        //    var jsonFileImported = Import.ContentFromFiles<FileContent>(
        //        new List<string> {jsonFile.FullName}, Names.FileContentPrefix).Single();
        //    var compareLogic = new CompareLogic();
        //    var comparisonResult = compareLogic.Compare(newFileContent, jsonFileImported);
        //    Assert.True(comparisonResult.AreEqual,
        //        $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");

        //    //?Check some details of the HTML?
        //    var updateWithoutUpdateResult = await FileGenerator.SaveAndGenerateHtml(newFileContent,
        //        expectedOriginalFileInMediaArchive, false, DebugProgressTracker());
        //    Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
        //        "Should not be able to update an entry without LastUpdated Values set");

        //    var updatedTime = DateTime.Now;
        //    newFileContent.Tags += ",testupdatetag";
        //    newFileContent.Title += " NP";
        //    newFileContent.LastUpdatedOn = updatedTime;
        //    newFileContent.LastUpdatedBy = "Test Updater";

        //    //?Check some details of the HTML?
        //    var updateResult = await FileGenerator.SaveAndGenerateHtml(newFileContent,
        //        expectedOriginalFileInMediaArchive, false, DebugProgressTracker());
        //    Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

        //    var updatedJsonFileImported = Import.ContentFromFiles<FileContent>(
        //        new List<string> {jsonFile.FullName}, Names.FileContentPrefix).Single();
        //    var updateComparisonResult = compareLogic.Compare(newFileContent, updatedJsonFileImported);
        //    Assert.True(updateComparisonResult.AreEqual,
        //        $"Updated Json Import does not match expected Updated File Content {comparisonResult.DifferencesString}");

        //    //Check Historic JSON File
        //    var historicJsonFile = new FileInfo(Path.Combine(
        //        UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newFileContent).FullName,
        //        $"{Names.HistoricFileContentPrefix}{newFileContent.ContentId}.json"));
        //    var historicJsonFileImported = Import
        //        .ContentFromFiles<List<HistoricFileContent>>(new List<string> {historicJsonFile.FullName},
        //            Names.HistoricFileContentPrefix).SelectMany(x => x).ToList();

        //    Assert.AreEqual(1, historicJsonFileImported.Count,
        //        "Wrong number of Historic Entries in the Historic Json File");

        //    var expectedHistoricValues = new HistoricFileContent();
        //    expectedHistoricValues.InjectFrom(jsonFileImported);
        //    expectedHistoricValues.Id = historicJsonFileImported.First().Id;

        //    var historicJsonComparisonResult =
        //        compareLogic.Compare(expectedHistoricValues, historicJsonFileImported.First());
        //    Assert.IsTrue(historicJsonComparisonResult.AreEqual,
        //        $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

        //    newFileContent.Title += " Again";
        //    newFileContent.LastUpdatedOn = DateTime.Now;
        //    newFileContent.LastUpdatedBy = "Test Updater 2";

        //    var updateTwoResult = await FileGenerator.SaveAndGenerateHtml(newFileContent,
        //        expectedOriginalFileInMediaArchive, false, DebugProgressTracker());
        //    Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

        //    historicJsonFileImported = Import
        //        .ContentFromFiles<List<HistoricFileContent>>(new List<string> {historicJsonFile.FullName},
        //            Names.HistoricFileContentPrefix).SelectMany(x => x).ToList();
        //    Assert.AreEqual(2, historicJsonFileImported.Count,
        //        "Wrong number of Historic Entries in the Historic Json File");
        //}

        //[Test]
        //public async Task A30_ImageContentImportAndUpdate()
        //{
        //    var fullSizeImageTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestContent",
        //        "2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg"));
        //    Assert.True(fullSizeImageTest.Exists,
        //        "Test Image 2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg not found");

        //    var newImageContent = new ImageContent
        //    {
        //        BodyContentFormat = ContentFormatDefaults.Content.ToString(),
        //        BodyContent = "Image Body Content",
        //        ContentId = Guid.NewGuid(),
        //        ContentVersion = DateTime.Now.ToUniversalTime(),
        //        CreatedBy = "A30_ImageContentImportAndUpdate",
        //        CreatedOn = DateTime.Now,
        //        Folder = "Bridges",
        //        ShowInSearch = false,
        //        Slug = SlugUtility.Create(true, "AZT Under Highway"),
        //        Summary = "A trail and bridge story",
        //        Tags = "arizona trail,highway 77,wash",
        //        Title = "AZT Under Highway",
        //        UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
        //    };

        //    var validationReturn = await ImageGenerator.Validate(newImageContent, fullSizeImageTest);

        //    Assert.False(validationReturn.HasError);

        //    var saveReturn = await ImageGenerator.SaveAndGenerateHtml(newImageContent, fullSizeImageTest, true,
        //        DebugProgressTracker());
        //    Assert.False(saveReturn.generationReturn.HasError);

        //    Assert.IsTrue(newImageContent.MainPicture == newImageContent.ContentId,
        //        $"Main Picture - {newImageContent.MainPicture} - Should be set to Content Id {newImageContent.ContentId}");

        //    var expectedDirectory =
        //        UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newImageContent);
        //    Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        //    var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteImageHtmlFile(newImageContent);
        //    Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

        //    var expectedOriginalImageFileInContent =
        //        new FileInfo(Path.Combine(expectedDirectory.FullName, fullSizeImageTest.Name));
        //    Assert.IsTrue(expectedOriginalImageFileInContent.Exists,
        //        $"Expected to find original image in content directory but {expectedOriginalImageFileInContent.FullName} does not exist");

        //    var expectedOriginalImageFileInMediaArchive = new FileInfo(Path.Combine(
        //        UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
        //        expectedOriginalImageFileInContent.Name));
        //    Assert.IsTrue(expectedOriginalImageFileInMediaArchive.Exists,
        //        $"Expected to find original image in media archive image directory but {expectedOriginalImageFileInMediaArchive.FullName} does not exist");

        //    //Checking the count of files is useful to make sure there are not any unexpected files
        //    var expectedNumberOfFiles =
        //        PictureResizing.SrcSetSizeAndQualityList()
        //            .Count //This image should trigger all sizes atm, this will need adjustment if the size list changes
        //        + 1 //Original image
        //        + 1 //Display image
        //        + 1 //html file
        //        + 1; //json file
        //    Assert.AreEqual(expectedDirectory.GetFiles().Length, expectedNumberOfFiles,
        //        "Expected Number of Files Does Not Match");

        //    //Check that the Picture Asset processing finds all the files

        //    var pictureAssetInformation = PictureAssetProcessing.ProcessPictureDirectory(newImageContent.ContentId);
        //    var pictureAssetImageDbEntry = (ImageContent) pictureAssetInformation.DbEntry;
        //    Assert.IsTrue(pictureAssetImageDbEntry.ContentId == newImageContent.ContentId,
        //        $"Picture Asset appears to have gotten an incorrect DB entry of {pictureAssetImageDbEntry.ContentId} rather than {newImageContent.ContentId}");
        //    Assert.AreEqual(pictureAssetInformation.LargePicture.Width, 4000,
        //        "Picture Asset Large Width is not the expected Value");
        //    Assert.AreEqual(pictureAssetInformation.SmallPicture.Width, 100,
        //        "Picture Asset Small Width is not the expected Value");
        //    Assert.AreEqual(pictureAssetInformation.SrcsetImages.Count,
        //        PictureResizing.SrcSetSizeAndQualityList().Count, "Did not find the expected number of SrcSet Images");

        //    //Check JSON File
        //    var jsonFile =
        //        new FileInfo(Path.Combine(
        //            UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newImageContent).FullName,
        //            $"{Names.ImageContentPrefix}{newImageContent.ContentId}.json"));
        //    Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

        //    var jsonFileImported = Import.ContentFromFiles<ImageContent>(
        //        new List<string> {jsonFile.FullName}, Names.ImageContentPrefix).Single();
        //    var compareLogic = new CompareLogic();
        //    var comparisonResult = compareLogic.Compare(newImageContent, jsonFileImported);
        //    Assert.True(comparisonResult.AreEqual,
        //        $"Json Import does not match expected Image Content {comparisonResult.DifferencesString}");

        //    //?Check some details of the HTML?
        //    var updateWithoutUpdateResult = await ImageGenerator.SaveAndGenerateHtml(newImageContent,
        //        expectedOriginalImageFileInMediaArchive, false, DebugProgressTracker());
        //    Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
        //        "Should not be able to update an entry without LastUpdated Values set");

        //    var updatedTime = DateTime.Now;
        //    newImageContent.Tags += ",testupdatetag";
        //    newImageContent.Title += " Updated";
        //    newImageContent.LastUpdatedOn = updatedTime;
        //    newImageContent.LastUpdatedBy = "Test Image Updater";

        //    //?Check some details of the HTML?
        //    var updateResult = await ImageGenerator.SaveAndGenerateHtml(newImageContent,
        //        expectedOriginalImageFileInMediaArchive, false, DebugProgressTracker());
        //    Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

        //    var updatedJsonFileImported = Import.ContentFromFiles<ImageContent>(
        //        new List<string> {jsonFile.FullName}, Names.ImageContentPrefix).Single();
        //    var updateComparisonResult = compareLogic.Compare(newImageContent, updatedJsonFileImported);
        //    Assert.True(updateComparisonResult.AreEqual,
        //        $"Updated Json Import does not match expected Updated Image Content {comparisonResult.DifferencesString}");

        //    //Check Historic JSON File
        //    var historicJsonFile = new FileInfo(Path.Combine(
        //        UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(newImageContent).FullName,
        //        $"{Names.HistoricImageContentPrefix}{newImageContent.ContentId}.json"));
        //    var historicJsonFileImported = Import
        //        .ContentFromFiles<List<HistoricImageContent>>(new List<string> {historicJsonFile.FullName},
        //            Names.HistoricImageContentPrefix).SelectMany(x => x).ToList();

        //    Assert.AreEqual(1, historicJsonFileImported.Count,
        //        "Wrong number of Historic Entries in the Historic Json File");

        //    var expectedHistoricValues = new HistoricImageContent();
        //    expectedHistoricValues.InjectFrom(jsonFileImported);
        //    expectedHistoricValues.Id = historicJsonFileImported.First().Id;

        //    var historicJsonComparisonResult =
        //        compareLogic.Compare(expectedHistoricValues, historicJsonFileImported.First());
        //    Assert.IsTrue(historicJsonComparisonResult.AreEqual,
        //        $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

        //    newImageContent.Title += " Again";
        //    newImageContent.LastUpdatedOn = DateTime.Now;
        //    newImageContent.LastUpdatedBy = "Test Image Updater 2";

        //    var updateTwoResult = await ImageGenerator.SaveAndGenerateHtml(newImageContent,
        //        expectedOriginalImageFileInMediaArchive, false, DebugProgressTracker());
        //    Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

        //    historicJsonFileImported = Import
        //        .ContentFromFiles<List<HistoricImageContent>>(new List<string> {historicJsonFile.FullName},
        //            Names.HistoricImageContentPrefix).SelectMany(x => x).ToList();
        //    Assert.AreEqual(2, historicJsonFileImported.Count,
        //        "Wrong number of Historic Entries in the Historic Json File");
        //}

        //[Test]
        //public async Task A40_NoteContentImportAndUpdate()
        //{
        //    var newNoteContent = new NoteContent
        //    {
        //        BodyContent = "Grand Canyon Permit Info Link",
        //        BodyContentFormat = ContentFormatDefaults.Content.ToString(),
        //        ContentId = Guid.NewGuid(),
        //        ContentVersion = DateTime.Now.ToUniversalTime(),
        //        CreatedBy = "Test Series A",
        //        CreatedOn = DateTime.Now,
        //        Folder = "GrandCanyon",
        //        ShowInMainSiteFeed = true,
        //        Slug = await NoteGenerator.UniqueNoteSlug(),
        //        Summary = "GC Quick Info",
        //        Tags = "national parks,grand canyon,permits"
        //    };

        //    var validationReturn = await NoteGenerator.Validate(newNoteContent);

        //    Assert.False(validationReturn.HasError);

        //    var saveReturn = await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
        //    Assert.False(saveReturn.generationReturn.HasError);

        //    var expectedDirectory =
        //        UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newNoteContent);
        //    Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

        //    var expectedNote = UserSettingsSingleton.CurrentSettings().LocalSiteNoteHtmlFile(newNoteContent);
        //    Assert.IsTrue(expectedNote.Exists, $"Expected html Note {expectedNote.FullName} does not exist");

        //    Assert.AreEqual(expectedDirectory.GetFiles().Length, 2, "Expected Number of Files Does Not Match");

        //    //Check JSON Note
        //    var jsonNote =
        //        new FileInfo(Path.Combine(
        //            UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newNoteContent).FullName,
        //            $"{Names.NoteContentPrefix}{newNoteContent.ContentId}.json"));
        //    Assert.True(jsonNote.Exists, $"Json Note {jsonNote.FullName} does not exist?");

        //    var jsonNoteImported = Import.ContentFromFiles<NoteContent>(
        //        new List<string> {jsonNote.FullName}, Names.NoteContentPrefix).Single();
        //    var compareLogic = new CompareLogic();
        //    var comparisonResult = compareLogic.Compare(newNoteContent, jsonNoteImported);
        //    Assert.True(comparisonResult.AreEqual,
        //        $"Json Import does not match expected Note Content {comparisonResult.DifferencesString}");

        //    //?Check some details of the HTML?
        //    var updateWithoutUpdateResult =
        //        await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
        //    Assert.True(updateWithoutUpdateResult.generationReturn.HasError,
        //        "Should not be able to update an entry without LastUpdated Values set");

        //    var updatedTime = DateTime.Now;
        //    newNoteContent.Tags += ",testupdatetag";
        //    newNoteContent.LastUpdatedOn = updatedTime;
        //    newNoteContent.LastUpdatedBy = "Test Updater";

        //    //?Check some details of the HTML?
        //    var updateResult = await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
        //    Assert.True(!updateResult.generationReturn.HasError, "Problem Updating Item");

        //    var updatedJsonNoteImported = Import.ContentFromFiles<NoteContent>(
        //        new List<string> {jsonNote.FullName}, Names.NoteContentPrefix).Single();
        //    var updateComparisonResult = compareLogic.Compare(newNoteContent, updatedJsonNoteImported);
        //    Assert.True(updateComparisonResult.AreEqual,
        //        $"Updated Json Import does not match expected Updated Note Content {comparisonResult.DifferencesString}");

        //    //Check Historic JSON Note
        //    var historicJsonNote = new FileInfo(Path.Combine(
        //        UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newNoteContent).FullName,
        //        $"{Names.HistoricNoteContentPrefix}{newNoteContent.ContentId}.json"));
        //    var historicJsonNoteImported = Import
        //        .ContentFromFiles<List<HistoricNoteContent>>(new List<string> {historicJsonNote.FullName},
        //            Names.HistoricNoteContentPrefix).SelectMany(x => x).ToList();

        //    Assert.AreEqual(1, historicJsonNoteImported.Count,
        //        "Wrong number of Historic Entries in the Historic Json Note");

        //    var expectedHistoricValues = new HistoricNoteContent();
        //    expectedHistoricValues.InjectFrom(jsonNoteImported);
        //    expectedHistoricValues.Id = historicJsonNoteImported.First().Id;

        //    var historicJsonComparisonResult =
        //        compareLogic.Compare(expectedHistoricValues, historicJsonNoteImported.First());
        //    Assert.IsTrue(historicJsonComparisonResult.AreEqual,
        //        $"Historic JSON Entry doesn't have the expected values {historicJsonComparisonResult.DifferencesString}");

        //    newNoteContent.LastUpdatedOn = DateTime.Now;
        //    newNoteContent.LastUpdatedBy = "Test Updater 2";

        //    var updateTwoResult = await NoteGenerator.SaveAndGenerateHtml(newNoteContent, DebugProgressTracker());
        //    Assert.True(!updateTwoResult.generationReturn.HasError, "Problem Updating Item");

        //    historicJsonNoteImported = Import
        //        .ContentFromFiles<List<HistoricNoteContent>>(new List<string> {historicJsonNote.FullName},
        //            Names.HistoricNoteContentPrefix).SelectMany(x => x).ToList();
        //    Assert.AreEqual(2, historicJsonNoteImported.Count,
        //        "Wrong number of Historic Entries in the Historic Json Note");
        //}

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

        public void PhotoCheckFileCountAndPictureAssetsAfterHtmlGeneration(PhotoContent newPhotoContent, int photoWidth)
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

        public void PhotoCheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(PhotoContent newPhotoContent)
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

        public (bool hasInvalidComparison, string comparisonNotes) PhotoComparePhotoReferenceToPhotoObject(
            PhotoContent reference, PhotoContent toCompare)
        {
            var failure = false;
            var failureList = new List<string>();

            if (reference.AltText != toCompare.AltText)
            {
                failure = true;
                failureList.Add($"Error - AltText: Expected {reference.AltText}, Actual {toCompare.AltText}");
            }

            if (reference.Aperture != toCompare.Aperture)
            {
                failure = true;
                failureList.Add($"Error - Aperture: Expected {reference.Aperture}, Actual {toCompare.Aperture}");
            }

            if (reference.BodyContent != toCompare.BodyContent)
            {
                failure = true;
                failureList.Add(
                    $"Error - BodyContent: Expected {reference.BodyContent}, Actual {toCompare.BodyContent}");
            }

            if (reference.BodyContentFormat != toCompare.BodyContentFormat)
            {
                failure = true;
                failureList.Add(
                    $"Error - BodyContentFormat: Expected {reference.BodyContentFormat}, Actual {toCompare.BodyContentFormat}");
            }

            if (reference.CameraMake != toCompare.CameraMake)
            {
                failure = true;
                failureList.Add($"Error - CameraMake: Expected {reference.CameraMake}, Actual {toCompare.CameraMake}");
            }

            if (reference.CameraModel != toCompare.CameraModel)
            {
                failure = true;
                failureList.Add(
                    $"Error - CameraModel: Expected {reference.CameraModel}, Actual {toCompare.CameraModel}");
            }

            if (reference.Folder != toCompare.Folder)
            {
                failure = true;
                failureList.Add($"Error - Folder: Expected {reference.Folder}, Actual {toCompare.Folder}");
            }

            if (reference.Iso != toCompare.Iso)
            {
                failure = true;
                failureList.Add($"Error - Iso: Expected {reference.Iso}, Actual {toCompare.Iso}");
            }

            if (reference.FocalLength != toCompare.FocalLength)
            {
                failure = true;
                failureList.Add(
                    $"Error - FocalLength: Expected {reference.FocalLength}, Actual {toCompare.FocalLength}");
            }

            if (reference.Lens != toCompare.Lens)
            {
                failure = true;
                failureList.Add($"Error - Lens: Expected {reference.Lens}, Actual {toCompare.Lens}");
            }

            if (reference.License != toCompare.License)
            {
                failure = true;
                failureList.Add($"Error - License: Expected {reference.License}, Actual {toCompare.License}");
            }

            if (reference.PhotoCreatedBy != toCompare.PhotoCreatedBy)
            {
                failure = true;
                failureList.Add(
                    $"Error - License: Expected {reference.PhotoCreatedBy}, Actual {toCompare.PhotoCreatedBy}");
            }

            if (reference.PhotoCreatedOn != toCompare.PhotoCreatedOn)
            {
                failure = true;
                failureList.Add(
                    $"Error - License: Expected {reference.PhotoCreatedOn}, Actual {toCompare.PhotoCreatedOn}");
            }

            if (reference.ShutterSpeed != toCompare.ShutterSpeed)
            {
                failure = true;
                failureList.Add(
                    $"Error - ShutterSpeed: Expected {reference.ShutterSpeed}, Actual {toCompare.ShutterSpeed}");
            }

            if (reference.Summary != toCompare.Summary)
            {
                failure = true;
                failureList.Add($"Error - Summary: Expected {reference.Summary}, Actual {toCompare.Summary}");
            }

            if (reference.Title != toCompare.Title)
            {
                failure = true;
                failureList.Add($"Error - Title: Expected {reference.Title}, Actual {toCompare.Title}");
            }

            if (reference.Tags != toCompare.Tags)
            {
                failure = true;
                failureList.Add($"Error - Tags: Expected {reference.Tags}, Actual {toCompare.Tags}");
            }

            return (failure, string.Join(Environment.NewLine, failureList));
        }

        public void PhotoHtmlChecks(PhotoContent newPhotoContent)
        {
            var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newPhotoContent);

            Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

            var config = Configuration.Default;

            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var source = File.ReadAllText(htmlFile.FullName);
            var document = parser.ParseDocument(source);

            Assert.AreEqual(newPhotoContent.Title, document.Title);

            //Todo - check description

            Assert.AreEqual(newPhotoContent.Tags,
                document.QuerySelector("meta[name='keywords']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteName,
                document.QuerySelector("meta[property='og:site_name']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual($"https:{UserSettingsSingleton.CurrentSettings().PhotoPageUrl(newPhotoContent)}",
                document.QuerySelector("meta[property='og:url']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual("article",
                document.QuerySelector("meta[property='og:type']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            //Todo: Continue checking...
        }

        public void PhotoJsonTest(PhotoContent newPhotoContent)
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

        public async Task PhotoValidation(string photoFileName, PhotoContent photoReference, int photoWidth)
        {
            var fullSizePhotoTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "IronwoodTestContent",
                photoFileName));
            Assert.True(fullSizePhotoTest.Exists, "Test File Found");

            var (metadataGenerationReturn, newPhotoContent) =
                await PhotoGenerator.PhotoMetadataToNewPhotoContent(fullSizePhotoTest, DebugProgressTracker());
            Assert.False(metadataGenerationReturn.HasError, metadataGenerationReturn.GenerationNote);

            var photoComparison = PhotoComparePhotoReferenceToPhotoObject(photoReference, newPhotoContent);
            Assert.False(photoComparison.hasInvalidComparison, photoComparison.comparisonNotes);

            var validationReturn = await PhotoGenerator.Validate(newPhotoContent, fullSizePhotoTest);
            Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

            var saveReturn = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent, fullSizePhotoTest, true,
                DebugProgressTracker());
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