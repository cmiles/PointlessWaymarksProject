﻿using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using HtmlTableHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsWpfControls.AllContentList;
using PointlessWaymarks.CmsWpfControls.Diagnostics;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
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
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CmsWpfControls.TagExclusionEditor;
using PointlessWaymarks.CmsWpfControls.TagList;
using PointlessWaymarks.CmsWpfControls.UserSettingsEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsContentEditor;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow
{
    [ObservableProperty] private FilesWrittenLogListContext _filesWrittenContext;
    [ObservableProperty] private string _infoTitle;
    [ObservableProperty] private string _recentSettingsFilesNames;
    [ObservableProperty] private TabItem _selectedTab;
    [ObservableProperty] private UserSettingsEditorContext _settingsEditorContext;
    [ObservableProperty] private SettingsFileChooserControlContext _settingsFileChooser;
    [ObservableProperty] private bool _showSettingsFileChooser;
    [ObservableProperty] private HelpDisplayContext _softwareComponentsHelpContext;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private AllItemsWithActionsContext _tabAllListContext;
    [ObservableProperty] private FileListWithActionsContext _tabFileListContext;
    [ObservableProperty] private GeoJsonListWithActionsContext _tabGeoJsonListContext;
    [ObservableProperty] private ImageListWithActionsContext _tabImageListContext;
    [ObservableProperty] private LineListWithActionsContext _tabLineListContext;
    [ObservableProperty] private LinkListWithActionsContext _tabLinkContext;
    [ObservableProperty] private MapComponentListWithActionsContext _tabMapListContext;
    [ObservableProperty] private MenuLinkEditorContext _tabMenuLinkContext;
    [ObservableProperty] private NoteListWithActionsContext _tabNoteListContext;
    [ObservableProperty] private PhotoListWithActionsContext _tabPhotoListContext;
    [ObservableProperty] private PointListWithActionsContext _tabPointListContext;
    [ObservableProperty] private PostListWithActionsContext _tabPostListContext;
    [ObservableProperty] private TagExclusionEditorContext _tabTagExclusionContext;
    [ObservableProperty] private TagListContext _tabTagListContext;

    public MainWindow()
    {
        InitializeComponent();

        JotServices.Tracker.Configure<MainWindow>().Properties(x => new { x.RecentSettingsFilesNames });

        JotServices.Tracker.Track(this);

        if (Width < 900) Width = 900;
        if (Height < 650) Height = 650;

        WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse for generated ThisAssembly.Git.IsDirty
#pragma warning disable CS0162
        // ReSharper disable once HeuristicUnreachableCode
        //.Git IsDirty can change at runtime
        InfoTitle =
            $"Pointless Waymarks CMS - Built On {GetBuildDate(Assembly.GetEntryAssembly())} - Commit {ThisAssembly.Git.Commit} {(ThisAssembly.Git.IsDirty ? "(Has Local Changes)" : string.Empty)}";
#pragma warning restore CS0162

        ShowSettingsFileChooser = true;

        DataContext = this;

        StatusContext = new StatusControlContext();

        WindowStatus = new WindowIconStatus();

        PropertyChanged += OnPropertyChanged;

        //Common

        GenerateChangedHtmlAndShowPreviewCommand = StatusContext.RunBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            await HtmlGenerationGroups.GenerateChangedToHtml(StatusContext.ProgressTracker());

            await ThreadSwitcher.ResumeForegroundAsync();

            var sitePreviewWindow = new SiteOnDiskPreviewWindow();
            sitePreviewWindow.Show();
        });

        GenerateChangedHtmlAndStartUploadCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await S3UploadHelpers.GenerateChangedHtmlAndStartUpload(StatusContext));

        GenerateChangedHtmlCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await GenerationHelpers.GenerateChangedHtml(StatusContext.ProgressTracker()));

        RemoveUnusedFilesFromMediaArchiveCommand =
            StatusContext.RunBlockingTaskCommand(RemoveUnusedFilesFromMediaArchive);

        RemoveUnusedFoldersAndFilesFromContentCommand =
            StatusContext.RunBlockingTaskCommand(RemoveUnusedFoldersAndFilesFromContent);

        GenerateIndexCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateIndex(null, StatusContext.ProgressTracker()));

        CheckAllContentForInvalidBracketCodeContentIdsCommand =
            StatusContext.RunBlockingTaskCommand(CheckAllContentForInvalidBracketCodeContentIds);

        //All/Forced Regeneration
        GenerateAllHtmlCommand = StatusContext.RunBlockingTaskCommand(GenerateAllHtml);

        ConfirmOrGenerateAllPhotosImagesFilesCommand =
            StatusContext.RunBlockingTaskCommand(ConfirmOrGenerateAllPhotosImagesFiles);

        DeleteAndResizePicturesCommand = StatusContext.RunBlockingTaskCommand(CleanAndResizePictures);

        //Main Parts
        GenerateSiteResourcesCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await FileManagement.WriteSiteResourcesToGeneratedSite(StatusContext.ProgressTracker()));

        WriteStyleCssFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await FileManagement.WriteStylesCssToGeneratedSite(StatusContext.ProgressTracker()));

        GenerateHtmlForAllFileContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllFileHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllImageContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllImageHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllMapContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllMapData(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllNoteContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllNoteHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllPhotoContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllPhotoHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllPostContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllPostHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllPointContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllPointHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllLineContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllLineHtml(null, StatusContext.ProgressTracker()));

        GenerateHtmlForAllGeoJsonContentCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllGeoJsonHtml(null, StatusContext.ProgressTracker()));

        //Derived
        GenerateAllListHtmlCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllListHtml(null, StatusContext.ProgressTracker()));

        GenerateAllTagHtmlCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllTagHtml(null, StatusContext.ProgressTracker()));

        GenerateCameraRollCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateCameraRollHtml(null, StatusContext.ProgressTracker()));

        GenerateDailyGalleryHtmlCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await HtmlGenerationGroups.GenerateAllDailyPhotoGalleriesHtml(null, StatusContext.ProgressTracker()));

        //Rebuild
        ImportJsonFromDirectoryCommand = StatusContext.RunBlockingTaskCommand(ImportJsonFromDirectory);

        SettingsFileChooser = new SettingsFileChooserControlContext(StatusContext, RecentSettingsFilesNames);

        SettingsFileChooser.SettingsFileUpdated += SettingsFileChooserOnSettingsFileUpdatedEvent;

        StatusContext.RunFireAndForgetNonBlockingTask(CleanupTemporaryFiles);
    }

    public RelayCommand CheckAllContentForInvalidBracketCodeContentIdsCommand { get; set; }

    public RelayCommand ConfirmOrGenerateAllPhotosImagesFilesCommand { get; set; }

    public RelayCommand DeleteAndResizePicturesCommand { get; set; }

    public RelayCommand GenerateAllHtmlCommand { get; set; }

    public RelayCommand GenerateAllListHtmlCommand { get; set; }

    public RelayCommand GenerateAllTagHtmlCommand { get; set; }

    public RelayCommand GenerateCameraRollCommand { get; set; }

    public RelayCommand GenerateChangedHtmlAndShowPreviewCommand { get; set; }

    public RelayCommand GenerateChangedHtmlAndStartUploadCommand { get; set; }

    public RelayCommand GenerateChangedHtmlCommand { get; set; }

    public RelayCommand GenerateDailyGalleryHtmlCommand { get; set; }

    public RelayCommand GenerateHtmlForAllFileContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllGeoJsonContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllImageContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllLineContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllMapContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllNoteContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllPhotoContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllPointContentCommand { get; set; }

    public RelayCommand GenerateHtmlForAllPostContentCommand { get; set; }

    public RelayCommand GenerateIndexCommand { get; set; }

    public RelayCommand GenerateSiteResourcesCommand { get; set; }

    public RelayCommand ImportJsonFromDirectoryCommand { get; set; }

    public RelayCommand RemoveUnusedFilesFromMediaArchiveCommand { get; set; }

    public RelayCommand RemoveUnusedFoldersAndFilesFromContentCommand { get; set; }

    public WindowIconStatus WindowStatus { get; set; }

    public RelayCommand WriteStyleCssFileCommand { get; set; }

    private async Task CheckAllContentForInvalidBracketCodeContentIds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var generationResults =
            await CommonContentValidation.CheckAllContentForBadContentReferences(StatusContext.ProgressTracker());

        if (generationResults.All(x => !x.HasError))
        {
            StatusContext.ToastSuccess("No problems with Bracket Code Content Ids Found!");
            return;
        }

        await Reports.InvalidBracketCodeContentIdsHtmlReport(generationResults);
    }

    private async Task CleanAndResizeAllImageFiles()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var results = await FileManagement.CleanAndResizeAllImageFiles(StatusContext.ProgressTracker());

        if (results.Any())
        {
            var frozenNow = DateTime.Now;

            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"CleanAndResizeAllImageFiles-ErrorReport-{frozenNow:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                ($"<h1>Clean and Resize All Image Files Error Report - {frozenNow:yyyy-MM-dd---HH-mm-ss}</h1><br>" +
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

            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"CleanAndResizeAllPhotoFiles-ErrorReport-{frozenNow:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                ($"<h1>Clean and Resize All Photo Files Error Report - {frozenNow:yyyy-MM-dd---HH-mm-ss}</h1><br>" +
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

            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"ConfirmFileContent-ErrorReport-{frozenNow:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                ($"<h1>Confirm All File Content Files Error Report - {frozenNow:yyyy-MM-dd---HH-mm-ss}</h1><br>" +
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

    private async Task ConfirmOrGenerateAllPhotosImagesFiles()
    {
        await ConfirmAllPhotoContent();
        await ConfirmAllImageContent();
        await ConfirmAllFileContent();

        StatusContext.ToastSuccess("All HTML Generation Finished");
    }

    private async Task GenerateAllHtml()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        var generationResults = await HtmlGenerationGroups.GenerateAllHtml(StatusContext.ProgressTracker());

        if (generationResults.All(x => !x.HasError)) return;

        await Reports.InvalidBracketCodeContentIdsHtmlReport(generationResults);
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime;
    }

    private async Task ImportJsonFromDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting JSON load.");

        var dialog = new VistaFolderBrowserDialog();

        if (!(dialog.ShowDialog() ?? false)) return;

        var newDirectory = new DirectoryInfo(dialog.SelectedPath);

        if (!newDirectory.Exists)
        {
            StatusContext.ToastError("Directory doesn't exist?");
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

        LogHelpers.InitializeStaticLoggerAsEventLogger();

        StatusContext.Progress("Setting up UI Controls");

        TabAllListContext = new AllItemsWithActionsContext(null, WindowStatus);

        SettingsEditorContext = new UserSettingsEditorContext(null, UserSettingsSingleton.CurrentSettings());
        SoftwareComponentsHelpContext = new HelpDisplayContext(new List<string> { SoftwareUsedHelpMarkdown.HelpBlock });

        await ThreadSwitcher.ResumeForegroundAsync();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse for generated ThisAssembly.Git.IsDirty
#pragma warning disable CS0162
        // ReSharper disable once HeuristicUnreachableCode
        //.Git IsDirty can change at runtime
        InfoTitle =
            $"{UserSettingsSingleton.CurrentSettings().SiteName} - Pointless Waymarks CMS - Built On {GetBuildDate(Assembly.GetEntryAssembly())} - Commit {ThisAssembly.Git.Commit} {(ThisAssembly.Git.IsDirty ? "(Has Local Changes)" : string.Empty)}";
#pragma warning restore CS0162

        MainTabControl.SelectedIndex = 0;
    }

    private void LoadSelectedTabAsNeeded()
    {
        if (SelectedTab == null) return;

        if (SelectedTab.Header.ToString() == "Posts" && TabPostListContext == null)
            TabPostListContext = new PostListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Photos" && TabPhotoListContext == null)
            TabPhotoListContext = new PhotoListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Images" && TabImageListContext == null)
            TabImageListContext = new ImageListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Files" && TabFileListContext == null)
            TabFileListContext = new FileListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Points" && TabPointListContext == null)
            TabPointListContext = new PointListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Lines" && TabLineListContext == null)
            TabLineListContext = new LineListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "GeoJson" && TabGeoJsonListContext == null)
            TabGeoJsonListContext = new GeoJsonListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Maps" && TabMapListContext == null)
            TabMapListContext = new MapComponentListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Notes" && TabNoteListContext == null)
            TabNoteListContext = new NoteListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Links" && TabLinkContext == null)
            TabLinkContext = new LinkListWithActionsContext(null, WindowStatus);
        if (SelectedTab.Header.ToString() == "Tag Search Exclusions" && TabTagExclusionContext == null)
            TabTagExclusionContext = new TagExclusionEditorContext(null);
        if (SelectedTab.Header.ToString() == "Menu Links" && TabMenuLinkContext == null)
            TabMenuLinkContext = new MenuLinkEditorContext(null);
        if (SelectedTab.Header.ToString() == "Tags" && TabTagListContext == null)
            TabTagListContext = new TagListContext(null);
        if (SelectedTab.Header.ToString() == "File Log" && FilesWrittenContext == null)
            FilesWrittenContext = new FilesWrittenLogListContext(null, true);
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
            LoadSelectedTabAsNeeded();
    }

    private async Task RemoveUnusedFilesFromMediaArchive()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        await FileManagement.RemoveMediaArchiveFilesNotInDatabase(StatusContext.ProgressTracker());
    }

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
            StatusContext.ToastError("Error with Settings File? No name?");
            return;
        }

        if (settingReturn.isNew)
            await UserSettingsUtilities.SetupNewSite(settingReturn.userInput, StatusContext.ProgressTracker());
        else
            UserSettingsUtilities.SettingsFileFullName = settingReturn.userInput;

        StatusContext.Progress($"Using {UserSettingsUtilities.SettingsFileFullName}");

        var fileList = settingReturn.fileList ?? new List<string>();

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
}