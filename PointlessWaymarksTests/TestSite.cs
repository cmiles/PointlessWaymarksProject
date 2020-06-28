using System;
using System.Threading.Tasks;
using NUnit.Framework;
using PointlessWaymarksCmsData;

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