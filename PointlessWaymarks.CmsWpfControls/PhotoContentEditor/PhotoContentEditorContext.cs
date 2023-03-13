using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetTopologySuite.Features;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Point = NetTopologySuite.Geometries.Point;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

public partial class PhotoContentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    [ObservableProperty] private RelayCommand _addFeatureIntersectTagsCommand;
    [ObservableProperty] private StringDataEntryContext _altTextEntry;
    [ObservableProperty] private StringDataEntryContext _apertureEntry;
    [ObservableProperty] private RelayCommand _autoCleanRenameSelectedFileCommand;
    [ObservableProperty] private RelayCommand _autoRenameSelectedFileBasedOnTitleCommand;
    [ObservableProperty] private BodyContentEditorContext _bodyContent;
    [ObservableProperty] private StringDataEntryContext _cameraMakeEntry;
    [ObservableProperty] private StringDataEntryContext _cameraModelEntry;
    [ObservableProperty] private RelayCommand _chooseFileAndFillMetadataCommand;
    [ObservableProperty] private RelayCommand _chooseFileCommand;
    [ObservableProperty] private ContentIdViewerControlContext _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
    [ObservableProperty] private PhotoContent _dbEntry;
    [ObservableProperty] private ConversionDataEntryContext<double?> _elevationEntry;
    [ObservableProperty] private RelayCommand _extractNewLinksCommand;
    [ObservableProperty] private StringDataEntryContext _focalLengthEntry;
    [ObservableProperty] private RelayCommand _getElevationCommand;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext _helpContext;
    [ObservableProperty] private FileInfo _initialPhoto;
    [ObservableProperty] private ConversionDataEntryContext<int?> _isoEntry;
    [ObservableProperty] private ConversionDataEntryContext<double?> _latitudeEntry;
    [ObservableProperty] private StringDataEntryContext _lensEntry;
    [ObservableProperty] private StringDataEntryContext _licenseEntry;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private FileInfo _loadedFile;
    [ObservableProperty] private ConversionDataEntryContext<double?> _longitudeEntry;
    [ObservableProperty] private ContentSiteFeedAndIsDraftContext _mainSiteFeed;
    [ObservableProperty] private StringDataEntryContext _photoCreatedByEntry;
    [ObservableProperty] private ConversionDataEntryContext<DateTime> _photoCreatedOnEntry;
    [ObservableProperty] private ConversionDataEntryContext<DateTime?> _photoCreatedOnUtcEntry;
    [ObservableProperty] private RelayCommand _pointFromPhotoLocationCommand;
    [ObservableProperty] private RelayCommand _renameSelectedFileCommand;
    [ObservableProperty] private bool _resizeSelectedFile;
    [ObservableProperty] private RelayCommand _rotatePhotoLeftCommand;
    [ObservableProperty] private RelayCommand _rotatePhotoRightCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveAndReprocessPhotoCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private FileInfo _selectedFile;
    [ObservableProperty] private BitmapSource _selectedFileBitmapSource;
    [ObservableProperty] private bool _selectedFileHasPathOrNameChanges;
    [ObservableProperty] private bool _selectedFileHasValidationIssues;
    [ObservableProperty] private bool _selectedFileNameHasInvalidCharacters;
    [ObservableProperty] private string _selectedFileValidationMessage;
    [ObservableProperty] private BoolDataEntryContext _showPosition;
    [ObservableProperty] private BoolDataEntryContext _showSizes;
    [ObservableProperty] private StringDataEntryContext _shutterSpeedEntry;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext _tagEdit;
    [ObservableProperty] private TitleSummarySlugEditorContext _titleSummarySlugFolder;
    [ObservableProperty] private UpdateNotesEditorContext _updateNotes;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;
    [ObservableProperty] private RelayCommand _viewPhotoMetadataCommand;
    [ObservableProperty] private RelayCommand _viewSelectedFileCommand;

    public EventHandler? RequestContentEditorWindowClose;

    private PhotoContentEditorContext(StatusControlContext statusContext)
    {
        SetupContextAndCommands(statusContext);

        PropertyChanged += OnPropertyChanged;
    }

    public string PhotoEditorHelpText =>
        @"
### Photo Content

Photo Content puts a jpg file together with photo specific data like Aperture, Shutter Speed, ISO, etc. Photo Content is automatically organized into Daily and All galleries.

Photo Content Notes:
 - New Photos created from files have good support for importing metadata - importing a file is generally the best way to create new Photo Content.
 - Photo and Image Content both work with jpg files - main differences include the photo specific data that is stored (aperture, shutter speed, ISO, etc.), Photos are organized into generated Daily Photos pages and Photos
