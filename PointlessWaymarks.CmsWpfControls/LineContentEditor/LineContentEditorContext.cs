using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
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
using PointlessWaymarks.CmsWpfControls.WpfCmsHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.LineContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LineContentEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation, IWebViewMessenger
{
    public EventHandler? RequestContentEditorWindowClose;

    private LineContentEditorContext(StatusControlContext statusContext, LineContent dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        var initialWebFilesMessage = new FileBuilder();

        initialWebFilesMessage.Create.AddRange(WpfCmsHtmlDocument.CmsLeafletMapHtmlAndJs("Map",
            UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            UserSettingsSingleton.CurrentSettings().LongitudeDefault));

        ToWebView.Enqueue(initialWebFilesMessage);

        ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

        JsonFromWebView = new WorkQueue<FromWebViewMessage>(true);

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public BodyContentEditorContext? BodyContent { get; set; }
    public ConversionDataEntryContext<double>? ClimbElevationEntry { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public LineContent DbEntry { get; set; }
    public ConversionDataEntryContext<double>? DescentElevationEntry { get; set; }
    public ConversionDataEntryContext<double>? DistanceEntry { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public WorkQueue<FromWebViewMessage> JsonFromWebView { get; set; }
    public string LineGeoJson { get; set; } = string.Empty;
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public ConversionDataEntryContext<double>? MaximumElevationEntry { get; set; }
    public ConversionDataEntryContext<double>? MinimumElevationEntry { get; set; }
    public BoolDataEntryContext? PublicDownloadLink { get; set; }
    public ConversionDataEntryContext<DateTime?>? RecordingEndedOnEntry { get; set; }
    public ConversionDataEntryContext<DateTime?>? RecordingStartedOnEntry { get; set; }
    public bool ReplaceElevationOnImport { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }
    public bool UpdateStatsOnImport { get; set; } = true;

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    [BlockingCommand]
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

        var possibleTags = featureToCheck.IntersectionTags(
            UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile, CancellationToken.None,
            StatusContext.ProgressTracker());

        if (!possibleTags.Any())
        {
            StatusContext.ToastWarning("No tags found...");
            return;
        }

        TagEdit!.Tags =
            $"{TagEdit.Tags}{(string.IsNullOrWhiteSpace(TagEdit.Tags) ? "" : ",")}{string.Join(",", possibleTags)}";
    }

    public static async Task<LineContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        LineContent? lineContent)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newControl = new LineContentEditorContext(statusContext ?? new StatusControlContext(),
            NewContentModels.InitializeLineContent(lineContent));
        await newControl.LoadData(lineContent);
        return newControl;
    }

    private LineContent CurrentStateToLineContent()
    {
        var newEntry = LineContent.CreateInstance();

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
        newEntry.UpdateNotes = UpdateNotes!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent!.UserValue.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;
        newEntry.Line = LineGeoJson;

        newEntry.PublicDownloadLink = PublicDownloadLink!.UserValue;

        newEntry.RecordingStartedOn = RecordingStartedOnEntry!.UserValue;
        newEntry.RecordingEndedOn = RecordingEndedOnEntry!.UserValue;

        newEntry.LineDistance = DistanceEntry!.UserValue;
        newEntry.MaximumElevation = MaximumElevationEntry!.UserValue;
        newEntry.MinimumElevation = MinimumElevationEntry!.UserValue;
        newEntry.ClimbElevation = ClimbElevationEntry!.UserValue;
        newEntry.DescentElevation = DescentElevationEntry!.UserValue;

        return newEntry;
    }

    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{BodyContent!.UserValue} {UpdateNotes!.UserValue}", StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task ImportFromGpx()
    {
        await ImportFromGpx(ReplaceElevationOnImport, UpdateStatsOnImport);
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

        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder!.TitleEntry.UserValue))
            TitleSummarySlugFolder.TitleEntry.UserValue = trackToImport.Name;
        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.SummaryEntry.UserValue))
            TitleSummarySlugFolder.SummaryEntry.UserValue = trackToImport.Description;
        if (string.IsNullOrWhiteSpace(TitleSummarySlugFolder.FolderEntry.UserValue) &&
            trackToImport.StartsOnLocal != null)
            TitleSummarySlugFolder.FolderEntry.UserValue = trackToImport.StartsOnLocal.Value.Year.ToString();

        LineGeoJson = await LineTools.GeoJsonWithLineStringFromCoordinateList(trackToImport.Track,
            replaceElevations, StatusContext.ProgressTracker());

        if (updateStats)
        {
            await UpdateStatistics();

            RecordingStartedOnEntry!.UserText =
                trackToImport.StartsOnLocal?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
            RecordingEndedOnEntry!.UserText =
                trackToImport.EndsOnLocal?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;
        }
    }

    [BlockingCommand]
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

    [BlockingCommand]
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

    [BlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeLines.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(LineContent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializeLineContent(toLoad);

        TitleSummarySlugFolder =
            await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry, null, null, null);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);
        LineGeoJson = toLoad?.Line ?? string.Empty;

        PublicDownloadLink = await BoolDataEntryContext.CreateInstance();
        PublicDownloadLink.Title = "Show Public Download Link";
        PublicDownloadLink.ReferenceValue = DbEntry.PublicDownloadLink;
        PublicDownloadLink.UserValue = DbEntry.PublicDownloadLink;
        PublicDownloadLink.HelpText =
            "If checked there will be a hyperlink will on the File Content Page to download the content. NOTE! The File" +
            "will be copied into the generated HTML for the site regardless of this setting - this setting is only about " +
            "whether a download link is shown.";

        RecordingStartedOnEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        RecordingStartedOnEntry.Title = "Line Recording Starts On";
        RecordingStartedOnEntry.HelpText = "Date and, optionally, Time for the Start of the Line";
        RecordingStartedOnEntry.ReferenceValue = DbEntry.RecordingStartedOn;
        RecordingStartedOnEntry.UserText =
            DbEntry.RecordingStartedOn?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;

        RecordingEndedOnEntry =
            await ConversionDataEntryContext<DateTime?>.CreateInstance(ConversionDataEntryHelpers
                .DateTimeNullableConversion);
        RecordingEndedOnEntry.Title = "Line Recording Ends On";
        RecordingEndedOnEntry.HelpText = "Date and, optionally, Time for the Start of the Line";
        RecordingEndedOnEntry.ReferenceValue = DbEntry.RecordingEndedOn;
        RecordingEndedOnEntry.UserText = DbEntry.RecordingEndedOn?.ToString("MM/dd/yyyy h:mm:ss tt") ?? string.Empty;

        ClimbElevationEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        ClimbElevationEntry.Title = "Climb Elevation - Feet";
        ClimbElevationEntry.HelpText = "Total amount of Climbing";
        ClimbElevationEntry.ReferenceValue = DbEntry.ClimbElevation;
        ClimbElevationEntry.UserText = DbEntry.ClimbElevation.ToString("F0");

        DescentElevationEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        DescentElevationEntry.Title = "Descent Elevation - Feet";
        DescentElevationEntry.HelpText = "Total amount of Descent";
        DescentElevationEntry.ReferenceValue = DbEntry.DescentElevation;
        DescentElevationEntry.UserText = DbEntry.DescentElevation.ToString("F0");

        MaximumElevationEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        MaximumElevationEntry.Title = "Maximum Elevation - Feet";
        MaximumElevationEntry.HelpText = "Highest Elevation the Line Reaches";
        MaximumElevationEntry.ReferenceValue = DbEntry.MaximumElevation;
        MaximumElevationEntry.UserText = DbEntry.MaximumElevation.ToString("F0");

        MinimumElevationEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        MinimumElevationEntry.Title = "Minimum Elevation - Feet";
        MinimumElevationEntry.HelpText = "Lowest Elevation the Line Reaches";
        MinimumElevationEntry.ReferenceValue = DbEntry.MinimumElevation;
        MinimumElevationEntry.UserText = DbEntry.MinimumElevation.ToString("F0");

        DistanceEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        DistanceEntry.Title = "Distance - Miles";
        DistanceEntry.HelpText = "Total Distance of the Line";
        DistanceEntry.ReferenceValue = DbEntry.LineDistance;
        DistanceEntry.UserText = DbEntry.LineDistance.ToString("F2");

        HelpContext = new HelpDisplayContext([CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock]);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();

        if (e.PropertyName == nameof(LineGeoJson)) StatusContext.RunNonBlockingTask(RefreshMapPreview);
    }

    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        return Task.CompletedTask;
    }

    [BlockingCommand]
    public async Task RefreshMapPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(LineGeoJson))
        {
            StatusContext.ToastError("Nothing to preview?");
            return;
        }

        ToWebView.Enqueue(JsonData.CreateRequest(await MapCmsJson.NewMapFeatureCollectionDtoSerialized(LineGeoJson)));
    }

    [BlockingCommand]
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

    [BlockingCommand]
    public async Task Save()
    {
        await SaveAndGenerateHtml(false);
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await SaveAndGenerateHtml(true);
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

    [BlockingCommand]
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

        DistanceEntry!.UserText = lineStatistics.Length.ToString("F2");
        MaximumElevationEntry!.UserText = lineStatistics.MaximumElevation.ToString("F0");
        MinimumElevationEntry!.UserText = lineStatistics.MinimumElevation.ToString("F0");
        ClimbElevationEntry!.UserText = lineStatistics.ElevationClimb.ToString("F0");
        DescentElevationEntry!.UserText = lineStatistics.ElevationDescent.ToString("F0");
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

        var url = $"{settings.LinePageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}