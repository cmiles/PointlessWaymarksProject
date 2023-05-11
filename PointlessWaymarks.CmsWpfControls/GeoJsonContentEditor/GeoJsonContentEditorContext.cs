using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;

public partial class GeoJsonContentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    [ObservableProperty] private RelayCommand _addFeatureIntersectTagsCommand;
    [ObservableProperty] private BodyContentEditorContext? _bodyContent;
    [ObservableProperty] private ContentIdViewerControlContext? _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext? _createdUpdatedDisplay;
    [ObservableProperty] private GeoJsonContent _dbEntry;
    [ObservableProperty] private RelayCommand _extractNewLinksCommand;
    [ObservableProperty] private string _geoJsonText = string.Empty;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext? _helpContext;
    [ObservableProperty] private RelayCommand _importGeoJsonFileCommand;
    [ObservableProperty] private RelayCommand _importGeoJsonFromClipboardCommand;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private ContentSiteFeedAndIsDraftContext? _mainSiteFeed;
    [ObservableProperty] private string _previewGeoJsonDto = string.Empty;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private RelayCommand _refreshMapPreviewCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext? _tagEdit;
    [ObservableProperty] private TitleSummarySlugEditorContext? _titleSummarySlugFolder;
    [ObservableProperty] private UpdateNotesEditorContext? _updateNotes;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;
    public EventHandler? RequestContentEditorWindowClose;

    private GeoJsonContentEditorContext(StatusControlContext statusContext, GeoJsonContent dbEntry)
    {
        _statusContext = statusContext;

        PropertyChanged += OnPropertyChanged;

        _saveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
        _saveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
        _viewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
        _extractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
            LinkExtraction.ExtractNewAndShowLinkContentEditors($"{BodyContent!.BodyContent} {UpdateNotes!.UpdateNotes}",
                StatusContext.ProgressTracker()));
        _importGeoJsonFileCommand = StatusContext.RunBlockingTaskCommand(ImportGeoJsonFile);
        _importGeoJsonFromClipboardCommand = StatusContext.RunBlockingTaskCommand(ImportGeoJsonFromClipboard);
        _refreshMapPreviewCommand = StatusContext.RunBlockingTaskCommand(RefreshMapPreview);
        _linkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);
        _addFeatureIntersectTagsCommand = StatusContext.RunBlockingTaskCommand(AddFeatureIntersectTags);

        _previewHtml = WpfHtmlDocument.ToHtmlLeafletGeoJsonDocument("GeoJson",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);

        _dbEntry = dbEntry;
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    private async Task AddFeatureIntersectTags()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(GeoJsonText))
        {
            StatusContext.ToastError("No current line?");
            return;
        }

        var featuresToCheck = GeoJsonContent.FeaturesFromGeoJson(GeoJsonText);

        if (!featuresToCheck.Any())
        {
            StatusContext.ToastError("No features in the GeoJson check?");
            return;
        }

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            StatusContext.ToastError(
                "To use this feature the Feature Intersect Settings file must be set in the Site Settings...");
            return;
        }

        var intersectResult = new IntersectResult(featuresToCheck);

        var possibleTags = intersectResult
            .IntersectionTags(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
                CancellationToken.None, StatusContext.ProgressTracker()).Tags;

        if (!possibleTags.Any())
        {
            StatusContext.ToastWarning("No tags found...");
            return;
        }

        TagEdit!.Tags =
            $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }

    public static async Task<GeoJsonContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        GeoJsonContent? geoJsonContent)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newControl = new GeoJsonContentEditorContext(statusContext ?? new StatusControlContext(),
            NewContentModels.InitializeGeoJsonContent(geoJsonContent));
        await newControl.LoadData(geoJsonContent);
        return newControl;
    }

    private GeoJsonContent CurrentStateToGeoJsonContent()
    {
        var newEntry = NewContentModels.InitializeGeoJsonContent(null);

        if (DbEntry.Id > 0)
        {
            newEntry.ContentId = DbEntry.ContentId;
            newEntry.CreatedOn = DbEntry.CreatedOn;
            newEntry.LastUpdatedOn = DateTime.Now;
            newEntry.LastUpdatedBy = CreatedUpdatedDisplay!.UpdatedByEntry.UserValue.TrimNullToEmpty();
        }

        newEntry.Folder = TitleSummarySlugFolder!.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed!.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.Tags = TagEdit!.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes!.UpdateNotes.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent!.BodyContent.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.GeoJson = GeoJsonText;

        return newEntry;
    }

    public async Task ImportGeoJsonFile()
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

        string geoJson;

        await using (var fs = new FileStream(newFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs, Encoding.Default))
        {
            geoJson = await sr.ReadToEndAsync();
        }

        var (isValid, explanation) = await CommonContentValidation.GeoJsonValidation(geoJson);

        if (!isValid)
        {
            await StatusContext.ShowMessageWithOkButton("Error with GeoJson Import", explanation);
            return;
        }

        GeoJsonText = geoJson;
    }

    public async Task ImportGeoJsonFromClipboard()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Getting GeoJson from Clipboard");

        var clipboardText = Clipboard.GetText();

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            StatusContext.ToastError("Blank/Empty Clipboard?");
            return;
        }

        var (isValid, explanation) = await CommonContentValidation.GeoJsonValidation(clipboardText);

        if (!isValid)
        {
            await StatusContext.ShowMessageWithOkButton("Error with GeoJson Import", explanation);
            return;
        }

        GeoJsonText = clipboardText;
    }

    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeGeoJson.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(GeoJsonContent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializeGeoJsonContent(toLoad);

        TitleSummarySlugFolder =
            await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry, null, null, null);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        GeoJsonText = StringTools.NullToEmptyTrim(DbEntry.GeoJson);

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary,
            BracketCodeHelpMarkdown.HelpBlock,
            GeoJsonContentHelpMarkdown.HelpBlock
        });

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(GeoJsonText)) StatusContext.RunNonBlockingTask(RefreshMapPreview);
    }

    public async Task RefreshMapPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(GeoJsonText))
        {
            StatusContext.ToastError("Nothing to preview?");
            return;
        }

        //Using the new Guid as the page URL forces a changed value into the LineJsonDto
        PreviewGeoJsonDto = await GeoJsonData.GenerateGeoJson(GeoJsonText, Guid.NewGuid().ToString());
    }

    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) = await GeoJsonGenerator.SaveAndGenerateHtml(CurrentStateToGeoJsonContent(),
            null, StatusContext.ProgressTracker());

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


    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Please save the content first...");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.GeoJsonPageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}