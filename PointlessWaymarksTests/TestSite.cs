using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Generation;

namespace PointlessWaymarksTests
{
    public class TestSite
    {
        public const string TestSiteName = "PwTestAyvzztsxH";

        public static UserSettings TestSiteSettings;

        public static IProgress<string> ConsoleProgressTracker()
        {
            var toReturn = new Progress<string>();
            toReturn.ProgressChanged += ConsoleProgressTrackerChange;
            return toReturn;
        }

        private static void ConsoleProgressTrackerChange(object sender, string e)
        {
            Console.WriteLine(e);
        }

        [OneTimeSetUp]
        public async Task CreateTestSite()
        {
            var outSettings = await UserSettingsUtilities.SetupNewSite(TestSiteName, ConsoleProgressTracker());
            TestSiteSettings = outSettings;
        }

        [Test]
        public async Task ImportAndGeneratePhoto()
        {
            var fullSizePhotoTest = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestContent",
                "2019-01-Bridge-Under-Highway-77-on-the-Arizona-Trail.jpg"));
            Assert.True(fullSizePhotoTest.Exists);

            var (generationReturn, newPhotoContent) =
                await PhotoGenerator.PhotoMetadataToPhotoContent(fullSizePhotoTest, ConsoleProgressTracker());

            Assert.False(generationReturn.HasError);

            var validationReturn = await PhotoGenerator.Validate(newPhotoContent, fullSizePhotoTest);

            Assert.False(validationReturn.HasError);

            var saveReturn = await PhotoGenerator.SaveAndGenerateHtml(newPhotoContent, fullSizePhotoTest, true,
                ConsoleProgressTracker());

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