";

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges;
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              SelectedFileHasValidationIssues;
    }

    private async Task AddFeatureIntersectTags()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var featureToCheck = await FeatureFromPoint();
        if (featureToCheck == null)
        {
            StatusContext.ToastError("No valid Lat/Long to check?");
            return;
        }

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            StatusContext.ToastError(
                "To use this feature the Feature Intersect Settings file must be set in the Site Settings...");
            return;
        }

        var possibleTags = featureToCheck.IntersectionTags(
            UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
            CancellationToken.None, StatusContext.ProgressTracker());

        if (!possibleTags.Any())
        {
            StatusContext.ToastWarning("No tags found...");
            return;
        }

        TagEdit.Tags =
            $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }

    public async Task ChooseFile(bool loadMetadata)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting photo load.");

        var dialog = new VistaOpenFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

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
            await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());

        if (generationReturn.HasError)
        {
            await StatusContext.ShowMessageWithOkButton("Photo Metadata Load Issue", generationReturn.GenerationNote);
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
        newEntry.ShowInMainSiteFeed = MainSiteFeed.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
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
        newEntry.PhotoCreatedOnUtc = PhotoCreatedOnUtcEntry.UserValue;
        newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.ShowPhotoSizes = ShowSizes.UserValue;
        newEntry.Latitude = LatitudeEntry.UserValue;
        newEntry.Longitude = LongitudeEntry.UserValue;
        newEntry.Elevation = ElevationEntry.UserValue;

        return newEntry;
    }

    /// <summary>
    ///     Returns a NTS Feature based on the current Lat/Long - if values are null or invalid
    ///     null is returned.
    /// </summary>
    /// <returns></returns>
    public async Task<IFeature?> FeatureFromPoint()
    {
        if (LatitudeEntry.UserValue == null || LongitudeEntry.UserValue == null) return null;

        var latitudeValidation = await CommonContentValidation.LatitudeValidation(LatitudeEntry.UserValue.Value);
        var longitudeValidation = await CommonContentValidation.LongitudeValidation(LongitudeEntry.UserValue.Value);

        if (!latitudeValidation.Valid || !longitudeValidation.Valid) return null;

        if (ElevationEntry.UserValue is null)
            return new Feature(
                new Point(LongitudeEntry.UserValue.Value, LatitudeEntry.UserValue.Value),
                new AttributesTable());
        return new Feature(
            new Point(LongitudeEntry.UserValue.Value, LatitudeEntry.UserValue.Value, ElevationEntry.UserValue.Value),
            new AttributesTable());
    }

    public async Task GetElevation()
    {
        if (LatitudeEntry.HasValidationIssues || LongitudeEntry.HasValidationIssues)
        {
            StatusContext.ToastError("Lat Long is not valid");
            return;
        }

        if (LatitudeEntry.UserValue == null || LongitudeEntry.UserValue == null)
        {
            StatusContext.ToastError("Lat Long is not set");
            return;
        }

        var possibleElevation = await ElevationGuiHelper.GetElevation(LongitudeEntry.UserValue.Value,
            LongitudeEntry.UserValue.Value, StatusContext);

        if (possibleElevation != null) ElevationEntry.UserText = possibleElevation.Value.ToString("F2");
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

        var created = DateTime.Now;

        DbEntry = toLoad ?? new PhotoContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            FeedOn = created,
            ShowPhotoSizes = UserSettingsSingleton.CurrentSettings().PhotoPagesHaveLinksToPhotoSizesByDefault,
            ShowPhotoPosition = UserSettingsSingleton.CurrentSettings().PhotoPagesShowPositionByDefault
        };

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry, "To File Name",
            AutoRenameSelectedFileBasedOnTitleCommand,
            x => !Path.GetFileNameWithoutExtension(SelectedFile.Name)
                .Equals(SlugTools.CreateSlug(false, x.TitleEntry.UserValue), StringComparison.OrdinalIgnoreCase));
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

        ShowPosition = BoolDataEntryContext.CreateInstance();
        ShowPosition.Title = "Show Photo Position";
        ShowPosition.HelpText = "If enabled the page users be shown a list of all available sizes";
        ShowPosition.ReferenceValue = DbEntry.ShowPhotoPosition;
        ShowPosition.UserValue = DbEntry.ShowPhotoPosition;

        ShowSizes = BoolDataEntryContext.CreateInstance();
        ShowSizes.Title = "Show Photo Sizes";
        ShowSizes.HelpText = "If enabled the page users are shown will have a list of all available sizes";
        ShowSizes.ReferenceValue = DbEntry.ShowPhotoSizes;
        ShowSizes.UserValue = DbEntry.ShowPhotoSizes;

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

        IsoEntry = ConversionDataEntryContext<int?>.CreateInstance(ConversionDataEntryHelpers.IntNullableConversion);
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

        PhotoCreatedOnUtcEntry =
            ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers.DateTimeNullableConversion);
        PhotoCreatedOnUtcEntry.Title = "Photo Created On UTC Date/Time";
        PhotoCreatedOnUtcEntry.HelpText =
            "UTC Date and Time the Photo was Created - the UTC Date Time is not displayed but is used to compare the Photo's Date Time to data like GPX Files/Lines.";
        PhotoCreatedOnUtcEntry.ReferenceValue = DbEntry.PhotoCreatedOnUtc;
        PhotoCreatedOnUtcEntry.UserText = DbEntry.PhotoCreatedOnUtc?.ToString("MM/dd/yyyy h:mm:ss tt");

        LatitudeEntry =
            ConversionDataEntryContext<double?>.CreateInstance(ConversionDataEntryHelpers.DoubleNullableConversion);
        LatitudeEntry.ValidationFunctions = new List<Func<double?, Task<IsValid>>>
        {
            CommonContentValidation.LatitudeValidationWithNullOk
        };
        LatitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .0000001);
        LatitudeEntry.Title = "Latitude";
        LatitudeEntry.HelpText = "In DDD.DDDDDD°";
        LatitudeEntry.ReferenceValue = DbEntry.Latitude;
        LatitudeEntry.UserText = DbEntry.Latitude?.ToString("F6");

        LongitudeEntry =
            ConversionDataEntryContext<double?>.CreateInstance(ConversionDataEntryHelpers.DoubleNullableConversion);
        LongitudeEntry.ValidationFunctions = new List<Func<double?, Task<IsValid>>>
        {
            CommonContentValidation.LongitudeValidationWithNullOk
        };
        LongitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .0000001);
        LongitudeEntry.Title = "Longitude";
        LongitudeEntry.HelpText = "In DDD.DDDDDD°";
        LongitudeEntry.ReferenceValue = DbEntry.Longitude;
        LongitudeEntry.UserText = DbEntry.Longitude?.ToString("F6");

        ElevationEntry =
            ConversionDataEntryContext<double?>.CreateInstance(ConversionDataEntryHelpers.DoubleNullableConversion);
        ElevationEntry.ValidationFunctions = new List<Func<double?, Task<IsValid>>>
        {
            CommonContentValidation.ElevationValidation
        };
        ElevationEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .001);
        ElevationEntry.Title = "Elevation";
        ElevationEntry.HelpText = "Elevation in Feet";
        ElevationEntry.ReferenceValue = DbEntry.Elevation;
        ElevationEntry.UserText = DbEntry.Elevation?.ToString("F2") ?? string.Empty;

        if (DbEntry.Id < 1 && _initialPhoto is { Exists: true } && FileHelpers.PhotoFileTypeIsSupported(_initialPhoto))
        {
            SelectedFile = _initialPhoto;
            ResizeSelectedFile = true;
            _initialPhoto = null;
            var (generationReturn, metadataReturn) =
                await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());
            if (!generationReturn.HasError) PhotoMetadataToCurrentContent(metadataReturn);
        }

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(SelectedFile)) StatusContext.RunFireAndForgetNonBlockingTask(SelectedFileChanged);
    }

    public void PhotoMetadataToCurrentContent(PhotoMetadata metadata)
    {
        ApertureEntry.UserValue = metadata.Aperture;
        CameraMakeEntry.UserValue = metadata.CameraMake;
        CameraModelEntry.UserValue = metadata.CameraModel;
        FocalLengthEntry.UserValue = metadata.FocalLength;
        IsoEntry.UserText = metadata.Iso?.ToString("F0") ?? string.Empty;
        LatitudeEntry.UserText = metadata.Latitude?.ToString("F6") ?? string.Empty;
        LongitudeEntry.UserText = metadata.Longitude?.ToString("F6") ?? string.Empty;
        ElevationEntry.UserText = metadata.Elevation?.ToString("F2") ?? string.Empty;
        LensEntry.UserValue = metadata.Lens;
        LicenseEntry.UserValue = metadata.License;
        PhotoCreatedByEntry.UserValue = metadata.PhotoCreatedBy;
        PhotoCreatedOnEntry.UserText = metadata.PhotoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");
        PhotoCreatedOnUtcEntry.UserText = metadata.PhotoCreatedOnUtc?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
        ShutterSpeedEntry.UserValue = metadata.ShutterSpeed;
        TitleSummarySlugFolder.SummaryEntry.UserValue = metadata.Summary;
        TagEdit.Tags = metadata.Tags;
        TitleSummarySlugFolder.TitleEntry.UserValue = metadata.Title;
        TitleSummarySlugFolder.TitleToSlug();
        TitleSummarySlugFolder.FolderEntry.UserValue = metadata.PhotoCreatedOn.Year.ToString("F0");
    }

    private async Task PointFromPhotoLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (LatitudeEntry.UserValue == null || LongitudeEntry.UserValue == null)
        {
            StatusContext.ToastError("Latitude or Longitude is missing?");
            return;
        }

        var latitudeValidation = await CommonContentValidation.LatitudeValidation(LatitudeEntry.UserValue.Value);
        var longitudeValidation = await CommonContentValidation.LongitudeValidation(LongitudeEntry.UserValue.Value);

        if (!latitudeValidation.Valid || !longitudeValidation.Valid)
        {
            StatusContext.ToastError("Latitude/Longitude is not valid?");
            return;
        }

        var frozenNow = DateTime.Now;

        var newPartialPoint = new PointContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            Latitude = UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            Longitude = UserSettingsSingleton.CurrentSettings().LongitudeDefault,
            CreatedOn = frozenNow,
            FeedOn = frozenNow,
            BodyContent = DbEntry != null && DbEntry.ContentId != Guid.Empty
                ? BracketCodePhotos.Create(DbEntry)
                : string.Empty,
            Title = string.IsNullOrWhiteSpace(TitleSummarySlugFolder.TitleEntry.UserValue)
                ? string.Empty
                : $"Point From {TitleSummarySlugFolder.TitleEntry.UserValue}",
            Tags = TagEdit.TagListString()
        };

        newPartialPoint.Slug = SlugTools.CreateSlug(true, newPartialPoint.Title);

        newPartialPoint.Latitude = LatitudeEntry.UserValue.Value;
        newPartialPoint.Longitude = LongitudeEntry.UserValue.Value;
        newPartialPoint.Elevation = ElevationEntry.UserValue;


        var initialBody = DbEntry != null && DbEntry.ContentId != Guid.Empty
            ? BracketCodePhotos.Create(DbEntry)
            : string.Empty;

        var initialTitle = string.IsNullOrWhiteSpace(TitleSummarySlugFolder.TitleEntry.UserValue)
            ? string.Empty
            : $"Point From {TitleSummarySlugFolder.TitleEntry.UserValue}";

        var pointWindow = await PointContentEditorWindow.CreateInstance(newPartialPoint);

        await pointWindow.PositionWindowAndShowOnUiThread();
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

        await MagicScalerImageResizer.Rotate(SelectedFile, rotationType);
        ResizeSelectedFile = true;

        StatusContext.RunFireAndForgetNonBlockingTask(SelectedFileChanged);
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
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task SelectedFileChanged()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedFileHasPathOrNameChanges =
            (SelectedFile?.FullName ?? string.Empty) != (_loadedFile?.FullName ?? string.Empty);

        var (isValid, explanation) =
            await CommonContentValidation.PhotoFileValidation(SelectedFile, DbEntry?.ContentId);

        TitleSummarySlugFolder?.CheckForChangesToTitleToFunctionStates();

        SelectedFileHasValidationIssues = !isValid;

        SelectedFileValidationMessage = explanation;

        SelectedFileNameHasInvalidCharacters =
            await CommonContentValidation.FileContentFileFileNameHasInvalidCharacters(SelectedFile, DbEntry?.ContentId);

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
        AutoCleanRenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await FileHelpers.TryAutoCleanRenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
        AutoRenameSelectedFileBasedOnTitleCommand = StatusContext.RunBlockingTaskCommand(async () =>
        {
            await FileHelpers.TryAutoRenameSelectedFile(SelectedFile, TitleSummarySlugFolder.TitleEntry.UserValue,
                StatusContext, x => SelectedFile = x);
        });
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
            LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent.BodyContent,
                StatusContext.ProgressTracker()));
        RotatePhotoRightCommand =
            StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate90));
        RotatePhotoLeftCommand =
            StatusContext.RunBlockingTaskCommand(async () => await RotateImage(Orientation.Rotate270));

        PointFromPhotoLocationCommand = StatusContext.RunBlockingTaskCommand(PointFromPhotoLocation);
        AddFeatureIntersectTagsCommand = StatusContext.RunBlockingTaskCommand(AddFeatureIntersectTags);

        GetElevationCommand = StatusContext.RunBlockingTaskCommand(GetElevation);

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

        var url = $@"{settings.PhotoPageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    private async Task ViewSelectedFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            StatusContext.ToastError("No Selected Photo or Selected Photo no longer exists?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}