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

            //Initial State is blank and invalid
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

            //Spaces detected as blank
            newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = "             ";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

            //Null detected as blank
            newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = null;
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

            //Empty String detected as blank
            newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = string.Empty;
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

            //Valid Title
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

            //Valid Slug Entry
            newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SlugEntry.HasChanges);




            //Folder Tests
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);

            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Folder;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasChanges);

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

            //Valid Entry for Folder
            newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Folder;
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.FolderEntry.HasChanges);




            //Summary Tests

            //Blank is not valid and is starting condition
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);

            //Valid Entry
            newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "Simple Summary";
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);

            //Blank not permitted
            newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);

            //Null not permitted and handled ok
            newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = null;
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);

            //All spaces detected as blank
            newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "              ";
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);

            //Valid Summary
            newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "Simple Summary";
            Assert.IsFalse(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.IsTrue(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);



            //Tags

            //Invalid initial blank state
            Assert.IsTrue(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsFalse(newFileContext.TagEdit.HasChanges);

            //Single valid tag
            newFileContext.TagEdit.Tags = "simple test";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);

            //Blanks as nothing
            newFileContext.TagEdit.Tags = "    ";
            Assert.IsTrue(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsFalse(newFileContext.TagEdit.HasChanges);

            //Null as nothing
            newFileContext.TagEdit.Tags = null;
            Assert.IsTrue(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsFalse(newFileContext.TagEdit.HasChanges);

            //Reset to good state
            newFileContext.TagEdit.Tags = "simple test";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);

            //Test invalid symbol is removed in processing
            newFileContext.TagEdit.Tags = "simple test; another ";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);
            Assert.AreEqual("simple test another ", newFileContext.TagEdit.Tags);

            //Capitals removed
            newFileContext.TagEdit.Tags = "  SIMPLE TEST ";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);
            Assert.AreEqual("  simple test ", newFileContext.TagEdit.Tags);

            //Hyphens Valid - 3 tags
            newFileContext.TagEdit.Tags = "test-one, test--two, test---three";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);
            Assert.AreEqual(3, newFileContext.TagEdit.TagList().Count);

            //New Line not Valid
            newFileContext.TagEdit.Tags = "test-1, test--2, \r\n test---3";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);
            Assert.AreEqual("test-1, test--2,  test---3", newFileContext.TagEdit.Tags);

            //Hyphens Valid - 3 tags
            newFileContext.TagEdit.Tags = "test-1, test--2, test---3";
            Assert.IsFalse(newFileContext.TagEdit.HasValidationIssues);
            Assert.IsTrue(newFileContext.TagEdit.HasChanges);
            Assert.AreEqual(3, newFileContext.TagEdit.TagList().Count);
        }
    }
}