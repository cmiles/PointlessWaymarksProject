using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PhotoSauce.MagicScaler;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.BoolDataEntry;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageContentEditor;

[ObservableObject]
public partial class ImageContentEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private StringDataEntryContext _altTextEntry;
    [ObservableProperty] private RelayCommand _autoRenameSelectedFileCommand;
    [ObservableProperty] private BodyContentEditorContext _bodyContent;
    [ObservableProperty] private RelayCommand _chooseFileCommand;
    [ObservableProperty] private ContentIdViewerControlContext _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
    [ObservableProperty] private ImageContent _dbEntry;
    [ObservableProperty] private RelayCommand _extractNewLinksCommand;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext _helpContext;
    [ObservableProperty] private FileInfo _initialImage;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private FileInfo _loadedFile;
    [ObservableProperty] private ContentSiteFeedAndIsDraftContext _mainSiteFeed;
    [ObservableProperty] private RelayCommand _renameSelectedFileCommand;
    [ObservableProperty] private bool _resizeSelectedFile;
    [ObservableProperty] private RelayCommand _rotateImageLeftCommand;
    [ObservableProperty] private RelayCommand _rotateImageRightCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveAndReprocessImageCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private FileInfo _selectedFile;
    [ObservableProperty] private BitmapSource _selectedFileBitmapSource;
    [ObservableProperty] private bool _selectedFileHasPathOrNameChanges;
    [ObservableProperty] private bool _selectedFileHasValidationIssues;
    [ObservableProperty] private bool _selectedFileNameHasInvalidCharacters;
    [ObservableProperty] private string _selectedFileValidationMessage;
    [ObservableProperty] private BoolDataEntryContext _showInSearch;
    [ObservableProperty] private BoolDataEntryContext _showSizes;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext _tagEdit;
    [ObservableProperty] private TitleSummarySlugEditorContext _titleSummarySlugFolder;
    [ObservableProperty] private UpdateNotesEditorContext _updateNotes;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;
    [ObservableProperty] private RelayCommand _viewSelectedFileCommand;

    public EventHandler RequestContentEditorWindowClose;

    private ImageContentEditorContext(StatusControlContext statusContext)
    {
        SetupContextAndCommands(statusContext);

        PropertyChanged += OnPropertyChanged;
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) || SelectedFileHasPathOrNameChanges;
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                              SelectedFileHasValidationIssues;
    }

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
        if (initialImage is { Exists: true }) newContext._initialImage = initialImage;
        await newContext.LoadData(contentToLoad);
        return newContext;
    }

    private ImageContent CurrentStateToImageContent()
    {
        var newEntry = new ImageContent();

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
        newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
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
        newEntry.ShowImageSizes = ShowSizes.UserValue;

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

        var linkString = BracketCodeImages.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }


    private async Task LoadData(ImageContent toLoad, bool skipMediaDirectoryCheck = false)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var created = DateTime.Now;

        DbEntry = toLoad ?? new ImageContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            FeedOn = created,
            ShowImageSizes = UserSettingsSingleton.CurrentSettings().ImagePagesHaveLinksToImageSizesByDefault
        };

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ShowInSearch = BoolDataEntryContext.CreateInstanceForShowInSearch(DbEntry, true);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

        ShowSizes = BoolDataEntryContext.CreateInstance();
        ShowSizes.Title = "Show Image Sizes";
        ShowSizes.HelpText = "If enabled the page users be shown a list of all available sizes";
        ShowSizes.ReferenceValue = DbEntry.ShowImageSizes;
        ShowSizes.UserValue = DbEntry.ShowImageSizes;

        if (!skipMediaDirectoryCheck && toLoad != null && !string.IsNullOrWhiteSpace(DbEntry.OriginalFileName))

        {
            await FileManagement.CheckImageFileIsInMediaAndContentDirectories(DbEntry);

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
                await StatusContext.ShowMessageWithOkButton("Missing Image",
                    $"There is an original image file listed for this image - {DbEntry.OriginalFileName} -" +
                    $" but it was not found in the expected location of {archiveFile.FullName} - " +
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

        if (DbEntry.Id < 1 && _initialImage is { Exists: true } && FileHelpers.ImageFileTypeIsSupported(_initialImage))
        {
            SelectedFile = _initialImage;
            ResizeSelectedFile = true;
            _initialImage = null;
        }

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(SelectedFile)) StatusContext.RunFireAndForgetNonBlockingTask(SelectedFileChanged);
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

    private async Task SaveAndGenerateHtml(bool overwriteExistingFiles, bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) = await ImageGenerator.SaveAndGenerateHtml(CurrentStateToImageContent(),
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
            RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
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

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

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
        AutoRenameSelectedFileCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await FileHelpers.TryAutoCleanRenameSelectedFile(SelectedFile, StatusContext, x => SelectedFile = x));
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

        var url = $@"{settings.ImagePageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

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