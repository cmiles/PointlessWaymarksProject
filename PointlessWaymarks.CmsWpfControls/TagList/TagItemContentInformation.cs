using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.TagList;

public partial class TagItemContentInformation : ObservableObject
{
    [ObservableProperty] private Guid _contentId;
    [ObservableProperty] private string _contentType = string.Empty;
    [ObservableProperty] private string _tags = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
}