using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;

public partial class ContentSiteFeedAndIsDraftContext : ObservableObject, IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private IMainSiteFeed? _dbEntry;
    [ObservableProperty] private ConversionDataEntryContext<DateTime> _feedOnEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private BoolDataEntryContext? _isDraftEntry;
    [ObservableProperty] private BoolDataEntryContext? _showInMainSiteFeedEntry;
    [ObservableProperty] private StatusControlContext _statusContext;

    public ContentSiteFeedAndIsDraftContext(StatusControlContext? statusContext)
    {
        StatusContext = statusContext ?? new();
        PropertyChanged += OnPropertyChanged;
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<ContentSiteFeedAndIsDraftContext?> CreateInstance(StatusControlContext? statusContext,
        IMainSiteFeed? dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItem = new ContentSiteFeedAndIsDraftContext(statusContext);
        await newItem.LoadData(dbEntry);

        return newItem;
    }

    public async Task LoadData(IMainSiteFeed? dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = dbEntry;

        ShowInMainSiteFeedEntry = BoolDataEntryTypes.CreateInstanceForShowInMainSiteFeed(DbEntry, false);

        FeedOnEntry =
            ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
        FeedOnEntry.Title = "Show in Site Feeds On";
        FeedOnEntry.HelpText = "Sets when (if enabled) the content will appear on the Front Page and in RSS Feeds";
        FeedOnEntry.ReferenceValue = DbEntry.FeedOn;
        FeedOnEntry.UserText = DbEntry.FeedOn.ToString("MM/dd/yyyy h:mm:ss tt");

        IsDraftEntry = BoolDataEntryTypes.CreateInstanceForIsDraft(DbEntry, false);

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}