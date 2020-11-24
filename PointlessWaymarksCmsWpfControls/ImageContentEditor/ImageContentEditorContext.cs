using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.BoolDataEntry;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.StringDataEntry;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.ImageContentEditor
{
    public class ImageContentEditorContext : INotifyPropertyChanged, IHasChanges
    {
        private StringDataEntryContext _altText;
        private BodyContentEditorContext _bodyContent;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private ImageContent _dbEntry;
        private Command _extractNewLinksCommand;
        private FileInfo _initialImage;
        private Command _linkToClipboardCommand;
        private FileInfo _loadedFile;
        private Command _renameSelectedFileCommand;
        private bool _resizeSelectedFile;
        private Command _rotateImageLeftCommand;
        private Command _rotateImageRightCommand;
        private Command _saveAndCloseCommand;
        private Command _saveAndReprocessImageCommand;
        private Command _saveCommand;
        private FileInfo _selectedFile;
        private BitmapSource _selectedFileBitmapSource;
        private bool _selectedFileHasPathOrNameChanges;
        private bool _selectedFileHasValidationIssues;
        private string _selectedFileValidationMessage;
        private BoolDataEntryContext _showInSearch;
        private BoolDataEntryContext _showInSiteFeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;
        private Command _viewSelectedFileCommand;

        public EventHandler RequestContentEditorWindowClose;

        private ImageContentEditorContext(StatusControlContext statusContext)
        {
            SetupContextAndCommands(statusContext);
        }

        public StringDataEntryContext AltTextEntry
        {
            get => _altText;
            set
            {
                if (Equals(value, _altText)) return;
                _altText = value;
                OnPropertyChanged();
            }
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

        public ImageContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
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

        public bool HasChanges => PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges;

        public Command LinkToClipboardCommand
        {
            get => _linkToClipboardCommand;
            set
            {
                if (Equals(value, _linkToClipboardCommand)) return;
                _linkToClipboardCommand = value;
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

        public bool ResizeSelectedFile
        {
            get => _resizeSelectedFile;
            set
            {
                if (value == _resizeSelectedFile) return;
                _resizeSelectedFile = value;
                OnPropertyChanged();
            }
        }

        public Command RotateImageLeftCommand
        {
            get => _rotateImageLeftCommand;
            set
            {
                if (Equals(value, _rotateImageLeftCommand)) return;
                _rotateImageLeftCommand = value;
                OnPropertyChanged();
            }
        }

        public Command RotateImageRightCommand
        {
            get => _rotateImageRightCommand;
            set
            {
                if (Equals(value, _rotateImageRightCommand)) return;
                _rotateImageRightCommand = value;
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

        public Command SaveAndReprocessImageCommand
        {
            get => _saveAndReprocessImageCommand;
            set
            {
                if (Equals(value, _saveAndReprocessImageCommand)) return;
                _saveAndReprocessImageCommand = value;
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

        public BitmapSource SelectedFileBitmapSource
        {
            get => _selectedFileBitmapSource;
            set
            {
                if (Equals(value, _selectedFileBitmapSource)) return;
                _selectedFileBitmapSource = value;
                OnPropertyChanged();
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

        public BoolDataEntryContext ShowInSearch
        {
            get => _showInSearch;
            set
            {
                if (Equals(value, _showInSearch)) return;
                _showInSearch = value;
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

        public Command ViewSelectedFileCommand
        {
            get => _viewSelectedFileCommand;
            set
            {
                if (Equals(value, _viewSelectedFileCommand)) return;
                _viewSelectedFileCommand = value;
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

            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!newFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            if (!FileHelpers.ImageFileTypeIsSupported(newFile))
            {
                StatusContext.ToastError("Only jpeg files are supported...");
                return;
            }

            SelectedFile = newFile;
            ResizeSelectedFile = true;

            StatusContext.Progress($"Image set - {SelectedFile.FullName}");
        }

        public static async Task<ImageContentEditorContext> CreateInstance(StatusControlContext statusContext,
            ImageContent contentToLoad = null, FileInfo initialImage = null)
        {
            var newContext = new ImageContentEditorContext(statusContext);
            if (initialImage != null && initialImage.Exists) newContext._initialImage = initialImage;
            await newContext.LoadData(contentToLoad);
            return newContext;
        }

        private ImageContent CurrentStateToPhotoContent()
        {
            var newEntry = new ImageContent();

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
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.MainPicture = newEntry.ContentId;
            newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.UserValue;
            newEntry.ShowInSearch = ShowInSearch.UserValue;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
            newEntry.AltText = AltTextEntry.UserValue.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            return newEntry;
        }

        private async Task LinkToClipboard()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                StatusContext.ToastError("Sorry - please save before getting link...");
                return;
            }

            var linkString = BracketCodeImages.ImageBracketCode(DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(linkString);

            StatusContext.ToastSuccess($"To Clipboard: {linkString}");
        }


        private async Task LoadData(ImageContent toLoad, bool skipMediaDirectoryCheck = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new ImageContent
            {
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            };

            TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
            ShowInSiteFeed = BoolDataEntryContext.CreateInstanceForShowInSiteFeed(DbEntry, false);
            ShowInSearch = BoolDataEntryContext.CreateInstanceForShowInSearch(DbEntry, true);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))

            {
                await FileManagement.CheckImageFileIsInMediaAndContentDirectories(DbEntry,
                    StatusContext.ProgressTracker());

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
                    toLoad.OriginalFileName));

                if (archiveFile.Exists)
                {
                    _loadedFile = archiveFile;
                    SelectedFile = archiveFile;
                }
                else
                {
                    await StatusContext.ShowMessage("Missing Photo",
                        $"There is an original image file listed for this image - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected location of {archiveFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the image or " +
                        "maybe your media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?", new List<string> {"OK"});
                }
            }

            AltTextEntry = StringDataEntryContext.CreateInstance();
            AltTextEntry.Title = "Alt Text";
            AltTextEntry.HelpText =
                "A short text description of the image - in some cases the Summary may be all that is needed.";
            AltTextEntry.ReferenceValue = DbEntry.AltText ?? string.Empty;
            AltTextEntry.UserValue = DbEntry.AltText.TrimNullToEmpty();

            if (DbEntry.Id < 1 && _initialImage != null && _initialImage.Exists &&
                FileHelpers.ImageFileTypeIsSupported(_initialImage))
            {
                SelectedFile = _initialImage;
                ResizeSelectedFile = true;
                _initialImage = null;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task RotateImage(Orientation rotationType)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null)
            {
                StatusContext.ToastError("No File Selected?");
                return;
            }

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                StatusContext.ToastError("File doesn't appear to exist?");
                return;
            }

            var rotate = new MagicScalerImageResizer();

            rotate.Rotate(SelectedFile, rotationType);
            ResizeSelectedFile = true;

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(SelectedFileChanged);
        }

        private async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await ImageGenerator.SaveAndGenerateHtml(CurrentStateToPhotoContent(),
                SelectedFile, overwriteExistingFiles, null, StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving", generationReturn.GenerationNote);
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
                await CommonContentValidation.ImageFileValidation(SelectedFile, DbEntry?.ContentId);

            SelectedFileHasValidationIssues = !isValid;

            SelectedFileValidationMessage = explanation;

            if (SelectedFile == null)
            {
                SelectedFileBitmapSource = ImageHelpers.BlankImage;
                return;
            }

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                SelectedFileBitmapSource = ImageHelpers.BlankImage;
                return;
            }

            SelectedFileBitmapSource = await ImageHelpers.InMemoryThumbnailFromFile(SelectedFile, 450, 72);
        }

        public void SetupContextAndCommands(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ChooseFileCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile());
            SaveCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges, false));
            SaveAndReprocessImageCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true, false));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges, true));
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ViewSelectedFileCommand = StatusContext.RunNonBlockingTaskCommand(ViewSelectedFile);
            RenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
            RotateImageRightCommand =
                StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate90));
            RotateImageLeftCommand =
                StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate270));
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent.BodyContent,
                    StatusContext.ProgressTracker()));
            LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);
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

            var url = $@"http://{settings.ImagePageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task ViewSelectedFile()
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
    }
}