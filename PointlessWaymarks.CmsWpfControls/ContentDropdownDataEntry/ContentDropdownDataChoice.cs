using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.ContentDropdownDataEntry;

[NotifyPropertyChanged]
public partial class ContentDropdownDataChoice
{
    public required Guid ContentId { get; set; }
    public required string DisplayString { get; set; } = string.Empty;
}