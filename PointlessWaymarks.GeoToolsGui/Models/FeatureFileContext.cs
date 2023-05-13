using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.GeoToolsGui.Models;

public partial class FeatureFileContext : ObservableObject
{
    [ObservableProperty] private List<string> _attributesForTags = new();
    [ObservableProperty] private Guid _contentId = Guid.NewGuid();
    [ObservableProperty] private string _downloaded = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _source = string.Empty;
    [ObservableProperty] private string _tagAll = string.Empty;
}