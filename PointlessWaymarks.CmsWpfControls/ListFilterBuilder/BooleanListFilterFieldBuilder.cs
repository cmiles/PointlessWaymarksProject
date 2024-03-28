using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
public partial class BooleanListFilterFieldBuilder
{
    public required string FieldTitle { get; set; }
    public bool SearchBoolean { get; set; }
}