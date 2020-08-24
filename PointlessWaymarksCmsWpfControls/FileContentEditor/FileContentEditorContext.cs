using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.HelpDisplay;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileContentEditor
{
    public class FileContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private BodyContentEditorContext _bodyContent;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private FileContent _dbEntry;
        private Command _extractNewLinksCommand;
        private HelpDisplayContext _helpContext;
        private FileInfo _initialFile;
        private FileInfo _loadedFile;
        private Command _openSelectedFileDirectoryCommand;
        private string _pdfToImagePageToExtract = "1";
        private bool _publicDownloadLink = true;
        private bool _publicDownloadLinkHasChanges;
        private Command _renameSelectedFileCommand;
        private Command _saveAndExtractImageFromPdfCommand;
        private Command _saveAndGenerateHtmlCommand;
        private FileInfo _selectedFile;
        private bool _selectedFileHasPathOrNameChanges;
        private bool _selectedFileHasValidationIssues;
        private string _selectedFileValidationMessage;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public FileContentEditorContext(StatusControlContext statusContext)
        {
            SetupStatusContextAndCommands(statusContext);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(null));
        }

        public FileContentEditorContext(StatusControlContext statusContext, FileInfo initialFile)
        {
            if (initialFile != null && initialFile.Exists) _initialFile = initialFile;

            SetupStatusContextAndCommands(statusContext);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(null));
        }

        public FileContentEditorContext(StatusControlContext statusContext, FileContent toLoad)
        {
            SetupStatusContextAndCommands(statusContext);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
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

        public bool PublicDownloadLink
        {
            get => _publicDownloadLink;
            set
            {
                if (value == _publicDownloadLink) return;
                _publicDownloadLink = value;
                OnPropertyChanged();
            }
        }

        public bool PublicDownloadLinkHasChanges
        {
            get => _publicDownloadLinkHasChanges;
            set
            {
                if (value == _publicDownloadLinkHasChanges) return;
                _publicDownloadLinkHasChanges = value;
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

        public Command SaveAndGenerateHtmlCommand
        {
            get => _saveAndGenerateHtmlCommand;
            set
            {
                if (Equals(value, _saveAndGenerateHtmlCommand)) return;
                _saveAndGenerateHtmlCommand = value;
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

                SelectedFileHasPathOrNameChanges = (SelectedFile?.FullName ?? string.Empty) !=
                                                   (_loadedFile?.FullName ?? string.Empty);

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

        public ShowInMainSiteFeedEditorContext ShowInSiteFeed
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

        public bool HasChanges()
        {
            return !(StringHelpers.AreEqual(DbEntry.Folder, TitleSummarySlugFolder.Folder) &&
                     StringHelpers.AreEqual(DbEntry.Slug, TitleSummarySlugFolder.Slug) &&
                     StringHelpers.AreEqual(DbEntry.Summary, TitleSummarySlugFolder.Summary) &&
                     DbEntry.ShowInMainSiteFeed == ShowInSiteFeed.ShowInMainSite && !TagEdit.TagsHaveChanges &&
                     StringHelpers.AreEqual(DbEntry.Title, TitleSummarySlugFolder.Title) &&
                     StringHelpers.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy) &&
                     StringHelpers.AreEqual(DbEntry.UpdateNotes, UpdateNotes.UpdateNotes) &&
                     StringHelpers.AreEqual(DbEntry.UpdateNotesFormat,
                         UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString) &&
                     StringHelpers.AreEqual(DbEntry.BodyContent, BodyContent.BodyContent) &&
                     StringHelpers.AreEqual(DbEntry.BodyContentFormat,
                         BodyContent.BodyContentFormat.SelectedContentFormatAsString) &&
                     StringHelpers.AreEqual(DbEntry.OriginalFileName, SelectedFile?.Name ?? string.Empty) &&
                     DbEntry.PublicDownloadLink == PublicDownloadLink && DbEntry.MainPicture ==
                     BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(BodyContent.BodyContent));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChanges()
        {
            PublicDownloadLinkHasChanges = PublicDownloadLink != (DbEntry?.PublicDownloadLink ?? true);
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

        private FileContent CurrentStateToFileContent()
        {
            var newEntry = new FileContent();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy.TrimNullToEmpty();
            }

            newEntry.Folder = TitleSummarySlugFolder.Folder.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.Slug.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.Summary.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.Title.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.PublicDownloadLink = PublicDownloadLink;

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

            var linkString = BracketCodeFileDownloads.FileDownloadLinkBracketCode(DbEntry);

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

            var linkString = BracketCodeFiles.FileLinkBracketCode(DbEntry);

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
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                PublicDownloadLink = true
            };

            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, DbEntry, UserSettingsSingleton.CurrentSettings().LocalSiteFileDirectory());
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, DbEntry, false);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            ContentId = new ContentIdViewerControlContext(StatusContext, DbEntry);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);
            BodyContent = new BodyContentEditorContext(StatusContext, DbEntry);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                await FileManagement.CheckFileOriginalFileIsInMediaAndContentDirectories(DbEntry,
                    StatusContext.ProgressTracker());

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                    DbEntry.OriginalFileName));

                var fileContentDirectory =
                    UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(toLoad);

                var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, toLoad.OriginalFileName));

                if (archiveFile.Exists)
                {
                    SelectedFile = archiveFile;
                    _loadedFile = archiveFile;
                }
                else
                {
                    await StatusContext.ShowMessage("Missing File",
                        $"There is an original file listed for this entry - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected locations of {archiveFile.FullName} or {contentFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the file or " +
                        "maybe your media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?", new List<string> {"OK"});
                }
            }

            if (DbEntry.Id < 1 && _initialFile != null && _initialFile.Exists)
            {
                SelectedFile = _initialFile;
                _initialFile = null;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges")) CheckForChanges();
        }

        private async Task OpenSelectedFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null || !SelectedFile.Exists || SelectedFile.Directory == null ||
                !SelectedFile.Directory.Exists)
            {
                StatusContext.ToastError("No Selected File or Selected File no longer exists?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(SelectedFile.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task OpenSelectedFileDirectory()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null || !SelectedFile.Exists || SelectedFile.Directory == null ||
                !SelectedFile.Directory.Exists)
            {
                StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(SelectedFile.Directory.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task SaveAndExtractImageFromPdf()
        {
            if (SelectedFile == null || !SelectedFile.Exists || !SelectedFile.Extension.ToLower().Contains("pdf"))
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

            var saveResult = await FileGenerator.SaveAndGenerateHtml(CurrentStateToFileContent(), SelectedFile, true,
                null, StatusContext.ProgressTracker());

            if (saveResult.generationReturn.HasError)
            {
                await StatusContext.ShowMessageWithOkButton("Trouble Saving",
                    $"Trouble saving - you must be able to save before extracting a page - {saveResult.generationReturn.GenerationNote}");
                return;
            }

            await LoadData(saveResult.fileContent);

            await PdfConversion.PdfPageToImageWithPdfToCairo(StatusContext, new List<FileContent> {DbEntry},
                pageNumber);
        }

        public async Task SaveAndGenerateHtml(bool overwriteExistingFiles)
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

            HelpContext = new HelpDisplayContext(FileContentHelpMarkdown.HelpBlock + Environment.NewLine +
                                                 BracketCodeHelpMarkdown.HelpBlock);

            ChooseFileCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile());
            SaveAndGenerateHtmlCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
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

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}