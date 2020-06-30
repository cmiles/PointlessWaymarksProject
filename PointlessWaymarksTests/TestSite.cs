using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Generation;

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

            Assert.False(generationReturn.HasError);

            var validationReturn = await PhotoGenerator.Validate(newPhotoContent, fullSizePhotoTest);

            Assert.False(validationReturn.HasError);

            var saveReturn = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent, fullSizePhotoTest, true,
                DebugProgressTracker());

            //TODO: Need to test that files were generated

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