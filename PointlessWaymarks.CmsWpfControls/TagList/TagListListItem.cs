using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.TagList;

[ObservableObject]
public partial class TagListListItem
{
    [ObservableProperty] private int _contentCount;
    [ObservableProperty] private List<TagItemContentInformation> _contentInformation;
    [ObservableProperty] private bool _isExcludedTag;
    [ObservableProperty] private string _tagName;
}