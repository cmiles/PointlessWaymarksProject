using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.BodyContentEditor;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.PhotoList;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PhotoContentEditor
{
    public class PhotoContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private string _altText;
        private string _aperture;
        private BodyContentEditorContext _bodyContent;
        private string _cameraMake;
        private string _cameraModel;
        private Command _chooseFileAndFillMetadataCommand;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PhotoContent _dbEntry;
        private Command _extractNewLinksCommand;
        private string _focalLength;
        private FileInfo _initialPhoto;
        private int? _iso;
        private string _lens;
        private string _license;
        private string _photoCreatedBy;
        private DateTime _photoCreatedOn;
        private Command _rotatePhotoLeftCommand;
        private Command _rotatePhotoRightCommand;
        private Command _saveAndGenerateHtmlAndCloseCommand;
        private Command _saveAndGenerateHtmlCommand;
        private Command _saveUpdateDatabaseCommand;
        private FileInfo _selectedFile;
        private BitmapSource _selectedFileBitmapSource = ImageConstants.BlankImage;
        private string _selectedFileFullPath;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private string _shutterSpeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;
        private Command _viewPhotoMetadataCommand;
        private Command _viewSelectedFileCommand;

        public EventHandler RequestContentEditorWindowClose;


        public PhotoContentEditorContext(StatusControlContext statusContext, bool skipInitialLoad)
        {
            SetupContextAndCommands(statusContext);

            if (!skipInitialLoad)
                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(null));
        }

        public PhotoContentEditorContext(StatusControlContext statusContext, FileInfo initialPhoto)
        {
            if (initialPhoto != null && initialPhoto.Exists) _initialPhoto = initialPhoto;

            SetupContextAndCommands(statusContext);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(null));
        }

        public PhotoContentEditorContext(StatusControlContext statusContext, PhotoContent toLoad)
        {
            SetupContextAndCommands(statusContext);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
        }

        public string AltText
        {
            get => _altText;
            set
            {
                if (value == _altText) return;
                _altText = value;
                OnPropertyChanged();
            }
        }

        public string Aperture
        {
            get => _aperture;
            set
            {
                if (value == _aperture) return;
                _aperture = value;
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

        public string CameraMake
        {
            get => _cameraMake;
            set
            {
                if (value == _cameraMake) return;
                _cameraMake = value;
                OnPropertyChanged();
            }
        }

        public string CameraModel
        {
            get => _cameraModel;
            set
            {
                if (value == _cameraModel) return;
                _cameraModel = value;
                OnPropertyChanged();
            }
        }

        public Command ChooseFileAndFillMetadataCommand
        {
            get => _chooseFileAndFillMetadataCommand;
            set
            {
                if (Equals(value, _chooseFileAndFillMetadataCommand)) return;
                _chooseFileAndFillMetadataCommand = value;
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

        public PhotoContent DbEntry
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

        public string FocalLength
        {
            get => _focalLength;
            set
            {
                if (value == _focalLength) return;
                _focalLength = value;
                OnPropertyChanged();
            }
        }

        public int? Iso
        {
            get => _iso;
            set
            {
                if (value == _iso) return;
                _iso = value;
                OnPropertyChanged();
            }
        }

        public string Lens
        {
            get => _lens;
            set
            {
                if (value == _lens) return;
                _lens = value;
                OnPropertyChanged();
            }
        }

        public string License
        {
            get => _license;
            set
            {
                if (value == _license) return;
                _license = value;
                OnPropertyChanged();
            }
        }

        public string PhotoCreatedBy
        {
            get => _photoCreatedBy;
            set
            {
                if (value == _photoCreatedBy) return;
                _photoCreatedBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime PhotoCreatedOn
        {
            get => _photoCreatedOn;
            set
            {
                if (value.Equals(_photoCreatedOn)) return;
                _photoCreatedOn = value;
                OnPropertyChanged();
            }
        }

        public Command RotatePhotoLeftCommand
        {
            get => _rotatePhotoLeftCommand;
            set
            {
                if (Equals(value, _rotatePhotoLeftCommand)) return;
                _rotatePhotoLeftCommand = value;
                OnPropertyChanged();
            }
        }

        public Command RotatePhotoRightCommand
        {
            get => _rotatePhotoRightCommand;
            set
            {
                if (Equals(value, _rotatePhotoRightCommand)) return;
                _rotatePhotoRightCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveAndGenerateHtmlAndCloseCommand
        {
            get => _saveAndGenerateHtmlAndCloseCommand;
            set
            {
                if (Equals(value, _saveAndGenerateHtmlAndCloseCommand)) return;
                _saveAndGenerateHtmlAndCloseCommand = value;
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

        public string ShutterSpeed
        {
            get => _shutterSpeed;
            set
            {
                if (value == _shutterSpeed) return;
                _shutterSpeed = value;
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

        public Command ViewPhotoMetadataCommand
        {
            get => _viewPhotoMetadataCommand;
            set
            {
                if (Equals(value, _viewPhotoMetadataCommand)) return;
                _viewPhotoMetadataCommand = value;
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

        public bool HasChanges()
        {
            return !(StringHelpers.AreEqual(DbEntry.Aperture, Aperture) &&
                     StringHelpers.AreEqual(DbEntry.Folder, TitleSummarySlugFolder.Folder) &&
                     StringHelpers.AreEqual(DbEntry.Lens, Lens) && StringHelpers.AreEqual(DbEntry.License, License) &&
                     StringHelpers.AreEqual(DbEntry.Slug, TitleSummarySlugFolder.Slug) &&
                     StringHelpers.AreEqual(DbEntry.Summary, TitleSummarySlugFolder.Summary) &&
                     StringHelpers.AreEqual(DbEntry.Title, TitleSummarySlugFolder.Title) &&
                     StringHelpers.AreEqual(DbEntry.AltText, AltText) &&
                     StringHelpers.AreEqual(DbEntry.CameraMake, CameraMake) &&
                     StringHelpers.AreEqual(DbEntry.CameraModel, CameraModel) &&
                     StringHelpers.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy) &&
                     StringHelpers.AreEqual(DbEntry.FocalLength, FocalLength) &&
                     StringHelpers.AreEqual(DbEntry.ShutterSpeed, ShutterSpeed) &&
                     StringHelpers.AreEqual(DbEntry.UpdateNotes, UpdateNotes.UpdateNotes) &&
                     StringHelpers.AreEqual(DbEntry.UpdateNotesFormat,
                         UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString) &&
                     StringHelpers.AreEqual(DbEntry.OriginalFileName, SelectedFile?.Name ?? string.Empty) &&
                     StringHelpers.AreEqual(DbEntry.PhotoCreatedBy, PhotoCreatedBy) && DbEntry.Iso == Iso &&
                     DbEntry.PhotoCreatedOn == PhotoCreatedOn &&
                     StringHelpers.AreEqual(DbEntry.BodyContent, BodyContent.BodyContent) &&
                     StringHelpers.AreEqual(DbEntry.BodyContentFormat,
                         BodyContent.BodyContentFormat.SelectedContentFormatAsString) &&
                     DbEntry.ShowInMainSiteFeed == ShowInSiteFeed.ShowInMainSite && !TagEdit.TagsHaveChanges);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task ChooseFile(bool loadMetadata)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Starting photo load.");

            var dialog = new VistaOpenFileDialog();

            if (!(dialog.ShowDialog() ?? false)) return;

            var newFile = new FileInfo(dialog.FileName);

            if (!newFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            if (!FileTypeHelpers.PhotoFileTypeIsSupported(newFile))
            {
                StatusContext.ToastError("Only JPEGs are supported...");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile = newFile;

            StatusContext.Progress($"Photo load - {SelectedFile.FullName} ");

            if (!loadMetadata) return;

            var (generationReturn, metadata) =
                await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                await StatusContext.ShowMessageWithOkButton("Photo Metadata Load Issue",
                    generationReturn.GenerationNote);
                return;
            }

            PhotoMetadataToCurrentContent(metadata);
        }

        private PhotoContent CurrentStateToPhotoContent()
        {
            var newEntry = new PhotoContent();

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

            newEntry.MainPicture = newEntry.ContentId;
            newEntry.Aperture = Aperture.TrimNullToEmpty();
            newEntry.Folder = TitleSummarySlugFolder.Folder.TrimNullToEmpty();
            newEntry.Iso = Iso;
            newEntry.Lens = Lens.TrimNullToEmpty();
            newEntry.License = License.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.Slug.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.Summary.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.Title.TrimNullToEmpty();
            newEntry.AltText = AltText.TrimNullToEmpty();
            newEntry.CameraMake = CameraMake.TrimNullToEmpty();
            newEntry.CameraModel = CameraModel.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy.TrimNullToEmpty();
            newEntry.FocalLength = FocalLength.TrimNullToEmpty();
            newEntry.ShutterSpeed = ShutterSpeed.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.PhotoCreatedBy = PhotoCreatedBy.TrimNullToEmpty();
            newEntry.PhotoCreatedOn = PhotoCreatedOn;
            newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            return newEntry;
        }

        public async Task LoadData(PhotoContent toLoad, bool skipMediaDirectoryCheck = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PhotoContent
            {
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
                ShowInMainSiteFeed = false
            };

            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, DbEntry);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, DbEntry, false);
            ContentId = new ContentIdViewerControlContext(StatusContext, DbEntry);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);
            BodyContent = new BodyContentEditorContext(StatusContext, DbEntry);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                await FileManagement.CheckPhotoFileIsInMediaAndContentDirectories(DbEntry,
                    StatusContext.ProgressTracker());

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                    toLoad.OriginalFileName));

                if (archiveFile.Exists)
                    SelectedFile = archiveFile;
                else
                    await StatusContext.ShowMessage("Missing Photo",
                        $"There is an original file listed for this photo - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected location of {archiveFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the photo or " +
                        "maybe your media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?", new List<string> {"OK"});
            }

            Aperture = DbEntry.Aperture ?? string.Empty;
            Iso = DbEntry.Iso;
            Lens = DbEntry.Lens ?? string.Empty;
            License = DbEntry.License ?? string.Empty;
            AltText = DbEntry.AltText ?? string.Empty;
            CameraMake = DbEntry.CameraMake ?? string.Empty;
            CameraModel = DbEntry.CameraModel ?? string.Empty;
            FocalLength = DbEntry.FocalLength ?? string.Empty;
            ShutterSpeed = DbEntry.ShutterSpeed ?? string.Empty;
            PhotoCreatedBy = DbEntry.PhotoCreatedBy ?? string.Empty;
            PhotoCreatedOn = DbEntry.PhotoCreatedOn;

            if (DbEntry.Id < 1 && _initialPhoto != null && _initialPhoto.Exists &&
                FileTypeHelpers.PhotoFileTypeIsSupported(_initialPhoto))
            {
                SelectedFile = _initialPhoto;
                _initialPhoto = null;
                var (generationReturn, metadataReturn) =
                    await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, StatusContext.ProgressTracker());
                if (!generationReturn.HasError) PhotoMetadataToCurrentContent(metadataReturn);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PhotoMetadataToCurrentContent(PhotoMetadata metadata)
        {
            Aperture = metadata.Aperture;
            CameraMake = metadata.CameraMake;
            CameraModel = metadata.CameraModel;
            FocalLength = metadata.FocalLength;
            Iso = metadata.Iso;
            Lens = metadata.Lens;
            License = metadata.License;
            PhotoCreatedBy = metadata.PhotoCreatedBy;
            PhotoCreatedOn = metadata.PhotoCreatedOn;
            ShutterSpeed = metadata.ShutterSpeed;
            TitleSummarySlugFolder.Summary = metadata.Summary;
            TagEdit.Tags = metadata.Tags;
            TitleSummarySlugFolder.Title = metadata.Title;
            TitleSummarySlugFolder.TitleToSlug();
            TitleSummarySlugFolder.Folder = PhotoCreatedOn.Year.ToString("F0");
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

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(SelectedFileChanged);
        }

        public async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await PhotoGenerator.SaveAndGenerateHtml(CurrentStateToPhotoContent(),
                SelectedFile, overwriteExistingFiles, StatusContext.ProgressTracker());

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

        private async Task SaveToDbWithValidation()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await PhotoGenerator.SaveToDb(CurrentStateToPhotoContent(),
                SelectedFile, StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving", generationReturn.GenerationNote);
                return;
            }

            await LoadData(newContent);
        }

        private async Task SelectedFileChanged()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null)
            {
                SelectedFileFullPath = string.Empty;
                SelectedFileBitmapSource = ImageConstants.BlankImage;
                return;
            }

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                SelectedFileFullPath = SelectedFile.FullName;
                SelectedFileBitmapSource = ImageConstants.BlankImage;
                return;
            }

            await using var fileStream = new FileStream(SelectedFile.FullName, FileMode.Open, FileAccess.Read);
            await using var outStream = new MemoryStream();

            var settings = new ProcessImageSettings {Width = 450, JpegQuality = 72};
            MagicImageProcessor.ProcessImage(fileStream, outStream, settings);

            outStream.Position = 0;

            var uiImage = new BitmapImage();
            uiImage.BeginInit();
            uiImage.CacheOption = BitmapCacheOption.OnLoad;
            uiImage.StreamSource = outStream;
            uiImage.EndInit();
            uiImage.Freeze();

            SelectedFileBitmapSource = uiImage;

            SelectedFileFullPath = SelectedFile.FullName;
        }

        public void SetupContextAndCommands(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ChooseFileAndFillMetadataCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile(true));
            ChooseFileCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile(false));
            SaveAndGenerateHtmlCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            SaveAndGenerateHtmlAndCloseCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true, true));
            ViewPhotoMetadataCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await PhotoMetadataReport.AllPhotoMetadataToHtml(SelectedFile, StatusContext));
            SaveUpdateDatabaseCommand = StatusContext.RunBlockingTaskCommand(SaveToDbWithValidation);
            ViewSelectedFileCommand = StatusContext.RunNonBlockingTaskCommand(ViewSelectedFile);
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent.BodyContent,
                    StatusContext.ProgressTracker()));
            RotatePhotoRightCommand =
                StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate90));
            RotatePhotoLeftCommand =
                StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate270));
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

            var url = $@"http://{settings.PhotoPageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task ViewSelectedFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null || !SelectedFile.Exists || SelectedFile.Directory == null ||
                !SelectedFile.Directory.Exists)
            {
                StatusContext.ToastError("No Selected Photo or Selected Photo no longer exists?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(SelectedFile.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}