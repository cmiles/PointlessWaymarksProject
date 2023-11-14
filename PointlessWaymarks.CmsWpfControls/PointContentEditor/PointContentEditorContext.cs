using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using NetTopologySuite.Features;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.BodyContentEditor;
using PointlessWaymarks.CmsWpfControls.ContentIdViewer;
using PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.PointDetailEditor;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using Point = NetTopologySuite.Geometries.Point;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PointContentEditorContext : IHasChanges, ICheckForChangesAndValidation,
    IHasValidationIssues
{
    public EventHandler? RequestContentEditorWindowClose;

    private PointContentEditorContext(StatusControlContext statusContext, PointContent pointContent)
    {
        StatusContext = statusContext;

        BuildCommands();

        DbEntry = pointContent;

        PropertyChanged += OnPropertyChanged;
    }

    public BodyContentEditorContext? BodyContent { get; set; }
    public bool BroadcastLatLongChange { get; set; } = true;
    public ContentIdViewerControlContext? ContentId { get; set; }
    public CreatedAndUpdatedByAndOnDisplayContext? CreatedUpdatedDisplay { get; set; }
    public PointContent DbEntry { get; set; }
    public ConversionDataEntryContext<double?>? ElevationEntry { get; set; }
    public HelpDisplayContext? HelpContext { get; set; }
    public ConversionDataEntryContext<double>? LatitudeEntry { get; set; }
    public ConversionDataEntryContext<double>? LongitudeEntry { get; set; }
    public ContentSiteFeedAndIsDraftContext? MainSiteFeed { get; set; }
    public StringDataEntryContext? MapLabelContent { get; set; }
    public PointDetailListContext? PointDetails { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public TagsEditorContext? TagEdit { get; set; }
    public TitleSummarySlugEditorContext? TitleSummarySlugFolder { get; set; }
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

    public static async Task<PointContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        PointContent? pointContent)
    {
        var newControl = new PointContentEditorContext(statusContext ?? new StatusControlContext(),
            NewContentModels.InitializePointContent(pointContent));
        await newControl.LoadData(pointContent);
        return newControl;
    }

    private PointContent CurrentStateToPointContent()
    {
        var newEntry = PointContent.CreateInstance();

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
        newEntry.MapLabel = MapLabelContent!.UserValue.TrimNullToEmpty();

        newEntry.Latitude = LatitudeEntry!.UserValue;
        newEntry.Longitude = LongitudeEntry!.UserValue;
        newEntry.Elevation = ElevationEntry!.UserValue;

        return newEntry;
    }

    private PointContentDto CurrentStateToPointContentDto()
    {
        var toReturn = new PointContentDto();
        var currentPoint = CurrentStateToPointContent();
        toReturn.InjectFrom(currentPoint);
        toReturn.PointDetails = PointDetails!.CurrentStateToPointDetailsList();
        toReturn.PointDetails.ForEach(x => x.PointContentId = toReturn.ContentId);
        return toReturn;
    }


    [BlockingCommand]
    public async Task ExtractNewLinks()
    {
        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{BodyContent!.BodyContent} {UpdateNotes!.UpdateNotes}",
            StatusContext.ProgressTracker());
    }

    public Task<IFeature?> FeatureFromPoint()
    {
        if (LatitudeEntry!.HasValidationIssues || LongitudeEntry!.HasValidationIssues)
            return Task.FromResult((IFeature?)null);

        if (ElevationEntry!.UserValue is null)
            return Task.FromResult((IFeature?)new Feature(
                new Point(LongitudeEntry.UserValue, LatitudeEntry.UserValue),
                new AttributesTable()));
        return Task.FromResult((IFeature?)new Feature(
            new Point(LongitudeEntry.UserValue, LatitudeEntry.UserValue, ElevationEntry.UserValue.Value),
            new AttributesTable()));
    }

    [BlockingCommand]
    public async Task GetElevation()
    {
        if (LatitudeEntry!.HasValidationIssues || LongitudeEntry!.HasValidationIssues)
        {
            StatusContext.ToastError("Lat Long is not valid");
            return;
        }

        var possibleElevation =
            await ElevationGuiHelper.GetElevation(LatitudeEntry.UserValue, LongitudeEntry.UserValue, StatusContext);

        if (possibleElevation != null) ElevationEntry!.UserText = possibleElevation.Value.ToString("F2");
    }

    private void LatitudeLongitudeChangeBroadcast()
    {
        if (BroadcastLatLongChange && !LatitudeEntry!.HasValidationIssues && !LongitudeEntry!.HasValidationIssues)
            RaisePointLatitudeLongitudeChange?.Invoke(this,
                new PointLatitudeLongitudeChange(LatitudeEntry.UserValue, LongitudeEntry.UserValue));
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

        var linkString = BracketCodePointLinks.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(PointContent? toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = NewContentModels.InitializePointContent(toLoad);

        TitleSummarySlugFolder =
            await TitleSummarySlugEditorContext.CreateInstance(StatusContext, DbEntry, null, null, null);
        CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
        MainSiteFeed = await ContentSiteFeedAndIsDraftContext.CreateInstance(StatusContext, DbEntry);
        ContentId = await ContentIdViewerControlContext.CreateInstance(StatusContext, DbEntry);
        UpdateNotes = await UpdateNotesEditorContext.CreateInstance(StatusContext, DbEntry);
        TagEdit = await TagsEditorContext.CreateInstance(StatusContext, DbEntry);
        BodyContent = await BodyContentEditorContext.CreateInstance(StatusContext, DbEntry);

        MapLabelContent = StringDataEntryContext.CreateInstance();
        MapLabelContent.Title = "Map Label";
        MapLabelContent.HelpText =
            "This text will be used to identify the point on a map. A very short string is likely best...";
        MapLabelContent.ReferenceValue = DbEntry.MapLabel ?? string.Empty;
        MapLabelContent.UserValue = StringTools.NullToEmptyTrim(DbEntry.MapLabel);

        ElevationEntry =
            await ConversionDataEntryContext<double?>.CreateInstance(
                ConversionDataEntryHelpers.DoubleNullableConversion);
        ElevationEntry.ValidationFunctions = new List<Func<double?, Task<IsValid>>>
        {
            CommonContentValidation.ElevationValidation
        };
        ElevationEntry.ComparisonFunction = (o, u) => (o == null && u == null) || o.IsApproximatelyEqualTo(u, .001);
        ElevationEntry.Title = "Elevation";
        ElevationEntry.HelpText = "Elevation in Feet";
        ElevationEntry.ReferenceValue = DbEntry.Elevation;
        ElevationEntry.UserText = DbEntry.Elevation?.ToString("F2") ?? string.Empty;

        LatitudeEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LatitudeEntry.ValidationFunctions = new List<Func<double, Task<IsValid>>>
        {
            CommonContentValidation.LatitudeValidation
        };
        LatitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
        LatitudeEntry.Title = "Latitude";
        LatitudeEntry.HelpText = "In DDD.DDDDDD°";
        LatitudeEntry.ReferenceValue = DbEntry.Latitude;
        LatitudeEntry.UserText = DbEntry.Latitude.ToString("F6");
        LatitudeEntry.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
            if (args.PropertyName == nameof(LatitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
        };

        LongitudeEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LongitudeEntry.ValidationFunctions = new List<Func<double, Task<IsValid>>>
        {
            CommonContentValidation.LongitudeValidation
        };
        LongitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
        LongitudeEntry.Title = "Longitude";
        LongitudeEntry.HelpText = "In DDD.DDDDDD°";
        LongitudeEntry.ReferenceValue = DbEntry.Longitude;
        LongitudeEntry.UserText = DbEntry.Longitude.ToString("F6");
        LongitudeEntry.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
            if (args.PropertyName == nameof(LongitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
        };

        PointDetails = await PointDetailListContext.CreateInstance(StatusContext, DbEntry);

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    public void OnRaisePointLatitudeLongitudeChange(object? sender, PointLatitudeLongitudeChange e)
    {
        BroadcastLatLongChange = false;

        LatitudeEntry!.UserText = e.Latitude.ToString("F6");
        LongitudeEntry!.UserText = e.Longitude.ToString("F6");

        BroadcastLatLongChange = true;
    }

    public event EventHandler<PointLatitudeLongitudeChange>? RaisePointLatitudeLongitudeChange;

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

        var (generationReturn, newContent) = await PointGenerator.SaveAndGenerateHtml(CurrentStateToPointContentDto(),
            null, StatusContext.ProgressTracker());

        if (generationReturn.HasError || newContent == null)
        {
            await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                generationReturn.GenerationNote);
            return;
        }

        await LoadData(Db.PointContentDtoToPointContentAndDetails(newContent).content);

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
            StatusContext.ToastError("Please save the content first...");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.PointPageUrl(DbEntry)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}