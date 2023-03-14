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
    [ObservableProperty] private IMainSiteFeed _dbEntry;
    [ObservableProperty] private ConversionDataEntryContext<DateTime> _feedOnEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private BoolDataEntryContext _isDraftEntry;
    [ObservableProperty] private BoolDataEntryContext _showInMainSiteFeedEntry;
    [ObservableProperty] private StatusControlContext _statusContext;

    private ContentSiteFeedAndIsDraftContext(StatusControlContext statusContext, IMainSiteFeed mainSiteFeed,
        ConversionDataEntryContext<DateTime> feedOnContext, BoolDataEntryContext isDraftContext,
        BoolDataEntryContext showInMainSiteFeedContext)
    {
        _statusContext = statusContext;

        _dbEntry = mainSiteFeed;

        _feedOnEntry = feedOnContext;
        _isDraftEntry = isDraftContext;
        _showInMainSiteFeedEntry = showInMainSiteFeedContext;

        PropertyChanged += OnPropertyChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public static async Task<ContentSiteFeedAndIsDraftContext> CreateInstance(StatusControlContext statusContext,
        IMainSiteFeed dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryIsDraftContest = BoolDataEntryTypes.CreateInstanceForIsDraft(dbEntry, false);
        var factoryShowInMainSiteFeedContext = BoolDataEntryTypes.CreateInstanceForShowInMainSiteFeed(dbEntry, false);

        var factoryFeedContext =
            ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
        factoryFeedContext.Title = "Show in Site Feeds On";
        factoryFeedContext.HelpText = "Sets when (if enabled) the content will appear on the Front Page and in RSS Feeds";
        factoryFeedContext.ReferenceValue = dbEntry.FeedOn;
        factoryFeedContext.UserText = dbEntry.FeedOn.ToString("MM/dd/yyyy h:mm:ss tt");

        var newItem = new ContentSiteFeedAndIsDraftContext(statusContext, dbEntry, factoryFeedContext, factoryIsDraftContest, factoryShowInMainSiteFeedContext);

        return newItem;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}