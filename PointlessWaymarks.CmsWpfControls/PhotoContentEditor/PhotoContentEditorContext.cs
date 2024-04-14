using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.ImageHelpers;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PhotoContentEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;
    
    private PhotoContentEditorContext(StatusControlContext statusContext, PhotoContent dbEntry)
    {
        StatusContext = statusContext;
        
        BuildCommands();
        
        DbEntry = dbEntry;
        
        PropertyChanged += OnPropertyChanged;
    }
    
    public StringDataEntryContext? AltTextEntry { get; set; }
    public StringDataEntryContext? ApertureEntry { get; set; }
    public BodyContentEditorContext? BodyContent { get; set; }
    public StringDataEntryContext? CameraMakeEntry { get; set; }
    public StringDataEntryContext? CameraModelEntry { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public PhotoContent DbEntry { get; set; }
    public StringDataEntryContext? FocalLengthEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public FileInfo? InitialPhoto { get; set; }
    public ConversionDataEntryContext<int?>? IsoEntry { get; set; }
    public StringDataEntryContext? LensEntry { get; set; }
    public StringDataEntryContext? LicenseEntry { get; set; }
    public FileInfo? LoadedFile { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public OptionalLocationEntryContext? OptionalLocationEntry { get; set; }
    public StringDataEntryContext? PhotoCreatedByEntry { get; set; }
    public ConversionDataEntryContext<DateTime>? PhotoCreatedOnEntry { get; set; }
    public ConversionDataEntryContext<DateTime?>? PhotoCreatedOnUtcEntry { get; set; }
    
    public string PhotoEditorHelpText =>
        @"
### Photo Content

Photo Content puts a jpg file together with photo specific data like Aperture, Shutter Speed, ISO, etc. Photo Content is automatically organized into Daily and All galleries.

Photo Content Notes:
 - New Photos created from files have good support for importing metadata - importing a file is generally the best way to create new Photo Content.
 - Photo and Image Content both work with jpg files - main differences include the photo specific data that is stored (aperture, shutter speed, ISO, etc.), Photos are organized into generated Daily Photos pages and Photos
";
    
    public bool ResizeSelectedFile { get; set; }
    public FileInfo? SelectedFile { get; set; }
    public BitmapSource? SelectedFileBitmapSource { get; set; }
    public bool SelectedFileHasPathOrNameChanges { get; set; }
    public bool SelectedFileHasValidationIssues { get; set; }
    public bool SelectedFileNameHasInvalidCharacters { get; set; }
    public string SelectedFileValidationMessage { get; set; } = string.Empty;
    public BoolDataEntryContext? ShowSizesEntry { get; set; }
    public StringDataEntryContext? ShutterSpeedEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    
    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges;
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              SelectedFileHasValidationIssues;
    }
    
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    
    [BlockingCommand]
    private async Task AddFeatureIntersectTags()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var possibleTags = await OptionalLocationEntry!.GetFeatureIntersectTagsWithUiAlerts();
        
        if (possibleTags.Any())
            TagEdit!.Tags =
                $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }
    
    [BlockingCommand]
    public async Task AutoCleanRenameSelectedFile()
    {
        await FileHelpers.TryAutoCleanRenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x);
    }
    
    [BlockingCommand]
    public async Task AutoRenameSelectedFileBasedOnTitle()
    {
        await FileHelpers.TryAutoRenameSelectedFile(SelectedFile, TitleSummarySlugFolder!.TitleEntry.UserValue,
            StatusContext, x => SelectedFile = x);
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
        
        if (!PhotoGenerator.PhotoFileTypeIsSupported(newFile))
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
        
        if (generationReturn.HasError || metadata == null)
        {
            await StatusContext.ShowMessageWithOkButton("Photo Metadata Load Issue", generationReturn.GenerationNote);
            return;
        }
        
        await PhotoMetadataToCurrentContent(metadata);
    }
    
    [BlockingCommand]
    public async Task ChooseFileAndFillMetadata()
    {
        await ChooseFile(true);
    }
    
    [BlockingCommand]
    public async Task ChooseFileWithoutMetadataLoad()
    {
        await ChooseFile(false);
    }
    
    public static async Task<PhotoContentEditorContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var newContext =
            new PhotoContentEditorContext(statusContext ?? new StatusControlContext(), PhotoContent.CreateInstance());
        await newContext.LoadData(null);
        return newContext;
    }
    
    public static async Task<PhotoContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        FileInfo initialPhoto)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var newContext =
            new PhotoContentEditorContext(statusContext ?? new StatusControlContext(), PhotoContent.CreateInstance())
                { StatusContext = { BlockUi = true } };
        
        if (initialPhoto is { Exists: true }) newContext.InitialPhoto = initialPhoto;
        await newContext.LoadData(null);
        
        newContext.StatusContext.BlockUi = false;
        
        return newContext;
    }
    
    public static async Task<PhotoContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        PhotoContent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var newContext =
            new PhotoContentEditorContext(statusContext ?? new StatusControlContext(), PhotoContent.CreateInstance());
        await newContext.LoadData(toLoad);
        return newContext;
    }
    
    private PhotoContent CurrentStateToPhotoContent()
    {
        var newEntry = PhotoContent.CreateInstance();
        
        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }
        
        newEntry.MainPicture = newEntry.ContentId;
        newEntry.Aperture = ApertureEntry!.UserValue.TrimNullToEmpty();
        newEntry.Folder = TitleSummarySlugFolder!.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Iso = IsoEntry!.UserValue;
        newEntry.Lens = LensEntry!.UserValue.TrimNullToEmpty();
        newEntry.License = LicenseEntry!.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed!.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.Tags = TagEdit!.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.AltText = AltTextEntry!.UserValue.TrimNullToEmpty();
        newEntry.CameraMake = CameraMakeEntry!.UserValue.TrimNullToEmpty();
        newEntry.CameraModel = CameraModelEntry!.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.FocalLength = FocalLengthEntry!.UserValue.TrimNullToEmpty();
        newEntry.ShutterSpeed = ShutterSpeedEntry!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.OriginalFileName = SelectedFile?.Name;
        newEntry.PhotoCreatedBy = PhotoCreatedByEntry!.UserValue.TrimNullToEmpty();
        newEntry.PhotoCreatedOn = PhotoCreatedOnEntry!.UserValue;
        newEntry.PhotoCreatedOnUtc = PhotoCreatedOnUtcEntry!.UserValue;
        newEntry.BodyContent = BodyContent!.UserValue.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.ShowPhotoSizes = ShowSizesEntry!.UserValue;
        newEntry.Latitude = OptionalLocationEntry!.LatitudeEntry!.UserValue;
        newEntry.Longitude = OptionalLocationEntry.LongitudeEntry!.UserValue;
        newEntry.Elevation = OptionalLocationEntry.ElevationEntry!.UserValue;
        newEntry.ShowLocation = OptionalLocationEntry.ShowLocationEntry!.UserValue;
        
        return newEntry;
    }
    
    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(BodyContent!.UserValue,
            StatusContext.ProgressTracker());
    }
    
    [BlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }
        
        var linkString = BracketCodePhotos.Create(DbEntry);
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        Clipboard.SetText(linkString);
        
        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }
    
    public async Task LoadData(PhotoContent? toLoad, bool skipMediaDirectoryCheck = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        DbEntry = NewContentModels.InitializePhotoContent(toLoad);
        
        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry,
            "To File Name",
            AutoRenameSelectedFileBasedOnTitleCommand,
            x => SelectedFile != null && !Path.GetFileNameWithoutExtension(SelectedFile.Name)
                .Equals(SlugTools.CreateSlug(false, x.TitleEntry.UserValue), StringComparison.OrdinalIgnoreCase));
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        
        ShowSizesEntry = await BoolDataEntryContext.CreateInstance();
        ShowSizesEntry.Title = "Show Photo Sizes";
        ShowSizesEntry.HelpText = "If enabled the page users are shown will have a list of all available sizes";
        ShowSizesEntry.ReferenceValue = DbEntry.ShowPhotoSizes;
        ShowSizesEntry.UserValue = DbEntry.ShowPhotoSizes;
        
        LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);
        
        if (!skipMediaDirectoryCheck && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName) && DbEntry.Id > 0)
        {
            await FileManagement.CheckPhotoFileIsInMediaAndContentDirectories(DbEntry);
            
            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().FullName,
                DbEntry.OriginalFileName));
            
            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentDirectory(DbEntry);
            
            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, DbEntry.OriginalFileName));
            
            if (!archiveFile.Exists && contentFile.Exists)
            {
                await FileManagement.WriteSelectedPhotoContentFileToMediaArchive(contentFile);
                archiveFile.Refresh();
            }
            
            if (archiveFile.Exists)
            {
                LoadedFile = archiveFile;
                SelectedFile = archiveFile;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Missing Photo",
                    $"There is an original file listed for this photo - {DbEntry.OriginalFileName} -" +
                    $" but it was not found in the expected location of {archiveFile.FullName} or {contentFile.FullName} - " +
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
        
        IsoEntry = await ConversionDataEntryContext<int?>.CreateInstance(ConversionDataEntryHelpers
            .IntNullableConversion);
        IsoEntry.Title = "ISO";
        IsoEntry.HelpText = "A measure of a sensor films sensitivity to light, 100 is a typical value";
        IsoEntry.ReferenceValue = DbEntry.Iso;
        IsoEntry.UserText = DbEntry.Iso?.ToString("F0") ?? string.Empty;
        
        PhotoCreatedOnEntry =
            await ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
        PhotoCreatedOnEntry.Title = "Photo Created On";
        PhotoCreatedOnEntry.HelpText = "Date and, optionally, Time the Photo was Created";
        PhotoCreatedOnEntry.ReferenceValue = DbEntry.PhotoCreatedOn;
        PhotoCreatedOnEntry.UserText = DbEntry.PhotoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");
        
        PhotoCreatedOnUtcEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        PhotoCreatedOnUtcEntry.Title = "Photo Created On UTC Date/Time";
        PhotoCreatedOnUtcEntry.HelpText =
            "UTC Date and Time the Photo was Created - the UTC Date Time is not displayed but is used to compare the Photo's Date Time to data like GPX Files/Lines.";
        PhotoCreatedOnUtcEntry.ReferenceValue = DbEntry.PhotoCreatedOnUtc;
        PhotoCreatedOnUtcEntry.UserText = DbEntry.PhotoCreatedOnUtc?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
        
        OptionalLocationEntry = await OptionalLocationEntryContext.CreateInstance(StatusContext, DbEntry);
        
        HelpContext = new HelpDisplayContext([
            PhotoEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        ]);
        
        if (DbEntry.Id < 1 && InitialPhoto is { Exists: true } && PhotoGenerator.PhotoFileTypeIsSupported(InitialPhoto))
        {
            SelectedFile = InitialPhoto;
            ResizeSelectedFile = true;
            InitialPhoto = null;
            var (generationReturn, metadataReturn) =
                await PhotoGenerator.PhotoMetadataFromFile(SelectedFile, false, StatusContext.ProgressTracker());
            if (!generationReturn.HasError && metadataReturn != null)
                await PhotoMetadataToCurrentContent(metadataReturn);
        }
        
        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        
        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
        
        if (e.PropertyName == nameof(SelectedFile)) StatusContext.RunFireAndForgetNonBlockingTask(SelectedFileChanged);
    }
    
    public async Task PhotoMetadataToCurrentContent(PhotoMetadata metadata)
    {
        ApertureEntry!.UserValue = metadata.Aperture ?? string.Empty;
        CameraMakeEntry!.UserValue = metadata.CameraMake ?? string.Empty;
        CameraModelEntry!.UserValue = metadata.CameraModel ?? string.Empty;
        FocalLengthEntry!.UserValue = metadata.FocalLength ?? string.Empty;
        IsoEntry!.UserText = metadata.Iso?.ToString("F0") ?? string.Empty;
        OptionalLocationEntry!.LatitudeEntry!.UserText = metadata.Latitude?.ToString("F6") ?? string.Empty;
        OptionalLocationEntry.LongitudeEntry!.UserText = metadata.Longitude?.ToString("F6") ?? string.Empty;
        OptionalLocationEntry.ElevationEntry!.UserText = metadata.Elevation?.ToString("N0") ?? string.Empty;
        LensEntry!.UserValue = metadata.Lens ?? string.Empty;
        LicenseEntry!.UserValue = metadata.License ?? string.Empty;
        PhotoCreatedByEntry!.UserValue = metadata.PhotoCreatedBy ?? string.Empty;
        PhotoCreatedOnEntry!.UserText = metadata.PhotoCreatedOn.ToString("MM/dd/yyyy h:mm:ss tt");
        PhotoCreatedOnUtcEntry!.UserText =
            metadata.PhotoCreatedOnUtc?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
        ShutterSpeedEntry!.UserValue = metadata.ShutterSpeed ?? string.Empty;
        TitleSummarySlugFolder!.SummaryEntry.UserValue = metadata.Summary ?? string.Empty;
        TagEdit!.Tags = metadata.Tags ?? string.Empty;
        TitleSummarySlugFolder.TitleEntry.UserValue = metadata.Title ?? string.Empty;
        await TitleSummarySlugFolder.TitleToSlug();
        TitleSummarySlugFolder.FolderEntry.UserValue = metadata.PhotoCreatedOn.Year.ToString("F0");
    }
    
    [BlockingCommand]
    private async Task PointFromLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("The Photo must be saved before creating a Point.");
            return;
        }
        
        if (OptionalLocationEntry!.LatitudeEntry!.UserValue == null ||
            OptionalLocationEntry.LongitudeEntry!.UserValue == null)
        {
            StatusContext.ToastError("Latitude or Longitude is missing?");
            return;
        }
        
        var latitudeValidation =
            await CommonContentValidation.LatitudeValidation(OptionalLocationEntry.LatitudeEntry.UserValue.Value);
        var longitudeValidation =
            await CommonContentValidation.LongitudeValidation(OptionalLocationEntry.LongitudeEntry.UserValue.Value);
        
        if (!latitudeValidation.Valid || !longitudeValidation.Valid)
        {
            StatusContext.ToastError("Latitude/Longitude is not valid?");
            return;
        }
        
        var frozenNow = DateTime.Now;
        
        var newPartialPoint = PointContent.CreateInstance();
        
        newPartialPoint.CreatedOn = frozenNow;
        newPartialPoint.FeedOn = frozenNow;
        newPartialPoint.BodyContent = BracketCodePhotos.Create(DbEntry);
        newPartialPoint.Title = $"Point From {TitleSummarySlugFolder!.TitleEntry.UserValue}";
        newPartialPoint.Tags = TagEdit!.TagListString();
        newPartialPoint.Slug = SlugTools.CreateSlug(true, newPartialPoint.Title);
        newPartialPoint.Latitude = OptionalLocationEntry.LatitudeEntry.UserValue.Value;
        newPartialPoint.Longitude = OptionalLocationEntry.LongitudeEntry.UserValue.Value;
        newPartialPoint.Elevation = OptionalLocationEntry.ElevationEntry!.UserValue;
        
        var pointWindow = await PointContentEditorWindow.CreateInstance(newPartialPoint);
        
        await pointWindow.PositionWindowAndShowOnUiThread();
    }
    
    [BlockingCommand]
    public async Task RenameSelectedFile()
    {
        await FileHelpers.RenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x);
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
    
    [BlockingCommand]
    public async Task RotatePhotoLeft()
    {
        await RotateImage(Orientation.Rotate270);
    }
    
    [BlockingCommand]
    public async Task RotatePhotoRight()
    {
        await RotateImage(Orientation.Rotate90);
    }
    
    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges);
    }
    
    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges, true);
    }
    
    public async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (SelectedFile == null)
        {
            StatusContext.ToastError("No File Selected? There must be a photograph to Save...");
            return;
        }
        
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
    
    [BlockingCommand]
    public async Task SaveAndReprocessPhoto()
    {
        await SaveAndGenerateHtml(true);
    }
    
    private async Task SelectedFileChanged()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        SelectedFileHasPathOrNameChanges =
            (SelectedFile?.FullName ?? string.Empty) != (LoadedFile?.FullName ?? string.Empty);
        
        var (isValid, explanation) =
            await CommonContentValidation.PhotoFileValidation(SelectedFile, DbEntry.ContentId);
        
        SelectedFileHasValidationIssues = !isValid;
        
        SelectedFileValidationMessage = explanation;
        
        SelectedFileNameHasInvalidCharacters =
            await CommonContentValidation.FileContentFileFileNameHasInvalidCharacters(SelectedFile, DbEntry.ContentId);
        
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
        
        TitleSummarySlugFolder?.CheckForChangesToTitleToFunctionStates();
    }
    
    [BlockingCommand]
    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Please save the content first...");
            return;
        }
        
        var settings = UserSettingsSingleton.CurrentSettings();
        
        var url = $"{settings.PhotoPageUrl(DbEntry)}";
        
        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
    
    [BlockingCommand]
    public async Task ViewPhotoMetadata()
    {
        await PhotoMetadataReport.AllPhotoMetadataToHtml(SelectedFile, StatusContext);
    }
    
    [BlockingCommand]
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