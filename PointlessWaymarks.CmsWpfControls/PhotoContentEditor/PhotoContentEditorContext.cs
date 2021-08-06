﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.BoolDataEntry;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ConversionDataEntry;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor
{
    public class PhotoContentEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private StringDataEntryContext _altTextEntry;
        private StringDataEntryContext _apertureEntry;
        private BodyContentEditorContext _bodyContent;
        private StringDataEntryContext _cameraMakeEntry;
        private StringDataEntryContext _cameraModelEntry;
        private Command _chooseFileAndFillMetadataCommand;
        private Command _chooseFileCommand;
        private ContentIdViewerControlContext _contentId;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private PhotoContent _dbEntry;
        private Command _extractNewLinksCommand;
        private StringDataEntryContext _focalLengthEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private HelpDisplayContext _helpContext;
        private FileInfo _initialPhoto;
        private ConversionDataEntryContext<int?> _isoEntry;
        private StringDataEntryContext _lensEntry;
        private StringDataEntryContext _licenseEntry;
        private Command _linkToClipboardCommand;
        private FileInfo _loadedFile;
        private StringDataEntryContext _photoCreatedByEntry;
        private ConversionDataEntryContext<DateTime> _photoCreatedOnEntry;
        private Command _renameSelectedFileCommand;
        private bool _resizeSelectedFile;
        private Command _rotatePhotoLeftCommand;
        private Command _rotatePhotoRightCommand;
        private Command _saveAndCloseCommand;
        private Command _saveAndReprocessPhotoCommand;
        private Command _saveCommand;
        private FileInfo _selectedFile;
        private BitmapSource _selectedFileBitmapSource;
        private bool _selectedFileHasPathOrNameChanges;
        private bool _selectedFileHasValidationIssues;
        private string _selectedFileValidationMessage;
        private BoolDataEntryContext _showInSiteFeed;
        private StringDataEntryContext _shutterSpeedEntry;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private UpdateNotesEditorContext _updateNotes;
        private Command _viewOnSiteCommand;
        private Command _viewPhotoMetadataCommand;
        private Command _viewSelectedFileCommand;

        public EventHandler RequestContentEditorWindowClose;

        private PhotoContentEditorContext(StatusControlContext statusContext)
        {
            SetupContextAndCommands(statusContext);
        }

        public StringDataEntryContext AltTextEntry
        {
            get => _altTextEntry;
            set
            {
                if (Equals(value, _altTextEntry)) return;
                _altTextEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext ApertureEntry
        {
            get => _apertureEntry;
            set
            {
                if (Equals(value, _apertureEntry)) return;
                _apertureEntry = value;
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

        public StringDataEntryContext CameraMakeEntry
        {
            get => _cameraMakeEntry;
            set
            {
                if (Equals(value, _cameraMakeEntry)) return;
                _cameraMakeEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext CameraModelEntry
        {
            get => _cameraModelEntry;
            set
            {
                if (Equals(value, _cameraModelEntry)) return;
                _cameraModelEntry = value;
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

        public StringDataEntryContext FocalLengthEntry
        {
            get => _focalLengthEntry;
            set
            {
                if (Equals(value, _focalLengthEntry)) return;
                _focalLengthEntry = value;
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

        public ConversionDataEntryContext<int?> IsoEntry
        {
            get => _isoEntry;
            set
            {
                if (Equals(value, _isoEntry)) return;
                _isoEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext LensEntry
        {
            get => _lensEntry;
            set
            {
                if (Equals(value, _lensEntry)) return;
                _lensEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext LicenseEntry
        {
            get => _licenseEntry;
            set
            {
                if (Equals(value, _licenseEntry)) return;
                _licenseEntry = value;
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

        public StringDataEntryContext PhotoCreatedByEntry
        {
            get => _photoCreatedByEntry;
            set
            {
                if (Equals(value, _photoCreatedByEntry)) return;
                _photoCreatedByEntry = value;
                OnPropertyChanged();
            }
        }

        public ConversionDataEntryContext<DateTime> PhotoCreatedOnEntry
        {
            get => _photoCreatedOnEntry;
            set
            {
                if (Equals(value, _photoCreatedOnEntry)) return;
                _photoCreatedOnEntry = value;
                OnPropertyChanged();
            }
        }

        public string PhotoEditorHelpText => @"
### Photo Content

Photo Content puts a jpg file together with photo specific data like Aperture, Shutter Speed, ISO, etc. Photo Content is automatically organized into Daily and All galleries.

Photo Content Notes:
 - New Photos created from files have good support for importing metadata - importing a file is generally the best way to create new Photo Content.
 - Photo and Image Content both work with jpg files - main differences include the photo specific data that is stored (aperture, shutter speed, ISO, etc.), Photos are organized into generated Daily Photos pages and Photos
";

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

        public Command SaveAndReprocessPhotoCommand
        {
            get => _saveAndReprocessPhotoCommand;
            set
            {
                if (Equals(value, _saveAndReprocessPhotoCommand)) return;
                _saveAndReprocessPhotoCommand = value;
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

        public StringDataEntryContext ShutterSpeedEntry
        {
            get => _shutterSpeedEntry;
            set
            {
                if (Equals(value, _shutterSpeedEntry)) return;
                _shutterSpeedEntry = value;
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

        public void CheckForChangesAndValidationIssues()
        {
            HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges;
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

            if (!FileHelpers.PhotoFileTypeIsSupported(newFile))
            {
                StatusContext.ToastError("Only JPEGs are supported...");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            SelectedFile = newFile;
            ResizeSelectedFile = true;

            StatusContext.Progress($"Photo load - {SelectedFile.FullName} ");

            if (!loadMetadata) return;

            var (generationReturn, metadata) =
                PhotoGenerator.PhotoMetadataFromFile(SelectedFile, StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                await StatusContext.ShowMessageWithOkButton("Photo Metadata Load Issue",
                    generationReturn.GenerationNote);
                return;
            }

            PhotoMetadataToCurrentContent(metadata);
        }

        public static async Task<PhotoContentEditorContext> CreateInstance(StatusControlContext statusContext)
        {
            var newContext = new PhotoContentEditorContext(statusContext);
            await newContext.LoadData(null);
            return newContext;
        }

        public static async Task<PhotoContentEditorContext> CreateInstance(StatusControlContext statusContext,
            FileInfo initialPhoto)
        {
            var newContext = new PhotoContentEditorContext(statusContext) { StatusContext = { BlockUi = true } };

            if (initialPhoto is { Exists: true }) newContext._initialPhoto = initialPhoto;
            await newContext.LoadData(null);

            newContext.StatusContext.BlockUi = false;

            return newContext;
        }

        public static async Task<PhotoContentEditorContext> CreateInstance(StatusControlContext statusContext,
            PhotoContent toLoad)
        {
            var newContext = new PhotoContentEditorContext(statusContext);
            await newContext.LoadData(toLoad);
            return newContext;
        }

        private PhotoContent CurrentStateToPhotoContent()
        {
            var newEntry = new PhotoContent();

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

            newEntry.MainPicture = newEntry.ContentId;
            newEntry.Aperture = ApertureEntry.UserValue.TrimNullToEmpty();
            newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
            newEntry.Iso = IsoEntry.UserValue;
            newEntry.Lens = LensEntry.UserValue.TrimNullToEmpty();
            newEntry.License = LicenseEntry.UserValue.TrimNullToEmpty();
            newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
            newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
            newEntry.ShowInMainSiteFeed = ShowInSiteFeed.UserValue;
            newEntry.Tags = TagEdit.TagListString();
            newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
            newEntry.AltText = AltTextEntry.UserValue.TrimNullToEmpty();
            newEntry.CameraMake = CameraMakeEntry.UserValue.TrimNullToEmpty();
            newEntry.CameraModel = CameraModelEntry.UserValue.TrimNullToEmpty();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.FocalLength = FocalLengthEntry.UserValue.TrimNullToEmpty();
            newEntry.ShutterSpeed = ShutterSpeedEntry.UserValue.TrimNullToEmpty();
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.OriginalFileName = SelectedFile.Name;
            newEntry.PhotoCreatedBy = PhotoCreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.PhotoCreatedOn = PhotoCreatedOnEntry.UserValue;
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

            var linkString = BracketCodePhotos.Create(DbEntry);

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(linkString);

            StatusContext.ToastSuccess($"To Clipboard: {linkString}");
        }

        public async Task LoadData(PhotoContent toLoad, bool skipMediaDirectoryCheck = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PhotoContent
            {
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice()
            };

            TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            ShowInSiteFeed = BoolDataEntryContext.CreateInstanceForShowInSiteFeed(DbEntry, false);
            ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
            UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
            BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
            LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);

            if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))
            {
                await FileManagement.CheckPhotoFileIsInMediaAndContentDirectories(DbEntry);

                var archiveFile = new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                    toLoad.OriginalFileName));

                if (archiveFile.Exists)
                {
                    _loadedFile = archiveFile;
                    SelectedFile = archiveFile;
                }
                else
                {
                    await StatusContext.ShowMessageWithOkButton("Missing Photo",
                        $"There is an original file listed for this photo - {DbEntry.OriginalFileName} -" +
                        $" but it was not found in the expected location of {archiveFile.FullName} - " +
                        "this will cause an error and prevent you from saving. You can re-load the photo or " +
                        "maybe your media directory moved unexpectedly and you could close this editor " +
                        "and restore it (or change it in settings) before continuing?");
                }
            }

            ApertureEntry = StringDataEntryContext.CreateInstance();
            ApertureEntry.Title = "Aperture";
            ApertureEntry.HelpText =
                "Ratio of the lens focal length to the diameter of the entrance pupil - usually entered in a format like f/8.0";
            ApertureEntry.ReferenceValue = DbEntry.Aperture ?? string.Empty;
            ApertureEntry.UserValue = DbEntry.Aperture.TrimNullToEmpty();

            LensEntry = StringDataEntryContext.CreateInstance();
            LensEntry.Title = "Lens";
            LensEntry.HelpText = "Description and/or identifier for the lens the photograph was taken with.";
            LensEntry.ReferenceValue = DbEntry.Lens ?? string.Empty;
            LensEntry.UserValue = DbEntry.Lens.TrimNullToEmpty();

            LicenseEntry = StringDataEntryContext.CreateInstance();
            LicenseEntry.Title = "License";
            LicenseEntry.HelpText = "The Photo's License";
            LicenseEntry.ReferenceValue = DbEntry.License ?? string.Empty;
            LicenseEntry.UserValue = DbEntry.License.TrimNullToEmpty();

            AltTextEntry = StringDataEntryContext.CreateInstance();
            AltTextEntry.Title = "Alt Text";
            AltTextEntry.HelpText = "A description for the photo, sometimes just the summary will be sufficient...";
            AltTextEntry.ReferenceValue = DbEntry.AltText ?? string.Empty;
            AltTextEntry.UserValue = DbEntry.AltText.TrimNullToEmpty();

            CameraMakeEntry = StringDataEntryContext.CreateInstance();
            CameraMakeEntry.Title = "Camera Make";
            CameraMakeEntry.HelpText = "The Make, or Brand, of the Camera";
            CameraMakeEntry.ReferenceValue = DbEntry.CameraMake ?? string.Empty;
            CameraMakeEntry.UserValue = DbEntry.CameraMake.TrimNullToEmpty();

            CameraModelEntry = StringDataEntryContext.CreateInstance();
            CameraModelEntry.Title = "Camera Model";
            CameraModelEntry.HelpText = "The Camera Model";
            CameraModelEntry.ReferenceValue = DbEntry.CameraModel ?? string.Empty;
            CameraModelEntry.UserValue = DbEntry.CameraModel.TrimNullToEmpty();

            FocalLengthEntry = StringDataEntryContext.CreateInstance();
            FocalLengthEntry.Title = "Focal Length";
            FocalLengthEntry.HelpText = "Usually entered as 50 mm or 110 mm";
            FocalLengthEntry.ReferenceValue = DbEntry.FocalLength ?? string.Empty;
            FocalLengthEntry.UserValue = DbEntry.FocalLength.TrimNullToEmpty();

            ShutterSpeedEntry = StringDataEntryContext.CreateInstance();
            ShutterSpeedEntry.Title = "Shutter Speed";
            ShutterSpeedEntry.HelpText = "Usually entered as 1/250 or 3\"";
            ShutterSpeedEntry.ReferenceValue = DbEntry.ShutterSpeed ?? string.Empty;
            ShutterSpeedEntry.UserValue = DbEntry.ShutterSpeed.TrimNullToEmpty();

            PhotoCreatedByEntry = StringDataEntryContext.CreateInstance();
            PhotoCreatedByEntry.Title = "Photo Created By";
            PhotoCreatedByEntry.HelpText = "Who created the photo";
            PhotoCreatedByEntry.ReferenceValue = DbEntry.PhotoCreatedBy ?? string.Empty;
            PhotoCreatedByEntry.UserValue = DbEntry.PhotoCreatedBy.TrimNullToEmpty();

            IsoEntry = ConversionDataEntryContext<int?>.CreateInstance(ConversionDataEntryHelpers
                .IntNullableConversion);
            IsoEntry.Title = "ISO";
            IsoEntry.HelpText = "A measure of a sensor films sensitivity to light, 100 is a typical value";
            IsoEntry.ReferenceValue = DbEntry.Iso;
            IsoEntry.UserText = DbEntry.Iso?.ToString("F0") ?? string.Empty;

            PhotoCreatedOnEntry =
                ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
            PhotoCreatedOnEntry.Title = "Photo Created On";
            PhotoCreatedOnEntry.HelpText = "Date and, optionally, Time the Photo was Created";
            PhotoCreatedOnEntry.ReferenceValue = DbEntry.PhotoCreatedOn;
            PhotoCreatedOnEntry.UserText = DbEntry.PhotoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");

            if (DbEntry.Id < 1 && _initialPhoto is { Exists: true } &&
                FileHelpers.PhotoFileTypeIsSupported(_initialPhoto))
            {
                SelectedFile = _initialPhoto;
                ResizeSelectedFile = true;
                _initialPhoto = null;
                var (generationReturn, metadataReturn) =
                    PhotoGenerator.PhotoMetadataFromFile(SelectedFile, StatusContext.ProgressTracker());
                if (!generationReturn.HasError) PhotoMetadataToCurrentContent(metadataReturn);
            }

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }

        public void PhotoMetadataToCurrentContent(PhotoMetadata metadata)
        {
            ApertureEntry.UserValue = metadata.Aperture;
            CameraMakeEntry.UserValue = metadata.CameraMake;
            CameraModelEntry.UserValue = metadata.CameraModel;
            FocalLengthEntry.UserValue = metadata.FocalLength;
            IsoEntry.UserText = metadata.Iso?.ToString("F0") ?? string.Empty;
            LensEntry.UserValue = metadata.Lens;
            LicenseEntry.UserValue = metadata.License;
            PhotoCreatedByEntry.UserValue = metadata.PhotoCreatedBy;
            PhotoCreatedOnEntry.UserText = metadata.PhotoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");
            ShutterSpeedEntry.UserValue = metadata.ShutterSpeed;
            TitleSummarySlugFolder.SummaryEntry.UserValue = metadata.Summary;
            TagEdit.Tags = metadata.Tags;
            TitleSummarySlugFolder.TitleEntry.UserValue = metadata.Title;
            TitleSummarySlugFolder.TitleToSlug();
            TitleSummarySlugFolder.FolderEntry.UserValue = metadata.PhotoCreatedOn.Year.ToString("F0");
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

            await MagicScalerImageResizer.Rotate(SelectedFile, rotationType);
            ResizeSelectedFile = true;

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(SelectedFileChanged);
        }

        public async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await PhotoGenerator.SaveAndGenerateHtml(CurrentStateToPhotoContent(),
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
                await CommonContentValidation.PhotoFileValidation(SelectedFile, DbEntry?.ContentId);

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

            ChooseFileAndFillMetadataCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile(true));
            ChooseFileCommand = StatusContext.RunBlockingTaskCommand(async () => await ChooseFile(false));
            SaveCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges));
            SaveAndReprocessPhotoCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges, true));
            ViewPhotoMetadataCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await PhotoMetadataReport.AllPhotoMetadataToHtml(SelectedFile, StatusContext));
            ViewSelectedFileCommand = StatusContext.RunNonBlockingTaskCommand(ViewSelectedFile);
            ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
            RenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
            ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
                LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent.BodyContent,
                    StatusContext.ProgressTracker()));
            RotatePhotoRightCommand =
                StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate90));
            RotatePhotoLeftCommand =
                StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate270));


            HelpContext = new HelpDisplayContext(new List<string>
            {
                PhotoEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
            });
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

            var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }

        private async Task ViewSelectedFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (SelectedFile is not { Exists: true, Directory: { Exists: true } })
            {
                StatusContext.ToastError("No Selected Photo or Selected Photo no longer exists?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
    }
}