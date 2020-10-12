using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.FileContentEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksTests
{
    public class TestSeries03TrailInfoGuiContextTest
    {
        public const string TestSiteName = "Trail Notes";
        public const string TestDefaultCreatedBy = "Trail Notes Ghost Writer";
        public const string TestSiteAuthors = "Pointless Waymarks Trail Notes 'Testers'";
        public const string TestSiteEmailTo = "Trail@Notes.Fake";

        public const string TestSiteKeywords = "trails, notes";

        public const string TestSummary = "'Testing' on foot";

        public static UserSettings TestSiteSettings;


        [OneTimeSetUp]
        public async Task A00_CreateTestSite()
        {
            var outSettings = await UserSettingsUtilities.SetupNewSite(
                $"TrailNotesTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}", DebugTrackers.DebugProgressTracker());
            TestSiteSettings = outSettings;
            TestSiteSettings.SiteName = TestSiteName;
            TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
            TestSiteSettings.SiteAuthors = TestSiteAuthors;
            TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
            TestSiteSettings.SiteKeywords = TestSiteKeywords;
            TestSiteSettings.SiteSummary = TestSummary;
            TestSiteSettings.SiteUrl = "localhost";
            await TestSiteSettings.EnsureDbIsPresent(DebugTrackers.DebugProgressTracker());
            await TestSiteSettings.WriteSettings();
            UserSettingsSingleton.CurrentSettings().InjectFrom(TestSiteSettings);
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
        public async Task B01_NewFile()
        {
            ThreadSwitcher.PinnedDispatcher = Dispatcher.CurrentDispatcher;
            DataNotifications.SuspendNotifications = false;
            DataNotifications.NewDataNotificationChannel().MessageReceived += DebugTrackers.DataNotificationDiagnostic;

            var testFileInOriginalLocation = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(),
                "IronwoodTestContent", TestFileInfo.TrailInfoGrandviewFilename));

            var newFileContext = await FileContentEditorContext.CreateInstance(null, testFileInOriginalLocation);

            //Blank Title is the only validation issue
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = TestFileInfo.GrandviewContent01.Title;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

            //Slug Tests
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            //Lowercase only
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug.ToUpper();
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            //Can't check every excluded character so just check a few - ( is not legal
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = $"({TestFileInfo.GrandviewContent01.Slug}(";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            //Can't check every excluded character so just check a few - , is not legal
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue =
                $"{TestFileInfo.GrandviewContent01.Slug},{TestFileInfo.GrandviewContent01.Slug}";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            //Can't check every excluded character so just check a few - [whitespace] is not legal
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue =
                $"{TestFileInfo.GrandviewContent01.Slug}   {TestFileInfo.GrandviewContent01.Slug}";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            //Check that - and _ are allowed
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue =
                $"----____{TestFileInfo.GrandviewContent01.Slug}---____";
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            //Check that 100 characters are allowed but 101 fails
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = $"{new string('-', 100)}";
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = $"{new string('-', 101)}";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasChanges);

            //Blank Summary is the only validation issue
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = TestFileInfo.GrandviewContent01.Summary;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);


            //Folder Tests
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Folder;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            //Upper Case permitted
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue =
                TestFileInfo.GrandviewContent01.Slug.ToUpper();
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            //Spaces not permitted
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "Test With Space";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            //Absolute File Path not permitted
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "C:\\TestFolder";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            //Rel File Path not permitted
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "\\TestFolder";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            //Comma not permitted
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "Test,Folder";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.HasChanges);


        }
    }
}