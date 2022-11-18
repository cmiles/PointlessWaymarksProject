using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui.Models;

[ObservableObject]
public partial class IntersectFileViewModel
{
    [ObservableProperty] private List<string> _attributesForTags = new();
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _source = string.Empty;
    [ObservableProperty] private string _tagAll = string.Empty;
}