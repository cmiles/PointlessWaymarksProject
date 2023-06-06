using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.TagList;

[NotifyPropertyChanged]
public partial class TagItemContentInformation
{
    public Guid ContentId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}