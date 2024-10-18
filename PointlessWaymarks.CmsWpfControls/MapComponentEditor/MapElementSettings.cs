using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementSettings : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    public required BoolDataEntryContext InInitialView { get; set; }
    public required BoolDataEntryContext IsFeaturedElement { get; set; }
    public required StringDataEntryContext LinkTo { get; set; }
    public required BoolDataEntryContext ShowInitialDetails { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    public bool HasChanges { get; set; }

    public bool HasValidationIssues { get; set; }

    public static async Task<MapElementSettings> CreateInstance(MapElement? mapElement)
    {
        var factoryInInitialEditor = await BoolDataEntryContext.CreateInstance();
        factoryInInitialEditor.Title = "In Initial View";
        factoryInInitialEditor.HelpText =
            "Determines if this element is used in calculating the bounding box for the initial map view.";
        factoryInInitialEditor.UserValue = mapElement?.IncludeInDefaultView ?? false;

        var factoryIsFeaturedEditor = await BoolDataEntryContext.CreateInstance();
        factoryIsFeaturedEditor.Title = "Featured";
        factoryIsFeaturedEditor.HelpText = "Is the element Featured.";
        factoryIsFeaturedEditor.UserValue = mapElement?.IsFeaturedElement ?? false;

        var factoryShowInitialDetailsEditor = await BoolDataEntryContext.CreateInstance();
        factoryShowInitialDetailsEditor.Title = "Show Details";
        factoryShowInitialDetailsEditor.HelpText =
            "If checked the element will have its popup open when the map is loaded.";
        factoryShowInitialDetailsEditor.UserValue = mapElement?.ShowDetailsDefault ?? false;

        var factoryLinkToEditor = StringDataEntryContext.CreateInstance();
        factoryLinkToEditor.Title = "Link To";
        factoryLinkToEditor.HelpText =
            "Specify a Bracket Code or hyperlink (include/start with http...) that will override the link used in the map element popup - this is useful in the case of a line where you want to direct the user back to a Post about a hike rather than the content page for the line.";
        factoryLinkToEditor.UserValue = mapElement?.LinksTo ?? string.Empty;

        var toReturn = new MapElementSettings
        {
            InInitialView = factoryInInitialEditor,
            IsFeaturedElement = factoryIsFeaturedEditor,
            LinkTo = factoryLinkToEditor,
            ShowInitialDetails = factoryShowInitialDetailsEditor
        };

        return toReturn;
    }
}