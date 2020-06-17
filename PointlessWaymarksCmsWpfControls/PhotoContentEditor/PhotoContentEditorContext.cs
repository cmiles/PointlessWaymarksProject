using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;
using HtmlTableHelper;
using JetBrains.Annotations;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.JsonFiles;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsWpfControls.ContentIdViewer;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.ShowInMainSiteFeedEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarksCmsWpfControls.UpdateNotesEditor;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.WpfHtml;
using XmpCore;

namespace PointlessWaymarksCmsWpfControls.PhotoContentEditor
{
    public class PhotoContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private string _altText;
        private string _aperture;
        private string _cameraMake;
        private string _cameraModel;
        private Command _chooseFileAndFillMetadataCommand;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PhotoContent _dbEntry;
        private string _focalLength;
        private FileInfo _initialPhoto;
        private int? _iso;
        private string _lens;
        private string _license;
        private string _photoCreatedBy;
        private DateTime _photoCreatedOn;
        private Command _resizeFileCommand;
        private Command _rotatePhotoLeftCommand;
        private Command _rotatePhotoRightCommand;
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

        public bool HasChanges()
        {
            return !(StringHelper.AreEqual(DbEntry.Aperture, Aperture) &&
                     StringHelper.AreEqual(DbEntry.Folder, TitleSummarySlugFolder.Folder) &&
                     StringHelper.AreEqual(DbEntry.Lens, Lens) && StringHelper.AreEqual(DbEntry.License, License) &&
                     StringHelper.AreEqual(DbEntry.Slug, TitleSummarySlugFolder.Slug) &&
                     StringHelper.AreEqual(DbEntry.Summary, TitleSummarySlugFolder.Summary) &&
                     StringHelper.AreEqual(DbEntry.Title, TitleSummarySlugFolder.Title) &&
                     StringHelper.AreEqual(DbEntry.AltText, AltText) &&
                     StringHelper.AreEqual(DbEntry.CameraMake, CameraMake) &&
                     StringHelper.AreEqual(DbEntry.CameraModel, CameraModel) &&
                     StringHelper.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy) &&
                     StringHelper.AreEqual(DbEntry.FocalLength, FocalLength) &&
                     StringHelper.AreEqual(DbEntry.ShutterSpeed, ShutterSpeed) &&
                     StringHelper.AreEqual(DbEntry.UpdateNotes, UpdateNotes.UpdateNotes) &&
                     StringHelper.AreEqual(DbEntry.UpdateNotesFormat,
                         UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString) &&
                     StringHelper.AreEqual(DbEntry.OriginalFileName, SelectedFile?.Name ?? string.Empty) &&
                     StringHelper.AreEqual(DbEntry.PhotoCreatedBy, PhotoCreatedBy) && DbEntry.Iso == Iso &&
                     DbEntry.PhotoCreatedOn == PhotoCreatedOn &&
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
                StatusContext.ToastError("Only jpegs are supported...");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile = newFile;

            StatusContext.Progress($"Photo load - {SelectedFile.FullName} ");

            if (loadMetadata) await ProcessSelectedFile();
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Photo Content - Generate HTML");

            var htmlContext = new SinglePhotoPage(DbEntry);

            htmlContext.WriteLocalHtml();
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

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                PictureResizing.CheckPhotoOriginalFileIsInMediaAndContentDirectories(DbEntry);

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchivePhotoDirectory().FullName,
                    toLoad.OriginalFileName));

                if (archiveFile.Exists)
                    SelectedFile = archiveFile;
                else
                    await StatusContext.ShowMessage("Missing Photo",
                        $"There is an original file listed for this photo - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected location of {archiveFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the photo or " +
                        "maybe your master media directory moved unexpectedly and you could close this editor " +
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
                await ProcessSelectedFile();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task ProcessSelectedFile()
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
            var gpsDirectory = ImageMetadataReader.ReadMetadata(SelectedFile.FullName).OfType<GpsDirectory>()
                .FirstOrDefault();
            var xmpDirectory = ImageMetadataReader.ReadMetadata(SelectedFile.FullName).OfType<XmpDirectory>()
                .FirstOrDefault();

            PhotoCreatedBy = exifDirectory?.GetDescription(ExifDirectoryBase.TagArtist) ?? string.Empty;

            var createdOn = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
            if (string.IsNullOrWhiteSpace(createdOn))
            {
                var gpsDateTime = DateTime.MinValue;
                if (gpsDirectory?.TryGetGpsDate(out gpsDateTime) ?? false)
                {
                    if (gpsDateTime != DateTime.MinValue) PhotoCreatedOn = gpsDateTime.ToLocalTime();
                }
                else
                {
                    PhotoCreatedOn = DateTime.Now;
                }
            }
            else
            {
                var createdOnParsed = DateTime.TryParseExact(
                    exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal), "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);

                PhotoCreatedOn = createdOnParsed ? parsedDate : DateTime.Now;
            }

            TitleSummarySlugFolder.Folder = PhotoCreatedOn.Year.ToString("F0");

            var isoString = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
            if (!string.IsNullOrWhiteSpace(isoString)) Iso = int.Parse(isoString);

            CameraMake = exifDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
            CameraModel = exifDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
            FocalLength = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength) ?? string.Empty;

            Lens = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagLensModel) ?? string.Empty;

            if (Lens == string.Empty || Lens == "----")
                Lens = xmpDirectory?.XmpMeta?.GetProperty(XmpConstants.NsExifAux, "Lens")?.Value ?? string.Empty;
            if (Lens == string.Empty || Lens == "----")
            {
                Lens = xmpDirectory?.XmpMeta?.GetProperty(XmpConstants.NsCameraraw, "LensProfileName")?.Value ??
                       string.Empty;

                if (Lens.StartsWith("Adobe ("))
                {
                    Lens = Lens.Substring(7, Lens.Length - 7);
                    if (Lens.EndsWith(")")) Lens = Lens.Substring(0, Lens.Length - 1);
                }
            }

            if (Lens == "----") Lens = string.Empty;

            Aperture = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagAperture) ?? string.Empty;
            License = exifDirectory?.GetDescription(ExifDirectoryBase.TagCopyright) ?? string.Empty;

            var shutterValue = new Rational();
            if (exifSubIfDirectory?.TryGetRational(37377, out shutterValue) ?? false)
                ShutterSpeed = ShutterSpeedToHumanReadableString(shutterValue);
            else
                ShutterSpeed = string.Empty;

            //The XMP data - vs the IPTC - will hold the full Title for a very long title (the IPTC will be truncated) - 
            //for a 'from Lightroom with no other concerns' export Title makes the most sense, but there are other possible
            //metadata fields to pull from that could be relevant in other contexts.
            TitleSummarySlugFolder.Title = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "title", 1)?.Value;

            if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.Title))
                TitleSummarySlugFolder.Title =
                    iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;
            //Use a variety of guess on common file names and make that the title - while this could result in an initial title 
            //like DSC001 style out of camera names but after having experimented with loading files I think 'default' is better
            //than an invalid blank.
            if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.Title))
                TitleSummarySlugFolder.Title = Path.GetFileNameWithoutExtension(SelectedFile.Name).Replace("-", " ")
                    .Replace("_", " ").SplitCamelCase();

            TitleSummarySlugFolder.Summary = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;

            //2020/3/22 - This matches a personal naming pattern where pictures 'always' start with 4 digit year 2 digit month
            if (!string.IsNullOrWhiteSpace(TitleSummarySlugFolder.Title))
            {
                if (TitleSummarySlugFolder.Title.StartsWith("2"))
                {
                    var possibleTitleDate =
                        Regex.Match(TitleSummarySlugFolder.Title, @"\A(?<possibleDate>\d\d\d\d[\s-]\d\d[\s-]*).*",
                            RegexOptions.IgnoreCase).Groups["possibleDate"].Value;
                    if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                        try
                        {
                            var tempDate = new DateTime(int.Parse(possibleTitleDate.Substring(0, 4)),
                                int.Parse(possibleTitleDate.Substring(5, 2)), 1);

                            TitleSummarySlugFolder.Summary =
                                $"{TitleSummarySlugFolder.Title.Substring(possibleTitleDate.Length, TitleSummarySlugFolder.Title.Length - possibleTitleDate.Length)}.";
                            TitleSummarySlugFolder.Title =
                                $"{tempDate:yyyy} {tempDate:MMMM} {TitleSummarySlugFolder.Title.Substring(possibleTitleDate.Length, TitleSummarySlugFolder.Title.Length - possibleTitleDate.Length)}";
                            TitleSummarySlugFolder.Folder = $"{tempDate:yyyy}";

                            StatusContext.Progress("Title updated based on 2yyy MM start pattern for file name");
                        }
                        catch
                        {
                            StatusContext.Progress("Did not successfully parse 2yyy MM start pattern for file name");
                        }
                }
                else if (TitleSummarySlugFolder.Title.StartsWith("0") || TitleSummarySlugFolder.Title.StartsWith("1"))
                {
                    try
                    {
                        if (Regex.IsMatch(TitleSummarySlugFolder.Title, @"\A[01]\d\d\d\s.*", RegexOptions.IgnoreCase))
                        {
                            var year = int.Parse(TitleSummarySlugFolder.Title.Substring(0, 2));
                            var month = int.Parse(TitleSummarySlugFolder.Title.Substring(2, 2));

                            var tempDate = year < 20
                                ? new DateTime(2000 + year, month, 1)
                                : new DateTime(1900 + year, month, 1);

                            TitleSummarySlugFolder.Summary =
                                $"{TitleSummarySlugFolder.Title.Substring(5, TitleSummarySlugFolder.Title.Length - 5)}.";
                            TitleSummarySlugFolder.Title =
                                $"{tempDate:yyyy} {tempDate:MMMM} {TitleSummarySlugFolder.Title.Substring(5, TitleSummarySlugFolder.Title.Length - 5)}";
                            TitleSummarySlugFolder.Folder = $"{tempDate:yyyy}";

                            StatusContext.Progress("Title updated based on YYMM start pattern for file name");
                        }
                    }
                    catch
                    {
                        StatusContext.Progress("Did not successfully parse YYMM start pattern for file name");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(TitleSummarySlugFolder.Title))
                TitleSummarySlugFolder.Title = Regex.Replace(TitleSummarySlugFolder.Title, @"\s+", " ").TrimNullSafe();

            //Order is important here - the title supplies the summary in the code above - but overwrite that if there is a 
            //description.
            var description = exifDirectory?.GetDescription(ExifDirectoryBase.TagImageDescription) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(description))
                TitleSummarySlugFolder.Summary = description;

            TitleSummarySlugFolder.Slug = SlugUtility.Create(true, TitleSummarySlugFolder.Title);

            var xmpSubjectKeywordList = new List<string>();

            var xmpSubjectArrayItemCount = xmpDirectory?.XmpMeta?.CountArrayItems(XmpConstants.NsDC, "subject");

            if (xmpSubjectArrayItemCount != null)
                for (var i = 1; i <= xmpSubjectArrayItemCount; i++)
                {
                    var subjectArrayItem = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "subject", i);
                    if (subjectArrayItem == null || string.IsNullOrWhiteSpace(subjectArrayItem.Value)) continue;
                    xmpSubjectKeywordList.AddRange(subjectArrayItem.Value.Replace(";", ",").Split(",")
                        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                }

            xmpSubjectKeywordList = xmpSubjectKeywordList.Distinct().ToList();

            var keywordTagList = new List<string>();

            var keywordValue = iptcDirectory?.GetDescription(IptcDirectory.TagKeywords)?.Replace(";", ",") ??
                               string.Empty;

            if (!string.IsNullOrWhiteSpace(keywordValue))
                keywordTagList.AddRange(keywordValue.Replace(";", ",").Split(",")
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            if (xmpSubjectKeywordList.Count == 0 && keywordTagList.Count == 0)
                TagEdit.Tags = string.Empty;
            else if (xmpSubjectKeywordList.Count >= keywordTagList.Count)
                TagEdit.Tags = string.Join(",", xmpSubjectKeywordList);
            else
                TagEdit.Tags = string.Join(",", keywordTagList);
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

            await WriteSelectedFileToLocalSite(true);
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
            await WriteSelectedFileToLocalSite(false);
            await GenerateHtml();
            await Export.WriteLocalDbJson(DbEntry);

            if (dataNotification != null)
                DataNotifications.ImageContentDataNotificationEventSource.Raise(this, dataNotification);
        }

        private async Task<DataNotificationEventArgs> SaveToDatabase(bool skipMediaDirectoryCheck = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newEntry = new PhotoContent();

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
            newEntry.Aperture = Aperture;
            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Iso = Iso;
            newEntry.Lens = Lens;
            newEntry.License = License;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.ShowInMainSite;
            newEntry.Tags = TagEdit.TagListString();
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

            if (DbEntry != null && DbEntry.Id > 0)
                if (DbEntry.Slug != newEntry.Slug || DbEntry.Folder != newEntry.Folder)
                {
                    var settings = UserSettingsSingleton.CurrentSettings();
                    var existingDirectory = settings.LocalSitePhotoContentDirectory(DbEntry, false);

                    if (existingDirectory.Exists)
                    {
                        var newDirectory =
                            new DirectoryInfo(settings.LocalSitePhotoContentDirectory(newEntry, false).FullName);
                        existingDirectory.MoveTo(settings.LocalSitePhotoContentDirectory(newEntry, false).FullName);
                        newDirectory.Refresh();

                        var possibleOldHtmlFile =
                            new FileInfo($"{Path.Combine(newDirectory.FullName, DbEntry.Slug)}.html");
                        if (possibleOldHtmlFile.Exists)
                            possibleOldHtmlFile.MoveTo(settings.LocalSitePhotoHtmlFile(newEntry).FullName);
                    }
                }

            await Db.SavePhotoContent(newEntry);

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

            ChooseFileAndFillMetadataCommand =
                new Command(() => StatusContext.RunBlockingTask(async () => await ChooseFile(true)));
            ChooseFileCommand = new Command(() => StatusContext.RunBlockingTask(async () => await ChooseFile(false)));
            ResizeFileCommand = new Command(() => StatusContext.RunBlockingTask(ResizePhoto));
            SaveAndGenerateHtmlCommand = new Command(() => StatusContext.RunBlockingTask(SaveAndGenerateHtml));
            ViewPhotoMetadataCommand = new Command(() => StatusContext.RunBlockingTask(ViewPhotoMetadata));
            SaveUpdateDatabaseCommand = new Command(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
            ViewOnSiteCommand = new Command(() => StatusContext.RunBlockingTask(ViewOnSite));
            RotatePhotoRightCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await RotateImage(Orientation.Rotate90)));
            RotatePhotoLeftCommand = new Command(() =>
                StatusContext.RunBlockingTask(async () => await RotateImage(Orientation.Rotate270)));
        }

        public static string ShutterSpeedToHumanReadableString(Rational rational)
        {
            return ShutterSpeedToHumanReadableString((Rational?) rational);
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

            if (!FileTypeHelpers.PhotoFileTypeIsSupported(SelectedFile))
                return (false, "The file doesn't appear to be a supported file type.");

            if (await (await Db.Context()).PhotoFilenameExistsInDatabase(SelectedFile.Name, DbEntry?.ContentId))
                return (false, "This filename already exists in the database - photo file names must be unique.");

            if (await (await Db.Context()).SlugExistsInDatabase(TitleSummarySlugFolder.Slug, DbEntry?.ContentId))
                return (false, "This slug already exists in the database - slugs must be unique.");

            return (true, string.Empty);
        }

        public async Task<List<(bool, string)>> ValidateAll()
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

            var url = $@"http://{settings.PhotoPageUrl(DbEntry)}";

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
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
                }).ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            var xmpDirectory = ImageMetadataReader.ReadMetadata(SelectedFile.FullName).OfType<XmpDirectory>()
                .FirstOrDefault();

            var xmpMetadata = xmpDirectory?.GetXmpProperties().Select(x => new {XmpKey = x.Key, XmpValue = x.Value})
                .ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            await ThreadSwitcher.ResumeForegroundAsync();

            var file = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"PhotoMetadata-{Path.GetFileNameWithoutExtension(SelectedFile.Name)}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

            var htmlString =
                ($"<h1>Metadata Report:</h1><h1>{HttpUtility.HtmlEncode(SelectedFile.FullName)}</h1><br><h1>Metadata - Part 1</h1><br>" +
                 tagHtml + "<br><br><h1>XMP - Part 2</h1><br>" + xmpMetadata)
                .ToHtmlDocumentWithPureCss("Photo Metadata", "body {margin: 12px;}");

            await File.WriteAllTextAsync(file.FullName, htmlString);

            var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }

        private async Task WriteSelectedFileToLocalSite(bool forcedResizeOverwriteExistingFiles)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();

            var targetDirectory = userSettings.LocalSitePhotoContentDirectory(DbEntry);

            var originalFileInTargetDirectoryFullName = Path.Combine(targetDirectory.FullName, SelectedFile.Name);

            var sourceImage = new FileInfo(originalFileInTargetDirectoryFullName);

            if (originalFileInTargetDirectoryFullName != SelectedFile.FullName)
            {
                if (sourceImage.Exists) sourceImage.Delete();
                SelectedFile.CopyTo(originalFileInTargetDirectoryFullName);
                sourceImage.Refresh();
            }

            PictureResizing.CleanDisplayAndSrcSetFilesInPhotoDirectory(DbEntry, true, StatusContext.ProgressTracker());

            PictureResizing.ResizeForDisplayAndSrcset(sourceImage, forcedResizeOverwriteExistingFiles,
                StatusContext.ProgressTracker());

            DataNotifications.PhotoContentDataNotificationEventSource.Raise(this,
                new DataNotificationEventArgs
                {
                    UpdateType = DataNotificationUpdateType.Update, ContentIds = new List<Guid> {DbEntry.ContentId}
                });
        }

        private async Task WriteSelectedFileToMasterMediaArchive()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var userSettings = UserSettingsSingleton.CurrentSettings();
            var destinationFileName = Path.Combine(userSettings.LocalMasterMediaArchivePhotoDirectory().FullName,
                SelectedFile.Name);
            if (destinationFileName == SelectedFile.FullName) return;

            var destinationFile = new FileInfo(destinationFileName);

            if (destinationFile.Exists) destinationFile.Delete();

            SelectedFile.CopyTo(destinationFileName);
        }
    }
}