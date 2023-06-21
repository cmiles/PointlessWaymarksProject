using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui.Models;

[NotifyPropertyChanged]
public partial class FeatureFileContext
{
    public List<string> AttributesForTags { get; set; } = new();
    public Guid ContentId { get; set; } = Guid.NewGuid();
    public string Downloaded { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string TagAll { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}