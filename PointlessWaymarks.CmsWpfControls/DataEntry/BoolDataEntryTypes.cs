using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.BoolDataEntry;

namespace PointlessWaymarks.CmsWpfControls.DataEntry;

public static class BoolDataEntryTypes
{
    public static async Task<BoolDataEntryContext> CreateInstanceForIsDraft(IMainSiteFeed? dbEntry, bool defaultSetting)
    {
        var newContext = await BoolDataEntryContext.CreateInstance();

        newContext.ReferenceValue = dbEntry?.IsDraft ?? defaultSetting;
        newContext.UserValue = dbEntry?.IsDraft ?? defaultSetting;
        newContext.Title = "Draft";
        newContext.HelpText =
            "'Draft' content will not appear in the Main Site Feed, Search or RSS Feeds - however html will " +
            "still be generated for the content, this is NOT a way to keep content hidden or secret!";

        return newContext;
    }

    public static async Task<BoolDataEntryContext> CreateInstanceForShowInMainSiteFeed(IMainSiteFeed? dbEntry, bool defaultSetting)
    {
        var newContext = await BoolDataEntryContext.CreateInstance();

        newContext.ReferenceValue = dbEntry?.ShowInMainSiteFeed ?? defaultSetting;
        newContext.UserValue = dbEntry?.ShowInMainSiteFeed ?? defaultSetting;
        newContext.Title = "Show in Main Site Feed";
        newContext.HelpText =
            "Checking this box will make the content appear in the Main Site RSS Feed and - if the content is recent - on the site's homepage";

        return newContext;
    }

    public static async Task<BoolDataEntryContext> CreateInstanceForShowInSearch(IShowInSearch? dbEntry, bool defaultSetting)
    {
        var newContext = await BoolDataEntryContext.CreateInstance();

        newContext.ReferenceValue = dbEntry?.ShowInSearch ?? defaultSetting;
        newContext.UserValue = dbEntry?.ShowInSearch ?? defaultSetting;
        newContext.Title = "Show in Search";
        newContext.HelpText =
            "If checked the content will appear in Site, Tag and other search screens - otherwise the content will still be " +
            "on the site and publicly available but it will not show in search";

        return newContext;
    }
}