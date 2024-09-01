using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HtmlTableHelper;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineMonthlyActivitySummaryHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Diagnostics;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.MenuLinkEditor;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.S3Uploads;
using PointlessWaymarks.CmsWpfControls.Server;
using PointlessWaymarks.CmsWpfControls.TagExclusionEditor;
using PointlessWaymarks.CmsWpfControls.TagList;
using PointlessWaymarks.CmsWpfControls.UserSettingsEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.ProgramUpdateMessage;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PointlessWaymarks.CmsGui;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class MainWindow
{
    private readonly string _currentDateVersion;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Configure<MainWindow>().Properties(x => new { x.RecentSettingsFilesNames });

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        var versionInfo =
            ProgramInfoTools.StandardAppInformationString(AppContext.BaseDirectory,
                "Pointless Waymarks CMS Beta");

        InfoTitle = versionInfo.humanTitleString;

        _currentDateVersion = versionInfo.dateVersion;

        ShowSettingsFileChooser = true;

        DataContext = this;

        StatusContext = new StatusControlContext();

        WindowStatus = new WindowIconStatus();

        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        PropertyChanged += OnPropertyChanged;

        UpdateMessageContext = new ProgramUpdateMessageContext();

        BuildCommands();

        StatusContext.RunFireAndForgetNonBlockingTask(CleanupTemporaryFiles);

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            SettingsFileChooser =
                await SettingsFileChooserControlContext.CreateInstance(StatusContext, RecentSettingsFilesNames);

            SettingsFileChooser.SettingsFileUpdated += SettingsFileChooserOnSettingsFileUpdatedEvent;
        });
    }

    public HelpDisplayContext AboutContext { get; set; }
    public CmsCommonCommands CommonCommands { get; set; }
    public FilesWrittenLogListContext FilesWrittenContext { get; set; }
    public string InfoTitle { get; set; }
    public string RecentSettingsFilesNames { get; set; }
    public TabItem SelectedTab { get; set; }
    public UserSettingsEditorContext SettingsEditorContext { get; set; }
    public SettingsFileChooserControlContext SettingsFileChooser { get; set; }
    public bool ShowSettingsFileChooser { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public AllContentListWithActionsContext TabAllListContext { get; set; }
    public FileListWithActionsContext TabFileListContext { get; set; }
    public GeoJsonListWithActionsContext TabGeoJsonListContext { get; set; }
    public ImageListWithActionsContext TabImageListContext { get; set; }
    public LineListWithActionsContext TabLineListContext { get; set; }
    public LinkListWithActionsContext TabLinkContext { get; set; }
    public MapComponentListWithActionsContext TabMapListContext { get; set; }
    public MenuLinkEditorContext TabMenuLinkContext { get; set; }
    public NoteListWithActionsContext TabNoteListContext { get; set; }
    public PhotoListWithActionsContext TabPhotoListContext { get; set; }
    public PointListWithActionsContext TabPointListContext { get; set; }
    public PostListWithActionsContext TabPostListContext { get; set; }
    public TagExclusionEditorContext TabTagExclusionContext { get; set; }
    public TagListContext TabTagListContext { get; set; }
    public VideoListWithActionsContext TabVideoListContext { get; set; }
    public ProgramUpdateMessageContext UpdateMessageContext { get; set; }
    public WindowIconStatus WindowStatus { get; }

    [BlockingCommand]
    private async Task CheckAllContentForInvalidBracketCodeContentIds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var generationResults =
            await CommonContentValidation.CheckAllContentForBadContentReferences(StatusContext.ProgressTracker());

        if (generationResults.All(x => !x.HasError))
        {
            await StatusContext.ToastSuccess("No problems with Bracket Code Content Ids Found!");
            return;
        }

        await Reports.InvalidBracketCodeContentIdsHtmlReport(generationResults);
    }

    public async Task CheckForProgramUpdate(string currentDateVersion)
    {
        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {UserSettingsSingleton.CurrentSettings().ProgramUpdateLocation}");

        if (string.IsNullOrEmpty(currentDateVersion)) return;

        var (dateString, setupFile) = ProgramInfoTools.LatestInstaller(
            UserSettingsSingleton.CurrentSettings().ProgramUpdateLocation,
            "PointlessWaymarksCmsSetup");

        Log.Information(
            $"Program Update Check - Current Version {currentDateVersion}, Installer Directory {UserSettingsSingleton.CurrentSettings().ProgramUpdateLocation}, Installer Date Found {dateString ?? string.Empty}, Setup File Found {setupFile?.FullName ?? string.Empty}");

        if (string.IsNullOrWhiteSpace(dateString) || setupFile is not { Exists: true }) return;

        if (string.Compare(currentDateVersion, dateString, StringComparison.OrdinalIgnoreCase) >= 0) return;

        await UpdateMessageContext.LoadData(currentDateVersion, dateString, setupFile);
    }

    private async Task CleanAndResizeAllImageFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var results = await FileManagement.CleanAndResizeAllImageFiles(StatusContext.ProgressTracker());

        if (results.Any())
        {
            var frozenNow = DateTime.Now;

            var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
                $"CleanAndResizeAllImageFiles-ErrorReport-{frozenNow:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                await (
                        $"<h1>Clean and Resize All Image Files Error Report - {frozenNow:yyyy-MM-dd---HH-mm-ss}</h1><br>" +
                        results.ToHtmlTable(new { @class = "pure-table pure-table-striped" }))
                    .ToHtmlDocumentWithPureCss("Clean and Resize Images Error Report", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };

            Process.Start(ps);

            await StatusContext.ShowMessageWithOkButton("Image Clean and Resize All Errors",
                $"There were {results.Count} errors while Cleaning and Resizing All Image Files - " +
                $"an error file - {file.FullName} - has been generated and should open automatically in " +
                "your browser.");
        }
    }


    private async Task CleanAndResizeAllPhotoFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var results = await FileManagement.CleanAndResizeAllPhotoFiles(StatusContext.ProgressTracker());

        if (results.Any())
        {
            var frozenNow = DateTime.Now;

            var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
                $"CleanAndResizeAllPhotoFiles-ErrorReport-{frozenNow:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                await (
                        $"<h1>Clean and Resize All Photo Files Error Report - {frozenNow:yyyy-MM-dd---HH-mm-ss}</h1><br>" +
                        results.ToHtmlTable(new { @class = "pure-table pure-table-striped" }))
                    .ToHtmlDocumentWithPureCss("Clean and Resize Photos Error Report", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };

            Process.Start(ps);

            await StatusContext.ShowMessageWithOkButton("Photo Clean and Resize All Errors",
                $"There were {results.Count} errors while Cleaning and Resizing All Photo Files - " +
                $"an error file - {file.FullName} - has been generated and should open automatically in " +
                "your browser.");
        }
    }

    [BlockingCommand]
    private async Task CleanAndResizePictures()
    {
        await CleanAndResizeAllPhotoFiles();
        await CleanAndResizeAllImageFiles();
    }

    private async Task CleanupTemporaryFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        FileManagement.CleanUpTemporaryFiles();
        FileManagement.CleanupTemporaryHtmlFiles();
    }

    private async Task ConfirmAllFileContent()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var results = await FileManagement.ConfirmAllFileContentFilesArePresent(StatusContext.ProgressTracker());

        if (results.Any())
        {
            var frozenNow = DateTime.Now;

            var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
                $"ConfirmFileContent-ErrorReport-{frozenNow:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                await ($"<h1>Confirm All File Content Files Error Report - {frozenNow:yyyy-MM-dd---HH-mm-ss}</h1><br>" +
                       results.ToHtmlTable(new { @class = "pure-table pure-table-striped" }))
                    .ToHtmlDocumentWithPureCss("Confirm Files Error Report", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };

            Process.Start(ps);

            await StatusContext.ShowMessageWithOkButton("File Content Files Check Errors",
                $"There were {results.Count} errors while Confirming all File Content Files - " +
                $"an error file - {file.FullName} - has been generated and should open automatically in " +
                "your browser.");
        }
    }

    private async Task ConfirmAllImageContent()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        var allItems = await db.ImageContents.ToListAsync();

        var loopCount = 1;
        var totalCount = allItems.Count;

        StatusContext.Progress($"Found {totalCount} Image to Confirm");

        foreach (var loopItem in allItems)
        {
            StatusContext.Progress($"Confirming Image Content for {loopItem.Title} - {loopCount} of {totalCount}");

            await PictureAssetProcessing.ConfirmOrGenerateImageDirectoryAndPictures(loopItem);

            loopCount++;
        }
    }

    private async Task ConfirmAllPhotoContent()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        var allItems = await db.PhotoContents.ToListAsync();

        var loopCount = 1;
        var totalCount = allItems.Count;

        StatusContext.Progress($"Found {totalCount} Photos to Confirm");

        foreach (var loopItem in allItems)
        {
            StatusContext.Progress($"Confirming Photos for {loopItem.Title} - {loopCount} of {totalCount}");

            await PictureAssetProcessing.ConfirmOrGeneratePhotoDirectoryAndPictures(loopItem);

            loopCount++;
        }
    }

    [BlockingCommand]
    private async Task ConfirmOrGenerateAllPhotosImagesFiles()
    {
        await ConfirmAllPhotoContent();
        await ConfirmAllImageContent();
        await ConfirmAllFileContent();

        await StatusContext.ToastSuccess("All HTML Generation Finished");
    }

    [BlockingCommand]
    private async Task GenerateAllHtml()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var generationResults = await SiteGeneration.AllSiteContent(StatusContext.ProgressTracker());

        if (generationResults.All(x => !x.HasError)) return;

        await Reports.InvalidBracketCodeContentIdsHtmlReport(generationResults);
    }

    [BlockingCommand]
    public async Task GenerateAllListHtml()
    {
        await SiteGenerationAllContent.GenerateAllListHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateAllTagHtml()
    {
        await SiteGenerationAllContent.GenerateAllTagHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateCameraRoll()
    {
        await SiteGeneration.GenerateCameraRollHtml(null, StatusContext.ProgressTracker());
    }


    [BlockingCommand]
    public async Task GenerateChangedHtml()
    {
        await GenerationHelpers.GenerateChangedHtml(StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateChangedHtmlAndStartUpload()
    {
        await S3UploadHelpers.GenerateChangedHtmlAndStartUpload(StatusContext);
    }

    [BlockingCommand]
    public async Task GenerateDailyGalleryHtml()
    {
        await SiteGenerationAllContent.GenerateAllDailyPhotoGalleriesHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllFileContent()
    {
        await SiteGenerationAllContent.GenerateAllFileHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllGeoJsonContent()
    {
        await SiteGenerationAllContent.GenerateAllGeoJsonHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllImageContent()
    {
        await SiteGenerationAllContent.GenerateAllImageHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllLineContent()
    {
        await SiteGenerationAllContent.GenerateAllLineHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllMapContent()
    {
        await SiteGenerationAllContent.GenerateAllMapData(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllNoteContent()
    {
        await SiteGenerationAllContent.GenerateAllNoteHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllPhotoContent()
    {
        await SiteGenerationAllContent.GenerateAllPhotoHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllPointContent()
    {
        await SiteGenerationAllContent.GenerateAllPointHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllPostContent()
    {
        await SiteGenerationAllContent.GenerateAllPostHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtmlForAllVideoContent()
    {
        await SiteGenerationAllContent.GenerateAllVideoHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateIndex()
    {
        await SiteGeneration.GenerateIndex(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateLatestContentGallery()
    {
        await SiteGeneration.GenerateLatestContentGalleryHtml(null, StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateMonthlyActivitySummaryHtml()
    {
        await new LineMonthlyActivitySummaryPage(null).WriteLocalHtml();
    }


    [BlockingCommand]
    public async Task GenerateSiteResources()
    {
        await FileManagement.WriteSiteResourcesToGeneratedSite(StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    private async Task ImportJsonFromDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting JSON load.");

        var dialog = new VistaFolderBrowserDialog();

        if (!(dialog.ShowDialog() ?? false)) return;

        var newDirectory = new DirectoryInfo(dialog.SelectedPath);

        if (!newDirectory.Exists)
        {
            await StatusContext.ToastError("Directory doesn't exist?");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        Import.FullImportFromRootDirectory(newDirectory, StatusContext.ProgressTracker());

        StatusContext.Progress("JSON Import Finished");
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ShowSettingsFileChooser = false;

        var settings = await UserSettingsUtilities.ReadFromCurrentSettingsFile(StatusContext.ProgressTracker());
        settings.VerifyOrCreateAllTopLevelFolders();

        await UserSettingsUtilities.EnsureDbIsPresent(StatusContext.ProgressTracker());

        PointlessWaymarksLogTools.InitializeStaticLoggerAsEventLogger();
        Log.Information(
            $"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");

        StatusContext.RunFireAndForgetWithToastOnError(async () =>
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            await PartialContentPreviewServer.PartialContentServer();
        });

        StatusContext.Progress("Setting up UI Controls");

        TabAllListContext = await AllContentListWithActionsContext.CreateInstance(null, WindowStatus);

        SettingsEditorContext =
            await UserSettingsEditorContext.CreateInstance(null, UserSettingsSingleton.CurrentSettings());
        AboutContext = new HelpDisplayContext([HelpMarkdown.CombinedAboutToolsAndPackages]);

        await ThreadSwitcher.ResumeForegroundAsync();

        InfoTitle =
            $"{UserSettingsSingleton.CurrentSettings().SiteName} - {InfoTitle}";
        MainTabControl.SelectedIndex = 0;

        StatusContext.RunBlockingTask(async () => await CheckForProgramUpdate(_currentDateVersion));
    }

    private async Task LoadSelectedTabAsNeeded()
    {
        if (SelectedTab == null) return;

        if (SelectedTab.Header.ToString() == "Posts" && TabPostListContext == null)
            TabPostListContext = await PostListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Photos" && TabPhotoListContext == null)
            TabPhotoListContext = await PhotoListWithActionsContext.CreateInstance(null, WindowStatus, null);
        if (SelectedTab.Header.ToString() == "Images" && TabImageListContext == null)
            TabImageListContext = await ImageListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Videos" && TabVideoListContext == null)
            TabVideoListContext = await VideoListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Files" && TabFileListContext == null)
            TabFileListContext = await FileListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Points" && TabPointListContext == null)
            TabPointListContext = await PointListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Lines" && TabLineListContext == null)
            TabLineListContext = await LineListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "GeoJson" && TabGeoJsonListContext == null)
            TabGeoJsonListContext = await GeoJsonListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Maps" && TabMapListContext == null)
            TabMapListContext = await MapComponentListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Notes" && TabNoteListContext == null)
            TabNoteListContext = await NoteListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Links" && TabLinkContext == null)
            TabLinkContext = await LinkListWithActionsContext.CreateInstance(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Tag Search Exclusions" && TabTagExclusionContext == null)
            TabTagExclusionContext = await TagExclusionEditorContext.CreateInstance(null);
        if (SelectedTab.Header.ToString() == "Menu Links" && TabMenuLinkContext == null)
            TabMenuLinkContext = await MenuLinkEditorContext.CreateInstance(null);
        if (SelectedTab.Header.ToString() == "Tags" && TabTagListContext == null)
            TabTagListContext = await TagListContext.CreateInstance(null);
        if (SelectedTab.Header.ToString() == "File Log" && FilesWrittenContext == null)
            FilesWrittenContext = await FilesWrittenLogListContext.CreateInstance(null, true);
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
        if (Application.Current.Windows.Count > 1)
        {
            //The Visual Studio in window WPF debug tool appear to the application as an
            //AdornerWindow - this exception allows closing as expecting when debugging.
            if (Application.Current.Windows is { Count: 2 } && Application.Current.Windows[1] != null &&
                Application.Current.Windows[1].GetType().Name.Contains("AdornerWindow")) return;

            StatusContext.ToastError("Please close child windows first...");
            e.Cancel = true;
        }
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(SelectedTab))
#pragma warning disable CS4014
            LoadSelectedTabAsNeeded();
#pragma warning restore CS4014
    }

    [BlockingCommand]
    private async Task RemoveUnusedFilesFromMediaArchive()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        await FileManagement.RemoveMediaArchiveFilesNotInDatabase(StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    private async Task RemoveUnusedFoldersAndFilesFromContent()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        await FileManagement.RemoveContentDirectoriesAndFilesNotFoundInCurrentDatabase(StatusContext.ProgressTracker());
    }

    private async Task SettingsFileChooserOnSettingsFileUpdated(
        (bool isNew, string userInput, List<string> fileList) settingReturn)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(settingReturn.userInput))
        {
            await StatusContext.ToastError("Error with Settings File? No name?");
            return;
        }

        if (settingReturn.isNew)
            await UserSettingsUtilities.SetupNewSite(settingReturn.userInput, StatusContext.ProgressTracker());
        else
            UserSettingsUtilities.SettingsFileFullName = settingReturn.userInput;

        StatusContext.Progress($"Using {UserSettingsUtilities.SettingsFileFullName}");

        var fileList = settingReturn.fileList ?? [];

        if (fileList.Contains(UserSettingsUtilities.SettingsFileFullName))
            fileList.Remove(UserSettingsUtilities.SettingsFileFullName);

        fileList = new List<string> { UserSettingsUtilities.SettingsFileFullName }.Concat(fileList).ToList();

        if (fileList.Count > 10)
            fileList = fileList.Take(10).ToList();

        RecentSettingsFilesNames = string.Join("|", fileList);

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private void SettingsFileChooserOnSettingsFileUpdatedEvent(object sender,
        (bool isNew, string userString, List<string> recentFiles) e)
    {
        StatusContext.RunFireAndForgetBlockingTask(async () => await SettingsFileChooserOnSettingsFileUpdated(e));
    }

    [BlockingCommand]
    public async Task WriteStyleCssFile()
    {
        await FileManagement.WriteStylesCssToGeneratedSite(StatusContext.ProgressTracker());
    }
}