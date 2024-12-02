using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentDropdownDataEntry;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;
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
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.TrailContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TrailContentEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    public EventHandler? RequestContentEditorWindowClose;

    private TrailContentEditorContext(StatusControlContext statusContext, TrailContent dbEntry)
    {
        StatusContext = statusContext;

        BuildCommands();

        DbEntry = dbEntry;

        PropertyChanged += OnPropertyChanged;
    }

    public BoolDataEntryContext BikesEntry { get; set; }
    public StringDataEntryContext BikesNoteEntry { get; set; }
    public BodyContentEditorContext? BodyContent { get; set; }
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public TrailContent DbEntry { get; set; }
    public BoolDataEntryContext DogsEntry { get; set; }
    public StringDataEntryContext DogsNoteEntry { get; set; }
    public ContentDropdownDataEntryContext EndingPointContentIdEntry { get; set; }
    public BoolDataEntryContext FeeEntry { get; set; }
    public StringDataEntryContext FeeNoteEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public ContentDropdownDataEntryContext LineContentIdEntry { get; set; }
    public StringDataEntryContext LocationAreaEntry { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public ContentDropdownDataEntryContext MapComponentIdEntry { get; set; }
    public OptionalLocationEntryContext? OptionalLocationEntry { get; set; }
    public StringDataEntryContext OtherDetailsEntry { get; set; }
    public BoolDataEntryContext ShowInSearch { get; set; }
    public ContentDropdownDataEntryContext StartingPointContentIdEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }

    public string TrailEditorHelpText =>
        @"
### Trail Content

Trail Content can bring together a Map, Line, Start and End Points and structured data common especially for trail descriptions for US hiking trails.

