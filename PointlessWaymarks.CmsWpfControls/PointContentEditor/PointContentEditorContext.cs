using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
using PointlessWaymarks.CmsWpfControls.ConversionDataEntry;
using PointlessWaymarks.CmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CmsWpfControls.PointDetailEditor;
using PointlessWaymarks.CmsWpfControls.StringDataEntry;
using PointlessWaymarks.CmsWpfControls.TagsEditor;
using PointlessWaymarks.CmsWpfControls.TitleSummarySlugFolderEditor;
using PointlessWaymarks.CmsWpfControls.UpdateNotesEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

[ObservableObject]
public partial class PointContentEditorContext : IHasChanges, ICheckForChangesAndValidation, IHasValidationIssues
{
    [ObservableProperty] private BodyContentEditorContext _bodyContent;
    [ObservableProperty] private bool _broadcastLatLongChange = true;
    [ObservableProperty] private ContentIdViewerControlContext _contentId;
    [ObservableProperty] private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
    [ObservableProperty] private PointContent _dbEntry;
    [ObservableProperty] private ConversionDataEntryContext<double?> _elevationEntry;
    [ObservableProperty] private RelayCommand _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand _getElevationCommand;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private HelpDisplayContext _helpContext;
    [ObservableProperty] private ConversionDataEntryContext<double> _latitudeEntry;
    [ObservableProperty] private RelayCommand _linkToClipboardCommand;
    [ObservableProperty] private ConversionDataEntryContext<double> _longitudeEntry;
    [ObservableProperty] private ContentSiteFeedAndIsDraftContext _mainSiteFeed;
    [ObservableProperty] private PointDetailListContext _pointDetails;
    [ObservableProperty] private RelayCommand _saveAndCloseCommand;
    [ObservableProperty] private RelayCommand _saveCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private TagsEditorContext _tagEdit;
    [ObservableProperty] private StringDataEntryContext _textMarkerTextContent;
    [ObservableProperty] private TitleSummarySlugEditorContext _titleSummarySlugFolder;
    [ObservableProperty] private UpdateNotesEditorContext _updateNotes;
    [ObservableProperty] private RelayCommand _viewOnSiteCommand;

    public EventHandler RequestContentEditorWindowClose;

    private PointContentEditorContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        PropertyChanged += OnPropertyChanged;

        SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
        SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand(ViewOnSite);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand(() =>
            LinkExtraction.ExtractNewAndShowLinkContentEditors($"{BodyContent.BodyContent} {UpdateNotes.UpdateNotes}",
                StatusContext.ProgressTracker()));
        GetElevationCommand = StatusContext.RunBlockingTaskCommand(GetElevation);
        LinkToClipboardCommand = StatusContext.RunBlockingTaskCommand(LinkToClipboard);

        HelpContext = new HelpDisplayContext(new List<string>
        {
            CommonFields.TitleSlugFolderSummary, BracketCodeHelpMarkdown.HelpBlock
        });
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<PointContentEditorContext> CreateInstance(StatusControlContext statusContext,
        PointContent pointContent)
    {
        var newControl = new PointContentEditorContext(statusContext);
        await newControl.LoadData(pointContent);
        return newControl;
    }

    private PointContent CurrentStateToPointContent()
    {
        var newEntry = new PointContent();

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

        newEntry.Latitude = LatitudeEntry.UserValue;
        newEntry.Longitude = LongitudeEntry.UserValue;
        newEntry.Elevation = ElevationEntry.UserValue;

        return newEntry;
    }

    private PointContentDto CurrentStateToPointContentDto()
    {
        var toReturn = new PointContentDto();
        var currentPoint = CurrentStateToPointContent();
        toReturn.InjectFrom(currentPoint);
        toReturn.PointDetails = PointDetails.CurrentStateToPointDetailsList() ?? new List<PointDetail>();
        toReturn.PointDetails.ForEach(x => x.PointContentId = toReturn.ContentId);
        return toReturn;
    }

    public async Task GetElevation()
    {
        if (LatitudeEntry.HasValidationIssues || LongitudeEntry.HasValidationIssues)
        {
            StatusContext.ToastError("Lat Long is not valid");
            return;
        }

        var possibleElevation =
            await ElevationGuiHelper.GetElevation(LatitudeEntry.UserValue, LongitudeEntry.UserValue, StatusContext);

        if (possibleElevation != null) ElevationEntry.UserText = possibleElevation.Value.ToString("F2");
    }

    private void LatitudeLongitudeChangeBroadcast()
    {
        if (_broadcastLatLongChange && !LatitudeEntry.HasValidationIssues && !LongitudeEntry.HasValidationIssues)
            RaisePointLatitudeLongitudeChange?.Invoke(this,
                new PointLatitudeLongitudeChange(LatitudeEntry.UserValue, LongitudeEntry.UserValue));
    }

    private async Task LinkToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
        {
            StatusContext.ToastError("Sorry - please save before getting link...");
            return;
        }

        var linkString = BracketCodePointLinks.Create(DbEntry);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(linkString);

        StatusContext.ToastSuccess($"To Clipboard: {linkString}");
    }

    public async Task LoadData(PointContent toLoad)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var created = DateTime.Now;

        DbEntry = toLoad ?? new PointContent
        {
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            Latitude = UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            Longitude = UserSettingsSingleton.CurrentSettings().LongitudeDefault,
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

        TextMarkerTextContent = StringDataEntryContext.CreateInstance();
        TextMarkerTextContent.Title = "Text Marker";
        TextMarkerTextContent.HelpText =
            "This text will be used as the 'marker' on the map instead of a standard symbol. A very short string is likely best...";
        TextMarkerTextContent.ReferenceValue = DbEntry.TextMarkerText ?? string.Empty;
        TextMarkerTextContent.UserValue = StringHelpers.NullToEmptyTrim(DbEntry?.TextMarkerText);

        ElevationEntry =
            ConversionDataEntryContext<double?>.CreateInstance(ConversionDataEntryHelpers.DoubleNullableConversion);
        ElevationEntry.ValidationFunctions = new List<Func<double?, IsValid>>
        {
            CommonContentValidation.ElevationValidation
        };
        ElevationEntry.ComparisonFunction = (o, u) => (o == null && u == null) || o.IsApproximatelyEqualTo(u, .001);
        ElevationEntry.Title = "Elevation";
        ElevationEntry.HelpText = "Elevation in Feet";
        ElevationEntry.ReferenceValue = DbEntry.Elevation;
        ElevationEntry.UserText = DbEntry.Elevation?.ToString("F2") ?? string.Empty;

        LatitudeEntry = ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LatitudeEntry.ValidationFunctions = new List<Func<double, IsValid>>
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

        LongitudeEntry = ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LongitudeEntry.ValidationFunctions = new List<Func<double, IsValid>>
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

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    public void OnRaisePointLatitudeLongitudeChange(object sender, PointLatitudeLongitudeChange e)
    {
        _broadcastLatLongChange = false;

        LatitudeEntry.UserText = e.Latitude.ToString("F6");
        LongitudeEntry.UserText = e.Longitude.ToString("F6");

        _broadcastLatLongChange = true;
    }

    public event EventHandler<PointLatitudeLongitudeChange> RaisePointLatitudeLongitudeChange;

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


    private async Task ViewOnSite()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (DbEntry == null || DbEntry.Id < 1)
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