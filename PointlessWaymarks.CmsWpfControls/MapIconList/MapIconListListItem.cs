using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapIconList;

[NotifyPropertyChanged]
public partial class MapIconListListItem : ISelectedTextTracker
{
    public required MapIcon DbEntry { get; set; }
    public string IconName { get; set; } = string.Empty;
    public required StringDataEntryContext IconNameEntry { get; set; }
    public string IconSource { get; set; } = string.Empty;
    public required StringDataEntryContext IconSourceEntry { get; set; }
    public string IconSvg { get; set; } = string.Empty;
    public required StringDataEntryContext IconSvgEntry { get; set; }
    public required StringDataEntryContext LastUpdatedByEntry { get; set; }
    public CurrentSelectedTextTracker? SelectedTextTracker { get; set; } = new();

    public static MapIconListListItem CreateInstance(MapIcon dbEntry)
    {
        //TODO:Add validation

        var factoryIconName = StringDataEntryContext.CreateInstance();
        factoryIconName.Title = "Icon Name";
        factoryIconName.UserValue = dbEntry.IconName ?? string.Empty;
        factoryIconName.ReferenceValue = dbEntry.IconName ?? string.Empty;
        factoryIconName.HelpText = "The name of the icon - can be used for reference with Point Content.";

        var factorySvgEntry = StringDataEntryContext.CreateInstance();
        factorySvgEntry.Title = "SVG (<svg></svg>)";
        factorySvgEntry.UserValue = dbEntry.IconSvg ?? string.Empty;
        factorySvgEntry.ReferenceValue = dbEntry.IconSvg ?? string.Empty;
        factorySvgEntry.HelpText = "The SVG Tag for the Icon";

        var factorySource = StringDataEntryContext.CreateInstance();
        factorySource.Title = "Icon Source";
        factorySource.UserValue = dbEntry.IconSource ?? string.Empty;
        factorySource.ReferenceValue = dbEntry.IconSource ?? string.Empty;
        factorySource.HelpText = "The source of the Icon";

        var factoryLastUpdatedBy = StringDataEntryContext.CreateInstance();
        factoryLastUpdatedBy.Title = "Last Updated By";
        factoryLastUpdatedBy.UserValue = dbEntry.LastUpdatedBy ?? string.Empty;
        factoryLastUpdatedBy.ReferenceValue = dbEntry.LastUpdatedBy ?? string.Empty;
        factoryLastUpdatedBy.HelpText = "Last updated by";

        return new MapIconListListItem
        {
            DbEntry = dbEntry,
            IconName = dbEntry.IconName ?? string.Empty,
            IconSource = dbEntry.IconSource ?? string.Empty,
            IconSvg = dbEntry.IconSvg ?? string.Empty,
            IconNameEntry = factoryIconName,
            IconSourceEntry = factorySource,
            IconSvgEntry = factorySvgEntry,
            LastUpdatedByEntry = factoryLastUpdatedBy
        };
    }
}