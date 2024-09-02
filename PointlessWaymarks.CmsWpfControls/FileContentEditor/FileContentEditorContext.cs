using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.FileContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
[StaThreadConstructorGuard]
public partial class FileContentEditorContext : IHasChangesExtended, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;

    private FileContentEditorContext(StatusControlContext? statusContext, FileContent dbEntry)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        BuildCommands();

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public BodyContentEditorContext? BodyContent { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public FileContent DbEntry { get; set; }
    public BoolDataEntryContext? EmbedFile { get; set; }

    public string FileEditorHelpText =>
        @"
### File Content

Interesting books, dissertations, academic papers, maps, meeting notes, articles, memos, reports, etc. are available on a wide variety of subjects - but over years, decades, of time resources can easily 'disappear' from the internet... Websites are no longer available, agencies delete documents they are no longer legally required to retain, older versions of a document are not kept when a newer version comes out, departments shut down, funding runs out...

File Content is intended to allow the creation of a 'library' of Files that you can tag, search, share and retain. The File you choose for File Content will be copied to the site just like an image or photo would be.

With any file you have on your site it is your responsibility to know if it is legally acceptable to have the file on the site - like any content in this CMS you should only enter it into the CMS if you want it 'publicly' available on your site, there are options that allow some content to be more discrete - but NO options that allow you to fully hide content.

Notes:
 - No File Previews are automatically generated - you will need to add any images/previews/etc. manually to the Body Content
 - To help when working with PDFs the program can extract pages of a PDF as Image Content for quick/easy use in the Body Content - details:
   - To use this functionality pdftocairo must be available on your computer and the location of pdftocairo must be set in the Settings
   - On windows the easiest way to install pdftocairo is to install MiKTeX - [Getting MiKTeX - MiKTeX.org](https://miktex.org/download)
   - The page you specify to generate an image is the page that the PDF Viewer you are using is showing (rather than the 'content page number' printed at the bottom of a page) - for example with a book in PDF format to get an image of the 'cover' the page number is '1'
 - The File Content page can contain a link to download the file - but it is not appropriate to offer all content for download, use the 'Show Public Download Link' to turn on/off the download link. This setting will impact the behaviour of the 'filedownloadlink' bracket code - if 'Show Public Download Link' is unchecked a filedownloadlink bracket code will become a link to the File Content Page (rather than a download link for the content).
 - Regardless of the 'Show Public Download Link' the file will be copied to the site - if you have a sensitive document that should not be copied beyond your computer consider just creating Post Content for it - the File Content type is only useful for content where you want the File to be 'with' the site.
 - If appropriate consider including links to the original source in the Body Content
 - If what you are writing about is a 'file' but you don't want/need to store the file itself on your site you should probably just create a Post (or other content type like and Image) - use File Content when you want to store the file. 
";

    public bool FileIsMp4 { get; set; }
    public bool FileIsPdf { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public FileInfo? InitialFile { get; set; }
    public FileInfo? LoadedFile { get; set; }
    public ImageContentEditorWindow? MainImageExternalEditorWindow { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public OptionalLocationEntryContext? OptionalLocationEntry { get; set; }
    public string PdfToImagePageToExtract { get; set; } = "1";
    public BoolDataEntryContext? PublicDownloadLink { get; set; }
    public FileInfo? SelectedFile { get; set; }
    public bool SelectedFileHasPathOrNameChanges { get; set; }
    public bool SelectedFileHasValidationIssues { get; set; }
    public bool SelectedFileNameHasInvalidCharacters { get; set; }
    public string? SelectedFileValidationMessage { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    public ConversionDataEntryContext<Guid?>? UserMainPictureEntry { get; set; }
    public IContentCommon? UserMainPictureEntryContent { get; set; }
    public string? UserMainPictureEntrySmallImageUrl { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        var childProperties = PropertyScanners.ChildPropertiesHaveChangesWithChangedList(this);

        HasChangesChangedList = childProperties.changeProperties;
        HasChangesChangedList.Add((SelectedFileHasPathOrNameChanges, nameof(SelectedFileHasPathOrNameChanges)));

        var mainPictureChanges = DbEntry.MainPicture != CurrentMainPicture();
        HasChangesChangedList.Add((mainPictureChanges, nameof(mainPictureChanges)));

        HasChanges = childProperties.hasChanges || SelectedFileHasPathOrNameChanges ||
                     mainPictureChanges;

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              SelectedFileHasValidationIssues;
    }

    public bool HasChanges { get; set; }
    public List<(bool hasChanges, string description)> HasChangesChangedList { get; set; } = [];
    public bool HasValidationIssues { get; set; }

    [BlockingCommand]
    private async Task AddFeatureIntersectTags()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var possibleTags = await OptionalLocationEntry!.GetFeatureIntersectTagsWithUiAlerts();

        if (possibleTags.Any())
            TagEdit!.Tags =
                $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }

    [BlockingCommand]
    public async Task AutoRenameSelectedFile()
    {
        await FileHelpers.TryAutoCleanRenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x);
    }


    [BlockingCommand]
    public async Task AutoRenameSelectedFileBasedOnTitle()
    {
        await FileHelpers.TryAutoRenameSelectedFile(SelectedFile, TitleSummarySlugFolder!.TitleEntry.UserValue,
            StatusContext, x => SelectedFile = x);
    }

    [BlockingCommand]
    public async Task ChooseFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting image load.");

        var dialog = new VistaOpenFileDialog();

        if (!(dialog.ShowDialog() ?? false)) return;

        var newFile = new FileInfo(dialog.FileName);

        if (!newFile.Exists)
        {
            await StatusContext.ToastError("File doesn't exist?");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedFile = newFile;

        StatusContext.Progress($"File load - {SelectedFile.FullName} ");
    }

    public static async Task<FileContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        FileInfo? initialFile = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new FileContentEditorContext(statusContext, FileContent.CreateInstance())
            { StatusContext = { BlockUi = true } };

        if (initialFile is { Exists: true }) newContext.InitialFile = initialFile;
        await newContext.LoadData(null);

        newContext.StatusContext.BlockUi = false;

        return newContext;
    }

    public static async Task<FileContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        FileContent initialContent)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new FileContentEditorContext(statusContext, FileContent.CreateInstance());
        await newContext.LoadData(initialContent);
        return newContext;
    }

    public Guid? CurrentMainPicture()
    {
        if (UserMainPictureEntry is { HasValidationIssues: false, UserValue: not null })
            return UserMainPictureEntry.UserValue;

        return BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(BodyContent?.UserValue, null).Result;
    }

    public FileContent CurrentStateToFileContent()
    {
        var newEntry = FileContent.CreateInstance();

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }

        newEntry.Folder = TitleSummarySlugFolder!.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed!.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.Tags = TagEdit!.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent!.UserValue.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.OriginalFileName = SelectedFile?.Name;
        newEntry.PublicDownloadLink = PublicDownloadLink!.UserValue;
        newEntry.EmbedFile = EmbedFile!.UserValue;
        newEntry.UserMainPicture = UserMainPictureEntry!.UserValue;
        newEntry.Latitude = OptionalLocationEntry!.LatitudeEntry!.UserValue;
        newEntry.Longitude = OptionalLocationEntry.LongitudeEntry!.UserValue;
        newEntry.Elevation = OptionalLocationEntry.ElevationEntry!.UserValue;
        newEntry.ShowLocation = OptionalLocationEntry.ShowLocationEntry!.UserValue;

        return newEntry;
    }

    public void DetectGuiFileTypes()
    {
        FileIsPdf = SelectedFile?.FullName.EndsWith("pdf", StringComparison.InvariantCultureIgnoreCase) ?? false;
        //8/6/2022 - This detection is mainly for extracting the first frame of a video - if changing/extending
        //this beyond mp4 (verified for example that the frame extraction works for avi) remember that the
        //there is a collision of concerns here and there may be merit in only encouraging formats that 
        //will work with the html video tag - see the file html...
        FileIsMp4 = SelectedFile?.FullName.EndsWith("mp4", StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    [NonBlockingCommand]
    private async Task DownloadLinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeFileDownloads.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        await StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    [NonBlockingCommand]
    public async Task EditUserMainPicture()
    {
        if (UserMainPictureEntryContent == null)
        {
            await StatusContext.ToastWarning("No Picture to Edit?");
            return;
        }

        await SetUserMainPicture();

        if (UserMainPictureEntryContent is PhotoContent photoToEdit)
        {
            var window =
                await PhotoContentEditorWindow.CreateInstance(photoToEdit);
            await window.PositionWindowAndShowOnUiThread();
            return;
        }

        if (UserMainPictureEntryContent is ImageContent imageToEdit)
        {
            var window =
                await ImageContentEditorWindow.CreateInstance(imageToEdit);
            await window.PositionWindowAndShowOnUiThread();
            return;
        }

        await StatusContext.ToastWarning("Didn't find the expected Photo/Image to edit?");
    }

    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{BodyContent!.UserValue} {UpdateNotes!.UserValue}",
            StatusContext.ProgressTracker());
    }

    [NonBlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeFiles.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        await StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    private async Task LoadData(FileContent? toLoad, bool skipMediaDirectoryCheck = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Loading Data...");

        DbEntry = NewContentModels.InitializeFileContent(toLoad);

        PublicDownloadLink = await BoolDataEntryContext.CreateInstance();
        PublicDownloadLink.Title = "Show Public Download Link";
        PublicDownloadLink.ReferenceValue = DbEntry.PublicDownloadLink;
        PublicDownloadLink.UserValue = DbEntry.PublicDownloadLink;
        PublicDownloadLink.HelpText =
            "If checked there will be a hyperlink will on the File Content Page to download the content. NOTE! The File" +
            "will be copied into the generated HTML for the site regardless of this setting - this setting is only about " +
            "whether a download link is shown.";

        PublicDownloadLink.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "UserValue")
            {
                if (PublicDownloadLink.UserValue == false)
                {
                    EmbedFile!.UserValue = false;
                    EmbedFile.IsEnabled = false;
                }
                else
                {
                    EmbedFile!.IsEnabled = true;
                }
            }
        };

        HelpContext = new HelpDisplayContext([
            FileEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        ]);

        EmbedFile = await BoolDataEntryContext.CreateInstance();
        EmbedFile.Title = "Embed File in Page";
        EmbedFile.ReferenceValue = DbEntry.EmbedFile;
        EmbedFile.UserValue = DbEntry.EmbedFile;
        EmbedFile.HelpText =
            "If checked supported file types will be embedded in the the page - in general this means that" +
            "there will be a viewer/player for the file. This option is only available if 'Show Public" +
            "Download Link' is checked and not all content types are supported.";

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry,
            "To File Name",
            AutoRenameSelectedFileBasedOnTitleCommand,
            x =>
                SelectedFile != null && !Path.GetFileNameWithoutExtension(SelectedFile.Name)
                    .Equals(SlugTools.CreateSlug(false, x.TitleEntry.UserValue), StringComparison.OrdinalIgnoreCase));
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        UserMainPictureEntry =
            await ConversionDataEntryContext<Guid?>.CreateInstance(ConversionDataEntryTypes
                .GuidNullableAndBracketCodeConversion);
        UserMainPictureEntry.ValidationFunctions = [CommonContentValidation.ValidateUserMainPicture];
        UserMainPictureEntry.ReferenceValue = DbEntry.UserMainPicture;
        UserMainPictureEntry.UserText = DbEntry.UserMainPicture.ToString() ?? string.Empty;
        UserMainPictureEntry.Title = "Link Image";
        UserMainPictureEntry.HelpText =
            "Putting a Photo or Image ContentId here will cause that image to be used as the 'link' image for the file - very useful when the content is embedded and you don't have a photo or image in the Body Content.";
        UserMainPictureEntry.PropertyChanged += UserMainPictureEntryOnPropertyChanged;
        await SetUserMainPicture();

        OptionalLocationEntry = await OptionalLocationEntryContext.CreateInstance(StatusContext, DbEntry);

        if (!skipMediaDirectoryCheck && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName) && DbEntry.Id > 0)
        {
            await FileManagement.CheckFileOriginalFileIsInMediaAndContentDirectories(DbEntry);

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                DbEntry.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(DbEntry);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, DbEntry.OriginalFileName));

            if (!archiveFile.Exists && contentFile.Exists)
            {
                await FileManagement.WriteSelectedFileContentFileToMediaArchive(contentFile);
                archiveFile.Refresh();
            }

            if (archiveFile.Exists)
            {
                LoadedFile = archiveFile;
                SelectedFile = archiveFile;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Missing File",
                    $"There is an original file listed for this entry - {DbEntry.OriginalFileName} -" +
                    $" but it was not found in the expected locations of {archiveFile.FullName} or {contentFile.FullName} - " +
                    "this will cause an error and prevent you from saving. You can re-load the file or " +
                    "maybe your media directory moved unexpectedly and you could close this editor " +
                    "and restore it (or change it in settings) before continuing?");
            }
        }

        if (DbEntry.Id < 1 && InitialFile is { Exists: true })
        {
            SelectedFile = InitialFile;
            InitialFile = null;

            if (SelectedFile.Extension == ".mp4")
            {
                var (generationReturn, metadata) =
                    await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());

                if (!generationReturn.HasError && metadata != null)
                {
                    TitleSummarySlugFolder.SummaryEntry.UserValue = metadata.Summary ?? string.Empty;
                    TagEdit.Tags = metadata.Tags ?? string.Empty;
                    TitleSummarySlugFolder.TitleEntry.UserValue = metadata.Title ?? string.Empty;
                    await TitleSummarySlugFolder.TitleToSlug();
                    TitleSummarySlugFolder.FolderEntry.UserValue = metadata.PhotoCreatedOn.Year.ToString("F0");
                    EmbedFile.UserValue = true;
                }
            }

            if (SelectedFile.Extension == ".pdf") EmbedFile.UserValue = true;

            if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.SummaryEntry.UserValue))
                TitleSummarySlugFolder.TitleEntry.UserValue = Regex.Replace(
                    Path.GetFileNameWithoutExtension(SelectedFile.Name).Replace("-", " ").Replace("_", " ")
                        .CamelCaseToSpacedString(), @"\s+", " ");

            if (!string.IsNullOrWhiteSpace(TitleSummarySlugFolder.TitleEntry.UserValue))
            {
                var possibleDateTimeFromTitle =
                    DateTimeTools.DateOnlyFromTitleStringByConvention(TitleSummarySlugFolder.TitleEntry.UserValue);
                if (possibleDateTimeFromTitle != null)
                    TitleSummarySlugFolder.FolderEntry.UserValue =
                        possibleDateTimeFromTitle.Value.titleDate.Year.ToString("F0");
            }
        }

        await SelectedFileChanged();

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void MainImageExternalContextSaved(object? sender, EventArgs e)
    {
        if (sender is ImageContentEditorContext imageContext)
        {
            StatusContext.RunNonBlockingTask(async () =>
                await TryAddUserMainPicture(imageContext.DbEntry.ContentId));

            if (MainImageExternalEditorWindow?.ImageEditor != null)
                MainImageExternalEditorWindow.ImageEditor.Saved -= MainImageExternalContextSaved;

            MainImageExternalEditorWindowCleanup();
        }
    }

    public void MainImageExternalEditorWindowCleanup()
    {
        if (MainImageExternalEditorWindow == null) return;

        try
        {
            MainImageExternalEditorWindow.Closed -= OnMainImageExternalEditorWindowOnClosed;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        try
        {
            if (MainImageExternalEditorWindow?.ImageEditor != null)
                MainImageExternalEditorWindow.ImageEditor.Saved -= MainImageExternalContextSaved;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnMainImageExternalEditorWindowOnClosed(object? sender, EventArgs args)
    {
        MainImageExternalEditorWindowCleanup();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(SelectedFile)) StatusContext.RunFireAndForgetNonBlockingTask(SelectedFileChanged);
    }

    [BlockingCommand]
    private async Task OpenSelectedFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            await StatusContext.ToastError("No Selected File or Selected File no longer exists?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [BlockingCommand]
    private async Task OpenSelectedFileDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            await StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
            return;
        }

        await ProcessHelpers.OpenExplorerWindowForFile(SelectedFile.FullName);
    }

    [BlockingCommand]
    private async Task PointFromLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("The Photo must be saved before creating a Point.");
            return;
        }

        if (OptionalLocationEntry!.LatitudeEntry!.UserValue == null ||
            OptionalLocationEntry.LongitudeEntry!.UserValue == null)
        {
            await StatusContext.ToastError("Latitude or Longitude is missing?");
            return;
        }

        var latitudeValidation =
            await CommonContentValidation.LatitudeValidation(OptionalLocationEntry.LatitudeEntry.UserValue.Value);
        var longitudeValidation =
            await CommonContentValidation.LongitudeValidation(OptionalLocationEntry.LongitudeEntry.UserValue.Value);

        if (!latitudeValidation.Valid || !longitudeValidation.Valid)
        {
            await StatusContext.ToastError("Latitude/Longitude is not valid?");
            return;
        }

        var frozenNow = DateTime.Now;

        var newPartialPoint = PointContent.CreateInstance();

        newPartialPoint.CreatedOn = frozenNow;
        newPartialPoint.FeedOn = frozenNow;
        newPartialPoint.BodyContent = EmbedFile!.UserValue
            ? BracketCodeFileEmbed.Create(DbEntry)
            : BracketCodeFiles.Create(DbEntry);
        newPartialPoint.Title = $"Point From {TitleSummarySlugFolder!.TitleEntry.UserValue}";
        newPartialPoint.Tags = TagEdit!.TagListString();
        newPartialPoint.Slug = SlugTools.CreateSlug(true, newPartialPoint.Title);
        newPartialPoint.Latitude = OptionalLocationEntry.LatitudeEntry.UserValue.Value;
        newPartialPoint.Longitude = OptionalLocationEntry.LongitudeEntry.UserValue.Value;
        newPartialPoint.Elevation = OptionalLocationEntry.ElevationEntry!.UserValue;

        var pointWindow = await PointContentEditorWindow.CreateInstance(newPartialPoint);

        await pointWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    public async Task RenameSelectedFile()
    {
        await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x);
    }

    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(true);
    }

    [BlockingCommand]
    private async Task SaveAndExtractImageFromMp4()
    {
        if (SelectedFile is not { Exists: true } || !SelectedFile.Extension.ToUpperInvariant().Contains("MP4"))
        {
            await StatusContext.ToastError("Please selected a valid mp4 file");
            return;
        }

        var (generationReturn, fileContent) = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
            SelectedFile, null, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            await StatusContext.ShowMessageWithOkButton("Trouble Saving",
                $"Trouble saving - you must be able to save before extracting a frame - {generationReturn.GenerationNote}");
            return;
        }

        await LoadData(fileContent);

        var autoSaveResult = await ImageExtractionHelpers.VideoFrameToImageAutoSave(StatusContext, DbEntry);

        if (autoSaveResult == null) return;

        UserMainPictureEntry!.UserText = autoSaveResult.Value.ToString();
    }

    [BlockingCommand]
    private async Task SaveAndExtractImageFromPdf()
    {
        if (SelectedFile is not { Exists: true } || !SelectedFile.Extension.ToUpperInvariant().Contains("PDF"))
        {
            await StatusContext.ToastError("Please selected a valid pdf file");
            return;
        }

        if (string.IsNullOrWhiteSpace(PdfToImagePageToExtract))
        {
            await StatusContext.ToastError("Please enter a page number");
            return;
        }

        if (!int.TryParse(PdfToImagePageToExtract, out var pageNumber))
        {
            await StatusContext.ToastError("Please enter a valid page number");
            return;
        }

        if (pageNumber < 1)
        {
            await StatusContext.ToastError("Please selected a valid page number");
            return;
        }

        var (generationReturn, fileContent) = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
            SelectedFile, null, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            await StatusContext.ShowMessageWithOkButton("Trouble Saving",
                $"Trouble saving - you must be able to save before extracting a page - {generationReturn.GenerationNote}");
            return;
        }

        await LoadData(fileContent);

        var autosaveReturn = await ImageExtractionHelpers.PdfPageToImageWithAutoSave(StatusContext,
            DbEntry,
            pageNumber);

        if (autosaveReturn.contentId != null)
        {
            UserMainPictureEntry!.UserText = autosaveReturn.contentId.Value.ToString();
            return;
        }

        if (autosaveReturn.editor != null)
        {
            MainImageExternalEditorWindow = autosaveReturn.editor;
            MainImageExternalEditorWindow.Closed += OnMainImageExternalEditorWindowOnClosed;
            if (MainImageExternalEditorWindow?.ImageEditor != null)
                MainImageExternalEditorWindow.ImageEditor.Saved += MainImageExternalContextSaved;
        }
    }

    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile == null)
        {
            await StatusContext.ToastError("No File Selected?");
            return;
        }

        var (generationReturn, newContent) = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
            SelectedFile, null, StatusContext.ProgressTracker());

        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                generationReturn.GenerationNote);
            return;
        }

        await LoadData(newContent);

        if (closeAfterSave)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task SelectedFileChanged()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedFileHasPathOrNameChanges =
            (SelectedFile?.FullName ?? string.Empty) != (LoadedFile?.FullName ?? string.Empty);

        var (isValid, explanation) =
            await CommonContentValidation.FileContentFileValidation(SelectedFile, DbEntry.ContentId);

        SelectedFileHasValidationIssues = !isValid;

        SelectedFileValidationMessage = explanation;

        SelectedFileNameHasInvalidCharacters =
            await CommonContentValidation.FileContentFileFileNameHasInvalidCharacters(SelectedFile, DbEntry.ContentId);

        DetectGuiFileTypes();

        TitleSummarySlugFolder?.CheckForChangesToTitleToFunctionStates();
    }

    public async Task SetUserMainPicture()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (UserMainPictureEntry!.HasValidationIssues ||
            UserMainPictureEntry.UserValue == null)
        {
            UserMainPictureEntrySmallImageUrl = null;
            UserMainPictureEntryContent = null;
            return;
        }

        try
        {
            var db = await Db.Context();
            UserMainPictureEntryContent = await db.ContentFromContentId(UserMainPictureEntry.UserValue.Value);

            UserMainPictureEntrySmallImageUrl = PictureAssetProcessing
                .ProcessPictureDirectory(UserMainPictureEntry.UserValue.Value)?.SmallPicture
                ?.File?.FullName;
        }
        catch (Exception e)
        {
            UserMainPictureEntrySmallImageUrl = null;
            UserMainPictureEntryContent = null;
            Log.Error(e, "Caught exception in FileContentEditorContext while trying to setup the User Main Picture.");
        }
    }

    public async Task TryAddUserMainPicture(Guid? contentId)
    {
        if (contentId == null || contentId == Guid.Empty) return;
        var context = await Db.Context();
        if (context.ImageContents.Any(x => x.ContentId == contentId))
            UserMainPictureEntry!.UserText = contentId.Value.ToString();
    }

    private void UserMainPictureEntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StatusContext.RunFireAndForgetNonBlockingTask(SetUserMainPicture);
    }

    [BlockingCommand]
    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Please save the content first...");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $"{settings.FilePageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [NonBlockingCommand]
    public async Task ViewUserMainPicture()
    {
        if (UserMainPictureEntryContent == null)
        {
            await StatusContext.ToastWarning("No Picture to View?");
            return;
        }

        await SetUserMainPicture();

        if (UserMainPictureEntryContent is PhotoContent photoToEdit)
        {
            var possibleFile = UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(photoToEdit);

            if (possibleFile is not { Exists: true })
            {
                await StatusContext.ToastWarning("No Media File Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(possibleFile.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
            return;
        }

        if (UserMainPictureEntryContent is ImageContent imageToEdit)
        {
            var possibleFile = UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(imageToEdit);

            if (possibleFile is not { Exists: true })
            {
                await StatusContext.ToastWarning("No Media File Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(possibleFile.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
            return;
        }

        await StatusContext.ToastWarning("Didn't find the expected Photo/Image to view?");
    }
}