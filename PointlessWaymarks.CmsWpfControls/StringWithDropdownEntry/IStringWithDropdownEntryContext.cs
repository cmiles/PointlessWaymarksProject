using System.Collections.ObjectModel;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.StringWithDropdownEntry;

public interface IStringWithDropdownEntryContext : IHasChanges, IHasValidationIssues
{
    ObservableCollection<string> ExistingChoices { get; set; }
    string HelpText { get; set; }
    string? ReferenceValue { get; set; }
    StatusControlContext StatusContext { get; set; }
    string Title { get; set; }
    string? UserValue { get; set; }
    string ValidationMessage { get; set; }
}