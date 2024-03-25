using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class BooleanSearchFieldBuilder
{
    public required string FieldTitle { get; set; }
    public bool SearchBoolean { get; set; }
}