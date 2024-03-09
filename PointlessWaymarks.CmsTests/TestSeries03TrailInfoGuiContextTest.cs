using System.Windows;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
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
        Assert.Multiple(() =>
        {
            Assert.That(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
            Assert.That(TestSiteSettings.LocalMediaArchiveImageDirectory().Exists);
        });
        Assert.That(TestSiteSettings.LocalMediaArchiveFileDirectory().Exists);
        Assert.That(TestSiteSettings.LocalMediaArchivePhotoDirectory().Exists);
        Assert.That(TestSiteSettings.LocalSiteDirectory().Exists);
    }

    [Test]
    public async Task B01_NewFile()
    {
        DataNotifications.SuspendNotifications = false;
        DataNotifications.NewDataNotificationChannel().MessageReceived += DebugTrackers.DataNotificationDiagnostic;

        var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            TestFileInfo.TrailInfoGrandviewFilename));

        var newFileContext = await FileContentEditorContext.CreateInstance(null, testFile);

        Assert.Multiple(() =>
        {
            //Starting State
            Assert.That(newFileContext.SelectedFileHasValidationIssues, Is.False);
            Assert.That(newFileContext.SelectedFileHasPathOrNameChanges);
        });

        //Simulate no file
        newFileContext.SelectedFile = null;

        Assert.That(() => newFileContext.SelectedFileHasValidationIssues, Is.True.After(4000));
        Assert.That(newFileContext.SelectedFileHasPathOrNameChanges, Is.False);

        //To make clean URLs Files have a restricted set of allowed characters
        var illegalCharacterTestFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "TestMedia",
            "GrandviewTrail'sIllegalName.pdf"));

        newFileContext.SelectedFile = illegalCharacterTestFile;

        Assert.That(() => newFileContext.SelectedFileHasValidationIssues, Is.True.After(1000));
        Assert.That(newFileContext.SelectedFileHasPathOrNameChanges);


        //Back to Valid File
        newFileContext.SelectedFile = testFile;

        Assert.That(() => newFileContext.SelectedFileHasValidationIssues, Is.False.After(1000));
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.SelectedFileHasPathOrNameChanges);


            //Blank Title is the only validation issue

            //Initial State - 
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);
        });

        //Spaces detected as blank
        newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = "             ";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges, Is.False);
        });

        //Null detected as blank
        newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = null;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges, Is.False);
        });

        //Empty String detected as blank
        newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = string.Empty;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges, Is.False);
        });

        //Valid Title
        newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue = TestFileInfo.GrandviewContent01.Title;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.TitleEntry.HasChanges);


            //Slug Tests
            Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
        });

        newFileContext.TitleSummarySlugFolder.TitleToSlug();
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);

        //Lowercase only
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug.ToUpper();
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);

        //Can't check every excluded character so just check a few - ( is not legal
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = $"({TestFileInfo.GrandviewContent01.Slug}(";
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);

        //Can't check every excluded character so just check a few - , is not legal
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue =
            $"{TestFileInfo.GrandviewContent01.Slug},{TestFileInfo.GrandviewContent01.Slug}";
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);

        //Can't check every excluded character so just check a few - [whitespace] is not legal
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue =
            $"{TestFileInfo.GrandviewContent01.Slug}   {TestFileInfo.GrandviewContent01.Slug}";
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);

        //Check that - and _ are allowed
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue =
            $"----____{TestFileInfo.GrandviewContent01.Slug}---____";
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);

        //Check that 100 characters are allowed but 101 fails
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = $"{new string('-', 100)}";
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);
        newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue = $"{new string('-', 101)}";
        Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues);

        //Valid Slug Entry
        newFileContext.TitleSummarySlugFolder.TitleToSlug();
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.SlugEntry.HasChanges);


            //Folder Tests
            Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
        });

        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Folder;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasChanges);
        });

        //Upper Case permitted
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue =
            TestFileInfo.GrandviewContent01.Slug.ToUpper();
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);

        //Spaces not permitted
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "Test With Space";
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);

        //Absolute File Path not permitted
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "C:\\TestFolder";
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);

        //Rel File Path not permitted
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "\\TestFolder";
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);

        //Comma not permitted
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = "Test,Folder";
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues);
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Slug;
        Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);

        //Valid Entry for Folder
        newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue = TestFileInfo.GrandviewContent01.Folder;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.FolderEntry.HasChanges);


            //Summary Tests

            //Blank is not valid and is starting condition
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges, Is.False);
        });

        //Valid Entry
        newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "Simple Summary";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);
        });

        //Blank not permitted
        newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges, Is.False);
        });

        //Null not permitted and handled ok
        newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = null;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges, Is.False);
        });

        //All spaces detected as blank
        newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "              ";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues);
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges, Is.False);
        });

        //Valid Summary
        newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue = "Simple Summary";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TitleSummarySlugFolder.SummaryEntry.HasChanges);


            //Tags

            //Invalid initial blank state
            Assert.That(newFileContext.TagEdit.HasValidationIssues);
            Assert.That(newFileContext.TagEdit.HasChanges, Is.False);
        });

        //Single valid tag
        newFileContext.TagEdit.Tags = "simple test";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
        });

        //Blanks as nothing
        newFileContext.TagEdit.Tags = "    ";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues);
            Assert.That(newFileContext.TagEdit.HasChanges, Is.False);
        });

        //Null as nothing
        newFileContext.TagEdit.Tags = null;
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues);
            Assert.That(newFileContext.TagEdit.HasChanges, Is.False);
        });

        //Reset to good state
        newFileContext.TagEdit.Tags = "simple test";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
        });

        //Test invalid symbol is removed in processing
        newFileContext.TagEdit.Tags = "simple test; another ";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
            Assert.That(newFileContext.TagEdit.Tags, Is.EqualTo("simple test another "));
        });

        //Capitals removed
        newFileContext.TagEdit.Tags = "  SIMPLE TEST ";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
            Assert.That(newFileContext.TagEdit.Tags, Is.EqualTo("  simple test "));
        });

        //Hyphens Valid - 3 tags
        newFileContext.TagEdit.Tags = "test-one, test--two, test---three";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
            Assert.That(newFileContext.TagEdit.TagList().Count, Is.EqualTo(3));
        });

        //New Line not Valid
        newFileContext.TagEdit.Tags = "test-1, test--2, \r\n test---3";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
            Assert.That(newFileContext.TagEdit.Tags, Is.EqualTo("test-1, test--2,  test---3"));
        });

        //Hyphens Valid - 3 tags
        newFileContext.TagEdit.Tags = "test-1, test--2, test---3";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.TagEdit.HasValidationIssues, Is.False);
            Assert.That(newFileContext.TagEdit.HasChanges);
            Assert.That(newFileContext.TagEdit.TagList().Count, Is.EqualTo(3));


            //Created/Updated By

            Assert.That(newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue, Is.EqualTo("Trail Notes Ghost Writer"));
            Assert.That(newFileContext.CreatedUpdatedDisplay.HasChanges, Is.False);
            Assert.That(newFileContext.CreatedUpdatedDisplay.HasValidationIssues, Is.False);
        });

        newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue = "   ";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.CreatedUpdatedDisplay.HasChanges);
            Assert.That(newFileContext.CreatedUpdatedDisplay.HasValidationIssues);
        });

        newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue = "Trail Notes Ghost Writer 2";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.CreatedUpdatedDisplay.HasChanges);
            Assert.That(newFileContext.CreatedUpdatedDisplay.HasValidationIssues, Is.False);


            //Body Text
            Assert.That(newFileContext.BodyContent.HasChanges, Is.False);
        });
        newFileContext.BodyContent.UserValue =
            "UI Context Testing File with Bad Content Id {{postlink c3c63473-6b60-4531-97f7-2d201d84e2be; Cocopa and Yuma Points, Grand Canyon - 9/30-10/1/2020}}";
        Assert.Multiple(() =>
        {
            Assert.That(newFileContext.BodyContent.HasChanges);

            //Update Text
            Assert.That(newFileContext.UpdateNotes.HasChanges, Is.False);
        });
        newFileContext.UpdateNotes.UserValue =
            "UI Context Testing File with Bad Content Id {{postlink c3c63473-6b60-4531-97f7-2d201d84e2be; text Bad Content Id Text ;Cocopa and Yuma Points, Grand Canyon - 9/30-10/1/2020}}";
        Assert.That(newFileContext.UpdateNotes.HasChanges);

        var validationResult = await FileGenerator.Validate(newFileContext.CurrentStateToFileContent(),
            newFileContext.SelectedFile);
        Assert.That(validationResult.HasError);


        //Body Text
        newFileContext.BodyContent.UserValue = "UI Context Testing File";
        Assert.That(newFileContext.BodyContent.HasChanges);

        validationResult = await FileGenerator.Validate(newFileContext.CurrentStateToFileContent(),
            newFileContext.SelectedFile);
        Assert.That(validationResult.HasError);

        //Update Text
        newFileContext.UpdateNotes.UserValue = "UI Context Testing File Update";
        Assert.That(newFileContext.UpdateNotes.HasChanges);

        validationResult = await FileGenerator.Validate(newFileContext.CurrentStateToFileContent(),
            newFileContext.SelectedFile);
        Assert.That(validationResult.HasError, Is.False);

        var (generationReturn, _) = await FileGenerator.SaveAndGenerateHtml(
            newFileContext.CurrentStateToFileContent(), newFileContext.SelectedFile, null,
            DebugTrackers.DebugProgressTracker());

        Assert.That(generationReturn.HasError, Is.False);

        var db = await Db.Context();

        Assert.That(db.FileContents.Count(), Is.EqualTo(1));

        var dbContent = await db.FileContents.FirstOrDefaultAsync();

        Assert.Multiple(() =>
        {
            Assert.That(dbContent.OriginalFileName == newFileContext.SelectedFile.Name);
            Assert.That(dbContent.Title == newFileContext.TitleSummarySlugFolder.TitleEntry.UserValue);
            Assert.That(dbContent.Slug == newFileContext.TitleSummarySlugFolder.SlugEntry.UserValue);
            Assert.That(dbContent.Folder == newFileContext.TitleSummarySlugFolder.FolderEntry.UserValue);
            Assert.That(dbContent.Summary == newFileContext.TitleSummarySlugFolder.SummaryEntry.UserValue);
            Assert.That(dbContent.Tags == newFileContext.TagEdit.TagListString());
            Assert.That(dbContent.CreatedBy == newFileContext.CreatedUpdatedDisplay.CreatedByEntry.UserValue);
            Assert.That(dbContent.BodyContent == newFileContext.BodyContent.UserValue);
            Assert.That(dbContent.UpdateNotes == newFileContext.UpdateNotes.UserValue);
        });
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