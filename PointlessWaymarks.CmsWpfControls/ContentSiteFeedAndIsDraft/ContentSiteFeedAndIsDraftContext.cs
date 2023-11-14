using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.DataEntry;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ContentSiteFeedAndIsDraft;

[NotifyPropertyChanged]
public partial class ContentSiteFeedAndIsDraftContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    private ContentSiteFeedAndIsDraftContext(StatusControlContext statusContext, IMainSiteFeed mainSiteFeed,
        ConversionDataEntryContext<DateTime> feedOnContext, BoolDataEntryContext isDraftContext,
        BoolDataEntryContext showInMainSiteFeedContext)
    {
        StatusContext = statusContext;

        DbEntry = mainSiteFeed;

        FeedOnEntry = feedOnContext;
        IsDraftEntry = isDraftContext;
        ShowInMainSiteFeedEntry = showInMainSiteFeedContext;

        PropertyChanged += OnPropertyChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public IMainSiteFeed DbEntry { get; set; }
    public ConversionDataEntryContext<DateTime> FeedOnEntry { get; set; }
    public BoolDataEntryContext IsDraftEntry { get; set; }
    public BoolDataEntryContext ShowInMainSiteFeedEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);

        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public static async Task<ContentSiteFeedAndIsDraftContext> CreateInstance(StatusControlContext statusContext,
        IMainSiteFeed dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryIsDraftContest = await BoolDataEntryTypes.CreateInstanceForIsDraft(dbEntry, false);
        var factoryShowInMainSiteFeedContext =
            await BoolDataEntryTypes.CreateInstanceForShowInMainSiteFeed(dbEntry, false);

        var factoryFeedContext =
            await ConversionDataEntryContext<DateTime>.CreateInstance(ConversionDataEntryHelpers.DateTimeConversion);
        factoryFeedContext.Title = "Show in Site Feeds On";
        factoryFeedContext.HelpText =
            "Sets when (if enabled) the content will appear on the Front Page and in RSS Feeds";
        factoryFeedContext.ReferenceValue = dbEntry.FeedOn;
        factoryFeedContext.UserText = dbEntry.FeedOn.ToString("MM/dd/yyyy h:mm:ss tt");

        var newItem = new ContentSiteFeedAndIsDraftContext(statusContext, dbEntry, factoryFeedContext,
            factoryIsDraftContest, factoryShowInMainSiteFeedContext);
        newItem.CheckForChangesAndValidationIssues();

        return newItem;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}