";

    public StringDataEntryContext TrailShapeEntry { get; set; }
    public UpdateNotesEditorContext? UpdateNotes { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
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

    public static async Task<TrailContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        TrailContent? toLoad = null)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new TrailContentEditorContext(factoryStatusContext,
            NewContentModels.InitializeTrailContent(toLoad));
        await newContext.LoadData(toLoad);
        return newContext;
    }

    private TrailContent CurrentStateToTrailContent()
    {
        var newEntry = TrailContent.CreateInstance();

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
        newEntry.ShowInSearch = ShowInSearch.UserValue;
        newEntry.Tags = TagEdit!.TagListString();
        newEntry.Title = TitleSummarySlugFolder.TitleEntry.UserValue.TrimNullToEmpty();
        newEntry.CreatedBy = CreatedUpdatedDisplay!.CreatedByEntry.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotes = UpdateNotes!.UserValue.TrimNullToEmpty();
        newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
        newEntry.BodyContent = BodyContent!.UserValue.TrimNullToEmpty();
        newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

        newEntry.Fees = FeeEntry.UserValue;
        newEntry.FeesNote = FeeNoteEntry.UserValue.TrimNullToEmpty();
        newEntry.Dogs = DogsEntry.UserValue;
        newEntry.DogsNote = DogsNoteEntry.UserValue.TrimNullToEmpty();
        newEntry.Bikes = BikesEntry.UserValue;
        newEntry.BikesNote = BikesNoteEntry.UserValue.TrimNullToEmpty();
        newEntry.OtherDetails = OtherDetailsEntry.UserValue.TrimNullToEmpty();
        newEntry.LocationArea = LocationAreaEntry.UserValue.TrimNullToEmpty();
        newEntry.TrailShape = TrailShapeEntry.UserValue.TrimNullToEmpty();

        newEntry.MapComponentId = MapComponentIdEntry.UserValue;
        newEntry.LineContentId = LineContentIdEntry.UserValue;
        newEntry.StartingPointContentId = StartingPointContentIdEntry.UserValue;
        newEntry.EndingPointContentId = EndingPointContentIdEntry.UserValue;

        return newEntry;
    }

    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{BodyContent!.UserValue} {UpdateNotes!.UserValue}",
            StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodeTrails.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        await StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(TrailContent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializeTrailContent(toLoad);

        TitleSummarySlugFolder =
            await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry, null, null, null);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ShowInSearch = await BoolDataEntryTypes.CreateInstanceForShowInSearch(DbEntry, true);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

        FeeEntry = await BoolDataEntryContext.CreateInstance();
        FeeEntry.Title = "Fees";
        FeeEntry.HelpText =
            "Indicate if there is a fee - or leave this blank - this can be a complicated issue but limiting your entry to Yes or No is useful for search.";
        FeeEntry.ReferenceValue = DbEntry.Fees;
        FeeEntry.UserValue = DbEntry.Fees;

        FeeNoteEntry = StringDataEntryContext.CreateInstance();
        FeeNoteEntry.Title = "Fee Note";
        FeeNoteEntry.HelpText = "Notes on any fees - could be text, links, snippets or bracket codes to other content.";
        FeeNoteEntry.ReferenceValue = DbEntry.FeesNote ?? string.Empty;
        FeeNoteEntry.UserValue = DbEntry.FeesNote ?? string.Empty;

        DogsEntry = await BoolDataEntryContext.CreateInstance();
        DogsEntry.Title = "Dogs";
        DogsEntry.HelpText =
            "Indicate if dogs are allowed - or leave this blank - this can be a complicated issue but limiting your entry to Yes or No is useful for search.";
        DogsEntry.ReferenceValue = DbEntry.Dogs;
        DogsEntry.UserValue = DbEntry.Dogs;

        DogsNoteEntry = StringDataEntryContext.CreateInstance();
        DogsNoteEntry.Title = "Dogs Note";
        DogsNoteEntry.HelpText = "Notes on dogs - could be text, links, snippets or bracket codes to other content.";
        DogsNoteEntry.ReferenceValue = DbEntry.DogsNote ?? string.Empty;
        DogsNoteEntry.UserValue = DbEntry.DogsNote ?? string.Empty;

        BikesEntry = await BoolDataEntryContext.CreateInstance();
        BikesEntry.Title = "Bikes";
        BikesEntry.HelpText =
            "Indicate if bikes are allowed - or leave this blank - this can be a complicated issue but limiting your entry to Yes or No is useful for search.";
        BikesEntry.ReferenceValue = DbEntry.Bikes;
        BikesEntry.UserValue = DbEntry.Bikes;

        BikesNoteEntry = StringDataEntryContext.CreateInstance();
        BikesNoteEntry.Title = "Bikes Note";
        BikesNoteEntry.HelpText = "Notes on bikes - could be text, links, snippets or bracket codes to other content.";
        BikesNoteEntry.ReferenceValue = DbEntry.BikesNote ?? string.Empty;
        BikesNoteEntry.UserValue = DbEntry.BikesNote ?? string.Empty;

        OtherDetailsEntry = StringDataEntryContext.CreateInstance();
        OtherDetailsEntry.Title = "Other Details";
        OtherDetailsEntry.HelpText =
            "Other details - could be text, links, snippets or bracket codes to other content.";
        OtherDetailsEntry.ReferenceValue = DbEntry.OtherDetails ?? string.Empty;
        OtherDetailsEntry.UserValue = DbEntry.OtherDetails ?? string.Empty;

        LocationAreaEntry = StringDataEntryContext.CreateInstance();
        LocationAreaEntry.Title = "Location Area";
        LocationAreaEntry.HelpText =
            "A 'general location' - the intent is for this to be short and helpful to a human.";
        LocationAreaEntry.ReferenceValue = DbEntry.OtherDetails ?? string.Empty;
        LocationAreaEntry.UserValue = DbEntry.OtherDetails ?? string.Empty;

        TrailShapeEntry = StringDataEntryContext.CreateInstance();
        TrailShapeEntry.Title = "Trail Shape";
        TrailShapeEntry.HelpText = "Trail Shape - out-and-back, loop, one-way, lollipop...";
        TrailShapeEntry.ReferenceValue = DbEntry.TrailShape ?? string.Empty;
        TrailShapeEntry.UserValue = DbEntry.TrailShape ?? string.Empty;

        MapComponentIdEntry = await ContentDropdownDataEntryContext.CreateInstance(StatusContext,
            ContentDropdownDataEntryLoaders.GetCurrentMapEntries, DbEntry.MapComponentId,
            [DataNotificationContentType.Map]);
        MapComponentIdEntry.Title = "Map Component";
        MapComponentIdEntry.HelpText =
            "Map to display on the page";

        LineContentIdEntry = await ContentDropdownDataEntryContext.CreateInstance(StatusContext,
            ContentDropdownDataEntryLoaders.GetCurrentLineEntries, DbEntry.LineContentId,
            [DataNotificationContentType.Line]);
        LineContentIdEntry.Title = "Line Content";
        LineContentIdEntry.HelpText = "Line Content";

        StartingPointContentIdEntry =
            await ContentDropdownDataEntryContext.CreateInstance(StatusContext,
                ContentDropdownDataEntryLoaders.GetCurrentPointEntries, DbEntry.StartingPointContentId,
                [DataNotificationContentType.Point]);
        StartingPointContentIdEntry.Title = "Starting Point";
        StartingPointContentIdEntry.HelpText = "Starting Point";

        EndingPointContentIdEntry = await ContentDropdownDataEntryContext.CreateInstance(StatusContext,
            ContentDropdownDataEntryLoaders.GetCurrentPointEntries, DbEntry.EndingPointContentId,
            [DataNotificationContentType.Point]);
        EndingPointContentIdEntry.Title = "Ending Point";
        EndingPointContentIdEntry.HelpText = "Ending Point";

        HelpContext = new HelpDisplayContext([
            TrailEditorHelpText, CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        ]);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    [BlockingCommand]
    private async Task PointFromLocation()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("The Photo must be saved before creating a Point.");
            return;
        }

        if (OptionalLocationEntry!.LatitudeEntry!.UserValue == null ||
            OptionalLocationEntry.LongitudeEntry!.UserValue == null)
        {
            await StatusContext.ToastError("Latitude or Longitude is missing?");
            return;
        }

        var latitudeValidation =
            await CommonContentValidation.LatitudeValidation(OptionalLocationEntry.LatitudeEntry.UserValue.Value);
        var longitudeValidation =
            await CommonContentValidation.LongitudeValidation(OptionalLocationEntry.LongitudeEntry.UserValue.Value);

        if (!latitudeValidation.Valid || !longitudeValidation.Valid)
        {
            await StatusContext.ToastError("Latitude/Longitude is not valid?");
            return;
        }

        var frozenNow = DateTime.Now;

        var newPartialPoint = PointContent.CreateInstance();

        newPartialPoint.CreatedOn = frozenNow;
        newPartialPoint.FeedOn = frozenNow;
        newPartialPoint.BodyContent = BracketCodeTrails.Create(DbEntry);
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

        var (generationReturn, newContent) = await TrailGenerator.SaveAndGenerateHtml(CurrentStateToTrailContent(),
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
    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry.Id < 1)
        {
            await StatusContext.ToastError("Please save the content first...");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $"{settings.TrailPageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}