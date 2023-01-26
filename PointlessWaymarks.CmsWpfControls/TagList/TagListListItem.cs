using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.TagList;

public partial class TagListListItem : ObservableObject
{
    [ObservableProperty] private int _contentCount;
    [ObservableProperty] private List<TagItemContentInformation> _contentInformation;
    [ObservableProperty] private bool _isExcludedTag;
    [ObservableProperty] private string _tagName;
}