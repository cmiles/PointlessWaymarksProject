using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetTopologySuite.Features;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.ConversionDataEntry;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.LineContentEditor;

public partial class LineContentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    [ObservableProperty] private RelayCommand _addFeatureIntersectTagsCommand;
    [ObservableProperty] private BodyContentEditorContext _bodyContent;
    [ObservableProperty] private ConversionDataEntryContext<double> _climbElevationEntry;
    [ObservableProperty] private ContentIdViewerControlContext _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
    [ObservableProperty] private LineContent _dbEntry;
    [ObservableProperty] private ConversionDataEntryContext<double> _descentElevationEntry;
    [ObservableProperty] private ConversionDataEntryContext<double> _distanceEntry;
    [ObservableProperty] private RelayCommand _extractNewLinksCommand;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext _helpContext;
    [ObservableProperty] private RelayCommand _importFromGpxCommand;
    [ObservableProperty] private RelayCommand _importGeoJsonFromClipboardCommand;
    [ObservableProperty] private string _lineGeoJson;
    [ObservableProperty] private RelayCommand _lineGeoJsonToClipboardCommand;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private ContentSiteFeedAndIsDraftContext _mainSiteFeed;
    [ObservableProperty] private ConversionDataEntryContext<double> _maximumElevationEntry;
    [ObservableProperty] private ConversionDataEntryContext<double> _minimumElevationEntry;
    [ObservableProperty] private string _previewHtml;
    [ObservableProperty] private string _previewLineJsonDto;
    [ObservableProperty] private ConversionDataEntryContext<DateTime?> _recordingEndedOnEntry;
    [ObservableProperty] private ConversionDataEntryContext<DateTime?> _recordingStartedOnEntry;
    [ObservableProperty] private RelayCommand _refreshMapPreviewCommand;
    [ObservableProperty] private bool _replaceElevationOnImport;
    [ObservableProperty] private RelayCommand _replaceElevationsCommand;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext _tagEdit;
    [ObservableProperty] private TitleSummarySlugEditorContext _titleSummarySlugFolder;
    [ObservableProperty] private UpdateNotesEditorContext _updateNotes;
    [ObservableProperty] private RelayCommand _updateStatisticsCommand;
    [ObservableProperty] private bool _updateStatsOnImport = true;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;

    public EventHandler RequestContentEditorWindowClose;

    private LineContentEditorContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;

        SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
        SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
            LinkExtraction.ExtractNewAndShowLinkContentEditors(
                $"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}", StatusContext.ProgressTracker()));
        ImportFromGpxCommand =
            StatusContext.RunBlockingTaskCommand(async () =>
                await ImportFromGpx(ReplaceElevationOnImport, UpdateStatsOnImport));
        ReplaceElevationsCommand = StatusContext.RunBlockingTaskCommand(async () => await ReplaceElevations());
        RefreshMapPreviewCommand = StatusContext.RunBlockingTaskCommand(RefreshMapPreview);
        LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);
        UpdateStatisticsCommand = StatusContext.RunBlockingTaskCommand(UpdateStatistics);
        LineGeoJsonToClipboardCommand = StatusContext.RunNonBlockingTaskCommand(LineGeoJsonToClipboard);
        ImportGeoJsonFromClipboardCommand = StatusContext.RunBlockingTaskCommand(ImportGeoJsonFromClipboard);
        AddFeatureIntersectTagsCommand = StatusContext.RunBlockingTaskCommand(AddFeatureIntersectTags);

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        PreviewHtml = WpfHtmlDocument.ToHtmlLeafletLineDocument("Line",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault, string.Empty);
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    private async Task AddFeatureIntersectTags()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(LineGeoJson))
        {
            StatusContext.ToastError("No current line?");
            return;
        }

        var featureToCheck = LineContent.FeatureFromGeoJsonLine(LineGeoJson);

        if (featureToCheck == null)
        {
            StatusContext.ToastError("No valid Line check?");
            return;
        }

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            StatusContext.ToastError(
                "To use this feature the Feature Intersect Settings file must be set in the Site Settings...");
            return;
        }

        var possibleTags = featureToCheck.IntersectionTags(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile, CancellationToken.None, StatusContext.ProgressTracker());

        if (!possibleTags.Any())
        {
            StatusContext.ToastWarning("No tags found...");
            return;
        }

        TagEdit.Tags = $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }

    public static async Task<LineContentEditorContext> CreateInstance(StatusControlContext statusContext,
        LineContent lineContent)
    {
        var newControl = new LineContentEditorContext(statusContext);
        await newControl.LoadData(lineContent);
        return newControl;
    }

    private LineContent CurrentStateToLineContent()
    {
        var newEntry = new LineContent();

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

        newEntry.Folder = TitleSummarySlugFolder.FolderEntry.UserValue.TrimNullToEmpty();
        newEntry.Slug = TitleSummarySlugFolder.SlugEntry.UserValue.TrimNullToEmpty();
        newEntry.Summary = TitleSummarySlugFolder.SummaryEntry.UserValue.TrimNullToEmpty();
        newEntry.ShowInMainSiteFeed = MainSiteFeed.ShowInMainSiteFeedEntry.UserValue;
        newEntry.FeedOn = MainSiteFeed.FeedOnEntry.UserValue;
        newEntry.IsDraft = MainSiteFeed.IsDraftEntry.UserValue;
        newEntry.Tags = TagEdit.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes.UpdateNotes.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent.BodyContent.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.Line = LineGeoJson;

        newEntry.RecordingStartedOn = RecordingStartedOnEntry.UserValue;
        newEntry.RecordingEndedOn = RecordingEndedOnEntry.UserValue;

        newEntry.LineDistance = DistanceEntry.UserValue;
        newEntry.MaximumElevation = MaximumElevationEntry.UserValue;
        newEntry.MinimumElevation = MinimumElevationEntry.UserValue;
        newEntry.ClimbElevation = ClimbElevationEntry.UserValue;
        newEntry.DescentElevation = DescentElevationEntry.UserValue;

        return newEntry;
    }

    public async Task ImportFromGpx(bool replaceElevations, bool updateStats)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting Line load.");

        var dialog = new VistaOpenFileDialog();

        if (!(dialog.ShowDialog() ?? false)) return;

        var newFile = new FileInfo(dialog.FileName);

        if (!newFile.Exists)
        {
            StatusContext.ToastError("File doesn't exist?");
            return;
        }

        var tracksList = await GpxTools.TracksFromGpxFile(newFile, StatusContext.ProgressTracker());

        if (tracksList.Count < 1)
        {
            StatusContext.ToastError("No Tracks in GPX File?");
            return;
        }

        GpxTools.GpxTrackInformation trackToImport;

        if (tracksList.Count > 1)
        {
            var importTrackName = await StatusContext.ShowMessage("Choose Track",
                "The GPX file contains more than 1 track - choose the track to import:",
                tracksList.Select(x => x.Name).ToList());

            var possibleSelectedTrack = tracksList.Where(x => x.Name == importTrackName).ToList();

            if (possibleSelectedTrack.Count == 1)
            {
                trackToImport = possibleSelectedTrack.Single();
            }
            else
            {
                StatusContext.ToastError("Track not found?");
                return;
            }
        }
        else
        {
            trackToImport = tracksList.First();
        }

        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.TitleEntry.UserValue))
            TitleSummarySlugFolder.TitleEntry.UserValue = trackToImport.Name;
        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.SummaryEntry.UserValue))
            TitleSummarySlugFolder.SummaryEntry.UserValue = trackToImport.Description;
        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.FolderEntry.UserValue) && trackToImport.StartsOnLocal != null)
            TitleSummarySlugFolder.FolderEntry.UserValue = trackToImport.StartsOnLocal.Value.Year.ToString();

        LineGeoJson = await LineTools.GeoJsonWithLineStringFromCoordinateList(trackToImport.Track,
            replaceElevations, StatusContext.ProgressTracker());

        if (updateStats)
        {
            await UpdateStatistics();

            RecordingStartedOnEntry.UserText =
                trackToImport.StartsOnLocal?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
            RecordingEndedOnEntry.UserText = trackToImport.EndsOnLocal?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
        }
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

        var (isValid, explanation) = CommonContentValidation.LineGeoJsonValidation(clipboardText);

        if (!isValid)
        {
            await StatusContext.ShowMessageWithOkButton("Error with GeoJson Import", explanation);
            return;
        }

        LineGeoJson = clipboardText;

        await UpdateStatistics();
    }

    private async Task LineGeoJsonToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(LineGeoJson))
        {
            StatusContext.ToastWarning("No Line?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(LineGeoJson);
    }

    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeLines.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(LineContent toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var created = DateTime.Now;

        DbEntry = toLoad ?? new LineContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            FeedOn = created
        };

        TitleSummarySlugFolder = await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        LineGeoJson = toLoad?.Line ?? string.Empty;

        RecordingStartedOnEntry =
            ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers.DateTimeNullableConversion);
        RecordingStartedOnEntry.Title = "Line Recording Starts On";
        RecordingStartedOnEntry.HelpText = "Date and, optionally, Time for the Start of the Line";
        RecordingStartedOnEntry.ReferenceValue = DbEntry.RecordingStartedOn;
        RecordingStartedOnEntry.UserText =
            DbEntry.RecordingStartedOn?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;

        RecordingEndedOnEntry =
            ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers.DateTimeNullableConversion);
        RecordingEndedOnEntry.Title = "Line Recording Ends On";
        RecordingEndedOnEntry.HelpText = "Date and, optionally, Time for the Start of the Line";
        RecordingEndedOnEntry.ReferenceValue = DbEntry.RecordingEndedOn;
        RecordingEndedOnEntry.UserText = DbEntry.RecordingEndedOn?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;

        ClimbElevationEntry =
            ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        ClimbElevationEntry.Title = "Climb Elevation - Feet";
        ClimbElevationEntry.HelpText = "Total amount of Climbing";
        ClimbElevationEntry.ReferenceValue = DbEntry.ClimbElevation;
        ClimbElevationEntry.UserText = DbEntry.ClimbElevation.ToString("F0");

        DescentElevationEntry =
            ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        DescentElevationEntry.Title = "Descent Elevation - Feet";
        DescentElevationEntry.HelpText = "Total amount of Descent";
        DescentElevationEntry.ReferenceValue = DbEntry.DescentElevation;
        DescentElevationEntry.UserText = DbEntry.DescentElevation.ToString("F0");

        MaximumElevationEntry =
            ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        MaximumElevationEntry.Title = "Maximum Elevation - Feet";
        MaximumElevationEntry.HelpText = "Highest Elevation the Line Reaches";
        MaximumElevationEntry.ReferenceValue = DbEntry.MaximumElevation;
        MaximumElevationEntry.UserText = DbEntry.MaximumElevation.ToString("F0");

        MinimumElevationEntry =
            ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        MinimumElevationEntry.Title = "Minimum Elevation - Feet";
        MinimumElevationEntry.HelpText = "Lowest Elevation the Line Reaches";
        MinimumElevationEntry.ReferenceValue = DbEntry.MinimumElevation;
        MinimumElevationEntry.UserText = DbEntry.MinimumElevation.ToString("F0");

        DistanceEntry = ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        DistanceEntry.Title = "Distance - Miles";
        DistanceEntry.HelpText = "Total Distance of the Line";
        DistanceEntry.ReferenceValue = DbEntry.LineDistance;
        DistanceEntry.UserText = DbEntry.LineDistance.ToString("F2");

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(LineGeoJson)) StatusContext.RunNonBlockingTask(RefreshMapPreview);
    }

    public async Task RefreshMapPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(LineGeoJson))
        {
            StatusContext.ToastError("Nothing to preview?");
            return;
        }

        //Using the new Guid as the page URL forces a changed value into the LineJsonDto
        PreviewLineJsonDto = await LineData.GenerateLineJson(LineGeoJson,
            TitleSummarySlugFolder.TitleEntry.UserValue ?? string.Empty, Guid.NewGuid().ToString());
    }

    public async Task ReplaceElevations()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(LineGeoJson))
        {
            StatusContext.ToastError("There is no line data?");
            return;
        }

        LineGeoJson = await GeoJsonTools.ReplaceElevationsInGeoJsonWithLineString(LineGeoJson,
            StatusContext.ProgressTracker());
    }

    public async Task SaveAndGenerateHtml(bool closeAfterSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var (generationReturn, newContent) = await LineGenerator.SaveAndGenerateHtml(CurrentStateToLineContent(),
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

    public async Task UpdateStatistics()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(LineGeoJson))
        {
            StatusContext.ToastError("No Line to Update Stats From?");
            return;
        }

        var coordinateList = LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(LineGeoJson);

        var lineStatistics = DistanceTools.LineStatsInImperialFromCoordinateList(coordinateList);

        DistanceEntry.UserText = lineStatistics.Length.ToString("F2");
        MaximumElevationEntry.UserText = lineStatistics.MaximumElevation.ToString("F0");
        MinimumElevationEntry.UserText = lineStatistics.MinimumElevation.ToString("F0");
        ClimbElevationEntry.UserText = lineStatistics.ElevationClimb.ToString("F0");
        DescentElevationEntry.UserText = lineStatistics.ElevationDescent.ToString("F0");
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

        var url = $@"{settings.LinePageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}