using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ImageContentEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;

    private ImageContentEditorContext(StatusControlContext statusContext, ImageContent dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public StringDataEntryContext? AltTextEntry { get; set; }
    public BodyContentEditorContext? BodyContent { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public ImageContent DbEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public FileInfo? InitialImage { get; set; }
    public FileInfo? LoadedFile { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public bool ResizeSelectedFile { get; set; }
    public EventHandler<EventArgs>? Saved { get; set; }
    public FileInfo? SelectedFile { get; set; }
    public BitmapSource? SelectedFileBitmapSource { get; set; }
    public bool SelectedFileHasPathOrNameChanges { get; set; }
    public bool SelectedFileHasValidationIssues { get; set; }
    public bool SelectedFileNameHasInvalidCharacters { get; set; }
    public string? SelectedFileValidationMessage { get; set; }
    public BoolDataEntryContext? ShowInSearch { get; set; }
    public BoolDataEntryContext? ShowSizes { get; set; }
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

    [BlockingCommand]
    public async Task ChooseFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting image load.");

        var dialog = new VistaOpenFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

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

    public static async Task<ImageContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        ImageContent? contentToLoad = null, FileInfo? initialImage = null)
    {
        var newContext = new ImageContentEditorContext(statusContext ?? new StatusControlContext(),
            NewContentModels.InitializeImageContent(contentToLoad));
        if (initialImage is { Exists: true }) newContext.InitialImage = initialImage;
        await newContext.LoadData(contentToLoad);
        return newContext;
    }

    private ImageContent CurrentStateToImageContent()
    {
        var newEntry = ImageContent.CreateInstance();

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }

        newEntry.MainPicture = newEntry.ContentId;
        newEntry.Folder = TitleSummarySlugFolder!.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed!.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.ShowInSearch = ShowInSearch!.UserValue;
        newEntry.Tags = TagEdit!.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.AltText = AltTextEntry!.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.OriginalFileName = SelectedFile?.Name;
        newEntry.BodyContent = BodyContent!.UserValue.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.ShowImageSizes = ShowSizes!.UserValue;

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

        var linkString = BracketCodeImages.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }


    private async Task LoadData(ImageContent? toLoad, bool skipMediaDirectoryCheck = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializeImageContent(toLoad);

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry,
            "To File Name",
            AutoRenameSelectedFileBasedOnTitleCommand,
            x => SelectedFile != null && !Path.GetFileNameWithoutExtension(SelectedFile.Name)
                .Equals(SlugTools.CreateSlug(false, x.TitleEntry.UserValue), StringComparison.OrdinalIgnoreCase));
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ShowInSearch = await BoolDataEntryTypes.CreateInstanceForShowInSearch(DbEntry, true);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        ShowSizes = await BoolDataEntryContext.CreateInstance();
        ShowSizes.Title = "Show Image Sizes";
        ShowSizes.HelpText = "If enabled the page users be shown a list of all available sizes";
        ShowSizes.ReferenceValue = DbEntry.ShowImageSizes;
        ShowSizes.UserValue = DbEntry.ShowImageSizes;

        if (!skipMediaDirectoryCheck && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName) && DbEntry.Id > 0)

        {
            await FileManagement.CheckImageFileIsInMediaAndContentDirectories(DbEntry);

            var archiveFile = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageDirectory().FullName,
                DbEntry.OriginalFileName));

            var fileContentDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentDirectory(DbEntry);

            var contentFile = new FileInfo(Path.Combine(fileContentDirectory.FullName, DbEntry.OriginalFileName));

            if (!archiveFile.Exists && contentFile.Exists)
            {
                await FileManagement.WriteSelectedImageContentFileToMediaArchive(contentFile);
                archiveFile.Refresh();
            }

            if (archiveFile.Exists)
            {
                LoadedFile = archiveFile;
                SelectedFile = archiveFile;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Missing Image",
                    $"There is an original image file listed for this image - {DbEntry.OriginalFileName} -" +
                    $" but it was not found in the expected location of {archiveFile.FullName} or {contentFile.FullName} - " +
                    "this will cause an error and prevent you from saving. You can re-load the image or " +
                    "maybe your media directory moved unexpectedly and you could close this editor " +
                    "and restore it (or change it in settings) before continuing?");
            }
        }

        AltTextEntry = StringDataEntryContext.CreateInstance();
        AltTextEntry.Title = "Alt Text";
        AltTextEntry.HelpText =
            "A short text description of the image - in some cases the Summary may be all that is needed.";
        AltTextEntry.ReferenceValue = DbEntry.AltText ?? string.Empty;
        AltTextEntry.UserValue = DbEntry.AltText.TrimNullToEmpty();

        if (DbEntry.Id < 1 && InitialImage is { Exists: true } && FileHelpers.ImageFileTypeIsSupported(InitialImage))
        {
            SelectedFile = InitialImage;
            ResizeSelectedFile = true;
            InitialImage = null;

            var imageMetadata =
                await ImageGenerator.ImageMetadataFromFile(SelectedFile, StatusContext.ProgressTracker());

            if (imageMetadata is { generationReturn: { HasError : false } } and { metadata: not null })
            {
                TitleSummarySlugFolder.SummaryEntry.UserValue = imageMetadata.metadata.Summary ?? string.Empty;
                TagEdit.Tags = imageMetadata.metadata.Tags ?? string.Empty;
                TitleSummarySlugFolder.TitleEntry.UserValue = imageMetadata.metadata.Title ?? string.Empty;
                await TitleSummarySlugFolder.TitleToSlug();
            }
            else
            {
                TitleSummarySlugFolder.TitleEntry.UserValue = Path.GetFileNameWithoutExtension(SelectedFile.Name)
                    .Replace("-", " ").Replace("_", " ")
                    .CamelCaseToSpacedString();
            }


            if (!string.IsNullOrWhiteSpace(TitleSummarySlugFolder.TitleEntry.UserValue))
            {
                var possibleDateTimeFromTitle = DateTimeTools.DateOnlyFromTitleStringByConvention(TitleSummarySlugFolder.TitleEntry.UserValue);
                if (possibleDateTimeFromTitle != null)
                    TitleSummarySlugFolder.FolderEntry.UserValue = possibleDateTimeFromTitle.Value.titleDate.Year.ToString("F0");
            }
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
    public async Task RotateImageLeft()
    {
        await RotateImage(Orientation.Rotate270);
    }

    [BlockingCommand]
    public async Task RotateImageRight()
    {
        await RotateImage(Orientation.Rotate90);
    }

    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges, false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(ResizeSelectedFile || SelectedFileHasPathOrNameChanges, true);
    }

    private async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile == null)
        {
            StatusContext.ToastError("No File Selected? There must be a image to Save...");
            return;
        }

        var (generationReturn, newContent) = await ImageGenerator.SaveAndGenerateHtml(CurrentStateToImageContent(),
            SelectedFile, overwriteExistingFiles, null, StatusContext.ProgressTracker());

        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving", generationReturn.GenerationNote);
            return;
        }

        await LoadData(newContent);

        Saved?.Invoke(this, EventArgs.Empty);

        if (closeAfterSave)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
        }
    }

    [BlockingCommand]
    public async Task SaveAndReprocessImage()
    {
        await SaveAndGenerateHtml(true, false);
    }

    private async Task SelectedFileChanged()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        SelectedFileHasPathOrNameChanges =
            (SelectedFile?.FullName ?? string.Empty) != (LoadedFile?.FullName ?? string.Empty);

        var (isValid, explanation) =
            await CommonContentValidation.ImageFileValidation(SelectedFile, DbEntry.ContentId);

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

        var url = $"{settings.ImagePageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [BlockingCommand]
    private async Task ViewSelectedFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            StatusContext.ToastError("No Selected File or Selected File no longer exists?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}