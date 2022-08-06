using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.TagList;

[ObservableObject]
public partial class TagItemContentInformation
{
    [ObservableProperty] private Guid _contentId;
    [ObservableProperty] private string _contentType;
    [ObservableProperty] private string _tags;
    [ObservableProperty] private string _title;
}