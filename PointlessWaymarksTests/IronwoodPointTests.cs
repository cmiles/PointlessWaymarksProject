using System;
using System.Threading.Tasks;
using NUnit.Framework;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksTests
{
    public class IronwoodPointTests
    {
        public const string TestSiteName = "Ironwood Forest's Test Site";
        public const string TestDefaultCreatedBy = "Ironwood Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Ironwood 'Testers'";
        public const string TestSiteEmailTo = "Ironwood@Forest.Fake";

        public const string TestSiteKeywords =
            "ironwood forest national monument, samaniego hills, waterman mountains, test'ing";

        public const string TestSummary = "'Testing' in the beautiful Sonoran Desert";

        public static UserSettings TestSiteSettings;


        [OneTimeSetUp]
        public async Task A00_CreateTestSite()
        {
            var outSettings = await UserSettingsUtilities.SetupNewSite(
                $"IronwoodForestTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}", DebugTrackers.DebugProgressTracker());
            TestSiteSettings = outSettings;
            TestSiteSettings.SiteName = TestSiteName;
            TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
            TestSiteSettings.SiteAuthors = TestSiteAuthors;
            TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
            TestSiteSettings.SiteKeywords = TestSiteKeywords;
            TestSiteSettings.SiteSummary = TestSummary;
            TestSiteSettings.SiteUrl = "IronwoodTest.com";
            await TestSiteSettings.EnsureDbIsPresent(DebugTrackers.DebugProgressTracker());
            await TestSiteSettings.WriteSettings();
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
    }
}