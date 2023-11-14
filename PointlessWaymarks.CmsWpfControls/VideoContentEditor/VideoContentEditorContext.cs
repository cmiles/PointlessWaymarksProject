using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.SimpleMediaPlayer;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.VideoContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class VideoContentEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;

    private VideoContentEditorContext(StatusControlContext statusContext, VideoContent dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        VideoContext = new SimpleMediaPlayerContext();

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public BodyContentEditorContext? BodyContent { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public VideoContent DbEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public FileInfo? InitialVideo { get; set; }
    public StringDataEntryContext? LicenseEntry { get; set; }
    public FileInfo? LoadedFile { get; set; }
    public ImageContentEditorWindow? MainImageExternalEditorWindow { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public FileInfo? SelectedFile { get; set; }
    public bool SelectedFileHasPathOrNameChanges { get; set; }
    public bool SelectedFileHasValidationIssues { get; set; }
    public bool SelectedFileNameHasInvalidCharacters { get; set; }
    public string SelectedFileValidationMessage { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    public ConversionDataEntryContext<Guid?>? UserMainPictureEntry { get; set; }
    public IContentCommon? UserMainPictureEntryContent { get; set; }
    public string? UserMainPictureEntrySmallImageUrl { get; set; }
    public SimpleMediaPlayerContext? VideoContext { get; set; }
    public StringDataEntryContext? VideoCreatedByEntry { get; set; }
    public ConversionDataEntryContext<DateTime>? VideoCreatedOnEntry { get; set; }
    public ConversionDataEntryContext<DateTime?>? VideoCreatedOnUtcEntry { get; set; }


    public string VideoEditorHelpText =>
        @"
### Video Content

Interesting books, dissertations, academic papers, maps, meeting notes, articles, memos, reports, etc. are available on a wide variety of subjects - but over years, decades, of time resources can easily 'disappear' from the internet... Websites are no longer available, agencies delete documents they are no longer legally required to retain, older versions of a document are not kept when a newer version comes out, departments shut down, funding runs out...

Video Content is intended to allow the creation of a 'library' of Videos that you can tag, search, share and retain. The Video you choose for Video Content will be copied to the site just like an image or photo would be.

With any file you have on your site it is your responsibility to know if it is legally acceptable to have the file on the site - like any content in this CMS you should only enter it into the CMS if you want it 'publicly' available on your site, there are options that allow some content to be more discrete - but NO options that allow you to fully hide content.

Notes:
 - No Video Previews are automatically generated - you will need to add any images/previews/etc. manually to the Body Content
 - To help when working with PDFs the program can extract pages of a PDF as Image Content for quick/easy use in the Body Content - details:
   - To use this functionality pdftocairo must be available on your computer and the location of pdftocairo must be set in the Settings
   - On windows the easiest way to install pdftocairo is to install MiKTeX - [Getting MiKTeX - MiKTeX.org](https://miktex.org/download)
   - The page you specify to generate an image is the page that the PDF Viewer you are using is showing (rather than the 'content page number' printed at the bottom of a page) - for example with a book in PDF format to get an image of the 'cover' the page number is '1'
 - The Video Content page can contain a link to download the file - but it is not appropriate to offer all content for download, use the 'Show Public Download Link' to turn on/off the download link. This setting will impact the behaviour of the 'filedownloadlink' bracket code - if 'Show Public Download Link' is unchecked a filedownloadlink bracket code will become a link to the Video Content Page (rather than a download link for the content).
 - Regardless of the 'Show Public Download Link' the file will be copied to the site - if you have a sensitive document that should not be copied beyond your computer consider just creating Post Content for it - the Video Content type is only useful for content where you want the Video to be 'with' the site.
 - If appropriate consider including links to the original source in the Body Content
 - If what you are writing about is a 'file' but you don't want/need to store the file itself on your site you should probably just create a Post (or other content type like and Image) - use Video Content when you want to store the file. 
";


    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges ||
                     DbEntry.MainPicture != CurrentMainPicture();
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              SelectedFileHasValidationIssues;
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    [BlockingCommand]
    public async Task AutoCleanRenameSelectedFile()
    {
        await FileHelpers.TryAutoCleanRenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x);
    }

    [BlockingCommand]
    public async Task AutoRenameSelectedFileBasedOnTitle()
    {
        await FileHelpers.TryAutoRenameSelectedFile(SelectedFile, TitleSummarySlugFolder!.TitleEntry.UserValue,
            StatusContext, x => SelectedFile = x);
    }

    public async Task ChooseFile(bool loadMetadata)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting image load.");

        var dialog = new VistaOpenFileDialog { Filter = "supported formats (*.mp4;*.webm,*.ogg)|*.mp4;*.webm;*.ogg" };

        if (!(dialog.ShowDialog() ?? false)) return;

        var newFile = new FileInfo(dialog.FileName);

        if (!newFile.Exists)
        {
            StatusContext.ToastError("Video doesn't exist?");
            return;
        }

        if (!FileHelpers.VideoFileTypeIsSupported(newFile))
        {
            StatusContext.ToastError("Only JPEGs are supported...");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedFile = newFile;

        StatusContext.Progress($"Video load - {SelectedFile.FullName} ");

        if (!loadMetadata) return;

        var (generationReturn, metadata) =
            await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            await StatusContext.ShowMessageWithOkButton("Video Metadata Load Issue", generationReturn.GenerationNote);
            return;
        }

        if (metadata == null)
        {
            await StatusContext.ShowMessageWithOkButton("Video Metadata in Null?", generationReturn.GenerationNote);
            return;
        }

        VideoMetadataToCurrentContent(metadata);
    }

    [BlockingCommand]
    public async Task ChooseFileAndFillMetadata()
    {
        await ChooseFile(true);
    }

    [BlockingCommand]
    public async Task ChooseFileWithoutMetadataLoad()
    {
        await ChooseFile(false);
    }

    public static async Task<VideoContentEditorContext> CreateInstance(StatusControlContext statusContext,
        FileInfo? initialVideo = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toLoad = VideoContent.CreateInstance();

        var newContext = new VideoContentEditorContext(statusContext, toLoad) { StatusContext = { BlockUi = true } };

        if (initialVideo is { Exists: true }) newContext.InitialVideo = initialVideo;

        await newContext.LoadData(toLoad);
        
        newContext.StatusContext.BlockUi = false;

        return newContext;
    }

    public static async Task<VideoContentEditorContext> CreateInstance(StatusControlContext statusContext,
        VideoContent? initialContent)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newControl =
            new VideoContentEditorContext(statusContext, NewContentModels.InitializeVideoContent(initialContent));
        await newControl.LoadData(initialContent);
        return newControl;
    }

    public Guid? CurrentMainPicture()
    {
        if (UserMainPictureEntry is { HasValidationIssues: false, UserValue: not null })
            return UserMainPictureEntry.UserValue;

        return BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(BodyContent?.UserBodyContent);
    }

    public VideoContent CurrentStateToVideoContent()
    {
        var newEntry = VideoContent.CreateInstance();

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
        newEntry.UpdateNotes = UpdateNotes!.UpdateNotes.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent!.BodyContent.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.OriginalFileName = SelectedFile!.Name;
        newEntry.UserMainPicture = UserMainPictureEntry!.UserValue;

        newEntry.License = LicenseEntry!.UserValue.TrimNullToEmpty();
        newEntry.VideoCreatedBy = VideoCreatedByEntry!.UserValue.TrimNullToEmpty();
        newEntry.VideoCreatedOn = VideoCreatedOnEntry!.UserValue;
        newEntry.VideoCreatedOnUtc = VideoCreatedOnUtcEntry!.UserValue;

        return newEntry;
    }

    [NonBlockingCommand]
    public async Task EditUserMainPicture()
    {
        if (UserMainPictureEntryContent == null)
        {
            StatusContext.ToastWarning("No Picture to Edit?");
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

        StatusContext.ToastWarning("Didn't find the expected Photo/Image to edit?");
    }

    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{BodyContent!.BodyContent} {UpdateNotes!.UpdateNotes}",
            StatusContext.ProgressTracker());
    }

    [NonBlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeVideoEmbed.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    private async Task LoadData(VideoContent? toLoad, bool skipMediaDirectoryCheck = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Loading Data...");

        DbEntry = NewContentModels.InitializeVideoContent(toLoad);

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry,
            "To File Name",
            AutoRenameSelectedFileBasedOnTitleCommand,
            x => SelectedFile != null && !Path.GetFileNameWithoutExtension(SelectedFile.Name)
                .Equals(SlugTools.CreateSlug(false, x.TitleEntry.UserValue), StringComparison.OrdinalIgnoreCase));
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);

        LicenseEntry = StringDataEntryContext.CreateInstance();
        LicenseEntry.Title = "License";
        LicenseEntry.HelpText = "The Video's License";
        LicenseEntry.ReferenceValue = DbEntry.License ?? string.Empty;
        LicenseEntry.UserValue = DbEntry.License.TrimNullToEmpty();

        VideoCreatedByEntry = StringDataEntryContext.CreateInstance();
        VideoCreatedByEntry.Title = "Video Created By";
        VideoCreatedByEntry.HelpText = "Who created the video";
        VideoCreatedByEntry.ReferenceValue = DbEntry.VideoCreatedBy ?? string.Empty;
        VideoCreatedByEntry.UserValue = DbEntry.VideoCreatedBy.TrimNullToEmpty();

        VideoCreatedOnEntry =
            await ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
        VideoCreatedOnEntry.Title = "Video Created On";
        VideoCreatedOnEntry.HelpText = "Date and, optionally, Time the Video was Created";
        VideoCreatedOnEntry.ReferenceValue = DbEntry.VideoCreatedOn;
        VideoCreatedOnEntry.UserText = DbEntry.VideoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");

        VideoCreatedOnUtcEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        VideoCreatedOnUtcEntry.Title = "Video Created On UTC Date/Time";
        VideoCreatedOnUtcEntry.HelpText =
            "UTC Date and Time the Video was Created - the UTC Date Time is not displayed but is used to compare the Video's Date Time to data like GPX Files/Lines.";
        VideoCreatedOnUtcEntry.ReferenceValue = DbEntry.VideoCreatedOnUtc;
        VideoCreatedOnUtcEntry.UserText = DbEntry.VideoCreatedOnUtc?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;

        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        UserMainPictureEntry =
            await ConversionDataEntryContext<Guid?>.CreateInstance(ConversionDataEntryTypes
                .GuidNullableAndBracketCodeConversion);
        UserMainPictureEntry.ValidationFunctions = new List<Func<Guid?, Task<IsValid>>>
            { CommonContentValidation.ValidateUserMainPicture };
        UserMainPictureEntry.ReferenceValue = DbEntry.UserMainPicture;
        UserMainPictureEntry.UserText = DbEntry.UserMainPicture.ToString() ?? string.Empty;
        UserMainPictureEntry.Title = "Link Image";
        UserMainPictureEntry.HelpText =
            "Putting a Photo or Image ContentId here will cause that image to be used as the 'link' image for the file - very useful when the content is embedded and you don't have a photo or image in the Body Content.";
        UserMainPictureEntry.PropertyChanged += UserMainPictureEntryOnPropertyChanged;
        await SetUserMainPicture();

        HelpContext = new HelpDisplayContext(new List<string>
        {
            VideoEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        if (!skipMediaDirectoryCheck && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName) && DbEntry.Id > 0)
        {
            await FileManagement.CheckVideoOriginalFileIsInMediaAndContentDirectories(DbEntry);

            var archiveVideo = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveVideoDirectory().FullName,
                DbEntry.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteVideoContentDirectory(DbEntry);

            var contentVideo = new FileInfo(Path.Combine(fileContentDirectory.FullName, DbEntry.OriginalFileName));

            if (!archiveVideo.Exists && contentVideo.Exists)
            {
                await FileManagement.WriteSelectedVideoContentFileToMediaArchive(contentVideo);
                archiveVideo.Refresh();
            }

            if (archiveVideo.Exists)
            {
                LoadedFile = archiveVideo;
                SelectedFile = archiveVideo;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Missing Video",
                    $"There is an original file listed for this entry - {DbEntry.OriginalFileName} -" +
                    $" but it was not found in the expected locations of {archiveVideo.FullName} or {contentVideo.FullName} - " +
                    "this will cause an error and prevent you from saving. You can re-load the file or " +
                    "maybe your media directory moved unexpectedly and you could close this editor " +
                    "and restore it (or change it in settings) before continuing?");
            }
        }

        if (DbEntry.Id < 1 && InitialVideo is { Exists: true } && FileHelpers.VideoFileTypeIsSupported(InitialVideo))
        {
            SelectedFile = InitialVideo;
            InitialVideo = null;
            var (generationReturn, metadataReturn) =
                await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());
            if (!generationReturn.HasError && metadataReturn != null) VideoMetadataToCurrentContent(metadataReturn);
        }

        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.SummaryEntry.UserValue) && SelectedFile != null)
            TitleSummarySlugFolder.TitleEntry.UserValue = Regex.Replace(
                Path.GetFileNameWithoutExtension(SelectedFile.Name).Replace("-", " ").Replace("_", " ")
                    .CamelCaseToSpacedString(), @"\s+", " ");

        await SelectedFileChanged();
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
        if (MainImageExternalEditorWindow?.ImageEditor == null) return;

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
            StatusContext.ToastError("No Selected Video or Selected Video no longer exists?");
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
            StatusContext.ToastWarning("No Selected Video or Selected Video no longer exists?");
            return;
        }

        await ProcessHelpers.OpenExplorerWindowForFile(SelectedFile.FullName);
    }

    [BlockingCommand]
    public async Task RenameSelectedFile()
    {
        await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x);
    }

    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(true, false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(true, true);
    }

    [BlockingCommand]
    private async Task SaveAndExtractImageFromMp4()
    {
        if (SelectedFile is not { Exists: true } || !SelectedFile.Extension.ToUpperInvariant().Contains("MP4"))
        {
            StatusContext.ToastError("Please selected a valid mp4 file");
            return;
        }

        var (generationReturn, fileContent) = await VideoGenerator.SaveAndGenerateHtml(CurrentStateToVideoContent(),
            SelectedFile, true, null, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            await StatusContext.ShowMessageWithOkButton("Trouble Saving",
                $"Trouble saving - you must be able to save before extracting a frame - {generationReturn.GenerationNote}");
            return;
        }

        await LoadData(fileContent);

        var autoSaveResult =
            await ImageExtractionHelpers.VideoFrameToImageAutoSave(StatusContext, DbEntry,
                VideoContext!.VideoPositionInMilliseconds);

        if (autoSaveResult == null) return;

        UserMainPictureEntry!.UserText = autoSaveResult.Value.ToString();
    }

    public async Task SaveAndGenerateHtml(bool overwriteExistingVideos, bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile == null)
        {
            StatusContext.ToastError("No File Selected? There must be a video to Save...");
            return;
        }

        var (generationReturn, newContent) = await VideoGenerator.SaveAndGenerateHtml(CurrentStateToVideoContent(),
            SelectedFile, overwriteExistingVideos, null, StatusContext.ProgressTracker());

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

        VideoContext!.VideoSource = SelectedFile is { Exists: true }
            ? VideoContext.VideoSource = SelectedFile.FullName
            : VideoContext.VideoSource = string.Empty;
        
        TitleSummarySlugFolder?.CheckForChangesToTitleToFunctionStates();
    }

    public async Task SetUserMainPicture()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (UserMainPictureEntry == null || UserMainPictureEntry.HasValidationIssues ||
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
            Log.Error(e, "Caught exception in VideoContentEditorContext while trying to setup the User Main Picture.");
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


    public void VideoMetadataToCurrentContent(PhotoMetadata metadata)
    {
        LicenseEntry!.UserValue = metadata.License ?? string.Empty;
        VideoCreatedByEntry!.UserValue = metadata.PhotoCreatedBy ?? string.Empty;
        VideoCreatedOnEntry!.UserText = metadata.PhotoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");
        VideoCreatedOnUtcEntry!.UserText =
            metadata.PhotoCreatedOnUtc?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
        TitleSummarySlugFolder!.SummaryEntry.UserValue = metadata.Summary ?? string.Empty;
        TagEdit!.Tags = metadata.Tags ?? string.Empty;
        TitleSummarySlugFolder.TitleEntry.UserValue = metadata.Title ?? string.Empty;
        TitleSummarySlugFolder.TitleToSlug();
        TitleSummarySlugFolder.FolderEntry.UserValue = metadata.PhotoCreatedOn.Year.ToString("F0");
    }

    [BlockingCommand]
    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Please save the content first...");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.VideoPageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [NonBlockingCommand]
    public async Task ViewUserMainPicture()
    {
        if (UserMainPictureEntryContent == null)
        {
            StatusContext.ToastWarning("No Picture to View?");
            return;
        }

        await SetUserMainPicture();

        if (UserMainPictureEntryContent is PhotoContent photoToEdit)
        {
            var possibleVideo = UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(photoToEdit);

            if (possibleVideo is not { Exists: true })
            {
                StatusContext.ToastWarning("No Media Video Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(possibleVideo.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
            return;
        }

        if (UserMainPictureEntryContent is ImageContent imageToEdit)
        {
            var possibleVideo = UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(imageToEdit);

            if (possibleVideo is not { Exists: true })
            {
                StatusContext.ToastWarning("No Media Video Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(possibleVideo.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
            return;
        }

        StatusContext.ToastWarning("Didn't find the expected Photo/Image to view?");
    }

    [BlockingCommand]
    public async Task ViewVideoMetadata()
    {
        await PhotoMetadataReport.AllPhotoMetadataToHtml(SelectedFile, StatusContext);
    }
}