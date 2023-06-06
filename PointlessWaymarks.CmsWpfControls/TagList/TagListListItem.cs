using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.TagList;

[NotifyPropertyChanged]
public partial class TagListListItem
{
    public int ContentCount { get; set; }
    public List<TagItemContentInformation> ContentInformation { get; set; } = new();
    public bool IsExcludedTag { get; set; }
    public string TagName { get; set; } = string.Empty;
}