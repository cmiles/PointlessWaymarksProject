using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Generation;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksTests
{
    public class TestSite
    {
        public const string TestSiteName = "AutomatedTestSite";

        public static UserSettings TestSiteSettings;

        [OneTimeSetUp]
        public async Task CreateTestSite()
        {
            var outSettings =
                await UserSettingsUtilities.SetupNewSite($"AutomatedTesting-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}",
                    DebugProgressTracker());
            TestSiteSettings = outSettings;
            await TestSiteSettings.EnsureDbIsPresent(DebugProgressTracker());
        }

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

        [Test]
        public async Task ImportAndGeneratePhoto()
        {
            var fullSizePhotoTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestContent",
                "2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg"));
            Assert.True(fullSizePhotoTest.Exists,
                "Test Photo 2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg not found");

            var (generationReturn, newPhotoContent) =
                await PhotoGenerator.PhotoMetadataToPhotoContent(fullSizePhotoTest, "Automated Tester",
                    DebugProgressTracker());

            //Check the Metadata
            Assert.IsTrue(newPhotoContent.Title == "2019 January Bridge Under Highway 77 on the Arizona Trail",
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

            Assert.IsTrue(newPhotoContent.Folder == newPhotoContent.PhotoCreatedOn.ToString("yyyy"), $"Default Folder was {newPhotoContent.Folder} but should be Photo Taken on Year {newPhotoContent.PhotoCreatedOn:yyyy}");
            Assert.IsTrue(newPhotoContent.Slug == "2019-january-bridge-under-highway-77-on-the-arizona-trail", $"Slug is {newPhotoContent.Slug} Should be 2019-january-bridge-under-highway-77-on-the-arizona-trail");

            Assert.False(generationReturn.HasError);

            var validationReturn = await PhotoGenerator.Validate(newPhotoContent, fullSizePhotoTest);

            Assert.False(validationReturn.HasError);

            var saveReturn = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent, fullSizePhotoTest, true,
                DebugProgressTracker());

            Assert.IsTrue(newPhotoContent.MainPicture == newPhotoContent.ContentId, $"Main Picture - {newPhotoContent.MainPicture} - Should be set to Content Id {newPhotoContent.ContentId}");

            var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(newPhotoContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSitePhotoHtmlFile(newPhotoContent);
            Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

            var expectedOriginalPhotoFileInContent = new FileInfo(Path.Combine(expectedDirectory.FullName, fullSizePhotoTest.Name));
            Assert.IsTrue(expectedOriginalPhotoFileInContent.Exists, $"Expected to find original photo in content directory but {expectedOriginalPhotoFileInContent.FullName} does not exist");

            var expectedOriginalPhotoFileInMediaArchive = new FileInfo(Path.Combine(UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName, expectedOriginalPhotoFileInContent.Name));
            Assert.IsTrue(expectedOriginalPhotoFileInMediaArchive.Exists, $"Expected to find original photo in media archive photo directory but {expectedOriginalPhotoFileInMediaArchive.FullName} does not exist");

            //Checking the count of files is useful to make sure there are not any unexpected files
            var expectedNumberOfFiles =
                PictureResizing.SrcSetSizeAndQualityList()
                    .Count //This image should trigger all sizes atm, this will need adjustment if the size list changes
                + 1 //Original image
                + 1 //Display image
                + 1 //html file
                + 1; //json file
            Assert.AreEqual(expectedDirectory.GetFiles().Length, expectedNumberOfFiles, "Expected Number of Files Does Not Match" );

            //Check that the Picture Asset processing finds all the files
            //Check JSON File
            //?Check some details of the HTML

            Assert.False(saveReturn.HasError);
        }

        [Test]
        public void TestSiteBasicStructureCheck()
        {
            Assert.True(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchiveImageDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.True(TestSiteSettings.LocalMediaArchivePhotoDirectory().Exists);
            Assert.True(TestSiteSettings.LocalSiteDirectory().Exists);
        }
    }
}