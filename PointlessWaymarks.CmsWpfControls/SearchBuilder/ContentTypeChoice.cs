using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class ContentTypeSearchChoice
{
    public bool IsSelected { get; set; }
    public required string TypeDescription { get; set; }
}