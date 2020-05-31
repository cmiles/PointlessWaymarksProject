using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsData.JsonFiles;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;
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
        private Command _openSelectedFileDirectoryCommand;
        private string _pdfToImagePageToExtract = "1";
        private bool _publicDownloadLink = true;
        private Command _saveAndCreateLocalCommand;
        private Command _saveUpdateDatabaseCommand;
        private FileInfo _selectedFile;
        private string _selectedFileFullPath;
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

        public Command SaveAndCreateLocalCommand
        {
            get => _saveAndCreateLocalCommand;
            set
            {
                if (Equals(value, _saveAndCreateLocalCommand)) return;
                _saveAndCreateLocalCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndExtractImageFromPdfCommand { get; set; }


        public Command SaveUpdateDatabaseCommand
        {
            get => _saveUpdateDatabaseCommand;
            set
            {
                if (Equals(value, _saveUpdateDatabaseCommand)) return;
                _saveUpdateDatabaseCommand = value;
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

                if (SelectedFile == null) return;
                SelectedFileFullPath = SelectedFile.FullName;
            }
        }

        public string SelectedFileFullPath
        {
            get => _selectedFileFullPath;
            set
            {
                if (value == _selectedFileFullPath) return;
                _selectedFileFullPath = value;
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
            return !(StringHelper.AreEqual(DbEntry.Folder, TitleSummarySlugFolder.Folder) &&
                     StringHelper.AreEqual(DbEntry.Slug, TitleSummarySlugFolder.Slug) &&
                     StringHelper.AreEqual(DbEntry.Summary, TitleSummarySlugFolder.Summary) &&
                     DbEntry.ShowInMainSiteFeed == ShowInSiteFeed.ShowInMainSite && !TagEdit.TagsHaveChanges &&
                     StringHelper.AreEqual(DbEntry.Title, TitleSummarySlugFolder.Title) &&
                     StringHelper.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy) &&
                     StringHelper.AreEqual(DbEntry.UpdateNotes, UpdateNotes.UpdateNotes) &&
                     StringHelper.AreEqual(DbEntry.UpdateNotesFormat,
                         UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString) &&
                     StringHelper.AreEqual(DbEntry.BodyContent, BodyContent.BodyContent) &&
                     StringHelper.AreEqual(DbEntry.BodyContentFormat,
                         BodyContent.BodyContentFormat.SelectedContentFormatAsString) &&
                     StringHelper.AreEqual(DbEntry.OriginalFileName, SelectedFile?.Name ?? string.Empty) &&
                     DbEntry.PublicDownloadLink == PublicDownloadLink && DbEntry.MainPicture ==
                     BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(BodyContent.BodyContent));
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

        private async Task DownloadLinkToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                StatusContext.ToastError("Sorry - please save before getting link...");
                return;
            }

            var linkString = BracketCodeFileDownload.FileDownloadLinkBracketCode(DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(linkString);

            StatusContext.ToastSuccess($"To Clipboard: {linkString}");
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Generating Html...");

            var htmlContext = new SingleFilePage(DbEntry);

            htmlContext.WriteLocalHtml();
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

            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, DbEntry);
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, DbEntry, false);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            ContentId = new ContentIdViewerControlContext(StatusContext, DbEntry);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);
            BodyContent = new BodyContentEditorContext(StatusContext, DbEntry);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                PictureResizing.CheckFileOriginalFileIsInMediaAndContentDirectories(DbEntry);

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchiveFileDirectory().FullName,
                    DbEntry.OriginalFileName));

                var fileContentDirectory =
                    UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(toLoad);

                var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, toLoad.OriginalFileName));

                if (archiveFile.Exists)
                    SelectedFile = archiveFile;
                else
                    await StatusContext.ShowMessage("Missing Photo",
                        $"There is an original file listed for this entry - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected locations of {archiveFile.FullName} or {contentFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the file or " +
                        "maybe your master media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?", new List<string> {"OK"});
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

        private async Task SaveAndCreateLocal()
        {
            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await WriteSelectedFileToMasterMediaArchive();
            await SaveToDatabase();
            await WriteSelectedFileToLocalSite();
            await GenerateHtml();
            await Export.WriteLocalDbJson(DbEntry, StatusContext.ProgressTracker());
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

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveAndCreateLocal();

            await PdfConversion.PdfPageToImageWithPdfToCairo(StatusContext, new List<FileContent> {DbEntry},
                pageNumber);
        }


        private async Task SaveToDatabase(bool skipMediaDirectoryCheck = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Starting File Content Save to Database");

            var newEntry = new FileContent();

            var isNewEntry = false;

            if (DbEntry == null || DbEntry.Id < 1)
            {
                isNewEntry = true;
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
                newEntry.ContentVersion = newEntry.CreatedOn.ToUniversalTime();
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.ContentVersion = newEntry.LastUpdatedOn.Value.ToUniversalTime();
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy;
            }

            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.Tags;
            newEntry.Title = TitleSummarySlugFolder.Title;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes;
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent;
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.PublicDownloadLink = PublicDownloadLink;
            newEntry.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(newEntry.BodyContent);

            if (DbEntry != null && DbEntry.Id > 0)
                if (DbEntry.Slug != newEntry.Slug || DbEntry.Folder != newEntry.Folder)
                {
                    var settings = UserSettingsSingleton.CurrentSettings();
                    var existingDirectory = settings.LocalSiteFileContentDirectory(DbEntry, false);

                    if (existingDirectory.Exists)
                    {
                        var newDirectory =
                            new DirectoryInfo(settings.LocalSiteFileContentDirectory(newEntry, false).FullName);
                        existingDirectory.MoveTo(settings.LocalSiteFileContentDirectory(newEntry, false).FullName);
                        newDirectory.Refresh();

                        var possibleOldHtmlFile =
                            new FileInfo($"{Path.Combine(newDirectory.FullName, DbEntry.Slug)}.html");
                        if (possibleOldHtmlFile.Exists)
                            possibleOldHtmlFile.MoveTo(settings.LocalSiteFileHtmlFile(newEntry).FullName);
                    }
                }

            var context = await Db.Context();

            var toHistoric = await context.FileContents.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            await context.FileContents.AddAsync(newEntry);

            await context.SaveChangesAsync(true);

            DbEntry = newEntry;

            await LoadData(newEntry, skipMediaDirectoryCheck);

            if (isNewEntry)
                DataNotifications.FileContentDataNotificationEventSource.Raise(this,
                    new DataNotificationEventArgs
                    {
                        UpdateType = DataNotificationUpdateType.New,
                        ContentIds = new List<Guid> {newEntry.ContentId}
                    });
            else
                DataNotifications.FileContentDataNotificationEventSource.Raise(this,
                    new DataNotificationEventArgs
                    {
                        UpdateType = DataNotificationUpdateType.Update,
                        ContentIds = new List<Guid> {newEntry.ContentId}
                    });
        }

        private async Task SaveToDbWithValidationAndArchiveMedia()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await WriteSelectedFileToMasterMediaArchive();
            await SaveToDatabase();
        }

        public void SetupStatusContextAndCommands(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            HelpContext = new HelpDisplayContext(FileContentHelpMarkdown.HelpBlock + Environment.NewLine +
                                                 BracketCodeHelpMarkdown.HelpBlock);

            ChooseFileCommand = new Command(() => StatusContext.RunBlockingTask(async () => await ChooseFile()));
            SaveAndCreateLocalCommand = new Command(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand =
                new Command(() => StatusContext.RunBlockingTask(SaveToDbWithValidationAndArchiveMedia));
            OpenSelectedFileDirectoryCommand =
                new Command(() => StatusContext.RunBlockingTask(OpenSelectedFileDirectory));
            OpenSelectedFileCommand = new Command(() => StatusContext.RunBlockingTask(OpenSelectedFile));
            ViewOnSiteCommand = new Command(() => StatusContext.RunBlockingTask(ViewOnSite));
            ExtractNewLinksCommand = new Command(() => StatusContext.RunBlockingTask(() =>
                LinkExtraction.ExtractNewAndShowLinkStreamEditors(
                    $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker())));
            SaveAndExtractImageFromPdfCommand =
                new Command(() => StatusContext.RunBlockingTask(SaveAndExtractImageFromPdf));
            LinkToClipboardCommand = new Command(() => StatusContext.RunNonBlockingTask(LinkToClipboard));
            DownloadLinkToClipboardCommand =
                new Command(() => StatusContext.RunNonBlockingTask(DownloadLinkToClipboard));
        }

        private async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null || !SelectedFile.Exists)
                return (false, "No Selected File?");

            if (await (await Db.Context()).FileFilenameExistsInDatabase(SelectedFile.Name, DbEntry?.ContentId))
                return (false, "This filename already exists in the database - image file names must be unique.");

            if (await (await Db.Context()).SlugExistsInDatabase(TitleSummarySlugFolder.Slug, DbEntry?.ContentId))
                return (false, "This slug already exists in the database - slugs must be unique.");

            return (true, string.Empty);
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Running Validations");

            return new List<(bool, string)>
            {
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await TitleSummarySlugFolder.Validate(),
                await CreatedUpdatedDisplay.Validate(),
                await Validate()
            };
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

        private async Task WriteSelectedFileToLocalSite()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();

            var targetDirectory = userSettings.LocalSiteFileContentDirectory(DbEntry);

            var originalFileInTargetDirectoryFullName = Path.Combine(targetDirectory.FullName, SelectedFile.Name);

            var sourceFile = new FileInfo(originalFileInTargetDirectoryFullName);
            //Todo: Could use some checking here for 'same file'
            if (originalFileInTargetDirectoryFullName != SelectedFile.FullName)
            {
                if (sourceFile.Exists) sourceFile.Delete();
                SelectedFile.CopyTo(originalFileInTargetDirectoryFullName);
                sourceFile.Refresh();
            }
        }


        private async Task WriteSelectedFileToMasterMediaArchive()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null) return;

            StatusContext.Progress("Saving File to Archive");

            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMasterMediaArchiveFileDirectory().FullName,
                SelectedFile.Name);
            if (destinationFileName == SelectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            SelectedFile.CopyTo(destinationFileName);
        }
    }
}