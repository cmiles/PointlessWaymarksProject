using System.Windows;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;

namespace PointlessWaymarks.CmsTests;

public class TestSeries03TrailInfoGuiContextTest
{
    public const string TestDefaultCreatedBy = "Trail Notes Ghost Writer";
    public const string TestSiteAuthors = "Pointless Waymarks Trail Notes 'Testers'";
    public const string TestSiteEmailTo = "Trail@Notes.Fake";

    public const string TestSiteKeywords = "trails, notes";
    public const string TestSiteName = "Trail Notes";

    public const string TestSummary = "'Testing' on foot";

    public static UserSettings TestSiteSettings;

    [OneTimeSetUp]
    public async Task A00_CreateTestSite()
    {
        //This is one of the lower answers from the StackOverflow question below - I found this
        //to be a very easy and understandable way to allow WPF GUI oriented code that contains
        //sections that must run on the GUI thread to run without issue.
        //
        //https://stackoverflow.com/questions/1106881/using-the-wpf-dispatcher-in-unit-tests
        var waitForApplicationRun = new TaskCompletionSource<bool>();
#pragma warning disable 4014
        Task.Run(() =>
#pragma warning restore 4014
        {
            var application = new Application();
            application.Startup += (s, e) => { waitForApplicationRun.SetResult(true); };
            application.Run();
        });
        waitForApplicationRun.Task.Wait();

        var outSettings = await UserSettingsUtilities.SetupNewSite(
            $"TrailNotesTestSite-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}", DebugTrackers.DebugProgressTracker());
        TestSiteSettings = outSettings;
        TestSiteSettings.SiteName = TestSiteName;
        TestSiteSettings.DefaultCreatedBy = TestDefaultCreatedBy;
        TestSiteSettings.SiteAuthors = TestSiteAuthors;
        TestSiteSettings.SiteEmailTo = TestSiteEmailTo;
        TestSiteSettings.SiteKeywords = TestSiteKeywords;
        TestSiteSettings.SiteSummary = TestSummary;
        TestSiteSettings.SiteDomainName = "localhost";
        await UserSettingsUtilities.EnsureDbIsPresent(DebugTrackers.DebugProgressTracker());
        await TestSiteSettings.WriteSettings();
        UserSettingsSingleton.CurrentSettings().InjectFrom(TestSiteSettings);

        PointlessWaymarksLogTools.InitializeStaticLoggerAsEventLogger();
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
        DataNotifications.SuspendNotifications = false;
        DataNotifications.NewDataNotificationChannel().MessageReceived += DebugTrackers.DataNotificationDiagnostic;

        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            TestFileInfo.TrailInfoGrandviewFilename));

        var newFileContext = await FileContentEditorContext.CreateInstance(null, testFile);

        //Starting State
        Assert.False(newFileContext.SelectedFileHasValidationIssues);
        Assert.True(newFileContext.SelectedFileHasPathOrNameChanges);

        //Simulate no file
        newFileContext.SelectedFile = null;

        Assert.That(() => newFileContext.SelectedFileHasValidationIssues, Is.True.After(4000));
        Assert.False(newFileContext.SelectedFileHasPathOrNameChanges);

        //To make clean URLs Files have a restricted set of allowed characters
        var illegalCharacterTestFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "GrandviewTrail'sIllegalName.pdf"));

        newFileContext.SelectedFile = illegalCharacterTestFile;

        Assert.That(() => newFileContext.SelectedFileHasValidationIssues, Is.True.After(1000));
        Assert.True(newFileContext.SelectedFileHasPathOrNameChanges);


        //Back to Valid File
        newFileContext.SelectedFile = testFile;

        Assert.That(() => newFileContext.SelectedFileHasValidationIssues, Is.False.After(1000));
        Assert.True(newFileContext.SelectedFileHasPathOrNameChanges);


        //Blank Title is the only validation issue

        //Initial State - 
        Assert.IsFalse(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
        Assert.IsTrue(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);

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

        newFileContext.TitleSummarySlugFolder.TitleToSlug();
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
        newFileContext.TitleSummarySlugFolder.TitleToSlug();
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


        //Created/Updated By

        Assert.AreEqual("Trail Notes Ghost Writer", newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue);
        Assert.True(newFileContext.CreatedUpdatedDisplay.HasChanges);
        Assert.False(newFileContext.CreatedUpdatedDisplay.HasValidationIssues);

        newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue = "   ";
        Assert.False(newFileContext.CreatedUpdatedDisplay.HasChanges);
        Assert.True(newFileContext.CreatedUpdatedDisplay.HasValidationIssues);

        newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue = "Trail Notes Ghost Writer";
        Assert.True(newFileContext.CreatedUpdatedDisplay.HasChanges);
        Assert.False(newFileContext.CreatedUpdatedDisplay.HasValidationIssues);


        //Body Text
        Assert.False(newFileContext.BodyContent.HasChanges);
        newFileContext.BodyContent.BodyContent =
            "UI Context Testing File with Bad Content Id {{postlink c3c63473-6b60-4531-97f7-2d201d84e2be; Cocopa and Yuma Points, Grand Canyon - 9/30-10/1/2020}}";
        Assert.True(newFileContext.BodyContent.HasChanges);

        //Update Text
        Assert.False(newFileContext.UpdateNotes.HasChanges);
        newFileContext.UpdateNotes.UpdateNotes =
            "UI Context Testing File with Bad Content Id {{postlink c3c63473-6b60-4531-97f7-2d201d84e2be; text Bad Content Id Text ;Cocopa and Yuma Points, Grand Canyon - 9/30-10/1/2020}}";
        Assert.True(newFileContext.UpdateNotes.HasChanges);

        var validationResult = await FileGenerator.Validate(newFileContext.CurrentStateToFileContent(),
            newFileContext.SelectedFile);
        Assert.True(validationResult.HasError);


        //Body Text
        newFileContext.BodyContent.BodyContent = "UI Context Testing File";
        Assert.True(newFileContext.BodyContent.HasChanges);

        validationResult = await FileGenerator.Validate(newFileContext.CurrentStateToFileContent(),
            newFileContext.SelectedFile);
        Assert.True(validationResult.HasError);

        //Update Text
        newFileContext.UpdateNotes.UpdateNotes = "UI Context Testing File Update";
        Assert.True(newFileContext.UpdateNotes.HasChanges);

        validationResult = await FileGenerator.Validate(newFileContext.CurrentStateToFileContent(),
            newFileContext.SelectedFile);
        Assert.False(validationResult.HasError);

        var (generationReturn, _) = await FileGenerator.SaveAndGenerateHtml(
            newFileContext.CurrentStateToFileContent(), newFileContext.SelectedFile, false, null,
            DebugTrackers.DebugProgressTracker());

        Assert.IsFalse(generationReturn.HasError);

        var db = await Db.Context();

        Assert.AreEqual(1, db.FileContents.Count());

        var dbContent = await db.FileContents.FirstOrDefaultAsync();

        Assert.True(dbContent.OriginalFileName == newFileContext.SelectedFile.Name);
        Assert.True(dbContent.Title == newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue);
        Assert.True(dbContent.Slug == newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue);
        Assert.True(dbContent.Folder == newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue);
        Assert.True(dbContent.Summary == newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue);
        Assert.True(dbContent.Tags == newFileContext.TagEdit.TagListString());
        Assert.True(dbContent.CreatedBy == newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue);
        Assert.True(dbContent.BodyContent == newFileContext.BodyContent.BodyContent);
        Assert.True(dbContent.UpdateNotes == newFileContext.UpdateNotes.UpdateNotes);
    }

    [OneTimeTearDown]
    public void Z01_TearDown()
    {
        //See the note and code in the setup method
        //
        //https://stackoverflow.com/questions/1106881/using-the-wpf-dispatcher-in-unit-tests
        Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
    }
}