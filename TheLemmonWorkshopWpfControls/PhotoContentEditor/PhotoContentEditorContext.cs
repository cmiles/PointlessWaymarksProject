using GalaSoft.MvvmLight.CommandWpf;
using HtmlTableHelper;
using JetBrains.Annotations;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ContentIdViewer;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.TagsEditor;
using TheLemmonWorkshopWpfControls.TitleSummarySlugEditor;
using TheLemmonWorkshopWpfControls.UpdateNotesEditor;
using TheLemmonWorkshopWpfControls.UpdatesByAndOnDisplay;
using TheLemmonWorkshopWpfControls.Utility;
using TheLemmonWorkshopWpfControls.Utility.PictureHelper02.Controls.ImageLoader;

namespace TheLemmonWorkshopWpfControls.PhotoContentEditor
{
    public class PhotoContentEditorContext : INotifyPropertyChanged
    {
        private string _altText;
        private string _aperture;
        private string _originalFileName;
        private string _cameraMake;
        private string _cameraModel;
        private RelayCommand _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdAndUpdatedByAndOnDisplay;
        private PhotoContent _dbEntry;
        private string _focalLength;
        private int? _iso;
        private string _lens;
        private string _license;
        private string _photoCreatedBy;
        private DateTime _photoCreatedOn;
        private RelayCommand _resizeFileCommand;
        private FileInfo _selectedFile;
        private string _selectedFileFullPath;
        private string _shutterSpeed;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tags;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;

        public PhotoContentEditorContext(StatusControlContext statusContext, PhotoContent toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public string OriginalFileName
        {
            get => _originalFileName;
            set
            {
                if (value == _originalFileName) return;
                _originalFileName = value;
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

        public CreatedAndUpdatedByAndOnDisplayContext CreatedAndUpdatedByAndOnDisplay
        {
            get => _createdAndUpdatedByAndOnDisplay;
            set
            {
                if (Equals(value, _createdAndUpdatedByAndOnDisplay)) return;
                _createdAndUpdatedByAndOnDisplay = value;
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

        public TagsEditorContext Tags
        {
            get => _tags;
            set
            {
                if (Equals(value, _tags)) return;
                _tags = value;
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

        public RelayCommand ViewPhotoMetadataCommand { get; set; }

        public static string ShutterSpeedToHumanReadableString(Rational? toProcess)
        {
            if (toProcess == null) return string.Empty;

            if (toProcess.Value.Numerator < 0)
            {
                return Math.Round(Math.Pow(2, (double) -1 * toProcess.Value.Numerator / toProcess.Value.Denominator), 1)
                    .ToString("N1");
            }

            return
                $"1/{Math.Round(Math.Pow(2, (double) toProcess.Value.Numerator / toProcess.Value.Denominator), 1):N0}";
        }

        public async Task ChooseFile()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();

            if (!(dialog.ShowDialog() ?? false)) return;

            var newFile = new FileInfo(dialog.FileName);

            if (!newFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile = newFile;

            await ProcessSelectedFile();
        }

        public async Task LoadData(PhotoContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PhotoContent();
            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, toLoad);
            CreatedAndUpdatedByAndOnDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            ContentId = new ContentIdViewerControlContext(StatusContext, toLoad);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, toLoad);
            Tags = new TagsEditorContext(StatusContext, toLoad);

            ChooseFileCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ChooseFile));
            ResizeFileCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ResizePhoto));
            ViewPhotoMetadataCommand = new RelayCommand(() => StatusContext.RunBlockingTask(ViewPhotoMetadata));

            SaveAndCreateLocalCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
        }

        private async Task SaveAndCreateLocal()
        {
            var titleSummarySlugFolderValidation = await TitleSummarySlugFolder.Validate();
        }

        public RelayCommand SaveAndCreateLocalCommand { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task ProcessSelectedFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile.Refresh();

            if (!SelectedFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

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
            if (!string.IsNullOrWhiteSpace(isoString))
            {
                Iso = int.Parse(isoString);
            }

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
            Tags.Tags = iptcDirectory?.GetDescription(IptcDirectory.TagKeywords).Replace(";", ",") ?? string.Empty;
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

        private async Task ViewPhotoMetadata()
        {
            if (SelectedFile == null)
            {
                StatusContext.ToastError("No photo...");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

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

            var viewerWindow = new HtmlViewer.HtmlViewerWindow(tagHtml);
            viewerWindow.Show();
        }
    }
}