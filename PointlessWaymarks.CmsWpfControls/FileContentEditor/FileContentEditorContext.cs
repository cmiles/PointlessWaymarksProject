using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
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
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.FileContentEditor;

public partial class FileContentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private RelayCommand _autoRenameSelectedFileCommand;
    [ObservableProperty] private BodyContentEditorContext _bodyContent;
    [ObservableProperty] private RelayCommand _chooseFileCommand;
    [ObservableProperty] private ContentIdViewerControlContext _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
    [ObservableProperty] private FileContent _dbEntry;
    [ObservableProperty] private RelayCommand _downloadLinkToClipboardCommand;
    [ObservableProperty] private RelayCommand _editUserMainPictureCommand;
    [ObservableProperty] private BoolDataEntryContext _embedFile;
    [ObservableProperty] private RelayCommand _extractNewLinksCommand;
    [ObservableProperty] private bool _fileIsMp4;
    [ObservableProperty] private bool _fileIsPdf;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext _helpContext;
    [ObservableProperty] private FileInfo _initialFile;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private FileInfo _loadedFile;


    [ObservableProperty] [CanBeNull] private ImageContentEditorWindow _mainImageExternalEditorWindow;
    [ObservableProperty] private ContentSiteFeedAndIsDraftContext _mainSiteFeed;
    [ObservableProperty] private RelayCommand _openSelectedFileCommand;
    [ObservableProperty] private RelayCommand _openSelectedFileDirectoryCommand;
    [ObservableProperty] private string _pdfToImagePageToExtract = "1";
    [ObservableProperty] private BoolDataEntryContext _publicDownloadLink;
    [ObservableProperty] private RelayCommand _renameSelectedFileCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveAndExtractImageFromPdfCommand;
    [ObservableProperty] private RelayCommand _saveAndExtractImageFromVideoCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private FileInfo _selectedFile;
    [ObservableProperty] private bool _selectedFileHasPathOrNameChanges;
    [ObservableProperty] private bool _selectedFileHasValidationIssues;
    [ObservableProperty] private bool _selectedFileNameHasInvalidCharacters;
    [ObservableProperty] private string _selectedFileValidationMessage;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext _tagEdit;
    [ObservableProperty] private TitleSummarySlugEditorContext _titleSummarySlugFolder;
    [ObservableProperty] private UpdateNotesEditorContext _updateNotes;
    [ObservableProperty] private ConversionDataEntryContext<Guid?> _userMainPictureEntry;
    [ObservableProperty] private IContentCommon _userMainPictureEntryContent;
    [ObservableProperty] private string _userMainPictureEntrySmallImageUrl;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;
    [ObservableProperty] private RelayCommand _viewUserMainPictureCommand;

    public EventHandler RequestContentEditorWindowClose;

    private FileContentEditorContext(StatusControlContext statusContext, FileInfo initialFile = null)
    {
        if (initialFile is { Exists: true }) _initialFile = initialFile;

        PropertyChanged += OnPropertyChanged;

        SetupStatusContextAndCommands(statusContext);
    }

    private FileContentEditorContext(StatusControlContext statusContext)
    {
        PropertyChanged += OnPropertyChanged;

        SetupStatusContextAndCommands(statusContext);
    }


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


    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges ||
                     DbEntry?.MainPicture != CurrentMainPicture();
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              SelectedFileHasValidationIssues;
    }

    public async Task ChooseFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting image load.");

        var dialog = new VistaOpenFileDialog();

        if (!(dialog.ShowDialog() ?? false)) return;

        var newFile = new FileInfo(dialog.FileName);

        if (!newFile.Exists)
        {
            StatusContext.ToastError("File doesn't exist?");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedFile = newFile;

        StatusContext.Progress($"File load - {SelectedFile.FullName} ");
    }

    public static async Task<FileContentEditorContext> CreateInstance(StatusControlContext statusContext,
        FileInfo initialFile = null)
    {
        var newControl = new FileContentEditorContext(statusContext, initialFile);
        await newControl.LoadData(null);
        return newControl;
    }

    public static async Task<FileContentEditorContext> CreateInstance(StatusControlContext statusContext,
        FileContent initialContent)
    {
        var newControl = new FileContentEditorContext(statusContext);
        await newControl.LoadData(initialContent);
        return newControl;
    }

    public Guid? CurrentMainPicture()
    {
        if (UserMainPictureEntry is { HasValidationIssues: false, UserValue: { } })
            return UserMainPictureEntry.UserValue;

        return BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(BodyContent?.UserBodyContent);
    }

    public FileContent CurrentStateToFileContent()
    {
        var newEntry = new FileContent();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            newEntry.ContentId = Guid.NewGuid();
            newEntry.CreatedOn = DbEntry?.CreatedOn ?? DateTime.Now;
            if (newEntry.CreatedOn == DateTime.MinValue) newEntry.CreatedOn = DateTime.Now;
        }
        else
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }

        newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.Tags = TagEdit.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.OriginalFileName = SelectedFile.Name;
        newEntry.PublicDownloadLink = PublicDownloadLink.UserValue;
        newEntry.EmbedFile = PublicDownloadLink.UserValue && EmbedFile.UserValue;
        newEntry.UserMainPicture = UserMainPictureEntry.UserValue;

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

    private async Task DownloadLinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeFileDownloads.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

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

    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeFiles.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    private async Task LoadData(FileContent toLoad, bool skipMediaDirectoryCheck = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Loading Data...");

        var created = DateTime.Now;

        DbEntry = toLoad ?? new FileContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            PublicDownloadLink = true,
            CreatedOn = created,
            FeedOn = created
        };

        PublicDownloadLink = BoolDataEntryContext.CreateInstance();
        PublicDownloadLink.Title = "Show Public Download Link";
        PublicDownloadLink.ReferenceValue = DbEntry.PublicDownloadLink;
        PublicDownloadLink.UserValue = DbEntry.PublicDownloadLink;
        PublicDownloadLink.HelpText =
            "If checked there will be a hyperlink will on the File Content Page to download the content. NOTE! The File" +
            "will be copied into the generated HTML for the site regardless of this setting - this setting is only about " +
            "whether a download link is shown.";
        PublicDownloadLink.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "UserValue" && PublicDownloadLink != null && EmbedFile != null)
            {
                if (PublicDownloadLink.UserValue == false)
                {
                    EmbedFile.UserValue = false;
                    EmbedFile.IsEnabled = false;
                }
                else
                {
                    EmbedFile.IsEnabled = true;
                }
            }
        };

        EmbedFile = BoolDataEntryContext.CreateInstance();
        EmbedFile.Title = "Embed File in Page";
        EmbedFile.ReferenceValue = DbEntry.EmbedFile;
        EmbedFile.UserValue = DbEntry.EmbedFile;
        EmbedFile.HelpText =
            "If checked supported file types will be embedded in the the page - in general this means that" +
            "there will be a viewer/player for the file. This option is only available if 'Show Public" +
            "Download Link' is checked and not all content types are supported.";

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, "To File Name",
        AutoRenameSelectedFileBasedOnTitleCommand,
        x => !Path.GetFileNameWithoutExtension(SelectedFile.Name)
            .Equals(SlugTools.CreateSlug(false, x.TitleEntry.UserValue), StringComparison.OrdinalIgnoreCase),
        DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        UserMainPictureEntry =
            ConversionDataEntryContext<Guid?>.CreateInstance(ConversionDataEntryTypes.GuidNullableAndBracketCodeConversion);
        UserMainPictureEntry.ValidationFunctions = new List<Func<Guid?, Task<IsValid>>>
            { CommonContentValidation.ValidateUserMainPicture };
        UserMainPictureEntry.ReferenceValue = DbEntry.UserMainPicture;
        UserMainPictureEntry.UserText = DbEntry?.UserMainPicture.ToString() ?? string.Empty;
        UserMainPictureEntry.Title = "Link Image";
        UserMainPictureEntry.HelpText =
            "Putting a Photo or Image ContentId here will cause that image to be used as the 'link' image for the file - very useful when the content is embedded and you don't have a photo or image in the Body Content.";
        UserMainPictureEntry.PropertyChanged += UserMainPictureEntryOnPropertyChanged;
        await SetUserMainPicture();

        if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
        {
            await FileManagement.CheckFileOriginalFileIsInMediaAndContentDirectories(DbEntry);

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                DbEntry.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(toLoad);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, toLoad.OriginalFileName));

            if (archiveFile.Exists)
            {
                _loadedFile = archiveFile;
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

        if (DbEntry.Id < 1 && _initialFile is { Exists: true })
        {
            SelectedFile = _initialFile;
            _initialFile = null;

            if (SelectedFile.Extension == ".mp4")
            {
                var (generationReturn, metadata) =
                    await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());

                if (!generationReturn.HasError)
                {
                    TitleSummarySlugFolder.SummaryEntry.UserValue = metadata.Summary;
                    TagEdit.Tags = metadata.Tags;
                    TitleSummarySlugFolder.TitleEntry.UserValue = metadata.Title;
                    TitleSummarySlugFolder.TitleToSlug();
                    TitleSummarySlugFolder.FolderEntry.UserValue = metadata.PhotoCreatedOn.Year.ToString("F0");
                    EmbedFile.UserValue = true;
                }
            }

            if (SelectedFile.Extension == ".pdf") EmbedFile.UserValue = true;

            if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.SummaryEntry.UserValue))
                TitleSummarySlugFolder.TitleEntry.UserValue = Regex.Replace(
                    Path.GetFileNameWithoutExtension(SelectedFile.Name).Replace("-", " ").Replace("_", " ")
                        .SplitCamelCase(), @"\s+", " ");
        }

        await SelectedFileChanged();
    }

    private void MainImageExternalContextSaved(object sender, EventArgs e)
    {
        if (sender is ImageContentEditorContext imageContext)
        {
            StatusContext.RunNonBlockingTask(async () =>
                await TryAddUserMainPicture(imageContext.DbEntry.ContentId));

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
            MainImageExternalEditorWindow.ImageEditor.Saved -= MainImageExternalContextSaved;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnMainImageExternalEditorWindowOnClosed(object sender, EventArgs args)
    {
        MainImageExternalEditorWindowCleanup();
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(SelectedFile)) StatusContext.RunFireAndForgetNonBlockingTask(SelectedFileChanged);
    }

    private async Task OpenSelectedFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            StatusContext.ToastError("No Selected File or Selected File no longer exists?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    private async Task OpenSelectedFileDirectory()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.Directory.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    private async Task SaveAndExtractImageFromMp4()
    {
        if (SelectedFile is not { Exists: true } || !SelectedFile.Extension.ToUpperInvariant().Contains("MP4"))
        {
            StatusContext.ToastError("Please selected a valid mp4 file");
            return;
        }

        var (generationReturn, fileContent) = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
            SelectedFile, true, null, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            await StatusContext.ShowMessageWithOkButton("Trouble Saving",
                $"Trouble saving - you must be able to save before extracting a frame - {generationReturn.GenerationNote}");
            return;
        }

        await LoadData(fileContent);

        var autoSaveResult = await ImageExtractionHelpers.VideoFrameToImageAutoSave(StatusContext, DbEntry);

        if (autoSaveResult == null) return;

        UserMainPictureEntry.UserText = autoSaveResult.Value.ToString();
    }

    private async Task SaveAndExtractImageFromPdf()
    {
        if (SelectedFile is not { Exists: true } || !SelectedFile.Extension.ToUpperInvariant().Contains("PDF"))
        {
            StatusContext.ToastError("Please selected a valid pdf file");
            return;
        }

        if (string.IsNullOrWhiteSpace(PdfToImagePageToExtract))
        {
            StatusContext.ToastError("Please enter a page number");
            return;
        }

        if (!int.TryParse(PdfToImagePageToExtract, out var pageNumber))
        {
            StatusContext.ToastError("Please enter a valid page number");
            return;
        }

        if (pageNumber < 1)
        {
            StatusContext.ToastError("Please selected a valid page number");
            return;
        }

        var (generationReturn, fileContent) = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
            SelectedFile, true, null, StatusContext.ProgressTracker());

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
            UserMainPictureEntry.UserText = autosaveReturn.contentId.Value.ToString();
            return;
        }

        if (autosaveReturn.editor != null)
        {
            MainImageExternalEditorWindow = autosaveReturn.editor;
            MainImageExternalEditorWindow.Closed += OnMainImageExternalEditorWindowOnClosed;
            MainImageExternalEditorWindow.ImageEditor.Saved += MainImageExternalContextSaved;
        }
    }

    public async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(),
            SelectedFile, overwriteExistingFiles, null, StatusContext.ProgressTracker());

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
            (SelectedFile?.FullName ?? string.Empty) != (_loadedFile?.FullName ?? string.Empty);

        var (isValid, explanation) =
            await CommonContentValidation.FileContentFileValidation(SelectedFile, DbEntry?.ContentId);

        SelectedFileHasValidationIssues = !isValid;

        SelectedFileValidationMessage = explanation;

        SelectedFileNameHasInvalidCharacters =
            await CommonContentValidation.FileContentFileFileNameHasInvalidCharacters(SelectedFile, DbEntry?.ContentId);

        DetectGuiFileTypes();
    }

    public void SetupStatusContextAndCommands(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        HelpContext = new HelpDisplayContext(new List<string>
        {
            FileEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        ChooseFileCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile());
        SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true, false));
        SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true, true));
        OpenSelectedFileDirectoryCommand = StatusContext.RunBlockingTaskCommand(OpenSelectedFileDirectory);
        OpenSelectedFileCommand = StatusContext.RunBlockingTaskCommand(OpenSelectedFile);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
        RenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
        AutoRenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await FileHelpers.TryAutoCleanRenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
        AutoRenameSelectedFileBasedOnTitleCommand = StatusContext.RunBlockingTaskCommand(async () =>
        {
            await FileHelpers.TryAutoRenameSelectedFile(SelectedFile, TitleSummarySlugFolder.TitleEntry.UserValue,
                StatusContext, x => SelectedFile = x);
        });
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
            LinkExtraction.ExtractNewAndShowLinkContentEditors($"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}",
                StatusContext.ProgressTracker()));
        SaveAndExtractImageFromPdfCommand = StatusContext.RunBlockingTaskCommand(SaveAndExtractImageFromPdf);
        SaveAndExtractImageFromVideoCommand = StatusContext.RunBlockingTaskCommand(SaveAndExtractImageFromMp4);
        LinkToClipboardCommand = StatusContext.RunNonBlockingTaskCommand(LinkToClipboard);
        DownloadLinkToClipboardCommand = StatusContext.RunNonBlockingTaskCommand(DownloadLinkToClipboard);
        ViewUserMainPictureCommand = StatusContext.RunNonBlockingTaskCommand(ViewUserMainPicture);
        EditUserMainPictureCommand = StatusContext.RunNonBlockingTaskCommand(EditUserMainPicture);
    }

    public RelayCommand AutoRenameSelectedFileBasedOnTitleCommand { get; set; }

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
            Log.Error(e, "Caught exception in FileContentEditorContext while trying to setup the User Main Picture.");
        }
    }

    public async Task TryAddUserMainPicture(Guid? contentId)
    {
        if (contentId == null || contentId == Guid.Empty) return;
        var context = await Db.Context();
        if (context.ImageContents.Any(x => x.ContentId == contentId))
            UserMainPictureEntry.UserText = contentId.Value.ToString();
    }

    private void UserMainPictureEntryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        StatusContext.RunFireAndForgetNonBlockingTask(SetUserMainPicture);
    }

    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            StatusContext.ToastError("Please save the content first...");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.FilePageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

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
            var possibleFile = UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(photoToEdit);

            if (possibleFile is not { Exists: true })
            {
                StatusContext.ToastWarning("No Media File Found?");
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
                StatusContext.ToastWarning("No Media File Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(possibleFile.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
            return;
        }

        StatusContext.ToastWarning("Didn't find the expected Photo/Image to view?");
    }
}