using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.ImageHtml;
using PointlessWaymarksCmsData.JsonFiles;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.ShowInSearchEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ImageContentEditor
{
    public class ImageContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private string _altText;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private ImageContent _dbEntry;
        private Command _extractNewLinksCommand;
        private string _imageSourceNotes;
        private FileInfo _initialImage;
        private Command _linkToClipboardCommand;
        private Command _resizeFileCommand;
        private Command _rotateImageLeftCommand;
        private Command _rotateImageRightCommand;
        private Command _saveAndGenerateHtmlCommand;
        private Command _saveUpdateDatabaseCommand;
        private FileInfo _selectedFile;
        private BitmapSource _selectedFileBitmapSource;
        private string _selectedFileFullPath;
        private ShowInSearchEditorContext _showInSearch;
        private ShowInMainSiteFeedEditorContext _showInSiteFeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;

        public ImageContentEditorContext(StatusControlContext statusContext)
        {
            SetupContextAndCommands(statusContext);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(null));
        }

        public ImageContentEditorContext(StatusControlContext statusContext, FileInfo initialImage)
        {
            SetupContextAndCommands(statusContext);

            if (initialImage != null && initialImage.Exists) _initialImage = initialImage;

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(null));
        }

        public ImageContentEditorContext(StatusControlContext statusContext, ImageContent toLoad)
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

        public string ImageSourceNotes
        {
            get => _imageSourceNotes;
            set
            {
                if (value == _imageSourceNotes) return;
                _imageSourceNotes = value;
                OnPropertyChanged();
            }
        }

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

        public Command ResizeFileCommand
        {
            get => _resizeFileCommand;
            set
            {
                if (Equals(value, _resizeFileCommand)) return;
                _resizeFileCommand = value;
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

        public ShowInSearchEditorContext ShowInSearch
        {
            get => _showInSearch;
            set
            {
                if (Equals(value, _showInSearch)) return;
                _showInSearch = value;
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
                     StringHelper.AreEqual(DbEntry.Title, TitleSummarySlugFolder.Title) &&
                     StringHelper.AreEqual(DbEntry.AltText, AltText) &&
                     StringHelper.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy) &&
                     StringHelper.AreEqual(DbEntry.UpdateNotes, UpdateNotes.UpdateNotes) &&
                     StringHelper.AreEqual(DbEntry.UpdateNotesFormat,
                         UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString) &&
                     StringHelper.AreEqual(DbEntry.OriginalFileName, SelectedFile?.Name ?? string.Empty) &&
                     StringHelper.AreEqual(DbEntry.ImageSourceNotes, ImageSourceNotes) &&
                     DbEntry.ShowInSearch == ShowInSearch.ShowInSearch &&
                     DbEntry.ShowInMainSiteFeed == ShowInSiteFeed.ShowInMainSite && !TagEdit.TagsHaveChanges);
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

            if (!FileTypeHelpers.ImageFileTypeIsSupported(newFile))
            {
                StatusContext.ToastError("Only jpeg files are supported...");
                return;
            }

            SelectedFile = newFile;

            StatusContext.Progress($"Image set - {SelectedFile.FullName}");
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var htmlContext = new SingleImagePage(DbEntry);

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
                CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            };

            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, DbEntry);
            ShowInSiteFeed = new ShowInMainSiteFeedEditorContext(StatusContext, DbEntry, false);
            ShowInSearch = new ShowInSearchEditorContext(StatusContext, DbEntry, true);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            ContentId = new ContentIdViewerControlContext(StatusContext, DbEntry);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))

            {
                PictureResizing.CheckImageOriginalFileIsInMediaAndContentDirectories(DbEntry);

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchiveImageDirectory().FullName,
                    toLoad.OriginalFileName));

                if (archiveFile.Exists)
                    SelectedFile = archiveFile;
                else
                    await StatusContext.ShowMessage("Missing Photo",
                        $"There is an original image file listed for this image - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected location of {archiveFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the image or " +
                        "maybe your master media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?", new List<string> {"OK"});
            }

            ImageSourceNotes = DbEntry.ImageSourceNotes ?? string.Empty;
            AltText = DbEntry.AltText ?? string.Empty;

            if (DbEntry.Id < 1 && _initialImage != null && _initialImage.Exists &&
                FileTypeHelpers.ImageFileTypeIsSupported(_initialImage))
            {
                SelectedFile = _initialImage;
                _initialImage = null;
            }
        }


        private DirectoryInfo LocalContentDirectory(UserSettings settings)
        {
            var imageDirectory =
                new DirectoryInfo(Path.Combine(LocalFolderDirectory(settings).FullName, TitleSummarySlugFolder.Slug));
            if (!imageDirectory.Exists) imageDirectory.Create();

            imageDirectory.Refresh();

            return imageDirectory;
        }

        private DirectoryInfo LocalFolderDirectory(UserSettings settings)
        {
            var folderDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteImageDirectory().FullName,
                TitleSummarySlugFolder.Folder));
            if (!folderDirectory.Exists) folderDirectory.Create();

            folderDirectory.Refresh();

            return folderDirectory;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task ResizeImage()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null)
            {
                StatusContext.ToastError("Can't Resize - No File?");
                return;
            }

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                StatusContext.ToastError("Can't Resize - No File?");
                return;
            }

            PictureResizing.ResizeForDisplayAndSrcset(SelectedFile, true, StatusContext.ProgressTracker());

            DataNotifications.ImageContentDataNotificationEventSource.Raise(this,
                new DataNotificationEventArgs
                {
                    UpdateType = DataNotificationUpdateType.Update, ContentIds = new List<Guid> {DbEntry.ContentId}
                });
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

        public async Task SaveAndGenerateHtml()
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
            var dataNotification = await SaveToDatabase();
            await WriteSelectedFileToLocalSite();
            await GenerateHtml();
            await Export.WriteLocalDbJson(DbEntry);

            if (dataNotification != null)
                DataNotifications.ImageContentDataNotificationEventSource.Raise(this, dataNotification);
        }

        private async Task<DataNotificationEventArgs> SaveToDatabase(bool skipMediaDirectoryCheck = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newEntry = new ImageContent();

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

            newEntry.MainPicture = newEntry.ContentId;
            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.ShowInSearch = ShowInSearch.ShowInSearch;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.Title;
            newEntry.AltText = AltText;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes;
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.ImageSourceNotes = ImageSourceNotes;

            if (DbEntry != null && DbEntry.Id > 0)
                if (DbEntry.Slug != newEntry.Slug || DbEntry.Folder != newEntry.Folder)
                {
                    var settings = UserSettingsSingleton.CurrentSettings();
                    var existingDirectory = settings.LocalSiteImageContentDirectory(DbEntry, false);

                    if (existingDirectory.Exists)
                    {
                        var newDirectory =
                            new DirectoryInfo(settings.LocalSiteImageContentDirectory(newEntry, false).FullName);
                        existingDirectory.MoveTo(settings.LocalSiteImageContentDirectory(newEntry, false).FullName);
                        newDirectory.Refresh();

                        var possibleOldHtmlFile =
                            new FileInfo($"{Path.Combine(newDirectory.FullName, DbEntry.Slug)}.html");
                        if (possibleOldHtmlFile.Exists)
                            possibleOldHtmlFile.MoveTo(settings.LocalSiteImageHtmlFile(newEntry).FullName);
                    }
                }

            await Db.SaveImageContent(newEntry);

            DbEntry = newEntry;

            await LoadData(newEntry, skipMediaDirectoryCheck);

            if (isNewEntry)
                return new DataNotificationEventArgs
                {
                    UpdateType = DataNotificationUpdateType.New, ContentIds = new List<Guid> {newEntry.ContentId}
                };
            return new DataNotificationEventArgs
            {
                UpdateType = DataNotificationUpdateType.Update, ContentIds = new List<Guid> {newEntry.ContentId}
            };
        }

        private async Task SaveToDbWithValidation()
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
            var dataNotification = await SaveToDatabase();

            if (dataNotification != null)
                DataNotifications.ImageContentDataNotificationEventSource.Raise(this, dataNotification);
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

            ChooseFileCommand = new Command(() => StatusContext.RunBlockingTask(async () => await ChooseFile()));
            ResizeFileCommand = new Command(() => StatusContext.RunBlockingTask(ResizeImage));
            SaveAndGenerateHtmlCommand = new Command(() => StatusContext.RunBlockingTask(SaveAndGenerateHtml));
            SaveUpdateDatabaseCommand = new Command(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
            ViewOnSiteCommand = new Command(() => StatusContext.RunBlockingTask(ViewOnSite));
            RotateImageRightCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await RotateImage(Orientation.Rotate90)));
            RotateImageLeftCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await RotateImage(Orientation.Rotate270)));
            ExtractNewLinksCommand = new Command(() => StatusContext.RunBlockingTask(() =>
                LinkExtraction.ExtractNewAndShowLinkStreamEditors(ImageSourceNotes, StatusContext.ProgressTracker())));
            LinkToClipboardCommand = new Command(() => StatusContext.RunBlockingTask(LinkToClipboard));
        }

        private async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile.Refresh();

            if (!SelectedFile.Exists) return (false, "File doesn't exist?");

            if (!FileTypeHelpers.ImageFileTypeIsSupported(SelectedFile))
                return (false, "The file doesn't appear to be a supported file type.");

            if (await (await Db.Context()).ImageFilenameExistsInDatabase(SelectedFile.Name, DbEntry?.ContentId))
                return (false, "This filename already exists in the database - image file names must be unique.");

            if (await (await Db.Context()).SlugExistsInDatabase(TitleSummarySlugFolder.Slug, DbEntry?.ContentId))
                return (false, "This slug already exists in the database - slugs must be unique.");

            return (true, string.Empty);
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return new List<(bool, string)>
            {
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await UserSettingsUtilities.ValidateLocalMasterMediaArchive(),
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

            var url = $@"http://{settings.ImagePageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task WriteSelectedFileToLocalSite()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();

            var targetDirectory = LocalContentDirectory(userSettings);

            var originalFileInTargetDirectoryFullName = Path.Combine(targetDirectory.FullName, SelectedFile.Name);

            var sourceImage = new FileInfo(originalFileInTargetDirectoryFullName);

            if (originalFileInTargetDirectoryFullName != SelectedFile.FullName)
            {
                if (sourceImage.Exists) sourceImage.Delete();
                SelectedFile.CopyTo(originalFileInTargetDirectoryFullName);
                sourceImage.Refresh();
            }

            PictureResizing.ResizeForDisplayAndSrcset(sourceImage, false, StatusContext.ProgressTracker());

            DataNotifications.ImageContentDataNotificationEventSource.Raise(this,
                new DataNotificationEventArgs
                {
                    UpdateType = DataNotificationUpdateType.Update, ContentIds = new List<Guid> {DbEntry.ContentId}
                });
        }

        private async Task WriteSelectedFileToMasterMediaArchive()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMasterMediaArchiveImageDirectory().FullName,
                SelectedFile.Name);
            if (destinationFileName == SelectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            SelectedFile.CopyTo(destinationFileName);
        }
    }
}