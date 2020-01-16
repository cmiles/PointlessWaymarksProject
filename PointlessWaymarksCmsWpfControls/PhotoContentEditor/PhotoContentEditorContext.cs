using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using HtmlTableHelper;
using JetBrains.Annotations;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.HtmlViewer;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.PictureHelper02.Controls.ImageLoader;

namespace PointlessWaymarksCmsWpfControls.PhotoContentEditor
{
    public class PhotoContentEditorContext : INotifyPropertyChanged
    {
        private string _altText;
        private string _aperture;
        private string _cameraMake;
        private string _cameraModel;
        private RelayCommand _chooseFileAndFillMetadataCommand;
        private RelayCommand _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PhotoContent _dbEntry;
        private string _focalLength;
        private int? _iso;
        private string _lens;
        private string _license;
        private string _photoCreatedBy;
        private DateTime _photoCreatedOn;
        private RelayCommand _resizeFileCommand;
        private RelayCommand _saveAndGenerateHtmlCommand;
        private RelayCommand _saveUpdateDatabaseCommand;
        private FileInfo _selectedFile;
        private string _selectedFileFullPath;
        private string _shutterSpeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private RelayCommand _viewPhotoMetadataCommand;

        public PhotoContentEditorContext(StatusControlContext statusContext, PhotoContent toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

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

        public RelayCommand ChooseFileAndFillMetadataCommand
        {
            get => _chooseFileAndFillMetadataCommand;
            set
            {
                if (Equals(value, _chooseFileAndFillMetadataCommand)) return;
                _chooseFileAndFillMetadataCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ChooseFileCommand
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

        public RelayCommand ResizeFileCommand
        {
            get => _resizeFileCommand;
            set
            {
                if (Equals(value, _resizeFileCommand)) return;
                _resizeFileCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveAndCreateLocalCommand { get; set; }

        public RelayCommand SaveAndGenerateHtmlCommand
        {
            get => _saveAndGenerateHtmlCommand;
            set
            {
                if (Equals(value, _saveAndGenerateHtmlCommand)) return;
                _saveAndGenerateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveUpdateDatabaseCommand
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

        public RelayCommand ViewPhotoMetadataCommand
        {
            get => _viewPhotoMetadataCommand;
            set
            {
                if (Equals(value, _viewPhotoMetadataCommand)) return;
                _viewPhotoMetadataCommand = value;
                OnPropertyChanged();
            }
        }

        public async Task ChooseFile(bool loadMetadata)
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

            StatusContext.Progress($"Image load - {SelectedFile.FullName} ");

            if (loadMetadata) await ProcessSelectedFile();
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Photo Content - Generate HTML");

            var htmlContext = new SinglePhotoPage(DbEntry);

            htmlContext.WriteLocalHtml();
        }

        public async Task LoadData(PhotoContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PhotoContent();
            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, toLoad);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            ContentId = new ContentIdViewerControlContext(StatusContext, toLoad);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, toLoad);
            TagEdit = new TagsEditorContext(StatusContext, toLoad);

            if (toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                var settings = await UserSettingsUtilities.ReadSettings();
                var archiveFile = new FileInfo(Path.Combine(settings.LocalMasterMediaArchivePhotoDirectory().FullName,
                    toLoad.OriginalFileName));
                if (archiveFile.Exists) SelectedFile = archiveFile;
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

            ChooseFileAndFillMetadataCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(async () => await ChooseFile(true)));
            ChooseFileCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(async () => await ChooseFile(false)));
            ResizeFileCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ResizePhoto));
            SaveAndGenerateHtmlCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveAndGenerateHtml));
            ViewPhotoMetadataCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ViewPhotoMetadata));
            SaveAndCreateLocalCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
        }

        private DirectoryInfo LocalContentDirectory(UserSettings settings)
        {
            var photoDirectory =
                new DirectoryInfo(Path.Combine(LocalFolderDirectory(settings).FullName, TitleSummarySlugFolder.Slug));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        private DirectoryInfo LocalFolderDirectory(UserSettings settings)
        {
            var folderDirectory = new DirectoryInfo(Path.Combine(settings.LocalSitePhotoDirectory().FullName,
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

        private async Task ProcessSelectedFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Starting Metadata Processing");

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            StatusContext.Progress("Getting Directories");

            var exifSubIfDirectory = ImageMetadataReader.ReadMetadata(SelectedFile.FullName)
                .OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var exifDirectory = ImageMetadataReader.ReadMetadata(SelectedFile.FullName).OfType<ExifIfd0Directory>()
                .FirstOrDefault();
            var iptcDirectory = ImageMetadataReader.ReadMetadata(SelectedFile.FullName).OfType<IptcDirectory>()
                .FirstOrDefault();

            PhotoCreatedBy = exifDirectory?.GetDescription(ExifDirectoryBase.TagArtist) ?? string.Empty;
            PhotoCreatedOn =
                DateTime.ParseExact(exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal),
                    "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            TitleSummarySlugFolder.Folder = PhotoCreatedOn.Year.ToString("F0");

            var isoString = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
            if (!string.IsNullOrWhiteSpace(isoString)) Iso = int.Parse(isoString);

            CameraMake = exifDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
            CameraModel = exifDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
            FocalLength = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength) ?? string.Empty;
            Lens = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagLensModel) ?? string.Empty;
            if (Lens == "----") Lens = string.Empty;
            Aperture = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagAperture) ?? string.Empty;
            License = exifDirectory?.GetDescription(ExifDirectoryBase.TagCopyright) ?? string.Empty;
            ShutterSpeed = ShutterSpeedToHumanReadableString(exifSubIfDirectory?.GetRational(37377));
            TitleSummarySlugFolder.Title = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;
            TitleSummarySlugFolder.Slug = Slug.Create(true, TitleSummarySlugFolder.Title);
            TagEdit.Tags = iptcDirectory?.GetDescription(IptcDirectory.TagKeywords).Replace(";", ",") ?? string.Empty;
        }

        private async Task ResizePhoto()
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

            ImageResizing.ResizeForDisplayAndSrcset(SelectedFile, StatusContext.ProgressTracker());
        }

        private async Task SaveAndCreateLocal()
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

            await SaveToDatabase();
            await WriteSelectedFileToMasterMediaArchive();
            await WriteSelectedFileToLocalSite();
            await GenerateHtml();
            await WriteLocalDbJson();
        }

        public async Task SaveAndGenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            await SaveToDatabase();
            await WriteSelectedFileToMasterMediaArchive();
            await GenerateHtml();
            await WriteLocalDbJson();
        }

        private async Task SaveToDatabase()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

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
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy;
            }

            newEntry.Aperture = Aperture;
            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Iso = Iso;
            newEntry.Lens = Lens;
            newEntry.License = License;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.Tags = TagEdit.Tags;
            newEntry.Title = TitleSummarySlugFolder.Title;
            newEntry.AltText = AltText;
            newEntry.CameraMake = CameraMake;
            newEntry.CameraModel = CameraModel;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.FocalLength = FocalLength;
            newEntry.ShutterSpeed = ShutterSpeed;
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes;
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.PhotoCreatedBy = PhotoCreatedBy;
            newEntry.PhotoCreatedOn = PhotoCreatedOn;

            var context = await Db.Context();

            var toHistoric = await context.PhotoContents.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPhotoContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricPhotoContents.AddAsync(newHistoric);
                context.PhotoContents.Remove(loopToHistoric);
            }

            context.PhotoContents.Add(newEntry);

            await context.SaveChangesAsync(true);

            DbEntry = newEntry;

            await LoadData(newEntry);
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

            await SaveToDatabase();
            await WriteSelectedFileToMasterMediaArchive();
        }

        public static string ShutterSpeedToHumanReadableString(Rational? toProcess)
        {
            if (toProcess == null) return string.Empty;

            if (toProcess.Value.Numerator < 0)
                return Math.Round(Math.Pow(2, (double) -1 * toProcess.Value.Numerator / toProcess.Value.Denominator), 1)
                    .ToString("N1");

            return
                $"1/{Math.Round(Math.Pow(2, (double) toProcess.Value.Numerator / toProcess.Value.Denominator), 1):N0}";
        }

        private async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile.Refresh();

            if (!SelectedFile.Exists) return (false, "File doesn't exist?");

            if (!(SelectedFile.Extension.ToLower().Contains("jpg") ||
                  SelectedFile.Extension.ToLower().Contains("jpeg")))
                return (false, "The file doesn't appear to be a supported file type.");

            if (await (await Db.Context()).PhotoFilenameExistsInDatabase(SelectedFile.Name, DbEntry?.ContentId))
                return (false, "This filename already exists in the database - photo file names must be unique.");

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

        private async Task ViewPhotoMetadata()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile == null)
            {
                StatusContext.ToastError("No photo...");
                return;
            }

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist.");
                return;
            }

            var photoMetaTags = ImageMetadataReader.ReadMetadata(SelectedFile.FullName);

            var tagHtml = photoMetaTags.SelectMany(x => x.Tags).OrderBy(x => x.DirectoryName).ThenBy(x => x.Name)
                .ToList().Select(x => new
                {
                    DataType = x.Type.ToString(),
                    x.DirectoryName,
                    Tag = x.Name,
                    TagValue = ObjectDumper.Dump(x.Description)
                }).ToHtmlTable();

            await ThreadSwitcher.ResumeForegroundAsync();

            var viewerWindow = new HtmlViewerWindow(tagHtml);
            viewerWindow.Show();
        }

        private async Task WriteLocalDbJson()
        {
            var settings = await UserSettingsUtilities.ReadSettings();
            var db = await Db.Context();
            var jsonDbEntry = JsonSerializer.Serialize(DbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPhotoContents.Where(x => x.ContentId == DbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePhotoContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}-Historic.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        private async Task WriteSelectedFileToLocalSite()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = await UserSettingsUtilities.ReadSettings();

            var targetDirectory = LocalContentDirectory(userSettings);

            var originalFileInTargetDirectoryFullName = Path.Combine(targetDirectory.FullName, SelectedFile.Name);

            var sourceImage = new FileInfo(originalFileInTargetDirectoryFullName);

            if (originalFileInTargetDirectoryFullName != SelectedFile.FullName)
            {
                if (sourceImage.Exists) sourceImage.Delete();
                SelectedFile.CopyTo(originalFileInTargetDirectoryFullName);
                sourceImage.Refresh();
            }

            ImageResizing.ResizeForDisplayAndSrcset(sourceImage, StatusContext.ProgressTracker());
        }

        private async Task WriteSelectedFileToMasterMediaArchive()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = await UserSettingsUtilities.ReadSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMasterMediaArchivePhotoDirectory().FullName,
                SelectedFile.Name);
            if (destinationFileName == SelectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            SelectedFile.CopyTo(destinationFileName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}