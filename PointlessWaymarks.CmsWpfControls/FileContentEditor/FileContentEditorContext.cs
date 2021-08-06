using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.BoolDataEntry;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.FileContentEditor
{
    public class FileContentEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private BodyContentEditorContext _bodyContent;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private FileContent _dbEntry;
        private BoolDataEntryContext _embedFile;
        private Command _extractNewLinksCommand;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private HelpDisplayContext _helpContext;
        private FileInfo _initialFile;
        private FileInfo _loadedFile;
        private Command _openSelectedFileDirectoryCommand;
        private string _pdfToImagePageToExtract = "1";
        private BoolDataEntryContext _publicDownloadLink;
        private Command _renameSelectedFileCommand;
        private Command _saveAndCloseCommand;
        private Command _saveAndExtractImageFromPdfCommand;
        private Command _saveCommand;
        private FileInfo _selectedFile;
        private bool _selectedFileHasPathOrNameChanges;
        private bool _selectedFileHasValidationIssues;
        private string _selectedFileValidationMessage;
        private BoolDataEntryContext _showInSiteFeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public EventHandler RequestContentEditorWindowClose;

        private FileContentEditorContext(StatusControlContext statusContext, FileInfo initialFile = null)
        {
            if (initialFile is { Exists: true }) _initialFile = initialFile;

            SetupStatusContextAndCommands(statusContext);
        }

        private FileContentEditorContext(StatusControlContext statusContext)
        {
            SetupStatusContextAndCommands(statusContext);
        }

        public BodyContentEditorContext BodyContent
        {
            get => _bodyContent;
            set
            {
                if (Equals(value, _bodyContent)) return;
                _bodyContent = value;
                OnPropertyChanged();
            }
        }

        public Command ChooseFileCommand
        {
            get => _chooseFileCommand;
            set
            {
                if (Equals(value, _chooseFileCommand)) return;
                _chooseFileCommand = value;
                OnPropertyChanged();
            }
        }

        public ContentIdViewerControlContext ContentId
        {
            get => _contentId;
            set
            {
                if (Equals(value, _contentId)) return;
                _contentId = value;
                OnPropertyChanged();
            }
        }

        public CreatedAndUpdatedByAndOnDisplayContext CreatedUpdatedDisplay
        {
            get => _createdUpdatedDisplay;
            set
            {
                if (Equals(value, _createdUpdatedDisplay)) return;
                _createdUpdatedDisplay = value;
                OnPropertyChanged();
            }
        }

        public FileContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public Command DownloadLinkToClipboardCommand { get; set; }

        public BoolDataEntryContext EmbedFile
        {
            get => _embedFile;
            set
            {
                if (Equals(value, _embedFile)) return;
                _embedFile = value;
                OnPropertyChanged();
            }
        }

        public Command ExtractNewLinksCommand
        {
            get => _extractNewLinksCommand;
            set
            {
                if (Equals(value, _extractNewLinksCommand)) return;
                _extractNewLinksCommand = value;
                OnPropertyChanged();
            }
        }

        public string FileEditorHelpText => @"
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

        public HelpDisplayContext HelpContext
        {
            get => _helpContext;
            set
            {
                if (Equals(value, _helpContext)) return;
                _helpContext = value;
                OnPropertyChanged();
            }
        }

        public Command LinkToClipboardCommand { get; set; }

        public Command OpenSelectedFileCommand { get; set; }

        public Command OpenSelectedFileDirectoryCommand
        {
            get => _openSelectedFileDirectoryCommand;
            set
            {
                if (Equals(value, _openSelectedFileDirectoryCommand)) return;
                _openSelectedFileDirectoryCommand = value;
                OnPropertyChanged();
            }
        }

        public string PdfToImagePageToExtract
        {
            get => _pdfToImagePageToExtract;
            set
            {
                if (value == _pdfToImagePageToExtract) return;
                _pdfToImagePageToExtract = value;
                OnPropertyChanged();
            }
        }

        public BoolDataEntryContext PublicDownloadLink
        {
            get => _publicDownloadLink;
            set
            {
                if (value == _publicDownloadLink) return;
                _publicDownloadLink = value;
                OnPropertyChanged();
            }
        }

        public Command RenameSelectedFileCommand
        {
            get => _renameSelectedFileCommand;
            set
            {
                if (Equals(value, _renameSelectedFileCommand)) return;
                _renameSelectedFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndCloseCommand
        {
            get => _saveAndCloseCommand;
            set
            {
                if (Equals(value, _saveAndCloseCommand)) return;
                _saveAndCloseCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndExtractImageFromPdfCommand
        {
            get => _saveAndExtractImageFromPdfCommand;
            set
            {
                if (Equals(value, _saveAndExtractImageFromPdfCommand)) return;
                _saveAndExtractImageFromPdfCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveCommand
        {
            get => _saveCommand;
            set
            {
                if (Equals(value, _saveCommand)) return;
                _saveCommand = value;
                OnPropertyChanged();
            }
        }

        public FileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (Equals(value, _selectedFile)) return;
                _selectedFile = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(SelectedFileChanged);
            }
        }

        public bool SelectedFileHasPathOrNameChanges
        {
            get => _selectedFileHasPathOrNameChanges;
            set
            {
                if (value == _selectedFileHasPathOrNameChanges) return;
                _selectedFileHasPathOrNameChanges = value;
                OnPropertyChanged();
            }
        }

        public bool SelectedFileHasValidationIssues
        {
            get => _selectedFileHasValidationIssues;
            set
            {
                if (value == _selectedFileHasValidationIssues) return;
                _selectedFileHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string SelectedFileValidationMessage
        {
            get => _selectedFileValidationMessage;
            set
            {
                if (value == _selectedFileValidationMessage) return;
                _selectedFileValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public BoolDataEntryContext ShowInSiteFeed
        {
            get => _showInSiteFeed;
            set
            {
                if (Equals(value, _showInSiteFeed)) return;
                _showInSiteFeed = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public TagsEditorContext TagEdit
        {
            get => _tagEdit;
            set
            {
                if (Equals(value, _tagEdit)) return;
                _tagEdit = value;
                OnPropertyChanged();
            }
        }

        public TitleSummarySlugEditorContext TitleSummarySlugFolder
        {
            get => _titleSummarySlugFolder;
            set
            {
                if (Equals(value, _titleSummarySlugFolder)) return;
                _titleSummarySlugFolder = value;
                OnPropertyChanged();
            }
        }

        public UpdateNotesEditorContext UpdateNotes
        {
            get => _updateNotes;
            set
            {
                if (Equals(value, _updateNotes)) return;
                _updateNotes = value;
                OnPropertyChanged();
            }
        }

        public Command ViewOnSiteCommand
        {
            get => _viewOnSiteCommand;
            set
            {
                if (Equals(value, _viewOnSiteCommand)) return;
                _viewOnSiteCommand = value;
                OnPropertyChanged();
            }
        }

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges ||
                         DbEntry?.MainPicture !=
                         BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(BodyContent?.BodyContent);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                                  SelectedFileHasValidationIssues;
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.UserValue;
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

            return newEntry;
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

            DbEntry = toLoad ?? new FileContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                PublicDownloadLink = true
            };

            PublicDownloadLink = BoolDataEntryContext.CreateInstance();
            PublicDownloadLink.Title = "Show Public Download Link";
            PublicDownloadLink.ReferenceValue = DbEntry.PublicDownloadLink;
            PublicDownloadLink.UserValue = DbEntry.PublicDownloadLink;
            PublicDownloadLink.HelpText =
                "If checked there will be a hyperlink will on the File Content Page to download the content. NOTE! The File" +
                "will be copied into the generated HTML for the site regardless of this setting - this setting is only about " +
                "whether a download link is shown.";
            PublicDownloadLink.PropertyChanged += (sender, args) =>
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

            TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
            ShowInSiteFeed = BoolDataEntryContext.CreateInstanceForShowInSiteFeed(DbEntry, false);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                await FileManagement.CheckFileOriginalFileIsInMediaAndContentDirectories(DbEntry);

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                    DbEntry.OriginalFileName));

                var fileContentDirectory =
                    UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(toLoad);

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
            }

            await SelectedFileChanged();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }

        private async Task OpenSelectedFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile is not { Exists: true, Directory: { Exists: true } })
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

            if (SelectedFile is not { Exists: true, Directory: { Exists: true } })
            {
                StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(SelectedFile.Directory.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }

        private async Task SaveAndExtractImageFromPdf()
        {
            if (SelectedFile is not { Exists: true } || !SelectedFile.Extension.ToLower().Contains("pdf"))
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

            await PdfHelpers.PdfPageToImageWithPdfToCairo(StatusContext, new List<FileContent> { DbEntry }, pageNumber);
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
                RequestContentEditorWindowClose?.Invoke(this, new EventArgs());
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
            SaveAndCloseCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true, true));
            OpenSelectedFileDirectoryCommand = StatusContext.RunBlockingTaskCommand(OpenSelectedFileDirectory);
            OpenSelectedFileCommand = StatusContext.RunBlockingTaskCommand(OpenSelectedFile);
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            RenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(
                    $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker()));
            SaveAndExtractImageFromPdfCommand = StatusContext.RunBlockingTaskCommand(SaveAndExtractImageFromPdf);
            LinkToClipboardCommand = StatusContext.RunNonBlockingTaskCommand(LinkToClipboard);
            DownloadLinkToClipboardCommand = StatusContext.RunNonBlockingTaskCommand(DownloadLinkToClipboard);
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

            var url = $@"http://{settings.FilePageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
    }
}