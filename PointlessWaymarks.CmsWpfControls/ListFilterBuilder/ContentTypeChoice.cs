using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
public partial class ContentTypeListFilterChoice
{
    public bool IsSelected { get; set; }
    public required string TypeDescription { get; set; }
}